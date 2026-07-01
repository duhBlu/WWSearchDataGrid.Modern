using System.Windows;
using System.Windows.Markup;

// The editor controls own their default styles, so this assembly participates in WPF's per-assembly
// theme discovery: [ThemeInfo] points FindResource / theme-style fallback at this assembly's
// Themes/Generic.xaml. Without it, controls whose DefaultStyleKey type lives here (e.g. WWTextEdit,
// which keys off WWBaseEdit) never resolve a template, because theme-style lookup consults only the
// owning assembly's generic.xaml — never Application.Resources.
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,          // no per-theme dictionaries
    ResourceDictionaryLocation.SourceAssembly // generic dictionary lives in this assembly
)]

// Contribute the Editors namespace to the shared WWControls XML namespace, so consumer XAML
// (and the theme dictionaries) reach the editor controls through the one `ww` xmlns.
//
// NOTE: WWControls.Wpf.Editors is a namespace that intentionally spans two assemblies — the
// editor *controls* (WWBaseEdit, the WWxxxEdit family, SegmentedDateTimeEditor, EditorChrome,
// EditSettingsThemeKeys) live here, while the grid-side EditSettings *adapters* (TextEditSettings,
// BaseEditSettings, ...) keep this same namespace but stay in the grid assembly as the bridge.
// The grid assembly therefore ALSO declares this XmlnsDefinition for its own .Editors types; both
// mappings union under the single URL so `ww:TextEditSettings` and `ww:WWTextEdit` both resolve.
[assembly: XmlnsPrefix("http://schemas.wwcontrols.com/wpf", "ww")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Editors")]
