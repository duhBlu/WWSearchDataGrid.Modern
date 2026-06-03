namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Visual state machine for Header-mode <see cref="ColumnFilterControl"/>. Cell-mode
    /// stays pinned at <see cref="Editing"/> so the same template's VisualStates resolve.
    /// </summary>
    public enum HeaderDisplayState
    {
        /// <summary>No active filter, no hover — header shows column name + sort arrow.</summary>
        Idle,

        /// <summary>Mouse over the header (no active filter); the search-icon button fades in.</summary>
        Hover,

        /// <summary>Editor + search-type selector visible. Used in both placement modes during input.</summary>
        Editing,

        /// <summary>Filter is active — header shows the filter value in accent color. Header-mode only.</summary>
        FilteredIdle,
    }
}
