namespace WWSearchDataGrid.Modern.WPF
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
        /// The column is placed at the right end of the grid, after all
        /// unpinned columns. Provides a stable right-anchored display position;
        /// note that WPF's <c>DataGrid</c> only natively freezes from the left,
        /// so right-pinned columns participate in horizontal scrolling but
        /// remain ordered to the right of every unpinned column.
        /// </summary>
        Right = 2
    }
}
