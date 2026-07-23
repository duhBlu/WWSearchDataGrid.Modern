using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Layout panel for the banded column-header area. Hosts one <see cref="BandHeaderCell"/> per
    /// band; each cell is positioned to span its member columns horizontally (mirroring the column
    /// headers' rendered X/width, the same source of truth the filter row uses) and stacked
    /// vertically by band <see cref="BandHeaderCell.BandLevel"/>. Non-virtualizing — every band
    /// gets a materialized cell.
    /// </summary>
    /// <remarks>
    /// Bands are contiguous (columns can't leave their band), so a band's bounds are simply the
    /// leftmost member's left edge to the rightmost member's right edge. Fixed/pinned-column band
    /// clipping is not handled here yet (v1 limitation — see the plan); the presenter's
    /// <c>ClipToBounds</c> keeps cells inside the viewport.
    /// </remarks>
    public class BandHeaderPanel : Panel
    {
        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(nameof(OwnerGrid), typeof(SearchDataGrid), typeof(BandHeaderPanel),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(BandHeaderPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty RowHeightProperty =
            DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(BandHeaderPanel),
                new FrameworkPropertyMetadata(26.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }

        /// <summary>The grid's horizontal scroll offset, pushed by the presenter, used by the width fallback.</summary>
        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>Height of a single band caption row.</summary>
        public double RowHeight
        {
            get => (double)GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var grid = OwnerGrid;
            double rowHeight = RowHeight;
            int depth = grid?.MaxBandDepth ?? 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;
                double estWidth = child is BandHeaderCell cell ? EstimateSpanWidth(cell) : 0;
                child.Measure(new Size(estWidth > 0 ? estWidth : double.PositiveInfinity, rowHeight));
            }

            double totalWidth = 0;
            if (grid != null)
            {
                foreach (var column in grid.Columns)
                {
                    if (column.Visibility != Visibility.Visible) continue;
                    totalWidth += ResolveColumnWidth(column);
                }
            }

            return new Size(totalWidth, depth * rowHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var grid = OwnerGrid;
            if (grid == null || InternalChildren.Count == 0)
                return finalSize;

            double rowHeight = RowHeight;
            int maxDepth = Math.Max(1, grid.MaxBandDepth);
            var geometry = BuildColumnGeometry(grid);

            // Snap band edges to whole DEVICE pixels via a SHARED row-boundary array. A tall
            // single-band cell (rows 0..2) and a short nested-band cell (rows 1..2) share their
            // bottom edge; rounding each cell's Y and Height independently drifts that shared edge
            // by a device pixel at fractional DPI (e.g. 125%). Taking both cells' bottom from the
            // same snapped rowEdges[] value keeps the border lines flush.
            double scaleX = 1.0, scaleY = 1.0;
            try { var dpi = VisualTreeHelper.GetDpi(this); scaleX = dpi.DpiScaleX; scaleY = dpi.DpiScaleY; }
            catch (InvalidOperationException) { }
            double SnapX(double v) => scaleX > 0 ? Math.Round(v * scaleX) / scaleX : v;
            double SnapY(double v) => scaleY > 0 ? Math.Round(v * scaleY) / scaleY : v;

            var rowEdges = new double[maxDepth + 1];
            for (int i = 0; i <= maxDepth; i++)
                rowEdges[i] = SnapY(i * rowHeight);

            foreach (UIElement child in InternalChildren)
            {
                if (child is not BandHeaderCell cell)
                {
                    child?.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                double minX = double.PositiveInfinity;
                double maxRight = double.NegativeInfinity;
                foreach (var column in cell.MemberColumns)
                {
                    if (column == null || column.Visibility != Visibility.Visible) continue;
                    if (!geometry.TryGetValue(column, out var g)) continue;
                    if (g.x < minX) minX = g.x;
                    if (g.x + g.width > maxRight) maxRight = g.x + g.width;
                }

                if (double.IsInfinity(minX) || maxRight <= minX)
                {
                    // No visible/resolved members — collapse the cell.
                    cell.Arrange(new Rect(0, 0, 0, 0));
                    cell.SetCurrentValue(VisibilityProperty, Visibility.Hidden);
                    continue;
                }

                cell.ClearValue(VisibilityProperty);

                // Leaf bands (no child bands) span down to the column headers so the caption sits
                // directly above their columns; bands with sub-bands stay one row (children fill below).
                int level = Math.Min(cell.BandLevel, maxDepth - 1);
                int rows = cell.HasChildBands ? 1 : Math.Max(1, maxDepth - level);
                int bottomIndex = Math.Min(level + rows, maxDepth);

                double x = SnapX(minX);
                double right = SnapX(maxRight);
                double top = rowEdges[level];
                double bottom = rowEdges[bottomIndex];
                cell.Arrange(new Rect(x, top, right - x, bottom - top));
            }

            return finalSize;
        }

        /// <summary>
        /// Column → (x, width) in this panel's coordinate space. Primary: mirror each column
        /// header's rendered position (the headers are the source of truth — data cells share
        /// their arrangement). Fallback (headers not materialized yet): cumulative widths in
        /// display order, shifted by the horizontal scroll offset.
        /// </summary>
        private Dictionary<DataGridColumn, (double x, double width)> BuildColumnGeometry(SearchDataGrid grid)
        {
            var map = new Dictionary<DataGridColumn, (double x, double width)>(grid.Columns.Count);

            var headerLookup = BuildHeaderLookup(grid);
            if (headerLookup != null && headerLookup.Count > 0)
            {
                foreach (var column in grid.Columns)
                {
                    if (column.Visibility != Visibility.Visible) continue;
                    if (!headerLookup.TryGetValue(column, out var header) || header.ActualWidth <= 0) continue;
                    try
                    {
                        double x = header.TranslatePoint(new Point(0, 0), this).X;
                        map[column] = (x, header.ActualWidth);
                    }
                    catch (InvalidOperationException)
                    {
                        // No common ancestor yet — skip; the fallback below covers first paint.
                    }
                }
                if (map.Count > 0)
                    return map;
            }

            double cursor = 0;
            double offset = HorizontalOffset;
            foreach (var column in grid.Columns.OrderBy(c => c.DisplayIndex))
            {
                if (column.Visibility != Visibility.Visible) continue;
                double w = ResolveColumnWidth(column);
                map[column] = (cursor - offset, w);
                cursor += w;
            }
            return map;
        }

        private double EstimateSpanWidth(BandHeaderCell cell)
        {
            double sum = 0;
            foreach (var column in cell.MemberColumns)
            {
                if (column == null || column.Visibility != Visibility.Visible) continue;
                sum += ResolveColumnWidth(column);
            }
            return sum;
        }

        // The headers presenter is named inside DG_ScrollViewer's own template namescope — the
        // grid template's namescope only knows DG_ScrollViewer itself (same lookup the filter row
        // uses; a plain grid.Template.FindName returns null here).
        private static Dictionary<DataGridColumn, DataGridColumnHeader> BuildHeaderLookup(SearchDataGrid grid)
        {
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

        // Returns 0 before the grid's first measure resolves Auto/Star widths; callers substitute.
        private static double ResolveColumnWidth(DataGridColumn column)
        {
            if (column.ActualWidth > 0) return column.ActualWidth;
            if (column.Width.IsAbsolute) return column.Width.Value;
            return 0;
        }
    }
}
