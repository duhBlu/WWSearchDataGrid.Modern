using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Primitives
{
    /// <summary>
    /// Backs the HighlightTextBlock playground. A search term and MatchMode drive a live-highlighted
    /// list, <see cref="MatchCount"/> reports how many rows match (using the same anchoring rules the
    /// control applies), and the "Highlight run style" options compose a <see cref="HighlightRunStyle"/>
    /// (a Style targeting Run) that restyles the matched run — weight, italic, underline, foreground,
    /// and an optional background fill — live across every row.
    /// </summary>
    public partial class HighlightTextBlockSampleViewModel : ObservableObject
    {
        // ── Search ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MatchCount))]
        private string _searchTerm = "ba";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MatchCount))]
        private HighlightMatchMode _matchMode = HighlightMatchMode.Contains;

        // ── Highlight run style — each option rebuilds HighlightRunStyle ─────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HighlightRunStyle))]
        private FontWeight _weight = FontWeights.Bold;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HighlightRunStyle))]
        private bool _italic;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HighlightRunStyle))]
        private bool _underline;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HighlightRunStyle))]
        private Color _foreground = Hex("#1C7ED6");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HighlightRunStyle))]
        private bool _useBackground;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HighlightRunStyle))]
        private Color _background = Hex("#FFF3BF");

        public IReadOnlyList<FontWeight> Weights { get; } = new[]
        {
            FontWeights.Normal,
            FontWeights.SemiBold,
            FontWeights.Bold,
        };
        
        public IReadOnlyList<HighlightMatchMode> MatchModes { get; } = new[]
        {
            HighlightMatchMode.Contains,
            HighlightMatchMode.StartsWith,
            HighlightMatchMode.EndsWith,
        };

        /// <summary>Shared swatch grid handed to both color pickers' popups.</summary>
        public IReadOnlyList<Color> StylePresets { get; } = new[]
        {
            Hex("#1C7ED6"), Hex("#2F9E44"), Hex("#E8590C"), Hex("#E03131"),
            Hex("#9C36B5"), Hex("#212529"), Hex("#FFF3BF"), Hex("#D3F9D8"),
            Hex("#FFE3E3"), Hex("#FFFFFF"),
        };

        /// <summary>
        /// The Style applied to the matched Run — rebuilt whenever any style option changes. Only the
        /// setters the current options call for are added, so e.g. no background is emitted unless the
        /// fill is on. The control assigns this to the matched run's <c>Style</c>.
        /// </summary>
        public Style HighlightRunStyle
        {
            get
            {
                var style = new Style(typeof(Run));
                style.Setters.Add(new Setter(TextElement.FontWeightProperty, Weight));
                style.Setters.Add(new Setter(TextElement.ForegroundProperty, Frozen(Foreground)));
                if (Italic)
                    style.Setters.Add(new Setter(TextElement.FontStyleProperty, FontStyles.Italic));
                if (Underline)
                    style.Setters.Add(new Setter(Inline.TextDecorationsProperty, TextDecorations.Underline));
                if (UseBackground)
                    style.Setters.Add(new Setter(TextElement.BackgroundProperty, Frozen(Background)));
                return style;
            }
        }

        // ── Sample data ─────────────────────────────────────────────────────────
        public IReadOnlyList<string> Items { get; } = new[]
        {
            "Base Cabinet",
            "Wall Cabinet",
            "Drawer Base",
            "Corner Base",
            "Sink Base",
            "Pantry Tall",
            "Vanity Base",
            "Oven Cabinet",
            "Refrigerator Panel",
            "Base End Panel",
        };

        public int MatchCount => Items.Count(item => IsMatch(item, SearchTerm, MatchMode));

        // ── Helpers ───────────────────────────────────────────────────────────────
        /// <summary>Mirrors HighlightTextBlock.FindMatchIndex so the readout agrees with the highlighting.</summary>
        private static bool IsMatch(string text, string term, HighlightMatchMode mode)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term))
                return false;

            switch (mode)
            {
                case HighlightMatchMode.StartsWith:
                    return text.StartsWith(term, StringComparison.CurrentCultureIgnoreCase);
                case HighlightMatchMode.EndsWith:
                    return text.EndsWith(term, StringComparison.CurrentCultureIgnoreCase);
                default:
                    return text.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
        }

        private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private static Brush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
