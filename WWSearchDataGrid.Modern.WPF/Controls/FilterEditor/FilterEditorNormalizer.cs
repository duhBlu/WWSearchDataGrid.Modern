using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Editor-time tree cleanup invoked after user-driven removals. Mirrors the build-time
    /// rules in <see cref="FilterEditorTreeBuilder.BuildEditorNodeFromGroup"/> so the live
    /// tree stays free of empty groups and redundant single-child wrappers as the user
    /// edits.
    /// </summary>
    internal static class FilterEditorNormalizer
    {
        /// <summary>
        /// Full-tree cleanup used when a tree first lands in the editor (open-time) or after
        /// edits made outside the editor have left redundancy behind. Walks the tree in
        /// post-order so the deepest groups are normalized first; each visit applies the same
        /// rules as <see cref="NormalizeAfterRemoval"/>, and the cascade naturally bubbles
        /// collapses up to the root.
        /// </summary>
        public static void Normalize(FilterGroupNode root)
        {
            if (root == null) return;

            var groupsBottomUp = new System.Collections.Generic.List<FilterGroupNode>();
            CollectGroupsPostOrder(root, groupsBottomUp);

            foreach (var group in groupsBottomUp)
            {
                // An earlier iteration's cascade may have detached this node from the tree.
                if (group != root && group.Parent == null) continue;
                NormalizeAfterRemoval(group);
            }
        }

        private static void CollectGroupsPostOrder(FilterGroupNode node, System.Collections.Generic.List<FilterGroupNode> sink)
        {
            // Snapshot child groups before recursing — collapses inside the recursion can
            // mutate Children, which would otherwise invalidate a live enumerator.
            var childGroups = node.Children.OfType<FilterGroupNode>().ToList();
            foreach (var child in childGroups)
            {
                CollectGroupsPostOrder(child, sink);
            }
            sink.Add(node);
        }

        /// <summary>
        /// Walks up from <paramref name="startFrom"/>, collapsing redundant nesting introduced
        /// when a child was removed. At the root, absorbs a sole surviving group child so the
        /// editor doesn't render a stray <c>AND &gt; AND &gt; {templates}</c> chain after the
        /// last sibling subgroup is removed.
        /// </summary>
        public static void NormalizeAfterRemoval(FilterGroupNode startFrom)
        {
            var current = startFrom;
            while (current != null)
            {
                var parent = current.Parent;

                if (parent == null)
                {
                    // Root: structural, can't be removed or unwrapped. If it's left with a
                    // single group child, that wrapper adds no semantic — with one child
                    // root's own operator is irrelevant to combination, so adopting the
                    // child's operator and pulling up its children is semantically safe.
                    // Limited to plain root operators (And/Or); negated roots carry an
                    // explicit negation contract that must be preserved.
                    if (current.Children.Count == 1 &&
                        current.Children[0] is FilterGroupNode onlyGroup &&
                        (current.Operator == LogicalOperator.And || current.Operator == LogicalOperator.Or))
                    {
                        current.Operator = onlyGroup.Operator;
                        current.Children.RemoveAt(0);

                        var moving = onlyGroup.Children.ToList();
                        onlyGroup.Children.Clear();
                        foreach (var node in moving)
                        {
                            current.Children.Add(node);
                        }

                        continue;
                    }
                    return;
                }

                if (current.Children.Count == 0)
                {
                    parent.Children.Remove(current);
                    current = parent;
                    continue;
                }

                if (current.Children.Count == 1 && current.Operator == LogicalOperator.And)
                {
                    var only = current.Children[0];
                    int index = parent.Children.IndexOf(current);

                    current.Children.RemoveAt(0);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, only);

                    current = parent;
                    continue;
                }

                return;
            }
        }
    }
}
