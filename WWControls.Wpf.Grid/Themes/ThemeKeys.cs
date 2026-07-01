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
    /// &lt;Style x:Key="{x:Static sdg:ThemeKeys.GridSearchDataGrid}"
    ///        TargetType="{x:Type sdg:SearchDataGrid}"
    ///        BasedOn="{StaticResource {x:Static sdg:ThemeKeys.GridSearchDataGrid}}" /&gt;
    /// </code>
    /// Editor element styles live separately in <see cref="EditSettingsThemeKeys"/>.
    /// </summary>
    public static class ThemeKeys
    {
        #region Primitives

        /// <summary>Sdg-themed <see cref="Button"/> style used inside library templates and available to consumers.</summary>
        public static ComponentResourceKey PrimitivesButton { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesButton));

        /// <summary>Sdg-themed <see cref="CheckBox"/> style.</summary>
        public static ComponentResourceKey PrimitivesCheckBox { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesCheckBox));

        /// <summary>Sdg-themed <see cref="ComboBox"/> style.</summary>
        public static ComponentResourceKey PrimitivesComboBox { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesComboBox));

        /// <summary>
        /// Sdg-themed <see cref="ComboBoxItem"/> style. Applied implicitly inside
        /// <see cref="PrimitivesComboBox"/>'s popup; exposed as a key so consumer-defined
        /// ComboBoxes that want the same dropdown-item look can opt in.
        /// </summary>
        public static ComponentResourceKey PrimitivesComboBoxItem { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesComboBoxItem));

        /// <summary>
        /// Sdg-themed <see cref="ListBoxItem"/> style — the shared "dropdown item" look used
        /// inside library popups (SearchTextBox suggestions, FilterEditor token popups). Visuals
        /// mirror <see cref="PrimitivesComboBoxItem"/> and <see cref="PrimitivesMenuItem"/> so
        /// every dropdown row in the library reads as the same control.
        /// </summary>
        public static ComponentResourceKey PrimitivesListBoxItem { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesListBoxItem));

        /// <summary>Sdg-themed <see cref="Primitives.ScrollBar"/> style.</summary>
        public static ComponentResourceKey PrimitivesScrollBar { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesScrollBar));

        /// <summary>Sdg-themed <see cref="TabControl"/> style.</summary>
        public static ComponentResourceKey PrimitivesTabControl { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesTabControl));

        /// <summary>Sdg-themed <see cref="TabItem"/> style.</summary>
        public static ComponentResourceKey PrimitivesTabItem { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesTabItem));

        /// <summary>Sdg-themed resize <see cref="Thumb"/> used by column splitters and resizers.</summary>
        public static ComponentResourceKey PrimitivesResizeThumb { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesResizeThumb));

        /// <summary>
        /// Sdg-themed <see cref="System.Windows.Controls.Primitives.ToggleButton"/> style — a pill
        /// that fills with the accent tint while checked. Used by the summary text-styling editor's
        /// Bold / Italic / Underline toggles.
        /// </summary>
        public static ComponentResourceKey PrimitivesToggleButton { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesToggleButton));

        /// <summary>Default style for the <see cref="WWControls.Wpf.WWColorPicker"/> swatch + HSV popup primitive.</summary>
        public static ComponentResourceKey PrimitivesColorPicker { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesColorPicker));

        /// <summary>Default style for the custom <see cref="SearchTextBox"/> primitive.</summary>
        public static ComponentResourceKey PrimitivesSearchTextBox { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesSearchTextBox));

        /// <summary>
        /// Sdg-themed plain <see cref="TextBox"/> style — same chrome as
        /// <see cref="PrimitivesComboBox"/> (border, radius, bottom-lip depth cue) with an
        /// accent underline while focused.
        /// </summary>
        public static ComponentResourceKey PrimitivesTextBox { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesTextBox));

        /// <summary>Default style for the custom <see cref="NumericUpDown"/> primitive.</summary>
        public static ComponentResourceKey PrimitivesNumericUpDown { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesNumericUpDown));

        /// <summary>Default style for the custom <see cref="RangeSlider"/> two-thumb range slider primitive.</summary>
        public static ComponentResourceKey PrimitivesRangeSlider { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesRangeSlider));

        /// <summary>Default style for the <see cref="StatusIcon"/> status badge primitive.</summary>
        public static ComponentResourceKey PrimitivesStatusIcon { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesStatusIcon));

        /// <summary>
        /// Default style for <see cref="ValidationCellPresenter"/> — the validated-cell layout
        /// (a left badge gutter beside the cell content). Retemplate this key to change how the
        /// data-annotation error badge is arranged within a cell.
        /// </summary>
        public static ComponentResourceKey ValidationCellPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(ValidationCellPresenter));

        /// <summary>
        /// Default style for the <see cref="ContextMenu"/> shell — rounded white surface with
        /// soft border and shadow. Applied by the four shared SearchDataGrid context menus
        /// (column header, cell, row header, grid body) and available for consumer-defined
        /// context menus that should match the library's look.
        /// </summary>
        public static ComponentResourceKey PrimitivesContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesContextMenu));

        /// <summary>
        /// Default style for <see cref="MenuItem"/>s hosted inside an SDG context menu —
        /// icon column, header, gesture text, and a chevron when the item carries a submenu.
        /// Wired up as the <c>ItemContainerStyle</c> of <see cref="PrimitivesContextMenu"/>.
        /// </summary>
        public static ComponentResourceKey PrimitivesMenuItem { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesMenuItem));

        /// <summary>
        /// Style for an <see cref="Icon"/> hosted in a <see cref="MenuItem"/>'s icon slot — dims the
        /// glyph to 35% opacity while the owning menu item is disabled, so every menu icon tracks the
        /// item's enabled state. Trigger-only (no sizing); the icon element sets its own Width/Height,
        /// or a sized glyph layers a <c>BasedOn</c> style on top (see the summary-function icon styles).
        /// </summary>
        public static ComponentResourceKey PrimitivesMenuItemIcon { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesMenuItemIcon));

        /// <summary>
        /// Default chrome for every <see cref="Window"/> the library opens (Filter Editor,
        /// group summary editor, Column Chooser) — borderless DWM window with rounded corners,
        /// drop shadow, accent border, and a 30px caption with taskbar-aware Min / Max / Close
        /// buttons. Also available to consumer windows that should match the library's look;
        /// the caption buttons invoke <see cref="System.Windows.SystemCommands"/>, so a consumer
        /// window needs those command bindings registered (the library wires its own hosts).
        /// </summary>
        public static ComponentResourceKey PrimitivesWindow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesWindow));

        #endregion

        #region Editors

        /// <summary>
        /// Default style for <see cref="WWBaseEdit"/> — the shared editor chrome (border, background,
        /// padding, focus accent, disabled visual, content host, decoration-button slot). The
        /// concrete editors (<see cref="WWTextEdit"/>, …) point their default style key at
        /// <see cref="WWBaseEdit"/>, so retheming this one key rechromes every editor at once.
        /// </summary>
        public static ComponentResourceKey EditorsBaseEdit { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(EditorsBaseEdit));

        #endregion

        #region Grid

        /// <summary>Default style for the top-level <see cref="SearchDataGrid"/> control.</summary>
        public static ComponentResourceKey GridSearchDataGrid { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGrid));

        /// <summary>Default style applied to <see cref="DataGridCell"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey GridSearchDataGridCell { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridCell));

        /// <summary>Default style applied to <see cref="DataGridRow"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey GridSearchDataGridRow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridRow));

        /// <summary>Default style applied to <see cref="DataGridRowHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridRowHeader { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridRowHeader));

        /// <summary>
        /// Default style for the <see cref="RowEditPresenter"/> — the bright, column-aligned editor
        /// strip (plus Update / Cancel action bar) shown over the row open in full-row edit mode
        /// (<see cref="SearchDataGrid.RowEditTrigger"/>).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridRowEditPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridRowEditPresenter));

        /// <summary>
        /// Default style for the <see cref="EditFormPresenter"/> — the caption/editor form (plus
        /// Update / Cancel action bar) shown for the row open in full-row edit mode when
        /// <see cref="SearchDataGrid.EditFormShowMode"/> is not <see cref="EditFormShowMode.None"/>.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridEditFormPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridEditFormPresenter));

        /// <summary>
        /// <see cref="DataTemplate"/> the grid assigns to <see cref="DataGrid.RowDetailsTemplate"/>
        /// to host an <see cref="EditFormPresenter"/> in the editing row's details area. The
        /// template's <c>DataContext</c> is the row item; it wires <c>OwnerGrid</c>, the editing
        /// item, and the grid's <c>EditFormTemplate</c> / caption onto the presenter.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridEditFormRowDetailsTemplate { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridEditFormRowDetailsTemplate));

        /// <summary>
        /// <see cref="ControlTemplate"/> for an in-body group-header row — the full-width header
        /// rendered in place of cells when a <see cref="SearchDataGridRow.IsGroupHeader"/> container
        /// materializes. Renders the row-header gutter, per-level indent, chevron, column-prefixed
        /// value, and count chip, reading the level / owning column straight off the
        /// <see cref="GroupHeaderRow"/>. The default <c>GridSearchDataGridRow</c> style swaps to this
        /// via an <c>IsGroupHeader</c> trigger.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupHeaderRow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupHeaderRow));

        /// <summary>
        /// Default style applied to <see cref="GroupSummaryCell"/> — one per visible column in a
        /// group header row's aligned-summary layer
        /// (<see cref="SearchDataGrid.GroupSummaryDisplayMode"/> = AlignByColumns).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupSummaryCell { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupSummaryCell));

        /// <summary>Default style applied to <see cref="GroupSummaryCellsPresenter"/> — the aligned-summary layer inside a group header row.</summary>
        public static ComponentResourceKey GridSearchDataGridGroupSummaryCellsPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupSummaryCellsPresenter));

        /// <summary>
        /// Default style applied to <see cref="FixedGroupSummaryCellsPresenter"/> — the
        /// aligned-summary layer inside a pinned-strip entry. Collapsed unless
        /// <see cref="SearchDataGrid.GroupSummaryDisplayMode"/> is AlignByColumns.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedGroupSummaryCellsPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridFixedGroupSummaryCellsPresenter));

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
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupHeaderChevronButton));

        /// <summary>Default style applied to <see cref="DataGridColumnHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeader { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridColumnHeader));

        /// <summary>
        /// Default <see cref="Style"/> for the <see cref="GroupPanel"/> — the strip above the
        /// column headers that shows one pill per grouped column.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupPanel { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupPanel));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> for the empty area of the
        /// <see cref="GroupPanel"/> — Expand All / Collapse All / Clear Grouping.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupPanelContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupPanelContextMenu));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> for an individual group-panel pill — Full Expand
        /// / Full Collapse on top, then the full column-header menu mirror (Copy / Sort / Best Fit
        /// / Hide / Pin / Filter operations). <c>x:Shared="False"</c> so every pill gets its own.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupPillContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupPillContextMenu));

        /// <summary>
        /// Default <see cref="Style"/> for the <see cref="FixedGroupHeadersPresenter"/> — the
        /// sticky strip pinned to the top of the data area that mirrors the active group chain
        /// of the topmost visible row when <see cref="SearchDataGrid.AllowFixedGroups"/> is true.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedGroupHeadersPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridFixedGroupHeadersPresenter));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> attached to every pinned header in the sticky
        /// strip — Expand / Collapse this group, Expand / Collapse all at this level, Ungroup at
        /// this level. Sibling to <see cref="GridSearchDataGridGroupHeaderContextMenu"/> but
        /// commands take a <see cref="FixedGroupHeaderEntry"/> instead of an <see cref="Expander"/>
        /// because the strip lives outside the rows-presenter visual subtree.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedGroupHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridFixedGroupHeaderContextMenu));

        /// <summary>
        /// Right-click <see cref="ContextMenu"/> attached to the in-body group-header rows —
        /// Expand / Collapse this group, Expand / Collapse all at this level, Ungroup at this level.
        /// Its commands take a <c>GroupHeaderRow</c> (the row template's DataContext); the
        /// <see cref="GridSearchDataGridFixedGroupHeaderContextMenu"/> sibling takes a
        /// <see cref="FixedGroupHeaderEntry"/> for the pinned strip.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource (declared with
        /// <c>x:Shared="False"</c> so every consumer gets its own instance) that hosts the
        /// column-header right-click menu — sort / best-fit / hide / pin / clear-filter. The
        /// header style and the column chooser ListBoxItem style both reference this key so
        /// the chooser rows behave like header surfaces. The menu's <c>DataContext</c> is set
        /// to a <see cref="Commands.ContextMenuContext"/> by the consumer.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridColumnHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the cell right-click menu —
        /// copy / copy-with-headers. The menu's <c>DataContext</c> is set to a
        /// <see cref="Commands.ContextMenuContext"/> by the consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridCellContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridCellContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the row-header right-click menu —
        /// copy / copy-with-headers. The menu's <c>DataContext</c> is set to a
        /// <see cref="Commands.ContextMenuContext"/> by the consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridRowHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridRowHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the grid-body right-click menu —
        /// filter operations, column profiles, export, and layout actions. The menu's
        /// <c>DataContext</c> is set to a <see cref="Commands.ContextMenuContext"/> by the
        /// consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridContextMenu));

        /// <summary>Default style for the select-all corner button in the SearchDataGrid header.</summary>
        public static ComponentResourceKey GridSelectAllButton { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSelectAllButton));

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
            new ComponentResourceKey(typeof(ThemeKeys), nameof(ColumnChooserSectionItem));

        #endregion

        #region ColumnFilterPopup

        /// <summary>Default style for the <see cref="ColumnFilterPopup"/> popup.</summary>
        public static ComponentResourceKey ColumnFilterPopup { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(ColumnFilterPopup));

        #endregion

        #region FilterSummaryPanel

        /// <summary>Default style for the <see cref="FilterSummaryPanel"/> chip strip below the grid.</summary>
        public static ComponentResourceKey FilterSummaryPanel { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterSummaryPanel));

        #endregion

        #region FilterTokens

        /// <summary>Visual leading bracket for a grouped token run.</summary>
        public static ComponentResourceKey FilterTokensOpenBracket { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensOpenBracket));

        /// <summary>Column-name chip — orange pill carrying the column header text.</summary>
        public static ComponentResourceKey FilterTokensColumnName { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensColumnName));

        /// <summary>Search-type label that sits between the column chip and the value chip(s).</summary>
        public static ComponentResourceKey FilterTokensSearchType { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensSearchType));

        /// <summary>Unary search-type chip (IsNull / IsToday / AboveAverage / …) with a click-to-confirm remove overlay.</summary>
        public static ComponentResourceKey FilterTokensUnarySearchType { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensUnarySearchType));

        /// <summary>Single value chip (the green pill) with a click-to-confirm remove overlay.</summary>
        public static ComponentResourceKey FilterTokensValue { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensValue));

        /// <summary>Inline operator label between two value chips (e.g. the "and" in "between X and Y").</summary>
        public static ComponentResourceKey FilterTokensValueOperator { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensValueOperator));

        /// <summary>Visual trailing bracket for a grouped token run.</summary>
        public static ComponentResourceKey FilterTokensCloseBracket { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensCloseBracket));

        /// <summary>Logical connector chip between search-template groups — clickable to toggle AND/OR.</summary>
        public static ComponentResourceKey FilterTokensGroupLogicalConnector { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensGroupLogicalConnector));

        /// <summary>Logical connector chip between search templates within a group — clickable to toggle AND/OR.</summary>
        public static ComponentResourceKey FilterTokensTemplateLogicalConnector { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensTemplateLogicalConnector));

        /// <summary>Hover-revealed remove button that detaches an entire filter run.</summary>
        public static ComponentResourceKey FilterTokensRemoveAction { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterTokensRemoveAction));

        #endregion

        #region FilterEditorDialog

        /// <summary>Default style for the modal <see cref="FilterEditorDialog"/> window's content control.</summary>
        public static ComponentResourceKey FilterEditorDialog { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorDialog));

        /// <summary>Default style for the <see cref="ColumnNameTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorColumnNameToken { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorColumnNameToken));

        /// <summary>Default style for the <see cref="SearchTypeTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorSearchTypeToken { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorSearchTypeToken));

        /// <summary>Default style for the <see cref="ValueTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorValueToken { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorValueToken));

        /// <summary>Default style for the <see cref="GroupOperatorChip"/> inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorGroupOperatorChip { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorGroupOperatorChip));

        /// <summary>
        /// DataTemplate for a single condition row in the recursive Filter Editor tree —
        /// column chip + search-type chip + value chip + hover-revealed remove button.
        /// Looked up by <see cref="FilterEditorNodeTemplateSelector"/> at runtime.
        /// </summary>
        public static ComponentResourceKey FilterEditorConditionRow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorConditionRow));

        /// <summary>
        /// DataTemplate for a group node in the recursive Filter Editor tree — operator
        /// chip + add-popup toggle + warning banner + recursive child list. Looked up by
        /// <see cref="FilterEditorNodeTemplateSelector"/> at runtime.
        /// </summary>
        public static ComponentResourceKey FilterEditorGroup { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorGroup));

        #endregion

        #region FilterRow

        /// <summary>Default style for the <see cref="SearchTypeSelector"/> per-column mode picker.</summary>
        public static ComponentResourceKey FilterRowSearchTypeSelector { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterRowSearchTypeSelector));

        /// <summary>Default style for the <see cref="FilterRowPresenter"/> pinned filter row.</summary>
        public static ComponentResourceKey FilterRowPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterRowPresenter));

        /// <summary>Default style for the per-column <see cref="ColumnFilterControl"/>.</summary>
        public static ComponentResourceKey FilterRowColumnFilterControl { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterRowColumnFilterControl));

        #endregion

        #region Total Summary Row

        /// <summary>Default style for the <see cref="TotalSummaryRowPresenter"/> pinned total summary row.</summary>
        public static ComponentResourceKey GridSearchDataGridTotalSummaryRow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridTotalSummaryRow));

        /// <summary>Default style for the per-column <see cref="TotalSummaryCell"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridTotalSummaryCell { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridTotalSummaryCell));

        /// <summary>
        /// Right-click <see cref="System.Windows.Controls.ContextMenu"/> for a
        /// <see cref="TotalSummaryCell"/> — the runtime summary picker (Count / Sum / Min /
        /// Max / Average toggles + Clear).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridTotalSummaryCellContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridTotalSummaryCellContextMenu));

        /// <summary>Default style for the <see cref="WWControls.Wpf.GroupSummaryEditor"/> ("View Totals") dialog body.</summary>
        public static ComponentResourceKey GroupSummaryEditor { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GroupSummaryEditor));

        /// <summary>
        /// Default style for the <see cref="WWControls.Wpf.SummaryTextStyleEditor"/>
        /// dialog body — the Prefix / Value / Suffix text-styling sub-editor opened from the
        /// summary editor's Order tab.
        /// </summary>
        public static ComponentResourceKey SummaryTextStyleEditor { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(SummaryTextStyleEditor));

        /// <summary>
        /// Right-click <see cref="System.Windows.Controls.ContextMenu"/> for the fixed total
        /// summary panel — Count (grid row count toggle) + Customize….
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridFixedTotalSummaryContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridFixedTotalSummaryContextMenu));

        /// <summary>Full-width group footer row template (one <see cref="GroupFooterCell"/> per column).</summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterRow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupFooterRow));

        /// <summary>Default style for the footer cells host (<see cref="GroupFooterCellsPresenter"/>).</summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterCellsPresenter { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupFooterCellsPresenter));

        /// <summary>Default style for the per-column <see cref="GroupFooterCell"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterCell { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupFooterCell));

        /// <summary>
        /// Right-click <see cref="System.Windows.Controls.ContextMenu"/> for a
        /// <see cref="GroupFooterCell"/> — the runtime footer summary picker (Count / Sum / Min /
        /// Max / Average toggles + Clear + Customize).
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupFooterCellContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupFooterCellContextMenu));

        #endregion
    }
}
