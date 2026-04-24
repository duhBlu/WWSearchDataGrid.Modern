using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    /// <summary>
    /// Converts an enum value to a boolean for RadioButton binding.
    /// ConverterParameter is the string name of the enum value to match.
    /// Returns true when the bound enum value matches the parameter.
    /// </summary>
    internal class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is true && parameter is string enumString)
                return Enum.Parse(targetType, enumString);

            return Binding.DoNothing;
        }
    }
}
