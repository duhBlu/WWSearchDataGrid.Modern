using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// Passes a non-null <see cref="Brush"/> through and maps a null brush to
    /// <see cref="DependencyProperty.UnsetValue"/>. Bound on the summary segment
    /// <see cref="System.Windows.Documents.Run"/>'s <c>Foreground</c> so an unset segment color
    /// (<see cref="SummaryTextStyle.Foreground"/> = null) leaves the run inheriting the hosting
    /// TextBlock's foreground instead of forcing a null (invisible) brush.
    /// </summary>
    public sealed class BrushToUnsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Brush brush ? brush : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Maps a non-default <see cref="System.Windows.FontWeight"/> through and a
    /// <see cref="FontWeights.Normal"/> weight to <see cref="DependencyProperty.UnsetValue"/>, so a
    /// segment with no weight override inherits the hosting TextBlock's weight (e.g. the fixed
    /// total panel's SemiBold) instead of forcing Normal.
    /// </summary>
    public sealed class FontWeightToUnsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is FontWeight weight && weight != FontWeights.Normal ? weight : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Maps a non-default <see cref="System.Windows.FontStyle"/> through and a
    /// <see cref="FontStyles.Normal"/> style to <see cref="DependencyProperty.UnsetValue"/>, so a
    /// segment with no slant override inherits the hosting TextBlock's font style.
    /// </summary>
    public sealed class FontStyleToUnsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is FontStyle style && style != FontStyles.Normal ? style : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
