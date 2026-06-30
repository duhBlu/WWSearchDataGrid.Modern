using System.ComponentModel;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// The active sort-groups-by-summary configuration — what
    /// <see cref="SearchDataGrid.SortGroupsBySummary"/> was last called with. Surfaced read-only
    /// via <see cref="SearchDataGrid.ActiveGroupSummarySort"/> (null when groups order by their
    /// key); a fresh instance per call, so bindings observe every change.
    /// </summary>
    public sealed class GroupSummarySortDescriptor
    {
        internal GroupSummarySortDescriptor(SummaryItemType summaryType, string fieldName, ListSortDirection direction)
        {
            SummaryType = summaryType;
            FieldName = fieldName;
            Direction = direction;
        }

        /// <summary>The aggregate function groups order by.</summary>
        public SummaryItemType SummaryType { get; }

        /// <summary>The aggregated field path; null with <see cref="SummaryItemType.Count"/> = group row count.</summary>
        public string FieldName { get; }

        /// <summary>Sort direction over the aggregate values.</summary>
        public ListSortDirection Direction { get; }

        /// <summary>True when <paramref name="other"/> describes the same sort.</summary>
        internal bool Matches(SummaryItemType summaryType, string fieldName, ListSortDirection direction)
            => SummaryType == summaryType
                && string.Equals(FieldName ?? string.Empty, fieldName ?? string.Empty, System.StringComparison.Ordinal)
                && Direction == direction;
    }
}
