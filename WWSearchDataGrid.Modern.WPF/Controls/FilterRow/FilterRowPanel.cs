using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Layout panel mirroring <see cref="DataGridCellsPanel"/>: children are ordered by
    /// <see cref="DataGridColumn.DisplayIndex"/>, the first <see cref="DataGrid.FrozenColumnCount"/>
    /// stay pinned, and the rest translate by <c>-HorizontalOffset</c> with clipping against
    /// the frozen block. Non-virtualizing — every column gets a materialized child.
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

            int frozenCount = Math.Min(grid.FrozenColumnCount, pairs.Count);

            // Same fallback math as MeasureOverride — arrange widths must match measure widths
            // or unresolved columns push siblings offscreen.
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
            for (int i = 0; i < frozenCount; i++)
            {
                var (column, child) = pairs[i];
                if (!IsColumnVisible(column))
                {
                    HideChild(child);
                    continue;
                }

                double width = ResolveArrangedWidth(column, fallbackWidth);
                ArrangeChild(child, new Rect(frozenCursor, 0, width, finalSize.Height), clip: null);
                frozenCursor += width;
            }

            double scroll = HorizontalOffset;
            double nonFrozenCursor = frozenCursor;

            for (int i = frozenCount; i < pairs.Count; i++)
            {
                var (column, child) = pairs[i];
                if (!IsColumnVisible(column))
                {
                    HideChild(child);
                    continue;
                }

                double width = ResolveArrangedWidth(column, fallbackWidth);
                double x = nonFrozenCursor - scroll;
                var bounds = new Rect(x, 0, width, finalSize.Height);

                double leftClip = Math.Max(0, frozenCursor - x);
                Geometry clip = null;
                if (leftClip > 0 && leftClip < width)
                    clip = new RectangleGeometry(new Rect(leftClip, 0, width - leftClip, finalSize.Height));
                else if (leftClip >= width)
                    clip = Geometry.Empty;

                ArrangeChild(child, bounds, clip);
                nonFrozenCursor += width;
            }

            return finalSize;
        }

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
