using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    /// <summary>
    /// Converts a value to a boolean based on whether it's null or not.
    /// Returns true if the value is not null, false if it is null.
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
