using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Produces the <see cref="FrameworkElement.Margin"/> for a pinned header in the sticky
    /// strip. Mirrors <see cref="GroupHeaderMarginConverter"/>'s output formula —
    /// <c>Left = rowHeaderWidth + level × indent</c> — so a pinned header lines up exactly with
    /// the in-place expander at the same nesting depth and there is no horizontal jump when an
    /// in-place header rises into the strip's slot.
    /// </summary>
    /// <remarks>
    /// Inputs (in order):
    /// <list type="number">
    ///   <item><see cref="FixedGroupHeaderEntry.Level"/> — the entry's zero-based nesting depth
    ///         (bound directly off the entry's <c>DataContext</c>).</item>
    ///   <item><see cref="System.Windows.Controls.DataGrid.RowHeaderActualWidth"/> — the base
    ///         offset that clears the row-header gutter at the grid's left edge.</item>
    ///   <item><see cref="SearchDataGrid.GroupIndentWidth"/> — per-level indent step.</item>
    /// </list>
    /// Unlike <see cref="GroupHeaderMarginConverter"/>, this converter takes the level as a
    /// primitive int rather than deriving it from a visual ancestor walk — pinned headers live
    /// outside the rows-presenter subtree and have no <see cref="System.Windows.Controls.GroupItem"/>
    /// ancestor to count.
    /// </remarks>
    public class FixedGroupIndentConverter : IMultiValueConverter
    {
        /// <summary>
        /// Fallback indent (in DIPs) used when the third binding value is missing or NaN.
        /// Matches <see cref="SearchDataGrid.GroupIndentWidth"/>'s default so the strip's
        /// stair-step stays consistent even if the grid binding hasn't resolved yet.
        /// </summary>
        public double FallbackIndent { get; set; } = 16;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int level = (values?.Length > 0 && values[0] is int l && l >= 0) ? l : 0;
            double rowHeaderWidth = (values?.Length > 1 && values[1] is double w && !double.IsNaN(w) && w > 0)
                ? w : 0;
            double indent = (values?.Length > 2 && values[2] is double i && !double.IsNaN(i) && i >= 0)
                ? i : FallbackIndent;

            return new Thickness(rowHeaderWidth + level * indent, 0, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;
    }
}
