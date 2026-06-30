using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace WWControls.Core
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
                    var groupBody = BuildGroupExpression(group, targetColumnType, ref hasCollectionContextFilters);

                    // Skip empty groups
                    if (groupBody == null)
                        continue;

                    // Combine with previous group expressions
                    if (groupExpression == null)
                    {
                        groupExpression = groupBody;
                    }
                    else
                    {
                        // Ensure OperatorFunction is not null, default to AndAlso if it is
                        var operatorFunc = group.OperatorFunction ?? Expression.AndAlso;
                        groupExpression = Compose(groupExpression, groupBody, operatorFunc);
                    }
                }

                // Compile the expression for non-collection-context filters
                if (groupExpression != null)
                {
                    result.FilterExpression = groupExpression.Compile();
                }
                else if (hasCollectionContextFilters)
                {
                    // Collection-context filters (TopN, AboveAverage, etc.) are evaluated separately
                    // via EvaluateWithCollectionContext at the data grid level.
                    // No standard expression to compile here.
                    result.FilterExpression = null;
                }
                else
                {
                    result.FilterExpression = null;
                }

                // Update the custom expression flag
                var hasMultipleGroups = searchGroups.Count > 1;
                var hasNestedGroups = searchGroups.Any(g => HasAnyChildGroups(g));
                var hasCustomFilterTemplates = searchGroups.Any(g => HasAnyCustomFilterTemplate(g));

                result.HasCustomExpression = searchGroups.Count > 0 && (hasMultipleGroups || hasNestedGroups || hasCustomFilterTemplates);

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
        /// Builds the expression body for a single <see cref="SearchTemplateGroup"/>, including
        /// its <c>SearchTemplates</c> combined via per-template operators and any nested
        /// <c>ChildGroups</c> recursed and combined via each child's operator.
        /// </summary>
        private Expression<Func<object, bool>> BuildGroupExpression(
            SearchTemplateGroup group,
            Type targetColumnType,
            ref bool hasCollectionContextFilters)
        {
            Expression<Func<object, bool>> body = null;

            foreach (var template in group.SearchTemplates)
            {
                template.HasChanges = false;

                if (IsCollectionContextFilter(template.SearchType))
                {
                    hasCollectionContextFilters = true;
                    continue;
                }

                Expression<Func<object, bool>> currentExpression;

                try
                {
                    currentExpression = template.BuildExpression(targetColumnType);
                }
                catch (Exception)
                {
                    var searchCondition = new SearchCondition(
                        targetColumnType,
                        template.SearchType,
                        template.SelectedValue,
                        template.SelectedSecondaryValue);

                    currentExpression = obj => SearchEngine.EvaluateCondition(obj, searchCondition);
                }

                body = body == null
                    ? currentExpression
                    : Compose(body, currentExpression, template.OperatorFunction);
            }

            foreach (var child in group.ChildGroups)
            {
                var childBody = BuildGroupExpression(child, targetColumnType, ref hasCollectionContextFilters);
                if (childBody == null) continue;

                var combiner = child.OperatorFunction ?? Expression.AndAlso;
                body = body == null
                    ? childBody
                    : Compose(body, childBody, combiner);
            }

            // Negated groups (NotAnd / NotOr) wrap their combined body in Expression.Not so the
            // group's predicate is the inverse of the inner combiner's result.
            if (body != null && group.IsNegated)
            {
                body = Expression.Lambda<Func<object, bool>>(Expression.Not(body.Body), body.Parameters);
            }

            return body;
        }

        private static bool HasAnyChildGroups(SearchTemplateGroup group)
        {
            if (group.ChildGroups.Count > 0) return true;
            foreach (var child in group.ChildGroups)
            {
                if (HasAnyChildGroups(child)) return true;
            }
            return false;
        }

        private static bool HasAnyCustomFilterTemplate(SearchTemplateGroup group)
        {
            if (group.SearchTemplates.Any(t => t.HasCustomFilter && t.IsValidFilter)) return true;
            foreach (var child in group.ChildGroups)
            {
                if (HasAnyCustomFilterTemplate(child)) return true;
            }
            return false;
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
