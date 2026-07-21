using System.Collections;
using System.Collections.Generic;
using WWControls.Core;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// The outcome of a single filter pass: the ordered match list (visual/pre-order, for cycling) and a
    /// child → parent map (so match navigation can expand a match's ancestors without the nodes exposing a
    /// Parent).
    /// </summary>
    internal sealed class TreeFilterResult
    {
        public List<IWWFilterableTreeNode> Matches { get; } = new List<IWWFilterableTreeNode>();
        public Dictionary<object, object> ParentMap { get; } = new Dictionary<object, object>();
    }

    /// <summary>
    /// One synchronous post-order pass over a tree of <see cref="IWWFilterableTreeNode"/>. Writes each
    /// node's <see cref="IWWFilterableTreeNode.IsVisibleInFilter"/> and
    /// <see cref="IWWFilterableTreeNode.HasMatchingDescendant"/>, refreshes realized child views, and
    /// returns the matches and parent map. Node text matching and traversal live here; the control owns
    /// debouncing and the visual/expansion side effects.
    /// </summary>
    internal static class TreeFilterEngine
    {
        public static TreeFilterResult Run(IEnumerable roots, SearchQuery query, TreeSearchMode mode, bool keepMatchedSubtree)
        {
            var result = new TreeFilterResult();
            if (roots == null)
                return result;

            foreach (var root in roots)
            {
                if (root is IWWFilterableTreeNode node)
                    Visit(node, query, mode, keepMatchedSubtree, false, result);
            }

            return result;
        }

        private static bool Visit(
            IWWFilterableTreeNode node,
            SearchQuery query,
            TreeSearchMode mode,
            bool keepMatchedSubtree,
            bool ancestorMatched,
            TreeFilterResult result)
        {
            bool isMatch = !query.IsEmpty && node.MatchesSelf(query);
            if (isMatch)
                result.Matches.Add(node); // pre-order: matches list follows visual top-to-bottom order

            bool childAncestorMatched = ancestorMatched || isMatch;
            bool descendantMatch = false;

            foreach (var childObj in node.Children)
            {
                if (childObj is IWWFilterableTreeNode child)
                {
                    result.ParentMap[child] = node;
                    if (Visit(child, query, mode, keepMatchedSubtree, childAncestorMatched, result))
                        descendantMatch = true;
                }
            }

            node.HasMatchingDescendant = descendantMatch;

            if (mode == TreeSearchMode.Filter && !query.IsEmpty)
            {
                node.IsVisibleInFilter = isMatch || descendantMatch || (keepMatchedSubtree && ancestorMatched);

                // Reveal the path to matches. IsExpanded is two-way bound to the container for
                // IWWTreeNode, so setting it here expands the realized branch.
                if (descendantMatch)
                    node.IsExpanded = true;
            }
            else
            {
                node.IsVisibleInFilter = true;
            }

            // Post-order: children carry their flags before the parent view re-evaluates them.
            node.RefreshChildrenView();

            return isMatch || descendantMatch;
        }
    }
}
