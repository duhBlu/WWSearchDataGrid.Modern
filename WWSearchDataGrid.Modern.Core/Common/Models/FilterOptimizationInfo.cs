using System;

namespace WWSearchDataGrid.Modern.Core.Common.Models
{
    /// <summary>
    /// Provides detailed information about filter optimization applied to selections
    /// </summary>
    public class FilterOptimizationInfo
    {
        /// <summary>
        /// Whether optimization was applied to the filter
        /// </summary>
        public bool OptimizationApplied { get; set; }

        /// <summary>
        /// The original filtering strategy that would have been used without optimization
        /// </summary>
        public string OriginalStrategy { get; set; } = string.Empty;

        /// <summary>
        /// The optimized filtering strategy that was actually applied
        /// </summary>
        public string OptimizedStrategy { get; set; } = string.Empty;

        /// <summary>
        /// Number of filter values saved by the optimization
        /// </summary>
        public int ValuesSaved { get; set; }

        /// <summary>
        /// User-friendly message explaining the optimization
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// Technical details about the optimization for debugging
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;

        /// <summary>
        /// Estimated performance improvement ratio (e.g., 0.8 means 80% performance gain)
        /// </summary>
        public double PerformanceGainRatio { get; set; }

        /// <summary>
        /// Creates optimization info for when optimization was applied
        /// </summary>
        /// <param name="originalCount">Number of values in original strategy</param>
        /// <param name="optimizedCount">Number of values in optimized strategy</param>
        /// <param name="originalType">Original search type</param>
        /// <param name="optimizedType">Optimized search type</param>
        /// <returns>FilterOptimizationInfo instance</returns>
        public static FilterOptimizationInfo CreateOptimized(
            int originalCount, 
            int optimizedCount, 
            string originalType, 
            string optimizedType)
        {
            var valuesSaved = originalCount - optimizedCount;
            var performanceGain = originalCount > 0 ? (double)valuesSaved / originalCount : 0;

            return new FilterOptimizationInfo
            {
                OptimizationApplied = true,
                OriginalStrategy = $"{originalType} with {originalCount} values",
                OptimizedStrategy = $"{optimizedType} with {optimizedCount} values",
                ValuesSaved = valuesSaved,
                PerformanceGainRatio = performanceGain,
                UserMessage = CreateUserFriendlyMessage(originalCount, optimizedCount, originalType, optimizedType),
                TechnicalDetails = $"Reduced filter expression from {originalCount} to {optimizedCount} values ({performanceGain:P1} improvement)"
            };
        }

        /// <summary>
        /// Creates optimization info for when no optimization was applied
        /// </summary>
        /// <param name="reason">Reason why optimization was not applied</param>
        /// <returns>FilterOptimizationInfo instance</returns>
        public static FilterOptimizationInfo CreateUnoptimized(string reason)
        {
            return new FilterOptimizationInfo
            {
                OptimizationApplied = false,
                OriginalStrategy = "Standard inclusion filter",
                OptimizedStrategy = "Standard inclusion filter",
                ValuesSaved = 0,
                PerformanceGainRatio = 0,
                UserMessage = "Using standard filter strategy",
                TechnicalDetails = reason
            };
        }

        private static string CreateUserFriendlyMessage(int originalCount, int optimizedCount, string originalType, string optimizedType)
        {
            if (optimizedType.Contains("NotEquals"))
            {
                return $"Excluding {optimizedCount} item{(optimizedCount == 1 ? "" : "s")} instead of including {originalCount}";
            }
            else if (optimizedType.Contains("IsNoneOf"))
            {
                return $"Excluding {optimizedCount} items instead of including {originalCount}";
            }
            else
            {
                return $"Optimized filter: {optimizedCount} values instead of {originalCount}";
            }
        }

        /// <summary>
        /// Gets a summary string for display in UI tooltips or status messages
        /// </summary>
        /// <returns>Brief summary of optimization</returns>
        public string GetSummary()
        {
            if (!OptimizationApplied)
                return "No optimization applied";

            return $"Optimized: {ValuesSaved} values saved ({PerformanceGainRatio:P0} improvement)";
        }

        /// <summary>
        /// Gets an icon name or symbol that represents the optimization status
        /// </summary>
        /// <returns>Icon identifier for UI display</returns>
        public string GetStatusIcon()
        {
            if (!OptimizationApplied)
                return "Standard";

            if (PerformanceGainRatio > 0.5)
                return "HighOptimization";
            else if (PerformanceGainRatio > 0.2)
                return "MediumOptimization";
            else
                return "LowOptimization";
        }
    }
}