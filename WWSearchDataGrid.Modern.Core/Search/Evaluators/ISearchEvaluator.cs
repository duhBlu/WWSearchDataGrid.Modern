using System;

namespace WWSearchDataGrid.Modern.Core.Strategies
{
    /// <summary>
    /// Interface for search condition evaluators
    /// </summary>
    public interface ISearchEvaluator
    {
        /// <summary>
        /// The search type this evaluator handles
        /// </summary>
        SearchType SearchType { get; }

        /// <summary>
        /// Evaluates whether a column value matches the search condition
        /// </summary>
        /// <param name="columnValue">Value from the column being filtered</param>
        /// <param name="searchCondition">Search condition to evaluate</param>
        /// <returns>True if the value matches the condition, false otherwise</returns>
        bool Evaluate(object columnValue, SearchCondition searchCondition);

        /// <summary>
        /// Gets the priority of this evaluator (higher values processed first)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this evaluator can handle the given search type
        /// </summary>
        /// <param name="searchType">Search type to check</param>
        /// <returns>True if this evaluator can handle the search type</returns>
        bool CanHandle(SearchType searchType);
    }

    /// <summary>
    /// Base class for search evaluators providing common functionality
    /// </summary>
    public abstract class SearchEvaluatorBase : ISearchEvaluator
    {
        /// <summary>
        /// The search type this evaluator handles
        /// </summary>
        public abstract SearchType SearchType { get; }

        /// <summary>
        /// Default priority for evaluators
        /// </summary>
        public virtual int Priority => 100;

        /// <summary>
        /// Evaluates whether a column value matches the search condition
        /// </summary>
        public abstract bool Evaluate(object columnValue, SearchCondition searchCondition);

        /// <summary>
        /// Default implementation checks if search type matches
        /// </summary>
        public virtual bool CanHandle(SearchType searchType)
        {
            return SearchType == searchType;
        }

        /// <summary>
        /// Helper method to safely convert column value to string
        /// </summary>
        protected string GetColumnString(object columnValue)
        {
            return (columnValue?.ToString() ?? string.Empty).ToLower();
        }

        /// <summary>
        /// Helper method to compare values using SearchEngine's comparison logic
        /// </summary>
        protected int CompareValues(object columnValue, SearchCondition searchCondition, object comparisonValue)
        {
            return SearchEngine.CompareValues(columnValue, searchCondition, comparisonValue);
        }
    }
}