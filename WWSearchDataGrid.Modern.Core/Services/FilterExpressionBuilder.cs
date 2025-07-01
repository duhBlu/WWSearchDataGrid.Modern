using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core.Services
{
    /// <summary>
    /// Service for building filter expressions from search templates
    /// </summary>
    public class FilterExpressionBuilder : IFilterExpressionBuilder
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
                            // All templates now implement BuildExpression
                            currentExpression = template.BuildExpression(targetColumnType);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error building expression for template: {ex.Message}");

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
                            : templateExpression.Compose(currentExpression, template.OperatorFunction);
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
                        // Ensure OperatorFunction is not null, default to And if it is
                        var operatorFunc = group.OperatorFunction ?? Expression.And;
                        groupExpression = groupExpression.Compose(templateExpression, operatorFunc);
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
                var hasCustomFilterTemplates = searchGroups.Any(g => g.SearchTemplates.Any(t => t.HasCustomFilter));
                
                System.Diagnostics.Debug.WriteLine($"FilterExpressionBuilder: searchGroups.Count = {searchGroups.Count}");
                System.Diagnostics.Debug.WriteLine($"FilterExpressionBuilder: hasMultipleGroups = {hasMultipleGroups}");
                System.Diagnostics.Debug.WriteLine($"FilterExpressionBuilder: hasCustomFilterTemplates = {hasCustomFilterTemplates}");
                
                if (searchGroups.Count > 0)
                {
                    for (int i = 0; i < searchGroups.Count; i++)
                    {
                        var group = searchGroups[i];
                        System.Diagnostics.Debug.WriteLine($"FilterExpressionBuilder: Group {i} has {group.SearchTemplates.Count} templates");
                        for (int j = 0; j < group.SearchTemplates.Count; j++)
                        {
                            var template = group.SearchTemplates[j];
                            System.Diagnostics.Debug.WriteLine($"FilterExpressionBuilder: Group {i} Template {j} - HasCustomFilter = {template.HasCustomFilter}");
                        }
                    }
                }
                
                result.HasCustomExpression = searchGroups.Count > 0 && (hasMultipleGroups || hasCustomFilterTemplates);
                System.Diagnostics.Debug.WriteLine($"FilterExpressionBuilder: Final HasCustomExpression = {result.HasCustomExpression}");

                result.HasCollectionContextFilters = hasCollectionContextFilters;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error building filter expression: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(result.ErrorMessage);
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
                    // Try to determine the specific numeric type from values
                    return DetermineNumericType(columnValues);
                case ColumnDataType.Boolean:
                    return typeof(bool);
                case ColumnDataType.Enum:
                    // Try to determine the enum type from values
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
    }
}