using System.Windows;
using System.Windows.Controls.Primitives;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Headers presenter that materializes <see cref="SearchDataGridColumnHeader"/> instances
    /// instead of the stock <see cref="DataGridColumnHeader"/>. The custom header carries the
    /// click-suppression override and filter-state DPs the grid's header chrome relies on;
    /// without overriding the container type AND the presenter's own template (which directly
    /// instantiates the filler header), WPF would mix stock and custom headers — the column-
    /// header style's strict <see cref="Style.TargetType"/> check then throws when the style
    /// is transferred down to the stock filler.
    /// </summary>
    /// <remarks>
    /// Two override seams are needed:
    /// <list type="bullet">
    ///   <item><see cref="GetContainerForItemOverride"/> — covers the regular per-column
    ///   headers produced by the items-control pipeline.</item>
    ///   <item>A re-templated <c>PART_FillerColumnHeader</c> in the presenter's own style —
    ///   the filler is created directly inside the presenter's control template, not via
    ///   the items pipeline, so it has to be retargeted to <see cref="SearchDataGridColumnHeader"/>
    ///   explicitly. The style ships in <c>Themes/Controls/SearchDataGrid.xaml</c>.</item>
    /// </list>
    /// </remarks>
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
