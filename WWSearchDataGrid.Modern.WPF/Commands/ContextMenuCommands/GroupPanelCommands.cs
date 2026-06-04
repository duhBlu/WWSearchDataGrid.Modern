using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        #region Group Panel Commands

        // The group panel surfaces three new families of action:
        //   · pill (per grouped column): toggle sort, full expand / collapse at this column's level
        //   · panel (global): expand all, collapse all, clear grouping (clear is already defined
        //     in GroupingCommands.cs; reused via the column-header menu's ClearGroupingCommand)
        //   · column-header (grid-level): toggle the group panel visibility itself
        // All pill commands take a GridColumn directly (the pill's DataContext); panel commands
        // take SearchDataGrid.

        private static ICommand _toggleGroupSortCommand;
        /// <summary>
        /// Flips the grouped column's sort direction (Ascending ↔ Descending). Grouping leads
        /// sorting (D2), so a grouped column is always sorted in one direction — there is no
        /// "unsorted" state to cycle through. Mutates the matching front-of-list
        /// <see cref="SortDescription"/> in place and pushes the new direction onto the
        /// generated <see cref="DataGridColumn.SortDirection"/> so the column-header arrow follows.
        /// </summary>
        public static ICommand ToggleGroupSortCommand => _toggleGroupSortCommand ??=
            new RelayCommand<GridColumn>(
                column =>
                {
                    try
                    {
                        if (column?.View == null || !column.IsGrouped) return;
                        var grid = column.View;
                        var path = column.ResolveGroupPath();
                        if (string.IsNullOrEmpty(path)) return;

                        var sorts = grid.Items?.SortDescriptions;
                        if (sorts == null) return;

                        for (int i = 0; i < sorts.Count; i++)
                        {
                            if (sorts[i].PropertyName != path) continue;

                            var next = sorts[i].Direction == ListSortDirection.Ascending
                                ? ListSortDirection.Descending
                                : ListSortDirection.Ascending;
                            // Remove + Insert (instead of indexer-replace) so the
                            // CollectionView observes Remove + Add events and definitely
                            // re-sorts. Indexer replace fires a Replace event whose handling
                            // varies across collection-view implementations.
                            sorts.RemoveAt(i);
                            sorts.Insert(i, new SortDescription(path, next));
                            if (column.InternalColumn != null)
                                column.InternalColumn.SortDirection = next;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ToggleGroupSortCommand: {ex.Message}");
                    }
                },
                column => column is { IsGrouped: true, View: not null });

        private static ICommand _fullExpandGroupCommand;
        /// <summary>
        /// Expands every realized group at this column's <see cref="GridColumn.GroupLevel"/>.
        /// Mirrors <see cref="ExpandAllAtLevelCommand"/> but parameterized by column rather than
        /// originating Expander — the entry point used by the group-panel pill.
        /// </summary>
        public static ICommand FullExpandGroupCommand => _fullExpandGroupCommand ??=
            new RelayCommand<GridColumn>(
                column => ApplyAtColumnLevel(column, expanded: true),
                column => column is { IsGrouped: true, View: not null });

        private static ICommand _fullCollapseGroupCommand;
        /// <summary>Collapses every realized group at this column's <see cref="GridColumn.GroupLevel"/>.</summary>
        public static ICommand FullCollapseGroupCommand => _fullCollapseGroupCommand ??=
            new RelayCommand<GridColumn>(
                column => ApplyAtColumnLevel(column, expanded: false),
                column => column is { IsGrouped: true, View: not null });

        private static ICommand _expandAllGroupsCommand;
        /// <summary>Grid-wide expand. Wraps <see cref="SearchDataGrid.ExpandAllGroups"/>.</summary>
        public static ICommand ExpandAllGroupsCommand => _expandAllGroupsCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try { grid?.ExpandAllGroups(); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ExpandAllGroupsCommand: {ex.Message}"); }
                },
                grid => grid != null && grid.GroupCount > 0);

        private static ICommand _collapseAllGroupsCommand;
        /// <summary>Grid-wide collapse. Wraps <see cref="SearchDataGrid.CollapseAllGroups"/>.</summary>
        public static ICommand CollapseAllGroupsCommand => _collapseAllGroupsCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try { grid?.CollapseAllGroups(); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CollapseAllGroupsCommand: {ex.Message}"); }
                },
                grid => grid != null && grid.GroupCount > 0);

        private static ICommand _clearAllGroupingCommand;
        /// <summary>
        /// Grid-wide ungroup. Sibling of the existing
        /// <see cref="ContextMenuCommands.ClearGroupingCommand"/> (which takes
        /// <see cref="ContextMenuContext"/>); this overload takes <see cref="SearchDataGrid"/>
        /// directly so the group-panel context menu can bind via <c>OwnerGrid</c> without
        /// detouring through the converter.
        /// </summary>
        public static ICommand ClearAllGroupingCommand => _clearAllGroupingCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try { grid?.ClearGrouping(); }
                    catch (Exception ex) { Debug.WriteLine($"Error in ClearAllGroupingCommand: {ex.Message}"); }
                },
                grid => grid != null && grid.GroupCount > 0);

        private static ICommand _showFilterEditorForColumnCommand;
        /// <summary>
        /// Opens the column's filter popup. Pill-side entry point: resolves the
        /// <see cref="IColumnFilterHost"/> for the grouped column from its grid's
        /// <see cref="SearchDataGrid.DataColumns"/> registry and calls
        /// <see cref="IColumnFilterHost.ShowFilterEditor"/>. Mirrors the column-header filter
        /// button's behavior without going through the existing
        /// <see cref="ShowFilterEditorCommand"/> (which expects a <c>DataGridColumnHeader</c> or
        /// <c>IColumnFilterHost</c> as its parameter).
        /// </summary>
        public static ICommand ShowFilterEditorForColumnCommand => _showFilterEditorForColumnCommand ??=
            new RelayCommand<GridColumn>(
                column =>
                {
                    try
                    {
                        var host = column?.View?.DataColumns?
                            .FirstOrDefault(h => h != null && (h.GridColumn == column || h.CurrentColumn == column.InternalColumn));
                        host?.ShowFilterEditor();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in ShowFilterEditorForColumnCommand: {ex.Message}");
                    }
                },
                column => column?.View != null);

        private static ICommand _toggleGroupPanelVisibilityCommand;
        /// <summary>
        /// Flips <see cref="SearchDataGrid.IsGroupPanelVisible"/>. Backs the "Show / Hide Group
        /// Panel" column-header menu item; the label switches based on the current value via a
        /// theme-side DataTrigger so a single MenuItem covers both directions.
        /// </summary>
        public static ICommand ToggleGroupPanelVisibilityCommand => _toggleGroupPanelVisibilityCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    if (grid != null) grid.IsGroupPanelVisible = !grid.IsGroupPanelVisible;
                },
                grid => grid != null);

        /// <summary>
        /// Shared body for the pill's Full Expand / Full Collapse commands. Walks every realized
        /// <see cref="GroupItem"/> in the grid whose nesting depth matches the column's
        /// <see cref="GridColumn.GroupLevel"/>, and toggles its Expander.
        /// </summary>
        private static void ApplyAtColumnLevel(GridColumn column, bool expanded)
        {
            try
            {
                if (column?.View == null || !column.IsGrouped) return;
                int targetLevel = column.GroupLevel;
                var grid = column.View;

                foreach (var groupItem in EnumerateGroupItemDescendants(grid))
                {
                    int depth = GetGroupItemDepth(groupItem);
                    if (depth != targetLevel) continue;
                    var expander = VisualTreeHelperMethods.FindVisualDescendant<Expander>(groupItem);
                    if (expander != null && expander.IsExpanded != expanded)
                        expander.IsExpanded = expanded;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyAtColumnLevel: {ex.Message}");
            }
        }

        private static int GetGroupItemDepth(GroupItem groupItem)
        {
            int depth = -1;
            System.Windows.DependencyObject current = groupItem;
            while (current != null)
            {
                if (current is GroupItem) depth++;
                current = VisualTreeHelper.GetParent(current);
            }
            return depth;
        }

        private static System.Collections.Generic.IEnumerable<GroupItem> EnumerateGroupItemDescendants(
            System.Windows.DependencyObject root)
        {
            if (root == null) yield break;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is GroupItem gi) yield return gi;
                foreach (var nested in EnumerateGroupItemDescendants(child))
                    yield return nested;
            }
        }

        #endregion
    }
}
