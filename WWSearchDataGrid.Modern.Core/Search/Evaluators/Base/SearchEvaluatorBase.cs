namespace WWSearchDataGrid.Modern.Core
{
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
        public virtual bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
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