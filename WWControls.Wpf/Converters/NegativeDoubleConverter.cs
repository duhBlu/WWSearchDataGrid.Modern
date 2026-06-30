using System;
using System.Globalization;
using System.Windows.Data;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// Negates a double. Used by render-transform bindings that pull an element leftwards by
    /// a positive width (e.g. the right fixed-band separator translating by
    /// −<see cref="SearchDataGrid.RightFixedColumnsWidth"/>).
    /// </summary>
    public class NegativeDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d ? -d : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d ? -d : 0.0;
    }
}
