using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    class VisualTreeHelperMethods
    {
        internal static T FindAncestor<T>(DependencyObject current) where T : class
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;

                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Finds a parent of a specific type in the visual tree
        /// </summary>
        internal static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            try
            {
                DependencyObject parentObject = VisualTreeHelper.GetParent(child);

                if (parentObject == null)
                    return null;

                if (parentObject is T parent)
                    return parent;

                return FindVisualParent<T>(parentObject);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds a child of a specific type with a specific name in the visual tree
        /// </summary>
        internal static T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            try
            {
                if (parent == null)
                    return null;

                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                    {
                        if (string.IsNullOrEmpty(name))
                            return typedChild;

                        if (child is FrameworkElement element && element.Name == name)
                            return typedChild;
                    }

                    var result = FindVisualChild<T>(child, name);
                    if (result != null)
                        return result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds all children of a specific type in the visual tree
        /// </summary>
        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();

            try
            {
                if (parent == null)
                    return children;

                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                        children.Add(typedChild);

                    children.AddRange(FindVisualChildren<T>(child));
                }
            }
            catch
            {
                // Return what we found so far
            }

            return children;
        }
    }
}
