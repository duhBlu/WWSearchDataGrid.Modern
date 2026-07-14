namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// How <see cref="WWComboBox"/> popup items select.
    /// </summary>
    public enum ComboBoxSelectionMode
    {
        /// <summary>Standard single selection — clicking an item selects it and closes the popup.</summary>
        Single,

        /// <summary>
        /// Each item shows a checkbox and clicking toggles membership in
        /// <see cref="WWComboBox.SelectedItems"/> without closing the popup. The selection box
        /// shows the checked items' display texts joined by
        /// <see cref="WWComboBox.MultiSelectSeparator"/>.
        /// </summary>
        Checkbox,

        /// <summary>
        /// Single selection with a radio glyph on each item reflecting the current selection.
        /// </summary>
        Radio
    }
}
