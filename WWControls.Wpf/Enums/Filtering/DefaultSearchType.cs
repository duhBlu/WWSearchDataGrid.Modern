namespace WWControls.Wpf
{
    /// <summary>
    /// Default search type for the auto-filter row's per-column quick search.
    /// </summary>
    public enum DefaultSearchType
    {
        /// <summary>
        /// Finds matches anywhere in the value.
        /// </summary>
        Contains = 0,

        /// <summary>
        /// Finds matches that start with the search text (the default for string columns).
        /// Spec synonym: <c>BeginsWith</c>.
        /// </summary>
        StartsWith = 1,

        /// <summary>
        /// Finds matches that end with the search text.
        /// </summary>
        EndsWith = 2,

        /// <summary>
        /// Finds exact matches only.
        /// </summary>
        Equals = 3
    }
}
