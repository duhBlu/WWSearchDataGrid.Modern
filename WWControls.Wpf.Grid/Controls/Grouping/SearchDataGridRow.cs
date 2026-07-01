using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWControls.Wpf.Grids
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

        private static readonly DependencyPropertyKey IsGroupFooterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsGroupFooter),
                typeof(bool),
                typeof(SearchDataGridRow),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only <see cref="IsGroupFooter"/> dependency property. The themed
        /// <c>GridSearchDataGridRow</c> style keys its template-swap <c>DataTrigger</c> on this.
        /// </summary>
        public static readonly DependencyProperty IsGroupFooterProperty = IsGroupFooterPropertyKey.DependencyProperty;

        /// <summary>True when this container is rendering a <see cref="GroupFooterRow"/> sentinel.</summary>
        public bool IsGroupFooter => (bool)GetValue(IsGroupFooterProperty);

        private static readonly DependencyPropertyKey GroupFooterPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(GroupFooter),
                typeof(GroupFooterRow),
                typeof(SearchDataGridRow),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="GroupFooter"/> dependency property.</summary>
        public static readonly DependencyProperty GroupFooterProperty = GroupFooterPropertyKey.DependencyProperty;

        /// <summary>The group-footer sentinel this row represents, or <c>null</c> for any other row.</summary>
        public GroupFooterRow GroupFooter => (GroupFooterRow)GetValue(GroupFooterProperty);

        /// <summary>
        /// Sets (or clears) the group-footer state for the item this container is being prepared
        /// for. Called by the grid from <c>PrepareContainerForItemOverride</c>. Footer rows are
        /// display-only — their cells carry the right-click summary picker, but the row itself
        /// never enters selection (see <see cref="OnMouseLeftButtonDown"/> /
        /// <see cref="SearchDataGrid.ScrubHeaderSelection"/>).
        /// </summary>
        internal void SetGroupFooter(GroupFooterRow footer)
        {
            SetValue(GroupFooterPropertyKey, footer);
            SetValue(IsGroupFooterPropertyKey, footer != null);
        }

        private static readonly DependencyPropertyKey IsRowEditingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsRowEditing),
                typeof(bool),
                typeof(SearchDataGridRow),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only <see cref="IsRowEditing"/> dependency property. The themed row
        /// style can key a highlight trigger on this so the row under the full-row-edit overlay reads
        /// as the active one.
        /// </summary>
        public static readonly DependencyProperty IsRowEditingProperty = IsRowEditingPropertyKey.DependencyProperty;

        /// <summary>
        /// True while this row is the one open in full-row ("edit entire row") edit mode — the row
        /// the <see cref="RowEditPresenter"/> overlay is editing. Pushed by
        /// <see cref="SearchDataGrid.BeginRowEdit"/> / cleared when the row edit ends.
        /// </summary>
        public bool IsRowEditing => (bool)GetValue(IsRowEditingProperty);

        /// <summary>Sets (or clears) the full-row-edit highlight state. Called by the owning grid.</summary>
        internal void SetRowEditing(bool editing) => SetValue(IsRowEditingPropertyKey, editing);

        private static readonly DependencyPropertyKey IsCellsHiddenPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsCellsHidden),
                typeof(bool),
                typeof(SearchDataGridRow),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only <see cref="IsCellsHidden"/> dependency property. The themed row
        /// template keys a trigger on this to collapse the cells presenter while the edit form
        /// stands in for the row (<see cref="EditFormShowMode.InlineHideRow"/>).
        /// </summary>
        public static readonly DependencyProperty IsCellsHiddenProperty = IsCellsHiddenPropertyKey.DependencyProperty;

        /// <summary>
        /// True while this row's cells are hidden so the inline edit form shows in place of the row
        /// (<see cref="EditFormShowMode.InlineHideRow"/>). The row's details area (hosting the form)
        /// stays visible. Pushed by <see cref="SearchDataGrid.ApplyEditFormRowState(SearchDataGridRow, bool)"/>.
        /// </summary>
        public bool IsCellsHidden => (bool)GetValue(IsCellsHiddenProperty);

        /// <summary>Sets (or clears) the cells-hidden state for InlineHideRow. Called by the owning grid.</summary>
        internal void SetCellsHidden(bool hidden) => SetValue(IsCellsHiddenPropertyKey, hidden);

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
            if (IsGroupFooter)
            {
                // Display-only: swallow the click so the footer sentinel never enters selection
                // (right-click still reaches the footer cells' summary picker).
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
