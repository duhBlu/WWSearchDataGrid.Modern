using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WWSearchDataGrid.Modern.WPF
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
    }

    /// <summary>
    /// A group-header sentinel placed in the flat row list alongside raw user rows. The grid's
    /// row-style selector renders these as full-width header rows (no cells); the
    /// <see cref="DataGrid.ItemsSource"/> is a mix of these and the user's own items, so a row is
    /// a header iff it is a <see cref="GroupHeaderRow"/>.
    /// </summary>
    public sealed class GroupHeaderRow
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

        /// <summary>Snapshot of the node's expansion state at projection time (chevron direction).</summary>
        public bool IsExpanded { get; }

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
