namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Controls when the per-cell clear (X) button appears in the filter row. Resolved
    /// per cell by <see cref="WWSearchDataGrid.Modern.WPF.Converters.ClearButtonModeToVisibilityConverter"/>,
    /// which combines this policy with <see cref="ColumnFilterControl.HasEditorInputValue"/>
    /// and <see cref="ColumnFilterControl.IsFilterCellEditing"/> — the filter cell's
    /// display / edit state. Display = focus is outside the control (read-only TextBlock
    /// surface visible); Edit = focus is inside the control (editor surface visible).
    /// Filters committed via the rule-filter popup (or set programmatically) do not light
    /// up the button — those aren't clearable from the X — so visibility is scoped to
    /// editor-only signals.
    /// </summary>
    public enum FilterClearButtonMode
    {
        /// <summary>
        /// Clear button is never shown. Useful when consumers wire their own clear UI via
        /// <see cref="ColumnFilterControl.ClearSearchTextCommand"/>.
        /// </summary>
        Never = 0,

        /// <summary>
        /// Clear button is shown whenever the editor holds a clearable value, regardless of
        /// which surface (display or edit) is rendering.
        /// </summary>
        Always = 1,

        /// <summary>
        /// Clear button is shown only when the editor holds a clearable value and is
        /// rendering the read-only display surface (focus is outside the control).
        /// </summary>
        Display = 2,

        /// <summary>
        /// Clear button is shown only when the editor holds a clearable value and is
        /// rendering the edit surface (focus is inside the control).
        /// </summary>
        Edit = 3,
    }
}
