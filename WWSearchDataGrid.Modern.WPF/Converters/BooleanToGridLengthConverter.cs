using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// True → star-sized <see cref="GridLength"/>, false → zero width. Collapses a Grid column
    /// entirely when a layout region is hidden — Visibility=Collapsed alone leaves a star
    /// column reserving its share of the width (used by the summary editor to give the single
    /// ordered list the full row when the alignment sides are hidden).
    /// </summary>
    public class BooleanToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
