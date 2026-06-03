using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Two-input MultiBinding converter: <c>values[0]</c> is a <see cref="bool"/> gate;
    /// <c>values[1]</c> is the passthrough value. Returns <c>values[1]</c> when the gate is
    /// <c>true</c>, otherwise <see cref="DependencyProperty.UnsetValue"/> so the target falls
    /// back to its default (typically <c>null</c> for reference-type DPs).
    /// </summary>
    public sealed class BoolGateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is { Length: >= 2 } && values[0] is bool gate && gate)
                return values[1];
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;
    }
}
