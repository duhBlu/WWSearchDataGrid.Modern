using System.Threading.Tasks;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// A tree node whose children are loaded on demand. The tree shows an expander when
    /// <see cref="HasChildren"/> is true even before any children exist, and calls
    /// <see cref="LoadChildrenAsync"/> once on first expand — the node populates its own
    /// <see cref="IWWTreeNode.Children"/>. No dummy placeholder rows are injected.
    /// </summary>
    public interface IWWLazyTreeNode : IWWTreeNode
    {
        /// <summary>
        /// Whether this node can have children — known before they are loaded. Drives the expander glyph.
        /// </summary>
        bool HasChildren { get; }

        /// <summary>Set by the tree around <see cref="LoadChildrenAsync"/> so a loading indicator can show.</summary>
        bool IsLoading { get; set; }

        /// <summary>
        /// Loads the children into <see cref="IWWTreeNode.Children"/>. Called at most once by the tree, on
        /// first expand. If it throws, the load is allowed to retry on the next expand.
        /// </summary>
        Task LoadChildrenAsync();
    }
}
