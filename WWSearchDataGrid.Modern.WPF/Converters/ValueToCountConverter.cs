using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converter that looks up a value's occurrence count from a dictionary and formats it
    /// Used to display count information in SearchTextBox dropdown items
    /// </summary>
    public class ValueToCountConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a value and a counts dictionary to a formatted count string
        /// </summary>
        /// <param name="values">Array with [0] = item value, [1] = IReadOnlyDictionary&lt;object, int&gt; counts</param>
        /// <param name="targetType">Target type (not used)</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture info (not used)</param>
        /// <returns>Formatted string like " (123)" or empty string if count not found</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return string.Empty;

            var value = values[0];
            var countsDict = values[1] as IReadOnlyDictionary<object, int>;

            if (value == null || countsDict == null)
                return string.Empty;

            if (countsDict.TryGetValue(value, out var count))
            {
                return $" ({count})";
            }

            return string.Empty;
        }

        /// <summary>
        /// Not implemented (one-way converter only)
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ValueToCountConverter is a one-way converter only");
        }
    }
}
