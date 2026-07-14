using System.Collections.Generic;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWColorPicker "palette studio": six pickers each bound two-way to a theme role
    /// (Canvas / Surface / Accent / Heading / Body / Border) that live-reskin a mock dashboard card.
    /// Each Color role projects to a frozen <see cref="Brush"/> the preview binds to — SelectedColor
    /// is a <see cref="Color"/>, not a Brush, so the projection is the one adapter a real consumer
    /// needs. The accent additionally projects an auto-contrast foreground so overlaid text stays
    /// readable as the swatch is dragged, and a soft tint for the stat tiles.
    ///
    /// The preset set is a coordinated theme: picking one both restocks every picker's PresetColors
    /// grid and re-seeds the six roles from that set's own swatches. Reset re-applies the current
    /// set's theme.
    /// </summary>
    public partial class ColorPickerSampleViewModel : ObservableObject
    {
        // ── Palette roles (each picker binds SelectedColor two-way here; seeded to the Material set) ─
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanvasBrush))]
        private Color _canvas = Hex("#ECEFF1");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SurfaceBrush))]
        private Color _surface = Colors.White;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AccentBrush), nameof(AccentForegroundBrush), nameof(AccentSoftBrush))]
        private Color _accent = Hex("#2196F3");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeadingBrush))]
        private Color _heading = Hex("#212121");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BodyBrush))]
        private Color _body = Hex("#546E7C");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BorderBrush))]
        private Color _border = Hex("#CFD8DC");

        // ── Brush projections the preview card binds to ──────────────────────────
        public Brush CanvasBrush => Frozen(Canvas);
        public Brush SurfaceBrush => Frozen(Surface);
        public Brush AccentBrush => Frozen(Accent);
        public Brush HeadingBrush => Frozen(Heading);
        public Brush BodyBrush => Frozen(Body);
        public Brush BorderBrush => Frozen(Border);

        /// <summary>Black or white, whichever reads on the accent — keeps overlaid text legible.</summary>
        public Brush AccentForegroundBrush => Frozen(IsLight(Accent) ? Colors.Black : Colors.White);

        /// <summary>A pale wash of the accent for the stat-tile backgrounds.</summary>
        public Brush AccentSoftBrush => Frozen(Mix(Colors.White, Accent, 0.12));

        // ── Options ──────────────────────────────────────────────────────────────
        /// <summary>Drives every picker's toggle-button face: swatch-only vs. swatch + hex text.</summary>
        [ObservableProperty]
        private bool _displayColorAndName = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentPresets))]
        private ColorPresetSet _presetSet = ColorPresetSet.Material;

        public IReadOnlyList<ColorPresetSet> PresetSets { get; } =
            new[] { ColorPresetSet.Material, ColorPresetSet.Grayscale, ColorPresetSet.Web };

        /// <summary>The swatch list handed to every picker's PresetColors — swaps with PresetSet.</summary>
        public List<Color> CurrentPresets => PresetSet switch
        {
            ColorPresetSet.Grayscale => GrayscalePresets,
            ColorPresetSet.Web => WebPresets,
            _ => MaterialPresets,
        };

        /// <summary>Switching the set re-themes all six roles from that set's own swatches.</summary>
        partial void OnPresetSetChanged(ColorPresetSet value) => ApplyTheme(value);

        // ── Commands ───────────────────────────────────────────────────────────
        /// <summary>Restores the current preset set's theme, discarding any hand-tuning.</summary>
        [RelayCommand]
        private void ResetPalette() => ApplyTheme(PresetSet);

        private void ApplyTheme(ColorPresetSet set)
        {
            var (canvas, surface, accent, heading, body, border) = ThemeFor(set);
            Canvas = canvas;
            Surface = surface;
            Accent = accent;
            Heading = heading;
            Body = body;
            Border = border;
        }

        /// <summary>The six role colors for a set — every value is a member of that set's swatch grid.</summary>
        private static (Color canvas, Color surface, Color accent, Color heading, Color body, Color border)
            ThemeFor(ColorPresetSet set) => set switch
        {
            ColorPresetSet.Grayscale =>
                (Hex("#F5F5F5"), Colors.White, Hex("#616161"), Hex("#212121"), Hex("#757575"), Hex("#E0E0E0")),
            ColorPresetSet.Web =>
                (Hex("#C0C0C0"), Colors.White, Hex("#FF0000"), Hex("#000000"), Hex("#808080"), Hex("#808080")),
            _ =>
                (Hex("#ECEFF1"), Colors.White, Hex("#2196F3"), Hex("#212121"), Hex("#546E7C"), Hex("#CFD8DC")),
        };

        // ── Helpers ──────────────────────────────────────────────────────────────
        private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private static Brush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>Perceptual brightness (ITU-R BT.601) — above the midpoint reads as "light".</summary>
        private static bool IsLight(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) > 150;

        private static Color Mix(Color a, Color b, double t) => Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));

        // ── Preset swatch sets — each spans the neutrals + accents its theme draws from ──────────
        private static readonly List<Color> MaterialPresets = new()
        {
            // Neutrals (Canvas / Surface / Body / Border / Heading live here)
            Hex("#FFFFFF"), Hex("#ECEFF1"), Hex("#CFD8DC"), Hex("#90A4AE"),
            Hex("#546E7C"), Hex("#263238"), Hex("#212121"),
            // Accents
            Hex("#F44336"), Hex("#E91E63"), Hex("#9C27B0"), Hex("#673AB7"),
            Hex("#3F51B5"), Hex("#2196F3"), Hex("#03A9F4"), Hex("#00BCD4"),
            Hex("#009688"), Hex("#4CAF50"), Hex("#8BC34A"), Hex("#FFC107"),
            Hex("#FF9800"), Hex("#FF5722"),
        };

        private static readonly List<Color> GrayscalePresets = new()
        {
            Hex("#FFFFFF"), Hex("#F5F5F5"), Hex("#E0E0E0"), Hex("#BDBDBD"),
            Hex("#9E9E9E"), Hex("#757575"), Hex("#616161"), Hex("#424242"),
            Hex("#212121"), Hex("#000000"),
        };

        private static readonly List<Color> WebPresets = new()
        {
            Hex("#000000"), Hex("#FFFFFF"), Hex("#FF0000"), Hex("#00FF00"),
            Hex("#0000FF"), Hex("#FFFF00"), Hex("#00FFFF"), Hex("#FF00FF"),
            Hex("#C0C0C0"), Hex("#808080"), Hex("#800000"), Hex("#808000"),
            Hex("#008000"), Hex("#800080"), Hex("#008080"), Hex("#000080"),
        };
    }

    /// <summary>Named swatch collections the palette studio can hand to each picker's PresetColors.</summary>
    public enum ColorPresetSet
    {
        Material,
        Grayscale,
        Web,
    }
}
