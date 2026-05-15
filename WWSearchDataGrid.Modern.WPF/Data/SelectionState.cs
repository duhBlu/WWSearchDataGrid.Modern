namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Selection state exposed on <see cref="GridCellData"/> for template authors that
    /// want to react to cell-level selection / focus. In filter-row context the cell is
    /// always <see cref="None"/>; cell-edit contexts (future) will populate the value.
    /// </summary>
    public enum SelectionState
    {
        None = 0,
        Selected = 1,
        Focused = 2,
    }
}
