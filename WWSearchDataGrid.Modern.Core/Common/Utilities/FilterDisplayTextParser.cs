using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Utility class for parsing display text into filter chip components
    /// </summary>
    internal static class FilterDisplayTextParser
    {
        /// <summary>
        /// Parses display text into individual filter chip components
        /// </summary>
        /// <param name="displayText">The display text to parse</param>
        /// <param name="searchType">The search type for categorization</param>
        /// <returns>Parsed filter chip components</returns>
        public static FilterChipComponents ParseDisplayText(string displayText, SearchType searchType)
        {
            if (string.IsNullOrWhiteSpace(displayText))
            {
                return new FilterChipComponents
                {
                    SearchTypeText = "No filter",
                    HasNoInputValues = true
                };
            }

            var components = new FilterChipComponents
            {
                IsDateInterval = IsDateIntervalType(searchType),
                HasNoInputValues = IsNoInputValueType(searchType)
            };

            // Extract components based on search type patterns
            ExtractComponentsBySearchType(displayText, searchType, components);

            return components;
        }

        private static void ExtractComponentsBySearchType(string displayText, SearchType searchType, FilterChipComponents components)
        {
            switch (searchType)
            {
                case SearchType.Contains:
                    ExtractSingleValueFilter(displayText, "Contains", components);
                    break;
                case SearchType.DoesNotContain:
                    ExtractSingleValueFilter(displayText, "Does not contain", components);
                    break;
                case SearchType.Equals:
                    ExtractSingleValueFilter(displayText, "=", components);
                    break;
                case SearchType.NotEquals:
                    ExtractSingleValueFilter(displayText, "≠", components);
                    break;
                case SearchType.StartsWith:
                    ExtractSingleValueFilter(displayText, "Starts with", components);
                    break;
                case SearchType.EndsWith:
                    ExtractSingleValueFilter(displayText, "Ends with", components);
                    break;
                case SearchType.GreaterThan:
                    ExtractSingleValueFilter(displayText, ">", components);
                    break;
                case SearchType.GreaterThanOrEqualTo:
                    ExtractSingleValueFilter(displayText, ">=", components);
                    break;
                case SearchType.LessThan:
                    ExtractSingleValueFilter(displayText, "<", components);
                    break;
                case SearchType.LessThanOrEqualTo:
                    ExtractSingleValueFilter(displayText, "<=", components);
                    break;
                case SearchType.IsLike:
                    ExtractSingleValueFilter(displayText, "Is like", components);
                    break;
                case SearchType.IsNotLike:
                    ExtractSingleValueFilter(displayText, "Is not like", components);
                    break;
                case SearchType.Between:
                case SearchType.NotBetween:
                    ExtractBetweenFilter(displayText, components);
                    break;
                case SearchType.BetweenDates:
                    ExtractBetweenDatesFilter(displayText, components);
                    break;
                case SearchType.TopN:
                    ExtractSingleValueFilter(displayText, "Top", components);
                    break;
                case SearchType.BottomN:
                    ExtractSingleValueFilter(displayText, "Bottom", components);
                    break;
                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                case SearchType.IsOnAnyOfDates:
                    ExtractMultiValueFilter(displayText, components);
                    break;
                case SearchType.DateInterval:
                    ExtractDateIntervalFilter(displayText, components);
                    break;
                // No-input types
                case SearchType.IsNull:
                    components.SearchTypeText = "Is null";
                    break;
                case SearchType.IsNotNull:
                    components.SearchTypeText = "Is not null";
                    break;
                case SearchType.AboveAverage:
                    components.SearchTypeText = "Above average";
                    break;
                case SearchType.BelowAverage:
                    components.SearchTypeText = "Below average";
                    break;
                case SearchType.Unique:
                    components.SearchTypeText = "Unique values";
                    break;
                case SearchType.Duplicate:
                    components.SearchTypeText = "Duplicate values";
                    break;
                case SearchType.Today:
                    components.SearchTypeText = "Is today";
                    break;
                case SearchType.Yesterday:
                    components.SearchTypeText = "Is yesterday";
                    break;
                default:
                    // Fallback - use the entire display text as search type
                    components.SearchTypeText = displayText;
                    break;
            }
        }

        private static void ExtractSingleValueFilter(string displayText, string searchTypeText, FilterChipComponents components)
        {
            components.SearchTypeText = searchTypeText;
            
            // Extract value between quotes or after the search type
            var match = Regex.Match(displayText, @"'([^']*)'");
            if (match.Success)
            {
                components.PrimaryValue = match.Groups[1].Value;
            }
            else
            {
                // Try to extract value after the search type text
                var index = displayText.IndexOf(searchTypeText, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var valueText = displayText.Substring(index + searchTypeText.Length).Trim();
                    components.PrimaryValue = valueText;
                }
            }
        }

        private static void ExtractBetweenFilter(string displayText, FilterChipComponents components)
        {
            if (displayText.StartsWith("Not between", StringComparison.OrdinalIgnoreCase))
            {
                components.SearchTypeText = "Not between";
            }
            else
            {
                components.SearchTypeText = "Between";
            }

            // Extract values between quotes with "and" separator
            var matches = Regex.Matches(displayText, @"'([^']*)'");
            if (matches.Count >= 2)
            {
                components.PrimaryValue = matches[0].Groups[1].Value;
                components.SecondaryValue = matches[1].Groups[1].Value;
                components.ValueOperatorText = "And";
            }
        }

        private static void ExtractBetweenDatesFilter(string displayText, FilterChipComponents components)
        {
            components.SearchTypeText = "Between dates";
            components.IsDateInterval = true;

            // Extract date values between quotes
            var matches = Regex.Matches(displayText, @"'([^']*)'");
            if (matches.Count >= 2)
            {
                components.PrimaryValue = matches[0].Groups[1].Value;
                components.SecondaryValue = matches[1].Groups[1].Value;
                components.ValueOperatorText = "And";
            }
        }

        private static void ExtractMultiValueFilter(string displayText, FilterChipComponents components)
        {
            // Extract the search type (text before the brackets)
            var bracketIndex = displayText.IndexOf('[');
            if (bracketIndex > 0)
            {
                components.SearchTypeText = displayText.Substring(0, bracketIndex).Trim();
                components.PrimaryValue = displayText.Substring(bracketIndex);
            }
            else
            {
                components.SearchTypeText = displayText;
            }
        }

        private static void ExtractDateIntervalFilter(string displayText, FilterChipComponents components)
        {
            components.SearchTypeText = "Date interval";
            components.IsDateInterval = true;
            components.PrimaryValue = displayText;
        }

        private static bool IsDateIntervalType(SearchType searchType)
        {
            return searchType == SearchType.DateInterval ||
                   searchType == SearchType.BetweenDates ||
                   searchType == SearchType.IsOnAnyOfDates;
        }

        private static bool IsNoInputValueType(SearchType searchType)
        {
            return searchType == SearchType.IsNull ||
                   searchType == SearchType.IsNotNull ||
                   searchType == SearchType.AboveAverage ||
                   searchType == SearchType.BelowAverage ||
                   searchType == SearchType.Unique ||
                   searchType == SearchType.Duplicate ||
                   searchType == SearchType.Today ||
                   searchType == SearchType.Yesterday;
        }
    }

}