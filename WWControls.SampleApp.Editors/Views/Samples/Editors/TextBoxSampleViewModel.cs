using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf.Controls.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWTextBox sample, which is a set of purpose-built example cards rather than one
    /// do-everything editor. Each card pairs a live WWTextBox with only the options that matter for
    /// its role, so the grouping reads as "here is how you configure this kind of text box":
    /// a single-line input (watermark, clear button, length, alignment, casing, debounce), a
    /// multi-line memo (wrapping, scrollbars, line bounds, spell check), and an adorned input
    /// (edge glyph + placement). Masking has its own dedicated sample (MaskSampleView). The enum
    /// choices are mapped to concrete resources in the view's XAML.
    /// </summary>
    public partial class TextBoxSampleViewModel : ObservableObject
    {
        // ── Card 1: single-line input ────────────────────────────────────────
        /// <summary>The single-line editor's bound value, echoed live beneath the control.</summary>
        [ObservableProperty]
        private string _basicText = "Acme Corporation";

        [ObservableProperty]
        private bool _basicShowClearButton = true;

        /// <summary>0 = unlimited (matches WWTextBox.MaxLength).</summary>
        [ObservableProperty]
        private int _basicMaxLength;

        [ObservableProperty]
        private TextAlignment _basicTextAlignment = TextAlignment.Left;

        [ObservableProperty]
        private CharacterCasing _basicCharacterCasing = CharacterCasing.Normal;

        /// <summary>Milliseconds; 0 = push on every keystroke.</summary>
        [ObservableProperty]
        private int _basicUpdateDelay;

        // ── Card 2: multi-line memo ──────────────────────────────────────────
        /// <summary>The memo editor's bound value; the character count echoes live beneath it.</summary>
        [ObservableProperty]
        private string _memoText =
            "This base cabinet run ships in three phases. Phase one is the sink base and the two " +
            "drawer stacks flanking it; phase two is the upper wall cabinets; phase three is the tall " +
            "pantry and all filler and trim. Confirm door style and finish before releasing phase one " +
            "to the mill.";

        [ObservableProperty]
        private TextWrapping _memoTextWrapping = TextWrapping.Wrap;

        [ObservableProperty]
        private ScrollBarVisibility _memoVerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        [ObservableProperty]
        private int _memoMinLines = 4;

        [ObservableProperty]
        private int _memoMaxLines = 8;

        [ObservableProperty]
        private bool _memoSpellCheck = true;

        // ── Card 3: adorned input (glyph) ────────────────────────────────────
        [ObservableProperty]
        private string _glyphText = string.Empty;

        [ObservableProperty]
        private GlyphKind _glyphKind = GlyphKind.Filter;

        [ObservableProperty]
        private GlyphPlacement _glyphPlacement = GlyphPlacement.Left;

        // ── Option choices ───────────────────────────────────────────────────
        public IReadOnlyList<TextAlignment> TextAlignments { get; } = new[]
        {
            TextAlignment.Left, TextAlignment.Center, TextAlignment.Right,
        };

        public IReadOnlyList<CharacterCasing> CharacterCasings { get; } = new[]
        {
            CharacterCasing.Normal, CharacterCasing.Upper, CharacterCasing.Lower,
        };

        public IReadOnlyList<TextWrapping> TextWrappings { get; } = new[]
        {
            TextWrapping.NoWrap, TextWrapping.Wrap, TextWrapping.WrapWithOverflow,
        };

        public IReadOnlyList<ScrollBarVisibility> ScrollBarVisibilities { get; } = new[]
        {
            ScrollBarVisibility.Hidden, ScrollBarVisibility.Auto, ScrollBarVisibility.Visible, ScrollBarVisibility.Disabled,
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

    /// <summary>Glyph choices offered by the WWTextBox adorned-input card; mapped to icon resources in the view's XAML.</summary>
    public enum GlyphKind
    {
        None,
        Edit,
        Calendar,
        Filter,
    }
}
