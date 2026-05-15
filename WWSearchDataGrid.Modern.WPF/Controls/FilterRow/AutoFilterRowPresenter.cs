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
    /// <see cref="SearchDataGrid"/>, parenting one child per data column. The actual filter
    /// editor (Phase 3's <c>ColumnFilterControl</c>) is resolved per column via
    /// <see cref="SearchDataGrid.FindColumnFilterControl(DataGridColumn)"/>; when no host is
    /// registered (Phase 2) a placeholder <see cref="Border"/> is used so the row is testable
    /// end-to-end before Phase 3 lands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscriptions kept live for the presenter's lifetime:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="SearchDataGrid.Columns"/> <c>CollectionChanged</c> — add / remove
    ///   children when columns are added, removed, or reset.</item>
    ///   <item>Per-column <c>ActualWidth</c> / <c>DisplayIndex</c> / <c>Visibility</c> change
    ///   notifications — invalidate measure / arrange on the panel.</item>
    ///   <item>The grid's inner <c>DG_ScrollViewer</c> <c>ScrollChanged</c> — feed the panel's
    ///   <see cref="FilterRowPanel.HorizontalOffset"/> so non-frozen children translate with
    ///   the cells.</item>
    /// </list>
    /// <para>
    /// Per-frame work during a scroll or resize is coalesced to
    /// <see cref="DispatcherPriority.Render"/> so a continuous resize-drag doesn't trigger
    /// N-editor remeasure on every mouse-move delta — only the last requested update in any
    /// given frame actually fires.
    /// </para>
    /// </remarks>
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
            // The grid's inner ScrollViewer is named DG_ScrollViewer in the SearchDataGrid
            // template. It may not be applied yet when the presenter is first parented;
            // retry on the next Loaded tick if so.
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

        // DataGridColumn.ActualWidth / DisplayIndex / Visibility are exposed via standard
        // DependencyPropertyDescriptor change notifications. Wiring through DPDs is the same
        // pattern WPF's own DataGridColumnHeadersPresenter uses; it's noticeably more reliable
        // than INotifyPropertyChanged here because ActualWidth in particular isn't always a
        // DP change on the column DO under all internal width-resolve paths.
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
            // Resize drags fire ActualWidth on every mouse-move delta — coalesce so we
            // remeasure once per render frame, not N times per drag pixel.
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
            // Coalesce: GenerateColumnsFromDescriptors fires Columns.CollectionChanged once
            // per Add — for a 7-column grid that's 7 events. Without coalescing we'd rebuild
            // the panel 7 times back-to-back, which both wastes work and creates a stream of
            // ColumnFilterControl instances that have to register / unregister synchronously
            // and tangle the column-filter-control registry. One rebuild per dispatcher tick
            // is sufficient — CollectionChanged events that arrive in the same tick all
            // resolve to the final column set.
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

            // OwnerGrid first — Children.Add can trigger a synchronous Measure/Arrange pass
            // (WPF doesn't wait for layout idle), and the panel's MeasureOverride /
            // ArrangeOverride branch on OwnerGrid to decide column widths. Without this
            // ordering the first layout pass runs with OwnerGrid still null, the children
            // arrange at child-DesiredSize widths instead of column widths, and the next
            // dependency-property-descriptor invalidate cycle catches up only after a render
            // frame — visible as a brief flash of mis-sized editors on first show.
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

            // Re-invalidate after the parent grid has finished its first layout pass.
            // Column ActualWidth is published during DataGridColumnHeadersPresenter measure;
            // by Loaded priority the grid has had at least one full measure+arrange cycle so
            // ResolveChildWidth no longer falls back to child.DesiredSize.Width.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_panel == null) return;
                _panel.InvalidateMeasure();
                _panel.InvalidateArrange();
            }), DispatcherPriority.Loaded);
        }

        private UIElement ResolveChildForColumn(DataGridColumn column)
        {
            // Always create a fresh ColumnFilterControl per cell. Earlier revisions tried to
            // detect a "registered elsewhere" case here (Phase 4 Header-mode chrome would
            // host the editor in the column header instead of the filter row), but the
            // detection misfired during multi-pass rebuilds: Children.Clear() synchronously
            // detaches the visual parent while the previous control's Unloaded-driven
            // unregister runs on a later dispatcher tick, leaving a "registered with no
            // parent" intermediate state that the check mistook for "registered elsewhere"
            // and substituted a transparent placeholder for. When Phase 4 needs the
            // registry-driven swap-out, re-introduce the check alongside synchronous
            // pre-clear unregistration.
            return new ColumnFilterControl
            {
                CurrentColumn = column,
                SourceDataGrid = _grid,
            };
        }
    }
}
