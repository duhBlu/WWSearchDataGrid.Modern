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
}