namespace WWControls.Wpf
{
    /// <summary>
    /// Specifies where a <see cref="GridColumn"/> is pinned within the
    /// <see cref="SearchDataGrid"/>. Pinned columns stay visible while the user scrolls
    /// the unpinned columns horizontally.
    /// </summary>
    public enum FixedColumnPosition
    {
        /// <summary>
        /// The column is not pinned and scrolls with the rest of the grid.
        /// </summary>
        None = 0,

        /// <summary>
        /// The column is pinned to the left edge of the grid (uses
        /// <see cref="System.Windows.Controls.DataGrid.FrozenColumnCount"/>).
        /// </summary>
        Left = 1,

        /// <summary>
        /// The column is pinned to the right edge of the grid's viewport: ordered after all
        /// unpinned columns and anchored in place while they scroll beneath it
        /// (via <see cref="FixedColumnsCellsPanel"/>'s right-band overlay).
        /// </summary>
        Right = 2
    }
}
