using System.Windows;
using System.Windows.Controls.Primitives;

namespace WWControls.Wpf
{
    /// <summary>
    /// Headers presenter that materializes <see cref="SearchDataGridColumnHeader"/> instead of
    /// the stock header. The retargeting needs both <see cref="GetContainerForItemOverride"/>
    /// (per-column headers) AND a re-templated <c>PART_FillerColumnHeader</c> in the
    /// presenter's own style — the filler isn't created via the items pipeline.
    /// </summary>
    public class SearchDataGridColumnHeadersPresenter : DataGridColumnHeadersPresenter
    {
        static SearchDataGridColumnHeadersPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SearchDataGridColumnHeadersPresenter),
                new FrameworkPropertyMetadata(typeof(SearchDataGridColumnHeadersPresenter)));
        }

        protected override DependencyObject GetContainerForItemOverride()
            => new SearchDataGridColumnHeader();

        protected override bool IsItemItsOwnContainerOverride(object item)
            => item is SearchDataGridColumnHeader;
    }
}
