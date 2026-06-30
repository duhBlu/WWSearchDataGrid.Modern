using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// The pinned total summary row beneath the data area, parenting one
    /// <see cref="TotalSummaryCell"/> per data column. Column tracking, scroll sync, and
    /// header-mirroring layout live in <see cref="ColumnAlignedRowPresenter"/> (shared with
    /// the filter row). Visibility is bound to
    /// <see cref="SearchDataGrid.ActualShowTotalSummaryRow"/> in the grid template.
    /// </summary>
    public class TotalSummaryRowPresenter : ColumnAlignedRowPresenter
    {
        static TotalSummaryRowPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TotalSummaryRowPresenter),
                new FrameworkPropertyMetadata(typeof(TotalSummaryRowPresenter)));
        }

        public const string PartSummaryRowPanelName = "PART_SummaryRowPanel";

        protected override string PanelPartName => PartSummaryRowPanelName;

        protected override UIElement ResolveChildForColumn(DataGridColumn column)
        {
            return new TotalSummaryCell
            {
                Column = column,
                Descriptor = OwnerGrid?.FindGridColumnDescriptor(column),
                OwnerGrid = OwnerGrid,
            };
        }
    }
}
