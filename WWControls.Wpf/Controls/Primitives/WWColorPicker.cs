using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// A compact HSV color picker: a swatch toggle button that drops a popup with preset swatches,
    /// hue / saturation / brightness sliders, and a hex input. Ported into the library so the
    /// summary text-styling editor can pick segment colors. Self-contained — the templated default
    /// style lives under <see cref="ThemeKeys.PrimitivesColorPicker"/>.
    /// </summary>
    [TemplatePart(Name = PART_ToggleButton, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_HueSlider, Type = typeof(Slider))]
    [TemplatePart(Name = PART_SaturationSlider, Type = typeof(Slider))]
    [TemplatePart(Name = PART_BrightnessSlider, Type = typeof(Slider))]
    [TemplatePart(Name = PART_HexTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_PresetColorsList, Type = typeof(ListBox))]
    [TemplatePart(Name = PART_PreviewBorder, Type = typeof(Border))]
    public class WWColorPicker : Control
    {
        private const string PART_ToggleButton = "PART_ToggleButton";
        private const string PART_Popup = "PART_Popup";
        private const string PART_HueSlider = "PART_HueSlider";
        private const string PART_SaturationSlider = "PART_SaturationSlider";
        private const string PART_BrightnessSlider = "PART_BrightnessSlider";
        private const string PART_HexTextBox = "PART_HexTextBox";
        private const string PART_PresetColorsList = "PART_PresetColorsList";
        private const string PART_PreviewBorder = "PART_PreviewBorder";

        private bool _isUpdating;
        private TextBox _hexTextBox;
        private ListBox _presetColorsList;

        static WWColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWColorPicker),
                new FrameworkPropertyMetadata(typeof(WWColorPicker)));
        }

        public WWColorPicker()
        {
            PresetColors = BuildPresetColors();
            HueTrackBrush = BuildHueGradient();
            UpdateSaturationGradient();
            UpdateBrightnessGradient();
        }

        #region Dependency Properties

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(WWColorPicker),
                new FrameworkPropertyMetadata(Colors.DodgerBlue,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedColorChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(nameof(Hue), typeof(double), typeof(WWColorPicker),
                new PropertyMetadata(0.0, OnHsvChanged));

        public double Hue
        {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(nameof(Saturation), typeof(double), typeof(WWColorPicker),
                new PropertyMetadata(0.0, OnHsvChanged));

        public double Saturation
        {
            get => (double)GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        public static readonly DependencyProperty BrightnessProperty =
            DependencyProperty.Register(nameof(Brightness), typeof(double), typeof(WWColorPicker),
                new PropertyMetadata(0.0, OnHsvChanged));

        public double Brightness
        {
            get => (double)GetValue(BrightnessProperty);
            set => SetValue(BrightnessProperty, value);
        }

        public static readonly DependencyProperty HexTextProperty =
            DependencyProperty.Register(nameof(HexText), typeof(string), typeof(WWColorPicker),
                new PropertyMetadata("#1E90FF"));

        public string HexText
        {
            get => (string)GetValue(HexTextProperty);
            set => SetValue(HexTextProperty, value);
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(WWColorPicker),
                new PropertyMetadata(false));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty DisplayColorAndNameProperty =
            DependencyProperty.Register(nameof(DisplayColorAndName), typeof(bool), typeof(WWColorPicker),
                new PropertyMetadata(false));

        public bool DisplayColorAndName
        {
            get => (bool)GetValue(DisplayColorAndNameProperty);
            set => SetValue(DisplayColorAndNameProperty, value);
        }

        public static readonly DependencyProperty HueTrackBrushProperty =
            DependencyProperty.Register(nameof(HueTrackBrush), typeof(Brush), typeof(WWColorPicker),
                new PropertyMetadata(null));

        public Brush HueTrackBrush
        {
            get => (Brush)GetValue(HueTrackBrushProperty);
            set => SetValue(HueTrackBrushProperty, value);
        }

        public static readonly DependencyProperty SaturationTrackBrushProperty =
            DependencyProperty.Register(nameof(SaturationTrackBrush), typeof(Brush), typeof(WWColorPicker),
                new PropertyMetadata(null));

        public Brush SaturationTrackBrush
        {
            get => (Brush)GetValue(SaturationTrackBrushProperty);
            set => SetValue(SaturationTrackBrushProperty, value);
        }

        public static readonly DependencyProperty BrightnessTrackBrushProperty =
            DependencyProperty.Register(nameof(BrightnessTrackBrush), typeof(Brush), typeof(WWColorPicker),
                new PropertyMetadata(null));

        public Brush BrightnessTrackBrush
        {
            get => (Brush)GetValue(BrightnessTrackBrushProperty);
            set => SetValue(BrightnessTrackBrushProperty, value);
        }

        public static readonly DependencyProperty PresetColorsProperty =
            DependencyProperty.Register(nameof(PresetColors), typeof(List<Color>), typeof(WWColorPicker),
                new PropertyMetadata(null));

        public List<Color> PresetColors
        {
            get => (List<Color>)GetValue(PresetColorsProperty);
            set => SetValue(PresetColorsProperty, value);
        }

        #endregion

        #region Template

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_presetColorsList != null)
                _presetColorsList.SelectionChanged -= PresetColorsList_SelectionChanged;
            if (_hexTextBox != null)
                _hexTextBox.KeyDown -= HexTextBox_KeyDown;

            _hexTextBox = GetTemplateChild(PART_HexTextBox) as TextBox;
            _presetColorsList = GetTemplateChild(PART_PresetColorsList) as ListBox;

            if (_presetColorsList != null)
                _presetColorsList.SelectionChanged += PresetColorsList_SelectionChanged;
            if (_hexTextBox != null)
                _hexTextBox.KeyDown += HexTextBox_KeyDown;

            // Initialize from current color
            SyncFromSelectedColor(SelectedColor);
        }

        #endregion

        #region Event Handlers

        private void PresetColorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_presetColorsList?.SelectedItem is Color color)
            {
                SelectedColor = color;
                _presetColorsList.SelectedItem = null;
            }
        }

        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                ApplyHexText();
                e.Handled = true;
            }
        }

        private void ApplyHexText()
        {
            var text = HexText?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            if (!text.StartsWith("#"))
                text = "#" + text;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(text);
                color.A = 255; // Force full opacity
                SelectedColor = color;
            }
            catch
            {
                // Invalid hex — revert display
                HexText = ColorToHex(SelectedColor);
            }
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWColorPicker picker && !picker._isUpdating)
            {
                picker.SyncFromSelectedColor((Color)e.NewValue);
            }
        }

        private static void OnHsvChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWColorPicker picker && !picker._isUpdating)
            {
                picker.SyncFromHsv();
            }
        }

        private void SyncFromSelectedColor(Color color)
        {
            _isUpdating = true;
            try
            {
                var (h, s, v) = RgbToHsv(color);
                Hue = h;
                Saturation = s;
                Brightness = v;
                HexText = ColorToHex(color);
                UpdateSaturationGradient();
                UpdateBrightnessGradient();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void SyncFromHsv()
        {
            _isUpdating = true;
            try
            {
                var color = HsvToRgb(Hue, Saturation, Brightness);
                SelectedColor = color;
                HexText = ColorToHex(color);
                UpdateSaturationGradient();
                UpdateBrightnessGradient();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        #endregion

        #region Gradient Builders

        private static LinearGradientBrush BuildHueGradient()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            // Rainbow: Red -> Yellow -> Green -> Cyan -> Blue -> Magenta -> Red
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 1.0 / 6));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 2.0 / 6));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 3.0 / 6));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 4.0 / 6));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 255), 5.0 / 6));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1.0));
            brush.Freeze();
            return brush;
        }

        private void UpdateSaturationGradient()
        {
            var pureColor = HsvToRgb(Hue, 100, 100);
            SaturationTrackBrush = new LinearGradientBrush(
                Color.FromRgb(128, 128, 128), // gray at 0% saturation (with B=100)
                pureColor,
                new Point(0, 0.5),
                new Point(1, 0.5));
        }

        private void UpdateBrightnessGradient()
        {
            var currentColor = HsvToRgb(Hue, Saturation, 100);
            BrightnessTrackBrush = new LinearGradientBrush(
                Colors.Black,
                currentColor,
                new Point(0, 0.5),
                new Point(1, 0.5));
        }

        #endregion

        #region HSV <-> RGB Conversion

        public static (double h, double s, double v) RgbToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0.0001)
            {
                if (max == r)
                    h = 60.0 * (((g - b) / delta) % 6);
                else if (max == g)
                    h = 60.0 * (((b - r) / delta) + 2);
                else
                    h = 60.0 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = (max > 0.0001) ? (delta / max) * 100 : 0;
            double v = max * 100;

            return (Math.Round(h, 1), Math.Round(s, 1), Math.Round(v, 1));
        }

        public static Color HsvToRgb(double h, double s, double v)
        {
            double S = s / 100.0;
            double V = v / 100.0;
            double C = V * S;
            double hh = h / 60.0;
            double X = C * (1 - Math.Abs((hh % 2) - 1));

            double r1 = 0, g1 = 0, b1 = 0;

            if (hh < 1) { r1 = C; g1 = X; }
            else if (hh < 2) { r1 = X; g1 = C; }
            else if (hh < 3) { g1 = C; b1 = X; }
            else if (hh < 4) { g1 = X; b1 = C; }
            else if (hh < 5) { r1 = X; b1 = C; }
            else { r1 = C; b1 = X; }

            double m = V - C;
            return Color.FromRgb(
                (byte)Math.Round((r1 + m) * 255),
                (byte)Math.Round((g1 + m) * 255),
                (byte)Math.Round((b1 + m) * 255));
        }

        #endregion

        #region Helpers

        private static string ColorToHex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        private static List<Color> BuildPresetColors()
        {
            return new List<Color>
            {
                // Neutrals
                Color.FromRgb(255, 255, 255), // White
                Color.FromRgb(224, 224, 224), // Light gray
                Color.FromRgb(189, 189, 189), // Gray
                Color.FromRgb(158, 158, 158), // Medium gray
                Color.FromRgb(117, 117, 117), // Dark gray
                Color.FromRgb(97, 97, 97),    // Darker gray
                Color.FromRgb(66, 66, 66),    // Very dark gray
                Color.FromRgb(33, 33, 33),    // Near black
                Color.FromRgb(0, 0, 0),       // Black

                // Reds
                Color.FromRgb(255, 205, 210), // Red 100
                Color.FromRgb(239, 83, 80),   // Red 400
                Color.FromRgb(211, 47, 47),   // Red 700
                Color.FromRgb(183, 28, 28),   // Red 900

                // Oranges
                Color.FromRgb(255, 224, 178), // Orange 100
                Color.FromRgb(255, 167, 38),  // Orange 400
                Color.FromRgb(245, 124, 0),   // Orange 700
                Color.FromRgb(230, 81, 0),    // Orange 900

                // Yellows
                Color.FromRgb(255, 249, 196), // Yellow 100
                Color.FromRgb(255, 238, 88),  // Yellow 400
                Color.FromRgb(251, 192, 45),  // Yellow 700
                Color.FromRgb(245, 127, 23),  // Amber 900

                // Greens
                Color.FromRgb(200, 230, 201), // Green 100
                Color.FromRgb(102, 187, 106), // Green 400
                Color.FromRgb(56, 142, 60),   // Green 700
                Color.FromRgb(27, 94, 32),    // Green 900

                // Teals
                Color.FromRgb(178, 223, 219), // Teal 100
                Color.FromRgb(38, 166, 154),  // Teal 400
                Color.FromRgb(0, 121, 107),   // Teal 700
                Color.FromRgb(0, 77, 64),     // Teal 900

                // Blues
                Color.FromRgb(187, 222, 251), // Blue 100
                Color.FromRgb(66, 165, 245),  // Blue 400
                Color.FromRgb(25, 118, 210),  // Blue 700
                Color.FromRgb(13, 71, 161),   // Blue 900

                // Purples
                Color.FromRgb(225, 190, 231), // Purple 100
                Color.FromRgb(171, 71, 188),  // Purple 400
                Color.FromRgb(123, 31, 162),  // Purple 700
                Color.FromRgb(74, 20, 140),   // Purple 900

                // Pinks
                Color.FromRgb(248, 187, 208), // Pink 100
                Color.FromRgb(236, 64, 122),  // Pink 400
                Color.FromRgb(194, 24, 91),   // Pink 700
                Color.FromRgb(136, 14, 79),   // Pink 900
            };
        }

        #endregion
    }
}
