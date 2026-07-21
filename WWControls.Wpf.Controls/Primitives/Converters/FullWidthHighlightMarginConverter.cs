using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Computes the left margin that pulls a <see cref="WWTreeViewItem"/>'s row-highlight overlay out to
    /// the tree's left edge when <see cref="WWTreeView.RowFullWidthHover"/> is on. A nested item is offset
    /// from the left by one indent per ancestor level, so the highlight (spanning the item's own columns)
    /// only reaches the left edge once shifted back by <c>level × indentation</c>. Off, the margin is zero.
    /// <para>Values: [0] row-full-width-hover (bool), [1] level / depth (int), [2] indentation (double).</para>
    /// </summary>
    public class FullWidthHighlightMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool fullWidth = values.Length > 0 && values[0] is bool b && b;
            if (!fullWidth)
                return new Thickness(0);

            int level = values.Length > 1 && values[1] is int i ? i : 0;
            double indent = values.Length > 2 && values[2] is double d ? d : 0d;
            return new Thickness(-(level * indent), 0, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
