using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{

    /// <summary>
    /// Template selector for filter value display
    /// </summary>
    public class FilterValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FlatListTemplate { get; set; }
        public DataTemplate GroupedTreeViewTemplate { get; set; }
        public DataTemplate DateTreeViewTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FilterValueViewModel viewModel)
            {
                switch (viewModel)
                {
                    case FlatListFilterValueViewModel _:
                        return FlatListTemplate;
                    case GroupedTreeViewFilterValueViewModel _:
                        return GroupedTreeViewTemplate;
                    case DateTreeViewFilterValueViewModel _:
                        return DateTreeViewTemplate;
                    default:
                        return base.SelectTemplate(item, container);
                }
            }
            return base.SelectTemplate(item, container);
        }
    }
}
