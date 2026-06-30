using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// The aligned-summary layer of one pinned-strip entry
    /// (<see cref="GroupSummaryDisplayMode.AlignByColumns"/> with
    /// <see cref="SearchDataGrid.AllowFixedGroups"/>). Unlike the in-body
    /// <see cref="GroupSummaryCellsPresenter"/> — whose host row scrolls with the content — the
    /// strip is pinned chrome, so this derives from <see cref="ColumnAlignedRowPresenter"/> (the
    /// filter row / totals row base) for header-mirroring layout and horizontal scroll sync,
    /// keeping each value under its column while the content pans beneath the strip. Cells read
    /// the entry (<see cref="FixedGroupHeaderEntry"/>, the DataContext) through the same
    /// <see cref="GroupSummaryCell"/> the in-body layer uses; in Header mode every cell reads
    /// empty, and the strip template collapses the layer entirely.
    /// </summary>
    public class FixedGroupSummaryCellsPresenter : ColumnAlignedRowPresenter
    {
        static FixedGroupSummaryCellsPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FixedGroupSummaryCellsPresenter),
                new FrameworkPropertyMetadata(typeof(FixedGroupSummaryCellsPresenter)));
        }

        public const string PartFixedSummaryCellsPanelName = "PART_FixedSummaryCellsPanel";

        protected override string PanelPartName => PartFixedSummaryCellsPanelName;

        public FixedGroupSummaryCellsPresenter()
        {
            DataContextChanged += OnDataContextChanged;
        }

        protected override UIElement ResolveChildForColumn(DataGridColumn column)
        {
            return new GroupSummaryCell
            {
                Column = column,
                Descriptor = OwnerGrid?.FindGridColumnDescriptor(column),
                Source = DataContext,
            };
        }

        /// <summary>
        /// Strip entries are value-shaped and replaced by the resolver — re-point the existing
        /// cells when the container re-binds.
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = HostPanel;
            if (panel == null) return;
            foreach (var child in panel.Children)
            {
                if (child is GroupSummaryCell cell)
                    cell.Source = DataContext;
            }
        }
    }
}
