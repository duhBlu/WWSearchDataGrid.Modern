using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core.Common.Models;

namespace WWSearchDataGrid.Modern.Core.Common.Utilities
{
    /// <summary>
    /// Analyzes filter value selections and determines the most efficient filter strategy
    /// </summary>
    public static class FilterSelectionOptimizer
    {
        /// <summary>
        /// Default threshold for switching to exclusion logic (75% of items selected)
        /// </summary>
        public static double DefaultOptimizationThreshold = 0.75;

        /// <summary>
        /// Minimum number of items required before optimization is considered
        /// </summary>
        public static int MinimumItemsForOptimization = 10;

        /// <summary>
        /// Global toggle for optimization feature
        /// </summary>
        public static bool EnableOptimization = true;

        /// <summary>
        /// Analyzes selection patterns and determines the optimal filter strategy
        /// </summary>
        /// <param name="allItems">All available filter items</param>
        /// <param name="selectedItems">Currently selected filter items</param>
        /// <param name="dataType">The data type of the column being filtered</param>
        /// <returns>Optimization result with recommended strategy</returns>
        public static OptimizedFilterResult OptimizeSelections(
            List<FilterValueItem> allItems,
            List<FilterValueItem> selectedItems,
            ColumnDataType dataType)
        {
            if (!EnableOptimization || allItems == null || selectedItems == null)
            {
                var reason = !EnableOptimization ? "Optimization disabled" : "Invalid input provided";
                return new OptimizedFilterResult
                {
                    RecommendedSearchType = SearchType.IsAnyOf,
                    FilterValues = selectedItems?.Select(item => item.Value).ToList() ?? new List<object>(),
                    UseExclusionLogic = false,
                    OptimizationReason = reason,
                    OptimizationInfo = FilterOptimizationInfo.CreateUnoptimized(reason)
                };
            }

            var totalCount = allItems.Count;
            var selectedCount = selectedItems.Count;

            // Handle edge cases
            if (totalCount == 0 || selectedCount == 0)
            {
                var reason = "No items to filter";
                return new OptimizedFilterResult
                {
                    RecommendedSearchType = SearchType.IsAnyOf,
                    FilterValues = new List<object>(),
                    UseExclusionLogic = false,
                    OptimizationReason = reason,
                    OptimizationInfo = FilterOptimizationInfo.CreateUnoptimized(reason)
                };
            }

            if (selectedCount == totalCount)
            {
                var reason = "All items selected - no optimization needed";
                return new OptimizedFilterResult
                {
                    RecommendedSearchType = SearchType.IsAnyOf,
                    FilterValues = selectedItems.Select(item => item.Value).ToList(),
                    UseExclusionLogic = false,
                    OptimizationReason = reason,
                    OptimizationInfo = FilterOptimizationInfo.CreateUnoptimized(reason)
                };
            }

            // Skip optimization for small datasets
            if (totalCount < MinimumItemsForOptimization)
            {
                var reason = $"Dataset too small ({totalCount} items) - optimization threshold is {MinimumItemsForOptimization}";
                return new OptimizedFilterResult
                {
                    RecommendedSearchType = SearchType.IsAnyOf,
                    FilterValues = selectedItems.Select(item => item.Value).ToList(),
                    UseExclusionLogic = false,
                    OptimizationReason = reason,
                    OptimizationInfo = FilterOptimizationInfo.CreateUnoptimized(reason)
                };
            }

            var optimizationThreshold = CalculateOptimizationThreshold(totalCount);
            var selectionRatio = (double)selectedCount / totalCount;

            // Determine if exclusion logic would be more efficient
            if (selectionRatio > optimizationThreshold)
            {
                var unselectedItems = allItems.Where(item => !selectedItems.Any(s => Equals(s.Value, item.Value))).ToList();
                var unselectedCount = unselectedItems.Count;

                var searchType = DetermineOptimalSearchType(totalCount, selectedCount, dataType);
                var searchTypeName = GetSearchTypeDisplayName(searchType);
                
                var optimizationInfo = FilterOptimizationInfo.CreateOptimized(
                    selectedCount, 
                    unselectedCount, 
                    "IsAnyOf", 
                    searchTypeName);
                
                return new OptimizedFilterResult
                {
                    RecommendedSearchType = searchType,
                    FilterValues = unselectedItems.Select(item => item.Value).ToList(),
                    UseExclusionLogic = true,
                    OptimizationReason = $"Exclusion more efficient: {unselectedCount} excluded vs {selectedCount} included (ratio: {selectionRatio:P1})",
                    OptimizationInfo = optimizationInfo
                };
            }

            // Use standard inclusion logic
            var noOptimizationInfo = FilterOptimizationInfo.CreateUnoptimized(
                $"Inclusion optimal: {selectedCount} included vs {totalCount - selectedCount} excluded (ratio: {selectionRatio:P1})");
            
            return new OptimizedFilterResult
            {
                RecommendedSearchType = SearchType.IsAnyOf,
                FilterValues = selectedItems.Select(item => item.Value).ToList(),
                UseExclusionLogic = false,
                OptimizationReason = $"Inclusion optimal: {selectedCount} included vs {totalCount - selectedCount} excluded (ratio: {selectionRatio:P1})",
                OptimizationInfo = noOptimizationInfo
            };
        }

