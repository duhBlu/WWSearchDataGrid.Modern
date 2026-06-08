namespace WWSearchDataGrid.Modern.WPF
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
    public sealed class FixedGroupHeaderEntry
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

        public override bool Equals(object obj)
            => obj is FixedGroupHeaderEntry other
                && other.Level == Level
                && other.Node?.PathKey == Node?.PathKey;

        public override int GetHashCode()
            => System.HashCode.Combine(Level, Node?.PathKey);
    }
}
