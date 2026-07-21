using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Sizes a <see cref="WWTreeViewItem"/>'s horizontal connector stub from the tree's
    /// <see cref="WWTreeView.Indentation"/> and whether the item has an expander. A leaf gets the full
    /// 1.5-indent stub so the line reaches the header content; a node with a chevron gets a half-indent
    /// stub that stops at the expander column instead of running under and past the chevron glyph.
    /// <para>Values: [0] indentation (double), [1] has-expandable-children (bool).</para>
    /// </summary>
    public class HorizontalConnectorWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double indent = values.Length > 0 && values[0] is double d ? d : 0d;
            bool hasExpander = values.Length > 1 && values[1] is bool b && b;
            return indent * (hasExpander ? 0.5 : 1.5);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
