namespace WWControls.Wpf
{
    /// <summary>
    /// Gates when a <see cref="SearchDataGrid"/> row promotes from ordinary single-cell editing into
    /// full-row ("edit entire row") edit mode — every cell becomes an editor at once behind a dimming
    /// overlay with a row-scoped Update / Cancel action bar, and the row stays open until the user
    /// commits or cancels.
    /// </summary>
    public enum RowEditTrigger
    {
        /// <summary>
        /// Full-row edit mode is disabled — cells edit one at a time, the stock DataGrid behaviour.
        /// This is the default.
        /// </summary>
        Never,

        /// <summary>
        /// The row promotes the instant any cell editor opens (a cell enters edit mode by click,
        /// F2, or typing). The overlay appears immediately, before the user changes anything.
        /// </summary>
        OnCellEditorOpen,

        /// <summary>
        /// The row stays in normal single-cell editing until the open editor's value actually
        /// changes; the first change promotes the whole row. Lets the user tab through cells without
        /// summoning the modal overlay unless they edit something.
        /// </summary>
        OnCellValueChange,
    }
}
