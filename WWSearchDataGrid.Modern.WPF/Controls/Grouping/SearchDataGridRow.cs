using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// The container the grid generates for every row when flat grouping is in play. A plain
    /// <see cref="DataGridRow"/> for data rows; for a <see cref="GroupHeaderRow"/> sentinel it
    /// reports <see cref="IsGroupHeader"/> so the themed row style swaps to the full-width
    /// group-header template (see <c>GroupStyle.xaml</c>).
    /// </summary>
    /// <remarks>
    /// Header rows are focusable, keyboard-navigable, focus-highlighted "tree" rows — but they
    /// never enter the grid's selection (see <see cref="SearchDataGrid.ScrubHeaderSelection"/>);
    /// the highlight is keyboard focus, not selection. Mouse: a single left-click focuses the
    /// header, a click on the chevron expands/collapses it, and a double-click anywhere toggles.
    /// Keyboard handling (arrows / Space / Enter) is delegated to
    /// <see cref="SearchDataGrid.HandleHeaderNavigationKey"/>. Header detection is pushed in by
    /// <see cref="SearchDataGrid.PrepareContainerForItemOverride"/> on (re)use and cleared on
    /// recycle, so a recycled container never renders the previous item's chrome.
    /// </remarks>
    public class SearchDataGridRow : DataGridRow
    {
        private static readonly DependencyPropertyKey IsGroupHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsGroupHeader),
                typeof(bool),
                typeof(SearchDataGridRow),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only <see cref="IsGroupHeader"/> dependency property. The themed
        /// <c>GridSearchDataGridRow</c> style keys its template-swap <c>DataTrigger</c> on this.
        /// </summary>
        public static readonly DependencyProperty IsGroupHeaderProperty = IsGroupHeaderPropertyKey.DependencyProperty;

        /// <summary>True when this container is rendering a <see cref="GroupHeaderRow"/> sentinel.</summary>
        public bool IsGroupHeader => (bool)GetValue(IsGroupHeaderProperty);

        private static readonly DependencyPropertyKey GroupHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(GroupHeader),
                typeof(GroupHeaderRow),
                typeof(SearchDataGridRow),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="GroupHeader"/> dependency property.</summary>
        public static readonly DependencyProperty GroupHeaderProperty = GroupHeaderPropertyKey.DependencyProperty;

        /// <summary>
        /// The group-header sentinel this row represents, or <c>null</c> for a data row. Mirrors
        /// the row's <see cref="FrameworkElement.DataContext"/> but typed, so the toggle path and
        /// header chrome read it without a cast.
        /// </summary>
        public GroupHeaderRow GroupHeader => (GroupHeaderRow)GetValue(GroupHeaderProperty);

        /// <summary>
        /// Sets (or clears) the group-header state for the item this container is being prepared
        /// for. Called by the grid from <c>PrepareContainerForItemOverride</c>; passing <c>null</c>
        /// reverts the row to an ordinary data row (default focus behavior restored).
        /// </summary>
        internal void SetGroupHeader(GroupHeaderRow header)
        {
            SetValue(GroupHeaderPropertyKey, header);
            SetValue(IsGroupHeaderPropertyKey, header != null);

            // Header rows are the keyboard-focus target themselves (they have no DataGridCell to
            // focus); data rows keep the stock behavior where focus lands on a cell.
            if (header != null)
                Focusable = true;
            else
                ClearValue(FocusableProperty);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsGroupHeader)
            {
                // Double-click anywhere toggles; a single click on the chevron toggles; any other
                // single click focuses the header (focus highlight, not selection). Handling the
                // press here also stops the DataGrid from row-selecting the sentinel.
                if (e.ClickCount >= 2 || IsChevronHit())
                {
                    Toggle();
                }
                else
                {
                    // Focus highlight only — clear any data selection so the header is the sole
                    // active row, matching keyboard navigation onto a header.
                    VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this)?.UnselectAll();
                    Focus();
                }
                e.Handled = true;
                return;
            }
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsGroupHeader)
            {
                var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this);
                if (grid != null && grid.HandleHeaderNavigationKey(this, e.Key))
                {
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>Expands/collapses this header's group. Safe to call only on header rows.</summary>
        internal void Toggle()
        {
            var header = GroupHeader;
            if (header == null) return;
            var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this);
            grid?.ToggleGroup(header);
        }

        /// <summary>True when the pointer is over the header's expand/collapse chevron.</summary>
        private bool IsChevronHit()
        {
            var chevron = VisualTreeHelperMethods.FindVisualDescendant<Icon>(this, "Chevron");
            return chevron != null && chevron.IsMouseOver;
        }
    }
}
