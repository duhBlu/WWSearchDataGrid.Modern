using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// The look of one summary text segment — the prefix, the value, or the suffix of a
    /// <see cref="SummaryItem"/>. Carried per segment by <see cref="SummaryItem.PrefixStyle"/> /
    /// <see cref="SummaryItem.ValueStyle"/> / <see cref="SummaryItem.SuffixStyle"/> and applied
    /// to the matching <see cref="System.Windows.Documents.Run"/> when the entry renders. A
    /// <see cref="Freezable"/> so it round-trips through the editor working copy and so the
    /// owning item's <see cref="System.Windows.FreezableCollection{T}"/> raises Changed when a
    /// segment style is edited. Every property at its default leaves the segment inheriting the
    /// surface's base look (<see cref="IsDefault"/>).
    /// </summary>
    public class SummaryTextStyle : Freezable
    {
        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(
                nameof(FontWeight),
                typeof(FontWeight),
                typeof(SummaryTextStyle),
                new PropertyMetadata(FontWeights.Normal));

        /// <summary>Segment weight — <see cref="FontWeights.Bold"/> for the Bold toggle, else Normal.</summary>
        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty =
            DependencyProperty.Register(
                nameof(FontStyle),
                typeof(FontStyle),
                typeof(SummaryTextStyle),
                new PropertyMetadata(FontStyles.Normal));

        /// <summary>Segment slant — <see cref="FontStyles.Italic"/> for the Italic toggle, else Normal.</summary>
        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty TextDecorationsProperty =
            DependencyProperty.Register(
                nameof(TextDecorations),
                typeof(TextDecorationCollection),
                typeof(SummaryTextStyle),
                new PropertyMetadata(null));

        /// <summary>
        /// Segment decorations — <see cref="TextDecorations.Underline"/> for the Underline toggle,
        /// null for none.
        /// </summary>
        public TextDecorationCollection TextDecorations
        {
            get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(SummaryTextStyle),
                new PropertyMetadata(null));

        /// <summary>
        /// Segment color. Null inherits the surface's base text color — the rendered
        /// <see cref="System.Windows.Documents.Run"/> binds this through a null-to-unset converter
        /// so an unset color falls back to the hosting TextBlock's foreground.
        /// </summary>
        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// True when every property sits at its default — no weight, slant, decoration, or color
        /// override. Persisted as a null style slot so unstyled entries stay clean.
        /// </summary>
        public bool IsDefault =>
            FontWeight == FontWeights.Normal
            && FontStyle == FontStyles.Normal
            && (TextDecorations == null || TextDecorations.Count == 0)
            && Foreground == null;

        /// <summary>
        /// Shared frozen "no overrides" instance — substituted for an unset segment style at
        /// render time so the rendering bindings always resolve against a non-null style.
        /// </summary>
        public static SummaryTextStyle Default { get; } = CreateDefault();

        private static SummaryTextStyle CreateDefault()
        {
            var style = new SummaryTextStyle();
            style.Freeze();
            return style;
        }

        /// <summary>A detached copy with the same values — used to seed the editor working copy.</summary>
        public SummaryTextStyle Copy() => new SummaryTextStyle
        {
            FontWeight = FontWeight,
            FontStyle = FontStyle,
            TextDecorations = TextDecorations,
            Foreground = Foreground,
        };

        protected override Freezable CreateInstanceCore() => new SummaryTextStyle();
    }
}
