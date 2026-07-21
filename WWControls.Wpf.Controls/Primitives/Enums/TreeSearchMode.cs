namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// How a <see cref="WWTreeView"/> reacts to its <see cref="WWTreeView.FilterText"/>.
    /// </summary>
    public enum TreeSearchMode
    {
        /// <summary>No searching. FilterText is ignored.</summary>
        Off,

        /// <summary>
        /// Every node stays visible; matches are counted and can be cycled with the match navigation
        /// commands. Nothing is hidden — the Component-Explorer style.
        /// </summary>
        Highlight,

        /// <summary>
        /// Non-matching nodes are hidden (their ancestors are kept so matches stay reachable). Requires
        /// nodes that expose a filtered <see cref="IWWFilterableTreeNode.ChildrenView"/> and an item
        /// template bound to it.
        /// </summary>
        Filter
    }
}
