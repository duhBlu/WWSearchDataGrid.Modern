using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf
{
    /// <summary>
    /// Produces the <see cref="FrameworkElement.Margin"/> for a group-panel pill so nested
    /// columns step downward (not rightward) from their parent: <c>Top = GroupLevel ×
    /// GroupIndentWidth</c>. Used by the pill <see cref="DataTemplate"/> in the default
    /// <c>GroupPanel</c> style, which lays its pills out horizontally — so deeper levels
    /// cascade downward while the panel stays compact horizontally.
    /// </summary>
    /// <remarks>
    /// Inputs (in order):
    /// <list type="number">
    ///   <item><see cref="GridColumn.GroupLevel"/> — zero-based nesting depth (the pill's
    ///         <c>DataContext</c> is the column, so this binds directly to the property).</item>
    ///   <item><see cref="SearchDataGrid.GroupIndentWidth"/> — DIPs per nesting level applied
    ///         to the <see cref="Thickness.Top"/> offset.</item>
    /// </list>
    /// Falls back to a 16-DIP indent if the second binding hasn't resolved yet (initial layout
    /// before the grid ancestor is found). A small <see cref="Thickness.Left"/> gap separates
    /// consecutive pills in the horizontal strip; the first pill keeps a zero left margin so
    /// it butts against the panel padding edge.
    /// </remarks>
    public class GroupPillIndentConverter : IMultiValueConverter
    {
        public double FallbackIndent { get; set; } = 16;

        /// <summary>
        /// Horizontal gap (in DIPs) between consecutive pills in the panel's horizontal layout.
        /// Applied to every pill except the first (level 0).
        /// </summary>
        public double InterPillGap { get; set; } = 4;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int level = (values?.Length > 0 && values[0] is int l && l >= 0) ? l : 0;
            double indent = (values?.Length > 1 && values[1] is double w && !double.IsNaN(w) && w >= 0)
                ? w : FallbackIndent;
            return new Thickness(level == 0 ? 0 : InterPillGap, level * indent, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;
    }
}
