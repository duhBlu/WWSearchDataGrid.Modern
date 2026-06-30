using System.Windows.Markup;

// Contribute the Primitives namespace to the shared WWControls XML namespace, so consumer XAML
// (and the theme dictionaries) reach the primitive controls through the one `ww` xmlns.
[assembly: XmlnsPrefix("http://schemas.wwcontrols.com/wpf", "ww")]
[assembly: XmlnsDefinition("http://schemas.wwcontrols.com/wpf", "WWControls.Wpf.Primitives")]
