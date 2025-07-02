using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converter that compares two string values for equality
    /// </summary>
    public class StringEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return false;

            var value1 = values[0]?.ToString();
            var value2 = values[1]?.ToString();

            return string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}