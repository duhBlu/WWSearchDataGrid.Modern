using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One node in the flat-grouping group tree. Outer levels (lower <see cref="Level"/>) hold
    /// <see cref="Children"/>; the innermost level holds leaf <see cref="Items"/> (the raw user
    /// rows). <see cref="IsExpanded"/> decides whether this node's descendants/items appear in
    /// the flattened projection. Rebuilt from scratch on every projection pass; expansion state
    /// survives via the grid's path-keyed expand map, not this instance.
    /// </summary>
    internal sealed class GroupNode
    {
        /// <summary>Zero-based nesting depth (matches <see cref="GridColumn.GroupLevel"/>).</summary>
        public int Level { get; init; }

        /// <summary>
        /// Stable key for expansion-state persistence — the U+001F-joined display-value path from
        /// the root to this node. Survives reflattening so a toggled group keeps its state.
        /// </summary>
        public string PathKey { get; init; } = string.Empty;

        /// <summary>The group's value (the key the rows bucket on), shown in the header.</summary>
        public object DisplayValue { get; init; }

        /// <summary>The descriptor that owns this group level (for header templates / count).</summary>
        public GridColumn OwningColumn { get; init; }

        public bool IsExpanded { get; set; }

        /// <summary>Child group nodes when this is not the innermost level; otherwise empty.</summary>
        public List<GroupNode> Children { get; } = new();

        /// <summary>Leaf user rows when this IS the innermost level; otherwise empty.</summary>
        public List<object> Items { get; } = new();

        /// <summary>Total leaf rows beneath this node (recursive) — the header count chip.</summary>
        public int Count { get; set; }

        /// <summary>
        /// Left-side group-summary run for this node's leaf rows (entries with
        /// <see cref="SummaryItemAlignment.Left"/>, rendered inline after the header content).
        /// Computed at projection time and recomputed in place by
        /// <see cref="SearchDataGrid.RefreshGroupSummaryTexts"/> (cell edit commits, definition
        /// edits); null when nothing is configured for the left side.
        /// </summary>
        public string SummaryLeftText { get; set; }

        /// <summary>
        /// Right-side group-summary run (entries with <see cref="SummaryItemAlignment.Right"/>,
        /// the default — rendered right-aligned at the header row's right edge). Includes the
        /// opt-in <see cref="SearchDataGrid.ShowGroupRowCount"/> entry. Null when empty.
        /// </summary>
        public string SummaryRightText { get; set; }

        /// <summary>
        /// Structured per-entry results behind <see cref="SummaryLeftText"/> — the data the header
        /// renders as styled per-segment runs. Null when the left side is empty.
        /// </summary>
        public IReadOnlyList<SummaryResult> SummaryLeftInfo { get; set; }

        /// <summary>
        /// Structured per-entry results behind <see cref="SummaryRightText"/> — the data the header
        /// renders as styled per-segment runs. Null when the right side is empty.
        /// </summary>
        public IReadOnlyList<SummaryResult> SummaryRightInfo { get; set; }

        /// <summary>
        /// Column-aligned group-summary results for this node's header row, keyed by the target
        /// column descriptor — populated only while
        /// <see cref="SearchDataGrid.GroupSummaryDisplayMode"/> is
        /// <see cref="GroupSummaryDisplayMode.AlignByColumns"/> (header mode leaves it null).
        /// Recomputed in place alongside the header runs.
        /// </summary>
        public Dictionary<GridColumn, List<SummaryResult>> AlignedSummaryResults { get; set; }

        /// <summary>
        /// Per-column footer results for this node's group footer row, keyed by the column
        /// descriptor — each column's <see cref="GridColumn.GroupFooterSummaries"/> computed over
        /// this group's leaf rows. Populated only while any column defines footer summaries;
        /// recomputed in place alongside the header summaries (cell edit commits / definition
        /// edits). Null when the feature carries no content.
        /// </summary>
        public Dictionary<GridColumn, List<SummaryResult>> FooterSummaryResults { get; set; }
    }

    /// <summary>
    /// A provider of column-aligned group-summary results
    /// (<see cref="SearchDataGrid.GroupSummaryDisplayMode"/> = AlignByColumns), read by
    /// <see cref="GroupSummaryCell"/>. Implemented by both header surfaces — the in-body
    /// <see cref="GroupHeaderRow"/> sentinel and the pinned strip's
    /// <see cref="FixedGroupHeaderEntry"/> — so the same cell renders either. Change
    /// notification rides the implementer's <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    internal interface IAlignedGroupSummarySource : INotifyPropertyChanged
    {
        IReadOnlyList<SummaryResult> GetAlignedResultsFor(GridColumn descriptor);
    }

    /// <summary>
    /// A provider of per-column group-footer results, read by <see cref="GroupFooterCell"/>.
    /// Implemented by the <see cref="GroupFooterRow"/> sentinel. Change notification rides the
    /// implementer's <see cref="INotifyPropertyChanged"/>, so an in-place footer recompute reaches
    /// an already-rendered footer cell without a reflatten.
    /// </summary>
    internal interface IGroupFooterSummarySource : INotifyPropertyChanged
    {
        IReadOnlyList<SummaryResult> GetFooterResultsFor(GridColumn descriptor);
    }

    /// <summary>
    /// A group-header sentinel placed in the flat row list alongside raw user rows. The grid's
    /// row-style selector renders these as full-width header rows (no cells); the
    /// <see cref="DataGrid.ItemsSource"/> is a mix of these and the user's own items, so a row is
    /// a header iff it is a <see cref="GroupHeaderRow"/>.
    /// </summary>
    public sealed class GroupHeaderRow : INotifyPropertyChanged, IAlignedGroupSummarySource
    {
        internal GroupHeaderRow(GroupNode node)
        {
            Node = node;
            Level = node.Level;
            DisplayValue = node.DisplayValue;
            Count = node.Count;
            IsExpanded = node.IsExpanded;
            OwningColumn = node.OwningColumn;
        }

        internal GroupNode Node { get; }

        /// <summary>Zero-based nesting depth — drives the header's left indent.</summary>
        public int Level { get; }

        /// <summary>The group value rendered in the header.</summary>
        public object DisplayValue { get; }

        /// <summary>Leaf-row count under this group.</summary>
        public int Count { get; }

        /// <summary>
        /// The node's expansion state (chevron direction). Captured at projection time; on an
        /// animated splice toggle (which keeps this sentinel alive instead of reflattening) it is
        /// updated in place via <see cref="SetIsExpanded"/> with change notification, so the
        /// chevron and the context menu's expand/collapse gating track the toggle without a
        /// container swap.
        /// </summary>
        public bool IsExpanded { get; private set; }

        /// <summary>The grouped column that owns this header level (for value/header templates).</summary>
        public GridColumn OwningColumn { get; }

        /// <summary>
        /// Convenience accessor aliasing <see cref="DisplayValue"/>, so the shared group-header
        /// templates (which bind <c>Name</c> against a <see cref="System.Windows.Data.CollectionViewGroup"/>
        /// or a <see cref="FixedGroupHeaderEntry"/>) render a flat header row identically without a
        /// flat-specific template.
        /// </summary>
        public object Name => DisplayValue;

        /// <summary>
        /// Convenience accessor aliasing <see cref="Count"/>, so the shared default header chrome
        /// (which binds <c>ItemCount</c>) renders the count chip on a flat header row unchanged.
        /// </summary>
        public int ItemCount => Count;

        /// <summary>
        /// Left-side group-summary run for this group — read through the live
        /// <see cref="GroupNode.SummaryLeftText"/> (not a snapshot) so an in-place recompute
        /// reaches an already-rendered header via <see cref="NotifySummaryTextsChanged"/>.
        /// Rendered inline after the header content; null when empty (the chrome collapses
        /// the slot).
        /// </summary>
        public string GroupSummaryLeftText => Node?.SummaryLeftText;

        /// <summary>
        /// Right-side group-summary run (reads <see cref="GroupNode.SummaryRightText"/> live) —
        /// rendered right-aligned at the header row's right edge. Null when empty.
        /// </summary>
        public string GroupSummaryRightText => Node?.SummaryRightText;

        /// <summary>
        /// Structured left-side run (reads <see cref="GroupNode.SummaryLeftInfo"/> live) — the data
        /// the header renders as styled per-segment runs. Null when empty.
        /// </summary>
        public IReadOnlyList<SummaryResult> GroupSummaryLeftInfo => Node?.SummaryLeftInfo;

        /// <summary>
        /// Structured right-side run (reads <see cref="GroupNode.SummaryRightInfo"/> live) — the
        /// data the header renders as styled per-segment runs. Null when empty.
        /// </summary>
        public IReadOnlyList<SummaryResult> GroupSummaryRightInfo => Node?.SummaryRightInfo;

        /// <summary>
        /// The computed summary results targeting <paramref name="descriptor"/>'s column for
        /// this group, rendered aligned under the column in the header row (AlignByColumns
        /// mode). Reads through the live <see cref="GroupNode.AlignedSummaryResults"/> (not a
        /// snapshot) so an in-place recompute reaches an already-rendered header via
        /// <see cref="NotifySummaryTextsChanged"/>. Null when the column carries no entries.
        /// </summary>
        IReadOnlyList<SummaryResult> IAlignedGroupSummarySource.GetAlignedResultsFor(GridColumn descriptor)
        {
            if (descriptor == null) return null;
            var results = Node?.AlignedSummaryResults;
            return results != null && results.TryGetValue(descriptor, out var list) ? list : null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Flips the sentinel's expansion state in place during an animated splice toggle. No-op
        /// when the value is unchanged.
        /// </summary>
        internal void SetIsExpanded(bool value)
        {
            if (IsExpanded == value) return;
            IsExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }

        /// <summary>
        /// Raises change notification for both summary runs after the grid recomputes the node's
        /// texts in place, so the rendered header refreshes without a reflatten.
        /// </summary>
        internal void NotifySummaryTextsChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryLeftText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryRightText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryLeftInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryRightInfo)));
        }
    }

    /// <summary>
    /// A group-footer sentinel placed in the flat row list after a group's content (expanded) or
    /// directly beneath its header (collapsed). The grid's row-style selector renders these as a
    /// full-width footer row of column-aligned <see cref="GroupFooterCell"/>s — a per-group
    /// counterpart to the grid's total summary row. Emitted only while some column defines
    /// <see cref="GridColumn.GroupFooterSummaries"/>; a row is a footer iff it is a
    /// <see cref="GroupFooterRow"/>.
    /// </summary>
    public sealed class GroupFooterRow : INotifyPropertyChanged, IGroupFooterSummarySource
    {
        internal GroupFooterRow(GroupNode node)
        {
            Node = node;
            Level = node.Level;
        }

        internal GroupNode Node { get; }

        /// <summary>Zero-based nesting depth of the group this footer closes — drives the left indent.</summary>
        public int Level { get; }

        /// <summary>
        /// The footer results targeting <paramref name="descriptor"/>'s column for this group,
        /// read through the live <see cref="GroupNode.FooterSummaryResults"/> (not a snapshot) so
        /// an in-place recompute reaches an already-rendered footer cell via
        /// <see cref="NotifyFooterResultsChanged"/>. Null when the column carries no footer entries.
        /// </summary>
        IReadOnlyList<SummaryResult> IGroupFooterSummarySource.GetFooterResultsFor(GridColumn descriptor)
        {
            if (descriptor == null) return null;
            var results = Node?.FooterSummaryResults;
            return results != null && results.TryGetValue(descriptor, out var list) ? list : null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises change notification after the grid recomputes this group's footer results in
        /// place, so the rendered footer cells refresh without a reflatten.
        /// </summary>
        internal void NotifyFooterResultsChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IGroupFooterSummarySource.GetFooterResultsFor)));
    }

    /// <summary>
    /// <see cref="ObservableCollection{T}"/> with a single-notification bulk reset, so reflattening
    /// the whole projection raises one <see cref="NotifyCollectionChangedAction.Reset"/> instead of
    /// O(n) add/remove events (which would make every toggle/filter janky on large grids).
    /// </summary>
    internal sealed class GroupRowCollection : ObservableCollection<object>
    {
        public void ResetWith(IList<object> newItems)
        {
            Items.Clear();
            if (newItems != null)
            {
                foreach (var item in newItems)
                    Items.Add(item);
            }
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
