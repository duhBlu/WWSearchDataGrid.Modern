using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Picks the template used by the Filter Editor's inner ItemsControl for each child node —
    /// the recursive group template for <see cref="FilterGroupNode"/>, the row template for
    /// <see cref="FilterConditionNode"/>. Templates are resolved by resource key at runtime so
    /// the master template dictionary can declare them in any order.
    /// </summary>
    public class FilterEditorNodeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            if (element == null) return base.SelectTemplate(item, container);

            switch (item)
            {
                case FilterGroupNode _: return element.TryFindResource(SdgThemeKeys.FilterEditorGroup) as DataTemplate;
                case FilterConditionNode _: return element.TryFindResource(SdgThemeKeys.FilterEditorConditionRow) as DataTemplate;
                default: return base.SelectTemplate(item, container);
            }
        }
    }
}
