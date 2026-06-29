namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Which parts of a column participate in a best-fit width calculation.
    /// </summary>
    public enum BestFitArea
    {
        /// <summary>Fit to the larger of the header and the data cells.</summary>
        All,

        /// <summary>Fit to the column header only.</summary>
        Header,

        /// <summary>Fit to the data cells only.</summary>
        Rows,
    }
}
