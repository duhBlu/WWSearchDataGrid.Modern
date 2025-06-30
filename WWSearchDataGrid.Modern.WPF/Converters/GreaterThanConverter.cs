using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converter that returns true if the value is greater than the parameter
    /// </summary>
    public class GreaterThanConverter : IValueConverter
    {
        public static readonly GreaterThanConverter Instance = new GreaterThanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            if (double.TryParse(value.ToString(), out double doubleValue) &&
                double.TryParse(parameter.ToString(), out double threshold))
            {
                return doubleValue > threshold;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}