using System.Windows;
using System.Windows.Input;

namespace WWControls.Wpf
{
    /// <summary>
    /// Shared setup for the windows the library creates in code (Filter Editor, group summary
    /// editor, Column Chooser): applies the <see cref="PrimitiveThemeKeys.Window"/> chrome and
    /// wires the <see cref="SystemCommands"/> its caption buttons invoke — per window instance,
    /// so consumers don't need app-level class command bindings for the chrome to work.
    /// </summary>
    internal static class WindowHostHelper
    {
        /// <summary>
        /// Applies the library's default window chrome to <paramref name="host"/>. The style
        /// resolves by ComponentResourceKey through <paramref name="resourceScope"/> (element
        /// scope → app scope → the theme assembly's <c>Themes/Generic.xaml</c> via
        /// <c>[ThemeInfo]</c>), so consumers get the default with no manual merge and can
        /// override it by re-keying the resource.
        /// </summary>
        internal static void ApplyDefaultChrome(Window host, FrameworkElement resourceScope)
        {
            if (resourceScope.TryFindResource(PrimitiveThemeKeys.Window) is Style style)
                host.Style = style;
            WireSystemCommands(host);
        }

        /// <summary>
        /// Registers instance handlers for the caption-button SystemCommands. Safe alongside any
        /// app-level class registrations — instance bindings take precedence.
        /// </summary>
        internal static void WireSystemCommands(Window host)
        {
            host.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
                (s, _) => SystemCommands.CloseWindow((Window)s)));
            host.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand,
                (s, _) => SystemCommands.MaximizeWindow((Window)s)));
            host.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                (s, _) => SystemCommands.MinimizeWindow((Window)s)));
            host.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                (s, _) => SystemCommands.RestoreWindow((Window)s)));
        }
    }
}
