using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// Produces the <see cref="FrameworkElement.Margin"/> for a group header's chevron + label,
    /// applied inside the group Expander template so the indent affects only the header — not
    /// the Expander itself. Outermost group headers start at
    /// <see cref="DataGrid.RowHeaderActualWidth"/> (clearing the row-header gutter); each
    /// additional nesting level compounds by <see cref="SearchDataGrid.GroupIndentWidth"/>.
    /// </summary>
    /// <remarks>
    /// Inputs (in order):
    /// <list type="number">
    ///   <item><see cref="GroupItem"/> — the originating container; the converter walks its
    ///         visual ancestors and counts each <see cref="GroupItem"/> hop to derive the
    ///         absolute nesting depth.</item>
    ///   <item><see cref="DataGrid.RowHeaderActualWidth"/> — the base offset that clears the
    ///         row-header gutter at the grid's left edge.</item>
    ///   <item><see cref="SearchDataGrid.GroupIndentWidth"/> — per-level indent step.</item>
    /// </list>
    /// Output: a <see cref="Thickness"/> with only <see cref="Thickness.Left"/> set, equal to
    /// <c>rowHeaderWidth + depth * indent</c>. The full depth is encoded in the converter
    /// output (rather than relying on parent Expander margins to compound) because the
    /// Expander now spans the full GroupItem width — its content area must stay flush with
    /// the grid so DataGridRowHeaders remain pinned at x=0.
    /// </remarks>
    public class GroupHeaderMarginConverter : IMultiValueConverter
    {
        /// <summary>
        /// Fallback indent (in DIPs) used when the third binding value is missing or NaN.
        /// Matches <see cref="SearchDataGrid.GroupIndentWidth"/>'s default so the visual stays
        /// consistent even if the grid binding hasn't resolved yet.
        /// </summary>
        public double FallbackIndent { get; set; } = 16;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1) return new Thickness();
            if (values[0] is not GroupItem groupItem) return new Thickness();

            int depth = 0;
            DependencyObject current = VisualTreeHelper.GetParent(groupItem);
            while (current != null)
            {
                if (current is GroupItem) depth++;
                current = VisualTreeHelper.GetParent(current);
            }

            double rowHeaderWidth = (values.Length > 1 && values[1] is double w && !double.IsNaN(w) && w > 0)
                ? w : 0;
            double indent = (values.Length > 2 && values[2] is double i && !double.IsNaN(i) && i >= 0)
                ? i : FallbackIndent;

            return new Thickness(rowHeaderWidth + depth * indent, 0, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;
    }
}
