using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        #region Fixed-Group (Sticky Strip) Commands

        // Sibling set to GroupHeaderCommands, but every command takes a FixedGroupHeaderEntry
        // instead of an Expander. Pinned chrome lives in a separate visual subtree from the rows
        // presenter (the strip overlays the data area), so an Expander parameter can't reach a
        // GroupItem / grid via the ancestor walk GroupHeaderCommands uses — the entry carries
        // (Level, Group, Column, RepresentedGroupItem) directly. Every operation routes through
        // RepresentedGroupItem; from there we can find the real Expander to toggle, the level
        // (already on the entry), and the owning SearchDataGrid (via ancestor walk from the
        // GroupItem) for level-wide operations.

        private static ICommand _expandFixedGroupCommand;
        /// <summary>
        /// Expands the single group whose pinned header was right-clicked. Toggles the realized
        /// real <see cref="Expander"/> inside the entry's <see cref="FixedGroupHeaderEntry.RepresentedGroupItem"/>
        /// — that fires the existing class handler that captures into the persistence map and (if
        /// <c>ExpandGroupsRecursively</c>) cascades.
        /// </summary>
        public static ICommand ExpandFixedGroupCommand => _expandFixedGroupCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try
                    {
                        var expander = FindRepresentedExpander(entry);
                        if (expander != null && !expander.IsExpanded)
                        {
                            expander.IsExpanded = true;
                            RefreshStrip(entry);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ExpandFixedGroupCommand: {ex.Message}");
                    }
                },
                entry => FindRepresentedExpander(entry) is { IsExpanded: false });

        private static ICommand _collapseFixedGroupCommand;
        /// <summary>Collapses the single group whose pinned header was right-clicked.</summary>
        public static ICommand CollapseFixedGroupCommand => _collapseFixedGroupCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try
                    {
                        var expander = FindRepresentedExpander(entry);
                        if (expander != null && expander.IsExpanded)
                        {
                            expander.IsExpanded = false;
                            RefreshStrip(entry);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in CollapseFixedGroupCommand: {ex.Message}");
                    }
                },
                entry => FindRepresentedExpander(entry) is { IsExpanded: true });

        private static ICommand _toggleFixedGroupCommand;
        /// <summary>
        /// Toggles the represented group's expanded state. Bound to the pinned chrome's
        /// <see cref="System.Windows.Controls.Primitives.ToggleButton"/> click so the user
        /// interaction goes through the same path as the right-click Expand/Collapse menu items.
        /// </summary>
        public static ICommand ToggleFixedGroupCommand => _toggleFixedGroupCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try
                    {
                        var expander = FindRepresentedExpander(entry);
                        if (expander != null)
                        {
                            expander.IsExpanded = !expander.IsExpanded;
                            RefreshStrip(entry);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ToggleFixedGroupCommand: {ex.Message}");
                    }
                },
                entry => FindRepresentedExpander(entry) != null);

        private static ICommand _expandAllAtFixedLevelCommand;
        /// <summary>
        /// Expands every realized group at the same nesting depth as the originating pinned entry.
        /// Walks <see cref="EnumerateDescendants{T}"/> over the owning grid; virtualized groups
        /// inherit their captured state on next realization.
        /// </summary>
        public static ICommand ExpandAllAtFixedLevelCommand => _expandAllAtFixedLevelCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry => ApplyAtFixedLevel(entry, expanded: true),
                entry => entry?.RepresentedGroupItem != null);

        private static ICommand _collapseAllAtFixedLevelCommand;
        /// <summary>Collapses every realized group at the same nesting depth as the originating pinned entry.</summary>
        public static ICommand CollapseAllAtFixedLevelCommand => _collapseAllAtFixedLevelCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry => ApplyAtFixedLevel(entry, expanded: false),
                entry => entry?.RepresentedGroupItem != null);

        private static ICommand _ungroupAtFixedLevelCommand;
        /// <summary>
        /// Removes the column at <see cref="FixedGroupHeaderEntry.Level"/> from the grouping.
        /// Resolves the grid via ancestor walk from the entry's
        /// <see cref="FixedGroupHeaderEntry.RepresentedGroupItem"/>.
        /// </summary>
        public static ICommand UngroupAtFixedLevelCommand => _ungroupAtFixedLevelCommand ??=
            new RelayCommand<FixedGroupHeaderEntry>(
                entry =>
                {
                    try
                    {
                        var grid = FindOwnerGrid(entry);
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
                    var grid = FindOwnerGrid(entry);
                    return grid != null && grid.GetGroupedColumnAtLevel(entry.Level) != null;
                });

        /// <summary>
        /// Walks every realized <see cref="GroupItem"/> in the owning grid and toggles its
        /// expander when the depth matches the originating entry's <see cref="FixedGroupHeaderEntry.Level"/>.
        /// </summary>
        private static void ApplyAtFixedLevel(FixedGroupHeaderEntry entry, bool expanded)
        {
            try
            {
                var grid = FindOwnerGrid(entry);
                if (grid == null) return;
                int level = entry.Level;

                foreach (var groupItem in EnumerateDescendants<GroupItem>(grid))
                {
                    if (GetGroupDepth(groupItem) != level) continue;
                    var expander = VisualTreeHelperMethods.FindVisualDescendant<Expander>(groupItem);
                    if (expander != null && expander.IsExpanded != expanded)
                        expander.IsExpanded = expanded;
                }

                RefreshStrip(entry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyAtFixedLevel: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns the <see cref="Expander"/> hosted inside the entry's realized
        /// <see cref="System.Windows.Controls.GroupItem"/>, or <c>null</c> when the group is not
        /// currently realized in the visual tree.
        /// </summary>
        private static Expander FindRepresentedExpander(FixedGroupHeaderEntry entry)
        {
            if (entry?.RepresentedGroupItem == null) return null;
            return VisualTreeHelperMethods.FindVisualDescendant<Expander>(entry.RepresentedGroupItem);
        }

        /// <summary>
        /// Walks up from the entry's <see cref="System.Windows.Controls.GroupItem"/> to find the
        /// owning <see cref="SearchDataGrid"/>. Used by level-wide operations that need access to
        /// the grouping API.
        /// </summary>
        private static SearchDataGrid FindOwnerGrid(FixedGroupHeaderEntry entry)
        {
            if (entry?.RepresentedGroupItem == null) return null;
            return VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(entry.RepresentedGroupItem);
        }

        /// <summary>
        /// Asks the owning grid to re-resolve the active chain after the underlying expand state
        /// changed. New entries are constructed with fresh <see cref="FixedGroupHeaderEntry.RepresentedGroupItem"/>
        /// references; the pinned chrome's IsExpanded converter re-evaluates against the new state.
        /// </summary>
        private static void RefreshStrip(FixedGroupHeaderEntry entry)
        {
            FindOwnerGrid(entry)?.UpdateFixedGroupHeaders();
        }

        #endregion
    }
}
