using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Translates between the editor-time tree (<see cref="FilterGroupNode"/> /
    /// <see cref="FilterConditionNode"/>) and the per-column
    /// <see cref="SearchTemplateController.SearchGroups"/> structures the grid persists.
    /// </summary>
    internal static class FilterEditorTreeBuilder
    {
        /// <summary>
        /// Builds a grid-level tree from the per-column controllers using each non-first column's
        /// <see cref="SearchTemplateGroup.OperatorName"/> as the inter-column join operator
        /// (left-folded). Used by the FilterPanel operator-toggle path so an inter-column OR
        /// renders as a single OR group in both the editor and the FilterPanel chip strip,
        /// instead of getting interpreted as the inner combiner of one column's group.
        /// <para>
        /// Returns <c>null</c> when there are no active columns. Column-level OperatorName is
        /// deliberately treated as the inter-column join only; the intra-column combiner is
        /// derived from non-first template-level OperatorName values, mirroring
        /// <c>EvaluateGroupWithDisplayValues</c>.
        /// </para>
        /// </summary>
        public static FilterGroupNode SynthesizeFromColumnJoins(SearchDataGrid grid)
        {
            if (grid?.GridColumns == null) return null;

            var orderedActive = grid.GridColumns
                .Where(c => c?.SearchTemplateController?.HasCustomExpression == true)
                .OrderBy(c => c.InternalColumn?.DisplayIndex >= 0 ? c.InternalColumn.DisplayIndex : int.MaxValue)
                .ToList();

            if (orderedActive.Count == 0) return null;

            // Per-column subtrees paired with the operator joining each to the previous column.
            // The first column's joinOp is unused.
            var perColumn = new List<(FilterEditorNode subtree, LogicalOperator joinOp)>();
            foreach (var column in orderedActive)
            {
                var subtree = BuildSynthesizedColumnSubtree(column);
                if (subtree == null) continue;

                LogicalOperator joinOp = LogicalOperator.And;
                if (perColumn.Count > 0)
                {
                    var firstGroup = column.SearchTemplateController?.SearchGroups?.FirstOrDefault();
                    if (firstGroup != null)
                    {
                        joinOp = LogicalOperatorExtensions.Parse(firstGroup.OperatorName);
                    }
                }

                perColumn.Add((subtree, joinOp));
            }

            if (perColumn.Count == 0) return null;

            // Left-fold: A op1 B op2 C → ((A op1 B) op2 C). When the next op matches the
            // accumulator group's operator, extend in place to keep the tree flat.
            FilterEditorNode acc = perColumn[0].subtree;
            for (int i = 1; i < perColumn.Count; i++)
            {
                var (next, op) = perColumn[i];

                if (acc is FilterGroupNode g && g.Operator == op)
                {
                    g.Children.Add(next);
                }
                else
                {
                    var wrap = new FilterGroupNode { Operator = op };
                    wrap.Children.Add(acc);
                    wrap.Children.Add(next);
                    acc = wrap;
                }
            }

            FilterGroupNode root;
            if (acc is FilterGroupNode rootGroup)
            {
                root = rootGroup;
            }
            else
            {
                root = new FilterGroupNode { Operator = LogicalOperator.And };
                root.Children.Add(acc);
            }

            root.AvailableColumns = new ObservableCollection<GridColumn>(grid.GridColumns);
            return root;
        }

        /// <summary>
        /// Builds the editor-time subtree for a single column for the synthesize path. Walks every
        /// <see cref="SearchTemplateGroup"/> on the column. Intra-column combiner comes from
        /// template-level <see cref="SearchTemplate.OperatorName"/> values — the group-level
        /// OperatorName is reserved for the inter-column join (the synthesize entry point reads
        /// it once at the column boundary).
        /// </summary>
        private static FilterEditorNode BuildSynthesizedColumnSubtree(GridColumn column)
        {
            var controller = column?.SearchTemplateController;
            if (controller == null || !controller.HasCustomExpression) return null;

            var groupSubtrees = new List<FilterEditorNode>();
            foreach (var coreGroup in controller.SearchGroups)
            {
                var node = BuildSynthesizedSubtreeForGroup(coreGroup, column);
                if (node != null) groupSubtrees.Add(node);
            }

            if (groupSubtrees.Count == 0) return null;
            if (groupSubtrees.Count == 1) return groupSubtrees[0];

            var wrap = new FilterGroupNode { Operator = LogicalOperator.And };
            foreach (var sub in groupSubtrees) wrap.Children.Add(sub);
            return wrap;
        }

        /// <summary>
        /// Builds the editor-time subtree for one <see cref="SearchTemplateGroup"/> in the
        /// synthesize path. Mirrors <see cref="BuildEditorNodeFromGroup"/> but derives the inner
        /// combiner exclusively from non-first template OperatorName values, so the group-level
        /// OperatorName (which the FilterPanel toggle uses as the inter-column join) doesn't get
        /// misread as an intra-group operator.
        /// </summary>
        private static FilterEditorNode BuildSynthesizedSubtreeForGroup(SearchTemplateGroup coreGroup, GridColumn column)
        {
            var hasOrTemplate = coreGroup.SearchTemplates.Skip(1)
                .Any(t => t != null && string.Equals(t.OperatorName, "Or", StringComparison.OrdinalIgnoreCase));
            var innerOp = hasOrTemplate ? LogicalOperator.Or : LogicalOperator.And;

            var groupNode = new FilterGroupNode { Operator = innerOp };

            foreach (var template in coreGroup.SearchTemplates)
            {
                if (!template.HasCustomFilter || !template.IsValidFilter) continue;

                var conditionNode = new FilterConditionNode();
                conditionNode.AdoptTemplate(template, column);
                groupNode.Children.Add(conditionNode);
            }

            foreach (var childGroup in coreGroup.ChildGroups)
            {
                var child = BuildSynthesizedSubtreeForGroup(childGroup, column);
                if (child != null) groupNode.Children.Add(child);
            }

            if (groupNode.Children.Count == 0) return null;

            // Flatten when this group adds no semantics: single child + default And.
            if (groupNode.Children.Count == 1 && innerOp == LogicalOperator.And)
            {
                var only = groupNode.Children[0];
                groupNode.Children.Clear();
                return only;
            }

            return groupNode;
        }

        /// <summary>
        /// Open-time: returns the editor tree to display. If the grid already has a previously
        /// composed tree from an earlier Apply, reuses it (so cross-column groupings round-trip
        /// exactly). Otherwise walks each <see cref="GridColumn"/>'s controller and builds a
        /// root <see cref="FilterGroupNode"/> containing one per-column subgroup for each
        /// existing top-level <see cref="SearchTemplateGroup"/>. Recurses into
        /// <see cref="SearchTemplateGroup.ChildGroups"/> so nested editor groups round-trip.
        /// </summary>
        public static FilterGroupNode BuildFromGrid(SearchDataGrid grid)
        {
            var availableColumns = new ObservableCollection<GridColumn>(grid?.GridColumns ?? Enumerable.Empty<GridColumn>());

            if (grid?.GridFilterTree != null)
            {
                // The grid already has the editor's composed tree from a prior Apply. Re-use it
                // directly so cross-column groupings (OR across columns, nested groups) survive
                // close-and-reopen without going through the lossy per-column slicing.
                var existing = grid.GridFilterTree;
                existing.AvailableColumns = availableColumns;

                // Edits made outside the editor (per-column popup, FilterPanel chip removals,
                // etc.) can leave redundant single-AND wrappers behind. Clean them up now so
                // the editor opens on a normalized tree instead of a stale AND > AND > {…} chain.
                FilterEditorNormalizer.Normalize(existing);
                return existing;
            }

            var root = new FilterGroupNode
            {
                Operator = LogicalOperator.And,
                AvailableColumns = availableColumns
            };

            if (grid?.GridColumns == null) return root;

            foreach (var column in grid.GridColumns)
            {
                var controller = column.SearchTemplateController;
                if (controller == null) continue;

                // Every column carries a default scaffold group with an empty SearchTemplate so
                // the per-column popup always has a row to show. Those don't represent a real
                // filter — skip the column entirely unless the controller flagged a custom
                // expression (the same signal the grid uses to decide whether to filter).
                if (!controller.HasCustomExpression) continue;

                foreach (var coreGroup in controller.SearchGroups)
                {
                    var node = BuildEditorNodeFromGroup(coreGroup, column);
                    if (node != null) root.Children.Add(node);
                }
            }

            // The build above can yield redundant single-AND wrappers when a column has one
            // multi-template SearchTemplateGroup (it becomes root[innerAND[t1, t2]]). Apply
            // the same normalization the live-editing path uses so the editor opens flat.
            FilterEditorNormalizer.Normalize(root);
            return root;
        }

        /// <summary>
        /// Apply-time: stores the composed editor tree on the grid (so the unified evaluator can
        /// honor cross-column groupings) and replaces each touched column's
        /// <see cref="SearchTemplateController.SearchGroups"/> with the slice derived from the
        /// tree (so the per-column popup and FilterPanel chip strip still reflect what's
        /// active). Triggers <see cref="SearchTemplateController.UpdateFilterExpression(bool)"/>
        /// on every touched column.
        /// </summary>
        public static void WriteBackToGrid(FilterGroupNode root, SearchDataGrid grid)
        {
            if (root == null || grid?.GridColumns == null) return;

            // Store the tree as the authoritative filter source. The grid's FilterItemsSource
            // branches on this — when present, it compiles the tree into a row predicate rather
            // than AND-joining per-column predicates.
            grid.GridFilterTree = HasAnyConditions(root) ? root : null;

            var touched = new HashSet<GridColumn>();
            CollectTouchedColumns(root, touched);

            foreach (var column in touched)
            {
                var controller = column.SearchTemplateController;
                if (controller == null) continue;

                controller.SearchGroups.Clear();

                foreach (FilterEditorNode child in root.Children)
                {
                    var slice = BuildColumnSlice(child, column);
                    if (slice != null) controller.SearchGroups.Add(slice);
                }

                // The controller's HasCustomExpression flag is recomputed inside the builder via
                // UpdateFilterExpression; calling it once per touched column is enough.
                controller.UpdateFilterExpression();
            }

            // Columns that previously had filters but are no longer referenced must be reset so
            // their old predicates don't linger.
            foreach (var column in grid.GridColumns)
            {
                if (touched.Contains(column)) continue;
                var controller = column.SearchTemplateController;
                if (controller == null) continue;
                if (controller.SearchGroups.Count == 0) continue;

                controller.ClearAndReset();
            }
        }

        private static bool HasAnyConditions(FilterEditorNode node)
        {
            switch (node)
            {
                case FilterConditionNode _: return true;
                case FilterGroupNode g:
                    foreach (var child in g.Children)
                        if (HasAnyConditions(child)) return true;
                    return false;
                default: return false;
            }
        }

        /// <summary>
        /// Builds an editor-time node for a single <see cref="SearchTemplateGroup"/>. Returns
        /// either a wrapping <see cref="FilterGroupNode"/> (when the group is non-trivial) or the
        /// child <see cref="FilterConditionNode"/> directly (when the group is a single-condition
        /// scaffold with the default And operator). The flattening keeps single-column,
        /// single-rule filters from rendering as root → group → condition triple-nesting.
        /// </summary>
        private static FilterEditorNode BuildEditorNodeFromGroup(SearchTemplateGroup coreGroup, GridColumn column)
        {
            var op = DeriveEditorOperator(coreGroup);
            var groupNode = new FilterGroupNode { Operator = op };

            foreach (var template in coreGroup.SearchTemplates)
            {
                // The default scaffold template (SearchType=Contains, no value) shouldn't surface
                // as a "[Field] contains (empty)" row in the editor. HasCustomFilter is the same
                // signal SearchTemplateController.HasCustomExpression uses to decide whether a
                // template is meaningful.
                if (!template.HasCustomFilter || !template.IsValidFilter) continue;

                var conditionNode = new FilterConditionNode();
                conditionNode.AdoptTemplate(template, column);
                groupNode.Children.Add(conditionNode);
            }

            foreach (var childGroup in coreGroup.ChildGroups)
            {
                var child = BuildEditorNodeFromGroup(childGroup, column);
                if (child != null) groupNode.Children.Add(child);
            }

            // Drop empty groups — they would round-trip as no-ops.
            if (groupNode.Children.Count == 0) return null;

            // Flatten when the group adds no semantics: single child + default And operator. The
            // child's parent will be reassigned to the root group by the caller's
            // Children.CollectionChanged handler.
            if (groupNode.Children.Count == 1 && op == LogicalOperator.And)
            {
                var only = groupNode.Children[0];
                groupNode.Children.Clear();
                return only;
            }

            return groupNode;
        }

        /// <summary>
        /// Resolves the editor-time operator for a persisted <see cref="SearchTemplateGroup"/>.
        /// Prefers the group's own <see cref="SearchTemplateGroup.OperatorName"/> when it carries
        /// editor semantics (Or, NotAnd, NotOr). Falls back to inspecting non-first
        /// <see cref="SearchTemplate.OperatorName"/> values for legacy data that only stamped the
        /// inner combiner on the per-template join op.
        /// </summary>
        private static LogicalOperator DeriveEditorOperator(SearchTemplateGroup coreGroup)
        {
            var groupOp = LogicalOperatorExtensions.Parse(coreGroup.OperatorName);
            if (groupOp != LogicalOperator.And)
            {
                return groupOp;
            }

            var nonFirst = coreGroup.SearchTemplates.Skip(1).FirstOrDefault(t => t != null);
            if (nonFirst != null && string.Equals(nonFirst.OperatorName, "Or", StringComparison.OrdinalIgnoreCase))
            {
                return LogicalOperator.Or;
            }
            return LogicalOperator.And;
        }

        private static void CollectTouchedColumns(FilterEditorNode node, HashSet<GridColumn> sink)
        {
            switch (node)
            {
                case FilterConditionNode condition when condition.Column != null:
                    sink.Add(condition.Column);
                    break;
                case FilterGroupNode group:
                    foreach (var child in group.Children)
                        CollectTouchedColumns(child, sink);
                    break;
            }
        }

        /// <summary>
        /// Projects a single editor node into a <see cref="SearchTemplateGroup"/> tree containing
        /// only the conditions targeting <paramref name="column"/>. Returns <c>null</c> when no
        /// conditions for this column live anywhere in the subtree.
        /// </summary>
        private static SearchTemplateGroup BuildColumnSlice(FilterEditorNode node, GridColumn column)
        {
            if (node is FilterConditionNode condition)
            {
                if (condition.Column != column) return null;
                if (condition.SearchTemplate == null) return null;

                var wrapper = new SearchTemplateGroup
                {
                    OperatorName = "And"
                };
                wrapper.SearchTemplates.Add(condition.SearchTemplate);
                return wrapper;
            }

            if (node is FilterGroupNode group)
            {
                var slice = new SearchTemplateGroup
                {
                    OperatorName = group.Operator.ToTokenString()
                };

                // The inner combiner (And vs Or) gets stamped onto each non-leading template's
                // OperatorName so the existing FilterPanel chip strip, SearchDataGridFiltering
                // evaluator, and FilterExpressionBuilder all read the same intent. Negation lives
                // on the group's own OperatorName (set via group.Operator.ToTokenString() above).
                bool isOrCombiner = group.Operator == LogicalOperator.Or || group.Operator == LogicalOperator.NotOr;
                string innerCombinerToken = isOrCombiner ? "Or" : "And";

                foreach (var child in group.Children)
                {
                    if (child is FilterConditionNode c)
                    {
                        if (c.Column != column) continue;
                        if (c.SearchTemplate == null) continue;
                        slice.SearchTemplates.Add(c.SearchTemplate);
                    }
                    else if (child is FilterGroupNode g)
                    {
                        var childSlice = BuildColumnSlice(g, column);
                        if (childSlice != null) slice.ChildGroups.Add(childSlice);
                    }
                }

                // Per-template operators (skip the leading one; it's not used as a join).
                for (int i = 1; i < slice.SearchTemplates.Count; i++)
                {
                    slice.SearchTemplates[i].OperatorName = innerCombinerToken;
                }

                if (slice.SearchTemplates.Count == 0 && slice.ChildGroups.Count == 0) return null;
                return slice;
            }

            return null;
        }
    }
}
