namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// How the column-filter popup compares user-selected values against row data. Surfaced
    /// as <see cref="ColumnDataBase.ColumnFilterMode"/>. The popup UI itself is deferred — these
    /// enum values describe intended semantics when it ships.
    /// </summary>
    public enum ColumnFilterMode
    {
        /// <summary>
        /// Compare against the rendered display text (post-converter / format). Default.
        /// Matches Excel-style "what the user sees is what they filter by."
        /// </summary>
        DisplayText = 0,

        /// <summary>
        /// Compare against the raw underlying field value. Use when the displayed text
        /// loses information (e.g. a status enum displayed via a converter to a friendly
        /// label but you want filtering to match the enum identity).
        /// </summary>
        Value = 1
    }
}
