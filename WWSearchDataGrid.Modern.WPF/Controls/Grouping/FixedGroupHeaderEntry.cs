using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// One pinned-header view-model in the sticky strip rendered by
    /// <see cref="FixedGroupHeadersPresenter"/>. Carries everything the per-item template needs to
    /// mirror the in-place group header at <see cref="Level"/>: the live
    /// <see cref="CollectionViewGroup"/> (for <c>Name</c>, <c>ItemCount</c>, expand-state routing),
    /// the owning <see cref="GridColumn"/> (for column-driven <c>GroupHeaderTemplate</c> /
    /// <c>GroupValueTemplate</c> resolution), and a weak reference to the realized
    /// <see cref="GroupItem"/> if one currently exists in the visual tree (so click + scroll-into-view
    /// routing in later steps can find it without an ItemContainerGenerator walk).
    /// </summary>
    /// <remarks>
    /// Entries are recomputed by the grid's active-chain resolver on every scroll change (step 3).
    /// They are value-shaped — two entries with the same <see cref="Level"/> and reference-identical
    /// <see cref="Group"/> compare equal, so the resolver can skip no-op updates against the current
    /// collection cheaply.
    /// </remarks>
    public sealed class FixedGroupHeaderEntry
    {
        public FixedGroupHeaderEntry(int level, CollectionViewGroup group, GridColumn column, GroupItem representedGroupItem)
        {
            Level = level;
            Group = group;
            Column = column;
            RepresentedGroupItem = representedGroupItem;
        }

        /// <summary>
        /// Zero-based nesting depth this pinned header represents — matches
        /// <see cref="GridColumn.GroupLevel"/> for <see cref="Column"/>.
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// The live <see cref="CollectionViewGroup"/> whose chain currently contains the topmost
        /// visible row. Bind to <c>Name</c> / <c>ItemCount</c> from the per-item template.
        /// </summary>
        public CollectionViewGroup Group { get; }

        /// <summary>
        /// The grouped <see cref="GridColumn"/> at <see cref="Level"/>, resolved via
        /// <see cref="SearchDataGrid.GetGroupedColumnAtLevel"/>. The per-item template uses this to
        /// pick up the column's <c>GroupHeaderTemplate</c> / <c>GroupValueTemplate</c> instead of
        /// the chain-depth walk used by the in-place selectors (which has no <see cref="GroupItem"/>
        /// ancestor to read from in the strip).
        /// </summary>
        public GridColumn Column { get; }

        /// <summary>
        /// The realized <see cref="GroupItem"/> this pinned entry mirrors when one currently
        /// exists in the visual tree. <c>null</c> when the represented group has been virtualized
        /// past the viewport or has not yet realized; later steps re-resolve as needed rather than
        /// keeping a stale reference.
        /// </summary>
        public GroupItem RepresentedGroupItem { get; }

        /// <summary>
        /// Convenience accessor for the group's display name — what most per-item templates bind to.
        /// </summary>
        public object Name => Group?.Name;

        /// <summary>
        /// Convenience accessor for the group's row count — what most per-item templates bind to.
        /// </summary>
        public int ItemCount => Group?.ItemCount ?? 0;

        public override bool Equals(object obj)
            => obj is FixedGroupHeaderEntry other
                && other.Level == Level
                && ReferenceEquals(other.Group, Group);

        public override int GetHashCode()
            => System.HashCode.Combine(Level, Group is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Group));
    }
}
