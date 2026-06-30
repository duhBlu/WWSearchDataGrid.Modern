using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// Hosts a <see cref="FilterRowPanel"/> beneath the column headers of a
    /// <see cref="SearchDataGrid"/>, parenting one <see cref="ColumnFilterControl"/> per data
    /// column. Column tracking, scroll sync, and header-mirroring layout live in
    /// <see cref="ColumnAlignedRowPresenter"/>.
    /// </summary>
    public class FilterRowPresenter : ColumnAlignedRowPresenter
    {
        static FilterRowPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FilterRowPresenter),
                new FrameworkPropertyMetadata(typeof(FilterRowPresenter)));
        }

        public const string PartFilterRowPanelName = "PART_FilterRowPanel";

        protected override string PanelPartName => PartFilterRowPanelName;

        protected override UIElement ResolveChildForColumn(DataGridColumn column)
        {
            return new ColumnFilterControl
            {
                CurrentColumn = column,
                SourceDataGrid = OwnerGrid,
            };
        }
    }
}
