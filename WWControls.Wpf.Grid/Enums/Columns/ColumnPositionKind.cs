namespace WWControls.Wpf
{
    /// <summary>
    /// Position of a column within the visible-column collection. Surfaced as the read-only
    /// <see cref="ColumnLayoutBase.ColumnPosition"/> dependency property so templates can light up
    /// edge styling (rounded corners on the first/last column, separators in the middle, etc.)
    /// via property triggers without per-column boilerplate.
    /// </summary>
    public enum ColumnPositionKind
    {
        /// <summary>
        /// The column is not visible. Default for columns whose
        /// <see cref="ColumnLayoutBase.Visible"/> is false or whose
        /// <see cref="ColumnLayoutBase.ActualVisibleIndex"/> has not been resolved yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The column is the first visible column and at least one other column is visible.
        /// </summary>
        First = 1,

        /// <summary>
        /// The column has visible columns on both sides.
        /// </summary>
        Middle = 2,

        /// <summary>
        /// The column is the last visible column and at least one other column is visible.
        /// </summary>
        Last = 3,

        /// <summary>
        /// The column is the only visible column.
        /// </summary>
        Single = 4
    }
}
