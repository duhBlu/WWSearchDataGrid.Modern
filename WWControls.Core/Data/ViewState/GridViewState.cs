using System.Collections.Generic;

namespace WWControls.Core
{
    /// <summary>
    /// A serializable snapshot of a grid's saved view — its column layout, its filters, or both.
    /// One DTO models all three save modes: the populated sections determine the mode
    /// (<see cref="Layout"/> only, <see cref="Filters"/> only, or both). Persisted as
    /// System.Text.Json via <see cref="GridViewStateSerializer"/>.
    /// </summary>
    /// <remarks>
    /// The contract is deliberately WPF-free and self-describing: everything is keyed by column
    /// <c>FieldName</c>, operand values are stored as invariant strings (re-coerced to the column's
    /// runtime type on load), and enums serialize as their names (never ordinals). Grid-side layout
    /// enums (sort order, pinning, group interval) are held as their string names because their
    /// declaring types live in the WPF grid assembly; the capture/apply layer maps them.
    /// </remarks>
    public sealed class GridViewState
    {
        /// <summary>The current on-disk schema version. Bump when the shape changes incompatibly.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Schema version this instance was written with. Enables forward migration.</summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>Optional friendly name for the saved view (may be null for the implicit "last view").</summary>
        public string Name { get; set; }

        /// <summary>The visual layout section, or <c>null</c> for a filters-only view.</summary>
        public GridLayoutState Layout { get; set; }

        /// <summary>The filter section, or <c>null</c> for a layout-only view.</summary>
        public GridFilterState Filters { get; set; }
    }

    /// <summary>The visual-layout half of a <see cref="GridViewState"/>.</summary>
    public sealed class GridLayoutState
    {
        /// <summary>Per-column layout, one entry per persisted column (keyed by <see cref="GridColumnLayout.FieldName"/>).</summary>
        public List<GridColumnLayout> Columns { get; set; } = new List<GridColumnLayout>();

        /// <summary>Ordered grouping (outermost group first). Empty when the grid is not grouped.</summary>
        public List<GridGroupLayout> Grouping { get; set; } = new List<GridGroupLayout>();

        /// <summary>Whether the drag-to-group panel was showing. Null leaves the grid's current setting.</summary>
        public bool? IsGroupPanelVisible { get; set; }
    }

    /// <summary>Layout state for a single column.</summary>
    public sealed class GridColumnLayout
    {
        /// <summary>Column identity — matches <c>GridColumn.FieldName</c> (fallback <c>FilterMemberPath</c>).</summary>
        public string FieldName { get; set; }

        /// <summary>The column's position among the grid's columns (WPF <c>DataGridColumn.DisplayIndex</c>).</summary>
        public int? DisplayIndex { get; set; }

        /// <summary>Rendered pixel width. Null leaves the column's declared width (e.g. Auto/Star).</summary>
        public double? Width { get; set; }

        /// <summary>Whether the column is shown.</summary>
        public bool? Visible { get; set; }

        /// <summary>Pinning: the name of a <c>FixedColumnPosition</c> value (None/Left/Right).</summary>
        public string Fixed { get; set; }

        /// <summary>Sort direction: the name of a <c>ColumnSortOrder</c> value (None/Ascending/Descending).</summary>
        public string SortOrder { get; set; }

        /// <summary>Rank within a multi-column sort (0 = primary); null/absent when unsorted.</summary>
        public int? SortIndex { get; set; }
    }

    /// <summary>One level of grouping.</summary>
    public sealed class GridGroupLayout
    {
        /// <summary>Grouped column identity — matches <c>GridColumn.FieldName</c>.</summary>
        public string FieldName { get; set; }

        /// <summary>Bucketing mode: the name of a <c>ColumnGroupInterval</c> value.</summary>
        public string GroupInterval { get; set; }

        /// <summary>Group sort direction: the name of a <c>ColumnSortOrder</c> value.</summary>
        public string SortDirection { get; set; }
    }

    /// <summary>The filter half of a <see cref="GridViewState"/>: one entry per actively-filtered column.</summary>
    public sealed class GridFilterState
    {
        /// <summary>Per-column filter definitions.</summary>
        public List<GridColumnFilter> Columns { get; set; } = new List<GridColumnFilter>();
    }

    /// <summary>The full filter tree for one column (mirrors <c>SearchTemplateController.SearchGroups</c>).</summary>
    public sealed class GridColumnFilter
    {
        /// <summary>Column identity — matches <c>GridColumn.FieldName</c> (fallback <c>FilterMemberPath</c>).</summary>
        public string FieldName { get; set; }

        /// <summary>The column's resolved data type, used to coerce operand strings on load.</summary>
        public ColumnDataType ColumnDataType { get; set; }

        /// <summary>Ordered filter groups (combined by each group's <see cref="GridFilterGroup.Operator"/>).</summary>
        public List<GridFilterGroup> Groups { get; set; } = new List<GridFilterGroup>();
    }

    /// <summary>A single filter group — a set of conditions joined to the previous group by <see cref="Operator"/>.</summary>
    public sealed class GridFilterGroup
    {
        /// <summary>How this group combines with the previous one: "And" / "Or" / "NotAnd" / "NotOr".</summary>
        public string Operator { get; set; }

        /// <summary>The conditions in this group.</summary>
        public List<GridFilterCondition> Conditions { get; set; } = new List<GridFilterCondition>();
    }

    /// <summary>A single filter condition (mirrors one <c>SearchTemplate</c>).</summary>
    public sealed class GridFilterCondition
    {
        /// <summary>How this condition combines with the previous one in its group: "And" / "Or".</summary>
        public string Operator { get; set; }

        /// <summary>The comparison operator.</summary>
        public SearchType SearchType { get; set; }

        /// <summary>Primary operand as an invariant string (null for unary operators).</summary>
        public string Primary { get; set; }

        /// <summary>Secondary operand as an invariant string (used by Between / NotBetween / date ranges).</summary>
        public string Secondary { get; set; }

        /// <summary>Value set as invariant strings (used by IsAnyOf / IsNoneOf).</summary>
        public List<string> Values { get; set; }

        /// <summary>Explicit dates as round-trip ("o") strings (used by IsOnAnyOfDates).</summary>
        public List<string> Dates { get; set; }

        /// <summary>Selected relative date-interval names (used by DateInterval).</summary>
        public List<string> Intervals { get; set; }
    }
}
