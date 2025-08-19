using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Service for applying different types of filters to search templates
    /// </summary>
    public class FilterApplicationService : IFilterApplicationService
    {
        private readonly FilterRuleOptimizer _ruleOptimizer = new FilterRuleOptimizer();

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
            if (selectedItems.Count == 0 || selectedItems.Count == allValues.Count)
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
            // Check if user is on Rules tab with meaningful rules - if so, use rules
            if (selectedTabIndex == 0 && HasMeaningfulSearchCriteria(searchTemplateController))
            {
                return ApplyRuleBasedFilter(searchTemplateController);
            }
            
            // Otherwise, use value-based filtering (which includes optimization)
            return ApplyValueBasedFilter(filterValueViewModel, searchTemplateController, columnDataType);
        }

        /// <summary>
        /// Checks if the SearchTemplateController has meaningful search criteria
        /// </summary>
        private bool HasMeaningfulSearchCriteria(SearchTemplateController controller)
        {
            if (controller?.SearchGroups == null)
                return false;

            return controller.SearchGroups
                .SelectMany(g => g.SearchTemplates ?? Enumerable.Empty<SearchTemplate>())
                .Any(t => HasMeaningfulSearchCriteria(t));
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

                // Check if this is grouped filtering
                if (filterValueViewModel is GroupedTreeViewFilterValueViewModel groupedViewModel && 
                    groupedViewModel.IsGroupedFiltering)
                {
                    return ApplyGroupedValueBasedFilter(groupedViewModel, searchTemplateController, null, null);
                }

                // Standard flat filtering with intelligent rule preservation
                var allValues = filterValueViewModel.GetAllValues();
                var selectedItems = allValues.Where(item => item.IsSelected).ToList();

                // Clear if all items are selected (no filtering needed)
                if (selectedItems.Count == allValues.Count && allValues.Count > 0)
                {
                    return ClearAllFilters(searchTemplateController);
                }
                else if (selectedItems.Count != 0)
                {
                    // Use intelligent rule preservation instead of full replacement
                    return ApplyValueBasedFilterWithRulePreservation(filterValueViewModel, searchTemplateController, columnDataType);
                }
                else
                {
                    // No items selected - clear all filters
                    return ClearAllFilters(searchTemplateController);
                }
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying value-based filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies optimized value-based filtering using intelligent rule generation
        /// </summary>
        private FilterApplicationResult ApplyOptimizedValueBasedFilter(
            FilterValueViewModel filterValueViewModel,
            SearchTemplateController searchTemplateController,
            ColumnDataType columnDataType)
        {
            try
            {
                var allValues = filterValueViewModel.GetAllValues().Select(v => v.Value);
                var selectedValues = filterValueViewModel.GetSelectedValues();

                // Analyze selection pattern
                var analysis = _ruleOptimizer.AnalyzeSelection(allValues, selectedValues, columnDataType);

                // Generate optimized rules
                var optimizedRules = _ruleOptimizer.GenerateOptimizedRules(analysis, allValues, selectedValues);

                if (!optimizedRules.Any())
                {
                    // Clear all filters if no rules generated
                    return ClearAllFilters(searchTemplateController);
                }

                // Preserve existing operator preference
                var operatorName = searchTemplateController.SearchGroups.FirstOrDefault()?.OperatorName ?? "Or";

                // Clear and rebuild with optimized rules
                searchTemplateController.SearchGroups.Clear();

                // For multiple rules, we need to decide on the group operator
                // Positive rules (Equals, Between, IsAnyOf) use OR
                // Negative rules (NotEquals, NotBetween, IsNoneOf) use AND
                var hasNegativeRules = optimizedRules.Any(r => IsNegativeSearchType(r.SearchType));
                var groupOperator = hasNegativeRules ? "And" : operatorName;

                var group = new SearchTemplateGroup() { OperatorName = groupOperator };
                searchTemplateController.SearchGroups.Add(group);

                // Create templates for each optimized rule
                foreach (var rule in optimizedRules)
                {
                    var template = new SearchTemplate(columnDataType)
                    {
                        SearchType = rule.SearchType
                    };

                    // Set values based on search type
                    switch (rule.SearchType)
                    {
                        case SearchType.Equals:
                        case SearchType.NotEquals:
                        case SearchType.GreaterThan:
                        case SearchType.LessThan:
                        case SearchType.GreaterThanOrEqualTo:
                        case SearchType.LessThanOrEqualTo:
                            template.SelectedValue = rule.PrimaryValue;
                            break;

                        case SearchType.Between:
                        case SearchType.NotBetween:
                        case SearchType.BetweenDates:
                            template.SelectedValue = rule.PrimaryValue;
                            template.SelectedSecondaryValue = rule.SecondaryValue;
                            break;

                        case SearchType.IsAnyOf:
                        case SearchType.IsNoneOf:
                            template.SelectedValues.Clear();
                            foreach (var value in rule.Values)
                            {
                                template.SelectedValues.Add(new FilterListValue { Value = value });
                            }
                            break;

                        case SearchType.IsNull:
                        case SearchType.IsNotNull:
                        case SearchType.IsEmpty:
                        case SearchType.IsNotEmpty:
                            // These don't need values - they are self-contained rules
                            break;
                    }

                    group.SearchTemplates.Add(template);
                }

                // Update filter expression
                searchTemplateController.UpdateFilterExpression();

                return FilterApplicationResult.Success(FilterApplicationType.ValueBased);
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying optimized filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies value-based filtering with intelligent preservation of existing rules
        /// Analyzes existing rules, preserves those that are still relevant, and adds supplementary rules
        /// </summary>
        private FilterApplicationResult ApplyValueBasedFilterWithRulePreservation(
            FilterValueViewModel filterValueViewModel,
            SearchTemplateController searchTemplateController,
            ColumnDataType columnDataType)
        {
            try
            {
                var selectedValues = filterValueViewModel.GetSelectedValues().ToList();
                var allValues = filterValueViewModel.GetAllValues().Select(v => v.Value).ToList();

                // If no existing meaningful rules, use standard optimization
                if (!HasCustomFilterRules(searchTemplateController))
                {
                    System.Diagnostics.Debug.WriteLine("No custom filter rules found - using standard optimization");
                    return ApplyOptimizedValueBasedFilter(filterValueViewModel, searchTemplateController, columnDataType);
                }
                
                System.Diagnostics.Debug.WriteLine("Custom filter rules detected - using rule preservation logic");

                // Analyze existing rules to see which selected values they cover
                var ruleAnalysis = AnalyzeRuleValueCoverage(searchTemplateController, selectedValues, allValues);
                
                // Preserve rules that still cover meaningful portions of selected values
                var rulesToPreserve = ruleAnalysis.RulesToPreserve;
                var valuesCoveredByRules = ruleAnalysis.ValuesCoveredByPreservedRules;
                var uncoveredValues = selectedValues.Except(valuesCoveredByRules).ToList();

                // Start building the new rule structure
                var preservedGroup = searchTemplateController.SearchGroups.FirstOrDefault();
                if (preservedGroup == null)
                {
                    preservedGroup = new SearchTemplateGroup { OperatorName = "Or" };
                    searchTemplateController.SearchGroups.Add(preservedGroup);
                }

                // Clear existing templates and add preserved ones back
                preservedGroup.SearchTemplates.Clear();
                foreach (var template in rulesToPreserve)
                {
                    preservedGroup.SearchTemplates.Add(template);
                }

                // Add supplementary rules for uncovered values
                if (uncoveredValues.Any())
                {
                    var supplementaryRules = GenerateSupplementaryRules(uncoveredValues, allValues, columnDataType);
                    foreach (var rule in supplementaryRules)
                    {
                        var template = CreateTemplateFromOptimizedRule(rule, columnDataType);
                        preservedGroup.SearchTemplates.Add(template);
                    }
                }

                // Set appropriate group operator based on rule combination
                preservedGroup.OperatorName = DetermineOptimalGroupOperator(preservedGroup.SearchTemplates.ToList());

                // Update filter expression
                searchTemplateController.UpdateFilterExpression();

                return FilterApplicationResult.Success(FilterApplicationType.ValueBased);
            }
            catch (Exception ex)
            {
                return FilterApplicationResult.Failure($"Error applying rule-preserving filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes existing rules to determine which should be preserved and what values they cover
        /// </summary>
        private RulePreservationAnalysis AnalyzeRuleValueCoverage(
            SearchTemplateController searchTemplateController, 
            List<object> selectedValues, 
            List<object> allValues)
        {
            var analysis = new RulePreservationAnalysis();
            var allTemplates = searchTemplateController.SearchGroups
                .SelectMany(g => g.SearchTemplates ?? Enumerable.Empty<SearchTemplate>())
                .Where(t => HasMeaningfulSearchCriteria(t))
                .ToList();

            foreach (var template in allTemplates)
            {
                var coveredValues = EvaluateTemplateCoverage(template, selectedValues, allValues);
                
                // Preserve rule if it covers a significant portion of selected values
                // or if it's a complex rule type that's valuable to keep
                if (ShouldPreserveRule(template, coveredValues, selectedValues))
                {
                    analysis.RulesToPreserve.Add(template);
                    analysis.ValuesCoveredByPreservedRules.UnionWith(coveredValues);
                }
            }

            return analysis;
        }

        /// <summary>
        /// Evaluates which values from the selected set are covered by a specific template
        /// </summary>
        private HashSet<object> EvaluateTemplateCoverage(SearchTemplate template, List<object> selectedValues, List<object> allValues)
        {
            var coveredValues = new HashSet<object>();
            
            foreach (var value in selectedValues)
            {
                try
                {
                    if (EvaluateSearchTemplate(value, template))
                    {
                        coveredValues.Add(value);
                    }
                }
                catch
                {
                    // If evaluation fails, assume not covered
                }
            }

            return coveredValues;
        }

        /// <summary>
        /// Determines if a rule should be preserved based on its coverage and complexity
        /// </summary>
        private bool ShouldPreserveRule(SearchTemplate template, HashSet<object> coveredValues, List<object> selectedValues)
        {
            if (!coveredValues.Any())
                return false;

            // Always preserve complex rules that are hard to recreate
            if (IsComplexRule(template.SearchType))
                return true;

            // Preserve if rule covers a significant portion (>= 30%) of selected values
            var coverageRatio = (double)coveredValues.Count / selectedValues.Count;
            if (coverageRatio >= 0.3)
                return true;

            // Preserve if rule covers multiple values (shows user intent)
            if (coveredValues.Count >= 2)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a search type represents a complex rule worth preserving
        /// </summary>
        private bool IsComplexRule(SearchType searchType)
        {
            return searchType switch
            {
                SearchType.Contains or SearchType.DoesNotContain or
                SearchType.StartsWith or SearchType.EndsWith or
                SearchType.IsLike or SearchType.IsNotLike or
                SearchType.Between or SearchType.NotBetween or
                SearchType.BetweenDates or SearchType.DateInterval or
                SearchType.LessThan or SearchType.GreaterThan or
                SearchType.LessThanOrEqualTo or SearchType.GreaterThanOrEqualTo or
                SearchType.TopN or SearchType.BottomN or
                SearchType.AboveAverage or SearchType.BelowAverage => true,
                _ => false
            };
        }

        /// <summary>
        /// Generates supplementary rules for values not covered by preserved rules
        /// </summary>
        private List<FilterRuleOptimizer.OptimizedRule> GenerateSupplementaryRules(
            List<object> uncoveredValues, 
            List<object> allValues, 
            ColumnDataType columnDataType)
        {
            // Use existing optimizer to generate rules for uncovered values
            var analysis = _ruleOptimizer.AnalyzeSelection(allValues, uncoveredValues, columnDataType);
            return _ruleOptimizer.GenerateOptimizedRules(analysis, allValues, uncoveredValues);
        }

        /// <summary>
        /// Creates a SearchTemplate from an OptimizedRule
        /// </summary>
        private SearchTemplate CreateTemplateFromOptimizedRule(FilterRuleOptimizer.OptimizedRule rule, ColumnDataType columnDataType)
        {
            var template = new SearchTemplate(columnDataType)
            {
                SearchType = rule.SearchType
            };

            // Set values based on search type
            switch (rule.SearchType)
            {
                case SearchType.Equals:
                case SearchType.NotEquals:
                case SearchType.GreaterThan:
                case SearchType.LessThan:
                case SearchType.GreaterThanOrEqualTo:
                case SearchType.LessThanOrEqualTo:
                    template.SelectedValue = rule.PrimaryValue;
                    break;

                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    template.SelectedValue = rule.PrimaryValue;
                    template.SelectedSecondaryValue = rule.SecondaryValue;
                    break;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                    template.SelectedValues.Clear();
                    foreach (var value in rule.Values)
                    {
                        template.SelectedValues.Add(new FilterListValue { Value = value });
                    }
                    break;

                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.IsEmpty:
                case SearchType.IsNotEmpty:
                    // These don't need values - they are self-contained rules
                    break;
            }

            return template;
        }

        /// <summary>
        /// Determines the optimal group operator based on the types of templates
        /// </summary>
        private string DetermineOptimalGroupOperator(List<SearchTemplate> templates)
        {
            var hasNegativeRules = templates.Any(t => IsNegativeSearchType(t.SearchType));
            var hasPositiveRules = templates.Any(t => !IsNegativeSearchType(t.SearchType));

            // If we have both positive and negative rules, use AND for safety
            if (hasNegativeRules && hasPositiveRules)
                return "And";

            // If all negative rules, use AND
            if (hasNegativeRules && !hasPositiveRules)
                return "And";

            // If all positive rules, use OR (typical filtering behavior)
            return "Or";
        }

        /// <summary>
        /// Analysis result for rule preservation
        /// </summary>
        private class RulePreservationAnalysis
        {
            public List<SearchTemplate> RulesToPreserve { get; set; } = new List<SearchTemplate>();
            public HashSet<object> ValuesCoveredByPreservedRules { get; set; } = new HashSet<object>();
        }

        /// <summary>
        /// Determines if a search type represents a negative/exclusion rule
        /// </summary>
        private bool IsNegativeSearchType(SearchType searchType)
        {
            return searchType switch
            {
                SearchType.NotEquals => true,
                SearchType.DoesNotContain => true,
                SearchType.NotBetween => true,
                SearchType.IsNoneOf => true,
                SearchType.IsNotNull => true,
                SearchType.IsNotEmpty => true,
                _ => false
            };
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

                if (groupChildCombinations.Count == 0)
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
                    if (selectedValues.Count != 0)
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
            bool groupedFilter(object columnValue)
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
            }

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

        /// <summary>
        /// Determines if a specific value should be selected based on current rules
        /// </summary>
        public bool ShouldValueBeSelected(object value, SearchTemplateController rules)
        {
            if (rules == null || !HasCustomFilterRules(rules))
                return true; // Default to selected if no meaningful rules

            try
            {
                // Use the controller's existing filter expression if available
                if (rules.FilterExpression != null)
                {
                    return rules.FilterExpression(value);
                }

                // Evaluate using search groups and templates
                return EvaluateSearchGroups(value, rules);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating value '{value}' against rules: {ex.Message}");
                return true; // Default to selected on error
            }
        }

        /// <summary>
        /// Evaluates value against search groups using SearchEngine
        /// </summary>
        private bool EvaluateSearchGroups(object value, SearchTemplateController rules)
        {
            if (rules.SearchGroups == null || rules.SearchGroups.Count == 0)
                return true;

            var groupResults = new List<bool>();

            foreach (var group in rules.SearchGroups)
            {
                var groupResult = EvaluateSearchGroup(value, group);
                groupResults.Add(groupResult);
            }

            // For multiple groups, use OR logic (any group match = include)
            return groupResults.Any(result => result);
        }

        /// <summary>
        /// Evaluates value against a single search group
        /// </summary>
        private bool EvaluateSearchGroup(object value, SearchTemplateGroup group)
        {
            if (group.SearchTemplates == null || group.SearchTemplates.Count == 0)
                return true;

            var templateResults = new List<bool>();

            foreach (var template in group.SearchTemplates)
            {
                var templateResult = EvaluateSearchTemplate(value, template);
                templateResults.Add(templateResult);
            }

            // Combine template results based on group operator
            var operatorName = group.OperatorName?.ToLower() ?? "and";
            
            if (operatorName == "or")
            {
                return templateResults.Any(result => result);
            }
            else // Default to "and"
            {
                return templateResults.All(result => result);
            }
        }

        /// <summary>
        /// Evaluates value against a single search template using SearchEngine
        /// </summary>
        private bool EvaluateSearchTemplate(object value, SearchTemplate template)
        {
            if (template == null || !HasMeaningfulSearchCriteria(template))
                return true;

            try
            {
                // Handle special cases that need custom logic
                switch (template.SearchType)
                {
                    case SearchType.IsAnyOf:
                        if (template.SelectedValues?.Any() == true)
                        {
                            return template.SelectedValues.Any(v => Equals(v, value));
                        }
                        return false;

                    case SearchType.IsNoneOf:
                        if (template.SelectedValues?.Any() == true)
                        {
                            return !template.SelectedValues.Any(v => Equals(v, value));
                        }
                        return true;

                    case SearchType.IsOnAnyOfDates:
                        if (template.SelectedDates?.Any() == true)
                        {
                            if (value is DateTime dateValue)
                            {
                                return template.SelectedDates.Any(d => d.Date == dateValue.Date);
                            }
                        }
                        return false;

                    default:
                        // Create search condition from template
                        var searchCondition = CreateSearchCondition(template);
                        
                        // Use SearchEngine to evaluate the condition
                        return SearchEngine.EvaluateCondition(value, searchCondition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating template: {ex.Message}");
                return true; // Default to include on error
            }
        }

        /// <summary>
        /// Creates SearchCondition from SearchTemplate
        /// </summary>
        private SearchCondition CreateSearchCondition(SearchTemplate template)
        {
            var condition = new SearchCondition
            {
                SearchType = template.SearchType,
                TargetType = GetTargetTypeFromColumnDataType(template.ColumnDataType)
            };

            // Set values based on search type
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
                case SearchType.DateInterval:
                case SearchType.TopN:
                case SearchType.BottomN:
                    condition.RawPrimaryValue = template.SelectedValue;
                    break;

                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    condition.RawPrimaryValue = template.SelectedValue;
                    condition.RawSecondaryValue = template.SelectedSecondaryValue;
                    break;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                case SearchType.IsOnAnyOfDates:
                    // Handle list-based types - store the list in RawPrimaryValue
                    var values = template.SelectedValues?.Select(v => v).ToList() ?? new List<object>();
                    condition.RawPrimaryValue = values;
                    break;

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
                    // These don't need values
                    break;
            }

            // Convert the values after setting them
            condition.ConvertValues();
            return condition;
        }

        /// <summary>
        /// Gets the target type from ColumnDataType enum
        /// </summary>
        private Type GetTargetTypeFromColumnDataType(ColumnDataType columnDataType)
        {
            return columnDataType switch
            {
                ColumnDataType.String => typeof(string),
                ColumnDataType.Number => typeof(decimal),
                ColumnDataType.DateTime => typeof(DateTime),
                ColumnDataType.Boolean => typeof(bool),
                ColumnDataType.Enum => typeof(string),
                _ => typeof(string)
            };
        }
    }
}