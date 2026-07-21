using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf.Icons
{
    /// <summary>
    /// Typed <see cref="ComponentResourceKey"/> keys for the in-house <see cref="DrawingImage"/>
    /// icons in <c>Custom/CustomIcons.xaml</c> that have no stock Lucide equivalent.
    /// </summary>
    public static class CustomIconKeys
    {
        public static ComponentResourceKey IconCaretRight { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconCaretRight));

        public static ComponentResourceKey IconCaretLeft { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconCaretLeft));

        public static ComponentResourceKey IconCaretUp { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconCaretUp));

        public static ComponentResourceKey IconCaretDown { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconCaretDown));

        public static ComponentResourceKey IconStatusInfo { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconStatusInfo));

        public static ComponentResourceKey IconStatusSuccess { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconStatusSuccess));

        public static ComponentResourceKey IconStatusWarning { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconStatusWarning));

        public static ComponentResourceKey IconStatusError { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconStatusError));

        public static ComponentResourceKey IconStatusQuestion { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconStatusQuestion));

        public static ComponentResourceKey IconMin { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconMin));

        public static ComponentResourceKey IconMax { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconMax));

        public static ComponentResourceKey IconBestFit { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconBestFit));

        public static ComponentResourceKey IconUngroup { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconUngroup));

        public static ComponentResourceKey IconPinLeft { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconPinLeft));

        public static ComponentResourceKey IconPinRight { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconPinRight));

        public static ComponentResourceKey IconCut { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconCut));

        public static ComponentResourceKey IconPaste { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconPaste));

        public static ComponentResourceKey IconUndo { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconUndo));

        public static ComponentResourceKey IconRedo { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconRedo));

        public static ComponentResourceKey IconSelectAll { get; } =
            new ComponentResourceKey(typeof(CustomIconKeys), nameof(IconSelectAll));

    }
}
