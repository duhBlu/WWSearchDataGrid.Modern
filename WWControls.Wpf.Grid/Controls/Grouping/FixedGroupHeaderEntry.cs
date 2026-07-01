using System.Collections.Generic;
using System.ComponentModel;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One pinned-header view-model in the sticky strip rendered by
    /// <see cref="FixedGroupHeadersPresenter"/>. Carries everything the per-item template needs to
    /// mirror the in-place group header at <see cref="Level"/>: the group's display
    /// <see cref="Name"/> / <see cref="ItemCount"/>, the owning <see cref="GridColumn"/> (for
    /// column-driven <c>GroupHeaderTemplate</c> / <c>GroupValueTemplate</c> resolution), and the
    /// routing handle (<see cref="Node"/> + <see cref="OwnerGrid"/>) for expand-state reads and
    /// toggle commands.
    /// </summary>
    /// <remarks>
    /// Entries are recomputed by the grid's active-chain resolver on every scroll change. They are
    /// value-shaped — two entries with the same <see cref="Level"/> and the same
    /// <see cref="GroupNode.PathKey"/> compare equal — so the resolver can skip no-op updates
    /// against the current collection cheaply. (The backing <see cref="GroupNode"/> is rebuilt on
    /// every reprojection, so the stable <see cref="GroupNode.PathKey"/> is the group's identity,
    /// not reference equality.)
    /// </remarks>
    public sealed class FixedGroupHeaderEntry : INotifyPropertyChanged, IAlignedGroupSummarySource
    {
        internal FixedGroupHeaderEntry(int level, GroupNode node, GridColumn column, SearchDataGrid ownerGrid)
        {
            Level = level;
            Node = node;
            Column = column;
            OwnerGrid = ownerGrid;
        }

        /// <summary>
        /// Zero-based nesting depth this pinned header represents — matches
        /// <see cref="GridColumn.GroupLevel"/> for <see cref="Column"/>.
        /// </summary>
        public int Level { get; }

        /// <summary>The projection node this entry mirrors. Drives <see cref="Name"/> / <see cref="ItemCount"/> / expand-state.</summary>
        internal GroupNode Node { get; }

        /// <summary>
        /// The owning grid, so the is-expanded converter and the toggle / expand / collapse /
        /// ungroup commands can route through the grid's path-keyed expand state.
        /// </summary>
        internal SearchDataGrid OwnerGrid { get; }

        /// <summary>
        /// The grouped <see cref="GridColumn"/> at <see cref="Level"/>. The per-item template uses
        /// this to pick up the column's <c>GroupHeaderTemplate</c> / <c>GroupValueTemplate</c>.
        /// </summary>
        public GridColumn Column { get; }

        /// <summary>Convenience accessor for the group's display name — what most per-item templates bind to.</summary>
        public object Name => Node?.DisplayValue;

        /// <summary>Convenience accessor for the group's row count — what most per-item templates bind to.</summary>
        public int ItemCount => Node?.Count ?? 0;

        /// <summary>
        /// Left-side group-summary run for the mirrored group, so the pinned strip's shared
        /// header chrome renders the same summaries as the in-body header row.
        /// </summary>
        public string GroupSummaryLeftText => Node?.SummaryLeftText;

        /// <summary>Right-side group-summary run for the mirrored group. See <see cref="GroupSummaryLeftText"/>.</summary>
        public string GroupSummaryRightText => Node?.SummaryRightText;

        /// <summary>Structured left-side run for the mirrored group — styled per-segment rendering. See <see cref="GroupSummaryLeftText"/>.</summary>
        public IReadOnlyList<SummaryResult> GroupSummaryLeftInfo => Node?.SummaryLeftInfo;

        /// <summary>Structured right-side run for the mirrored group — styled per-segment rendering. See <see cref="GroupSummaryLeftText"/>.</summary>
        public IReadOnlyList<SummaryResult> GroupSummaryRightInfo => Node?.SummaryRightInfo;

        /// <summary>
        /// Column-aligned summary results for the mirrored group (AlignByColumns mode), so the
        /// pinned strip renders the same aligned values as the in-body header row. Reads the
        /// node live; <see cref="NotifySummaryTextsChanged"/> announces in-place recomputes.
        /// </summary>
        IReadOnlyList<SummaryResult> IAlignedGroupSummarySource.GetAlignedResultsFor(GridColumn descriptor)
        {
            if (descriptor == null) return null;
            var results = Node?.AlignedSummaryResults;
            return results != null && results.TryGetValue(descriptor, out var list) ? list : null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises change notification for both summary runs after the grid recomputes the mirrored
        /// node's texts in place. Needed because the strip's resolver skips slots whose entry
        /// compares equal (same level + path key), so a surviving entry instance must announce
        /// the node-backed values changed underneath it.
        /// </summary>
        internal void NotifySummaryTextsChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryLeftText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryRightText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryLeftInfo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupSummaryRightInfo)));
        }

        /// <summary>
        /// The mirrored group's expansion state, so the pinned chevron binds expand-state the same
        /// way the in-body header row does (<c>GroupHeaderRow.IsExpanded</c>). Reads the node live:
        /// a reflatten replaces the node (and the resolver's next pass rebinds against fresh
        /// entries), while an animated splice toggle flips the surviving node in place and pings
        /// reused entries via <see cref="NotifyIsExpandedChanged"/>.
        /// </summary>
        public bool IsExpanded => Node?.IsExpanded ?? true;

        /// <summary>
        /// Raises change notification for <see cref="IsExpanded"/> after a splice toggle flipped
        /// the backing node underneath a reused entry (the resolver skips slots whose entry
        /// compares equal, so the surviving instance must announce the change itself).
        /// </summary>
        internal void NotifyIsExpandedChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));

        public override bool Equals(object obj)
            => obj is FixedGroupHeaderEntry other
                && other.Level == Level
                && other.Node?.PathKey == Node?.PathKey;

        public override int GetHashCode()
            => System.HashCode.Combine(Level, Node?.PathKey);
    }
}
