using System;
using System.Diagnostics;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        #region Grouping Commands

        private static ICommand _groupByColumnCommand;
        /// <summary>
        /// Groups the grid by the context column (adds it as the innermost group level) by setting
        /// its <see cref="GridColumn.GroupIndex"/> through <see cref="SearchDataGrid.GroupBy(GridColumn)"/>.
        /// </summary>
        public static ICommand GroupByColumnCommand => _groupByColumnCommand ??=
            new RelayCommand<ContextMenuContext>(
                context =>
                {
                    try
                    {
                        var descriptor = ResolveDescriptor(context);
                        if (descriptor != null) context.Grid.GroupBy(descriptor);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in GroupByColumnCommand: {ex.Message}");
                    }
                },
                CanGroupByColumn);

        private static ICommand _ungroupColumnCommand;
        /// <summary>
        /// Removes the context column from the grid's grouping via
        /// <see cref="SearchDataGrid.Ungroup(GridColumn)"/>.
        /// </summary>
        public static ICommand UngroupColumnCommand => _ungroupColumnCommand ??=
            new RelayCommand<ContextMenuContext>(
                context =>
                {
                    try
                    {
                        var descriptor = ResolveDescriptor(context);
                        if (descriptor != null) context.Grid.Ungroup(descriptor);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in UngroupColumnCommand: {ex.Message}");
                    }
                },
                context => context?.Grid != null && (ResolveDescriptor(context)?.IsGrouped ?? false));

        private static ICommand _clearGroupingCommand;
        /// <summary>
        /// Clears the grid's entire grouping via <see cref="SearchDataGrid.ClearGrouping"/>.
        /// </summary>
        public static ICommand ClearGroupingCommand => _clearGroupingCommand ??=
            new RelayCommand<ContextMenuContext>(
                context =>
                {
                    try
                    {
                        context.Grid?.ClearGrouping();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ClearGroupingCommand: {ex.Message}");
                    }
                },
                context => context?.Grid != null && context.Grid.GroupCount > 0);

        /// <summary>
        /// Enables "Group By This Column" when the grid has a descriptor for the context column,
        /// the column is not already grouped, and it has a resolvable group path.
        /// </summary>
        private static bool CanGroupByColumn(ContextMenuContext context)
        {
            if (context?.Grid == null) return false;
            var descriptor = ResolveDescriptor(context);
            if (descriptor == null || descriptor.IsGrouped) return false;
            if (!descriptor.ActualAllowGrouping) return false;
            return !string.IsNullOrEmpty(descriptor.ResolveGroupPath());
        }

        #endregion
    }
}
