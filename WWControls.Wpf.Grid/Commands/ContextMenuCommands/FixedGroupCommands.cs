using System;
using System.Diagnostics;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        #region Fixed-Group (Sticky Strip) Commands

        // Sibling set to the in-body group-header commands, but every command takes a
        // FixedGroupHeaderEntry — the pinned chrome lives in a separate visual subtree from the rows
        // presenter (the strip overlays the data area). The entry carries (Level, Node, Column,
        // OwnerGrid); every operation routes through OwnerGrid + Node into the grid's path-keyed
        // expand state / grouping API.

        private static SearchDataGrid GridOf(FixedGroupHeaderEntry entry) => entry?.OwnerGrid;

        private static bool IsExpanded(FixedGroupHeaderEntry entry)
            => entry?.OwnerGrid?.GetGroupExpandedByPath(entry.Node.PathKey) ?? true;

        private static ICommand _expandFixedGroupCommand;
        /// <summary>Expands the single group whose pinned header was right-clicked.</summary>
        public static ICommand ExpandFixedGroupCommand => _expandFixedGroupCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try { GridOf(entry)?.SetGroupExpanded(entry.Node, true); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ExpandFixedGroupCommand: {ex.Message}"); }
                },
                entry => GridOf(entry) != null && !IsExpanded(entry));

        private static ICommand _collapseFixedGroupCommand;
        /// <summary>Collapses the single group whose pinned header was right-clicked.</summary>
        public static ICommand CollapseFixedGroupCommand => _collapseFixedGroupCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try { GridOf(entry)?.SetGroupExpanded(entry.Node, false); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CollapseFixedGroupCommand: {ex.Message}"); }
                },
                entry => GridOf(entry) != null && IsExpanded(entry));

        private static ICommand _toggleFixedGroupCommand;
        /// <summary>
        /// Toggles the represented group's expanded state. Bound to the pinned chrome's click so the
        /// interaction goes through the same path as the right-click Expand/Collapse menu items.
        /// </summary>
        public static ICommand ToggleFixedGroupCommand => _toggleFixedGroupCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try { GridOf(entry)?.SetGroupExpanded(entry.Node, !IsExpanded(entry)); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ToggleFixedGroupCommand: {ex.Message}"); }
                },
                entry => GridOf(entry) != null);

        private static ICommand _expandAllAtFixedLevelCommand;
        /// <summary>Expands every group at the same nesting depth as the originating pinned entry.</summary>
        public static ICommand ExpandAllAtFixedLevelCommand => _expandAllAtFixedLevelCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try { GridOf(entry)?.SetLevelExpanded(entry.Level, true); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ExpandAllAtFixedLevelCommand: {ex.Message}"); }
                },
                entry => GridOf(entry) != null);

        private static ICommand _collapseAllAtFixedLevelCommand;
        /// <summary>Collapses every group at the same nesting depth as the originating pinned entry.</summary>
        public static ICommand CollapseAllAtFixedLevelCommand => _collapseAllAtFixedLevelCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try { GridOf(entry)?.SetLevelExpanded(entry.Level, false); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CollapseAllAtFixedLevelCommand: {ex.Message}"); }
                },
                entry => GridOf(entry) != null);

        private static ICommand _ungroupAtFixedLevelCommand;
        /// <summary>Removes the column at <see cref="FixedGroupHeaderEntry.Level"/> from the grouping.</summary>
        public static ICommand UngroupAtFixedLevelCommand => _ungroupAtFixedLevelCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try
                    {
                        var grid = GridOf(entry);
                        if (grid == null) return;
                        var column = grid.GetGroupedColumnAtLevel(entry.Level);
                        if (column != null) grid.Ungroup(column);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in UngroupAtFixedLevelCommand: {ex.Message}");
                    }
                },
                entry =>
                {
                    var grid = GridOf(entry);
                    return grid != null && grid.GetGroupedColumnAtLevel(entry.Level) != null;
                });

        #endregion
    }
}
