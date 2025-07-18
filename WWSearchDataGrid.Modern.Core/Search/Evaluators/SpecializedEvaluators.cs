using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.Core.Strategies
{
    /// <summary>
    /// Evaluator for IsEmpty search type
    /// </summary>
    public class IsEmptyEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsEmpty;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var metadata = ValueMetadata.Create(columnValue);
            return metadata.Category == ValueCategory.Empty || metadata.Category == ValueCategory.Whitespace;
        }
    }

    /// <summary>
    /// Evaluator for IsNotEmpty search type
    /// </summary>
    public class IsNotEmptyEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsNotEmpty;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var metadata = ValueMetadata.Create(columnValue);
            return metadata.Category != ValueCategory.Empty && metadata.Category != ValueCategory.Whitespace && metadata.Category != ValueCategory.Null;
        }
    }

    /// <summary>
    /// Evaluator for IsNull search type
    /// </summary>
    public class IsNullEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsNull;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var metadata = ValueMetadata.Create(columnValue);
            return metadata.Category == ValueCategory.Null;
        }
    }

    /// <summary>
    /// Evaluator for IsNotNull search type
    /// </summary>
    public class IsNotNullEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsNotNull;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var metadata = ValueMetadata.Create(columnValue);
            return metadata.Category != ValueCategory.Null;
        }
    }


    /// <summary>
    /// Evaluator for IsLike search type (SQL LIKE patterns)
    /// </summary>
    public class IsLikeEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsLike;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var columnString = GetColumnString(columnValue);
            return searchCondition.StringValue != null && 
                   EvaluateLikePattern(columnString, searchCondition.StringValue);
        }

        private bool EvaluateLikePattern(string input, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return string.IsNullOrEmpty(input);

            // Convert SQL LIKE pattern to regex
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("%", ".*")
                .Replace("_", ".")
                + "$";

            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Evaluator for IsNotLike search type
    /// </summary>
    public class IsNotLikeEvaluator : IsLikeEvaluator
    {
        public override SearchType SearchType => SearchType.IsNotLike;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return !base.Evaluate(columnValue, searchCondition);
        }
    }

    /// <summary>
    /// Evaluator for BetweenDates search type
    /// </summary>
    public class BetweenDatesEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.BetweenDates;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            if (columnValue is DateTime dateValue &&
                searchCondition.PrimaryValue is DateTime fromDate &&
                searchCondition.SecondaryValue is DateTime toDate)
            {
                return dateValue.Date >= fromDate.Date && dateValue.Date <= toDate.Date;
            }
            return false;
        }
    }

    /// <summary>
    /// Evaluator for Yesterday search type
    /// </summary>
    public class YesterdayEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Yesterday;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var dateValue = ConvertToDateTime(columnValue);
            return dateValue?.Date == DateTime.Today.AddDays(-1);
        }
    }

    /// <summary>
    /// Evaluator for Today search type
    /// </summary>
    public class TodayEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Today;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var dateValue = ConvertToDateTime(columnValue);
            return dateValue?.Date == DateTime.Today;
        }
    }

    /// <summary>
    /// Evaluator for IsAnyOf search type
    /// </summary>
    public class IsAnyOfEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsAnyOf;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            if (searchCondition.RawPrimaryValue is IEnumerable<object> values)
            {
                return values.Contains(columnValue);
            }
            return false;
        }
    }

    /// <summary>
    /// Evaluator for IsNoneOf search type
    /// </summary>
    public class IsNoneOfEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsNoneOf;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            if (searchCondition.RawPrimaryValue is IEnumerable<object> excludeValues)
            {
                return !excludeValues.Contains(columnValue);
            }
            return false;
        }
    }

    /// <summary>
    /// Evaluator for IsOnAnyOfDates search type
    /// </summary>
    public class IsOnAnyOfDatesEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.IsOnAnyOfDates;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            if (columnValue is DateTime dateToCheck && 
                searchCondition.RawPrimaryValue is IEnumerable<DateTime> dates)
            {
                return dates.Any(d => d.Date == dateToCheck.Date);
            }
            return false;
        }
    }

    /// <summary>
    /// Evaluator for DateInterval search type
    /// </summary>
    public class DateIntervalEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.DateInterval;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            if (searchCondition.DateIntervalValue.HasValue)
            {
                return EvaluateDateInterval(columnValue, searchCondition.DateIntervalValue.Value);
            }
            return false;
        }

        private bool EvaluateDateInterval(object columnValue, DateInterval dateInterval)
        {
            var columnDate = ConvertToDateTime(columnValue);
            if (!columnDate.HasValue) return false;

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
                    return columnDate.Value.Date == yesterday;
                case DateInterval.Today:
                    return columnDate.Value.Date == today;
                case DateInterval.Tomorrow:
                    DateTime tomorrow = today.AddDays(1);
                    return columnDate.Value.Date == tomorrow;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Evaluator for collection-context filters that need special handling
    /// </summary>
    public class CollectionContextEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.TopN; // Default, but handles multiple

        public override int Priority => 50; // Lower priority

        public override bool CanHandle(SearchType searchType)
        {
            return searchType == SearchType.TopN ||
                   searchType == SearchType.BottomN ||
                   searchType == SearchType.AboveAverage ||
                   searchType == SearchType.BelowAverage ||
                   searchType == SearchType.Unique ||
                   searchType == SearchType.Duplicate;
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            // These require collection context and are handled at SearchTemplateController level
            return false;
        }
    }
}