using System.Linq;
using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Observable row-count surface. <see cref="OriginalItemsCount"/> already reports the unfiltered
    /// total, but it is a plain CLR property with no change notification, and the filtered total is
    /// otherwise only reachable as <c>Items.Count</c> — which is wrong while grouped, because the
    /// grid swaps its <c>Items</c> to a flat projection of header sentinels + (only expanded) rows.
    /// These two read-only DPs give consumers a bindable, grouping-aware pair: total leaf rows in the
    /// source, and leaf rows passing the active filter (independent of group collapse).
    /// </summary>
    public partial class SearchDataGrid
    {
        private static readonly DependencyPropertyKey TotalItemCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TotalItemCount), typeof(int), typeof(SearchDataGrid),
                new PropertyMetadata(0));

        /// <summary>Notifying mirror of <see cref="OriginalItemsCount"/> — the unfiltered source leaf count.</summary>
        public static readonly DependencyProperty TotalItemCountProperty = TotalItemCountPropertyKey.DependencyProperty;

        public int TotalItemCount
        {
            get => (int)GetValue(TotalItemCountProperty);
            private set => SetValue(TotalItemCountPropertyKey, value);
        }

        private static readonly DependencyPropertyKey FilteredItemCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(FilteredItemCount), typeof(int), typeof(SearchDataGrid),
                new PropertyMetadata(0));

        /// <summary>Data rows passing the active filter. Excludes group-header sentinels and is
        /// independent of which groups are collapsed; equals <see cref="TotalItemCount"/> when no
        /// filter is active.</summary>
        public static readonly DependencyProperty FilteredItemCountProperty = FilteredItemCountPropertyKey.DependencyProperty;

        public int FilteredItemCount
        {
            get => (int)GetValue(FilteredItemCountProperty);
            private set => SetValue(FilteredItemCountPropertyKey, value);
        }

        /// <summary>Pushes the live source count onto the notifying <see cref="TotalItemCount"/> DP.</summary>
        private void UpdateTotalItemCount()
        {
            int total = OriginalItemsCount;
            if (TotalItemCount != total) TotalItemCount = total;
        }

        /// <summary>
        /// Recomputes <see cref="FilteredItemCount"/>.
        /// <list type="bullet">
        /// <item>Grouped: sums the recursive leaf total of every root <c>GroupNode</c> — collapse-
        /// independent, and never counts the header sentinels in the flat projection.</item>
        /// <item>Unfiltered: the source count.</item>
        /// <item>Filtered &amp; ungrouped: the filtered view count (O(1)), discounting the zero-height
        /// empty-state scroll anchor and the new-item placeholder row.</item>
        /// </list>
        /// </summary>
        private void UpdateFilteredItemCount()
        {
            int count;
            if (originalItemsSource == null)
            {
                count = 0;
            }
            else if (_groupingActive)
            {
                count = _groupRoots?.Sum(n => n.Count) ?? 0;
            }
            else if (SearchFilter == null)
            {
                count = OriginalItemsCount;
            }
            else
            {
                count = Items.Count;
                if (_emptyStatePlaceholder != null) count--; // injected zero-height scroll anchor
                if (CanUserAddRows) count--;                 // trailing NewItemPlaceholder row
                if (count < 0) count = 0;
            }

            if (FilteredItemCount != count) FilteredItemCount = count;

            // Total summaries aggregate the same filtered row set this count reports — every
            // path that lands here (filter, source swap, projection rebuild, collection churn)
            // is a summary trigger too. Coalesced, so burst callers cost one recompute.
            ScheduleSummaryUpdate();
        }
    }
}
