namespace WWSearchDataGrid.Modern.Core
{
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
        bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext);

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
}