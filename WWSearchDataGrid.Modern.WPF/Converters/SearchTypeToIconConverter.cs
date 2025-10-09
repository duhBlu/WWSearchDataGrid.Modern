using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converts SearchType enum values to their corresponding DrawingImage icons
    /// </summary>
    public class SearchTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SearchType searchType)
            {
                var resourceKey = GetIconResourceKey(searchType);
                if (!string.IsNullOrEmpty(resourceKey))
                {
                    // Try to find the resource in the current application resources
                    var resource = Application.Current?.FindResource(resourceKey);
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
            throw new NotImplementedException("SearchTypeToIconConverter is one-way only");
        }

        private string GetIconResourceKey(SearchType searchType)
        {
            return searchType switch
            {
                SearchType.Equals => "EqualsDrawingImage",
                SearchType.NotEquals => "Does_not_equalDrawingImage",
                SearchType.GreaterThan => "Is_greater_thanDrawingImage",
                SearchType.GreaterThanOrEqualTo => "Is_greater_than_or_equal_toDrawingImage",
                SearchType.LessThan => "Is_less_thanDrawingImage",
                SearchType.LessThanOrEqualTo => "Is_less_than_or_equal_toDrawingImage",
                SearchType.Between => "Is_betweenDrawingImage",
                SearchType.NotBetween => "Is_not_betweenDrawingImage",
                SearchType.Contains => "ContainsDrawingImage",
                SearchType.DoesNotContain => "Does_not_containDrawingImage",
                SearchType.StartsWith => "Starts_withDrawingImage",
                SearchType.EndsWith => "Ends_withDrawingImage",
                SearchType.IsLike => "Is_likeDrawingImage",
                SearchType.IsNotLike => "Is_not_likeDrawingImage",
                SearchType.IsAnyOf => "Is_any_ofDrawingImage",
                SearchType.IsNoneOf => "Is_none_ofDrawingImage",
                SearchType.TopN => "Top_NDrawingImage",
                SearchType.BottomN => "Bottom_NDrawingImage",
                SearchType.AboveAverage => "Above_averageDrawingImage",
                SearchType.BelowAverage => "Below_AverageDrawingImage",
                SearchType.IsNull => "Is_null_or_blankDrawingImage",
                SearchType.IsNotNull => "Is_not_null_or_blankDrawingImage",
                SearchType.Unique => "UniqueDrawingImage",
                SearchType.Duplicate => "DuplicateDrawingImage",
                SearchType.Today => "Is_TodayDrawingImage",
                SearchType.Yesterday => "Is_YesterdayDrawingImage",
                SearchType.DateInterval => "Is_TodayDrawingImage", // Default for date interval
                SearchType.BetweenDates => "Is_betweenDrawingImage", // Reuse between icon
                _ => null
            };
        }
    }
}