using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Implementation of intelligent filter synchronization and merging
    /// </summary>
    public class FilterSynchronizationService : IFilterSynchronizationService
    {
        private readonly IFilterApplicationService _filterApplicationService;
        private readonly IRuleToValueSynchronizationService _ruleToValueSyncService;
        private readonly FilterRuleOptimizer _ruleOptimizer;

        /// <summary>
        /// Initializes a new instance of FilterSynchronizationService
        /// </summary>
        public FilterSynchronizationService()
        {
            _filterApplicationService = new FilterApplicationService();
            _ruleToValueSyncService = new RuleToValueSynchronizationService();
            _ruleOptimizer = new FilterRuleOptimizer();
        }

        /// <summary>
        /// Constructor for testing with dependency injection
        /// </summary>
        internal FilterSynchronizationService(
            IFilterApplicationService filterApplicationService,
            IRuleToValueSynchronizationService ruleToValueSyncService)
        {
            _filterApplicationService = filterApplicationService ?? throw new ArgumentNullException(nameof(filterApplicationService));
            _ruleToValueSyncService = ruleToValueSyncService ?? throw new ArgumentNullException(nameof(ruleToValueSyncService));
            _ruleOptimizer = new FilterRuleOptimizer();
        }

        /// <summary>
        /// Intelligently merges filter rules and values based on compatibility analysis
        /// </summary>
        public FilterMergeResult MergeFilters(
            SearchTemplateController rules,
            FilterValueViewModel values,
            ColumnDataType columnDataType,
            FilterChangeContext context)
        {
            try
            {
                if (rules == null || values == null)
                {
                    return FilterMergeResult.Failure("Rules or values are null");
                }

                // Determine the optimal merge strategy
                var strategy = DetermineMergeStrategy(rules, values, context);

                // Execute the merge based on the determined strategy
                return ExecuteMergeStrategy(strategy, rules, values, columnDataType, context);
            }
            catch (Exception ex)
            {
                return FilterMergeResult.Failure($"Error during filter merge: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes rule coverage against selected values
        /// </summary>
        public RuleCoverageAnalysis AnalyzeRuleCoverage(
            SearchTemplateController rules,
            IEnumerable<object> selectedValues,
            IEnumerable<object> allValues)
        {
            var analysis = new RuleCoverageAnalysis();
            var selectedList = selectedValues.ToList();
            var allList = allValues.ToList();

            if (!selectedList.Any())
            {
                return analysis; // Empty selection, no coverage needed
            }

            // Get all meaningful rules
            var meaningfulRules = GetMeaningfulRules(rules);
            if (!meaningfulRules.Any())
            {
                analysis.UncoveredValues = new HashSet<object>(selectedList);
                analysis.UncoveredValueCount = selectedList.Count;
                return analysis;
            }

            // Evaluate each selected value against the rules
            foreach (var value in selectedList)
            {
                var isValueCovered = _ruleToValueSyncService.ShouldValueBeSelected(value, rules);
                if (isValueCovered)
                {
                    analysis.CoveredValues.Add(value);
                }
                else
                {
                    analysis.UncoveredValues.Add(value);
                }
            }

            // Calculate coverage metrics
            analysis.CoveredValueCount = analysis.CoveredValues.Count;
            analysis.UncoveredValueCount = analysis.UncoveredValues.Count;
            analysis.CoveragePercentage = selectedList.Count > 0 
                ? (double)analysis.CoveredValueCount / selectedList.Count 
                : 0.0;

            // Classify rules as effective or ineffective
            foreach (var rule in meaningfulRules)
            {
                var ruleCoversAnySelectedValue = selectedList.Any(value => 
                    EvaluateSingleRuleAgainstValue(value, rule));
                
                if (ruleCoversAnySelectedValue)
                {
                    analysis.EffectiveRules.Add(rule);
                }
                else
                {
                    analysis.IneffectiveRules.Add(rule);
                }
            }

            return analysis;
        }

        /// <summary>
        /// Determines the optimal merge strategy based on rule and value compatibility
        /// </summary>
        public FilterMergeStrategy DetermineMergeStrategy(
            SearchTemplateController rules,
            FilterValueViewModel values,
            FilterChangeContext context)
        {
            var selectedValues = values?.GetSelectedValues().ToList() ?? new List<object>();
            var allValues = values?.GetAllValues().Select(v => v.Value).ToList() ?? new List<object>();

            // Handle special cases first
            if (context.IsBulkOperation || IsSelectAllScenario(selectedValues, allValues))
            {
                return FilterMergeStrategy.ClearAll;
            }

            // If no meaningful selections, preserve existing rules
            if (!selectedValues.Any() || selectedValues.Count == allValues.Count)
            {
                return FilterMergeStrategy.PreserveRules;
            }

            var meaningfulRules = GetMeaningfulRules(rules);
            
            // If no meaningful rules exist, replace with value-based rules
            if (!meaningfulRules.Any())
            {
                return FilterMergeStrategy.ReplaceWithValues;
            }

            // Analyze rule coverage
            var coverage = AnalyzeRuleCoverage(rules, selectedValues, allValues);

            // Determine strategy based on context and coverage
            return context.Source switch
            {
                FilterChangeSource.Rules when context.ActiveTabIndex == 0 => 
                    // User is on Rules tab - synchronize values to match rules
                    FilterMergeStrategy.SynchronizeValues,

                FilterChangeSource.Values when context.ActiveTabIndex == 1 => 
                    // User is on Values tab - decide based on coverage
                    DetermineValueBasedStrategy(coverage, context),

                _ => 
                    // Default to intelligent merge for ambiguous cases
                    FilterMergeStrategy.IntelligentMerge
            };
        }

        /// <summary>
        /// Determines strategy for value-based changes
        /// </summary>
        private FilterMergeStrategy DetermineValueBasedStrategy(RuleCoverageAnalysis coverage, FilterChangeContext context)
        {
            // High coverage - preserve and supplement
            if (coverage.IsSufficientCoverage)
            {
                return FilterMergeStrategy.PreserveAndSupplement;
            }

            // Low coverage but complex rules - still try to preserve and supplement
            if (coverage.EffectiveRules.Any(rule => IsComplexRule(rule)))
            {
                return FilterMergeStrategy.PreserveAndSupplement;
            }

            // Low coverage with simple rules - replace with optimized value-based rules
            if (coverage.SuggestsOptimization)
            {
                return FilterMergeStrategy.ReplaceWithValues;
            }

            // Medium coverage - intelligent merge
            return FilterMergeStrategy.IntelligentMerge;
        }

        /// <summary>
        /// Executes the determined merge strategy
        /// </summary>
        private FilterMergeResult ExecuteMergeStrategy(
            FilterMergeStrategy strategy,
            SearchTemplateController rules,
            FilterValueViewModel values,
            ColumnDataType columnDataType,
            FilterChangeContext context)
        {
            return strategy switch
            {
                FilterMergeStrategy.PreserveRules => ExecutePreserveRules(rules, values),
                FilterMergeStrategy.ReplaceWithValues => ExecuteReplaceWithValues(rules, values, columnDataType),
                FilterMergeStrategy.PreserveAndSupplement => ExecutePreserveAndSupplement(rules, values, columnDataType),
                FilterMergeStrategy.SynchronizeValues => ExecuteSynchronizeValues(rules, values),
                FilterMergeStrategy.ClearAll => ExecuteClearAll(rules),
                FilterMergeStrategy.IntelligentMerge => ExecuteIntelligentMerge(rules, values, columnDataType),
                _ => FilterMergeResult.Failure($"Unknown merge strategy: {strategy}")
            };
        }

        /// <summary>
        /// Preserve existing rules, don't change anything
        /// </summary>
        private FilterMergeResult ExecutePreserveRules(SearchTemplateController rules, FilterValueViewModel values)
        {
            // No changes needed - rules are preserved as-is
            var result = FilterMergeResult.Success(FilterMergeStrategy.PreserveRules);
            result.RulesPreserved = true;
            result.PreservedRuleCount = GetMeaningfulRules(rules).Count();
            return result;
        }

        /// <summary>
        /// Replace rules with value-based optimized rules
        /// </summary>
        private FilterMergeResult ExecuteReplaceWithValues(
            SearchTemplateController rules,
            FilterValueViewModel values,
            ColumnDataType columnDataType)
        {
            var applicationResult = _filterApplicationService.ApplyValueBasedFilter(values, rules, columnDataType);
            
            if (!applicationResult.IsSuccess)
            {
                return FilterMergeResult.Failure(applicationResult.ErrorMessage);
            }

            var result = FilterMergeResult.Success(FilterMergeStrategy.ReplaceWithValues);
            result.RulesPreserved = false;
            result.ValuesSynchronized = false; // Values drove the rules
            result.SupplementaryRuleCount = GetMeaningfulRules(rules).Count();
            return result;
        }

        /// <summary>
        /// Preserve compatible rules and add supplementary rules for uncovered values
        /// </summary>
        private FilterMergeResult ExecutePreserveAndSupplement(
            SearchTemplateController rules,
            FilterValueViewModel values,
            ColumnDataType columnDataType)
        {
            // This leverages the existing rule preservation logic in FilterApplicationService
            var applicationResult = _filterApplicationService.ApplyValueBasedFilter(values, rules, columnDataType);
            
            if (!applicationResult.IsSuccess)
            {
                return FilterMergeResult.Failure(applicationResult.ErrorMessage);
            }

            var result = FilterMergeResult.Success(FilterMergeStrategy.PreserveAndSupplement);
            result.RulesPreserved = true;
            result.ValuesSynchronized = false;
            
            // Count preserved vs supplementary rules (approximation)
            var totalRules = GetMeaningfulRules(rules).Count();
            result.PreservedRuleCount = totalRules / 2; // Rough estimate
            result.SupplementaryRuleCount = totalRules - result.PreservedRuleCount;
            
            return result;
        }

        /// <summary>
        /// Synchronize values to match existing rules
        /// </summary>
        private FilterMergeResult ExecuteSynchronizeValues(SearchTemplateController rules, FilterValueViewModel values)
        {
            try
            {
                // Get all available values
                var allValues = values.GetAllValues().Select(v => v.Value);
                
                // Evaluate which values should be selected based on current rules
                var selectedValues = _ruleToValueSyncService.EvaluateRulesAgainstValues(rules, allValues);
                
                // Update the FilterValueViewModel selections
                values.UpdateSelectionsFromRules(selectedValues);

                var result = FilterMergeResult.Success(FilterMergeStrategy.SynchronizeValues);
                result.RulesPreserved = true;
                result.ValuesSynchronized = true;
                result.PreservedRuleCount = GetMeaningfulRules(rules).Count();
                
                return result;
            }
            catch (Exception ex)
            {
                return FilterMergeResult.Failure($"Error synchronizing values: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all filters (Select All scenario)
        /// </summary>
        private FilterMergeResult ExecuteClearAll(SearchTemplateController rules)
        {
            var applicationResult = _filterApplicationService.ClearAllFilters(rules);
            
            if (!applicationResult.IsSuccess)
            {
                return FilterMergeResult.Failure(applicationResult.ErrorMessage);
            }

            var result = FilterMergeResult.Success(FilterMergeStrategy.ClearAll);
            result.RulesPreserved = false;
            result.ValuesSynchronized = false;
            return result;
        }

        /// <summary>
        /// Intelligent merge based on complex analysis
        /// </summary>
        private FilterMergeResult ExecuteIntelligentMerge(
            SearchTemplateController rules,
            FilterValueViewModel values,
            ColumnDataType columnDataType)
        {
            // For now, intelligent merge delegates to preserve and supplement
            // This can be enhanced with more sophisticated logic in the future
            return ExecutePreserveAndSupplement(rules, values, columnDataType);
        }

        /// <summary>
        /// Gets all meaningful rules from the controller
        /// </summary>
        private IEnumerable<SearchTemplate> GetMeaningfulRules(SearchTemplateController rules)
        {
            if (rules?.SearchGroups == null)
                return Enumerable.Empty<SearchTemplate>();

            return rules.SearchGroups
                .SelectMany(g => g.SearchTemplates ?? Enumerable.Empty<SearchTemplate>())
                .Where(t => HasMeaningfulSearchCriteria(t));
        }

        /// <summary>
        /// Determines if a search template has meaningful search criteria
        /// </summary>
        private bool HasMeaningfulSearchCriteria(SearchTemplate template)
        {
            if (template == null)
                return false;

            // Reuse logic from FilterApplicationService
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

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a rule is complex and worth preserving
        /// </summary>
        private bool IsComplexRule(SearchTemplate template)
        {
            return template.SearchType switch
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
        /// Evaluates a single rule against a value
        /// </summary>
        private bool EvaluateSingleRuleAgainstValue(object value, SearchTemplate rule)
        {
            try
            {
                // Create a temporary controller with just this rule for evaluation
                var tempController = new SearchTemplateController(typeof(SearchTemplate))
                {
                    ColumnDataType = rule.ColumnDataType
                };
                
                var tempGroup = new SearchTemplateGroup();
                tempGroup.SearchTemplates.Add(rule);
                tempController.SearchGroups.Add(tempGroup);
                
                return _ruleToValueSyncService.ShouldValueBeSelected(value, tempController);
            }
            catch
            {
                return false; // Default to not covered on evaluation error
            }
        }

        /// <summary>
        /// Determines if the current selection represents a Select All scenario
        /// </summary>
        private bool IsSelectAllScenario(List<object> selectedValues, List<object> allValues)
        {
            return selectedValues.Count == allValues.Count || selectedValues.Count == 0;
        }
    }
}