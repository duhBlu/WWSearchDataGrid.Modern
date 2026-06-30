using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        private static ICommand _clearAllFiltersCommand;
        /// <summary>
        /// Clears all filters on the grid
        /// </summary>
        public static ICommand ClearAllFiltersCommand => _clearAllFiltersCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            grid.ClearAllFilters();
        }, grid => grid?.DataColumns?.Any(c => c.HasActiveFilter) == true);

        private static ICommand _clearColumnFilterCommand;
        /// <summary>
        /// Clears the filter on the current column
        /// </summary>
        public static ICommand ClearColumnFilterCommand => _clearColumnFilterCommand ??= new RelayCommand(
            parameter =>
            {
                switch (parameter)
                {
                    case IColumnFilterHost columnSearchBox:
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
                    IColumnFilterHost columnSearchBox => columnSearchBox?.HasActiveFilter == true,
                    ContextMenuContext context => context?.ColumnSearchBox?.HasActiveFilter == true,
                    _ => false
                };
            });

        private static ICommand _filterByCellValueCommand;
        /// <summary>
        /// Replaces the column's filter with an equality filter against the right-clicked
        /// cell's raw value. When the cell value is null, applies an <see cref="SearchType.IsNull"/>
        /// filter instead so the action remains meaningful on empty cells. Mirrors the
        /// programmatic-filter pattern used by <c>ApplyCheckboxBooleanFilter</c>: rebuild the
        /// column's <see cref="SearchTemplateController.SearchGroups"/> to a single
        /// group+template, then drive the grid's filter pipeline.
        /// </summary>
        public static ICommand FilterByCellValueCommand => _filterByCellValueCommand ??= new RelayCommand<ContextMenuContext>(
            context =>
            {
                var controller = context?.ColumnSearchBox?.SearchTemplateController;
                if (controller == null) return;

                try
                {
                    controller.SearchGroups.Clear();
                    var group = new SearchTemplateGroup();
                    controller.SearchGroups.Add(group);

                    var template = new SearchTemplate(controller.ColumnDataType)
                    {
                        SearchType = context.CellValue == null ? SearchType.IsNull : SearchType.Equals,
                        SelectedValue = context.CellValue,
                        SearchTemplateController = controller,
                    };
                    controller.SubscribeToTemplateChanges(template);
                    group.SearchTemplates.Add(template);

                    controller.UpdateOperatorVisibility();
                    controller.UpdateFilterExpression();

                    context.Grid?.FilterItemsSource();
                    context.Grid?.UpdateFilterSummaryPanel();
                    context.ColumnSearchBox?.UpdateHasActiveFilterState();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in FilterByCellValueCommand: {ex.Message}");
                }
            },
            context => context?.ColumnSearchBox?.SearchTemplateController != null);

    }
}
