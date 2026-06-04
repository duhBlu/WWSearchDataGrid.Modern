using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Reads the live <c>IsExpanded</c> state of the <see cref="Expander"/> inside the entry's
    /// realized <see cref="GroupItem"/>. Lets the pinned chrome bind its chevron rotation
    /// (typically via <see cref="System.Windows.Controls.Primitives.ToggleButton.IsChecked"/>
    /// OneWay) to the real expand state without requiring a mutable property on
    /// <see cref="FixedGroupHeaderEntry"/>.
    /// </summary>
    /// <remarks>
    /// One-way only — the binding source is a method walk on the entry. After the user clicks a
    /// pinned header, <c>ContextMenuCommands.ToggleFixedGroupCommand</c> toggles the underlying
    /// expander AND requests a strip refresh, which constructs a fresh
    /// <see cref="FixedGroupHeaderEntry"/> instance; the binding re-evaluates against the new
    /// entry and the chevron updates.
    /// </remarks>
    public class FixedGroupEntryIsExpandedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not FixedGroupHeaderEntry entry || entry.RepresentedGroupItem == null)
                return true; // default: expanded (matches AutoExpandAllGroups default)

            var expander = VisualTreeHelperMethods.FindVisualDescendant<Expander>(entry.RepresentedGroupItem);
            return expander?.IsExpanded ?? true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
