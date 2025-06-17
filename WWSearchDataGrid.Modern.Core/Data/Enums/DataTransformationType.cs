using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Defines the types of data transformations that can be applied before traditional filtering
    /// </summary>
    public enum DataTransformationType
    {
        /// <summary>
        /// No transformation applied
        /// </summary>
        None,

        /// <summary>
        /// Show only the top N items by value (e.g., "Top 5 highest salaries")
        /// </summary>
        TopN,

        /// <summary>
        /// Show only the bottom N items by value (e.g., "Bottom 3 lowest scores")
        /// </summary>
        BottomN,

        /// <summary>
        /// Show only items with values above the column average
        /// </summary>
        AboveAverage,

        /// <summary>
        /// Show only items with values below the column average
        /// </summary>
        BelowAverage,

        /// <summary>
        /// Show only items where the column value appears once in the dataset
        /// </summary>
        Unique,

        /// <summary>
        /// Show only items where the column value appears multiple times in the dataset
        /// </summary>
        Duplicate
    }
}