using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Reads the expansion state of the group a pinned <see cref="FixedGroupHeaderEntry"/> mirrors,
    /// so the pinned chrome can bind its chevron rotation (typically via
    /// <see cref="System.Windows.Controls.Primitives.ToggleButton.IsChecked"/> OneWay) without a
    /// mutable property on the entry.
    /// </summary>
    /// <remarks>
    /// One-way only. The entry's node carries a snapshot of its expansion at projection time, and
    /// the strip rebuilds its entries on every toggle, so the binding re-evaluates against a fresh
    /// entry and the chevron updates.
    /// </remarks>
    public class FixedGroupEntryIsExpandedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is FixedGroupHeaderEntry { Node: { } node } ? node.IsExpanded : true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
