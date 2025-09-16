using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core.Services
{
    /// <summary>
    /// Service interface for building filter expressions from search templates
    /// </summary>
    internal interface IFilterExpressionBuilder
    {
        /// <summary>
        /// Builds a compiled filter expression from search groups
        /// </summary>
        /// <param name="searchGroups">Collection of search template groups</param>
        /// <param name="targetColumnType">The target column type for expression building</param>
        /// <param name="forceTargetTypeAsString">Whether to force the target type to string</param>
        /// <returns>Filter expression build result containing the compiled expression and metadata</returns>
        FilterExpressionResult BuildFilterExpression(
            ObservableCollection<SearchTemplateGroup> searchGroups,
            Type targetColumnType,
            bool forceTargetTypeAsString = false);

        /// <summary>
        /// Determines the target column type from available values and metadata
        /// </summary>
        /// <param name="columnDataType">The column data type</param>
        /// <param name="columnValues">Available column values for type inference</param>
        /// <returns>The determined target column type</returns>
        Type DetermineTargetColumnType(ColumnDataType columnDataType, System.Collections.Generic.HashSet<object> columnValues);
    }

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
}