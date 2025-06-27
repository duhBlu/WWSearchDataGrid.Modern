using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WWSearchDataGrid.Modern.Core.Strategies;

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
                    var columnDate = ConvertToDateTime(columnValue);
                    var comparisonDate = ConvertToDateTime(comparisonValue);

                    if (columnDate.HasValue && comparisonDate.HasValue)
                        return columnDate.Value.CompareTo(comparisonDate.Value);
                }
                else if (searchCondition.IsNumeric)
                {
                    var columnDecimal = ConvertToDecimal(columnValue);
                    var comparisonDecimal = ConvertToDecimal(comparisonValue);

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
                throw new InvalidSearchException($"Unable to compare values: {ex.Message}", ex);
            }
        }

        private static DateTime? ConvertToDateTime(object value)
        {
            if (value is DateTime dt)
                return dt;
            if (DateTime.TryParse(value.ToString(), out DateTime parsed))
                return parsed;
            return null;
        }

        private static decimal? ConvertToDecimal(object value)
        {
            if (value is decimal dec)
                return dec;
            if (value is int i)
                return (decimal)i;
            if (value is double d)
                return (decimal)d;
            if (value is float f)
                return (decimal)f;
            if (decimal.TryParse(value.ToString(), out decimal parsed))
                return parsed;
            return null;
        }

        /// <summary>
        /// Evaluates whether a column value matches a search condition using Strategy pattern
        /// </summary>
        /// <param name="columnValue">Value from the column being filtered</param>
        /// <param name="searchCondition">Search condition to evaluate</param>
        /// <returns>True if the value matches the condition, false otherwise</returns>
        public static bool EvaluateCondition(object columnValue, SearchCondition searchCondition)
        {
            try
            {
                // Get the appropriate evaluator for the search type
                var evaluator = SearchEvaluatorFactory.Instance.GetEvaluator(searchCondition.SearchType);
                
                if (evaluator != null)
                {
                    return evaluator.Evaluate(columnValue, searchCondition);
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
    }
}