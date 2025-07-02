using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Interface for filter tokens that can be individually wrapped in the filter panel
    /// </summary>
    public interface IFilterToken
    {
        /// <summary>
        /// Gets the display text for this token
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// Gets the type of this token
        /// </summary>
        FilterTokenType TokenType { get; }

        /// <summary>
        /// Gets the ID of the logical filter this token belongs to
        /// </summary>
        string FilterId { get; }

        /// <summary>
        /// Gets the order index within the logical filter for proper sequencing
        /// </summary>
        int OrderIndex { get; }

        /// <summary>
        /// Gets whether this token represents the start of a logical filter
        /// </summary>
        bool IsFilterStart { get; }

        /// <summary>
        /// Gets whether this token represents the end of a logical filter
        /// </summary>
        bool IsFilterEnd { get; }

        /// <summary>
        /// Gets the original filter data for remove operations
        /// </summary>
        ColumnFilterInfo SourceFilter { get; }
    }

    /// <summary>
    /// Types of filter tokens
    /// </summary>
    public enum FilterTokenType
    {
        /// <summary>
        /// Opening bracket token (e.g., "[")
        /// </summary>
        OpenBracket,

        /// <summary>
        /// Column name token (e.g., "ColumnName")
        /// </summary>
        ColumnName,

        /// <summary>
        /// Search type token (e.g., "is any of", "contains")
        /// </summary>
        SearchType,

        /// <summary>
        /// Individual value token (e.g., "'Value1'")
        /// </summary>
        Value,

        /// <summary>
        /// Operator token between values (e.g., "and")
        /// </summary>
        Operator,

        /// <summary>
        /// Closing bracket token (e.g., "]")
        /// </summary>
        CloseBracket,

        /// <summary>
        /// Logical connector between filters (e.g., "AND", "OR")
        /// </summary>
        LogicalConnector,

        /// <summary>
        /// Remove action token for the entire logical filter
        /// </summary>
        RemoveAction
    }
}