using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Hosts a <see cref="FilterRowPanel"/> beneath the column headers of a
    /// <see cref="SearchDataGrid"/>, parenting one <see cref="ColumnFilterControl"/> per data
    /// column. Tracks column collection changes, per-column ActualWidth / DisplayIndex /
    /// Visibility, and the grid's horizontal scroll offset; layout work is coalesced to
    /// <see cref="DispatcherPriority.Render"/> so a drag-resize doesn't remeasure per pixel.
    /// </summary>
    public class FilterRowPresenter : Control
    {
        static FilterRowPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FilterRowPresenter),
                new FrameworkPropertyMetadata(typeof(FilterRowPresenter)));
        }

        public const string PartFilterRowPanelName = "PART_FilterRowPanel";

        private SearchDataGrid _grid;
        private FilterRowPanel _panel;
        private ScrollViewer _scrollViewer;
        private bool _refreshScheduled;

        // Snapshot of (X, ActualWidth) per column header taken on the most recent panel
        // arrange. LayoutUpdated compares current header geometry against this and re-arranges
        // the panel when the headers shifted (resize drag, reorder, initial layout settle).
        private List<(DataGridColumn column, double x, double width)> _lastHeaderSnapshot;
        private bool _layoutUpdatedSubscribed;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _panel = GetTemplateChild(PartFilterRowPanelName) as FilterRowPanel;
            TryAttachToGrid();
            RebuildChildren();
            EnsureLayoutUpdatedSubscribed();
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            TryAttachToGrid();
            RebuildChildren();
        }

        private void TryAttachToGrid()
        {
            var newGrid = FindAncestorGrid();
            if (ReferenceEquals(newGrid, _grid)) return;

            DetachFromGrid();
            _grid = newGrid;
            if (_grid == null) return;

            if (_panel != null)
                _panel.OwnerGrid = _grid;

            _grid.Columns.CollectionChanged += OnColumnsCollectionChanged;
            foreach (var col in _grid.Columns)
                SubscribeColumn(col);

            AttachScrollViewer();
        }

        private void DetachFromGrid()
        {
            if (_grid == null) return;
            _grid.Columns.CollectionChanged -= OnColumnsCollectionChanged;
            foreach (var col in _grid.Columns)
                UnsubscribeColumn(col);
            DetachScrollViewer();
            if (_panel != null)
                _panel.OwnerGrid = null;
            _grid = null;
        }

        private SearchDataGrid FindAncestorGrid()
        {
            DependencyObject d = this;
            while (d != null)
            {
                if (d is SearchDataGrid grid) return grid;
                d = VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d);
            }
            return null;
        }

        private void AttachScrollViewer()
        {
            // DG_ScrollViewer may not be applied yet when the presenter is first parented;
            // retry on the next Loaded tick.
            _scrollViewer = _grid?.Template?.FindName("DG_ScrollViewer", _grid) as ScrollViewer;
            if (_scrollViewer == null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _scrollViewer = _grid?.Template?.FindName("DG_ScrollViewer", _grid) as ScrollViewer;
                    if (_scrollViewer != null)
                    {
                        _scrollViewer.ScrollChanged += OnGridScrollChanged;
                        SyncScrollOffset();
                    }
                }), DispatcherPriority.Loaded);
                return;
            }

            _scrollViewer.ScrollChanged += OnGridScrollChanged;
            SyncScrollOffset();
        }

        private void DetachScrollViewer()
        {
            if (_scrollViewer == null) return;
            _scrollViewer.ScrollChanged -= OnGridScrollChanged;
            _scrollViewer = null;
        }

        private void OnGridScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange == 0 && e.ViewportWidthChange == 0 && e.ExtentWidthChange == 0)
                return;
            SyncScrollOffset();
        }

        private void SyncScrollOffset()
        {
            if (_panel == null || _scrollViewer == null) return;
            _panel.HorizontalOffset = _scrollViewer.HorizontalOffset;
        }

        private void OnColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (DataGridColumn col in e.OldItems)
                    UnsubscribeColumn(col);

            if (e.NewItems != null)
                foreach (DataGridColumn col in e.NewItems)
                    SubscribeColumn(col);

            ScheduleRebuild();
        }

        // Use DependencyPropertyDescriptor (same pattern as DataGridColumnHeadersPresenter) —
        // ActualWidth isn't always a DP change on the column DO under all width-resolve paths.
        private void SubscribeColumn(DataGridColumn col)
        {
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn))
                .AddValueChanged(col, OnColumnLayoutChanged);
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.DisplayIndexProperty, typeof(DataGridColumn))
                .AddValueChanged(col, OnColumnLayoutChanged);
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn))
                .AddValueChanged(col, OnColumnLayoutChanged);
        }

        private void UnsubscribeColumn(DataGridColumn col)
        {
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn))
                .RemoveValueChanged(col, OnColumnLayoutChanged);
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.DisplayIndexProperty, typeof(DataGridColumn))
                .RemoveValueChanged(col, OnColumnLayoutChanged);
            DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn))
                .RemoveValueChanged(col, OnColumnLayoutChanged);
        }

        private void OnColumnLayoutChanged(object sender, EventArgs e)
        {
            // Resize drags fire ActualWidth per mouse-move — coalesce to once per render frame.
            if (_panel == null) return;
            ScheduleInvalidatePanel();
        }

        private void ScheduleInvalidatePanel()
        {
            if (_refreshScheduled || _panel == null) return;
            _refreshScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshScheduled = false;
                _panel?.InvalidateMeasure();
                _panel?.InvalidateArrange();
            }), DispatcherPriority.Render);
        }

        private bool _rebuildScheduled;

        private void ScheduleRebuild()
        {
            if (_panel == null) return;
            // Coalesce: column-by-column generation fires N CollectionChanged events that
            // all resolve to the same final column set — one rebuild per tick is enough,
            // and avoids tangling the column-filter-control registry on intermediate states.
            if (_rebuildScheduled) return;
            _rebuildScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _rebuildScheduled = false;
                RebuildChildren();
            }), DispatcherPriority.Render);
        }

        private void RebuildChildren()
        {
            if (_panel == null || _grid == null) return;

            // OwnerGrid before Children.Add — Children.Add triggers a synchronous Measure
            // pass that needs OwnerGrid set to resolve column widths.
            _panel.OwnerGrid = _grid;
            SyncScrollOffset();

            _panel.Children.Clear();
            foreach (DataGridColumn column in _grid.Columns)
            {
                var child = ResolveChildForColumn(column);
                _panel.Children.Add(child);
            }

            _panel.InvalidateMeasure();
            _panel.InvalidateArrange();

            // Re-invalidate at Loaded — by then the grid has done a full measure+arrange
            // cycle, so column ActualWidth is published and ResolveChildWidth has real values.
            // Then at ApplicationIdle as a safety net — covers the case where the DataGrid
            // settles on column ActualWidth values during a layout cycle that ran after Loaded
            // (e.g. star columns finalizing on a delayed pass, or initial item materialization
            // changing auto column widths). Without this second pass, the filter row can stay
            // arranged against an intermediate width and visibly misalign with the data cells.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_panel == null) return;
                _panel.InvalidateMeasure();
                _panel.InvalidateArrange();
            }), DispatcherPriority.Loaded);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_panel == null) return;
                _panel.InvalidateArrange();
            }), DispatcherPriority.ApplicationIdle);
        }

        private UIElement ResolveChildForColumn(DataGridColumn column)
        {
            return new ColumnFilterControl
            {
                CurrentColumn = column,
                SourceDataGrid = _grid,
            };
        }

        // LayoutUpdated-based safety net: WPF's standard column headers and data cells share
        // DataGridCellsPanel arrangement and stay in sync with each other, but the custom
        // FilterRowPanel runs in a separate layout subtree. Even with matching rounding,
        // its arrange can land one frame behind during a resize drag (the "jiggle") or pick
        // up stale widths on initial layout. We snapshot each header's (X, ActualWidth) at
        // arrange time and, on every LayoutUpdated, re-snapshot and compare — when anything
        // shifted we invalidate the panel so it re-mirrors on the next pass. The snapshot
        // gets updated before invalidating, so the re-arrange itself doesn't re-trigger the
        // detector (no infinite loop).
        private void EnsureLayoutUpdatedSubscribed()
        {
            if (_layoutUpdatedSubscribed) return;
            _layoutUpdatedSubscribed = true;
            LayoutUpdated += OnLayoutUpdatedResync;
        }

        private void OnLayoutUpdatedResync(object sender, EventArgs e)
        {
            if (_panel == null || _grid == null) return;

            var current = SnapshotHeaderPositions();
            if (current == null) return; // headers not materialized yet

            if (!HasHeaderGeometryChanged(_lastHeaderSnapshot, current))
                return;

            _lastHeaderSnapshot = current;
            _panel.InvalidateArrange();
        }

        private List<(DataGridColumn column, double x, double width)> SnapshotHeaderPositions()
        {
            if (_grid == null || _panel == null) return null;

            var presenter = _grid.Template?.FindName("PART_ColumnHeadersPresenter", _grid)
                as DataGridColumnHeadersPresenter;
            if (presenter == null) return null;

            var result = new List<(DataGridColumn, double, double)>(_grid.Columns.Count);
            CollectHeaderGeometry(presenter, _panel, result);
            return result.Count == 0 ? null : result;
        }

        private static void CollectHeaderGeometry(
            DependencyObject root,
            UIElement referenceForTransform,
            List<(DataGridColumn column, double x, double width)> result)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is DataGridColumnHeader header && header.Column != null && header.ActualWidth > 0)
                {
                    double x;
                    try
                    {
                        x = header.TranslatePoint(new Point(0, 0), referenceForTransform).X;
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }
                    result.Add((header.Column, x, header.ActualWidth));
                }
                CollectHeaderGeometry(child, referenceForTransform, result);
            }
        }

        private static bool HasHeaderGeometryChanged(
            List<(DataGridColumn column, double x, double width)> previous,
            List<(DataGridColumn column, double x, double width)> current)
        {
            if (previous == null) return true;
            if (previous.Count != current.Count) return true;

            for (int i = 0; i < current.Count; i++)
            {
                var p = previous[i];
                var c = current[i];
                if (!ReferenceEquals(p.column, c.column)) return true;
                if (Math.Abs(p.x - c.x) > 0.01) return true;
                if (Math.Abs(p.width - c.width) > 0.01) return true;
            }
            return false;
        }
    }
}
