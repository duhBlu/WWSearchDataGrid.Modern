using System;
using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.Core.Strategies
{
    /// <summary>
    /// Interface providing collection context for statistical and ranking operations
    /// </summary>
    internal interface ICollectionContext
    {
        /// <summary>
        /// Gets the full collection being filtered
        /// </summary>
        IEnumerable<object> Items { get; }

        /// <summary>
        /// Gets the column path for context-sensitive operations
        /// </summary>
        string ColumnPath { get; }

        /// <summary>
        /// Gets the average value for the column (lazy loaded)
        /// </summary>
        double? GetAverage();

        /// <summary>
        /// Gets items sorted by column value in descending order (lazy loaded)
        /// </summary>
        IEnumerable<object> GetSortedDescending();

        /// <summary>
        /// Gets items sorted by column value in ascending order (lazy loaded)
        /// </summary>
        IEnumerable<object> GetSortedAscending();

        /// <summary>
        /// Gets value frequency map for uniqueness operations (lazy loaded)
        /// </summary>
        Dictionary<object, List<object>> GetValueGroups();
    }
    /// <summary>
    /// Interface for search condition evaluators
    /// </summary>
    internal interface ISearchEvaluator
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
        /// Evaluates whether a column value matches the search condition with collection context
        /// </summary>
        /// <param name="columnValue">Value from the column being filtered</param>
        /// <param name="searchCondition">Search condition to evaluate</param>
        /// <param name="collectionContext">Collection context for statistical and ranking operations</param>
        /// <returns>True if the value matches the condition, false otherwise</returns>
        bool Evaluate(object columnValue, SearchCondition searchCondition, ICollectionContext collectionContext);

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

        /// <summary>
        /// Whether this evaluator requires collection context to function properly
        /// </summary>
        bool RequiresCollectionContext { get; }
    }

    /// <summary>
    /// Base class for search evaluators providing common functionality
    /// </summary>
    internal abstract class SearchEvaluatorBase : ISearchEvaluator
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
        /// Whether this evaluator requires collection context (default: false)
        /// </summary>
        public virtual bool RequiresCollectionContext => false;

        /// <summary>
        /// Evaluates whether a column value matches the search condition
        /// </summary>
        public abstract bool Evaluate(object columnValue, SearchCondition searchCondition);

        /// <summary>
        /// Evaluates whether a column value matches the search condition with collection context
        /// Default implementation delegates to the parameterless version
        /// </summary>
        public virtual bool Evaluate(object columnValue, SearchCondition searchCondition, ICollectionContext collectionContext)
        {
            return Evaluate(columnValue, searchCondition);
        }

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