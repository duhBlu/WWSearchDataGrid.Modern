using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// MultiBinding converter that returns the first input as a <see cref="double"/> when it has
    /// a numeric value, otherwise returns the second input (the fallback). ConvertBack only writes
    /// to the first source — the fallback slot is preserved via <see cref="Binding.DoNothing"/>.
    /// </summary>
    /// <remarks>
    /// Bind the source value first and the desired fallback second. Typical use is showing
    /// a sensible default position on a numeric editor when the underlying source is null
    /// (e.g. the upper thumb of a range slider sitting at the slider's maximum until the user
    /// supplies an explicit upper bound).
    /// </remarks>
    public class NullableNumericFallbackConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return 0.0;

            if (TryCoerceToDouble(values[0], out double sourceValue))
                return sourceValue;

            return TryCoerceToDouble(values[1], out double fallback) ? fallback : 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { value, Binding.DoNothing };
        }

        private static bool TryCoerceToDouble(object value, out double result)
        {
            if (value == null || value == DependencyProperty.UnsetValue)
            {
                result = 0.0;
                return false;
            }

            if (value is double d)
            {
                result = d;
                return true;
            }

            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }
}
