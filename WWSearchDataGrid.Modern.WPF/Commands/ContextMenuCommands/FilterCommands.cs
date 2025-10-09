using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    internal partial class ContextMenuCommands
    {
        /// <summary>
        /// Clears all filters on the grid
        /// </summary>
        public static ICommand ClearAllFiltersCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            grid.ClearAllFilters();
        }, grid => grid?.DataColumns?.Any(c => c.HasActiveFilter) == true);


        /// <summary>
        /// Clears the filter on the current column
        /// </summary>
        public static ICommand ClearColumnFilterCommand => new RelayCommand(
            parameter =>
            {
                switch (parameter)
                {
                    case ColumnSearchBox columnSearchBox:
                        columnSearchBox.ClearFilter();
                        break;
                    case ContextMenuContext context when context.ColumnSearchBox != null:
                        context.ColumnSearchBox.ClearFilter();
                        break;
                }
            },
            parameter =>
            {
                return parameter switch
                {
                    ColumnSearchBox columnSearchBox => columnSearchBox?.HasActiveFilter == true,
                    ContextMenuContext context => context?.ColumnSearchBox?.HasActiveFilter == true,
                    _ => false
                };
            });

    }
}
