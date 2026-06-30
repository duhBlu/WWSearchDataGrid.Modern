using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// The sticky strip overlaid at the top of the data area that mirrors the active group chain
    /// for the topmost visible row when <see cref="SearchDataGrid.AllowFixedGroups"/> is true.
    /// Items are pinned-header view-models — one per nesting level, in
    /// <see cref="GridColumn.GroupLevel"/> order — supplied by the grouping engine as the scroll
    /// position changes.
    /// </summary>
    /// <remarks>
    /// The strip stays pinned at the top; the scroll-driven resolver
    /// (<see cref="SearchDataGrid.UpdateFixedGroupHeaders"/>) swaps the active chain instantly as
    /// the user scrolls. Click + right-click routing on pinned expanders is wired through the
    /// per-item template's command bindings.
    /// </remarks>
    public class FixedGroupHeadersPresenter : ItemsControl
    {
        static FixedGroupHeadersPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FixedGroupHeadersPresenter),
                new FrameworkPropertyMetadata(typeof(FixedGroupHeadersPresenter)));
        }

        public FixedGroupHeadersPresenter()
        {
            Loaded += (_, _) => SetValue(OwnerGridPropertyKey, VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this));
        }

        private static readonly DependencyPropertyKey OwnerGridPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(OwnerGrid),
                typeof(SearchDataGrid),
                typeof(FixedGroupHeadersPresenter),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="OwnerGrid"/> dependency property.</summary>
        public static readonly DependencyProperty OwnerGridProperty = OwnerGridPropertyKey.DependencyProperty;

        /// <summary>
        /// The owning <see cref="SearchDataGrid"/>, resolved via visual-tree ancestor walk when
        /// this presenter is loaded. The scroll-driven resolver (step 2) reads this to locate the
        /// inner <see cref="ScrollViewer"/> and the realized rows whose group chain it mirrors.
        /// </summary>
        public SearchDataGrid OwnerGrid => (SearchDataGrid)GetValue(OwnerGridProperty);
    }
}
