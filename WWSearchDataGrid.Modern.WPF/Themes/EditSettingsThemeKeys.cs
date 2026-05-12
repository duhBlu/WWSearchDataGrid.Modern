using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Typed resource keys for the editor element styles defined in
    /// <c>Themes/EditSettings.xaml</c>. Each <see cref="ComponentResourceKey"/> identifies a
    /// default style applied to the display- or edit-mode element a <see cref="BaseEditSettings"/>
    /// builds for a cell. Consumers override an editor style globally by redefining the resource
    /// under the same key:
    /// <code>
    /// &lt;Style x:Key="{x:Static sdg:EditSettingsThemeKeys.EditTextBox}"
    ///        TargetType="{x:Type TextBox}"
    ///        BasedOn="{StaticResource {x:Static sdg:EditSettingsThemeKeys.EditTextBox}}"&gt;
    ///     &lt;Setter Property="FontFamily" Value="Consolas" /&gt;
    /// &lt;/Style&gt;
    /// </code>
    /// Per-column overrides via <see cref="BaseEditSettings.DisplayStyle"/> /
    /// <see cref="BaseEditSettings.EditorStyle"/> still beat the keyed default.
    /// </summary>
    /// <remarks>
    /// <see cref="ComponentResourceKey"/> participates in WPF's themeing protocol: the assembly's
    /// <c>[ThemeInfo]</c> attribute (declared in <c>Properties/AssemblyInfo.cs</c>) lets
    /// <see cref="FrameworkElement.FindResource(object)"/> walk into <c>Themes/Generic.xaml</c>
    /// of this assembly to resolve these keys, even when the consumer hasn't merged the
    /// dictionary explicitly. That removes the need for the runtime merge shim that previously
    /// lived in <c>SearchDataGrid</c>'s constructor.
    /// </remarks>
    public static class EditSettingsThemeKeys
    {
        /// <summary>Default style for the read-only TextBlock used by Text / ComboBox / Spin / Date display templates.</summary>
        public static ComponentResourceKey DisplayTextBlock { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(DisplayTextBlock));

        /// <summary>Right-aligned variant of <see cref="DisplayTextBlock"/> used by <see cref="SpinEditSettings"/>'s display template.</summary>
        public static ComponentResourceKey DisplayNumericTextBlock { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(DisplayNumericTextBlock));

        /// <summary>Default style for the CheckBox used by <see cref="CheckBoxEditSettings"/>. Applied to both the cell template and the editing template (the editing template is a no-op for checkbox cells — the display CheckBox is interactive).</summary>
        public static ComponentResourceKey DisplayCheckBox { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(DisplayCheckBox));

        /// <summary>Default style for the TextBox used by <see cref="TextEditSettings"/>'s edit template.</summary>
        public static ComponentResourceKey EditTextBox { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(EditTextBox));

        /// <summary>Right-aligned variant of <see cref="EditTextBox"/> used by <see cref="SpinEditSettings"/>'s edit template.</summary>
        public static ComponentResourceKey EditNumericTextBox { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(EditNumericTextBox));

        /// <summary>Default style for the ComboBox used by <see cref="ComboBoxEditSettings"/>'s edit template.</summary>
        public static ComponentResourceKey EditComboBox { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(EditComboBox));

        /// <summary>
        /// Default style for the calendar dropdown ToggleButton in <see cref="DateEditSettings"/>.
        /// The same style is applied in both surfaces — by the <c>SegmentedDateTimeEditor</c> template
        /// (functional, opens the popup) and by <see cref="DateEditSettings"/>'s display template
        /// (visual indicator when <c>EditorButtonShowMode</c> is <c>ShowAlways</c>). The calendar
        /// glyph lives inside the style's ControlTemplate, so overriding this one key changes the
        /// button's appearance — including the icon — in both places.
        /// </summary>
        public static ComponentResourceKey EditDateDropDownButton { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(EditDateDropDownButton));

        /// <summary>
        /// Default style for the dropdown ToggleButton used by <see cref="ComboBoxEditSettings"/>.
        /// Applied in both surfaces — as the chevron sub-element inside the <see cref="EditComboBox"/>
        /// template (visual only — the popup toggle is a separate full-area ToggleButton) and as the
        /// "ShowAlways" indicator built by <see cref="ComboBoxEditSettings"/>'s display template.
        /// The chevron glyph lives in the style's ControlTemplate, so overriding this one key
        /// changes the button's appearance — including the icon — in both places.
        /// </summary>
        public static ComponentResourceKey EditComboBoxDropDownButton { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(EditComboBoxDropDownButton));

        /// <summary>
        /// Default style for the up/down RepeatButtons used by <see cref="SpinEditSettings"/>'s
        /// display and edit templates. The button's <c>Content</c> is rendered as a Fluent-icon
        /// glyph by the style's template — callers pass the up or down chevron string in via
        /// <see cref="System.Windows.Controls.ContentControl.Content"/>.
        /// </summary>
        public static ComponentResourceKey SpinButton { get; } =
            new ComponentResourceKey(typeof(EditSettingsThemeKeys), nameof(SpinButton));
    }
}
