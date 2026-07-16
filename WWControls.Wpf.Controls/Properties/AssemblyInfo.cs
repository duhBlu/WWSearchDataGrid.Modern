using System.Windows;
using System.Windows.Markup;

// Every WW control (primitives + editors) owns its default style, so this assembly participates in
// WPF's per-assembly theme discovery: [ThemeInfo] points theme-style fallback at this assembly's
// Themes/Generic.xaml. Theme-style lookup consults only the owning assembly's generic.xaml (never
// Application.Resources), so without this a control whose DefaultStyleKey type lives here never
// resolves a template.
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,          // no per-theme dictionaries
    ResourceDictionaryLocation.SourceAssembly // generic dictionary lives in this assembly
)]

// Contribute every control namespace to the shared WWControls XML namespace, so consumer XAML (and
// the theme dictionaries) reach all controls through the one `ww` xmlns. The primitive controls
// (WWControls.Wpf.Controls.Primitives), the editor controls (WWControls.Wpf.Controls.Editors) and the
// grid-agnostic EditSettings adapters (WWControls.Wpf.Controls.Editors.Settings) all map to `ww`, so
// consumer XAML reaches `ww:WWButton`, `ww:WWTextBox` and `ww:TextBoxSettings` alike.
[assembly: XmlnsPrefix("http://schemas.wwcontrols.com/wpf", "ww")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Controls.Primitives")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Controls.Editors")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Controls.Editors.Settings")]
