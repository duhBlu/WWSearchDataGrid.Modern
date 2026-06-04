using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.WPF
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

        /// <summary>Default style for the custom <see cref="SearchTextBox"/> primitive.</summary>
        public static ComponentResourceKey PrimitivesSearchTextBox { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(PrimitivesSearchTextBox));

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

        /// <summary>Default style applied to <see cref="DataGridColumnHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeader { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridColumnHeader));

        /// <summary>
        /// Default <see cref="System.Windows.Controls.GroupStyle"/> attached to the grid the first
        /// time it is grouped — an expander group header showing the group value and item count.
        /// The grid pulls it by this key and adds it to its <c>GroupStyle</c> collection lazily;
        /// a consumer that supplies its own <c>GroupStyle</c> opts out.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupStyle { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupStyle));

        /// <summary>
        /// <see cref="ControlTemplate"/> applied to every group <see cref="Expander"/>. Renders
        /// the row-header gutter, chevron, and group header, and hosts the content via
        /// <see cref="GroupExpansionAnimator"/>. The animator drives a Height animation on the
        /// content host when <see cref="SearchDataGrid.UseGroupExpansionAnimation"/> is <c>true</c>
        /// and snaps the host's Visibility instantly when it is <c>false</c>, so this single
        /// template serves both the animated and non-animated modes.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupExpanderTemplate { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupExpanderTemplate));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource (declared with <c>x:Shared="False"</c> so
        /// every Expander gets its own instance) that hosts the group-header right-click menu —
        /// Expand / Collapse this group, Expand / Collapse all at this level, Ungroup at this
        /// level. Attached to each group Expander via the default <c>GroupExpanderStyle</c>.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridGroupHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(GridSearchDataGridGroupHeaderContextMenu));

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

        /// <summary>Default style applied to the <see cref="Window"/> that hosts the column chooser dialog.</summary>
        public static ComponentResourceKey ColumnChooserWindow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(ColumnChooserWindow));

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

        /// <summary>Default style applied to the <see cref="Window"/> that hosts the filter editor dialog.</summary>
        public static ComponentResourceKey FilterEditorWindow { get; } =
            new ComponentResourceKey(typeof(ThemeKeys), nameof(FilterEditorWindow));

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
    }
}
