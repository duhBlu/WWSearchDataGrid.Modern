using System;
using System.Collections.Generic;
using System.Linq;
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
        public static int CompareValues(object columnValue, SearchCondition searchCondition, object comparisonValue)
        {
            try
            {
                // Handle null cases first
                if (columnValue == null && comparisonValue == null)
                    return 0;
                if (columnValue == null)
                    return -1;
                if (comparisonValue == null)
                    return 1;

                // Convert both values to the same type for comparison
                if (searchCondition.IsDateTime)
                {
                    var columnDate = TypeTranslatorHelper.ConvertToDateTime(columnValue);
                    var comparisonDate = TypeTranslatorHelper.ConvertToDateTime(comparisonValue);

                    if (columnDate.HasValue && comparisonDate.HasValue)
                        return columnDate.Value.CompareTo(comparisonDate.Value);
                }
                else if (searchCondition.IsNumeric)
                {
                    var columnDecimal = TypeTranslatorHelper.ConvertToDecimal(columnValue);
                    var comparisonDecimal = TypeTranslatorHelper.ConvertToDecimal(comparisonValue);

                    if (columnDecimal.HasValue && comparisonDecimal.HasValue)
                        return columnDecimal.Value.CompareTo(comparisonDecimal.Value);
                }
                else if (searchCondition.IsString)
                {
                    var columnString = columnValue.ToString().ToLower();
                    var comparisonString = comparisonValue.ToString().ToLower();
                    return string.Compare(columnString, comparisonString, StringComparison.OrdinalIgnoreCase);
                }

                // Fallback to string comparison
                return string.Compare(columnValue.ToString(), comparisonValue.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CompareValues: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Evaluates whether a column value matches a search condition using Strategy pattern
        /// </summary>
        /// <param name="columnValue">Value from the column being filtered</param>
        /// <param name="searchCondition">Search condition to evaluate</param>
        /// <returns>True if the value matches the condition, false otherwise</returns>
        public static bool EvaluateCondition(object columnValue, SearchCondition searchCondition)
        {
            return EvaluateCondition(columnValue, searchCondition, null);
        }

        /// <summary>
        /// Evaluates whether a column value matches a search condition using Strategy pattern with collection context
        /// </summary>
        /// <param name="columnValue">Value from the column being filtered</param>
        /// <param name="searchCondition">Search condition to evaluate</param>
        /// <param name="collectionContext">Collection context for statistical and ranking operations</param>
        /// <returns>True if the value matches the condition, false otherwise</returns>
        internal static bool EvaluateCondition(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            try
            {
                // Get the appropriate evaluator for the search type
                var evaluator = SearchEvaluatorFactory.Instance.GetEvaluator(searchCondition.SearchType);
                
                if (evaluator != null)
                {
                    // Use collection context version if available and context is provided
                    if (collectionContext != null && evaluator.RequiresCollectionContext)
                    {
                        return evaluator.Evaluate(columnValue, searchCondition, collectionContext);
                    }
                    else
                    {
                        return evaluator.Evaluate(columnValue, searchCondition);
                    }
                }
                
                // Fallback to original logic for unknown search types
                System.Diagnostics.Debug.WriteLine($"No evaluator found for search type: {searchCondition.SearchType}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating search condition: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a search type requires collection context for proper evaluation
        /// </summary>
        /// <param name="searchType">The search type to check</param>
        /// <returns>True if the search type requires collection context</returns>
        public static bool RequiresCollectionContext(SearchType searchType)
        {
            var evaluator = SearchEvaluatorFactory.Instance.GetEvaluator(searchType);
            return evaluator?.RequiresCollectionContext ?? false;
        }
    }
}