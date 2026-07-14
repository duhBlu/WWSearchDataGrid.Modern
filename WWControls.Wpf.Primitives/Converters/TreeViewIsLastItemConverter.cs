using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// Returns <see langword="true"/> when the bound <see cref="TreeViewItem"/> is the last item in
    /// its parent <see cref="ItemsControl"/>. <see cref="WWTreeViewItem"/> uses it to stop the
    /// vertical connector line at the final child instead of running it past the node.
    /// </summary>
    public class TreeViewIsLastItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TreeViewItem item))
                return false;

            ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(item);
            if (parent == null)
                return false;

            return parent.ItemContainerGenerator.IndexFromContainer(item) == parent.Items.Count - 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
