using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Cells panel used as the items panel for both the data cells (row template) and the
    /// column headers (headers-presenter style), keeping the two pixel-aligned. Defers
    /// entirely to the native <see cref="DataGridCellsPanel"/> layout — which owns left-frozen
    /// columns via <see cref="DataGrid.FrozenColumnCount"/> and the internal scroll
    /// bookkeeping (<c>CellsPanelActualWidth</c>, <c>NonFrozenColumnsViewportHorizontalOffset</c>,
    /// the frozen-boundary clip) — then overlays right-pinned columns as a band anchored to
    /// the viewport's right edge: each right-pinned child is re-arranged at the anchor,
    /// raised above the scrollable cells, and scrollable cells sliding under the band are
    /// clipped at its leading edge. With no right-pinned columns the panel is byte-for-byte
    /// native.
    /// Coordinate frame: in panel space the viewport's leading content edge sits at
    /// <see cref="ScrollViewer.HorizontalOffset"/>, the same frame the native panel anchors
    /// frozen cells in.
    /// </summary>
    public class FixedColumnsCellsPanel : DataGridCellsPanel
    {
        private const int RightBandZIndex = 1;

        private SearchDataGrid _ownerGrid;
        private ScrollViewer _scrollViewer;
        private bool _overlayActive;
        private bool? _isHeadersPanel;

        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);

            // The separator strips consume layout space between each band and the
            // scrollable cells — widen the desired extent so the first/last scrollable
            // pixels can still scroll out from under the strips.
            var grid = ResolveOwnerGrid();
            if (grid != null && grid.HasFixedColumns)
            {
                double separator = grid.GetSeparatorWidth();
                if (separator > 0)
                {
                    double extra = (grid.LeftFixedColumnsWidth > 0 ? separator : 0)
                                 + (grid.RightFixedColumnsWidth > 0 ? separator : 0);
                    if (extra > 0)
                        size = new Size(size.Width + extra, size.Height);
                }
            }
            return size;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var size = base.ArrangeOverride(arrangeSize);

            var grid = ResolveOwnerGrid();
            if (grid == null)
                return size;

            EnsureScrollSubscription(grid);

            if (grid.HasFixedColumns && _scrollViewer != null)
                ArrangeBandOverlay(grid, arrangeSize);
            else
                ResetOverlay();

            // The headers panel is the one instance per grid that arranges on every column
            // resize — it keeps the band-width DPs (separator overlays, scrollbar spacer) fresh.
            _isHeadersPanel ??= (VisualTreeHelper.GetParent(this) as FrameworkElement)?.TemplatedParent
                is DataGridColumnHeadersPresenter;
            if (_isHeadersPanel == true)
                grid.UpdateFixedBandWidths();

            return size;
        }

        private void ArrangeBandOverlay(SearchDataGrid grid, Size arrangeSize)
        {
            double viewportWidth = grid.GetCellsViewportWidth();
            if (viewportWidth <= 0)
            {
                ResetOverlay();
                return;
            }

            double horizontalOffset = _scrollViewer.HorizontalOffset;
            double separator = grid.GetSeparatorWidth();

            List<(UIElement Child, DataGridColumn Column)> rightChildren = null;
            double leftBandWidth = 0;
            double rightBandWidth = 0;

            foreach (UIElement child in InternalChildren)
            {
                var column = GetColumn(child);
                if (column == null || column.Visibility != Visibility.Visible)
                    continue;

                switch (grid.GetFixedColumnPosition(column))
                {
                    case FixedColumnPosition.Left:
                        leftBandWidth += column.ActualWidth;
                        break;
                    case FixedColumnPosition.Right:
                        rightBandWidth += column.ActualWidth;
                        (rightChildren ??= new List<(UIElement, DataGridColumn)>()).Add((child, column));
                        break;
                }
            }

            if (leftBandWidth <= 0 && rightChildren == null)
            {
                ResetOverlay();
                return;
            }

            // Separator strips consume layout space: the scrollable window shrinks by one
            // strip per non-empty band, and the measure pass widened the extent by the same
            // amount so every scrollable pixel can still be reached.
            double leftSeparator = leftBandWidth > 0 ? separator : 0;
            double rightSeparator = rightBandWidth > 0 ? separator : 0;

            double bandStart = FixedColumnLayout.ComputeRightBandStart(
                horizontalOffset, viewportWidth, leftBandWidth + leftSeparator + rightSeparator, rightBandWidth);
            double viewportRight = horizontalOffset + viewportWidth;
            double windowStart = FixedColumnLayout.SnapToPixel(horizontalOffset + leftBandWidth + leftSeparator);
            double windowEnd = FixedColumnLayout.SnapToPixel(bandStart - rightSeparator);

            if (rightChildren != null)
            {
                rightChildren.Sort((a, b) => a.Column.DisplayIndex.CompareTo(b.Column.DisplayIndex));

                // Band cells, left-to-right with cumulative pixel snapping so adjacent seams
                // land on whole pixels (FilterRowPanel convention).
                double cursor = bandStart;
                double cursorSnapped = FixedColumnLayout.SnapToPixel(bandStart);
                foreach (var (child, column) in rightChildren)
                {
                    cursor += column.ActualWidth;
                    double nextSnapped = FixedColumnLayout.SnapToPixel(cursor);
                    double width = nextSnapped - cursorSnapped;
                    child.Arrange(new Rect(cursorSnapped, 0, width, arrangeSize.Height));
                    Panel.SetZIndex(child, RightBandZIndex);
                    SetChildClip(child, FixedColumnLayout.ComputeClipAtBoundary(
                        cursorSnapped, width, arrangeSize.Height, viewportRight));
                    cursorSnapped = nextSnapped;
                }
            }

            // Left-frozen cells keep their native arrangement untouched. Scrollable cells are
            // shifted right by the left strip (opening the gap the strip occupies) and clipped
            // to the window between the strips. The native frozen-boundary chop still applies
            // in child coordinates, so the shifted boundary cell's visible part starts exactly
            // at the window start.
            foreach (UIElement child in InternalChildren)
            {
                var column = GetColumn(child);
                if (column == null || child is not FrameworkElement element)
                    continue;

                var position = grid.GetFixedColumnPosition(column);
                if (position == FixedColumnPosition.Right)
                    continue;

                if (position == FixedColumnPosition.Left)
                {
                    child.ClearValue(Panel.ZIndexProperty);
                    SetChildClip(child, null);
                    continue;
                }

                child.ClearValue(Panel.ZIndexProperty);
                var slot = LayoutInformation.GetLayoutSlot(element);
                double x = slot.X + leftSeparator;
                if (leftSeparator > 0)
                    child.Arrange(new Rect(x, slot.Y, slot.Width, slot.Height));
                SetChildClip(child, FixedColumnLayout.ComputeClipToWindow(
                    x, slot.Width, arrangeSize.Height, windowStart, windowEnd));
            }

            _overlayActive = true;
        }

        /// <summary>
        /// Clears the ZIndex / clip the overlay left on children after the last right-pinned
        /// column is removed, restoring the pure-native arrangement.
        /// </summary>
        private void ResetOverlay()
        {
            if (!_overlayActive)
                return;
            _overlayActive = false;

            foreach (UIElement child in InternalChildren)
            {
                child.ClearValue(Panel.ZIndexProperty);
                child.ClearValue(ClipProperty);
            }
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            // Containers leaving the panel go back to the generator's recycle pool and can be
            // reused for a different column — strip overlay state so a stale clip or ZIndex
            // never rides along.
            if (visualRemoved is UIElement removed)
            {
                removed.ClearValue(Panel.ZIndexProperty);
                removed.ClearValue(ClipProperty);
            }
        }

        private static DataGridColumn GetColumn(UIElement child) => child switch
        {
            DataGridCell cell => cell.Column,
            DataGridColumnHeader header => header.Column,
            _ => null,
        };

        private static void SetChildClip(UIElement child, Geometry clip)
            => child.SetCurrentValue(ClipProperty, clip);

        private SearchDataGrid ResolveOwnerGrid()
        {
            if (_ownerGrid != null)
                return _ownerGrid;

            DependencyObject d = this;
            while (d != null)
            {
                if (d is SearchDataGrid grid)
                {
                    _ownerGrid = grid;
                    break;
                }
                d = VisualTreeHelper.GetParent(d);
            }
            return _ownerGrid;
        }

        /// <summary>
        /// The native plumbing only re-arranges cells panels on horizontal scroll when frozen
        /// (left-pinned) columns exist, so the right band needs its own scroll signal — same
        /// source <see cref="ColumnAlignedRowPresenter"/> uses. Invalidation is direct (no
        /// dispatcher hop): ScrollChanged fires inside the layout pass, so the re-arrange
        /// lands in the same frame and the band never visibly drifts.
        /// </summary>
        private void EnsureScrollSubscription(SearchDataGrid grid)
        {
            if (_scrollViewer != null)
                return;

            _scrollViewer = grid.Template?.FindName("DG_ScrollViewer", grid) as ScrollViewer;
            if (_scrollViewer == null)
                return;

            _scrollViewer.ScrollChanged += OnGridScrollChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= OnGridScrollChanged;
                _scrollViewer = null;
            }
            _ownerGrid = null;
        }

        private void OnGridScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange == 0 && e.ViewportWidthChange == 0 && e.ExtentWidthChange == 0)
                return;
            if (_ownerGrid == null || !_ownerGrid.HasFixedColumns)
                return;
            InvalidateArrange();
        }

        /// <summary>
        /// Re-runs layout on every realized band-aware panel under the grid — the headers /
        /// row cells panels and the group-chrome <see cref="FixedColumnAlignedPanel"/>s.
        /// Needed when a pin change doesn't move any
        /// <see cref="DataGridColumn.DisplayIndex"/> (e.g. unpinning a trailing right column)
        /// and when the separator width changes (it contributes to the measured extent) —
        /// neither produces a layout invalidation of its own. Descent stops at each panel,
        /// so per-cell subtrees are never walked.
        /// </summary>
        internal static void InvalidateBandLayout(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is FixedColumnsCellsPanel cellsPanel)
                {
                    cellsPanel.InvalidateMeasure();
                    cellsPanel.InvalidateArrange();
                }
                else if (child is FixedColumnAlignedPanel alignedPanel)
                {
                    alignedPanel.InvalidateMeasure();
                    alignedPanel.InvalidateArrange();
                }
                else
                {
                    InvalidateBandLayout(child);
                }
            }
        }
    }
}
