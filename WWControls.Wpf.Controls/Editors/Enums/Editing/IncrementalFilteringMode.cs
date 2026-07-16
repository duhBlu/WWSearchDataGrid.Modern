namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// How <see cref="WWComboBox"/> matches typed text against item display text when
    /// incremental filtering is active.
    /// </summary>
    public enum IncrementalFilteringMode
    {
        /// <summary>Item display text must start with the typed text.</summary>
        StartsWith,

        /// <summary>Item display text must contain the typed text anywhere.</summary>
        Contains,

        /// <summary>Item display text must end with the typed text.</summary>
        EndsWith,

        /// <summary>
        /// Contains-style matching with ranked ordering: exact matches first, then prefix
        /// matches, then substring matches ordered by how early the match occurs. Ordering
        /// requires the items view to be a <c>ListCollectionView</c> (any IList-backed
        /// ItemsSource); other views filter without reordering.
        /// </summary>
        Smart
    }
}
