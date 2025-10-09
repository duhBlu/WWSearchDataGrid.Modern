using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    internal partial class ContextMenuCommands
    {
        /// <summary>
        /// Groups the DataGrid by the specified column
        /// </summary>
        public static ICommand GroupByColumnCommand => new RelayCommand<ContextMenuContext>(
            context =>
            {
                if (context?.Grid?.GroupPanel == null || context.Column == null)
                    return;

                var groupPanel = context.Grid.GroupPanel;

                // Check if already grouped by this column
                var existingGroup = groupPanel.GroupedColumns
                    .FirstOrDefault(g => g.Column == context.Column);

                if (existingGroup != null)
                {
                    Debug.WriteLine($"Column '{context.Column.Header}' is already grouped");
                    return;
                }

                // Create new GroupColumnInfo
                var groupInfo = new GroupColumnInfo
                {
                    Column = context.Column,
                    BindingPath = context.Column.SortMemberPath,
                    HeaderText = context.Column.Header?.ToString() ?? "Unknown",
                    GroupLevel = groupPanel.GroupedColumns.Count
                };

                // Add to group panel
                groupPanel.GroupedColumns.Add(groupInfo);

                Debug.WriteLine($"Grouped by column: {groupInfo.HeaderText}");
            },
            context => context?.Grid?.IsGroupingEnabled == true
                && context?.Column != null
                && !string.IsNullOrEmpty(context.Column.SortMemberPath));

        /// <summary>
        /// Toggles the visibility of the group panel
        /// </summary>
        public static ICommand ToggleGroupPanelVisibilityCommand => new RelayCommand<SearchDataGrid>(
            grid =>
            {
                if (grid?.GroupPanel == null) return;

                grid.GroupPanel.IsPanelVisible = !grid.GroupPanel.IsPanelVisible;
                Debug.WriteLine($"Group panel visibility: {grid.GroupPanel.IsPanelVisible}");
            },
            grid => grid?.IsGroupingEnabled == true);

        /// <summary>
        /// Expands all group levels in the DataGrid
        /// </summary>
        public static ICommand ExpandAllGroupsCommand => new RelayCommand<SearchDataGrid>(
            grid =>
            {
                grid?.GroupPanel?.ExpandAllGroups();
                Debug.WriteLine("Expanding all groups");
            },
            grid => grid?.IsGroupingEnabled == true
                && grid?.GroupedColumns?.Any() == true);

        /// <summary>
        /// Collapses all group levels in the DataGrid
        /// </summary>
        public static ICommand CollapseAllGroupsCommand => new RelayCommand<SearchDataGrid>(
            grid =>
            {
                grid?.GroupPanel?.CollapseAllGroups();
                Debug.WriteLine("Collapsing all groups");
            },
            grid => grid?.IsGroupingEnabled == true
                && grid?.GroupedColumns?.Any() == true);

        /// <summary>
        /// Clears all grouping from the DataGrid
        /// </summary>
        public static ICommand ClearGroupingCommand => new RelayCommand<SearchDataGrid>(
            grid =>
            {
                grid?.ClearGrouping();
                Debug.WriteLine("Cleared all grouping");
            },
            grid => grid?.IsGroupingEnabled == true
                && grid?.GroupedColumns?.Any() == true);

        /// <summary>
        /// Removes a specific column from grouping
        /// </summary>
        public static ICommand UngroupColumnCommand => new RelayCommand<ContextMenuContext>(
            context =>
            {
                if (context?.Grid?.GroupPanel == null || context.GroupColumnInfo == null)
                    return;

                context.Grid.GroupPanel.RemoveGrouping(context.GroupColumnInfo);
            },
            context => context?.GroupColumnInfo != null
                && context?.Grid?.IsGroupingEnabled == true);
    }
}
