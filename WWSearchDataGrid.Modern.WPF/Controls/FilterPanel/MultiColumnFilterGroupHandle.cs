using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Sentinel placed on <see cref="WWSearchDataGrid.Modern.Core.ColumnFilterInfo.FilterData"/>
    /// when the FilterPanel chip represents an editor-tree group that spans more than one column.
    /// <para>
    /// Two modes:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Whole-tree clear</b> (no subtrees passed) — clears every touched
    ///   column's filter state and invalidates the grid-level tree. Used by the legacy
    ///   single-bracket unified rendering and by the Or-root case where the whole tree shares
    ///   fate.</description></item>
    /// <item><description><b>Subtree removal</b> (one or more nodes passed) — removes only the
    ///   referenced nodes from their parents, then rewrites per-column slices via
    ///   <see cref="FilterEditorTreeBuilder.WriteBackToGrid"/> so the editor and chip strip stay
    ///   in sync with what's left. Used by the And-root multi-bracket layout so each bracket's
    ///   × removes only its own contribution.</description></item>
    /// </list>
    /// </summary>
    internal sealed class MultiColumnFilterGroupHandle
    {
        private readonly SearchDataGrid _grid;
        private readonly List<GridColumn> _touchedColumns;
        private readonly List<FilterEditorNode> _subtreesToRemove;

        /// <summary>Whole-tree clear: reset every touched column and drop the grid tree.</summary>
        public MultiColumnFilterGroupHandle(SearchDataGrid grid, IEnumerable<GridColumn> touchedColumns)
            : this(grid, touchedColumns, (IEnumerable<FilterEditorNode>)null)
        {
        }

        /// <summary>
        /// Subtree removal: detach <paramref name="subtreesToRemove"/> from their parents in the
        /// grid tree on <see cref="ClearAll"/>. <paramref name="touchedColumns"/> is used only as
        /// a fallback when the detachment empties the tree entirely.
        /// </summary>
        public MultiColumnFilterGroupHandle(SearchDataGrid grid, IEnumerable<GridColumn> touchedColumns, IEnumerable<FilterEditorNode> subtreesToRemove)
        {
            _grid = grid;
            _touchedColumns = touchedColumns?.Distinct().ToList() ?? new List<GridColumn>();
            _subtreesToRemove = subtreesToRemove?.Where(n => n != null).ToList();
        }

        /// <summary>
        /// Columns whose <see cref="SearchTemplateController"/> contributed conditions to the
        /// multi-column editor group.
        /// </summary>
        public IReadOnlyList<GridColumn> TouchedColumns => _touchedColumns;

        /// <summary>
        /// <c>true</c> when this handle was constructed for subtree removal; <c>false</c> for the
        /// whole-tree clear mode. Used by <c>OnFilterRemoved</c> to decide whether the grid tree
        /// survives the operation.
        /// </summary>
        public bool RemovesSubtreeOnly => _subtreesToRemove != null && _subtreesToRemove.Count > 0;

        /// <summary>
        /// Either detaches the configured subtrees from the grid tree (and rewrites the per-column
        /// slices so they stay in sync), or — when no subtrees were configured, or detachment
        /// empties the tree — falls back to the whole-tree clear path that resets every touched
        /// column and nulls the grid tree.
        /// </summary>
        public void ClearAll()
        {
            if (RemovesSubtreeOnly && _grid?.GridFilterTree != null)
            {
                var parentsToNormalize = new HashSet<FilterGroupNode>();
                foreach (var node in _subtreesToRemove)
                {
                    var parent = node.Parent;
                    if (parent == null) continue;
                    parent.Children.Remove(node);
                    parentsToNormalize.Add(parent);
                }

                // Mirror the per-node RemoveCommand path: collapse redundant single-child
                // wrappers and empty groups left behind so the editor tree (and the chip
                // strip rendered from it) doesn't end up showing AND > AND > {templates}
                // when a sibling subtree is removed via the chip strip.
                foreach (var parent in parentsToNormalize)
                {
                    FilterEditorNormalizer.NormalizeAfterRemoval(parent);
                }

                var tree = _grid.GridFilterTree;
                if (tree.Children.Count == 0)
                {
                    // Detachment emptied the tree — fall through to whole-tree clear.
                    foreach (var column in _touchedColumns)
                    {
                        column?.SearchTemplateController?.ClearAndReset();
                    }
                    _grid.InvalidateGridFilterTree();
                    return;
                }

                // Tree still has structure — rewrite per-column slices from what's left so the
                // per-column popup and chip strip see the new state. WriteBackToGrid clears
                // controllers no longer referenced by the tree.
                FilterEditorTreeBuilder.WriteBackToGrid(tree, _grid);
                return;
            }

            // Whole-tree clear path.
            foreach (var column in _touchedColumns)
            {
                column?.SearchTemplateController?.ClearAndReset();
            }
            _grid?.InvalidateGridFilterTree();
        }
    }
}
