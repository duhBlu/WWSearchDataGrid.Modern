namespace WWControls.Wpf
{
    /// <summary>
    /// Controls when a click on a cell triggers <see cref="System.Windows.Controls.DataGrid.BeginEdit()"/>.
    /// </summary>
    public enum EditorShowMode
    {
        /// <summary>
        /// Sentinel meaning "fall through". On <see cref="BaseEditorSettings.EditorShowMode"/> this
        /// inherits the value from <see cref="SearchDataGrid.EditorShowMode"/>; on the grid itself
        /// it equals <see cref="None"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Click-to-edit is disabled. Cells focus on click but never auto-enter edit mode — the
        /// user can still invoke editing explicitly via Enter / F2. Use on a column's
        /// <see cref="BaseEditorSettings.EditorShowMode"/> to opt that one column out when the
        /// grid-wide policy is otherwise.
        /// </summary>
        None,

        /// <summary>Mouse-down on a cell enters edit mode immediately.</summary>
        MouseDown,

        /// <summary>
        /// Mouse-down enters edit mode only if the cell was already focused at the start of the
        /// gesture. First click focuses; second click edits. Useful when the user wants to
        /// commit-on-blur without accidentally entering edit on every cell click.
        /// </summary>
        MouseDownFocused,

        /// <summary>
        /// Mouse-up on a cell enters edit mode. Lets the user drag-select cells without entering
        /// edit. Edit only starts when the gesture completes inside the same cell.
        /// </summary>
        MouseUp,

        /// <summary>Mouse-up enters edit mode only if the cell was already focused before the gesture.</summary>
        MouseUpFocused,
    }
}
