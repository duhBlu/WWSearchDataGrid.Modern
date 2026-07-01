namespace WWControls.Wpf
{
    /// <summary>
    /// Where the column-aligned total summary row docks relative to the data area.
    /// <see cref="SearchDataGrid.ShowTotalSummary"/> remains the master toggle; this picks the
    /// edge (or suppresses the row entirely via <see cref="None"/>).
    /// </summary>
    public enum TotalSummaryPosition
    {
        /// <summary>No column-aligned total summary row.</summary>
        None,

        /// <summary>Pinned above the data area, beneath the filter row.</summary>
        Top,

        /// <summary>Pinned beneath the data area, above the horizontal scrollbar (the default).</summary>
        Bottom
    }
}
