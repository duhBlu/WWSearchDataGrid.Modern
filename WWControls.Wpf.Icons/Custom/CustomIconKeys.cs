using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf.Icons
{
    /// <summary>
    /// Typed <see cref="ComponentResourceKey"/> keys for the in-house <see cref="DrawingImage"/> icons
    /// defined in <c>Custom/CustomIcons.xaml</c> — icons drawn in the Lucide style that have no stock
    /// Lucide equivalent. See <see cref="LucideIconKeys"/> for the authoring convention and XAML usage.
    /// </summary>
    public static class CustomIconKeys
    {
        // One ComponentResourceKey per DrawingImage in Custom/CustomIcons.xaml, e.g.:
        // public static ComponentResourceKey MyGlyph { get; } =
        //     new ComponentResourceKey(typeof(CustomIconKeys), nameof(MyGlyph));
    }
}
