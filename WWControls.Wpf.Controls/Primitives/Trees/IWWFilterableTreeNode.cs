using System.ComponentModel;
using WWControls.Core;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// A tree node that participates in <see cref="WWTreeView"/> searching and filtering. The tree's
    /// filter pass owns traversal and writes the state flags; implementations only supply
    /// <see cref="MatchesSelf"/> (self-only, no recursion) and a <see cref="ChildrenView"/> gated on
    /// <see cref="IWWTreeNode.IsExpanded"/>-independent <see cref="IsVisibleInFilter"/>.
    /// Derive from <see cref="WWTreeNodeBase{T}"/> for a ready-made implementation.
    /// </summary>
    public interface IWWFilterableTreeNode : IWWTreeNode
    {
        /// <summary>Set by the filter pass: whether this node is shown in <see cref="TreeSearchMode.Filter"/>.</summary>
        bool IsVisibleInFilter { get; set; }

        /// <summary>Set by the filter pass: true when a descendant matches (drives auto-expand).</summary>
        bool HasMatchingDescendant { get; set; }

        /// <summary>Set during match navigation: the one match currently focused (drives the accent).</summary>
        bool IsCurrentSearchMatch { get; set; }

        /// <summary>
        /// Filtered view over the children, gated on each child's <see cref="IsVisibleInFilter"/>. The
        /// item template's <c>ItemsSource</c> binds to this so filtering hides items without touching
        /// the underlying collection.
        /// </summary>
        ICollectionView ChildrenView { get; }

        /// <summary>Self-only match test. The engine owns traversal; do not recurse here.</summary>
        bool MatchesSelf(SearchQuery query);

        /// <summary>Re-applies the <see cref="ChildrenView"/> filter — only if the view has been realized.</summary>
        void RefreshChildrenView();
    }
}
