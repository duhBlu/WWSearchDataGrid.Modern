using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Evaluator for LessThan search type
    /// </summary>
    internal class LessThanEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.LessThan;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) < 0;
        }
    }

    /// <summary>
    /// Evaluator for LessThanOrEqualTo search type
    /// </summary>
    internal class LessThanOrEqualToEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.LessThanOrEqualTo;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) <= 0;
        }
    }

    /// <summary>
    /// Evaluator for GreaterThan search type
    /// </summary>
    internal class GreaterThanEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.GreaterThan;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) > 0;
        }
    }

    /// <summary>
    /// Evaluator for GreaterThanOrEqualTo search type
    /// </summary>
    internal class GreaterThanOrEqualToEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.GreaterThanOrEqualTo;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) >= 0;
        }
    }

    /// <summary>
    /// Evaluator for Between search type
    /// </summary>
    internal class BetweenEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Between;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return searchCondition.PrimaryValue != null && searchCondition.SecondaryValue != null &&
                   CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) >= 0 &&
                   CompareValues(columnValue, searchCondition, searchCondition.SecondaryValue) <= 0;
        }
    }

    /// <summary>
    /// Evaluator for NotBetween search type
    /// </summary>
    internal class NotBetweenEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.NotBetween;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            return searchCondition.PrimaryValue != null && searchCondition.SecondaryValue != null &&
                   (CompareValues(columnValue, searchCondition, searchCondition.PrimaryValue) < 0 ||
                   CompareValues(columnValue, searchCondition, searchCondition.SecondaryValue) > 0);
        }
    }
}