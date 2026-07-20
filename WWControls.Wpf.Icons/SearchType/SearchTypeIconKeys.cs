using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf.Icons
{
    /// <summary>
    /// Typed <see cref="ComponentResourceKey"/> keys for the datagrid filter <see cref="DrawingImage"/>
    /// icons defined in <c>SearchType/SearchTypeIcons.xaml</c> — one per SearchType / DateInterval
    /// glyph used in the filter row and filter editor. See <see cref="LucideIconKeys"/> for the
    /// authoring convention and XAML usage.
    /// </summary>
    /// <remarks>
    /// Distinct from <c>WWControls.Wpf.Controls.Primitives.SearchTypeIconKeys</c> (the existing set in
    /// the Controls assembly) — same name, different namespace and assembly.
    /// </remarks>
    public static class SearchTypeIconKeys
    {
        // One ComponentResourceKey per DrawingImage in SearchType/SearchTypeIcons.xaml, e.g.:
        // public static ComponentResourceKey Equals { get; } =
        //     new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Equals));
    }
}
