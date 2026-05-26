using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converts a <see cref="SearchType"/> to the matching <see cref="DrawingImage"/> declared
    /// in <c>Resources/Icons/SearchTypeIcons.xaml</c>. Resolves via the typed
    /// <see cref="SearchTypeIconKeys"/> <see cref="ComponentResourceKey"/>s so the icon set
    /// stays out of the consumer's loose-string namespace.
    /// </summary>
    public class SearchTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SearchType searchType)
            {
                var key = GetIconKey(searchType);
                if (key != null && Application.Current?.TryFindResource(key) is DrawingImage drawingImage)
                {
                    return drawingImage;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SearchTypeToIconConverter is one-way only");
        }

        private static ComponentResourceKey GetIconKey(SearchType searchType) => searchType switch
        {
            SearchType.Equals => SearchTypeIconKeys.Equals,
            SearchType.NotEquals => SearchTypeIconKeys.NotEquals,
            SearchType.GreaterThan => SearchTypeIconKeys.GreaterThan,
            SearchType.GreaterThanOrEqualTo => SearchTypeIconKeys.GreaterThanOrEqualTo,
            SearchType.LessThan => SearchTypeIconKeys.LessThan,
            SearchType.LessThanOrEqualTo => SearchTypeIconKeys.LessThanOrEqualTo,
            SearchType.Between => SearchTypeIconKeys.Between,
            SearchType.NotBetween => SearchTypeIconKeys.NotBetween,
            SearchType.Contains => SearchTypeIconKeys.Contains,
            SearchType.DoesNotContain => SearchTypeIconKeys.DoesNotContain,
            SearchType.StartsWith => SearchTypeIconKeys.StartsWith,
            SearchType.EndsWith => SearchTypeIconKeys.EndsWith,
            SearchType.IsLike => SearchTypeIconKeys.IsLike,
            SearchType.IsNotLike => SearchTypeIconKeys.IsNotLike,
            SearchType.IsAnyOf => SearchTypeIconKeys.IsAnyOf,
            SearchType.IsNoneOf => SearchTypeIconKeys.IsNoneOf,
            SearchType.TopN => SearchTypeIconKeys.TopN,
            SearchType.BottomN => SearchTypeIconKeys.BottomN,
            SearchType.AboveAverage => SearchTypeIconKeys.AboveAverage,
            SearchType.BelowAverage => SearchTypeIconKeys.BelowAverage,
            SearchType.IsNull => SearchTypeIconKeys.IsNull,
            SearchType.IsNotNull => SearchTypeIconKeys.IsNotNull,
            SearchType.Today => SearchTypeIconKeys.Today,
            SearchType.Yesterday => SearchTypeIconKeys.Yesterday,
            SearchType.DateInterval => SearchTypeIconKeys.Today,
            SearchType.BetweenDates => SearchTypeIconKeys.Between,
            SearchType.NotBetweenDates => SearchTypeIconKeys.NotBetween,
            SearchType.Unique => SearchTypeIconKeys.Unique,
            SearchType.Duplicate => SearchTypeIconKeys.Duplicate,
            _ => null
        };
    }
}
