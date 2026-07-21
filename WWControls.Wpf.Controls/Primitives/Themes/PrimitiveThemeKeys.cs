using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Typed resource keys for the primitive controls' default styles. Consumers retheme a primitive
    /// by redefining a resource under the same key:
    /// <code>
    /// &lt;Style x:Key="{x:Static sdg:PrimitiveThemeKeys.Button}" TargetType="{x:Type Button}"
    ///        BasedOn="{StaticResource {x:Static sdg:PrimitiveThemeKeys.Button}}" /&gt;
    /// </code>
    /// Owned by the Primitives assembly so the primitives are self-theming (the keyed styles live in
    /// this assembly's <c>Themes/Generic.xaml</c> slice, reachable via <c>[ThemeInfo]</c>).
    /// </summary>
    public static class PrimitiveThemeKeys
    {
        /// <summary>
        /// Shared <see cref="CornerRadius"/> resource the library's chrome styles feed into
        /// <see cref="ControlHelper.CornerRadiusProperty"/>. Redefine it to re-round (or square)
        /// every consuming control at once.
        /// </summary>
        public static ComponentResourceKey ControlCornerRadius { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ControlCornerRadius));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.Button"/> style.</summary>
        public static ComponentResourceKey Button { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(Button));

        /// <summary>
        /// Default style for <see cref="Primitives.WWButton"/> — the library's simple / repeat /
        /// toggle button with glyph and async-wait support.
        /// </summary>
        public static ComponentResourceKey WWButton { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(WWButton));

        /// <summary>Default style for the <see cref="Primitives.WWSpinningWheel"/> progress ring.</summary>
        public static ComponentResourceKey SpinningWheel { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(SpinningWheel));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.CheckBox"/> style.</summary>
        public static ComponentResourceKey CheckBox { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(CheckBox));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.ComboBox"/> style.</summary>
        public static ComponentResourceKey ComboBox { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ComboBox));

        /// <summary>
        /// Sdg-themed <see cref="ComboBoxItem"/> style — the shared dropdown-item look. Applied
        /// implicitly inside <see cref="ComboBox"/>'s popup; exposed so consumer combos can opt in.
        /// </summary>
        public static ComponentResourceKey ComboBoxItem { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ComboBoxItem));

        /// <summary>
        /// Sdg-themed <see cref="System.Windows.Controls.ListBoxItem"/> style — the shared
        /// "dropdown item" look used inside library popups. Mirrors <see cref="ComboBoxItem"/> and
        /// <see cref="MenuItem"/> so every dropdown row reads as the same control.
        /// </summary>
        public static ComponentResourceKey ListBoxItem { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ListBoxItem));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.Primitives.ScrollBar"/> style.</summary>
        public static ComponentResourceKey ScrollBar { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ScrollBar));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.TabControl"/> style.</summary>
        public static ComponentResourceKey TabControl { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(TabControl));

        /// <summary>Sdg-themed <see cref="System.Windows.Controls.TabItem"/> style.</summary>
        public static ComponentResourceKey TabItem { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(TabItem));

        /// <summary>Sdg-themed resize <see cref="Thumb"/> used by column splitters and resizers.</summary>
        public static ComponentResourceKey ResizeThumb { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ResizeThumb));

        /// <summary>
        /// Sdg-themed <see cref="System.Windows.Controls.Primitives.ToggleButton"/> style — a pill
        /// that fills with the accent tint while checked (Bold / Italic / Underline toggles, etc.).
        /// </summary>
        public static ComponentResourceKey ToggleButton { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ToggleButton));

        /// <summary>
        /// Sdg-themed plain <see cref="System.Windows.Controls.TextBox"/> style — same chrome as
        /// <see cref="ComboBox"/> with an accent underline while focused.
        /// </summary>
        public static ComponentResourceKey TextBox { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(TextBox));

        /// <summary>Default style for the <see cref="StatusIcon"/> status badge primitive.</summary>
        public static ComponentResourceKey StatusIcon { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(StatusIcon));

        /// <summary>
        /// Default style for <see cref="Primitives.WWMessageBox"/> — the message dialog that renders
        /// a severity icon, message text, and a footer of <see cref="Primitives.WWButton"/>s built
        /// from a <see cref="Primitives.UICommand"/> list.
        /// </summary>
        public static ComponentResourceKey WWMessageBox { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(WWMessageBox));

        /// <summary>
        /// Default style for the <see cref="Primitives.WWTreeView"/> — a themed tree with two-way
        /// single-selection binding, drag-and-drop, and expand-all / collapse-all commands.
        /// </summary>
        public static ComponentResourceKey WWTreeView { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(WWTreeView));

        /// <summary>
        /// Default style for <see cref="Primitives.WWTreeViewItem"/> — the tree container that draws
        /// the connector lines, hover highlight, and per-item expand/collapse affordances.
        /// </summary>
        public static ComponentResourceKey WWTreeViewItem { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(WWTreeViewItem));

        /// <summary>
        /// Default style for the <see cref="System.Windows.Controls.ContextMenu"/> shell — rounded
        /// white surface with soft border and shadow. Available for consumer-defined context menus.
        /// </summary>
        public static ComponentResourceKey ContextMenu { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(ContextMenu));

        /// <summary>
        /// Default style for <see cref="System.Windows.Controls.MenuItem"/>s hosted inside an SDG
        /// context menu — icon column, header, gesture text, submenu chevron. Wired as the
        /// <c>ItemContainerStyle</c> of <see cref="ContextMenu"/>.
        /// </summary>
        public static ComponentResourceKey MenuItem { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(MenuItem));

        /// <summary>
        /// Style for an icon hosted in a <see cref="System.Windows.Controls.MenuItem"/>'s icon slot —
        /// dims the glyph while the owning item is disabled. Trigger-only (no sizing).
        /// </summary>
        public static ComponentResourceKey MenuItemIcon { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(MenuItemIcon));

        /// <summary>
        /// Shared text-editing <see cref="System.Windows.Controls.ContextMenu"/> — the standard
        /// Undo / Redo / Cut / Copy / Paste / Select All commands (stock <c>ApplicationCommands</c>
        /// routed to the menu's <c>PlacementTarget</c>) wearing the <see cref="ContextMenu"/> shell.
        /// Attached to the library's text inputs via their default styles so every editable text box
        /// gets the same themed right-click menu. Declared <c>x:Shared="False"</c>, so each text box
        /// receives its own instance (the spell-check suggestions <see cref="Editors.WWTextBox"/>
        /// injects on open stay isolated to that control).
        /// </summary>
        public static ComponentResourceKey TextBoxContextMenu { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(TextBoxContextMenu));

        /// <summary>
        /// Default chrome for every <see cref="System.Windows.Window"/> the library opens — borderless
        /// DWM window with rounded corners, drop shadow, accent border, and a caption with taskbar-aware
        /// Min / Max / Close buttons. Consumers that reuse it must register the SystemCommands bindings.
        /// </summary>
        public static ComponentResourceKey Window { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(Window));
    }
}
