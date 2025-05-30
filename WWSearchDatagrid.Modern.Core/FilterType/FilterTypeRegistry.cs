using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Registry of all filter types and their metadata
    /// </summary>
    public static class FilterTypeRegistry
    {
        private static readonly Dictionary<SearchType, FilterTypeMetadata> Registry;

        static FilterTypeRegistry()
        {
            Registry = new Dictionary<SearchType, FilterTypeMetadata>
            {
                // Single ComboBox filters
                [SearchType.Equals] = new FilterTypeMetadata(SearchType.Equals, "Equals",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean),

                [SearchType.NotEquals] = new FilterTypeMetadata(SearchType.NotEquals, "Does not equal",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean),

                [SearchType.GreaterThan] = new FilterTypeMetadata(SearchType.GreaterThan, "Is greater than",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.GreaterThanOrEqualTo] = new FilterTypeMetadata(SearchType.GreaterThanOrEqualTo, "Is greater than or equal to",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.LessThan] = new FilterTypeMetadata(SearchType.LessThan, "Is less than",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.LessThanOrEqualTo] = new FilterTypeMetadata(SearchType.LessThanOrEqualTo, "Is less than or equal to",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.Number, ColumnDataType.DateTime),

                [SearchType.Contains] = new FilterTypeMetadata(SearchType.Contains, "Contains",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String),

                [SearchType.DoesNotContain] = new FilterTypeMetadata(SearchType.DoesNotContain, "Does not contain",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String),

                [SearchType.StartsWith] = new FilterTypeMetadata(SearchType.StartsWith, "Starts with",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String),

                [SearchType.EndsWith] = new FilterTypeMetadata(SearchType.EndsWith, "Ends with",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String),

                [SearchType.IsLike] = new FilterTypeMetadata(SearchType.IsLike, "Is like",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String),

                [SearchType.IsNotLike] = new FilterTypeMetadata(SearchType.IsNotLike, "Is not like",
                    FilterInputTemplate.SingleComboBox, ColumnDataType.String),

                // Dual ComboBox filters
                [SearchType.Between] = new FilterTypeMetadata(SearchType.Between, "Is between",
                    FilterInputTemplate.DualComboBox, ColumnDataType.Number, ColumnDataType.String),

                [SearchType.NotBetween] = new FilterTypeMetadata(SearchType.NotBetween, "Is not between",
                    FilterInputTemplate.DualComboBox, ColumnDataType.Number, ColumnDataType.String),

                // Dual DateTime filters
                [SearchType.BetweenDates] = new FilterTypeMetadata(SearchType.BetweenDates, "Is between dates",
                    FilterInputTemplate.DualDateTimePicker, ColumnDataType.DateTime),

                // Numeric UpDown filters
                [SearchType.TopN] = new FilterTypeMetadata(SearchType.TopN, "Top N",
                    FilterInputTemplate.NumericUpDown, ColumnDataType.Number)
                { RequiresCollection = true },

                [SearchType.BottomN] = new FilterTypeMetadata(SearchType.BottomN, "Bottom N",
                    FilterInputTemplate.NumericUpDown, ColumnDataType.Number)
                { RequiresCollection = true },

                // No input filters
                [SearchType.AboveAverage] = new FilterTypeMetadata(SearchType.AboveAverage, "Above average",
                    FilterInputTemplate.NoInput, ColumnDataType.Number)
                { RequiresCollection = true },

                [SearchType.BelowAverage] = new FilterTypeMetadata(SearchType.BelowAverage, "Below average",
                    FilterInputTemplate.NoInput, ColumnDataType.Number)
                { RequiresCollection = true },

                [SearchType.IsEmpty] = new FilterTypeMetadata(SearchType.IsEmpty, "Is blank",
                    FilterInputTemplate.NoInput, ColumnDataType.String),

                [SearchType.IsNotEmpty] = new FilterTypeMetadata(SearchType.IsNotEmpty, "Is not blank",
                    FilterInputTemplate.NoInput, ColumnDataType.String),

                [SearchType.IsNull] = new FilterTypeMetadata(SearchType.IsNull, "Is null",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean),

                [SearchType.IsNotNull] = new FilterTypeMetadata(SearchType.IsNotNull, "Is not null",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime, ColumnDataType.Boolean),

                [SearchType.Unique] = new FilterTypeMetadata(SearchType.Unique, "Unique",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime)
                { RequiresCollection = true },

                [SearchType.Duplicate] = new FilterTypeMetadata(SearchType.Duplicate, "Duplicate",
                    FilterInputTemplate.NoInput, ColumnDataType.String, ColumnDataType.Number, ColumnDataType.DateTime)
                { RequiresCollection = true },

                [SearchType.Yesterday] = new FilterTypeMetadata(SearchType.Yesterday, "Is yesterday",
                    FilterInputTemplate.NoInput, ColumnDataType.DateTime),

                [SearchType.Today] = new FilterTypeMetadata(SearchType.Today, "Is today",
                    FilterInputTemplate.NoInput, ColumnDataType.DateTime),

                // List-based filters
                [SearchType.IsAnyOf] = new FilterTypeMetadata(SearchType.IsAnyOf, "Is any of",
                    FilterInputTemplate.ComboBoxList, ColumnDataType.String, ColumnDataType.Number),

                [SearchType.IsNoneOf] = new FilterTypeMetadata(SearchType.IsNoneOf, "Is none of",
                    FilterInputTemplate.ComboBoxList, ColumnDataType.String, ColumnDataType.Number),

                [SearchType.IsOnAnyOfDates] = new FilterTypeMetadata(SearchType.IsOnAnyOfDates, "Is on any of the following",
                    FilterInputTemplate.DateTimePickerList, ColumnDataType.DateTime),

                // Date interval filter
                [SearchType.DateInterval] = new FilterTypeMetadata(SearchType.DateInterval, "Date intervals",
                    FilterInputTemplate.DateIntervalCheckList, ColumnDataType.DateTime)
            };
        }

        public static FilterTypeMetadata GetMetadata(SearchType searchType)
        {
            return Registry.TryGetValue(searchType, out var metadata) ? metadata : null;
        }

        public static IEnumerable<FilterTypeMetadata> GetFiltersForDataType(ColumnDataType dataType)
        {
            return Registry.Values.Where(m => m.SupportedDataTypes.Contains(dataType));
        }

        public static bool IsValidForDataType(SearchType searchType, ColumnDataType dataType)
        {
            var metadata = GetMetadata(searchType);
            return metadata?.SupportedDataTypes.Contains(dataType) ?? false;
        }
    }
}
