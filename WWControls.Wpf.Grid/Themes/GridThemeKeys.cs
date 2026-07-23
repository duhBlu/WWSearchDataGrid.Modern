using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WWControls.Wpf;

namespace WWControls.Wpf
{
    /// <summary>
    /// Typed resource keys for the library's default styles and templates. Consumers retheme
    /// by redefining a resource under the same key:
    /// <code>
    /// &lt;Style x:Key="{x:Static sdg:GridThemeKeys.GridSearchDataGrid}"
    ///        TargetType="{x:Type sdg:SearchDataGrid}"
    ///        BasedOn="{StaticResource {x:Static sdg:GridThemeKeys.GridSearchDataGrid}}" /&gt;
    /// </code>
    /// Editor element styles live separately in <see cref="WWControls.Wpf.Controls.Editors.EditorThemeKeys"/>.
    /// </summary>
    public static class GridThemeKeys
    {
        #region Grid

        /// <summary>Default style for the top-level <see cref="SearchDataGrid"/> control.</summary>
        public static ComponentResourceKey GridSearchDataGrid { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGrid));

        /// <summary>Default style applied to <see cref="DataGridCell"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey GridSearchDataGridCell { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridCell));

        /// <summary>Default style applied to <see cref="DataGridRow"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey GridSearchDataGridRow { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridRow));

        /// <summary>Default style applied to <see cref="DataGridRowHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridRowHeader { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridRowHeader));

        /// <summary>
        /// Default style for the <see cref="RowEditPresenter"/> — the bright, column-aligned editor
        /// strip (plus Update / Cancel action bar) shown over the row open in full-row edit mode
        /// (<see cref="SearchDataGrid.RowEditTrigger"/>).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridRowEditPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridRowEditPresenter));

        /// <summary>
        /// Default style for the <see cref="EditFormPresenter"/> — the caption/editor form (plus
        /// Update / Cancel action bar) shown for the row open in full-row edit mode when
        /// <see cref="SearchDataGrid.EditFormShowMode"/> is not <see cref="EditFormShowMode.None"/>.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridEditFormPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridEditFormPresenter));

        /// <summary>
        /// <see cref="DataTemplate"/> the grid assigns to <see cref="DataGrid.RowDetailsTemplate"/>
        /// to host an <see cref="EditFormPresenter"/> in the editing row's details area. The
        /// template's <c>DataContext</c> is the row item; it wires <c>OwnerGrid</c>, the editing
        /// item, and the grid's <c>EditFormTemplate</c> / caption onto the presenter.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridEditFormRowDetailsTemplate { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridEditFormRowDetailsTemplate));

        /// <summary>
        /// <see cref="ControlTemplate"/> for an in-body group-header row — the full-width header
        /// rendered in place of cells when a <see cref="SearchDataGridRow.IsGroupHeader"/> container
        /// materializes. Renders the row-header gutter, per-level indent, chevron, column-prefixed
        /// value, and count chip, reading the level / owning column straight off the
        /// <see cref="GroupHeaderRow"/>. The default <c>GridSearchDataGridRow</c> style swaps to this
        /// via an <c>IsGroupHeader</c> trigger.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupHeaderRow { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupHeaderRow));

        /// <summary>
        /// Default style applied to <see cref="GroupSummaryCell"/> — one per visible column in a
        /// group header row's aligned-summary layer
        /// (<see cref="SearchDataGrid.GroupSummaryDisplayMode"/> = AlignByColumns).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupSummaryCell { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupSummaryCell));

