namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// How a <see cref="WWTreeView"/> handles selecting multiple items.
    /// </summary>
    public enum TreeSelectionMode
    {
        /// <summary>Native single selection (default). One item selected at a time.</summary>
        Single,

        /// <summary>
        /// Explorer-style: a plain click replaces the selection, Ctrl+click toggles a single item,
        /// Shift+click selects the range from the anchor to the clicked item.
        /// </summary>
        Extended,

        /// <summary>
        /// Each plain click toggles the clicked item in or out of the selection (no modifier needed);
        /// Shift+click still selects a range.
        /// </summary>
        Multiple
    }
}
