using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WWControls.Core;
using WWControls.Wpf.Controls.Primitives;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Shared base for Column Chooser tree nodes. Derives <see cref="WWTreeNodeBase{T}"/> so the nodes
    /// plug into <see cref="WWTreeView"/>'s native search / highlight / filter pass for free (Children,
    /// filtered ChildrenView, expansion + match-state flags); subclasses only supply
    /// <see cref="WWTreeNodeBase{T}.MatchesSelf"/>.
    /// </summary>
    public abstract class ColumnChooserNode : WWTreeNodeBase<ColumnChooserNode>
    {
    }

    /// <summary>
    /// Leaf node wrapping one grid column. The wrapped <see cref="ColumnVisibilityInfo"/> drives the row
    /// visuals (visibility checkbox, sort / filter / pin glyphs, shared header context menu) exactly as in
    /// the flat section view, and its display name is what the tree searches on.
    /// </summary>
    public sealed class ColumnChooserColumnNode : ColumnChooserNode
    {
        public ColumnChooserColumnNode(ColumnVisibilityInfo column)
        {
            Column = column;
        }

        /// <summary>The column this leaf represents.</summary>
        public ColumnVisibilityInfo Column { get; }

        /// <summary>Case-insensitive substring match against the column's display name.</summary>
        public override bool MatchesSelf(SearchQuery query) => query.Matches(Column?.DisplayName);
    }

    /// <summary>
    /// A band (parent) node. Mirrors one <see cref="GridColumnBand"/>: its children are nested band nodes
    /// and/or leaf <see cref="ColumnChooserColumnNode"/>s in declaration order, and the tri-state
    /// <see cref="IsChecked"/> shows or hides every descendant column at once. Matches on the band caption.
    /// </summary>
    public sealed class ColumnChooserBandNode : ColumnChooserNode, IDisposable
    {
        /// <summary>
        /// Creates a band node.
        /// </summary>
        /// <param name="header">Caption content copied from the band's <see cref="GridColumnBand.Header"/>.</param>
        /// <param name="headerTemplate">Optional caption template from <see cref="GridColumnBand.HeaderTemplate"/>.</param>
        /// <param name="children">Child nodes (nested bands and/or leaf columns) in declaration order.</param>
        /// <param name="leafColumns">Every descendant leaf column, flattened, backing <see cref="IsChecked"/>.</param>
        public ColumnChooserBandNode(
            object header,
            DataTemplate headerTemplate,
            IEnumerable<ColumnChooserNode> children,
            IReadOnlyList<ColumnVisibilityInfo> leafColumns)
        {
            Header = header;
            HeaderTemplate = headerTemplate;
            LeafColumns = leafColumns ?? Array.Empty<ColumnVisibilityInfo>();

            SetChildren(children?.ToList() ?? new List<ColumnChooserNode>());

            // Recompute the group tri-state whenever any descendant leaf's visibility flips — no matter
            // the source (this node's checkbox, a leaf checkbox, a nested band, the header menu, or Select All).
            foreach (var leaf in LeafColumns)
                leaf.PropertyChanged += OnLeafPropertyChanged;
        }

        /// <summary>Caption content copied from <see cref="GridColumnBand.Header"/>.</summary>
        public object Header { get; }

        /// <summary>Optional caption template copied from <see cref="GridColumnBand.HeaderTemplate"/>.</summary>
        public DataTemplate HeaderTemplate { get; }

        /// <summary>
        /// Every descendant leaf column (flattened across nested bands), backing the <see cref="IsChecked"/>
        /// tri-state.
        /// </summary>
        public IReadOnlyList<ColumnVisibilityInfo> LeafColumns { get; }

        /// <summary>
        /// Group visibility tri-state: <c>true</c> when every descendant column is visible, <c>false</c>
        /// when none are, <c>null</c> when mixed. Setting a non-null value applies it to all descendant
        /// columns, which cascades to the grid via <see cref="ColumnVisibilityInfo.IsVisible"/>. The bound
        /// checkbox is two-state for the user (never lets them pick indeterminate) but still renders the
        /// mixed state when the data is mixed.
        /// </summary>
        public bool? IsChecked
        {
            get
            {
                if (LeafColumns.Count == 0) return false;
                int visible = LeafColumns.Count(c => c.IsVisible);
                if (visible == 0) return false;
                if (visible == LeafColumns.Count) return true;
                return null;
            }
            set
            {
                if (value == null) return;
                foreach (var leaf in LeafColumns)
                    leaf.IsVisible = value.Value;

                // Leaf handlers already raise IsChecked, but raise once here too so a no-op set
                // (everything already at the target) still refreshes the bound checkbox.
                OnPropertyChanged(nameof(IsChecked));
            }
        }

        /// <summary>Case-insensitive substring match against the band caption.</summary>
        public override bool MatchesSelf(SearchQuery query) => query.Matches(Header?.ToString());

        private void OnLeafPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColumnVisibilityInfo.IsVisible))
                OnPropertyChanged(nameof(IsChecked));
        }

        /// <summary>Unsubscribes from descendant leaves. Called when the chooser rebuilds the tree.</summary>
        public void Dispose()
        {
            foreach (var leaf in LeafColumns)
                leaf.PropertyChanged -= OnLeafPropertyChanged;
        }
    }
}
