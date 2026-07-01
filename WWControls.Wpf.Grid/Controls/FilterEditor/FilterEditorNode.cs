using System;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Base type for nodes in the Filter Editor's editor-time tree. The tree is built when the
    /// editor opens, mutated in-memory, and written back to each column's
    /// <see cref="SearchTemplateController.SearchGroups"/> on Apply. Cancel discards the tree
    /// without touching per-column state.
    /// </summary>
    public abstract class FilterEditorNode : ObservableObject
    {
        private FilterGroupNode parent;

        /// <summary>
        /// Stable identifier for this node, used by selectors and key-based comparisons.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// The containing group node, or <c>null</c> for the root.
        /// </summary>
        public FilterGroupNode Parent
        {
            get => parent;
            internal set
            {
                if (SetProperty(value, ref parent))
                {
                    OnPropertyChanged(nameof(Depth));
                }
            }
        }

        /// <summary>
        /// Distance from the root (root = 0). Used by templates to indent nested groups.
        /// </summary>
        public int Depth => Parent == null ? 0 : Parent.Depth + 1;
    }
}
