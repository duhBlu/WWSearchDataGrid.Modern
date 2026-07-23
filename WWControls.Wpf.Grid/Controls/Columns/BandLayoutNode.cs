using System.Collections.Generic;
using System.Windows;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One band caption in the flattened band layout that <see cref="SearchDataGrid"/> builds from
    /// its <see cref="SearchDataGrid.Bands"/> tree. Leaf columns are not represented here — their
    /// own column headers are the bottom row; this model carries only the caption cells stacked
    /// above them.
    /// </summary>
    /// <remarks>
    /// Consumed by the band header presenter (added in a later phase): <see cref="Level"/> is the
    /// caption row (0 = topmost) and <see cref="MemberColumns"/> are the leaf columns the caption
    /// spans (its horizontal bounds run from the first member's left edge to the last member's
    /// right edge).
    /// </remarks>
    internal sealed class BandLayoutNode
    {
        public BandLayoutNode(
            object header,
            DataTemplate headerTemplate,
            int level,
            IReadOnlyList<GridColumn> memberColumns,
            IReadOnlyList<BandLayoutNode> children)
        {
            Header = header;
            HeaderTemplate = headerTemplate;
            Level = level;
            MemberColumns = memberColumns;
            Children = children;
        }

        /// <summary>The band's caption content.</summary>
        public object Header { get; }

        /// <summary>Optional template for <see cref="Header"/>; null renders as text.</summary>
        public DataTemplate HeaderTemplate { get; }

        /// <summary>Zero-based caption row (0 = topmost band row).</summary>
        public int Level { get; }

        /// <summary>
        /// All leaf columns beneath this band, in display order. The caption cell spans from the
        /// first member's left edge to the last member's right edge.
        /// </summary>
        public IReadOnlyList<GridColumn> MemberColumns { get; }

        /// <summary>Nested band captions directly under this band (excludes leaf columns).</summary>
        public IReadOnlyList<BandLayoutNode> Children { get; }
    }
}
