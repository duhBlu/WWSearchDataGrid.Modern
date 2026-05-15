namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Visual state machine for <see cref="ColumnFilterControl"/> when hosted inside a
    /// <see cref="System.Windows.Controls.Primitives.DataGridColumnHeader"/> in Header-mode
    /// placement. In Cell-mode placement the editor is always visible and the state stays at
    /// <see cref="Editing"/> — the enum still exists in that path so the same template's
    /// VisualStates resolve uniformly.
    /// </summary>
    public enum HeaderDisplayState
    {
        /// <summary>
        /// No active filter and the user isn't hovering. Header chrome shows the column name
        /// plus the sort arrow; the search-icon button and clear button are collapsed.
        /// </summary>
        Idle,

        /// <summary>
        /// Mouse is over the header (no active filter). The search-icon button fades in;
        /// clicking it transitions to <see cref="Editing"/>.
        /// </summary>
        Hover,

        /// <summary>
        /// The editor + search-type selector are visible and focused. Used in both placement
        /// modes during user input.
        /// </summary>
        Editing,

        /// <summary>
        /// User has finished editing and a filter is active. Header chrome shows the filter
        /// value in accent color where the search icon used to be; clicking it re-enters
        /// <see cref="Editing"/>. Header-mode only.
        /// </summary>
        FilteredIdle,
    }
}
