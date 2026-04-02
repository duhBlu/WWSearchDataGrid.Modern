using System;
using System.Globalization;
using System.Windows.Data;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Value converter that formats raw values through a mask pattern.
    /// Use the ConverterParameter to specify the mask string.
    ///
    /// Usage in XAML:
    ///   Binding="{Binding PhoneNumber, Converter={StaticResource MaskFormatConverter}, ConverterParameter='(000) 000-0000'}"
    /// </summary>
    public class MaskFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter is not string mask || string.IsNullOrEmpty(mask))
                return value;

            try
            {
                var formatter = new MaskFormatter(mask);
                return formatter.Format(value);
            }
            catch
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter is not string mask || string.IsNullOrEmpty(mask))
                return value;

            try
            {
                var formatter = new MaskFormatter(mask);
                return formatter.Parse(value.ToString());
            }
            catch
            {
                return value;
            }
        }
    }
}
