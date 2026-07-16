using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WWControls.Wpf.Controls.Primitives;

namespace WWControls.SampleApp.Editors.Views.Samples.Buttons
{
    /// <summary>The glyphs the button playground offers, mapped to <see cref="IconKeys"/> resources.</summary>
    public enum GlyphChoice
    {
        None,
        Add,
        Edit,
        Calendar,
        Filter,
        Copy,
        Check,
    }

    /// <summary>
    /// Resolves a <see cref="GlyphChoice"/> to the corresponding <see cref="IconKeys"/>
    /// <see cref="DrawingImage"/> resource (<see cref="GlyphChoice.None"/> → no glyph). Looked up
    /// from the app resources, where the Primitives theme registers the icon keys.
    /// </summary>
    public sealed class GlyphChoiceToImageConverter : IValueConverter
    {
        public static GlyphChoiceToImageConverter Instance { get; } = new GlyphChoiceToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ComponentResourceKey key = (value as GlyphChoice?) switch
            {
                GlyphChoice.Add => IconKeys.IconAdd,
                GlyphChoice.Edit => IconKeys.IconEdit,
                GlyphChoice.Calendar => IconKeys.IconCalendar,
                GlyphChoice.Filter => IconKeys.IconFilter,
                GlyphChoice.Copy => IconKeys.IconCopy,
                GlyphChoice.Check => IconKeys.IconCheck,
                _ => null,
            };
            return key == null ? null : Application.Current?.TryFindResource(key) as ImageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Maps a uniform slider value to a <see cref="CornerRadius"/> for the button chrome.</summary>
    public sealed class DoubleToCornerRadiusConverter : IValueConverter
    {
        public static DoubleToCornerRadiusConverter Instance { get; } = new DoubleToCornerRadiusConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => new CornerRadius(value is double d ? d : 0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is CornerRadius c ? c.TopLeft : 0d;
    }
}
