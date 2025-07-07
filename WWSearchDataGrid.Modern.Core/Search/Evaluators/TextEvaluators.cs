using System;

namespace WWSearchDataGrid.Modern.Core.Strategies
{
    /// <summary>
    /// Evaluator for Contains search type
    /// </summary>
    public class ContainsEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Contains;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var columnString = GetColumnString(columnValue);
            return searchCondition.StringValue != null && columnString.Contains(searchCondition.StringValue);
        }
    }

    /// <summary>
    /// Evaluator for DoesNotContain search type
    /// </summary>
    public class DoesNotContainEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.DoesNotContain;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var columnString = GetColumnString(columnValue);
            return searchCondition.StringValue != null && !columnString.Contains(searchCondition.StringValue);
        }
    }

    /// <summary>
    /// Evaluator for StartsWith search type
    /// </summary>
    public class StartsWithEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.StartsWith;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var columnString = GetColumnString(columnValue);
            return searchCondition.StringValue != null && 
                   columnString.StartsWith(searchCondition.StringValue, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Evaluator for EndsWith search type
    /// </summary>
    public class EndsWithEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.EndsWith;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            var columnString = GetColumnString(columnValue);
            return searchCondition.StringValue != null && 
                   columnString.EndsWith(searchCondition.StringValue, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Evaluator for Equals search type
    /// </summary>
    public class EqualsEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Equals;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) == 0;
        }
    }

    /// <summary>
    /// Evaluator for NotEquals search type
    /// </summary>
    public class NotEqualsEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.NotEquals;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) != 0;
        }
    }
}