namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Specifies the default search mode for simple textbox searches in column filters.
    /// This enum provides a safe subset of SearchType values appropriate for temporary search templates.
    /// </summary>
    public enum DefaultSearchMode
    {
        /// <summary>
        /// Finds matches anywhere in the value (default behavior).
        /// Best for general text search scenarios.
        /// </summary>
        Contains = 0,

        /// <summary>
        /// Finds matches that start with the search text.
        /// Best for ID columns, part numbers, or customer codes where users know the prefix.
        /// </summary>
        StartsWith = 1,

        /// <summary>
        /// Finds matches that end with the search text.
        /// Best for file extensions, domain suffixes, or similar patterns.
        /// </summary>
        EndsWith = 2,

        /// <summary>
        /// Finds exact matches only.
        /// Best for status codes, enum values, or scenarios requiring exact matches.
        /// </summary>
        Equals = 3
    }
}
