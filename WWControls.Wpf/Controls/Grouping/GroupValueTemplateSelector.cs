using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Picks the <see cref="DataTemplate"/> that renders a group header's <em>value</em> for a
    /// grouped <see cref="SearchDataGrid"/>. The grid groups by one or more columns; this selector
    /// resolves, for a given group row, the template declared on the column that owns that nesting
    /// level (<see cref="GridColumn.GroupValueTemplate"/> /
    /// <see cref="GridColumn.GroupValueTemplateSelector"/>), falling back to
    /// <see cref="DefaultTemplate"/>.
    /// </summary>
    /// <remarks>
    /// A single instance lives in the theme and is shared by every grouped grid — it carries no
    /// per-grid state. The owning column is found by depth: the selector counts the
    /// <see cref="GroupItem"/> ancestors of the header container (the innermost is this group's),
    /// so <c>level = ancestorCount - 1</c>, then asks the grid for the column whose
    /// <see cref="GridColumn.GroupLevel"/> matches. The header's <c>DataContext</c> is the
    /// <see cref="System.Windows.Data.CollectionViewGroup"/> (so templates bind <c>Name</c> /
    /// <c>ItemCount</c>); resolved column-side state (e.g. <c>HeaderCaption</c>) is reachable via
    /// the grid when a template wants it.
    /// </remarks>
    public class GroupValueTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Fallback template used when the group's owning column declares neither a
        /// <see cref="GridColumn.GroupValueTemplate"/> nor a selector that returns one (or when the
        /// owning column can't be resolved). Set from the theme; typically renders
        /// <c>CollectionViewGroup.Name</c>.
        /// </summary>
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // The pinned strip (FixedGroupHeaderEntry) and the flat-grouping header rows
            // (GroupHeaderRow) both render outside the rows-presenter subtree, so there is no
            // GroupItem ancestor to count — each carries its owning column directly.
            GridColumn column = item switch
            {
                FixedGroupHeaderEntry entry => entry.Column,
                GroupHeaderRow header => header.OwningColumn,
                _ => ResolveOwningColumn(container),
            };
            if (column != null)
            {
                var bySelector = column.ActualGroupValueTemplateSelector?.SelectTemplate(item, container);
                if (bySelector != null) return bySelector;
                if (column.GroupValueTemplate != null) return column.GroupValueTemplate;
            }
            return DefaultTemplate;
        }

        /// <summary>
        /// Resolves the <see cref="GridColumn"/> that owns the nesting level of the group header
        /// being rendered, or <c>null</c> when the grid or level can't be determined.
        /// </summary>
        private static GridColumn ResolveOwningColumn(DependencyObject container)
        {
            var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(container);
            if (grid == null) return null;

            int level = CountGroupItemAncestors(container) - 1;
            if (level < 0) return null;

            return grid.GetGroupedColumnAtLevel(level);
        }

        /// <summary>
        /// Counts the <see cref="GroupItem"/> ancestors of <paramref name="start"/> (inclusive of the
        /// immediate one). Each grouping level nests one more <see cref="GroupItem"/>, so the count
        /// maps directly to the rendered group's depth.
        /// </summary>
        private static int CountGroupItemAncestors(DependencyObject start)
        {
            int count = 0;
            var current = start;
            while (current != null)
            {
                if (current is GroupItem) count++;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return count;
        }
    }
}
