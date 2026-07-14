namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// Specifies when the expand-all / collapse-all buttons appear on a <see cref="WWTreeViewItem"/>.
    /// </summary>
    public enum ExpandCollapseButtonVisibility
    {
        /// <summary>Never show expand/collapse buttons.</summary>
        None,

        /// <summary>Show on all items that have children.</summary>
        All,

        /// <summary>Show only on root-level items that have children.</summary>
        RootsOnly,

        /// <summary>Show only on items that have grandchildren (children with children).</summary>
        HasGrandchildren
    }
}
