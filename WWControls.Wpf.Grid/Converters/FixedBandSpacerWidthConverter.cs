using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// <see cref="GridLength"/> of a horizontal-scrollbar spacer column covering one fixed
    /// band plus its separator strip. Takes [band width, separator width]; returns 0 while
    /// the band is empty so the scrollbar reclaims the space (the separator collapses with
    /// the band).
    /// </summary>
    public class FixedBandSpacerWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double band = values.Length > 0 && values[0] is double b ? b : 0;
            double separator = values.Length > 1 && values[1] is double s ? s : 0;
            return new GridLength(band > 0 ? band + Math.Max(0, separator) : 0.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
