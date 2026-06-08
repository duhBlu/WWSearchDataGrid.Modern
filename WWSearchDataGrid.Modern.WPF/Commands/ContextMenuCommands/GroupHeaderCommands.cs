using System;
using System.Diagnostics;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        #region In-Body Group Header Commands

        // The flat-grouping engine renders its in-body group headers as GroupHeaderRow sentinels in
        // a DataGridRow template — there is no GroupItem/Expander — so the legacy GroupHeaderCommands
        // (which take an Expander) can't drive them. These mirror that menu but take the
        // GroupHeaderRow directly (the row template's DataContext) and route through the grid's flat
        // expansion / grouping API. The owning grid is reached via the header's owning column
        // (GridColumn.View); expand-state reads use the row's snapshot, which is fresh because the
        // sentinel is rebuilt on every reflatten and the menu opens against the current instance.

        private static SearchDataGrid GridOf(GroupHeaderRow row) => row?.OwningColumn?.View;

        private static ICommand _expandGroupCommand;
        /// <summary>Expands the single flat group whose in-body header was right-clicked.</summary>
        public static ICommand ExpandGroupCommand => _expandGroupCommand ??=
            new RelayCommand<GroupHeaderRow>(
                row =>
                {
                    try { GridOf(row)?.SetGroupExpanded(row.Node, true); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ExpandGroupCommand: {ex.Message}"); }
                },
                row => row?.Node is { IsExpanded: false } && GridOf(row) != null);

        private static ICommand _collapseGroupCommand;
        /// <summary>Collapses the single flat group whose in-body header was right-clicked.</summary>
        public static ICommand CollapseGroupCommand => _collapseGroupCommand ??=
            new RelayCommand<GroupHeaderRow>(
                row =>
                {
                    try { GridOf(row)?.SetGroupExpanded(row.Node, false); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CollapseGroupCommand: {ex.Message}"); }
                },
                row => row?.Node is { IsExpanded: true } && GridOf(row) != null);

        private static ICommand _expandAllAtLevelCommand;
        /// <summary>Expands every flat group at the same nesting depth as the originating header row.</summary>
        public static ICommand ExpandAllAtLevelCommand => _expandAllAtLevelCommand ??=
            new RelayCommand<GroupHeaderRow>(
                row =>
                {
                    try { GridOf(row)?.SetLevelExpanded(row.Level, true); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ExpandAllAtLevelCommand: {ex.Message}"); }
                },
                row => GridOf(row) != null);

        private static ICommand _collapseAllAtLevelCommand;
        /// <summary>Collapses every flat group at the same nesting depth as the originating header row.</summary>
        public static ICommand CollapseAllAtLevelCommand => _collapseAllAtLevelCommand ??=
            new RelayCommand<GroupHeaderRow>(
                row =>
                {
                    try { GridOf(row)?.SetLevelExpanded(row.Level, false); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CollapseAllAtLevelCommand: {ex.Message}"); }
                },
                row => GridOf(row) != null);

        private static ICommand _ungroupAtLevelCommand;
        /// <summary>Removes the column at the originating header row's level from the grouping.</summary>
        public static ICommand UngroupAtLevelCommand => _ungroupAtLevelCommand ??=
            new RelayCommand<GroupHeaderRow>(
                row =>
                {
                    try
                    {
                        var grid = GridOf(row);
                        if (grid == null) return;
                        var column = grid.GetGroupedColumnAtLevel(row.Level);
                        if (column != null) grid.Ungroup(column);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in UngroupAtLevelCommand: {ex.Message}");
                    }
                },
                row =>
                {
                    var grid = GridOf(row);
                    return grid != null && grid.GetGroupedColumnAtLevel(row.Level) != null;
                });

        #endregion
    }
}
