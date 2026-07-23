using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// Maps a Filter Editor node's <c>Depth</c> to a background brush, cycling through a small
    /// palette so nested groups read as progressively shaded cards.
    /// </summary>
    public class FilterEditorDepthToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush[] Shades =
        {
            Frozen(0xF4, 0xF7, 0xFB),
            Frozen(0xE8, 0xEF, 0xF7),
            Frozen(0xDC, 0xE6, 0xF2),
            Frozen(0xD1, 0xDE, 0xEE),
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int depth = value is int i && i > 0 ? i : 0;
            return Shades[depth % Shades.Length];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        private static SolidColorBrush Frozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
