using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// Layout panel hosting one filter cell per data column. Primary strategy: mirror each
    /// column header's actual rendered X/width via <see cref="UIElement.TransformToVisual"/>,
    /// guaranteeing alignment with the column headers (which are the source of truth — the
    /// data cells use the same <see cref="DataGridCellsPanel"/> arrangement the headers do).
    /// Falls back to a snap-to-pixel cumulative-width arrangement when headers haven't
    /// materialized yet (initial layout pass). Non-virtualizing — every column gets a
    /// materialized child.
    /// </summary>
    public class FilterRowPanel : Panel
    {
        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(nameof(OwnerGrid), typeof(SearchDataGrid), typeof(FilterRowPanel),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(FilterRowPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var grid = OwnerGrid;
            double maxHeight = 0;

            if (grid == null || grid.Columns.Count == 0)
            {
                // No grid attached yet — measure children against the full available width
                // so the first frame renders before the presenter wires OwnerGrid.
                double totalDesired = 0;
                foreach (UIElement child in InternalChildren)
                {
                    if (child == null) continue;
                    child.Measure(availableSize);
                    if (child.DesiredSize.Height > maxHeight) maxHeight = child.DesiredSize.Height;
                    totalDesired += child.DesiredSize.Width;
                }
                return new Size(totalDesired, maxHeight);
            }

            // Fallback for columns whose ActualWidth isn't published yet — split the remaining
            // horizontal space evenly so cells don't collapse to zero on the first layout cycle.
            double resolvedSum = 0;
            int unresolvedCount = 0;
            int visibleCount = 0;
            foreach (var column in grid.Columns)
            {
                if (column.Visibility != Visibility.Visible) continue;
                visibleCount++;
                double w = ResolveColumnWidth(column);
                if (w > 0) resolvedSum += w;
                else unresolvedCount++;
            }

            double fallbackWidth = 0;
            if (unresolvedCount > 0)
            {
                double panelWidth = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
                double remaining = Math.Max(0, panelWidth - resolvedSum);
                fallbackWidth = remaining > 0
                    ? remaining / unresolvedCount
                    : DefaultColumnWidth; // last-ditch fallback for infinity-wide measure passes
            }

            double totalWidth = 0;
            int childIndex = 0;
            foreach (var column in grid.Columns)
            {
                if (childIndex >= InternalChildren.Count) break;
                var child = InternalChildren[childIndex++];
                if (child == null) continue;

                if (column.Visibility != Visibility.Visible)
                {
                    child.Measure(new Size(0, 0));
                    continue;
                }

                double width = ResolveColumnWidth(column);
                if (width <= 0) width = fallbackWidth;

                child.Measure(new Size(width, double.PositiveInfinity));
                if (child.DesiredSize.Height > maxHeight) maxHeight = child.DesiredSize.Height;
                totalWidth += width;
            }

            return new Size(totalWidth, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (InternalChildren.Count == 0)
                return finalSize;

            var grid = OwnerGrid;
            if (grid == null)
            {
                // OwnerGrid not wired yet — arrange at DesiredSize.Width so the row renders;
                // the presenter re-triggers arrange once OwnerGrid lands.
                double cursor = 0;
                foreach (UIElement child in InternalChildren)
                {
                    if (child == null) continue;
                    double w = child.DesiredSize.Width > 0 ? child.DesiredSize.Width : 0;
                    child.Arrange(new Rect(cursor, 0, w, finalSize.Height));
                    child.ClearValue(VisibilityProperty);
                    (child as FrameworkElement)?.SetCurrentValue(ClipProperty, null);
                    cursor += w;
                }
                return finalSize;
            }

            var pairs = BuildOrderedPairs(grid);
            if (pairs.Count == 0)
                return finalSize;

            // Try header-mirroring first. Falls back to cumulative-width arrangement when
            // headers aren't materialized yet (first paint before the headers presenter
            // arranges, or transient cases where the lookup misses an entry).
            if (TryArrangeFromHeaderPositions(pairs, grid, finalSize))
                return finalSize;

            ArrangeFromCumulativeWidths(pairs, grid, finalSize);
            return finalSize;
        }

        /// <summary>
        /// Arranges each filter cell at the X/width of the corresponding column header.
        /// Returns <c>true</c> when every visible column resolved to a usable header
        /// (positive ActualWidth and a transform back to this panel). When any column
        /// can't be resolved, returns <c>false</c> and the caller falls back to the
        /// cumulative-width arrangement so we don't half-mirror and leave some cells stranded.
        /// </summary>
        private bool TryArrangeFromHeaderPositions(
            List<(DataGridColumn column, UIElement child)> pairs,
            SearchDataGrid grid,
            Size finalSize)
        {
            var headerLookup = BuildHeaderLookup(grid);
            if (headerLookup == null || headerLookup.Count == 0)
                return false;

            // First pass: collect the resolved bounds. Bail out without mutating any
            // child arrangement if a single visible column doesn't resolve cleanly.
            var arrangements = new List<(DataGridColumn column, UIElement child, Rect bounds, bool hidden)>(pairs.Count);
            foreach (var (column, child) in pairs)
            {
                if (!IsColumnVisible(column))
                {
                    arrangements.Add((column, child, default, true));
                    continue;
                }

                if (!headerLookup.TryGetValue(column, out var header) || header.ActualWidth <= 0)
                    return false;

                Point origin;
                try
                {
                    origin = header.TranslatePoint(new Point(0, 0), this);
                }
                catch (InvalidOperationException)
                {
                    // No common ancestor / header detached — fall back.
                    return false;
                }

                arrangements.Add((column, child, new Rect(origin.X, 0, header.ActualWidth, finalSize.Height), false));
            }

            // Band boundaries from the mirrored geometry itself: scrollable cells may only
            // paint between the left band's trailing edge and the right band's leading edge.
            // The headers carry their own band-boundary clips (native frozen on the left, the
            // FixedColumnsCellsPanel overlay on the right), but the mirror copies X/width
            // only, so the window has to be re-applied here.
            double windowStart = double.NegativeInfinity;
            double windowEnd = double.PositiveInfinity;
            foreach (var (column, _, bounds, hidden) in arrangements)
            {
                if (hidden) continue;
                switch (grid.GetFixedColumnPosition(column))
                {
                    case FixedColumnPosition.Left:
                        windowStart = Math.Max(windowStart, bounds.Right);
                        break;
                    case FixedColumnPosition.Right:
                        windowEnd = Math.Min(windowEnd, bounds.X);
                        break;
                }
            }

            // The separator strips occupy the space just past each band edge — pull the
            // window in by one strip per band so scrollable cells clip where the cells do.
            double separator = grid.GetSeparatorWidth();
            if (!double.IsNegativeInfinity(windowStart)) windowStart += separator;
            if (!double.IsPositiveInfinity(windowEnd)) windowEnd -= separator;

            foreach (var (column, child, bounds, hidden) in arrangements)
            {
                if (hidden)
                {
                    HideChild(child);
                    continue;
                }

                if (grid.GetFixedColumnPosition(column) == FixedColumnPosition.None)
                {
                    // Viewport-edge clipping is handled by the outer ClipToBounds on the
                    // presenter template; only the band window needs per-cell clips.
                    child.ClearValue(Panel.ZIndexProperty);
                    ArrangeChild(child, bounds, FixedColumnLayout.ComputeClipToWindow(
                        bounds.X, bounds.Width, finalSize.Height, windowStart, windowEnd));
                }
                else
                {
                    Panel.SetZIndex(child, 1);
                    ArrangeChild(child, bounds, clip: null);
                }
            }
            return true;
        }

        /// <summary>
        /// Fallback arrangement used until the column headers are materialized. Three regions
        /// in panel (viewport) space: the left band pinned at 0, the scrollable middle at
        /// cumulative-minus-offset clipped to the window between the bands, and the right
        /// band anchored at the panel's right edge. Each cell's X / right is snapped via
        /// banker's rounding (matching WPF's UseLayoutRounding) so the panel lines up on
        /// integer pixels when the headers aren't available to mirror.
        /// </summary>
        private void ArrangeFromCumulativeWidths(
            List<(DataGridColumn column, UIElement child)> pairs,
            SearchDataGrid grid,
            Size finalSize)
        {
            int frozenCount = Math.Min(grid.FrozenColumnCount, pairs.Count);

            double resolvedSum = 0;
            int unresolvedCount = 0;
            foreach (var column in grid.Columns)
            {
                if (column.Visibility != Visibility.Visible) continue;
                double w = ResolveColumnWidth(column);
                if (w > 0) resolvedSum += w;
                else unresolvedCount++;
            }
            double fallbackWidth = unresolvedCount > 0
                ? Math.Max(0, finalSize.Width - resolvedSum) / unresolvedCount
                : 0;
            if (fallbackWidth <= 0 && unresolvedCount > 0)
                fallbackWidth = DefaultColumnWidth;

            double frozenCursor = 0;
            double frozenCursorSnapped = 0;
            for (int i = 0; i < frozenCount; i++)
            {
                var (column, child) = pairs[i];
                if (!IsColumnVisible(column))
                {
                    HideChild(child);
                    continue;
                }

                double width = ResolveArrangedWidth(column, fallbackWidth);
                frozenCursor += width;
                double nextSnapped = SnapToPixel(frozenCursor);
                ArrangeChild(child, new Rect(frozenCursorSnapped, 0, nextSnapped - frozenCursorSnapped, finalSize.Height), clip: null);
                frozenCursorSnapped = nextSnapped;
            }

            // Right band width — right-pinned pairs sit at the end of the display order.
            double rightBandWidth = 0;
            for (int i = frozenCount; i < pairs.Count; i++)
            {
                var (column, _) = pairs[i];
                if (!IsColumnVisible(column)) continue;
                if (grid.GetFixedColumnPosition(column) == FixedColumnPosition.Right)
                    rightBandWidth += ResolveArrangedWidth(column, fallbackWidth);
            }
            // Separator strips consume one strip of window per non-empty band; the scrollable
            // run shifts right past the left strip, mirroring the cells panel.
            double separator = grid.GetSeparatorWidth();
            double leftSeparator = frozenCursorSnapped > 0 ? separator : 0;
            double rightSeparator = rightBandWidth > 0 ? separator : 0;

            double rightBandStart = SnapToPixel(FixedColumnLayout.ComputeRightBandStart(
                0, finalSize.Width, frozenCursorSnapped + leftSeparator + rightSeparator, rightBandWidth));

            double scroll = HorizontalOffset;
            double nonFrozenCursor = frozenCursor;
            double nonFrozenCursorSnapped = frozenCursorSnapped;
            double rightCursor = rightBandStart;
            double rightCursorSnapped = rightBandStart;

            for (int i = frozenCount; i < pairs.Count; i++)
            {
                var (column, child) = pairs[i];
                if (!IsColumnVisible(column))
                {
                    HideChild(child);
                    continue;
                }

                double width = ResolveArrangedWidth(column, fallbackWidth);

                if (rightBandWidth > 0 && grid.GetFixedColumnPosition(column) == FixedColumnPosition.Right)
                {
                    rightCursor += width;
                    double nextRight = SnapToPixel(rightCursor);
                    double bandCellWidth = nextRight - rightCursorSnapped;
                    Panel.SetZIndex(child, 1);
                    ArrangeChild(child, new Rect(rightCursorSnapped, 0, bandCellWidth, finalSize.Height),
                        FixedColumnLayout.ComputeClipAtBoundary(rightCursorSnapped, bandCellWidth, finalSize.Height, finalSize.Width));
                    rightCursorSnapped = nextRight;
                    continue;
                }

                nonFrozenCursor += width;
                double nextSnapped = SnapToPixel(nonFrozenCursor);
                double snappedWidth = nextSnapped - nonFrozenCursorSnapped;
                double x = nonFrozenCursorSnapped - scroll + leftSeparator;

                child.ClearValue(Panel.ZIndexProperty);
                ArrangeChild(child, new Rect(x, 0, snappedWidth, finalSize.Height),
                    FixedColumnLayout.ComputeClipToWindow(x, snappedWidth, finalSize.Height,
                        frozenCursorSnapped + leftSeparator,
                        rightBandWidth > 0 ? rightBandStart - rightSeparator : double.PositiveInfinity));
                nonFrozenCursorSnapped = nextSnapped;
            }
        }

        /// <summary>
        /// Walks the grid's headers presenter and returns a column→header map. Returns
        /// <c>null</c> when the presenter isn't applied yet (transient on early layout
        /// passes); the caller falls back to width-based arrangement.
        /// </summary>
        private static Dictionary<DataGridColumn, DataGridColumnHeader> BuildHeaderLookup(SearchDataGrid grid)
        {
            // The presenter is named inside DG_ScrollViewer's own template — the grid
            // template's namescope only knows DG_ScrollViewer itself.
            var scrollViewer = grid.Template?.FindName("DG_ScrollViewer", grid) as ScrollViewer;
            var presenter = scrollViewer?.Template?.FindName("PART_ColumnHeadersPresenter", scrollViewer)
                as DataGridColumnHeadersPresenter;
            if (presenter == null) return null;

            var result = new Dictionary<DataGridColumn, DataGridColumnHeader>(grid.Columns.Count);
            CollectHeaders(presenter, result);
            return result;
        }

        private static void CollectHeaders(DependencyObject root, Dictionary<DataGridColumn, DataGridColumnHeader> result)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is DataGridColumnHeader header && header.Column != null)
                    result[header.Column] = header;
                CollectHeaders(child, result);
            }
        }

        // Mirror WPF UseLayoutRounding's rounding mode (banker's rounding / ToEven —
        // confirmed by symptom: AwayFromZero produces more misalignments than ToEven).
        // Matches the per-pixel snap the standard DataGridCellsPanel applies via
        // UseLayoutRounding="True" on the grid root.
        private static double SnapToPixel(double value)
            => Math.Round(value, MidpointRounding.ToEven);

        private static void ArrangeChild(UIElement child, Rect bounds, Geometry clip)
        {
            child.Arrange(bounds);
            child.ClearValue(VisibilityProperty);
            (child as FrameworkElement)?.SetCurrentValue(ClipProperty, clip);
        }

        private static void HideChild(UIElement child)
        {
            child.Arrange(new Rect(0, 0, 0, 0));
            child.SetCurrentValue(VisibilityProperty, Visibility.Hidden);
        }

        private List<(DataGridColumn column, UIElement child)> BuildOrderedPairs(SearchDataGrid grid)
        {
            var result = new List<(DataGridColumn column, UIElement child)>(InternalChildren.Count);
            var columns = grid.Columns;
            int childIndex = 0;

            for (int i = 0; i < columns.Count && childIndex < InternalChildren.Count; i++)
            {
                var col = columns[i];
                var child = InternalChildren[childIndex++];
                if (child == null) continue;
                result.Add((col, child));
            }

            result.Sort((a, b) => a.column.DisplayIndex.CompareTo(b.column.DisplayIndex));
            return result;
        }

        // Returns 0 before the grid's first measure resolves Auto/Star widths — the caller
        // substitutes a fallback (even split of remaining space).
        private static double ResolveColumnWidth(DataGridColumn column)
        {
            if (column.ActualWidth > 0) return column.ActualWidth;
            if (column.Width.IsAbsolute) return column.Width.Value;
            return 0;
        }

        private static double ResolveArrangedWidth(DataGridColumn column, double fallbackWidth)
        {
            double width = ResolveColumnWidth(column);
            return width > 0 ? width : fallbackWidth;
        }

        private static bool IsColumnVisible(DataGridColumn column)
            => column != null && column.Visibility == Visibility.Visible;

        // Last-resort per-column width — used only during the transient template-instantiation
        // window where neither the panel nor the grid has finite widths to work with.
        private const double DefaultColumnWidth = 120.0;
    }
}
