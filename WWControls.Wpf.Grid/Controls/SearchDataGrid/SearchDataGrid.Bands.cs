using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace WWControls.Wpf.Grids
{
    public partial class SearchDataGrid
    {
        #region Bands (banded column headers)

        /// <summary>Read-only key for <see cref="Bands"/>.</summary>
        private static readonly DependencyPropertyKey BandsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Bands),
                typeof(FreezableCollection<ColumnDescriptorElement>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="Bands"/> dependency property.</summary>
        public static readonly DependencyProperty BandsProperty = BandsPropertyKey.DependencyProperty;

        /// <summary>
        /// Banded column headers. Each <see cref="GridColumnBand"/> groups a run of columns — or
        /// nested bands — under a caption row above the column headers; bare <see cref="GridColumn"/>s
        /// may appear at any level and render with no caption. When populated, the band tree is
        /// flattened into <see cref="GridColumns"/> at load and becomes the source of truth for the
        /// column set and order, so declaring columns here is an alternative to declaring them
        /// directly in <see cref="GridColumns"/> (do not do both).
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;sdg:SearchDataGrid.Bands&gt;
        ///     &lt;sdg:GridColumnBand Header="Model Details"&gt;
        ///         &lt;sdg:GridColumn FieldName="Name" Header="Model" /&gt;
        ///         &lt;sdg:GridColumnBand Header="Identifiers"&gt;
        ///             &lt;sdg:GridColumn FieldName="Vin" /&gt;
        ///         &lt;/sdg:GridColumnBand&gt;
        ///     &lt;/sdg:GridColumnBand&gt;
        /// &lt;/sdg:SearchDataGrid.Bands&gt;
        /// </code>
        /// </example>
        public FreezableCollection<ColumnDescriptorElement> Bands =>
            (FreezableCollection<ColumnDescriptorElement>)GetValue(BandsProperty);

        /// <summary>Read-only key for <see cref="MaxBandDepth"/>.</summary>
        private static readonly DependencyPropertyKey MaxBandDepthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(MaxBandDepth),
                typeof(int),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(0));

        /// <summary>Identifies the <see cref="MaxBandDepth"/> dependency property.</summary>
        public static readonly DependencyProperty MaxBandDepthProperty = MaxBandDepthPropertyKey.DependencyProperty;

        /// <summary>
        /// Number of stacked band caption rows above the column headers (the maximum band nesting
        /// depth). <c>0</c> when no bands are declared. The band header presenter binds to this to
        /// size the caption area and collapse when there are no bands.
        /// </summary>
        public int MaxBandDepth => (int)GetValue(MaxBandDepthProperty);

        /// <summary>
        /// Resolved band layout — caption nodes only, leaf columns excluded — consumed by the band
        /// header presenter. Rebuilt by <see cref="FlattenBandsIntoGridColumns"/>; empty when no
        /// bands are declared.
        /// </summary>
        internal IReadOnlyList<BandLayoutNode> BandLayout => _bandLayout;
        private IReadOnlyList<BandLayoutNode> _bandLayout = Array.Empty<BandLayoutNode>();

        /// <summary>Guards <see cref="FlattenBandsIntoGridColumns"/> to a single pass.</summary>
        private bool _bandsFlattened;

        /// <summary>
        /// Seeds the <see cref="Bands"/> collection so XAML can populate it immediately. Called
        /// from the constructor (same seeding convention as <see cref="GridColumns"/>).
        /// </summary>
        private void InitializeBands()
        {
            SetValue(BandsPropertyKey, new FreezableCollection<ColumnDescriptorElement>());
        }

        /// <summary>
        /// Depth-first flatten of <see cref="Bands"/>: appends each leaf <see cref="GridColumn"/>
        /// (in declaration order) to <see cref="GridColumns"/> so the normal generation pipeline
        /// builds it, and builds the <see cref="BandLayout"/> model plus <see cref="MaxBandDepth"/>
        /// the header presenter reads. No-op when <see cref="Bands"/> is empty. Runs once, before
        /// column generation.
        /// </summary>
        private void FlattenBandsIntoGridColumns()
        {
            if (_bandsFlattened) return;
            _bandsFlattened = true;

            var bands = Bands;
            if (bands == null || bands.Count == 0)
            {
                _bandLayout = Array.Empty<BandLayoutNode>();
                SetValue(MaxBandDepthPropertyKey, 0);
                return;
            }

            var gridColumns = GridColumns;
            if (gridColumns.Count > 0)
            {
                // Bands owns the column set — warn (don't throw) so a mixed declaration still runs;
                // leaf columns from the band tree are appended after whatever is already present.
                Debug.WriteLine(
                    "SearchDataGrid: both Bands and GridColumns are populated. Bands drives the " +
                    "column set — leaf columns declared in Bands are appended to GridColumns.");
            }

            var orderedLeaves = new List<GridColumn>();
            var layout = new List<BandLayoutNode>();
            int maxDepth = 0;

            foreach (var node in bands)
                BuildBandNode(node, level: 0, orderedLeaves, layout, ref maxDepth);

            foreach (var column in orderedLeaves)
            {
                if (!gridColumns.Contains(column))
                    gridColumns.Add(column);
            }

            _bandLayout = layout;
            SetValue(MaxBandDepthPropertyKey, maxDepth);
        }

        /// <summary>
        /// Recursive band-tree walk. Leaf <see cref="GridColumn"/>s are collected into
        /// <paramref name="orderedLeaves"/> in declaration order; each <see cref="GridColumnBand"/>
        /// becomes a <see cref="BandLayoutNode"/> in <paramref name="layoutSink"/> carrying its
        /// level and the leaf columns it spans. DataContext is pushed onto every descriptor since
        /// descriptors live outside the logical tree and don't inherit it on their own (same reason
        /// <see cref="GenerateColumnsFromDescriptors"/> sets it on each column).
        /// </summary>
        private void BuildBandNode(
            ColumnDescriptorElement node,
            int level,
            List<GridColumn> orderedLeaves,
            List<BandLayoutNode> layoutSink,
            ref int maxDepth)
        {
            switch (node)
            {
                case GridColumn column:
                    column.DataContext = DataContext;
                    orderedLeaves.Add(column);
                    break;

                case GridColumnBand band:
                    band.DataContext = DataContext;
                    if (level + 1 > maxDepth) maxDepth = level + 1;

                    int firstLeafIndex = orderedLeaves.Count;
                    var childLayout = new List<BandLayoutNode>();
                    foreach (var child in band.Children)
                        BuildBandNode(child, level + 1, orderedLeaves, childLayout, ref maxDepth);

                    int leafCount = orderedLeaves.Count - firstLeafIndex;
                    var members = leafCount > 0
                        ? (IReadOnlyList<GridColumn>)orderedLeaves.GetRange(firstLeafIndex, leafCount)
                        : Array.Empty<GridColumn>();

                    layoutSink.Add(new BandLayoutNode(band.Header, band.HeaderTemplate, level, members, childLayout));
                    break;
            }
        }

        #endregion
    }
}
