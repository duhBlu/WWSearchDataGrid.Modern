using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Service interface for applying different types of filters
    /// </summary>
    public interface IFilterApplicationService
    {
        /// <summary>
        /// Applies value-based filtering from selected filter values
        /// </summary>
        /// <param name="filterValueViewModel">Filter value view model containing selections</param>
        /// <param name="searchTemplateController">Search template controller to update</param>
        /// <param name="columnDataType">Column data type for optimization</param>
        /// <returns>Filter application result</returns>
        FilterApplicationResult ApplyValueBasedFilter(
            FilterValueViewModel filterValueViewModel,
            SearchTemplateController searchTemplateController,
            ColumnDataType columnDataType);

        /// <summary>
        /// Applies grouped value-based filtering that respects group-child combinations
        /// </summary>
        /// <param name="groupedViewModel">Grouped filter value view model</param>
        /// <param name="searchTemplateController">Search template controller to update</param>
        /// <param name="currentColumnPath">Current column binding path</param>
        /// <param name="groupByColumnPath">Group by column binding path</param>
        /// <returns>Filter application result</returns>
        FilterApplicationResult ApplyGroupedValueBasedFilter(
            GroupedTreeViewFilterValueViewModel groupedViewModel,
            SearchTemplateController searchTemplateController,
            string currentColumnPath,
            string groupByColumnPath);

        /// <summary>
        /// Applies rule-based filtering from search templates
        /// </summary>
        /// <param name="searchTemplateController">Search template controller containing rules</param>
        /// <returns>Filter application result</returns>
        FilterApplicationResult ApplyRuleBasedFilter(SearchTemplateController searchTemplateController);

        /// <summary>
        /// Clears all filters and resets to default state
        /// </summary>
        /// <param name="searchTemplateController">Search template controller to clear</param>
        /// <returns>Filter application result</returns>
        FilterApplicationResult ClearAllFilters(SearchTemplateController searchTemplateController);
    }

    /// <summary>
    /// Result of a filter application operation
    /// </summary>
    public class FilterApplicationResult
    {
        /// <summary>
        /// Whether the filter application was successful
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Error message if application failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Whether the filter has custom expression logic
        /// </summary>
        public bool HasCustomExpression { get; set; }

        /// <summary>
        /// Whether the filter requires collection-context evaluation
        /// </summary>
        public bool RequiresCollectionContext { get; set; }

        /// <summary>
        /// Filter type that was applied
        /// </summary>
        public FilterApplicationType FilterType { get; set; }

        /// <summary>
        /// Additional metadata about the applied filter
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static FilterApplicationResult Success(FilterApplicationType filterType, bool hasCustomExpression = true)
        {
            return new FilterApplicationResult
            {
                IsSuccess = true,
                FilterType = filterType,
                HasCustomExpression = hasCustomExpression
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static FilterApplicationResult Failure(string errorMessage)
        {
            return new FilterApplicationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Types of filter applications
    /// </summary>
    public enum FilterApplicationType
    {
        /// <summary>
        /// No filter applied
        /// </summary>
        None,

        /// <summary>
        /// Value-based filter from selected values
        /// </summary>
        ValueBased,

        /// <summary>
        /// Grouped value-based filter with parent-child relationships
        /// </summary>
        GroupedValueBased,

        /// <summary>
        /// Rule-based filter from search templates
        /// </summary>
        RuleBased,

        /// <summary>
        /// All filters cleared
        /// </summary>
        Cleared
    }
}