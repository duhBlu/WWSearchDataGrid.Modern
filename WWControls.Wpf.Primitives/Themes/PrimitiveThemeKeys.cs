using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WWControls.Wpf.Primitives
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
        /// <summary>Sdg-themed <see cref="System.Windows.Controls.Button"/> style.</summary>
        public static ComponentResourceKey Button { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(Button));

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
        /// Default chrome for every <see cref="System.Windows.Window"/> the library opens — borderless
        /// DWM window with rounded corners, drop shadow, accent border, and a caption with taskbar-aware
        /// Min / Max / Close buttons. Consumers that reuse it must register the SystemCommands bindings.
        /// </summary>
        public static ComponentResourceKey Window { get; } =
            new ComponentResourceKey(typeof(PrimitiveThemeKeys), nameof(Window));
    }
}
