using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Service for building filter expressions from search templates
    /// </summary>
    internal class FilterExpressionBuilder
    {
        /// <summary>
        /// Builds a compiled filter expression from search groups
        /// </summary>
        public FilterExpressionResult BuildFilterExpression(
            ObservableCollection<SearchTemplateGroup> searchGroups,
            Type targetColumnType,
            bool forceTargetTypeAsString = false)
        {
            var result = new FilterExpressionResult();

            try
            {
                Expression<Func<object, bool>> groupExpression = null;

                if (forceTargetTypeAsString)
                {
                    targetColumnType = typeof(string);
                }

                // Track if we have collection-context filters that need special handling
                bool hasCollectionContextFilters = false;

                foreach (var group in searchGroups)
                {
                    Expression<Func<object, bool>> templateExpression = null;

                    foreach (var template in group.SearchTemplates)
                    {
                        template.HasChanges = false;

                        // Check if this is a collection-context filter
                        if (IsCollectionContextFilter(template.SearchType))
                        {
                            hasCollectionContextFilters = true;
                            continue; // Skip for now, will be handled separately
                        }

                        Expression<Func<object, bool>> currentExpression;

                        try
                        {
                            currentExpression = template.BuildExpression(targetColumnType);
                        }
                        catch (Exception ex)
                        {
                            // Fallback to basic expression
                            var searchCondition = new SearchCondition(
                                targetColumnType,
                                template.SearchType,
                                template.SelectedValue,
                                template.SelectedSecondaryValue);

                            currentExpression = obj => SearchEngine.EvaluateCondition(obj, searchCondition);
                        }

                        // Combine with previous expressions in this group
                        templateExpression = templateExpression == null
                            ? currentExpression
                            : Compose(templateExpression, currentExpression, template.OperatorFunction);
                    }

                    // Skip empty groups
                    if (templateExpression == null)
                        continue;

                    // Combine with previous group expressions
                    if (groupExpression == null)
                    {
                        groupExpression = templateExpression;
                    }
                    else
                    {
                        // Ensure OperatorFunction is not null, default to AndAlso if it is
                        var operatorFunc = group.OperatorFunction ?? Expression.AndAlso;
                        groupExpression = Compose(groupExpression, templateExpression, operatorFunc);
                    }
                }

                // Compile the expression for non-collection-context filters
                if (groupExpression != null)
                {
                    result.FilterExpression = groupExpression.Compile();
                }
                else if (hasCollectionContextFilters)
                {
                    // For collection-context filters, we'll need to handle them differently
                    // This would typically be done at the data grid level or in the UI
                    result.FilterExpression = obj => true; // Placeholder, actual filtering done elsewhere
                }
                else
                {
                    result.FilterExpression = null;
                }

                // Update the custom expression flag
                var hasMultipleGroups = searchGroups.Count > 1;
                var hasCustomFilterTemplates = searchGroups.Any(g => g.SearchTemplates.Any(t => t.HasCustomFilter && t.IsValidFilter));

                if (searchGroups.Count > 0)
                {
                    for (int i = 0; i < searchGroups.Count; i++)
                    {
                        var group = searchGroups[i];
                        for (int j = 0; j < group.SearchTemplates.Count; j++)
                        {
                            var template = group.SearchTemplates[j];
                        }
                    }
                }

                result.HasCustomExpression = searchGroups.Count > 0 && (hasMultipleGroups || hasCustomFilterTemplates);

                result.HasCollectionContextFilters = hasCollectionContextFilters;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error building filter expression: {ex.Message}";
                Debug.WriteLine(result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// Determines the target column type from available values and metadata
        /// </summary>
        public Type DetermineTargetColumnType(ColumnDataType columnDataType, HashSet<object> columnValues)
        {
            // First, try to use the explicitly set ColumnDataType
            switch (columnDataType)
            {
                case ColumnDataType.DateTime:
                    return typeof(DateTime);
                case ColumnDataType.Number:
                    return DetermineNumericType(columnValues);
                case ColumnDataType.Boolean:
                    return typeof(bool);
                case ColumnDataType.Enum:
                    return DetermineEnumType(columnValues);
                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// Checks if the search type is a collection-context filter
        /// </summary>
        private bool IsCollectionContextFilter(SearchType searchType)
        {
            return searchType == SearchType.TopN ||
                   searchType == SearchType.BottomN ||
                   searchType == SearchType.AboveAverage ||
                   searchType == SearchType.BelowAverage ||
                   searchType == SearchType.Unique ||
                   searchType == SearchType.Duplicate;
        }

        /// <summary>
        /// Determines the specific numeric type from column values
        /// </summary>
        private Type DetermineNumericType(HashSet<object> columnValues)
        {
            if (columnValues?.Any() == true)
            {
                var firstNumericValue = columnValues.FirstOrDefault(v => v != null && ReflectionHelper.IsNumericValue(v));
                if (firstNumericValue != null)
                {
                    return firstNumericValue.GetType();
                }
            }
            return typeof(decimal); // Default to decimal for numeric operations
        }

        /// <summary>
        /// Determines the enum type from column values
        /// </summary>
        private Type DetermineEnumType(HashSet<object> columnValues)
        {
            if (columnValues?.Any() == true)
            {
                var firstEnumValue = columnValues.FirstOrDefault(v => v != null && v.GetType().IsEnum);
                if (firstEnumValue != null)
                {
                    return firstEnumValue.GetType();
                }
            }
            return typeof(string); // Fallback to string
        }

        #region Expression Composition

        /// <summary>
        /// Composes two expressions using the specified combining function
        /// </summary>
        /// <typeparam name="T">Type of the parameter</typeparam>
        /// <param name="first">First expression</param>
        /// <param name="second">Second expression</param>
        /// <param name="merge">Function to merge the expression bodies</param>
        /// <returns>Combined expression</returns>
        private static Expression<Func<T, bool>> Compose<T>(
            Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second,
            Func<Expression, Expression, Expression> merge)
        {
            var map = first.Parameters
                .Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
            return Expression.Lambda<Func<T, bool>>(merge(first.Body, secondBody), first.Parameters);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Result of filter expression building operation
        /// </summary>
        internal class FilterExpressionResult
        {
            /// <summary>
            /// The compiled filter expression function
            /// </summary>
            public Func<object, bool> FilterExpression { get; set; }

            /// <summary>
            /// Whether the result has custom expression logic
            /// </summary>
            public bool HasCustomExpression { get; set; }

            /// <summary>
            /// Whether the expression contains collection-context filters that need special handling
            /// </summary>
            public bool HasCollectionContextFilters { get; set; }

            /// <summary>
            /// Error message if expression building failed
            /// </summary>
            public string ErrorMessage { get; set; }

            /// <summary>
            /// Whether the expression building was successful
            /// </summary>
            public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
        }

        /// <summary>
        /// Expression visitor for rebinding parameters in lambda expressions
        /// Used for expression composition operations
        /// </summary>
        private sealed class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

            public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(
                Dictionary<ParameterExpression, ParameterExpression> map,
                Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                if (_map.TryGetValue(p, out ParameterExpression replacement))
                {
                    return replacement;
                }
                return base.VisitParameter(p);
            }
        }

        #endregion
    }
}
