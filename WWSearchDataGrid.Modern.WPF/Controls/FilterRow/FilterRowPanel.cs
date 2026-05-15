using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom panel that lays out one child per <see cref="DataGridColumn"/> in the parent
    /// <see cref="SearchDataGrid"/>, mirroring the column ordering, widths, and frozen-column
    /// behavior of the grid's data cells. Used by <see cref="AutoFilterRowPresenter"/> to
    /// host per-column filter editors in the pinned filter row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Layout matches <see cref="DataGridCellsPanel"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item>Children are ordered by their column's <see cref="DataGridColumn.DisplayIndex"/>.</item>
    ///   <item>The first <see cref="DataGrid.FrozenColumnCount"/> display-positions stay pinned at
    ///   the left edge regardless of horizontal scroll.</item>
    ///   <item>Non-frozen children translate by <c>-HorizontalOffset</c> and are clipped to the
    ///   region right of the frozen block, so they scroll under the frozen columns rather
    ///   than overlapping them.</item>
    /// </list>
    /// <para>
    /// Non-virtualizing in v1: every column gets a materialized child for the lifetime of the
    /// row.
    /// </para>
    /// </remarks>
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
                // No grid attached yet — let each child measure against the panel's full
                // available width. Wrong long-term but produces a sensible first-frame
                // rendering until the presenter wires OwnerGrid.
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

            // Compute fallback width for columns whose ActualWidth hasn't been published yet
            // (first layout cycle, before DataGridColumnHeadersPresenter has resolved
            // Auto / Star widths). Divide the panel's remaining horizontal space evenly
            // among the unresolved columns so each cell gets a sane initial width instead
            // of collapsing to zero.
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
                // Fallback: parent template is in flight and OwnerGrid hasn't been wired yet,
                // but children exist. Arrange them in order at their DesiredSize.Width so they
                // at least render — the parent presenter will re-trigger arrange once
                // OwnerGrid lands. Without this branch the children render at zero size and
                // the row appears empty even though it has full layout space.
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

            // Same fallback math as MeasureOverride so the widths used at arrange time match
            // the per-child measure widths. Without this, a column whose ActualWidth is 0 at
            // measure time would arrange at a different width than it was measured against
            // (the old code returned child.DesiredSize.Width here — equal to the entire panel
            // width because the pre-pass over-allocated — pushing every other child offscreen).
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

        // ActualWidth is authoritative once the parent DataGrid has measured. Before that —
        // the very first layout cycle, when DataGridColumnHeadersPresenter hasn't yet
        // resolved Auto / Star widths — return 0 so the caller knows to substitute its own
        // fallback (an even split of the panel's remaining space across unresolved columns).
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

        // Last-resort per-column width used only when the panel itself has no finite
        // available width AND the parent grid hasn't published any column ActualWidths yet —
        // a transient state during template instantiation. 120 px is wider than typical
        // text columns and narrower than typical date / combo columns; the DPD subscription
        // on ActualWidthProperty will pull real widths through on the next layout pass.
        private const double DefaultColumnWidth = 120.0;
    }
}
