using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf
{
    public static class VisualTreeHelperMethods
    {
        /// <summary>
        /// Walks the visual tree upward from <paramref name="start"/> (inclusive) and returns
        /// the first element of type <typeparamref name="T"/>, or null if none is found.
        /// </summary>
        public static T FindVisualAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Walks the visual tree downward from <paramref name="parent"/> and returns the first
        /// descendant of type <typeparamref name="T"/>, optionally matching
        /// <see cref="FrameworkElement.Name"/>. Returns null if no match exists.
        /// </summary>
        public static T FindVisualDescendant<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed)
                {
                    if (string.IsNullOrEmpty(name)) return typed;
                    if (child is FrameworkElement fe && fe.Name == name) return typed;
                }

                var nested = FindVisualDescendant<T>(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

        /// <summary>
        /// Walks the visual tree downward from <paramref name="parent"/> and yields every
        /// descendant of type <typeparamref name="T"/>.
        /// </summary>
        public static IEnumerable<T> FindVisualDescendants<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) yield return typed;
                foreach (var nested in FindVisualDescendants<T>(child)) yield return nested;
            }
        }
    }
}