        /// <summary>
        /// Determines the optimal SearchType based on data characteristics
        /// </summary>
        /// <param name="totalCount">Total number of items</param>
        /// <param name="selectedCount">Number of selected items</param>
        /// <param name="dataType">Column data type</param>
        /// <returns>Recommended SearchType for exclusion logic</returns>
        public static SearchType DetermineOptimalSearchType(int totalCount, int selectedCount, ColumnDataType dataType)
        {
            var unselectedCount = totalCount - selectedCount;

            // For single value exclusion, use NotEquals when possible
            if (unselectedCount == 1)
            {
                // Most data types support NotEquals
                switch (dataType)
                {
                    case ColumnDataType.String:
                    case ColumnDataType.Number:
                    case ColumnDataType.DateTime:
                    case ColumnDataType.Boolean:
                        return SearchType.NotEquals;
                    default:
                        return SearchType.IsNoneOf; // Fallback for complex types
                }
            }

            // For multiple value exclusion, use IsNoneOf
            return SearchType.IsNoneOf;
        }

        /// <summary>
        /// Calculates the optimization threshold based on dataset size
        /// </summary>
        /// <param name="totalCount">Total number of items in the dataset</param>
        /// <returns>Threshold ratio for switching to exclusion logic</returns>
        public static double CalculateOptimizationThreshold(int totalCount)
        {
            // Adjust threshold based on dataset size
            if (totalCount <= 50)
                return 0.8; // Higher threshold for small datasets

            if (totalCount <= 200)
                return 0.75; // Standard threshold for medium datasets

            if (totalCount <= 1000)
                return 0.7; // Lower threshold for large datasets

            return 0.65; // Even lower threshold for very large datasets
        }

        /// <summary>
        /// Gets a user-friendly display name for a SearchType
        /// </summary>
        /// <param name="searchType">The SearchType to get display name for</param>
        /// <returns>Human-readable display name</returns>
        private static string GetSearchTypeDisplayName(SearchType searchType)
        {
            switch (searchType)
            {
                case SearchType.NotEquals:
                    return "NotEquals";
                case SearchType.IsNoneOf:
                    return "IsNoneOf";
                case SearchType.IsAnyOf:
                    return "IsAnyOf";
                default:
                    return searchType.ToString();
            }
        }
    }

    /// <summary>
    /// Result of filter selection optimization analysis
    /// </summary>
    public class OptimizedFilterResult
    {
        /// <summary>
        /// The recommended SearchType for optimal performance
        /// </summary>
        public SearchType RecommendedSearchType { get; set; }

        /// <summary>
        /// The values to use in the filter (either selected values for inclusion or unselected values for exclusion)
        /// </summary>
        public List<object> FilterValues { get; set; } = new List<object>();

        /// <summary>
        /// Indicates whether exclusion logic is being used
        /// </summary>
        public bool UseExclusionLogic { get; set; }

        /// <summary>
        /// Human-readable explanation of the optimization decision
        /// </summary>
        public string OptimizationReason { get; set; } = string.Empty;

        /// <summary>
        /// Detailed optimization information for user feedback
        /// </summary>
        public FilterOptimizationInfo OptimizationInfo { get; set; }

        /// <summary>
        /// Number of values saved by the optimization
        /// </summary>
        public int ValuesSaved => UseExclusionLogic && OptimizationInfo != null ? 
            OptimizationInfo.ValuesSaved : 0;
    }
}