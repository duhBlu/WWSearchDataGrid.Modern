using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
    public class AutoFilterRowPresenter : Control
    {
        static AutoFilterRowPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AutoFilterRowPresenter),
                new FrameworkPropertyMetadata(typeof(AutoFilterRowPresenter)));
        }

        public const string PartFilterRowPanelName = "PART_FilterRowPanel";

        private SearchDataGrid _grid;
        private FilterRowPanel _panel;
        private ScrollViewer _scrollViewer;
        private bool _refreshScheduled;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _panel = GetTemplateChild(PartFilterRowPanelName) as FilterRowPanel;
            TryAttachToGrid();
            RebuildChildren();
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

            // Re-invalidate at Loaded priority — by then the grid has done a full measure+arrange
            // cycle, so column ActualWidth is published and ResolveChildWidth has real values.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_panel == null) return;
                _panel.InvalidateMeasure();
                _panel.InvalidateArrange();
            }), DispatcherPriority.Loaded);
        }

        private UIElement ResolveChildForColumn(DataGridColumn column)
        {
            return new ColumnFilterControl
            {
                CurrentColumn = column,
                SourceDataGrid = _grid,
            };
        }
    }
}
