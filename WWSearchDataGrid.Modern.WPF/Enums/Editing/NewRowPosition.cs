namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Where the new-item row — the placeholder that lets the user type a brand-new record — sits in
    /// a <see cref="SearchDataGrid"/>. A high-level wrapper over the base
    /// <see cref="System.Windows.Controls.DataGrid.CanUserAddRows"/> plus the editable view's
    /// <see cref="System.ComponentModel.IEditableCollectionView.NewItemPlaceholderPosition"/>.
    /// </summary>
    public enum NewRowPosition
    {
        /// <summary>No new-item row — adding rows through the grid is disabled.</summary>
        None,

        /// <summary>The new-item row sits above the data, as the first row.</summary>
        Top,

        /// <summary>
        /// The new-item row sits below the data, as the last row. Matches the stock WPF DataGrid
        /// behaviour when <see cref="System.Windows.Controls.DataGrid.CanUserAddRows"/> is on.
        /// </summary>
        Bottom,
    }
}
