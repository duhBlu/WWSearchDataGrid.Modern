using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    /// <summary>
    /// Converts a boolean Submitted value to "Yes"/"No" display text.
    /// Supports ConvertBack for range-based filter parsing.
    /// </summary>
    internal class SubmittedToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? "Yes" : "No";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }
}
