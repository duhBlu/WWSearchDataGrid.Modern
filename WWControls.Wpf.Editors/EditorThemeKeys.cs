using System.Windows;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Typed resource keys for the editor controls' styles — each editor control's own chrome and
    /// the display / edit element styles the editor templates and <c>EditSettings</c> adapters apply.
    /// Consumers retheme an editor element by redefining a resource under the same key:
    /// <code>
    /// &lt;Style x:Key="{x:Static sdg:EditorThemeKeys.EditTextBox}" TargetType="{x:Type TextBox}"
    ///        BasedOn="{StaticResource {x:Static sdg:EditorThemeKeys.EditTextBox}}"&gt;
    ///     &lt;Setter Property="FontFamily" Value="Consolas" /&gt;
    /// &lt;/Style&gt;
    /// </code>
    /// Owned by the Editors assembly so the editors are self-theming (the keyed styles live in this
    /// assembly's <c>Themes/Generic.xaml</c> slice, reachable via <c>[ThemeInfo]</c>).
    /// </summary>
    public static class EditorThemeKeys
    {
        /// <summary>
        /// Default style for <see cref="WWTextEdit"/> — the editor's own chrome (border, background,
        /// padding, focus accent, disabled visual) hosting its inner <c>PART_TextBox</c>. The border
        /// draws per <see cref="WWBaseEdit.ShowBorder"/>; retheme this key to restyle the text editor.
        /// </summary>
        public static ComponentResourceKey TextEdit { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(TextEdit));

        /// <summary>
        /// Default style for <see cref="WWSpinEdit"/> — the editor's own chrome hosting its inner
        /// right-aligned <c>PART_TextBox</c> plus the <c>PART_UpButton</c> / <c>PART_DownButton</c>
        /// spinner column.
        /// </summary>
        public static ComponentResourceKey SpinEdit { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(SpinEdit));

        /// <summary>
        /// Default style for <see cref="WWComboEdit"/> — the editor's own chrome hosting the flat
        /// inner <c>PART_ComboBox</c> (whose own look comes from <see cref="EditComboBox"/>).
        /// </summary>
        public static ComponentResourceKey ComboEdit { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(ComboEdit));

        /// <summary>
        /// Default style for <see cref="WWDateEdit"/> — the editor's own chrome hosting the segmented
        /// date editor as <c>PART_Editor</c>.
        /// </summary>
        public static ComponentResourceKey DateEdit { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(DateEdit));

        /// <summary>
        /// Default style for <see cref="WWCheckEdit"/> — a borderless chrome hosting the interactive
        /// <c>PART_CheckBox</c> (a checkbox is a glyph, so it carries no border).
        /// </summary>
        public static ComponentResourceKey CheckEdit { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(CheckEdit));

        /// <summary>Default style for the read-only TextBlock used by Text / ComboBox / Spin / Date display templates.</summary>
        public static ComponentResourceKey DisplayTextBlock { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(DisplayTextBlock));

        /// <summary>Right-aligned variant of <see cref="DisplayTextBlock"/> used by the spin editor's display template.</summary>
        public static ComponentResourceKey DisplayNumericTextBlock { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(DisplayNumericTextBlock));

        /// <summary>Default style for the CheckBox used by the checkbox editor (both cell and editing template).</summary>
        public static ComponentResourceKey DisplayCheckBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(DisplayCheckBox));

        /// <summary>
        /// Default style for a plain edit TextBox — used by the masked <c>PART_TextBox</c> inside
        /// <see cref="SegmentedDateTimeEditor"/> and by the default filter-row text editor.
        /// </summary>
        public static ComponentResourceKey EditTextBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(EditTextBox));

        /// <summary>Default style for the flat ComboBox used by the combo editor's edit template.</summary>
        public static ComponentResourceKey EditComboBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(EditComboBox));

        /// <summary>
        /// Default style for the calendar dropdown ToggleButton in the date editor. Applied in both
        /// the <see cref="SegmentedDateTimeEditor"/> template and the date editor's display-mode
        /// ShowAlways indicator; the glyph lives in the style's ControlTemplate.
        /// </summary>
        public static ComponentResourceKey EditDateDropDownButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(EditDateDropDownButton));

        /// <summary>
        /// Default style for the dropdown ToggleButton used by the combo editor — the chevron
        /// sub-element inside the <see cref="EditComboBox"/> template and the display-mode ShowAlways
        /// indicator; the glyph lives in the style's ControlTemplate.
        /// </summary>
        public static ComponentResourceKey EditComboBoxDropDownButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(EditComboBoxDropDownButton));

        /// <summary>
        /// Default style for the up/down RepeatButtons used by the spin editor's display and edit
        /// templates. The button's Content is rendered as a Fluent-icon glyph by the style's template.
        /// </summary>
        public static ComponentResourceKey SpinButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(SpinButton));
    }
}
