using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Converts a tree's <see cref="WWTreeViewItem.Indentation"/> into a left-only <see cref="Thickness"/>
    /// used to position a connector-line element relative to its item. The <c>ConverterParameter</c> is the
    /// factor applied to the indentation — e.g. <c>-0.5</c> places the element half an indent to the left of
    /// the item (under the parent's expander), <c>-1</c> a full indent left. Deriving the offsets from the
    /// live indentation keeps the connector geometry correct when <see cref="WWTreeView.Indentation"/> changes.
    /// </summary>
    public class IndentationToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double indent = value is double d ? d : 0d;
            double factor = ParseFactor(parameter);
            return new Thickness(indent * factor, 0, 0, 0);
        }

        private static double ParseFactor(object parameter)
        {
            if (parameter is double pd)
                return pd;
            if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                return parsed;
            return 0d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
