using System;
using System.Collections.Generic;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Service for intelligently synchronizing and merging filter rules with filter values
    /// </summary>
    public interface IFilterSynchronizationService
    {
        /// <summary>
        /// Intelligently merges filter rules and values based on compatibility analysis
        /// </summary>
        /// <param name="rules">Current search template controller with rules</param>
        /// <param name="values">Current filter value view model with selections</param>
        /// <param name="columnDataType">Data type of the column being filtered</param>
        /// <param name="context">Context information about the change that triggered merging</param>
        /// <returns>Result of the merge operation</returns>
        FilterMergeResult MergeFilters(
            SearchTemplateController rules, 
            FilterValueViewModel values, 
            ColumnDataType columnDataType,
            FilterChangeContext context);

        /// <summary>
        /// Analyzes rule coverage against selected values
        /// </summary>
        /// <param name="rules">Current rules</param>
        /// <param name="selectedValues">Currently selected values</param>
        /// <param name="allValues">All available values</param>
        /// <returns>Coverage analysis result</returns>
        RuleCoverageAnalysis AnalyzeRuleCoverage(
            SearchTemplateController rules,
            IEnumerable<object> selectedValues,
            IEnumerable<object> allValues);

        /// <summary>
        /// Determines the optimal merge strategy based on rule and value compatibility
        /// </summary>
        /// <param name="rules">Current rules</param>
        /// <param name="values">Current value selections</param>
        /// <param name="context">Change context</param>
        /// <returns>Recommended merge strategy</returns>
        FilterMergeStrategy DetermineMergeStrategy(
            SearchTemplateController rules,
            FilterValueViewModel values,
            FilterChangeContext context);
    }

    /// <summary>
    /// Context information about filter changes
    /// </summary>
    public class FilterChangeContext
    {
        /// <summary>
        /// Source of the change that triggered synchronization
        /// </summary>
        public FilterChangeSource Source { get; set; }

        /// <summary>
        /// Name of the property that changed (if applicable)
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Intensity/scope of the change
        /// </summary>
        public ChangeIntensity Intensity { get; set; }

        /// <summary>
        /// Time elapsed since the last change
        /// </summary>
        public TimeSpan TimeSinceLastChange { get; set; }

        /// <summary>
        /// Index of the currently active tab (0 = Rules, 1 = Values)
        /// </summary>
        public int ActiveTabIndex { get; set; }

        /// <summary>
        /// Whether this is part of a bulk operation (Select All, etc.)
        /// </summary>
        public bool IsBulkOperation { get; set; }
    }

    /// <summary>
    /// Source of filter changes
    /// </summary>
    public enum FilterChangeSource
    {
        /// <summary>
        /// Change originated from Rules tab
        /// </summary>
        Rules,

        /// <summary>
        /// Change originated from Values tab
        /// </summary>
        Values,

        /// <summary>
        /// Change originated from external source (API, etc.)
        /// </summary>
        External,

        /// <summary>
        /// Initial load or setup
        /// </summary>
        Initialization
    }

    /// <summary>
    /// Intensity/scope of changes
    /// </summary>
    public enum ChangeIntensity
    {
        /// <summary>
        /// Minor change (single field, small adjustment)
        /// </summary>
        Minor,

        /// <summary>
        /// Major change (multiple fields, significant modification)
        /// </summary>
        Major,

        /// <summary>
        /// Complete change (full replacement, clear all)
        /// </summary>
        Complete
    }

    /// <summary>
    /// Result of filter merge operation
    /// </summary>
    public class FilterMergeResult
    {
        /// <summary>
        /// Whether the merge was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if merge failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Strategy that was used for merging
        /// </summary>
        public FilterMergeStrategy StrategyUsed { get; set; }

        /// <summary>
        /// Whether rules were preserved during merge
        /// </summary>
        public bool RulesPreserved { get; set; }

        /// <summary>
        /// Whether values were synchronized during merge
        /// </summary>
        public bool ValuesSynchronized { get; set; }

        /// <summary>
        /// Number of rules that were preserved
        /// </summary>
        public int PreservedRuleCount { get; set; }

        /// <summary>
        /// Number of supplementary rules that were added
        /// </summary>
        public int SupplementaryRuleCount { get; set; }

        /// <summary>
        /// Coverage percentage of final rules against selected values
        /// </summary>
        public double FinalCoveragePercentage { get; set; }

        /// <summary>
        /// Additional metadata about the merge operation
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful merge result
        /// </summary>
        public static FilterMergeResult Success(FilterMergeStrategy strategy)
        {
            return new FilterMergeResult
            {
                IsSuccess = true,
                StrategyUsed = strategy
            };
        }

        /// <summary>
        /// Creates a failed merge result
        /// </summary>
        public static FilterMergeResult Failure(string errorMessage)
        {
            return new FilterMergeResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Strategy for merging filters
    /// </summary>
    public enum FilterMergeStrategy
    {
        /// <summary>
        /// Preserve existing rules, don't modify anything
        /// </summary>
        PreserveRules,

        /// <summary>
        /// Replace rules completely with value-based rules
        /// </summary>
        ReplaceWithValues,

        /// <summary>
        /// Preserve compatible rules and supplement with additional rules for uncovered values
        /// </summary>
        PreserveAndSupplement,

        /// <summary>
        /// Synchronize values to match existing rules
        /// </summary>
        SynchronizeValues,

        /// <summary>
        /// Clear all filters (Select All scenario)
        /// </summary>
        ClearAll,

        /// <summary>
        /// Intelligent merge based on compatibility analysis
        /// </summary>
        IntelligentMerge
    }

    /// <summary>
    /// Analysis of how well rules cover selected values
    /// </summary>
    public class RuleCoverageAnalysis
    {
        /// <summary>
        /// Percentage of selected values covered by existing rules
        /// </summary>
        public double CoveragePercentage { get; set; }

        /// <summary>
        /// Number of selected values covered by rules
        /// </summary>
        public int CoveredValueCount { get; set; }

        /// <summary>
        /// Number of selected values not covered by rules
        /// </summary>
        public int UncoveredValueCount { get; set; }

        /// <summary>
        /// Values that are covered by existing rules
        /// </summary>
        public HashSet<object> CoveredValues { get; set; } = new HashSet<object>();

        /// <summary>
        /// Values that are not covered by existing rules
        /// </summary>
        public HashSet<object> UncoveredValues { get; set; } = new HashSet<object>();

        /// <summary>
        /// Rules that contribute to value coverage
        /// </summary>
        public List<SearchTemplate> EffectiveRules { get; set; } = new List<SearchTemplate>();

        /// <summary>
        /// Rules that don't contribute to current value coverage
        /// </summary>
        public List<SearchTemplate> IneffectiveRules { get; set; } = new List<SearchTemplate>();

        /// <summary>
        /// Whether the rule coverage is sufficient for preservation
        /// </summary>
        public bool IsSufficientCoverage => CoveragePercentage >= 0.8; // 80% threshold

        /// <summary>
        /// Whether the rule coverage suggests optimization opportunity
        /// </summary>
        public bool SuggestsOptimization => CoveragePercentage < 0.4 && UncoveredValueCount > 0; // 40% threshold
    }
}