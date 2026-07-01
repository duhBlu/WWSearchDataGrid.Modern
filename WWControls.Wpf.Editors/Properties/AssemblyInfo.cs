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
// This assembly holds the editor *controls* (ns WWControls.Wpf.Editors — WWBaseEdit, the WWxxxEdit
// family, SegmentedDateTimeEditor, EditorThemeKeys) AND the grid-agnostic EditSettings
// *adapters* (ns WWControls.Wpf.Editors.Settings — BaseEditSettings, TextEditSettings, ...). Both
// namespaces map to the one `ww` xmlns so consumer XAML reaches `ww:WWTextEdit` and
// `ww:TextEditSettings` alike. The grid connects to the adapters purely via the column EditSettings
// DP and the IEditorColumn / IEditingGridHost / IFilterEditorHost interfaces it implements.
[assembly: XmlnsPrefix("http://schemas.wwcontrols.com/wpf", "ww")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Editors")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Editors.Settings")]
