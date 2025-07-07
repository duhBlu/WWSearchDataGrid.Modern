using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.Core.Common.Utilities;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Service for applying different types of filters to search templates
    /// </summary>
    public class FilterApplicationService : IFilterApplicationService
    {
        #region Filter Context Detection

        /// <summary>
        /// Determines if the SearchTemplateController has meaningful custom filter rules
        /// </summary>
        private bool HasCustomFilterRules(SearchTemplateController searchTemplateController)
        {
            if (searchTemplateController == null)
                return false;

            // Check if we have a custom expression already set
            if (searchTemplateController.HasCustomExpression)
            {
                return true;
            }

            // Check if we have any groups with meaningful templates
            if (searchTemplateController.SearchGroups?.Any() == true)
            {
                foreach (var group in searchTemplateController.SearchGroups)
                {
                    if (group.SearchTemplates?.Any() == true)
                    {
                        foreach (var template in group.SearchTemplates)
                        {
                            // Check if template has configured search criteria
                            if (HasMeaningfulSearchCriteria(template))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a search template has meaningful search criteria configured
        /// </summary>
        private bool HasMeaningfulSearchCriteria(SearchTemplate template)
        {
            if (template == null)
                return false;

            // Check if template has a non-default search type with values
            switch (template.SearchType)
            {
                case SearchType.Contains:
                case SearchType.DoesNotContain:
                case SearchType.StartsWith:
                case SearchType.EndsWith:
                case SearchType.Equals:
                case SearchType.NotEquals:
                case SearchType.LessThan:
                case SearchType.LessThanOrEqualTo:
                case SearchType.GreaterThan:
                case SearchType.GreaterThanOrEqualTo:
                case SearchType.IsLike:
                case SearchType.IsNotLike:
                    return !string.IsNullOrEmpty(template.SelectedValue?.ToString());

                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    return template.SelectedValue != null && template.SelectedSecondaryValue != null;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                case SearchType.IsOnAnyOfDates:
                    return template.SelectedValues?.Any() == true;

                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.IsEmpty:
                case SearchType.IsNotEmpty:
                case SearchType.Yesterday:
                case SearchType.Today:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    return true; // These don't need values

                case SearchType.DateInterval:
                    return template.SelectedValue != null; // Should have interval type

                case SearchType.TopN:
                case SearchType.BottomN:
                    return template.SelectedValue != null && int.TryParse(template.SelectedValue.ToString(), out var n) && n > 0;

                case SearchType.GroupedInclusion:
                case SearchType.GroupedExclusion:
                case SearchType.GroupedCombination:
                    return true; // These are complex grouping operations that are meaningful

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if filter value selections represent actual filtering intent
        /// </summary>
        private bool HasMeaningfulValueSelections(FilterValueViewModel filterValueViewModel)
        {
            if (filterValueViewModel == null)
                return false;

            var allValues = filterValueViewModel.GetAllValues();
            var selectedItems = allValues.Where(item => item.IsSelected).ToList();

            // If no values or all values selected, this is not meaningful filtering
            if (!selectedItems.Any() || selectedItems.Count == allValues.Count)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if rule-based filtering should take precedence over value-based filtering
        /// </summary>
        private bool ShouldPreferRuleBasedFiltering(SearchTemplateController searchTemplateController, FilterValueViewModel filterValueViewModel)
        {
            var hasCustomRules = HasCustomFilterRules(searchTemplateController);
            var hasMeaningfulValues = HasMeaningfulValueSelections(filterValueViewModel);


            // If we have custom rules and no meaningful value selections, prefer rules
            if (hasCustomRules && !hasMeaningfulValues)
            {
                return true;
            }

            // If we have custom rules and meaningful value selections, still prefer rules to preserve custom expressions
            // This prevents operator changes from being lost when "all values selected"
            if (hasCustomRules && hasMeaningfulValues)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Intelligent Filter Application

        /// <summary>
        /// Intelligently applies the most appropriate filter method based on content rather than UI state
        /// </summary>
        public FilterApplicationResult ApplyIntelligentFilter(
            FilterValueViewModel filterValueViewModel,
            SearchTemplateController searchTemplateController,
            ColumnDataType columnDataType,
            int selectedTabIndex = -1)
        {
            try
            {
                if (searchTemplateController == null)
                {
                    return FilterApplicationResult.Failure("Search template controller is null");
                }

                var hasCustomRules = HasCustomFilterRules(searchTemplateController);
                var hasMeaningfulValues = HasMeaningfulValueSelections(filterValueViewModel);
                var isValuesTabSelected = selectedTabIndex == 1;


                // Decision matrix for intelligent filtering
                if (hasCustomRules && !hasMeaningfulValues)
                {
                    // Custom rules exist but no meaningful value selections - use rules
                    return ApplyRuleBasedFilter(searchTemplateController);
                }
                else if (hasCustomRules && hasMeaningfulValues)
                {
                    // Both custom rules and meaningful values exist
                    if (isValuesTabSelected)
                    {
                        // User is on values tab - but still preserve rules if they exist
                        return ApplyRuleBasedFilter(searchTemplateController);
                    }
                    else
                    {
                        // User is on rules tab - use rules
                        return ApplyRuleBasedFilter(searchTemplateController);
                    }
                }
                else if (!hasCustomRules && hasMeaningfulValues)
                {
                    // No custom rules but meaningful value selections - use values
                    return ApplyValueBasedFilter(filterValueViewModel, searchTemplateController, columnDataType);
                }
                else
                {
                    // No custom rules and no meaningful values - clear filter
                    return ClearAllFilters(searchTemplateController);
                }
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying intelligent filter: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Applies value-based filtering from selected filter values with intelligent rule preservation
        /// </summary>
        public FilterApplicationResult ApplyValueBasedFilter(
            FilterValueViewModel filterValueViewModel,
            SearchTemplateController searchTemplateController,
            ColumnDataType columnDataType)
        {
            try
            {
                if (filterValueViewModel == null)
                {
                    return FilterApplicationResult.Failure("Filter value view model is null");
                }

                if (searchTemplateController == null)
                {
                    return FilterApplicationResult.Failure("Search template controller is null");
                }

                // INTELLIGENT DECISION LOGIC: Check if we should prefer rule-based filtering instead
                if (ShouldPreferRuleBasedFiltering(searchTemplateController, filterValueViewModel))
                {
                    return ApplyRuleBasedFilter(searchTemplateController);
                }

                // Check if this is grouped filtering
                if (filterValueViewModel is GroupedTreeViewFilterValueViewModel groupedViewModel && 
                    groupedViewModel.IsGroupedFiltering)
                {
                    return ApplyGroupedValueBasedFilter(groupedViewModel, searchTemplateController, null, null);
                }

                // Standard flat filtering with optimization
                var allValues = filterValueViewModel.GetAllValues();
                var selectedItems = allValues.Where(item => item.IsSelected).ToList();

                // Only clear if no custom rules exist AND all items are selected
                if (selectedItems.Count == allValues.Count && allValues.Count > 0)
                {
                    // Check one more time for custom rules before clearing
                    if (HasCustomFilterRules(searchTemplateController))
                    {
                        return ApplyRuleBasedFilter(searchTemplateController);
                    }
                    else
                    {
                        return ClearAllFilters(searchTemplateController);
                    }
                }
                else if (selectedItems.Any())
                {
                    // Use optimizer to determine best filter strategy
                    var optimizationResult = FilterSelectionOptimizer.OptimizeSelections(
                        allValues, selectedItems, columnDataType);

                    var operatorName = searchTemplateController.SearchGroups.FirstOrDefault()?.OperatorName ?? "And";

                    // Clear and recreate more efficiently
                    searchTemplateController.SearchGroups.Clear();
                    var group = new SearchTemplateGroup() { OperatorName = operatorName };
                    searchTemplateController.SearchGroups.Add(group);

                    var template = new SearchTemplate(columnDataType)
                    {
                        SearchType = optimizationResult.RecommendedSearchType
                    };

                    // Set values based on optimization result and search type
                    if (optimizationResult.RecommendedSearchType == SearchType.Equals && 
                        optimizationResult.FilterValues.Count == 1)
                    {
                        // For single value Equals, set SelectedValue (singular)
                        template.SelectedValue = optimizationResult.FilterValues.First();
                    }
                    else
                    {
                        // For multi-value search types (IsAnyOf, IsNoneOf, NotEquals), use SelectedValues (plural)
                        template.SelectedValues.Clear();
                        foreach (var value in optimizationResult.FilterValues)
                        {
                            template.SelectedValues.Add(new FilterListValue { Value = value });
                        }
                    }

                    group.SearchTemplates.Add(template);
                    searchTemplateController.UpdateFilterExpression();

                    return FilterApplicationResult.Success(FilterApplicationType.ValueBased);
                }
                else
                {
                    // No items selected - check for custom rules before clearing
                    if (HasCustomFilterRules(searchTemplateController))
                    {
                        return ApplyRuleBasedFilter(searchTemplateController);
                    }
                    else
                    {
                        return ClearAllFilters(searchTemplateController);
                    }
                }
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying value-based filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies grouped value-based filtering that respects group-child combinations
        /// </summary>
        public FilterApplicationResult ApplyGroupedValueBasedFilter(
            GroupedTreeViewFilterValueViewModel groupedViewModel,
            SearchTemplateController searchTemplateController,
            string currentColumnPath,
            string groupByColumnPath)
        {
            try
            {
                if (groupedViewModel == null)
                {
                    return FilterApplicationResult.Failure("Grouped view model is null");
                }

                var groupChildCombinations = groupedViewModel.GetSelectedGroupChildCombinations().ToList();

                if (!groupChildCombinations.Any())
                {
                    // No selections - clear filters
                    return ClearAllFilters(searchTemplateController);
                }

                // Get the binding paths for both columns
                groupByColumnPath = groupByColumnPath ?? groupedViewModel.GroupByColumn;

                if (string.IsNullOrEmpty(currentColumnPath) || string.IsNullOrEmpty(groupByColumnPath))
                {
                    // Fallback to regular filtering if we can't determine the paths
                    var selectedValues = groupedViewModel.GetSelectedValues().ToList();
                    if (selectedValues.Any())
                    {
                        // Use the controller's column data type since the view model doesn't have one
                        return ApplyFallbackMultiValueFilter(searchTemplateController, selectedValues, 
                            searchTemplateController.ColumnDataType);
                    }
                    return ClearAllFilters(searchTemplateController);
                }

                // Create a custom filter expression that handles grouped logic
                return CreateGroupedFilterExpression(searchTemplateController, groupChildCombinations, 
                    currentColumnPath, groupByColumnPath, groupedViewModel);
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying grouped filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies rule-based filtering from search templates
        /// </summary>
        public FilterApplicationResult ApplyRuleBasedFilter(SearchTemplateController searchTemplateController)
        {
            try
            {
                if (searchTemplateController == null)
                {
                    return FilterApplicationResult.Failure("Search template controller is null");
                }

                for (int i = 0; i < searchTemplateController.SearchGroups.Count; i++)
                {
                    var group = searchTemplateController.SearchGroups[i];
                }

                searchTemplateController.UpdateFilterExpression();
                
                return FilterApplicationResult.Success(FilterApplicationType.RuleBased, 
                    searchTemplateController.HasCustomExpression);
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying rule-based filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all filters and resets to default state
        /// </summary>
        public FilterApplicationResult ClearAllFilters(SearchTemplateController searchTemplateController)
        {
            try
            {
                if (searchTemplateController == null)
                {
                    return FilterApplicationResult.Failure("Search template controller is null");
                }

                searchTemplateController.SearchGroups.Clear();
                searchTemplateController.UpdateFilterExpression();
                
                return FilterApplicationResult.Success(FilterApplicationType.Cleared, false);
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error clearing filters: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a fallback multi-value filter when grouped filtering paths are not available
        /// </summary>
        private FilterApplicationResult ApplyFallbackMultiValueFilter(
            SearchTemplateController searchTemplateController,
            List<object> selectedValues,
            ColumnDataType columnDataType)
        {
            searchTemplateController.SearchGroups.Clear();
            var group = new SearchTemplateGroup();
            searchTemplateController.SearchGroups.Add(group);

            var template = new SearchTemplate(columnDataType)
            {
                SearchType = SearchType.IsAnyOf
            };

            foreach (var value in selectedValues)
            {
                template.SelectedValues.Add(new FilterListValue { Value = value });
            }

            group.SearchTemplates.Add(template);
            searchTemplateController.UpdateFilterExpression();

            return FilterApplicationResult.Success(FilterApplicationType.ValueBased);
        }

        /// <summary>
        /// Creates a custom filter expression for grouped filtering
        /// </summary>
        private FilterApplicationResult CreateGroupedFilterExpression(
            SearchTemplateController searchTemplateController,
            List<(object GroupKey, object ChildValue)> combinations,
            string currentColumnPath,
            string groupByColumnPath,
            GroupedTreeViewFilterValueViewModel groupedViewModel)
        {
            // Clear existing groups
            searchTemplateController.SearchGroups.Clear();

            // Create a custom filter function that checks both the group column and current column
            Func<object, bool> groupedFilter = columnValue =>
            {
                try
                {
                    // For now, return true if the column value matches any selected child value
                    // This is a limitation - we'll need to enhance the SearchTemplateController
                    return combinations.Any(c => Equals(c.ChildValue, columnValue));
                }
                catch
                {
                    return false;
                }
            };

            // Set grouped filtering information on the SearchTemplateController
            searchTemplateController.GroupedFilterCombinations = combinations;
            searchTemplateController.GroupByColumnPath = groupByColumnPath;
            searchTemplateController.CurrentColumnPath = currentColumnPath;

            // Try to get the grouped view model to extract all group data
            var allGroupData = new Dictionary<object, List<object>>();
            foreach (var group in groupedViewModel.AllGroups)
            {
                var groupValues = group.Children.Select(c => c.Value).ToList();
                allGroupData[group.GroupKey] = groupValues;
            }
            searchTemplateController.AllGroupData = allGroupData;

            // Set the filter expression on the controller
            searchTemplateController.FilterExpression = groupedFilter;
            searchTemplateController.HasCustomExpression = true;

            var result = FilterApplicationResult.Success(FilterApplicationType.GroupedValueBased);
            result.RequiresCollectionContext = true;
            result.Metadata["GroupByColumn"] = groupByColumnPath;
            result.Metadata["CurrentColumn"] = currentColumnPath;
            result.Metadata["CombinationCount"] = combinations.Count;

            return result;
        }
    }
}