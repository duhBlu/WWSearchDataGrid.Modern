using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced search engine with support for new filter types
    /// </summary>
    public static class SearchEngine
    {
        /// <summary>
        /// Compares a column value against a search condition value
        /// </summary>
        /// <param name="columnValue">The value from the column</param>
        /// <param name="searchCondition">The search condition being evaluated</param>
        /// <param name="comparisonValue">The value to compare against</param>
        /// <returns>Comparison result (-1, 0, or 1)</returns>
        /// <exception cref="InvalidSearchException">Thrown when comparison fails due to type mismatch</exception>
        public static int CompareValues(object columnValue, SearchCondition searchCondition, object comparisonValue)
        {
            try
            {
                if (columnValue == null)
                {
                    return comparisonValue == null ? 0 : -1;
                }

                if (comparisonValue == null)
                {
                    return 1;
                }

                if (searchCondition.IsDateTime)
                {
                    return ((IComparable)columnValue).CompareTo(comparisonValue);
                }

                if (searchCondition.IsNumeric)
                {
                    return ((IComparable)columnValue).CompareTo(comparisonValue);
                }

                if (searchCondition.IsString)
                {
                    return columnValue.ToStringEmptyIfNull().ToLower().CompareTo(comparisonValue);
                }

                return columnValue.ToStringEmptyIfNull().CompareTo(searchCondition.StringValue);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidSearchException("The search criteria does not match the corresponding column type.", ex);
            }
        }

        /// <summary>
        /// Evaluates whether a column value matches a search condition
        /// </summary>
        /// <param name="columnValue">Value from the column being filtered</param>
        /// <param name="searchCondition">Search condition to evaluate</param>
        /// <returns>True if the value matches the condition, false otherwise</returns>
        public static bool EvaluateCondition(object columnValue, SearchCondition searchCondition)
        {
            string columnString = columnValue.ToStringEmptyIfNull().ToLower();

            switch (searchCondition.SearchType)
            {
                case SearchType.Contains:
                    return searchCondition.StringValue != null && columnString.Contains(searchCondition.StringValue);

                case SearchType.DoesNotContain:
                    return searchCondition.StringValue != null && !columnString.Contains(searchCondition.StringValue);

                case SearchType.IsEmpty:
                    return string.IsNullOrWhiteSpace(columnString);

                case SearchType.IsNotEmpty:
                    return !string.IsNullOrWhiteSpace(columnString);

                case SearchType.StartsWith:
                    return searchCondition.StringValue != null && columnString.StartsWith(searchCondition.StringValue, StringComparison.OrdinalIgnoreCase);

                case SearchType.EndsWith:
                    return searchCondition.StringValue != null && columnString.EndsWith(searchCondition.StringValue, StringComparison.OrdinalIgnoreCase);

                case SearchType.Equals:
                    return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) == 0;

                case SearchType.NotEquals:
                    return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) != 0;

                case SearchType.LessThan:
                    return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) < 0;

                case SearchType.LessThanOrEqualTo:
                    return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) <= 0;

                case SearchType.GreaterThan:
                    return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) > 0;

                case SearchType.GreaterThanOrEqualTo:
                    return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) >= 0;

                case SearchType.Between:
                    return searchCondition.PrimaryValue != null && searchCondition.SecondaryValue != null &&
                           CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) >= 0 &&
                           CompareValues(columnValue, searchCondition, searchCondition.SecondaryValue) <= 0;

                case SearchType.NotBetween:
                    return searchCondition.PrimaryValue != null && searchCondition.SecondaryValue != null &&
                           (CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) < 0 ||
                           CompareValues(columnValue, searchCondition, searchCondition.SecondaryValue) > 0);

                case SearchType.IsLike:
                    return searchCondition.StringValue != null &&
                           EvaluateLikePattern(columnString, searchCondition.StringValue);

                case SearchType.IsNotLike:
                    return searchCondition.StringValue != null &&
                           !EvaluateLikePattern(columnString, searchCondition.StringValue);

                case SearchType.TopN:
                case SearchType.BottomN:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    // These require collection context and are handled at SearchTemplateController level
                    return false;

                case SearchType.DateInterval:
                    if (searchCondition.DateIntervalValue.HasValue)
                    {
                        return EvaluateDateInterval(columnValue, searchCondition.DateIntervalValue.Value);
                    }
                    return false;

                // New filter types
                case SearchType.BetweenDates:
                    if (columnValue is DateTime dateValue &&
                        searchCondition.PrimaryValue is DateTime fromDate &&
                        searchCondition.SecondaryValue is DateTime toDate)
                    {
                        return dateValue.Date >= fromDate.Date && dateValue.Date <= toDate.Date;
                    }
                    return false;

                case SearchType.Yesterday:
                    return columnValue is DateTime dt && dt.Date == DateTime.Today.AddDays(-1);

                case SearchType.Today:
                    return columnValue is DateTime dt2 && dt2.Date == DateTime.Today;

                case SearchType.IsNull:
                    return columnValue == null;

                case SearchType.IsNotNull:
                    return columnValue != null;

                case SearchType.IsAnyOf:
                    if (searchCondition.RawPrimaryValue is IEnumerable<object> values)
                    {
                        return values.Contains(columnValue);
                    }
                    return false;

                case SearchType.IsNoneOf:
                    if (searchCondition.RawPrimaryValue is IEnumerable<object> excludeValues)
                    {
                        return !excludeValues.Contains(columnValue);
                    }
                    return false;

                case SearchType.IsOnAnyOfDates:
                    if (columnValue is DateTime dateToCheck && searchCondition.RawPrimaryValue is IEnumerable<DateTime> dates)
                    {
                        return dates.Any(d => d.Date == dateToCheck.Date);
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Evaluates a wildcard pattern using SQL LIKE-style syntax (% for any characters, _ for single character)
        /// </summary>
        private static bool EvaluateLikePattern(string input, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return string.IsNullOrEmpty(input);

            // Convert SQL LIKE pattern to regex
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("%", ".*")
                .Replace("_", ".")
                + "$";

            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Evaluates a date interval condition based on the DateInterval enum
        /// </summary>
        private static bool EvaluateDateInterval(object columnValue, DateInterval dateInterval)
        {
            if (columnValue == null)
                return false;

            DateTime columnDate;
            if (columnValue is DateTime dt)
                columnDate = dt;
            else if (DateTime.TryParse(columnValue.ToString(), out DateTime parsedDate))
                columnDate = parsedDate;
            else
                return false;

            DateTime now = DateTime.Now;
            DateTime today = now.Date;

            // Start of current week (assuming Sunday is first day of week)
            int dayOfWeek = (int)now.DayOfWeek;
            DateTime startOfWeek = today.AddDays(-dayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);

            // Start/end of current month
            DateTime startOfMonth = new DateTime(now.Year, now.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Start/end of current year
            DateTime startOfYear = new DateTime(now.Year, 1, 1);
            DateTime endOfYear = new DateTime(now.Year, 12, 31);

            switch (dateInterval)
            {
                case DateInterval.PriorThisYear:
                    return columnDate < startOfYear;

                case DateInterval.EarlierThisYear:
                    return columnDate >= startOfYear && columnDate < today;

                case DateInterval.LaterThisYear:
                    return columnDate > today && columnDate <= endOfYear;

                case DateInterval.BeyondThisYear:
                    return columnDate > endOfYear;

                case DateInterval.EarlierThisMonth:
                    return columnDate >= startOfMonth && columnDate < today;

                case DateInterval.LaterThisMonth:
                    return columnDate > today && columnDate <= endOfMonth;

                case DateInterval.EarlierThisWeek:
                    return columnDate >= startOfWeek && columnDate < today;

                case DateInterval.LaterThisWeek:
                    return columnDate > today && columnDate <= endOfWeek;

                case DateInterval.LastWeek:
                    DateTime startOfLastWeek = startOfWeek.AddDays(-7);
                    DateTime endOfLastWeek = startOfWeek.AddDays(-1);
                    return columnDate >= startOfLastWeek && columnDate <= endOfLastWeek;

                case DateInterval.NextWeek:
                    DateTime startOfNextWeek = endOfWeek.AddDays(1);
                    DateTime endOfNextWeek = startOfNextWeek.AddDays(6);
                    return columnDate >= startOfNextWeek && columnDate <= endOfNextWeek;

                case DateInterval.Yesterday:
                    DateTime yesterday = today.AddDays(-1);
                    return columnDate.Date == yesterday;

                case DateInterval.Today:
                    return columnDate.Date == today;

                case DateInterval.Tomorrow:
                    DateTime tomorrow = today.AddDays(1);
                    return columnDate.Date == tomorrow;

                default:
                    return false;
            }
        }
    }
}