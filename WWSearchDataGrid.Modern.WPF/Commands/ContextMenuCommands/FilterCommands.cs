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
        public static ICommand ClearColumnFilterCommand => new RelayCommand<ColumnSearchBox>(columnSearchBox =>
        {
            columnSearchBox.ClearFilter();
        }, columnSearchBox => columnSearchBox?.HasActiveFilter == true);

    }
}
