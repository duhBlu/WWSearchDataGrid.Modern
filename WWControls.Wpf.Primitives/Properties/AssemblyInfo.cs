using System.Windows;
using System.Windows.Markup;

// The primitive controls own their default styles, so this assembly participates in WPF's
// per-assembly theme discovery: [ThemeInfo] points theme-style fallback at this assembly's
// Themes/Generic.xaml, so a primitive resolves its template whether or not the consumer merged
// any dictionary (theme-style lookup consults only the owning assembly's generic.xaml).
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,          // no per-theme dictionaries
    ResourceDictionaryLocation.SourceAssembly // generic dictionary lives in this assembly
)]

// Contribute the Primitives namespace to the shared WWControls XML namespace, so consumer XAML
// (and the theme dictionaries) reach the primitive controls through the one `ww` xmlns.
[assembly: XmlnsPrefix("http://schemas.wwcontrols.com/wpf", "ww")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Primitives")]
