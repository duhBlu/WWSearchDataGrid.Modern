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
        /// Default style for <see cref="WWTextBox"/> — the editor's own chrome (border, background,
        /// padding, focus accent, disabled visual) hosting its inner <c>PART_TextBox</c>. The border
        /// draws per <see cref="WWEditorBase.ShowBorder"/>; retheme this key to restyle the text editor.
        /// </summary>
        public static ComponentResourceKey TextBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(TextBox));

        /// <summary>
        /// Default style for <see cref="WWNumericUpDown"/> — the editor's own chrome hosting its inner
        /// right-aligned <c>PART_TextBox</c> plus the <c>PART_UpButton</c> / <c>PART_DownButton</c>
        /// spinner column.
        /// </summary>
        public static ComponentResourceKey NumericUpDown { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(NumericUpDown));

        /// <summary>
        /// Default style for <see cref="WWComboBox"/> — a single self-contained template owning
        /// every part (chrome frame, selection box, editable text, chevron, popup).
        /// </summary>
        public static ComponentResourceKey ComboBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(ComboBox));

        /// <summary>
        /// Default style for <see cref="WWComboBoxItem"/> — the container <see cref="WWComboBox"/>
        /// generates for its popup rows: dropdown-row visuals plus the selection glyph column
        /// (checkbox / radio per <see cref="WWComboBox.SelectionMode"/>) and the built-in
        /// highlight rendering used during incremental filtering.
        /// </summary>
        public static ComponentResourceKey ComboBoxItem { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(ComboBoxItem));

        /// <summary>
        /// Default style for <see cref="WWDatePicker"/> — the editor's own chrome hosting the segmented
        /// date editor as <c>PART_Editor</c>.
        /// </summary>
        public static ComponentResourceKey DatePicker { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(DatePicker));

        /// <summary>
        /// Default style for <see cref="WWCheckBox"/> — a borderless chrome hosting the interactive
        /// <c>PART_CheckBox</c> (a checkbox is a glyph, so it carries no border).
        /// </summary>
        public static ComponentResourceKey CheckBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(CheckBox));

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

        /// <summary>
        /// Default style for a flat, borderless stock <see cref="System.Windows.Controls.ComboBox"/> —
        /// used by the filter row's combo editor and available for consumer combos that should match
        /// the editors' flat look. (<see cref="WWComboBox"/> no longer hosts one; its template is
        /// self-contained under <see cref="ComboBox"/>.)
        /// </summary>
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
        /// Text-only style for the nested <see cref="SegmentedDateTimeEditor"/> that edits the
        /// time-of-day inside the date editor's calendar popup. Its template is just the masked
        /// <c>PART_TextBox</c> — no calendar, dropdown button, or popup — which is also what stops
        /// the popup-hosting default template from recursing into itself.
        /// </summary>
        public static ComponentResourceKey SegmentedTimeEditor { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(SegmentedTimeEditor));

        /// <summary>
        /// Default style for the footer Buttons (Today / Clear) in the date editor's popup.
        /// </summary>
        public static ComponentResourceKey DatePickerPopupButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(DatePickerPopupButton));

        /// <summary>
        /// Modernized style for the <see cref="System.Windows.Controls.Calendar"/> inside the date
        /// editor's popup. Keyed (not implicit) so consumer apps' calendars are untouched; it wires
        /// the <see cref="CalendarItem"/> / <see cref="CalendarDayButton"/> /
        /// <see cref="CalendarButton"/> keys below through the Calendar's style properties.
        /// </summary>
        public static ComponentResourceKey Calendar { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(Calendar));

        /// <summary>
        /// Style for the calendar's month/year chrome — header button, chevron navigation, the
        /// day-title row, and the month / year view grids.
        /// </summary>
        public static ComponentResourceKey CalendarItem { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(CalendarItem));

        /// <summary>
        /// Style for a day cell in the calendar's month view — hover, accent-filled selection,
        /// today ring, faded adjacent-month days, blacked-out days.
        /// </summary>
        public static ComponentResourceKey CalendarDayButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(CalendarDayButton));

        /// <summary>
        /// Style for a month / year cell in the calendar's zoomed-out views.
        /// </summary>
        public static ComponentResourceKey CalendarButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(CalendarButton));

        /// <summary>
        /// Default style for the up/down RepeatButtons used by the spin editor's display and edit
        /// templates. The button's Content is rendered as a Fluent-icon glyph by the style's template.
        /// </summary>
        public static ComponentResourceKey SpinButton { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(SpinButton));

        /// <summary>Default style for the <see cref="WWColorPicker"/> swatch + HSV popup editor.</summary>
        public static ComponentResourceKey ColorPicker { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(ColorPicker));

        /// <summary>Default style for the <see cref="WWSearchTextBox"/> editor.</summary>
        public static ComponentResourceKey SearchTextBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(SearchTextBox));

        /// <summary>Default style for the <see cref="WWRangeSlider"/> two-thumb range slider editor.</summary>
        public static ComponentResourceKey RangeSlider { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(RangeSlider));

        /// <summary>
        /// Default style for <see cref="WWListBox"/> — list chrome plus the built-in reorder
        /// defaults (animation duration, ghost opacity).
        /// </summary>
        public static ComponentResourceKey ListBox { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(ListBox));

        /// <summary>
        /// Default style for <see cref="WWListBoxItem"/> — the container <see cref="WWListBox"/>
        /// generates for its rows: row visuals plus the selection glyph column (checkbox / radio
        /// per <see cref="WWListBox.ItemKind"/>), lit by IsSelected.
        /// </summary>
        public static ComponentResourceKey ListBoxItem { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(ListBoxItem));

        /// <summary>
        /// Default style for <see cref="WWPropertyGrid"/> — the whole property-grid chrome: optional
        /// title bar, search box, the category-grouped property list with its shared name/editor
        /// column splitter, and the description panel. Each property row hosts an editor supplied via
        /// <see cref="WWPropertyGrid.EditorDefinitions"/> (or the read-only placeholder).
        /// </summary>
        public static ComponentResourceKey PropertyGrid { get; } =
            new ComponentResourceKey(typeof(EditorThemeKeys), nameof(PropertyGrid));
    }
}
