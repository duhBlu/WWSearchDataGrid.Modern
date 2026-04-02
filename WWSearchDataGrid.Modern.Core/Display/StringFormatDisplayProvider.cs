using System;
using System.Globalization;

namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Display value provider that uses a .NET format string to transform raw values.
    /// Supports standard numeric formats (N2, C2, F3, P1), date formats (MM/dd/yyyy), and custom formats.
    /// </summary>
    public class StringFormatDisplayProvider : IDisplayValueProvider
    {
        private readonly string _format;

        /// <summary>
        /// Creates a new StringFormatDisplayProvider with the specified format string.
        /// </summary>
        /// <param name="format">A .NET format string (e.g., "N2", "C2", "MM/dd/yyyy", "0.00")</param>
        public StringFormatDisplayProvider(string format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        /// <summary>
        /// Formats a raw value using the configured format string.
        /// </summary>
        public string FormatValue(object rawValue)
        {
            if (rawValue == null)
                return string.Empty;

            if (rawValue is IFormattable formattable)
                return formattable.ToString(_format, CultureInfo.CurrentCulture);

            return rawValue.ToString();
        }

        /// <summary>
        /// Parses a formatted display string back to a numeric or date value.
        /// Handles currency symbols, thousands separators, and percentage signs automatically.
        /// </summary>
        public object ParseValue(string displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText))
                return null;

            // Try decimal first (handles currency, number, percent formats)
            if (decimal.TryParse(displayText, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal decimalResult))
                return decimalResult;

            // Try DateTime
            if (DateTime.TryParse(displayText, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dateResult))
                return dateResult;

            // Return the string as-is if no conversion works
            return displayText;
        }

        public bool CanParse => true;

        public bool UseRawComparison => false;
    }
}
