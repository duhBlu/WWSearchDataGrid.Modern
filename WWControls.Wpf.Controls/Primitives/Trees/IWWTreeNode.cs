using System.Collections;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Optional contract a <see cref="WWTreeView"/> bound item implements so the control can read its
    /// child collection and expansion/selection state directly, without reflecting over property names.
    /// Nodes that do not implement it still work — the control falls back to container inspection.
    /// </summary>
    public interface IWWTreeNode
    {
        /// <summary>The node's child nodes, or <see langword="null"/>/empty for a leaf.</summary>
        IEnumerable Children { get; }

        /// <summary>Whether the node is expanded. Kept in sync with the generated container.</summary>
        bool IsExpanded { get; set; }

        /// <summary>Whether the node is selected.</summary>
        bool IsSelected { get; set; }
    }
}
