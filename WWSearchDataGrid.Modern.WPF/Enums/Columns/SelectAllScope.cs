namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Defines the scope of items affected by the select-all checkbox operation.
    /// </summary>
    public enum SelectAllScope
    {
        /// <summary>
        /// Affects only the currently filtered/visible rows in the data grid.
        /// </summary>
        FilteredRows = 0,

        /// <summary>
        /// Affects only the currently selected rows.
        /// </summary>
        SelectedRows = 1,

        /// <summary>
        /// Affects all items in the ItemsSource regardless of filtering or selection state.
        /// </summary>
        AllItems = 2
    }
}
