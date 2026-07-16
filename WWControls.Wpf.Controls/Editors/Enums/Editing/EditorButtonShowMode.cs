namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Controls when an editor's decoration buttons (ComboBox toggle, spinner up/down,
    /// DatePicker calendar dropdown) are visible. Independent of edit-mode entry — these
    /// buttons can render in the display template too.
    /// </summary>
    public enum EditorButtonShowMode
    {
        /// <summary>
        /// Sentinel meaning "fall through". On <see cref="BaseEditorSettings.EditorButtonShowMode"/>
        /// this inherits the value from <see cref="SearchDataGrid.EditorButtonShowMode"/>; on the
        /// grid itself it equals <see cref="ShowOnlyInEditor"/>.
        /// </summary>
        Default = 0,

        /// <summary>Buttons render only while the cell is in edit mode.</summary>
        ShowOnlyInEditor,

        /// <summary>Buttons render whenever the cell has keyboard focus, edit mode or not.</summary>
        ShowForFocusedCell,

        /// <summary>
        /// Buttons render on every cell of the currently-selected row. Useful for record-style
        /// grids where the user expects to see editor affordances for the whole record at once.
        /// </summary>
        ShowForFocusedRow,

        /// <summary>Buttons render on every cell at all times.</summary>
        ShowAlways,
    }
}
