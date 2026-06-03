namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Sort direction surfaced on a <see cref="ColumnDataBase"/> descriptor. Distinct from
    /// <see cref="System.ComponentModel.ListSortDirection"/> in that it carries a
    /// <see cref="None"/> value for unsorted columns.
    /// </summary>
    public enum ColumnSortOrder
    {
        /// <summary>The column is not participating in the current sort.</summary>
        None = 0,

        /// <summary>The column is sorted in ascending order.</summary>
        Ascending = 1,

        /// <summary>The column is sorted in descending order.</summary>
        Descending = 2
    }
}
