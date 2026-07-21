using System.Windows.Markup;

// Register this assembly's types under the shared WWControls XAML namespace so consumers can
// reference the icon keys with the same `sdg:` prefix used for the rest of the library.
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Icons")]
[assembly: XmlnsPrefix("http://schemas.wwcontrols.com/wpf", "sdg")]
