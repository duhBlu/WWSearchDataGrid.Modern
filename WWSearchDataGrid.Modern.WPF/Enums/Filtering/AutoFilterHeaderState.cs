namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Aggregate, semantic state of a column's auto-filter (filter row) cell, surfaced on
    /// <see cref="ColumnDataBase.AutoFilterHeaderState"/> for binding in headers, status bars,
    /// or styling. Distinct from <see cref="HeaderDisplayState"/>, which drives the cell's
    /// visual-state chrome (hover / editing animations) rather than its filter semantics.
    /// Resolved with this precedence: <see cref="Hidden"/> &gt; <see cref="Disabled"/> &gt;
    /// <see cref="Active"/> &gt; <see cref="PendingInput"/> &gt; <see cref="Empty"/>.
    /// </summary>
    public enum AutoFilterHeaderState
    {
        /// <summary>No filter applied and no pending input — the cell is empty and idle.</summary>
        Empty,

        /// <summary>
        /// The user has entered or changed a value that has not yet been applied — the deferred
        /// (non-live) filtering case awaiting a commit (Enter / Tab / focus loss). No grid filter
        /// from this cell is active yet.
        /// </summary>
        PendingInput,

        /// <summary>A filter originating from this cell is currently applied to the grid.</summary>
        Active,

        /// <summary>
        /// The cell is visible but disabled (e.g. <see cref="ColumnDataBase.AllowAutoFilter"/> is
        /// <c>false</c>) — greyed, keeps its layout space, accepts no input.
        /// </summary>
        Disabled,

        /// <summary>
        /// The cell is hidden entirely (e.g. <see cref="ColumnDataBase.AllowFiltering"/> is
        /// <c>false</c>) — the column exposes no filter surface.
        /// </summary>
        Hidden,
    }
}
