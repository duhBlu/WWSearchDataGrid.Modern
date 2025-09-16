using System;

namespace WWSearchDataGrid.Modern.Core
{
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