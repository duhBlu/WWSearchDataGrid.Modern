using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// Horizontal per-column panel for the group chrome that lives in extent space inside the
    /// scrolling rows — the AlignByColumns header layer (<see cref="GroupSummaryCellsPresenter"/>)
    /// and the group footer cells (<see cref="GroupFooterCellsPresenter"/>). With no pinned
    /// columns it lays children out exactly like the horizontal StackPanel it replaces. With
    /// pinned columns it mirrors the data cells' band behavior: left-pinned cells anchor at the
    /// viewport's leading content edge (extent-space X = the horizontal scroll offset),
    /// right-pinned cells anchor at the trailing edge, and scrollable cells keep their natural
    /// extent positions clipped to the window between the bands. Children must expose their
    /// column (<see cref="GroupSummaryCell"/> / <see cref="GroupFooterCell"/>) and are added in
    /// display order by their presenters.
    /// </summary>
    public class FixedColumnAlignedPanel : Panel
    {
        private SearchDataGrid _ownerGrid;
        private ScrollViewer _scrollViewer;

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0, height = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                width += child.DesiredSize.Width;
                if (child.DesiredSize.Height > height) height = child.DesiredSize.Height;
            }
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var grid = ResolveOwnerGrid();
            EnsureScrollSubscription(grid);

            double offset = 0, viewportWidth = 0;
            bool banded = grid != null && grid.HasFixedColumns && _scrollViewer != null;
            if (banded)
            {
                offset = _scrollViewer.HorizontalOffset;
                viewportWidth = grid.GetCellsViewportWidth();
                banded = viewportWidth > 0;
            }

            if (!banded)
            {
                double cursor = 0, cursorSnapped = 0;
                foreach (UIElement child in InternalChildren)
                {
                    if (child == null) continue;
                    cursor += child.DesiredSize.Width;
                    double next = FixedColumnLayout.SnapToPixel(cursor);
                    child.ClearValue(Panel.ZIndexProperty);
                    child.SetCurrentValue(ClipProperty, null);
                    child.Arrange(new Rect(cursorSnapped, 0, next - cursorSnapped, finalSize.Height));
                    cursorSnapped = next;
                }
                return finalSize;
            }

            // Band widths from the children themselves (cell Width is bound to its column).
            double leftBandWidth = 0, rightBandWidth = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;
                switch (grid.GetFixedColumnPosition(GetColumn(child)))
                {
                    case FixedColumnPosition.Left: leftBandWidth += child.DesiredSize.Width; break;
                    case FixedColumnPosition.Right: rightBandWidth += child.DesiredSize.Width; break;
                }
            }

            // Separator strips consume one strip of window per non-empty band; scrollable
            // cells shift right past the left strip, mirroring the cells panel.
            double separator = grid.GetSeparatorWidth();
            double leftSeparator = leftBandWidth > 0 ? separator : 0;
            double rightSeparator = rightBandWidth > 0 ? separator : 0;

            double leftBandStart = FixedColumnLayout.SnapToPixel(offset);
            double rightBandStart = FixedColumnLayout.SnapToPixel(FixedColumnLayout.ComputeRightBandStart(
                offset, viewportWidth, leftBandWidth + leftSeparator + rightSeparator, rightBandWidth));
            double viewportRight = offset + viewportWidth;
            double windowStart = FixedColumnLayout.SnapToPixel(offset + leftBandWidth + leftSeparator);
            double windowEnd = rightBandStart - rightSeparator;

            double naturalCursor = 0, naturalSnapped = 0;
            double leftCursor = leftBandStart, leftSnapped = leftBandStart;
            double rightCursor = rightBandStart, rightSnapped = rightBandStart;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;
                double width = child.DesiredSize.Width;

                // Natural extent-space cursor advances for every child so scrollable cells
                // keep the same cumulative positions the data cells use.
                naturalCursor += width;
                double nextNatural = FixedColumnLayout.SnapToPixel(naturalCursor);

                switch (grid.GetFixedColumnPosition(GetColumn(child)))
                {
                    case FixedColumnPosition.Left:
                    {
                        leftCursor += width;
                        double next = FixedColumnLayout.SnapToPixel(leftCursor);
                        Panel.SetZIndex(child, 1);
                        child.SetCurrentValue(ClipProperty, null);
                        child.Arrange(new Rect(leftSnapped, 0, next - leftSnapped, finalSize.Height));
                        leftSnapped = next;
                        break;
                    }
                    case FixedColumnPosition.Right:
                    {
                        rightCursor += width;
                        double next = FixedColumnLayout.SnapToPixel(rightCursor);
                        double cellWidth = next - rightSnapped;
                        Panel.SetZIndex(child, 1);
                        child.SetCurrentValue(ClipProperty, FixedColumnLayout.ComputeClipAtBoundary(
                            rightSnapped, cellWidth, finalSize.Height, viewportRight));
                        child.Arrange(new Rect(rightSnapped, 0, cellWidth, finalSize.Height));
                        rightSnapped = next;
                        break;
                    }
                    default:
                    {
                        double cellWidth = nextNatural - naturalSnapped;
                        double x = naturalSnapped + leftSeparator;
                        child.ClearValue(Panel.ZIndexProperty);
                        child.SetCurrentValue(ClipProperty, FixedColumnLayout.ComputeClipToWindow(
                            x, cellWidth, finalSize.Height, windowStart, windowEnd));
                        child.Arrange(new Rect(x, 0, cellWidth, finalSize.Height));
                        break;
                    }
                }

                naturalSnapped = nextNatural;
            }

            return finalSize;
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            if (visualRemoved is UIElement removed)
            {
                removed.ClearValue(Panel.ZIndexProperty);
                removed.ClearValue(ClipProperty);
            }
        }

        private static DataGridColumn GetColumn(UIElement child) => child switch
        {
            GroupSummaryCell summary => summary.Column,
            GroupFooterCell footer => footer.Column,
            _ => null,
        };

        private SearchDataGrid ResolveOwnerGrid()
        {
            if (_ownerGrid != null) return _ownerGrid;
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
        /// The panel sits in physically scrolled content, so a horizontal scroll moves it
        /// without re-running its arrange — the band anchors need an explicit invalidation,
        /// same source the cells panel uses.
        /// </summary>
        private void EnsureScrollSubscription(SearchDataGrid grid)
        {
            if (_scrollViewer != null || grid == null)
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
    }
}
