namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Specifies which user gestures activate a <see cref="WWTreeViewItem"/> — raising
    /// <see cref="WWTreeView.ItemActivated"/> and executing <see cref="WWTreeView.ItemActivatedCommand"/>
    /// with the item's data context.
    /// </summary>
    public enum TreeItemActivationTrigger
    {
        /// <summary>No gesture activates an item.</summary>
        None,

        /// <summary>Double-clicking an item activates it.</summary>
        DoubleClick,

        /// <summary>Pressing Enter on the focused item activates it.</summary>
        Enter,

        /// <summary>Either a double-click or Enter activates an item.</summary>
        DoubleClickOrEnter
    }
}
