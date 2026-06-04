using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        #region Group Header Commands

        // Every command in this region takes the originating group-header Expander as its
        // parameter. The default group context menu is attached to the Expander itself (via
        // GroupExpanderStyle) and binds CommandParameter to `PlacementTarget`, which WPF sets to
        // that Expander when the menu opens. From the Expander we walk to the GroupItem (for
        // depth / GroupLevel) and to the SearchDataGrid (for grouping API + per-level walks).

        private static ICommand _expandGroupCommand;
        /// <summary>
        /// Expands the single group whose header was right-clicked. Visibility is gated in XAML —
        /// the command is also <see cref="CanExecute"/>-gated so a re-bound menu doesn't fire the
        /// no-op on an already-expanded group.
        /// </summary>
        public static ICommand ExpandGroupCommand => _expandGroupCommand ??=
            new RelayCommand<Expander>(
                expander =>
                {
                    try
                    {
                        if (expander != null && !expander.IsExpanded)
                            expander.IsExpanded = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ExpandGroupCommand: {ex.Message}");
                    }
                },
                expander => expander is { IsExpanded: false });

        private static ICommand _collapseGroupCommand;
        /// <summary>Collapses the single group whose header was right-clicked.</summary>
        public static ICommand CollapseGroupCommand => _collapseGroupCommand ??=
            new RelayCommand<Expander>(
                expander =>
                {
                    try
                    {
                        if (expander != null && expander.IsExpanded)
                            expander.IsExpanded = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in CollapseGroupCommand: {ex.Message}");
                    }
                },
                expander => expander is { IsExpanded: true });

        private static ICommand _expandAllAtLevelCommand;
        /// <summary>
        /// Expands every realized group at the same nesting depth as the originating group.
        /// "Same depth" matches the DevExpress convention — every group at that level across the
        /// grid, not just the originating group's siblings. Virtualized groups inherit their
        /// captured state (see <see cref="SearchDataGrid.TrackGroupExpansionProperty"/>) when they
        /// realize next.
        /// </summary>
        public static ICommand ExpandAllAtLevelCommand => _expandAllAtLevelCommand ??=
            new RelayCommand<Expander>(
                expander => ApplyAtLevel(expander, expanded: true),
                expander => expander != null);

        private static ICommand _collapseAllAtLevelCommand;
        /// <summary>Collapses every realized group at the same nesting depth as the originating group.</summary>
        public static ICommand CollapseAllAtLevelCommand => _collapseAllAtLevelCommand ??=
            new RelayCommand<Expander>(
                expander => ApplyAtLevel(expander, expanded: false),
                expander => expander != null);

        private static ICommand _ungroupAtLevelCommand;
        /// <summary>
        /// Removes the column whose <see cref="GridColumn.GroupLevel"/> matches the originating
        /// group's depth from the grouping. Equivalent to clicking the column-header "Ungroup"
        /// menu item on whichever column is responsible for this nesting level.
        /// </summary>
        public static ICommand UngroupAtLevelCommand => _ungroupAtLevelCommand ??=
            new RelayCommand<Expander>(
                expander =>
                {
                    try
                    {
                        if (!TryResolve(expander, out var grid, out int level)) return;
                        var column = grid.GetGroupedColumnAtLevel(level);
                        if (column != null) grid.Ungroup(column);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in UngroupAtLevelCommand: {ex.Message}");
                    }
                },
                expander => TryResolve(expander, out var grid, out int level)
                            && grid.GetGroupedColumnAtLevel(level) != null);

        /// <summary>
        /// Shared body for <see cref="ExpandAllAtLevelCommand"/> /
        /// <see cref="CollapseAllAtLevelCommand"/>: walks every realized <see cref="GroupItem"/>
        /// in the grid and toggles its expander when the GroupItem's depth matches the originating
        /// group's depth.
        /// </summary>
        private static void ApplyAtLevel(Expander originating, bool expanded)
        {
            try
            {
                if (!TryResolve(originating, out var grid, out int level)) return;

                foreach (var groupItem in EnumerateDescendants<GroupItem>(grid))
                {
                    if (GetGroupDepth(groupItem) != level) continue;
                    var expander = VisualTreeHelperMethods.FindVisualDescendant<Expander>(groupItem);
                    if (expander != null && expander.IsExpanded != expanded)
                        expander.IsExpanded = expanded;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyAtLevel: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves the owning <see cref="SearchDataGrid"/> and the originating group's level
        /// (zero-based, matching <see cref="GridColumn.GroupLevel"/>). Returns false when the
        /// expander isn't inside a group / grid — defensive for menu items invoked outside the
        /// usual chrome path.
        /// </summary>
        private static bool TryResolve(Expander expander, out SearchDataGrid grid, out int level)
        {
            grid = null;
            level = -1;
            if (expander == null) return false;

            var groupItem = VisualTreeHelperMethods.FindVisualAncestor<GroupItem>(expander);
            if (groupItem == null) return false;

            grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(expander);
            if (grid == null) return false;

            level = GetGroupDepth(groupItem);
            return level >= 0;
        }

        /// <summary>
        /// Returns the zero-based nesting depth of <paramref name="groupItem"/> — the number of
        /// <see cref="GroupItem"/> ancestors above it. Matches <see cref="GridColumn.GroupLevel"/>
        /// when the grouping is settled.
        /// </summary>
        private static int GetGroupDepth(GroupItem groupItem)
        {
            int depth = -1;
            DependencyObject current = groupItem;
            while (current != null)
            {
                if (current is GroupItem) depth++;
                current = VisualTreeHelper.GetParent(current);
            }
            return depth;
        }

        /// <summary>Depth-first enumeration of visual descendants of type <typeparamref name="T"/>.</summary>
        private static System.Collections.Generic.IEnumerable<T> EnumerateDescendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null) yield break;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var descendant in EnumerateDescendants<T>(child))
                    yield return descendant;
            }
        }

        #endregion
    }
}
