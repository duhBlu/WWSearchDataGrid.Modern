using System;
using System.Collections.Generic;
using System.Diagnostics;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Compiles a Filter Editor view-model tree (<see cref="FilterGroupNode"/>) into a single
    /// row-level predicate so cross-column group operators (e.g. an OR group spanning two
    /// different columns) evaluate correctly. Each leaf condition resolves its column's value
    /// from the row at evaluation time via the column's binding path; group operators recurse.
    /// </summary>
    /// <remarks>
    /// Scope of this MVP compiler:
    /// <list type="bullet">
    /// <item><description>Reuses each <see cref="SearchTemplate"/>'s own compiled predicate so all
    ///   search-type semantics (Between, IsAnyOf, DateInterval, etc.) stay in one place.</description></item>
    /// <item><description>Honors display value providers for text-based searches by routing
    ///   through the column's <see cref="SearchTemplateController"/> when one is configured.</description></item>
    /// <item><description>Skips collection-context filters (TopN, AboveAverage, etc.) — those
    ///   stay on the per-column path and are out of scope for cross-column composition.</description></item>
    /// </list>
    /// </remarks>
    internal static class GridFilterTreeCompiler
    {
        /// <summary>
        /// Produces a row predicate from the editor tree. Returns <c>null</c> when the tree is
        /// empty or contains no evaluable conditions; callers should treat that as "no filter."
        /// </summary>
        public static Func<object, bool> Compile(FilterGroupNode root)
        {
            if (root == null) return null;
            return CompileNode(root);
        }

        private static Func<object, bool> CompileNode(FilterEditorNode node)
        {
            switch (node)
            {
                case FilterConditionNode c: return CompileCondition(c);
                case FilterGroupNode g: return CompileGroup(g);
                default: return null;
            }
        }

        private static Func<object, bool> CompileGroup(FilterGroupNode group)
        {
            var children = new List<Func<object, bool>>();
            foreach (var child in group.Children)
            {
                var predicate = CompileNode(child);
                if (predicate != null) children.Add(predicate);
            }

            if (children.Count == 0) return null;

            var op = group.Operator;
            bool negate = op.IsNegated();
            bool isOrCombiner = op == LogicalOperator.Or || op == LogicalOperator.NotOr;

            // Single-child groups still need to honor negation — a NotAnd/NotOr group with a single
            // condition is the inverse of that condition, not the condition itself.
            if (children.Count == 1)
            {
                var only = children[0];
                return negate ? row => !only(row) : only;
            }

            Func<object, bool> combined;
            if (isOrCombiner)
            {
                combined = row =>
                {
                    foreach (var p in children)
                    {
                        if (p(row)) return true;
                    }
                    return false;
                };
            }
            else
            {
                combined = row =>
                {
                    foreach (var p in children)
                    {
                        if (!p(row)) return false;
                    }
                    return true;
                };
            }

            return negate ? row => !combined(row) : combined;
        }

        private static Func<object, bool> CompileCondition(FilterConditionNode condition)
        {
            var column = condition.Column;
            var template = condition.SearchTemplate;
            if (column == null || template == null) return null;

            var bindingPath = SearchDataGrid.ResolveBindingPathInternal(column);
            if (string.IsNullOrEmpty(bindingPath)) return null;

            var controller = column.SearchTemplateController;
            var targetType = ResolveTargetType(controller, template);

            Func<object, bool> templatePredicate;
            try
            {
                templatePredicate = template.BuildExpression(targetType).Compile();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GridFilterTreeCompiler: failed to compile predicate for column '{column.FieldName}': {ex.Message}");
                return null;
            }

            bool hasDisplayProvider = controller?.HasDisplayValueProvider == true;
            bool isTextSearch = SearchEngine.IsTextBasedSearchType(template.SearchType);

            return row =>
            {
                try
                {
                    var raw = ReflectionHelper.GetPropValue(row, bindingPath);

                    if (hasDisplayProvider && isTextSearch)
                    {
                        // Mask providers compare raw string; format/converter providers compare the
                        // display-formatted value. Mirrors EvaluateWithDisplayValues in
                        // SearchDataGridFiltering so the editor tree and per-column path produce
                        // the same answer for the same template.
                        var comparisonValue = controller.DisplayValueProvider.UseRawComparison
                            ? (object)(raw?.ToString() ?? string.Empty)
                            : controller.GetDisplayValue(raw);
                        return templatePredicate(comparisonValue);
                    }

                    return templatePredicate(raw);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GridFilterTreeCompiler: evaluation failed for column '{column.FieldName}': {ex.Message}");
                    return false;
                }
            };
        }

        private static Type ResolveTargetType(SearchTemplateController controller, SearchTemplate template)
        {
            // Mirrors the type-resolution rule used by FilterExpressionBuilder for the per-column
            // path so the compiled predicates behave identically.
            var dataType = controller?.ColumnDataType ?? template.ColumnDataType;
            switch (dataType)
            {
                case ColumnDataType.DateTime: return typeof(DateTime);
                case ColumnDataType.Number: return typeof(decimal);
                case ColumnDataType.Boolean: return typeof(bool);
                default: return typeof(string);
            }
        }
    }
}
