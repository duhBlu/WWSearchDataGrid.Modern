using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWTextBox playground: one live editor whose every property is driven by the options
    /// panel — chrome (ShowBorder), IsReadOnly, Watermark, ShowClearButton, an edge Glyph +
    /// GlyphPlacement, TextAlignment, MaxLength, and the UpdateDelay debounce. The bound value echoes
    /// back so the round-trip (and the debounce lag) is visible. Masking has its own dedicated sample
    /// (MaskSampleView). The enum choices are mapped to concrete resources in the view's XAML.
    /// </summary>
    public partial class TextBoxSampleViewModel : ObservableObject
    {
        /// <summary>The editor's bound value, echoed live beneath the control.</summary>
        [ObservableProperty]
        private string _sampleText = "The quick brown fox";

        [ObservableProperty]
        private bool _showBorder = true;

        [ObservableProperty]
        private bool _isReadOnly;

        [ObservableProperty]
        private bool _showClearButton = true;

        [ObservableProperty]
        private string _watermark = "Type here…";

        [ObservableProperty]
        private TextAlignment _textAlignment = TextAlignment.Left;

        [ObservableProperty]
        private GlyphKind _glyph = GlyphKind.None;

        [ObservableProperty]
        private GlyphPlacement _glyphPlacement = GlyphPlacement.Left;

        /// <summary>0 = unlimited (matches WWTextBox.MaxLength).</summary>
        [ObservableProperty]
        private int _maxLength;

        /// <summary>Milliseconds; 0 = push on every keystroke.</summary>
        [ObservableProperty]
        private int _updateDelay;

        public IReadOnlyList<TextAlignment> TextAlignments { get; } = new[]
        {
            TextAlignment.Left, TextAlignment.Center, TextAlignment.Right,
        };

        public IReadOnlyList<GlyphKind> GlyphKinds { get; } = new[]
        {
            GlyphKind.None, GlyphKind.Edit, GlyphKind.Calendar, GlyphKind.Filter,
        };

        public IReadOnlyList<GlyphPlacement> GlyphPlacements { get; } = new[]
        {
            GlyphPlacement.Left, GlyphPlacement.Right,
        };
    }

    /// <summary>Glyph choices offered by the WWTextBox playground; mapped to icon resources in the view's XAML.</summary>
    public enum GlyphKind
    {
        None,
        Edit,
        Calendar,
        Filter,
    }
}
