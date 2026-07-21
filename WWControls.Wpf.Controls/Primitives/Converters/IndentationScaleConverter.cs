using System;
using System.Globalization;
using System.Windows.Data;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Scales a tree's <see cref="WWTreeViewItem.Indentation"/> by the factor given in
    /// <c>ConverterParameter</c> and returns the resulting <see cref="double"/>. Used to size a
    /// connector element (e.g. the horizontal stub's length) relative to the live indent — a Rectangle
    /// has no intrinsic width, so it can't rely on <c>Stretch</c> inside the item's auto-sized indent column.
    /// </summary>
    public class IndentationScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double indent = value is double d ? d : 0d;
            double factor = ParseFactor(parameter);
            return indent * factor;
        }

        private static double ParseFactor(object parameter)
        {
            if (parameter is double pd)
                return pd;
            if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                return parsed;
            return 1d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
