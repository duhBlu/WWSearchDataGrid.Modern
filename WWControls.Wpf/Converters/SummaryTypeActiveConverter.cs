using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WWControls.Core;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// True when a summary collection contains an <em>own-target</em> item (no
    /// <see cref="SummaryItem.FieldName"/>) of the <see cref="SummaryItemType"/> passed as the
    /// converter parameter. Drives the check glyphs in the totals-cell picker and the fixed
    /// panel's Count item. FieldName-targeted entries are deliberately ignored — a
    /// <c>Max(OrderDate)</c> entry rendered under another column's cell is that entry's own
    /// configuration (managed via the Customize editor), not the cell column's Max. Bind the
    /// collection first and its <c>Count</c> second — the count binding exists purely so
    /// collection mutations re-evaluate the result (<see cref="FreezableCollection{T}"/>
    /// raises <c>Count</c> property changes).
    /// </summary>
    public class SummaryTypeActiveConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not { Length: > 0 }
                || values[0] is not FreezableCollection<SummaryItem> items
                || parameter is not SummaryItemType type)
                return false;

            foreach (var item in items)
            {
                if (item?.SummaryType == type && string.IsNullOrEmpty(item.FieldName))
                    return true;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
