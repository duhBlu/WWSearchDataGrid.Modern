namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// What the summary editor edits.
    /// </summary>
    public enum SummaryEditorMode
    {
        /// <summary>
        /// The grid-level <see cref="SearchDataGrid.GroupSummaries"/> — one shared set rendered
        /// in every group header at every level (entries target columns via
        /// <see cref="SummaryItem.FieldName"/>) — plus the grid's
        /// <see cref="SearchDataGrid.ShowGroupRowCount"/> entry.
        /// </summary>
        GroupHeaders,

        /// <summary>
        /// One column's <see cref="GridColumn.TotalSummaries"/> ("Totals for 'X'") — the
        /// entries rendered in that column's totals cell. The Items tab picks aggregation
        /// targets across every column; foreign targets render caption-qualified in the cell.
        /// </summary>
        ColumnTotals,

        /// <summary>
        /// The grid-level <see cref="SearchDataGrid.FixedTotalSummaries"/> — the fixed panel's
        /// own definition set. The "Show row count" checkbox maps to a no-FieldName Count entry
        /// (the same entry the panel's Count menu item toggles).
        /// </summary>
        FixedTotals,

        /// <summary>
        /// One column's <see cref="GridColumn.GroupFooterSummaries"/> ("Footer for 'X'") — the
        /// entries rendered in that column's cell of every group's footer row. Column-scoped like
        /// <see cref="ColumnTotals"/> (no alignment, no row count); entries may target other
        /// columns' fields and render caption-qualified.
        /// </summary>
        GroupFooterTotals
    }
}
