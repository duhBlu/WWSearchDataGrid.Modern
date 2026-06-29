using System;
using System.Windows;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Geometry for the right-fixed column band, shared by every surface that anchors
    /// right-pinned columns to the viewport's trailing edge (<see cref="FixedColumnsCellsPanel"/>
    /// for data cells and column headers; the filter-row and group-chrome panels as they adopt
    /// the aligned layout). Left-pinned columns ride WPF's native frozen-column layout
    /// (<see cref="System.Windows.Controls.DataGrid.FrozenColumnCount"/>), so the custom
    /// geometry is the right band plus the clips that let scrollable cells slide under it.
    /// All inputs are in cells-panel space, where the viewport's leading content edge sits at
    /// the grid's horizontal scroll offset — the same frame the native panel uses to anchor
    /// frozen cells.
    /// </summary>
    internal static class FixedColumnLayout
    {
        /// <summary>
        /// Banker's rounding to whole pixels — matches WPF's UseLayoutRounding mode and the
        /// snap convention <see cref="FilterRowPanel"/> established.
        /// </summary>
        internal static double SnapToPixel(double value)
            => Math.Round(value, MidpointRounding.ToEven);

        /// <summary>
        /// X where the right band starts. Anchored to the viewport's right edge, but never
        /// left of the left band's trailing edge — a right band wider than the remaining
        /// viewport is pushed right (its overflow clipped at the viewport edge) rather than
        /// overlapping the left band.
        /// </summary>
        internal static double ComputeRightBandStart(
            double horizontalOffset, double viewportWidth, double leftBandWidth, double rightBandWidth)
        {
            double anchored = horizontalOffset + viewportWidth - rightBandWidth;
            double leftBandEnd = horizontalOffset + leftBandWidth;
            return Math.Max(anchored, leftBandEnd);
        }

        /// <summary>
        /// Clip for a cell crossing a vertical boundary it must not paint past — a scrollable
        /// cell sliding under the right band (boundary = band start) or a right-band cell
        /// overflowing the viewport (boundary = viewport right edge). <c>null</c> when the
        /// cell sits fully inside the boundary, <see cref="Geometry.Empty"/> when fully past
        /// it, else a rect keeping only the leading visible portion.
        /// </summary>
        internal static Geometry ComputeClipAtBoundary(
            double cellX, double cellWidth, double cellHeight, double boundaryX)
        {
            double visibleWidth = boundaryX - cellX;
            if (visibleWidth >= cellWidth) return null;
            if (visibleWidth <= 0) return Geometry.Empty;
            return new RectangleGeometry(new Rect(0, 0, visibleWidth, cellHeight));
        }

        /// <summary>
        /// Clip for a scrollable cell that must stay inside the window between the two band
        /// boundaries — the left band's trailing edge and the right band's leading edge. Pass
        /// infinities for absent bands. <c>null</c> when fully inside,
        /// <see cref="Geometry.Empty"/> when fully covered, else a rect keeping the portion
        /// inside the window.
        /// </summary>
        internal static Geometry ComputeClipToWindow(
            double cellX, double cellWidth, double cellHeight, double windowStartX, double windowEndX)
        {
            double visibleStart = Math.Max(0, windowStartX - cellX);
            double visibleEnd = Math.Min(cellWidth, windowEndX - cellX);
            if (visibleEnd <= visibleStart) return Geometry.Empty;
            if (visibleStart <= 0 && visibleEnd >= cellWidth) return null;
            return new RectangleGeometry(new Rect(visibleStart, 0, visibleEnd - visibleStart, cellHeight));
        }
    }
}
