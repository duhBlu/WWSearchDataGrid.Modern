using System.ComponentModel;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// One entry in the "Sort By Summary" context-menu listing — an Ascending or Descending
    /// option per configured group-summary aggregate (e.g. <c>Sum by 'Total' - Ascending</c>),
    /// or the trailing Clear Summary Sort action. Built per menu open by
    /// <see cref="SearchDataGrid.BuildGroupSummarySortOptions"/> and executed by
    /// <c>ContextMenuCommands.SortGroupsBySummaryCommand</c>.
    /// </summary>
    public sealed class GroupSummarySortOption
    {
        internal GroupSummarySortOption(
            SearchDataGrid grid,
            string header,
            bool isChecked,
            bool isClear,
            SummaryItemType summaryType,
            string fieldName,
            ListSortDirection direction)
        {
            Grid = grid;
            Header = header;
            IsChecked = isChecked;
            IsClear = isClear;
            SummaryType = summaryType;
            FieldName = fieldName;
            Direction = direction;
        }

        internal SearchDataGrid Grid { get; }

        /// <summary>The menu item text.</summary>
        public string Header { get; }

        /// <summary>True when this option is the active summary sort (rendered check-marked).</summary>
        public bool IsChecked { get; }

        /// <summary>True for the trailing Clear Summary Sort action.</summary>
        public bool IsClear { get; }

        internal SummaryItemType SummaryType { get; }

        internal string FieldName { get; }

        internal ListSortDirection Direction { get; }
    }
}
