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
    /// &lt;Style x:Key="{x:Static sdg:SdgThemeKeys.GridSearchDataGrid}"
    ///        TargetType="{x:Type sdg:SearchDataGrid}"
    ///        BasedOn="{StaticResource {x:Static sdg:SdgThemeKeys.GridSearchDataGrid}}" /&gt;
    /// </code>
    /// Editor element styles live separately in <see cref="EditSettingsThemeKeys"/>.
    /// </summary>
    public static class SdgThemeKeys
    {
        #region Primitives

        /// <summary>Sdg-themed <see cref="Button"/> style used inside library templates and available to consumers.</summary>
        public static ComponentResourceKey PrimitivesButton { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesButton));

        /// <summary>Sdg-themed <see cref="CheckBox"/> style.</summary>
        public static ComponentResourceKey PrimitivesCheckBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesCheckBox));

        /// <summary>Sdg-themed <see cref="ComboBox"/> style.</summary>
        public static ComponentResourceKey PrimitivesComboBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesComboBox));

        /// <summary>Sdg-themed <see cref="Primitives.ScrollBar"/> style.</summary>
        public static ComponentResourceKey PrimitivesScrollBar { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesScrollBar));

        /// <summary>Sdg-themed <see cref="TabControl"/> style.</summary>
        public static ComponentResourceKey PrimitivesTabControl { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesTabControl));

        /// <summary>Sdg-themed <see cref="TabItem"/> style.</summary>
        public static ComponentResourceKey PrimitivesTabItem { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesTabItem));

        /// <summary>Sdg-themed resize <see cref="Thumb"/> used by column splitters and resizers.</summary>
        public static ComponentResourceKey PrimitivesResizeThumb { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesResizeThumb));

        /// <summary>Default style for the custom <see cref="SearchTextBox"/> primitive.</summary>
        public static ComponentResourceKey PrimitivesSearchTextBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesSearchTextBox));

        /// <summary>Default style for the custom <see cref="NumericUpDown"/> primitive.</summary>
        public static ComponentResourceKey PrimitivesNumericUpDown { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesNumericUpDown));

        /// <summary>
        /// Default style for the <see cref="ContextMenu"/> shell — rounded white surface with
        /// soft border and shadow. Applied by the four shared SearchDataGrid context menus
        /// (column header, cell, row header, grid body) and available for consumer-defined
        /// context menus that should match the library's look.
        /// </summary>
        public static ComponentResourceKey PrimitivesContextMenu { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesContextMenu));

        /// <summary>
        /// Default style for <see cref="MenuItem"/>s hosted inside an SDG context menu —
        /// icon column, header, gesture text, and a chevron when the item carries a submenu.
        /// Wired up as the <c>ItemContainerStyle</c> of <see cref="PrimitivesContextMenu"/>.
        /// </summary>
        public static ComponentResourceKey PrimitivesMenuItem { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(PrimitivesMenuItem));

        #endregion

        #region Grid

        /// <summary>Default style for the top-level <see cref="SearchDataGrid"/> control.</summary>
        public static ComponentResourceKey GridSearchDataGrid { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGrid));

        /// <summary>Default style applied to <see cref="DataGridCell"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey GridSearchDataGridCell { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridCell));

        /// <summary>Default style applied to <see cref="DataGridRow"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey GridSearchDataGridRow { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridRow));

        /// <summary>Default style applied to <see cref="DataGridRowHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridRowHeader { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridRowHeader));

        /// <summary>Default style applied to <see cref="DataGridColumnHeader"/>.</summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeader { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridColumnHeader));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource (declared with
        /// <c>x:Shared="False"</c> so every consumer gets its own instance) that hosts the
        /// column-header right-click menu — sort / best-fit / hide / pin / clear-filter. The
        /// header style and the column chooser ListBoxItem style both reference this key so
        /// the chooser rows behave like header surfaces. The menu's <c>DataContext</c> is set
        /// to a <see cref="Commands.ContextMenuContext"/> by the consumer.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridColumnHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridColumnHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the cell right-click menu —
        /// copy / copy-with-headers. The menu's <c>DataContext</c> is set to a
        /// <see cref="Commands.ContextMenuContext"/> by the consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridCellContextMenu { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridCellContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the row-header right-click menu —
        /// copy / copy-with-headers. The menu's <c>DataContext</c> is set to a
        /// <see cref="Commands.ContextMenuContext"/> by the consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridRowHeaderContextMenu { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridRowHeaderContextMenu));

        /// <summary>
        /// Shared <see cref="ContextMenu"/> resource for the grid-body right-click menu —
        /// filter operations, column profiles, export, and layout actions. The menu's
        /// <c>DataContext</c> is set to a <see cref="Commands.ContextMenuContext"/> by the
        /// consumer when the menu opens.
        /// </summary>
        public static ComponentResourceKey GridSearchDataGridContextMenu { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSearchDataGridContextMenu));

        /// <summary>Default style for the select-all corner button in the SearchDataGrid header.</summary>
        public static ComponentResourceKey GridSelectAllButton { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(GridSelectAllButton));

        #endregion

        #region ColumnChooser

        /// <summary>Default style applied to the <see cref="Window"/> that hosts the column chooser dialog.</summary>
        public static ComponentResourceKey ColumnChooserWindow { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ColumnChooserWindow));

        /// <summary>
        /// Default item-container style for the three section listboxes inside
        /// <see cref="ColumnChooser"/> (left-pinned, unpinned,
        /// right-pinned). Each row mirrors a column header — sort glyph, filter-active glyph,
        /// pin glyph — and reuses the shared column-header context menu so the chooser surface
        /// is consistent with right-clicks on the header band.
        /// </summary>
        public static ComponentResourceKey ColumnChooserSectionItem { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ColumnChooserSectionItem));

        #endregion

        #region ColumnFilterEditor

        /// <summary>Default style for the <see cref="ColumnFilterEditor"/> popup.</summary>
        public static ComponentResourceKey ColumnFilterEditor { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ColumnFilterEditor));

        #endregion

        #region FilterPanel

        /// <summary>Default style for the <see cref="FilterPanel"/> chip strip below the grid.</summary>
        public static ComponentResourceKey FilterPanel { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterPanel));

        #endregion

        #region FilterTokens

        /// <summary>Visual leading bracket for a grouped token run.</summary>
        public static ComponentResourceKey FilterTokensOpenBracket { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensOpenBracket));

        /// <summary>Column-name chip — orange pill carrying the column header text.</summary>
        public static ComponentResourceKey FilterTokensColumnName { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensColumnName));

        /// <summary>Search-type label that sits between the column chip and the value chip(s).</summary>
        public static ComponentResourceKey FilterTokensSearchType { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensSearchType));

        /// <summary>Unary search-type chip (IsNull / IsToday / AboveAverage / …) with a click-to-confirm remove overlay.</summary>
        public static ComponentResourceKey FilterTokensUnarySearchType { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensUnarySearchType));

        /// <summary>Single value chip (the green pill) with a click-to-confirm remove overlay.</summary>
        public static ComponentResourceKey FilterTokensValue { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensValue));

        /// <summary>Inline operator label between two value chips (e.g. the "and" in "between X and Y").</summary>
        public static ComponentResourceKey FilterTokensValueOperator { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensValueOperator));

        /// <summary>Visual trailing bracket for a grouped token run.</summary>
        public static ComponentResourceKey FilterTokensCloseBracket { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensCloseBracket));

        /// <summary>Logical connector chip between search-template groups — clickable to toggle AND/OR.</summary>
        public static ComponentResourceKey FilterTokensGroupLogicalConnector { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensGroupLogicalConnector));

        /// <summary>Logical connector chip between search templates within a group — clickable to toggle AND/OR.</summary>
        public static ComponentResourceKey FilterTokensTemplateLogicalConnector { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensTemplateLogicalConnector));

        /// <summary>Hover-revealed remove button that detaches an entire filter run.</summary>
        public static ComponentResourceKey FilterTokensRemoveAction { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterTokensRemoveAction));

        #endregion

        #region FilterEditor

        /// <summary>Default style for the modal <see cref="FilterEditor"/> window's content control.</summary>
        public static ComponentResourceKey FilterEditor { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditor));

        /// <summary>Default style applied to the <see cref="Window"/> that hosts the filter editor dialog.</summary>
        public static ComponentResourceKey FilterEditorWindow { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorWindow));

        /// <summary>Default style for the <see cref="ColumnNameTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorColumnNameToken { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorColumnNameToken));

        /// <summary>Default style for the <see cref="SearchTypeTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorSearchTypeToken { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorSearchTypeToken));

        /// <summary>Default style for the <see cref="ValueTokenEditor"/> chip inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorValueToken { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorValueToken));

        /// <summary>Default style for the <see cref="GroupOperatorChip"/> inside the Filter Editor.</summary>
        public static ComponentResourceKey FilterEditorGroupOperatorChip { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorGroupOperatorChip));

        /// <summary>
        /// DataTemplate for a single condition row in the recursive Filter Editor tree —
        /// column chip + search-type chip + value chip + hover-revealed remove button.
        /// Looked up by <see cref="FilterEditorNodeTemplateSelector"/> at runtime.
        /// </summary>
        public static ComponentResourceKey FilterEditorConditionRow { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorConditionRow));

        /// <summary>
        /// DataTemplate for a group node in the recursive Filter Editor tree — operator
        /// chip + add-popup toggle + warning banner + recursive child list. Looked up by
        /// <see cref="FilterEditorNodeTemplateSelector"/> at runtime.
        /// </summary>
        public static ComponentResourceKey FilterEditorGroup { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterEditorGroup));

        #endregion

        #region FilterRow

        /// <summary>Default style for the <see cref="SearchTypeSelector"/> per-column mode picker.</summary>
        public static ComponentResourceKey FilterRowSearchTypeSelector { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterRowSearchTypeSelector));

        /// <summary>Default style for the <see cref="AutoFilterRowPresenter"/> pinned filter row.</summary>
        public static ComponentResourceKey FilterRowAutoFilterRowPresenter { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterRowAutoFilterRowPresenter));

        /// <summary>Default style for the per-column <see cref="ColumnFilterControl"/>.</summary>
        public static ComponentResourceKey FilterRowColumnFilterControl { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterRowColumnFilterControl));

        #endregion
    }
}
