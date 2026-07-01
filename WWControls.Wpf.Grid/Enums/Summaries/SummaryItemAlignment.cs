namespace WWControls.Wpf
{
    /// <summary>
    /// Which side a summary entry renders on inside a horizontal summary run — the group
    /// header's summary area and the fixed total summary panel. Column-aligned total summary
    /// cells ignore it (they stack entries vertically under their column).
    /// </summary>
    public enum SummaryItemAlignment
    {
        /// <summary>Rendered in the left-side run, inline after the group header content.</summary>
        Left,

        /// <summary>Rendered right-aligned at the row's right edge (the default).</summary>
        Right
    }
}
