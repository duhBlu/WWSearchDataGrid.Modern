using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WWControls.Wpf.Controls.Primitives
{
    public static class VisualTreeHelperMethods
    {
        /// <summary>
        /// Walks up one step from <paramref name="node"/>, transparently hopping from a
        /// <see cref="ContentElement"/> (e.g. a <c>Run</c> or <c>Hyperlink</c> — which are not
        /// <see cref="Visual"/>s) onto its hosting element. Use this instead of
        /// <see cref="VisualTreeHelper.GetParent"/> when the starting node may be text content
        /// (a hit-test result or <c>e.OriginalSource</c>): <c>VisualTreeHelper.GetParent</c> throws
        /// "'…' is not a Visual or Visual3D" on a <see cref="ContentElement"/>.
        /// </summary>
        public static DependencyObject GetParent(DependencyObject node)
        {
            if (node == null)
                return null;

            if (node is Visual || node is Visual3D)
                return VisualTreeHelper.GetParent(node);

            if (node is ContentElement contentElement)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null)
                    return parent;

                return contentElement is FrameworkContentElement fce ? fce.Parent : null;
            }

            return (node as FrameworkElement)?.Parent;
        }

        /// <summary>
        /// Walks the tree upward from <paramref name="start"/> (inclusive) and returns
        /// the first element of type <typeparamref name="T"/>, or null if none is found.
        /// Tolerates a <see cref="ContentElement"/> start (e.g. a <c>Run</c>) via <see cref="GetParent"/>.
        /// </summary>
        public static T FindVisualAncestor<T>(DependencyObject start) where T : class
        {
            var current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = GetParent(current);
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
