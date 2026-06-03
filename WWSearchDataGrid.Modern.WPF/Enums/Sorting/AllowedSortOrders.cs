using System;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Bitmask describing which <see cref="ColumnSortOrder"/> values a column allows the user
    /// to cycle through. Surfaced as <see cref="ColumnDataBase.AllowedSortOrders"/> — when the user
    /// clicks the header, the next direction in the cycle that is allowed is selected.
    /// </summary>
    [Flags]
    public enum AllowedSortOrders
    {
        /// <summary>No sort directions are allowed (sorting effectively disabled).</summary>
        None = 0,

        /// <summary>The column may be sorted ascending.</summary>
        Ascending = 1,

        /// <summary>The column may be sorted descending.</summary>
        Descending = 2,

        /// <summary>Both ascending and descending sorting are allowed (the default).</summary>
        All = Ascending | Descending
    }
}
