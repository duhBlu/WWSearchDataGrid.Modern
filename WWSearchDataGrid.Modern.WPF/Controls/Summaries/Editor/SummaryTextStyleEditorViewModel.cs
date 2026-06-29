using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Working VM behind the <see cref="SummaryTextStyleEditor"/> — surfaces the selected entry's
    /// three segment styles (Prefix / Value / Suffix) as one tab each. Edits the entry's
    /// working-copy <see cref="SummaryTextStyle"/> objects in place; the parent summary editor's
    /// OK / Cancel decides whether they persist.
    /// </summary>
    public sealed class SummaryTextStyleEditorViewModel
    {
        internal SummaryTextStyleEditorViewModel(SummaryEditorEntry entry)
        {
            Prefix = new SummarySegmentStyleViewModel("Prefix", entry.PrefixStyle, SampleFor(entry.Prefix, "Prefix"));
            Value = new SummarySegmentStyleViewModel("Value", entry.ValueStyle, "123.45");
            Suffix = new SummarySegmentStyleViewModel("Suffix", entry.SuffixStyle, SampleFor(entry.Suffix, "Suffix"));
        }

        public SummarySegmentStyleViewModel Prefix { get; }
        public SummarySegmentStyleViewModel Value { get; }
        public SummarySegmentStyleViewModel Suffix { get; }

        private static string SampleFor(string text, string fallback)
            => string.IsNullOrEmpty(text) ? fallback : text;
    }

    /// <summary>
    /// One segment tab in the text-styling editor — Bold / Italic / Underline toggles plus a color,
    /// all writing through to the wrapped working-copy <see cref="SummaryTextStyle"/>.
    /// </summary>
    public sealed class SummarySegmentStyleViewModel : INotifyPropertyChanged
    {
        private static readonly Color DefaultColor = Color.FromRgb(0x33, 0x33, 0x33);

        internal SummarySegmentStyleViewModel(string header, SummaryTextStyle style, string sampleText)
        {
            Header = header;
            Style = style;
            SampleText = sampleText;
        }

        /// <summary>Tab caption — "Prefix" / "Value" / "Suffix".</summary>
        public string Header { get; }

        /// <summary>Representative text rendered in this tab's live preview.</summary>
        public string SampleText { get; }

        /// <summary>The working-copy style this tab edits — the preview Run binds straight to its DPs.</summary>
        public SummaryTextStyle Style { get; }

        public bool IsBold
        {
            get => Style.FontWeight == FontWeights.Bold;
            set { Style.FontWeight = value ? FontWeights.Bold : FontWeights.Normal; OnPropertyChanged(); }
        }

        public bool IsItalic
        {
            get => Style.FontStyle == FontStyles.Italic;
            set { Style.FontStyle = value ? FontStyles.Italic : FontStyles.Normal; OnPropertyChanged(); }
        }

        public bool IsUnderline
        {
            get => Style.TextDecorations != null && Style.TextDecorations.Count > 0;
            set { Style.TextDecorations = value ? TextDecorations.Underline : null; OnPropertyChanged(); }
        }

        /// <summary>
        /// The segment color, backed by <see cref="SummaryTextStyle.Foreground"/> as a
        /// <see cref="SolidColorBrush"/>. An unset color reads as the default text color so the
        /// picker opens on a sensible swatch; the brush is only written once the user picks.
        /// </summary>
        public Color Color
        {
            get => Style.Foreground is SolidColorBrush brush ? brush.Color : DefaultColor;
            set { Style.Foreground = new SolidColorBrush(value); OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
