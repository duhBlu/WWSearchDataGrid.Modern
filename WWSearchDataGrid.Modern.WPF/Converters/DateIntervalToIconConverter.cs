using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converts DateInterval enum values to their corresponding DrawingImage icons
    /// </summary>
    public class DateIntervalToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateInterval dateInterval)
            {
                var resourceKey = GetIconResourceKey(dateInterval);
                if (!string.IsNullOrEmpty(resourceKey))
                {
                    // Try to find the resource in the current application resources
                    var resource = System.Windows.Application.Current?.FindResource(resourceKey);
                    if (resource is DrawingImage drawingImage)
                    {
                        return drawingImage;
                    }
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("DateIntervalToIconConverter is one-way only");
        }

        private string GetIconResourceKey(DateInterval dateInterval)
        {
            return dateInterval switch
            {
                DateInterval.PriorThisYear => "Is_Prior_This_YearDrawingImage",
                DateInterval.EarlierThisYear => "Is_Earlier_This_YearDrawingImage",
                DateInterval.LaterThisYear => "Is_Later_This_YearDrawingImage",
                DateInterval.BeyondThisYear => "Is_Beyond_This_YearDrawingImage",
                DateInterval.EarlierThisMonth => "Is_Earlier_This_MonthDrawingImage",
                DateInterval.LaterThisMonth => "Is_Later_This_MonthDrawingImage",
                DateInterval.EarlierThisWeek => "Is_Earlier_This_WeekDrawingImage",
                DateInterval.LaterThisWeek => "Is_Later_This_WeekDrawingImage",
                DateInterval.LastWeek => "Is_Last_WeekDrawingImage",
                DateInterval.NextWeek => "Is_Next_WeekDrawingImage",
                DateInterval.Yesterday => "Is_YesterdayDrawingImage",
                DateInterval.Today => "Is_TodayDrawingImage",
                DateInterval.Tomorrow => "Is_TomorrowDrawingImage",
                _ => null
            };
        }
    }
}