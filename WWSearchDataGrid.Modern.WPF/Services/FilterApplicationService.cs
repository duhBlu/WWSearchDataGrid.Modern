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
        /// <summary>
        /// Applies value-based filtering from selected filter values
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

                // Check if this is grouped filtering
                if (filterValueViewModel is GroupedTreeViewFilterValueViewModel groupedViewModel && 
                    groupedViewModel.IsGroupedFiltering)
                {
                    return ApplyGroupedValueBasedFilter(groupedViewModel, searchTemplateController, null, null);
                }

                // Standard flat filtering with optimization
                var allValues = filterValueViewModel.GetAllValues();
                var selectedItems = allValues.Where(item => item.IsSelected).ToList();

                // Check if all items are selected
                if (selectedItems.Count == allValues.Count && allValues.Count > 0)
                {
                    // All items selected - clear the filter instead of creating one
                    return ClearAllFilters(searchTemplateController);
                }
                else if (selectedItems.Any())
                {
                    // Use optimizer to determine best filter strategy
                    var optimizationResult = FilterSelectionOptimizer.OptimizeSelections(
                        allValues, selectedItems, columnDataType);

                    var operatorName = searchTemplateController.SearchGroups.FirstOrDefault().OperatorName;

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
                    // No items selected - also clear filter
                    return ClearAllFilters(searchTemplateController);
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

                System.Diagnostics.Debug.WriteLine($"ApplyRuleBasedFilter: Starting with {searchTemplateController.SearchGroups.Count} search groups");
                for (int i = 0; i < searchTemplateController.SearchGroups.Count; i++)
                {
                    var group = searchTemplateController.SearchGroups[i];
                    System.Diagnostics.Debug.WriteLine($"ApplyRuleBasedFilter: Group {i} has {group.SearchTemplates.Count} templates, OperatorName = {group.OperatorName}");
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