        /// <summary>Default style applied to <see cref="GroupSummaryCellsPresenter"/> — the aligned-summary layer inside a group header row.</summary>
        public static ComponentResourceKey GridSearchDataGridGroupSummaryCellsPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupSummaryCellsPresenter));

        /// <summary>
        /// Default style applied to <see cref="FixedGroupSummaryCellsPresenter"/> — the
        /// aligned-summary layer inside a pinned-strip entry. Collapsed unless
        /// <see cref="SearchDataGrid.GroupSummaryDisplayMode"/> is AlignByColumns.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedGroupSummaryCellsPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridFixedGroupSummaryCellsPresenter));

        /// <summary>
        /// Style for the expand/collapse chevron <see cref="Button"/> on a group header — the padded,
        /// transparent hit target around the 9×9 chevron glyph, with the right-when-collapsed /
        /// down-when-expanded rotation and the idle/hover/pressed recolor. Shared by both the in-body
        /// header-row template (<see cref="GridSearchDataGridGroupHeaderRow"/>) and the pinned
        /// strip's per-entry template (<see cref="GridSearchDataGridFixedGroupHeadersPresenter"/>) so
        /// the chevron is identical fixed or unfixed; the rotation reads <c>IsExpanded</c> off the
        /// button's DataContext (a <see cref="GroupHeaderRow"/> or a <see cref="FixedGroupHeaderEntry"/>).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupHeaderChevronButton { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupHeaderChevronButton));

        /// <summary>Default style applied to <see cref="DataGridColumnHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeader { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridColumnHeader));

        /// <summary>
        /// Default style for a <see cref="BandHeaderCell"/> — one caption cell in the banded
        /// column-header area, spanning the columns grouped under a <see cref="GridColumnBand"/>.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridBandHeaderCell { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridBandHeaderCell));

        /// <summary>
        /// Default style for the <see cref="BandHeadersPresenter"/> — the banded column-header
        /// rows hosted above the column headers (one <see cref="BandHeaderCell"/> per band).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridBandHeadersPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridBandHeadersPresenter));

        /// <summary>
        /// Default <see cref="Style"/> for the <see cref="GroupPanel"/> — the strip above the
        /// column headers that shows one pill per grouped column.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupPanel { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupPanel));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> for the empty area of the
        /// <see cref="GroupPanel"/> — Expand All / Collapse All / Clear Grouping.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupPanelContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupPanelContextMenu));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> for an individual group-panel pill — Full Expand
        /// / Full Collapse on top, then the full column-header menu mirror (Copy / Sort / Best Fit
        /// / Hide / Pin / Filter operations). <c>x:Shared="False"</c> so every pill gets its own.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupPillContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupPillContextMenu));

        /// <summary>
        /// Default <see cref="Style"/> for the <see cref="FixedGroupHeadersPresenter"/> — the
        /// sticky strip pinned to the top of the data area that mirrors the active group chain
        /// of the topmost visible row when <see cref="SearchDataGrid.AllowFixedGroups"/> is true.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedGroupHeadersPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridFixedGroupHeadersPresenter));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> attached to every pinned header in the sticky
        /// strip — Expand / Collapse this group, Expand / Collapse all at this level, Ungroup at
        /// this level. Sibling to <see cref="GridSearchDataGridGroupHeaderContextMenu"/> but
        /// commands take a <see cref="FixedGroupHeaderEntry"/> instead of an <see cref="Expander"/>
        /// because the strip lives outside the rows-presenter visual subtree.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedGroupHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridFixedGroupHeaderContextMenu));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> attached to the in-body group-header rows —
        /// Expand / Collapse this group, Expand / Collapse all at this level, Ungroup at this level.
        /// Its commands take a <c>GroupHeaderRow</c> (the row template's DataContext); the
        /// <see cref="GridSearchDataGridFixedGroupHeaderContextMenu"/> sibling takes a
        /// <see cref="FixedGroupHeaderEntry"/> for the pinned strip.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource (declared with
        /// <c>x:Shared="False"</c> so every consumer gets its own instance) that hosts the
        /// column-header right-click menu — sort / best-fit / hide / pin / clear-filter. The
        /// header style and the column chooser ListBoxItem style both reference this key so
        /// the chooser rows behave like header surfaces. The menu's <c>DataContext</c> is set
        /// to a <see cref="Commands.ContextMenuContext"/> by the consumer.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridColumnHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the cell right-click menu —
        /// copy / copy-with-headers. The menu's <c>DataContext</c> is set to a
        /// <see cref="Commands.ContextMenuContext"/> by the consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridCellContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridCellContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the row-header right-click menu —
        /// copy / copy-with-headers. The menu's <c>DataContext</c> is set to a
        /// <see cref="Commands.ContextMenuContext"/> by the consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridRowHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridRowHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the grid-body right-click menu —
        /// filter operations, column profiles, export, and layout actions. The menu's
        /// <c>DataContext</c> is set to a <see cref="Commands.ContextMenuContext"/> by the
        /// consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridContextMenu));

        /// <summary>Default style for the select-all corner button in the SearchDataGrid header.</summary>
        public static ComponentResourceKey GridSelectAllButton { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSelectAllButton));

        #endregion

        #region ColumnChooser

        /// <summary>
        /// Default item-container style for the three section listboxes inside
        /// <see cref="ColumnChooser"/> (left-pinned, unpinned,
        /// right-pinned). Each row mirrors a column header — sort glyph, filter-active glyph,
        /// pin glyph — and reuses the shared column-header context menu so the chooser surface
        /// is consistent with right-clicks on the header band.
        /// </summary>
        public static ComponentResourceKey ColumnChooserSectionItem { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(ColumnChooserSectionItem));

        #endregion

        #region ColumnFilterPopup

        /// <summary>Default style for the <see cref="ColumnFilterPopup"/> popup.</summary>
        public static ComponentResourceKey ColumnFilterPopup { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(ColumnFilterPopup));

        #endregion

        #region FilterSummaryPanel

        /// <summary>Default style for the <see cref="FilterSummaryPanel"/> chip strip below the grid.</summary>
        public static ComponentResourceKey FilterSummaryPanel { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterSummaryPanel));

        #endregion

        #region FilterTokens

        /// <summary>Visual leading bracket for a grouped token run.</summary>
        public static ComponentResourceKey FilterTokensOpenBracket { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensOpenBracket));

        /// <summary>Column-name chip — orange pill carrying the column header text.</summary>
        public static ComponentResourceKey FilterTokensColumnName { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensColumnName));

        /// <summary>Search-type label that sits between the column chip and the value chip(s).</summary>
        public static ComponentResourceKey FilterTokensSearchType { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensSearchType));

        /// <summary>Unary search-type chip (IsNull / IsToday / AboveAverage / …) with a click-to-confirm remove overlay.</summary>
        public static ComponentResourceKey FilterTokensUnarySearchType { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensUnarySearchType));

        /// <summary>Single value chip (the green pill) with a click-to-confirm remove overlay.</summary>
        public static ComponentResourceKey FilterTokensValue { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensValue));

        /// <summary>Inline operator label between two value chips (e.g. the "and" in "between X and Y").</summary>
        public static ComponentResourceKey FilterTokensValueOperator { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensValueOperator));

        /// <summary>Visual trailing bracket for a grouped token run.</summary>
        public static ComponentResourceKey FilterTokensCloseBracket { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensCloseBracket));

        /// <summary>Logical connector chip between search-template groups — clickable to toggle AND/OR.</summary>
        public static ComponentResourceKey FilterTokensGroupLogicalConnector { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensGroupLogicalConnector));

        /// <summary>Logical connector chip between search templates within a group — clickable to toggle AND/OR.</summary>
        public static ComponentResourceKey FilterTokensTemplateLogicalConnector { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensTemplateLogicalConnector));

        /// <summary>Hover-revealed remove button that detaches an entire filter run.</summary>
        public static ComponentResourceKey FilterTokensRemoveAction { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterTokensRemoveAction));

        #endregion

        #region FilterEditorDialog

        /// <summary>Default style for the modal <see cref="FilterEditorDialog"/> window's content control.</summary>
        public static ComponentResourceKey FilterEditorDialog { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorDialog));

        /// <summary>Default style for the <see cref="ColumnNameTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorColumnNameToken { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorColumnNameToken));

        /// <summary>Default style for the <see cref="SearchTypeTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorSearchTypeToken { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorSearchTypeToken));

        /// <summary>Default style for the <see cref="ValueTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorValueToken { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorValueToken));

        /// <summary>Default style for the <see cref="GroupOperatorChip"/> inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorGroupOperatorChip { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorGroupOperatorChip));

        /// <summary>
        /// DataTemplate for a single condition row in the recursive Filter Editor tree —
        /// column chip + search-type chip + value chip + hover-revealed remove button.
        /// Looked up by <see cref="FilterEditorNodeTemplateSelector"/> at runtime.
        /// </summary>
        public static ComponentResourceKey FilterEditorConditionRow { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorConditionRow));

        /// <summary>
        /// DataTemplate for a group node in the recursive Filter Editor tree — operator
        /// chip + add-popup toggle + warning banner + recursive child list. Looked up by
        /// <see cref="FilterEditorNodeTemplateSelector"/> at runtime.
        /// </summary>
        public static ComponentResourceKey FilterEditorGroup { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterEditorGroup));

        #endregion

        #region FilterRow

        /// <summary>Default style for the <see cref="SearchTypeSelector"/> per-column mode picker.</summary>
        public static ComponentResourceKey FilterRowSearchTypeSelector { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterRowSearchTypeSelector));

        /// <summary>Default style for the <see cref="FilterRowPresenter"/> pinned filter row.</summary>
        public static ComponentResourceKey FilterRowPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterRowPresenter));

        /// <summary>Default style for the per-column <see cref="ColumnFilterControl"/>.</summary>
        public static ComponentResourceKey FilterRowColumnFilterControl { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(FilterRowColumnFilterControl));

        #endregion

        #region Total Summary Row

        /// <summary>Default style for the <see cref="TotalSummaryRowPresenter"/> pinned total summary row.</summary>
        public static ComponentResourceKey GridSearchDataGridTotalSummaryRow { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridTotalSummaryRow));

        /// <summary>Default style for the per-column <see cref="TotalSummaryCell"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridTotalSummaryCell { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridTotalSummaryCell));

        /// <summary>
        /// Right-click <see cref="System.Windows.Controls.ContextMenu"/> for a
        /// <see cref="TotalSummaryCell"/> — the runtime summary picker (Count / Sum / Min /
        /// Max / Average toggles + Clear).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridTotalSummaryCellContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridTotalSummaryCellContextMenu));

        /// <summary>Default style for the <see cref="WWControls.Wpf.GroupSummaryEditor"/> ("View Totals") dialog body.</summary>
        public static ComponentResourceKey GroupSummaryEditor { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GroupSummaryEditor));

        /// <summary>
        /// Default style for the <see cref="WWControls.Wpf.SummaryTextStyleEditor"/>
        /// dialog body — the Prefix / Value / Suffix text-styling sub-editor opened from the
        /// summary editor's Order tab.
        /// </summary>
        public static ComponentResourceKey SummaryTextStyleEditor { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(SummaryTextStyleEditor));

        /// <summary>
        /// Right-click <see cref="System.Windows.Controls.ContextMenu"/> for the fixed total
        /// summary panel — Count (grid row count toggle) + Customize….
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedTotalSummaryContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridFixedTotalSummaryContextMenu));

        /// <summary>Full-width group footer row template (one <see cref="GroupFooterCell"/> per column).</summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterRow { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupFooterRow));

        /// <summary>Default style for the footer cells host (<see cref="GroupFooterCellsPresenter"/>).</summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterCellsPresenter { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupFooterCellsPresenter));

        /// <summary>Default style for the per-column <see cref="GroupFooterCell"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterCell { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupFooterCell));

        /// <summary>
        /// Right-click <see cref="System.Windows.Controls.ContextMenu"/> for a
        /// <see cref="GroupFooterCell"/> — the runtime footer summary picker (Count / Sum / Min /
        /// Max / Average toggles + Clear + Customize).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterCellContextMenu { get; } =
            new ComponentResourceKey(typeof(GridThemeKeys), nameof(GridSearchDataGridGroupFooterCellContextMenu));

        #endregion
    }
}
