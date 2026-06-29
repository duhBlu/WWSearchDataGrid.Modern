namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// How much data a best-fit pass measures when computing a column's ideal width.
    /// </summary>
    public enum BestFitMode
    {
        /// <summary>
        /// Inherit: column-level <c>Default</c> falls back to the grid's
        /// <c>SearchDataGrid.BestFitMode</c>; a grid-level <c>Default</c> resolves to
        /// <see cref="AllRows"/>.
        /// </summary>
        Default,

        /// <summary>
        /// Measure only the realized (on-screen) rows via direct cell measurement. Fast, but the
        /// result depends on the current scroll position.
        /// </summary>
        VisibleRows,

        /// <summary>
        /// Measure every row in the filtered leaf set by formatting each cell's display text and
        /// measuring it, calibrated against realized cell chrome. Scroll-independent. Columns whose
        /// content can't be text-measured (user cell templates, checkbox columns) fall back to
        /// <see cref="VisibleRows"/> behavior.
        /// </summary>
        AllRows,
    }
}
