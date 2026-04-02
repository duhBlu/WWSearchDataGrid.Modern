using System;
using System.Globalization;
using System.Windows.Data;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Display
{
    /// <summary>
    /// Display value provider that wraps a WPF <see cref="IValueConverter"/> for display transformation.
    /// Lives in the WPF project since IValueConverter is a WPF type.
    /// </summary>
    public class ConverterDisplayProvider : IDisplayValueProvider
    {
        private readonly IValueConverter _converter;
        private readonly object _parameter;

        /// <summary>
        /// Creates a new ConverterDisplayProvider with the specified converter and optional parameter.
        /// </summary>
        /// <param name="converter">The WPF value converter to use for formatting</param>
        /// <param name="parameter">Optional converter parameter passed to Convert/ConvertBack</param>
        public ConverterDisplayProvider(IValueConverter converter, object parameter = null)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _parameter = parameter;
        }

        /// <summary>
        /// Converts a raw value to its display string using the converter's Convert method.
        /// </summary>
        public string FormatValue(object rawValue)
        {
            try
            {
                var result = _converter.Convert(rawValue, typeof(string), _parameter, CultureInfo.CurrentCulture);
                return result?.ToString() ?? string.Empty;
            }
            catch
            {
                return rawValue?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Converts a display string back to a raw value using the converter's ConvertBack method.
        /// </summary>
        public object ParseValue(string displayText)
        {
            try
            {
                return _converter.ConvertBack(displayText, typeof(object), _parameter, CultureInfo.CurrentCulture);
            }
            catch
            {
                return displayText;
            }
        }

        public bool CanParse => true;

        public bool UseRawComparison => false;
    }
}
