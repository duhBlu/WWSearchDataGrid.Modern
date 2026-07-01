using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WWControls.Core;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// Converts a <see cref="DateInterval"/> to the matching <see cref="DrawingImage"/> declared
    /// in <c>Resources/Icons/SearchTypeIcons.xaml</c>. Resolves via the typed
    /// <see cref="SearchTypeIconKeys"/> <see cref="ComponentResourceKey"/>s so the icon set
    /// stays out of the consumer's loose-string namespace.
    /// </summary>
    public class DateIntervalToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateInterval dateInterval)
            {
                var key = GetIconKey(dateInterval);
                if (key != null && Application.Current?.TryFindResource(key) is DrawingImage drawingImage)
                {
                    return drawingImage;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("DateIntervalToIconConverter is one-way only");
        }

        private static ComponentResourceKey GetIconKey(DateInterval dateInterval) => dateInterval switch
        {
            DateInterval.PriorThisYear => SearchTypeIconKeys.PriorThisYear,
            DateInterval.EarlierThisYear => SearchTypeIconKeys.EarlierThisYear,
            DateInterval.LaterThisYear => SearchTypeIconKeys.LaterThisYear,
            DateInterval.BeyondThisYear => SearchTypeIconKeys.BeyondThisYear,
            DateInterval.EarlierThisMonth => SearchTypeIconKeys.EarlierThisMonth,
            DateInterval.LaterThisMonth => SearchTypeIconKeys.LaterThisMonth,
            DateInterval.EarlierThisWeek => SearchTypeIconKeys.EarlierThisWeek,
            DateInterval.LaterThisWeek => SearchTypeIconKeys.LaterThisWeek,
            DateInterval.LastWeek => SearchTypeIconKeys.LastWeek,
            DateInterval.NextWeek => SearchTypeIconKeys.NextWeek,
            DateInterval.Yesterday => SearchTypeIconKeys.Yesterday,
            DateInterval.Today => SearchTypeIconKeys.Today,
            DateInterval.Tomorrow => SearchTypeIconKeys.Tomorrow,
            _ => null
        };
    }
}
