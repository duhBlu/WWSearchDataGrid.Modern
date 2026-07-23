using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Hosts the banded column-header rows above the column headers of a <see cref="SearchDataGrid"/>,
    /// parenting one <see cref="BandHeaderCell"/> per band in the grid's <see cref="SearchDataGrid.BandLayout"/>.
    /// </summary>
    /// <remarks>
    /// The column tracking, scroll sync, and header-geometry resync here deliberately mirror
    /// <see cref="ColumnAlignedRowPresenter"/> (the filter-row / summary-row base) rather than
    /// deriving from it: that base builds one child <em>per column</em>, whereas this builds one
    /// <see cref="BandHeaderCell"/> <em>per band</em> spanning several columns. Kept separate to
    /// avoid destabilizing the filter-row layout; a shared base is a future dedup once both settle.
    /// </remarks>
    public class BandHeadersPresenter : Control
    {
        static BandHeadersPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(BandHeadersPresenter),
                new FrameworkPropertyMetadata(typeof(BandHeadersPresenter)));
        }

        public const string PartBandHeaderPanelName = "PART_BandHeaderPanel";

        /// <summary>Height of a single band caption row; flowed to the panel via the theme template.</summary>
        public static readonly DependencyProperty BandRowHeightProperty =
            DependencyProperty.Register(nameof(BandRowHeight), typeof(double), typeof(BandHeadersPresenter),
                new FrameworkPropertyMetadata(26.0));

        public double BandRowHeight
        {
            get => (double)GetValue(BandRowHeightProperty);
            set => SetValue(BandRowHeightProperty, value);
        }

        private SearchDataGrid _grid;
        private BandHeaderPanel _panel;
        private ScrollViewer _scrollViewer;
        private bool _refreshScheduled;
        private bool _rebuildScheduled;
        private bool _layoutUpdatedSubscribed;

        // Snapshot of (column, X, width) taken on the most recent arrange; LayoutUpdated compares
        // current header geometry against it and re-arranges when the headers shifted.
        private List<(DataGridColumn column, double x, double width)> _lastHeaderSnapshot;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _panel = GetTemplateChild(PartBandHeaderPanelName) as BandHeaderPanel;
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

            // Column set changed (e.g. generation adds them) — rebuild so member columns re-resolve.
            ScheduleRebuild();
        }

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

        private void ScheduleRebuild()
        {
            if (_panel == null || _rebuildScheduled) return;
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

            _panel.OwnerGrid = _grid;
            SyncScrollOffset();

            _panel.Children.Clear();
            var layout = _grid.BandLayout;
            if (layout != null)
                AddCellsRecursive(layout);

            // Presenter visibility is driven by the grid template's binding to MaxBandDepth —
            // don't set it here, a local value would clobber that binding.

            _panel.InvalidateMeasure();
            _panel.InvalidateArrange();

            // Re-invalidate once the grid has published column ActualWidth (Loaded) and again at
            // idle after any delayed width settle — same safety net the filter row uses.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _panel?.InvalidateMeasure();
                _panel?.InvalidateArrange();
            }), DispatcherPriority.Loaded);
            Dispatcher.BeginInvoke(new Action(() => _panel?.InvalidateArrange()), DispatcherPriority.ApplicationIdle);
        }

        private void AddCellsRecursive(IReadOnlyList<BandLayoutNode> nodes)
        {
            foreach (var node in nodes)
            {
                _panel.Children.Add(new BandHeaderCell
                {
                    Header = node.Header,
                    HeaderTemplate = node.HeaderTemplate,
                    BandLevel = node.Level,
                    MemberColumns = ResolveMemberColumns(node),
                    HasChildBands = node.Children != null && node.Children.Count > 0,
                });
                if (node.Children != null && node.Children.Count > 0)
                    AddCellsRecursive(node.Children);
            }
        }

        private static IReadOnlyList<DataGridColumn> ResolveMemberColumns(BandLayoutNode node)
        {
            var list = new List<DataGridColumn>(node.MemberColumns.Count);
            foreach (var descriptor in node.MemberColumns)
            {
                if (descriptor?.InternalColumn != null)
                    list.Add(descriptor.InternalColumn);
            }
            return list;
        }

        // LayoutUpdated resync: the band panel runs in its own layout subtree and can land a frame
        // behind the headers during a resize drag or pick up stale widths on initial layout. Snapshot
        // each header's (X, width) at arrange time; on every LayoutUpdated, re-snapshot and compare —
        // re-arrange the panel when anything shifted. Snapshot is updated before invalidating so the
        // re-arrange doesn't re-trigger the detector.
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
            if (current == null) return;

            if (!HasHeaderGeometryChanged(_lastHeaderSnapshot, current))
                return;

            _lastHeaderSnapshot = current;
            _panel.InvalidateArrange();
        }

        private List<(DataGridColumn column, double x, double width)> SnapshotHeaderPositions()
        {
            if (_grid == null || _panel == null) return null;

            var scrollViewer = _grid.Template?.FindName("DG_ScrollViewer", _grid) as ScrollViewer;
            var presenter = scrollViewer?.Template?.FindName("PART_ColumnHeadersPresenter", scrollViewer)
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
