namespace WWControls.Wpf
{
    /// <summary>
    /// Controls where the per-column filter editors are placed on a <see cref="SearchDataGrid"/>.
    /// </summary>
    public enum FilterEditorPlacement
    {
        /// <summary>
        /// Filter editors live in a dedicated row pinned beneath the column headers.
        /// The row scrolls horizontally with the columns but stays fixed vertically.
        /// </summary>
        Row,

        /// <summary>
        /// Filter editors live inside each column header and expand in-place on demand.
        /// A search icon appears in the header (visible on hover or when a filter is active)
        /// and clicking it swaps the header text for the filter editor.
        /// </summary>
        InHeader
    }
}
