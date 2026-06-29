namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// How a grouped grid renders its <see cref="SearchDataGrid.GroupSummaries"/> inside the
    /// group header rows.
    /// </summary>
    public enum GroupSummaryDisplayMode
    {
        /// <summary>
        /// Summaries render inline in each group header's left/right runs (the default).
        /// </summary>
        Header,

        /// <summary>
        /// Summaries that target a column render in the group header row <em>aligned under
        /// their target columns</em>, scrolling with the columns. Entries that don't resolve
        /// to a column (and the opt-in row count) stay in the header runs.
        /// </summary>
        AlignByColumns,
    }
}
