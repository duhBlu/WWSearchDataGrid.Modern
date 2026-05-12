using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Typed resource keys for the public default styles of the WPF SearchDataGrid library —
    /// the custom controls, their primary sub-styles, and the primitive control styles consumed
    /// by both the library's own templates and (optionally) consumer XAML. Each
    /// <see cref="ComponentResourceKey"/> identifies a default <see cref="Style"/> that the
    /// library declares in <c>Themes/</c>; consumers retheme by redefining the style under the
    /// same key:
    /// <code>
    /// &lt;Style x:Key="{x:Static sdg:SdgThemeKeys.SearchDataGrid}"
    ///        TargetType="{x:Type sdg:SearchDataGrid}"
    ///        BasedOn="{StaticResource {x:Static sdg:SdgThemeKeys.SearchDataGrid}}"&gt;
    ///     &lt;Setter Property="HeadersVisibility" Value="Column" /&gt;
    /// &lt;/Style&gt;
    /// </code>
    /// </summary>
    /// <remarks>
    /// <see cref="ComponentResourceKey"/> participates in WPF's themeing protocol: the assembly's
    /// <c>[ThemeInfo]</c> attribute (declared in <c>Properties/AssemblyInfo.cs</c>) lets
    /// <see cref="FrameworkElement.FindResource(object)"/> walk into <c>Themes/Generic.xaml</c>
    /// of this assembly to resolve these keys, even when the consumer hasn't merged the
    /// dictionary explicitly. That's what makes drop-in defaults work without ceremony, and why
    /// the keys live in CLR (not string) namespace — consumer resource scopes don't pick up the
    /// names by accident.
    /// <para>
    /// Editor element styles consumed inside the cell-template builders live separately in
    /// <see cref="EditSettingsThemeKeys"/> — same pattern, narrower audience.
    /// </para>
    /// </remarks>
    public static class SdgThemeKeys
    {
        // ─── Custom controls ───────────────────────────────────────────────────────────

        /// <summary>Default style for the top-level <see cref="SearchDataGrid"/> control.</summary>
        public static ComponentResourceKey SearchDataGrid { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchDataGrid));

        /// <summary>Default style applied to <see cref="System.Windows.Controls.DataGridCell"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey SearchDataGridCell { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchDataGridCell));

        /// <summary>Default style applied to <see cref="System.Windows.Controls.DataGridRow"/> within a SearchDataGrid.</summary>
        public static ComponentResourceKey SearchDataGridRow { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchDataGridRow));

        /// <summary>Default style applied to <see cref="System.Windows.Controls.Primitives.DataGridRowHeader"/>.</summary>
        public static ComponentResourceKey SearchDataGridRowHeader { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchDataGridRowHeader));

        /// <summary>Default style applied to <see cref="System.Windows.Controls.Primitives.DataGridColumnHeader"/>.</summary>
        public static ComponentResourceKey SearchDataGridColumnHeader { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchDataGridColumnHeader));

        /// <summary>Default style for the select-all corner button in the SearchDataGrid header.</summary>
        public static ComponentResourceKey SelectAllButton { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SelectAllButton));

        /// <summary>Default style for the per-column <see cref="ColumnSearchBox"/>.</summary>
        public static ComponentResourceKey ColumnSearchBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ColumnSearchBox));

        /// <summary>Default style for the prefix-help <see cref="System.Windows.Controls.ToolTip"/> shown over a ColumnSearchBox.</summary>
        public static ComponentResourceKey SearchPrefixTooltip { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchPrefixTooltip));

        /// <summary>Default style for the <see cref="ColumnFilterEditor"/> popup.</summary>
        public static ComponentResourceKey ColumnFilterEditor { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ColumnFilterEditor));

        /// <summary>Default style applied to the <see cref="System.Windows.Window"/> that hosts the column chooser dialog.</summary>
        public static ComponentResourceKey ColumnChooserWindow { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ColumnChooserWindow));

        /// <summary>Default style for the <see cref="FilterPanel"/> chip strip below the grid.</summary>
        public static ComponentResourceKey FilterPanel { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(FilterPanel));

        /// <summary>Default style for the custom <see cref="SearchTextBox"/> primitive.</summary>
        public static ComponentResourceKey SearchTextBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(SearchTextBox));

        /// <summary>Default style for the custom <see cref="NumericUpDown"/> primitive.</summary>
        public static ComponentResourceKey NumericUpDown { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(NumericUpDown));

        // ─── Built-in primitives, themed for the SDG look ──────────────────────────────

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.Button"/> style used inside library templates and available to consumers.</summary>
        public static ComponentResourceKey Button { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(Button));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.CheckBox"/> style.</summary>
        public static ComponentResourceKey CheckBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(CheckBox));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.ComboBox"/> style.</summary>
        public static ComponentResourceKey ComboBox { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ComboBox));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.Primitives.ScrollBar"/> style.</summary>
        public static ComponentResourceKey ScrollBar { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ScrollBar));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.TabControl"/> style.</summary>
        public static ComponentResourceKey TabControl { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(TabControl));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.TabItem"/> style.</summary>
        public static ComponentResourceKey TabItem { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(TabItem));

        /// <summary>Sdg-themed resize <see cref="System.Windows.Controls.Primitives.Thumb"/> used by column splitters and resizers.</summary>
        public static ComponentResourceKey ResizeThumb { get; } =
            new ComponentResourceKey(typeof(SdgThemeKeys), nameof(ResizeThumb));
    }
}
