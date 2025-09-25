using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Registry of all filter types and their metadata
    /// </summary>
    internal static class SearchTypeRegistry
    {
        private static readonly Dictionary<SearchType, SearchTypeMetadata> Registry;

        static SearchTypeRegistry()
        {
            Registry = new Dictionary<SearchType, SearchTypeMetadata>
            {
                // Single ComboBox filters
                [SearchType.Equals] = new SearchTypeMetadata(SearchType.Equals, "Equals",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean, ColumnDataType.Enum),

                [SearchType.NotEquals] = new SearchTypeMetadata(SearchType.NotEquals, "Does not equal",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean, ColumnDataType.Enum),

                [SearchType.GreaterThan] = new SearchTypeMetadata(SearchType.GreaterThan, "Is greater than",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.GreaterThanOrEqualTo] = new SearchTypeMetadata(SearchType.GreaterThanOrEqualTo, "Is greater than or equal to",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.LessThan] = new SearchTypeMetadata(SearchType.LessThan, "Is less than",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.LessThanOrEqualTo] = new SearchTypeMetadata(SearchType.LessThanOrEqualTo, "Is less than or equal to",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.Contains] = new SearchTypeMetadata(SearchType.Contains, "Contains",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                [SearchType.DoesNotContain] = new SearchTypeMetadata(SearchType.DoesNotContain, "Does not contain",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                [SearchType.StartsWith] = new SearchTypeMetadata(SearchType.StartsWith, "Starts with",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                [SearchType.EndsWith] = new SearchTypeMetadata(SearchType.EndsWith, "Ends with",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                [SearchType.IsLike] = new SearchTypeMetadata(SearchType.IsLike, "Is like",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                [SearchType.IsNotLike] = new SearchTypeMetadata(SearchType.IsNotLike, "Is not like",
                    FilterInputTemplate.SingleSearchTextBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                // Dual ComboBox filters
                [SearchType.Between] = new SearchTypeMetadata(SearchType.Between, "Is between",
                    FilterInputTemplate.DualSearchTextBox, ColumnDataType.Number),

                [SearchType.NotBetween] = new SearchTypeMetadata(SearchType.NotBetween, "Is not between",
                    FilterInputTemplate.DualSearchTextBox, ColumnDataType.Number),

                // Dual DateTime filters
                [SearchType.BetweenDates] = new SearchTypeMetadata(SearchType.BetweenDates, "Is between dates",
                    FilterInputTemplate.DualDateTimePicker, ColumnDataType.DateTime),

                // Numeric UpDown filters
                [SearchType.TopN] = new SearchTypeMetadata(SearchType.TopN, "Top N",
                    FilterInputTemplate.NumericUpDown, ColumnDataType.Number)
                { RequiresCollection = true },

                [SearchType.BottomN] = new SearchTypeMetadata(SearchType.BottomN, "Bottom N",
                    FilterInputTemplate.NumericUpDown, ColumnDataType.Number)
                { RequiresCollection = true },

                // No input filters
                [SearchType.AboveAverage] = new SearchTypeMetadata(SearchType.AboveAverage, "Above average",
                    FilterInputTemplate.NoInput, ColumnDataType.Number)
                { RequiresCollection = true },

                [SearchType.BelowAverage] = new SearchTypeMetadata(SearchType.BelowAverage, "Below average",
                    FilterInputTemplate.NoInput, ColumnDataType.Number)
                { RequiresCollection = true },

                [SearchType.IsNull] = new SearchTypeMetadata(SearchType.IsNull, "Is null",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean, ColumnDataType.Enum),

                [SearchType.IsNotNull] = new SearchTypeMetadata(SearchType.IsNotNull, "Is not null",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean, ColumnDataType.Enum),

                [SearchType.Unique] = new SearchTypeMetadata(SearchType.Unique, "Unique",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime)
                { RequiresCollection = true },

                [SearchType.Duplicate] = new SearchTypeMetadata(SearchType.Duplicate, "Duplicate",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime)
                { RequiresCollection = true },

                [SearchType.Yesterday] = new SearchTypeMetadata(SearchType.Yesterday, "Is yesterday",
                    FilterInputTemplate.NoInput, ColumnDataType.DateTime),

                [SearchType.Today] = new SearchTypeMetadata(SearchType.Today, "Is today",
                    FilterInputTemplate.NoInput, ColumnDataType.DateTime),

                // List-based filters
                [SearchType.IsAnyOf] = new SearchTypeMetadata(
                    SearchType.IsAnyOf, "Is any of",
                    FilterInputTemplate.SearchTextBoxList, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.Enum),

                [SearchType.IsNoneOf] = new SearchTypeMetadata(SearchType.IsNoneOf, "Is none of",
                    FilterInputTemplate.SearchTextBoxList, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean, ColumnDataType.Enum),

                [SearchType.IsOnAnyOfDates] = new SearchTypeMetadata(SearchType.IsOnAnyOfDates, "Is on any of the following",
                    FilterInputTemplate.DateTimePickerList, ColumnDataType.DateTime),

                // Date interval filter
                [SearchType.DateInterval] = new SearchTypeMetadata(SearchType.DateInterval, "Date intervals",
                    FilterInputTemplate.DateIntervalCheckList, ColumnDataType.DateTime)
            };
        }

        public static SearchTypeMetadata GetMetadata(SearchType searchType)
        {
            return Registry.TryGetValue(searchType, out var metadata) ? metadata : null;
        }

        public static IEnumerable<SearchTypeMetadata> GetFiltersForDataType(ColumnDataType dataType)
        {
            return Registry.Values.Where(m => m.SupportedDataTypes.Contains(dataType));
        }

        /// <summary>
        /// Gets filter types for a data type, optionally filtering out null-related search types for non-nullable types
        /// </summary>
        /// <param name="dataType">The column data type</param>
        /// <param name="isNullable">Whether the column type allows null values</param>
        /// <returns>Filtered collection of filter type metadata</returns>
        public static IEnumerable<SearchTypeMetadata> GetFiltersForDataType(ColumnDataType dataType, bool isNullable)
        {
            var baseFilters = Registry.Values.Where(m => m.SupportedDataTypes.Contains(dataType));

            // If the type is not nullable, filter out null-related search types
            if (!isNullable)
            {
                baseFilters = baseFilters.Where(m => 
                    m.SearchType != SearchType.IsNull && 
                    m.SearchType != SearchType.IsNotNull);
            }

            return baseFilters;
        }

        public static bool IsValidForDataType(SearchType searchType, ColumnDataType dataType)
        {
            var metadata = GetMetadata(searchType);
            return metadata?.SupportedDataTypes.Contains(dataType) ?? false;
        }
    }
}
