using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A column descriptor that defines how a column should be created and configured in a
    /// <see cref="SearchDataGrid"/>. Instead of manually creating <see cref="System.Windows.Controls.DataGridColumn"/>
    /// instances and setting attached properties, declare <see cref="GridColumn"/> descriptors
    /// inside <c>SearchDataGrid.GridColumns</c> and the grid will generate the internal WPF
    /// columns automatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GridColumn"/> sits at the bottom of the column hierarchy:
    /// <see cref="ColumnDescriptorElement"/> → <see cref="ColumnLayoutBase"/> →
    /// <see cref="ColumnDataBase"/> → <see cref="GridColumn"/>. The base tiers carry layout, data
    /// identity, filtering, sorting, and editor concerns; this tier is reserved for the
    /// grid-specific surface (grouping, total summaries) that does not apply to other column
    /// hosts.
    /// </para>
    /// <para>
    /// The <see cref="ColumnDataBase.FieldName"/> property is the primary key: it drives <c>Binding</c>,
    /// <c>SortMemberPath</c>, and <c>FilterMemberPath</c> unless explicitly overridden.
    /// </para>
    /// </remarks>
    public class GridColumn : ColumnDataBase
    {
        public GridColumn()
        {
            // Seed the summary collection at construction so XAML property-element syntax
            // (`<sdg:GridColumn.TotalSummaries><sdg:SummaryItem .../></...>`) can add entries —
            // the XAML reader calls GetValue directly to find the target list and fails when
            // it's null (same convention as ColumnDataBase's CustomColumnFilterTabs).
            SetValue(TotalSummariesProperty, new FreezableCollection<SummaryItem>());
            SetValue(GroupFooterSummariesProperty, new FreezableCollection<SummaryItem>());
        }

        #region Grouping

        /// <summary>
        /// Zero-based position of this column in the grid's grouping order. <c>-1</c> (the default)
        /// means the column is not part of the grouping. Setting a non-negative value asks the
        /// owning <see cref="SearchDataGrid"/> to group its rows by this column; the grid maintains
        /// <see cref="System.Windows.Data.CollectionView.GroupDescriptions"/> as the ordered
        /// projection of every column with a non-negative <see cref="GroupIndex"/>, ascending.
        /// </summary>
        /// <remarks>
        /// This is the single source of truth for grouping (the <c>IsGrouped</c> / <c>GroupLevel</c>
        /// projections derive from it). After every grouping change the grid normalizes the set of
        /// grouped columns to a contiguous <c>0..N-1</c> range, so <see cref="GroupIndex"/> and
        /// <see cref="GroupLevel"/> converge once the layout settles.
        /// </remarks>
        public static readonly DependencyProperty GroupIndexProperty =
            DependencyProperty.Register(
                nameof(GroupIndex),
                typeof(int),
                typeof(GridColumn),
                new PropertyMetadata(-1, OnGroupIndexChanged));

        public int GroupIndex
        {
            get => (int)GetValue(GroupIndexProperty);
            set => SetValue(GroupIndexProperty, value);
        }

        private static void OnGroupIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;

            // Keep IsGrouped in lockstep with GroupIndex immediately, before the grid rebuild
            // runs, so bindings see a consistent state even while detached from a grid.
            col.SetIsGrouped((int)e.NewValue >= 0);

            // The grid owns the GroupDescriptions projection (and the GroupIndex normalization).
            // While detached, GenerateColumnsFromDescriptors picks the value up on attach.
            col.View?.OnColumnGroupIndexChanged(col);
        }

        private static readonly DependencyPropertyKey IsGroupedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsGrouped),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="IsGrouped"/> for bindings.</summary>
        public static readonly DependencyProperty IsGroupedProperty = IsGroupedPropertyKey.DependencyProperty;

        /// <summary>
        /// True when this column participates in the grid's grouping
        /// (<see cref="GroupIndex"/> &gt;= 0). Derived directly from <see cref="GroupIndex"/>.
        /// </summary>
        public bool IsGrouped => (bool)GetValue(IsGroupedProperty);

        internal void SetIsGrouped(bool value) => SetValue(IsGroupedPropertyKey, value);

        private static readonly DependencyPropertyKey GroupLevelPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(GroupLevel),
                typeof(int),
                typeof(GridColumn),
                new PropertyMetadata(-1));

        /// <summary>Read-only dependency property exposing <see cref="GroupLevel"/> for bindings.</summary>
        public static readonly DependencyProperty GroupLevelProperty = GroupLevelPropertyKey.DependencyProperty;

        /// <summary>
        /// Zero-based rank of this column among the currently grouped columns, ordered by
        /// <see cref="GroupIndex"/>. <c>-1</c> when the column is not grouped. Because the grid
        /// normalizes <see cref="GroupIndex"/> to a contiguous range, <see cref="GroupLevel"/> and
        /// <see cref="GroupIndex"/> hold the same value once a grouping change settles; the two are
        /// kept distinct so consumers can bind to the semantic "nesting depth" independently.
        /// </summary>
        public int GroupLevel => (int)GetValue(GroupLevelProperty);

        internal void SetGroupLevel(int value) => SetValue(GroupLevelPropertyKey, value);

        /// <summary>
        /// Resolves the property path the grid groups this column's rows by:
        /// <see cref="ColumnDataBase.SortMemberPath"/> when set, then
        /// <see cref="ColumnDataBase.FieldName"/>, then the cell <c>Binding</c>'s path
        /// (<see cref="ColumnDataBase.ResolveValuePath"/>) for binding-only columns. Mirrors the
        /// coalescing the grid uses for <c>SortMemberPath</c> so a grouped column sorts and groups
        /// on the same member.
        /// </summary>
        internal string ResolveGroupPath()
        {
            if (!string.IsNullOrEmpty(SortMemberPath)) return SortMemberPath;
            if (!string.IsNullOrEmpty(FieldName)) return FieldName;
            return ResolveValuePath();
        }

        /// <summary>
        /// Default sort direction applied to this column when it joins the grouping and has no
        /// explicit <see cref="ColumnDataBase.SortOrder"/>. Read by the grouping engine
        /// (<see cref="SearchDataGrid.RebuildGroupDescriptions"/> →
        /// <c>ReconcileGroupSortDescriptions</c>) to seed the group-path
        /// <see cref="System.ComponentModel.SortDescription"/>; an explicit
        /// <see cref="ColumnDataBase.SortOrder"/> from a header click or programmatic sort takes
        /// precedence. Default <see cref="ColumnSortOrder.Ascending"/>.
        /// <see cref="ColumnSortOrder.None"/> is treated as Ascending — a grouped column always
        /// needs a sort so its buckets render in a stable order.
        /// </summary>
        public static readonly DependencyProperty DefaultGroupBySortDirectionProperty =
            DependencyProperty.Register(
                nameof(DefaultGroupBySortDirection),
                typeof(ColumnSortOrder),
                typeof(GridColumn),
                new PropertyMetadata(ColumnSortOrder.Ascending, OnDefaultGroupBySortDirectionChanged));

        public ColumnSortOrder DefaultGroupBySortDirection
        {
            get => (ColumnSortOrder)GetValue(DefaultGroupBySortDirectionProperty);
            set => SetValue(DefaultGroupBySortDirectionProperty, value);
        }

        private static void OnDefaultGroupBySortDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;
            // Only rebuild when the column is grouped AND has no explicit sort — otherwise the
            // explicit SortOrder wins in the engine and the new default has no observable effect.
            if (col.IsGrouped && col.SortOrder == ColumnSortOrder.None)
                col.View?.RebuildGroupDescriptions();
        }

        #endregion

        #region Grouping Gating

        /// <summary>
        /// Column-level gate for whether this column may participate in grouping. <c>true</c> (the
        /// default) allows it; <c>false</c> blocks the column from being grouped (the
        /// "Group By This Column" menu item is removed and <see cref="SearchDataGrid.GroupBy(GridColumn)"/>
        /// refuses). The resolved value — this gate AND the grid's
        /// <see cref="SearchDataGrid.AllowGrouping"/> — is exposed by <see cref="ActualAllowGrouping"/>.
        /// </summary>
        public static readonly DependencyProperty AllowGroupingProperty =
            DependencyProperty.Register(
                nameof(AllowGrouping),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnAllowGroupingChanged));

        public bool AllowGrouping
        {
            get => (bool)GetValue(AllowGroupingProperty);
            set => SetValue(AllowGroupingProperty, value);
        }

        private static readonly DependencyPropertyKey ActualAllowGroupingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowGrouping),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true));

        /// <summary>Read-only dependency property exposing <see cref="ActualAllowGrouping"/> for bindings.</summary>
        public static readonly DependencyProperty ActualAllowGroupingProperty = ActualAllowGroupingPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved grouping permission: the column-level <see cref="AllowGrouping"/> AND the grid's
        /// <see cref="SearchDataGrid.AllowGrouping"/> (defaulting to allowed when no grid is attached).
        /// </summary>
        public bool ActualAllowGrouping => (bool)GetValue(ActualAllowGroupingProperty);

        /// <summary>
        /// Resolves the effective grouping permission. The single source of truth read by
        /// <see cref="RefreshActualAllowGrouping"/> and the grouping engine.
        /// </summary>
        internal bool ResolveEffectiveAllowGrouping() => AllowGrouping && (View?.AllowGrouping ?? true);

        /// <summary>
        /// Recomputes <see cref="ActualAllowGrouping"/>. Called on the column change, on grid attach
        /// (<see cref="OnViewChanged"/>), and when the grid's
        /// <see cref="SearchDataGrid.AllowGrouping"/> changes (the grid walks columns).
        /// </summary>
        internal void RefreshActualAllowGrouping()
            => SetValue(ActualAllowGroupingPropertyKey, ResolveEffectiveAllowGrouping());

        private static void OnAllowGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;
            col.RefreshActualAllowGrouping();
            // Turning grouping off on an already-grouped column must drop it from the grouping.
            if (!col.ActualAllowGrouping && col.GroupIndex >= 0)
                col.GroupIndex = -1;
            else
                col.View?.RebuildGroupDescriptions();
        }

        /// <summary>
        /// Column-level override for whether this column stays visible while it is grouped.
        /// <c>null</c> (default) inherits the grid's <see cref="SearchDataGrid.ShowGroupedColumns"/>;
        /// <c>true</c>/<c>false</c> overrides it for this column. The resolved value is exposed by
        /// <see cref="ActualShowGroupedColumn"/>; when it resolves <c>false</c> and the column is
        /// grouped, the grid collapses the generated column without disturbing the descriptor's own
        /// <see cref="ColumnLayoutBase.Visible"/>.
        /// </summary>
        public static readonly DependencyProperty ShowGroupedColumnProperty =
            DependencyProperty.Register(
                nameof(ShowGroupedColumn),
                typeof(bool?),
                typeof(GridColumn),
                new PropertyMetadata(null, OnShowGroupedColumnChanged));

        public bool? ShowGroupedColumn
        {
            get => (bool?)GetValue(ShowGroupedColumnProperty);
            set => SetValue(ShowGroupedColumnProperty, value);
        }

        private static readonly DependencyPropertyKey ActualShowGroupedColumnPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualShowGroupedColumn),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="ActualShowGroupedColumn"/> for bindings.</summary>
        public static readonly DependencyProperty ActualShowGroupedColumnProperty = ActualShowGroupedColumnPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved "keep visible while grouped" state: the column override when set, otherwise the
        /// grid's <see cref="SearchDataGrid.ShowGroupedColumns"/> (defaulting to <c>false</c> when no
        /// grid is attached).
        /// </summary>
        public bool ActualShowGroupedColumn => (bool)GetValue(ActualShowGroupedColumnProperty);

        /// <summary>
        /// Resolves the effective "show grouped column" value — column override first, then the grid
        /// default, then <c>false</c>.
        /// </summary>
        internal bool ResolveEffectiveShowGroupedColumn() => ShowGroupedColumn ?? View?.ShowGroupedColumns ?? false;

        /// <summary>
        /// Recomputes <see cref="ActualShowGroupedColumn"/>. Called on the column change, on grid
        /// attach (<see cref="OnViewChanged"/>), and when the grid's
        /// <see cref="SearchDataGrid.ShowGroupedColumns"/> changes (the grid walks columns).
        /// </summary>
        internal void RefreshActualShowGroupedColumn()
            => SetValue(ActualShowGroupedColumnPropertyKey, ResolveEffectiveShowGroupedColumn());

        private static void OnShowGroupedColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;
            col.RefreshActualShowGroupedColumn();
            // Re-apply grouped-column visibility so the generated column shows/hides per the new value.
            col.View?.ApplyGroupColumnVisibility();
        }

        /// <summary>
        /// Gets or sets how this column buckets rows when grouped — whole value (the default),
        /// alphabetical first letter, or a date interval (year / month / day / weekday / relative
        /// range). The date modes require a <see cref="System.DateTime"/> /
        /// <see cref="System.DateTimeOffset"/> <see cref="ColumnDataBase.FieldType"/>, enforced by
        /// <see cref="Validate"/>.
        /// </summary>
        public static readonly DependencyProperty GroupIntervalProperty =
            DependencyProperty.Register(
                nameof(GroupInterval),
                typeof(ColumnGroupInterval),
                typeof(GridColumn),
                new PropertyMetadata(ColumnGroupInterval.Default, OnGroupIntervalChanged));

        public ColumnGroupInterval GroupInterval
        {
            get => (ColumnGroupInterval)GetValue(GroupIntervalProperty);
            set => SetValue(GroupIntervalProperty, value);
        }

        private static void OnGroupIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-bucket immediately when the interval changes on an already-grouped column.
            if (d is GridColumn col && col.IsGrouped)
                col.View?.RebuildGroupDescriptions();
        }

        /// <summary>True when <see cref="GroupInterval"/> is one of the date-based bucketing modes.</summary>
        internal bool IsDateGroupInterval =>
            GroupInterval == ColumnGroupInterval.DateYear
            || GroupInterval == ColumnGroupInterval.DateMonth
            || GroupInterval == ColumnGroupInterval.DateDay
            || GroupInterval == ColumnGroupInterval.DateWeekDay
            || GroupInterval == ColumnGroupInterval.DateRange;

        /// <summary>
        /// Validates that this column can be grouped. Called by the grouping engine before a group
        /// is applied; a failure clears the offending <see cref="GroupIndex"/> and logs, rather than
        /// throwing. Rules (D5): a grouped column must resolve to a property path
        /// (<see cref="ResolveGroupPath"/>); a date <see cref="GroupInterval"/> requires a
        /// <see cref="System.DateTime"/> / <see cref="System.DateTimeOffset"/>
        /// <see cref="ColumnDataBase.FieldType"/>.
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrEmpty(ResolveGroupPath()))
            {
                Debug.WriteLine(
                    $"GridColumn.Validate: grouping requires a resolvable path " +
                    $"(FieldName / SortMemberPath / Binding); column '{HeaderCaption}' skipped.");
                return false;
            }

            if (IsDateGroupInterval && !IsDateFieldType())
            {
                Debug.WriteLine(
                    $"GridColumn.Validate: GroupInterval '{GroupInterval}' requires a DateTime/DateTimeOffset " +
                    $"FieldType; column '{HeaderCaption}' (FieldType '{FieldType?.Name ?? "unknown"}') skipped.");
                return false;
            }

            return true;
        }

        private bool IsDateFieldType()
        {
            var type = FieldType;
            if (type == null) return false;
            var underlying = System.Nullable.GetUnderlyingType(type) ?? type;
            return underlying == typeof(System.DateTime) || underlying == typeof(System.DateTimeOffset);
        }

        #endregion

        #region Group Value Templating

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to render this column's value in a group
        /// header when the grid is grouped by it. The template's <c>DataContext</c> is the
        /// <see cref="System.Windows.Data.CollectionViewGroup"/> (bind <c>Name</c> for the group
        /// value, <c>ItemCount</c> for the row count). When unset, the grid falls back to a default
        /// header that shows the group value as text. The item-count suffix is rendered by the group
        /// chrome regardless, so this template only customizes the value portion.
        /// </summary>
        public static readonly DependencyProperty GroupValueTemplateProperty =
            DependencyProperty.Register(
                nameof(GroupValueTemplate),
                typeof(DataTemplate),
                typeof(GridColumn),
                new PropertyMetadata(null));

        public DataTemplate GroupValueTemplate
        {
            get => (DataTemplate)GetValue(GroupValueTemplateProperty);
            set => SetValue(GroupValueTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="DataTemplateSelector"/> that chooses the group-value template
        /// per group. Takes precedence over <see cref="GroupValueTemplate"/> when it returns a
        /// non-null template. <see cref="ActualGroupValueTemplateSelector"/> exposes the resolved
        /// value (mirrors this today; reserved for future grid-level default resolution).
        /// </summary>
        public static readonly DependencyProperty GroupValueTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(GroupValueTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(GridColumn),
                new PropertyMetadata(null, OnGroupValueTemplateSelectorChanged));

        public DataTemplateSelector GroupValueTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(GroupValueTemplateSelectorProperty);
            set => SetValue(GroupValueTemplateSelectorProperty, value);
        }

        private static readonly DependencyPropertyKey ActualGroupValueTemplateSelectorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualGroupValueTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(GridColumn),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="ActualGroupValueTemplateSelector"/> for bindings.</summary>
        public static readonly DependencyProperty ActualGroupValueTemplateSelectorProperty = ActualGroupValueTemplateSelectorPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved group-value template selector. Mirrors <see cref="GroupValueTemplateSelector"/>
        /// today; reserved for future grid-level default resolution (matching the
        /// <c>HeaderTemplateSelector</c> → <c>ActualHeaderTemplateSelector</c> pattern).
        /// </summary>
        public DataTemplateSelector ActualGroupValueTemplateSelector => (DataTemplateSelector)GetValue(ActualGroupValueTemplateSelectorProperty);

        private static void OnGroupValueTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn col)
                col.SetValue(ActualGroupValueTemplateSelectorPropertyKey, e.NewValue);
        }

        #endregion

        #region Group Header Templating

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> that renders the <em>entire</em> group
        /// header for this column when the grid is grouped by it (the expander's
        /// <see cref="System.Windows.Controls.Expander.Header"/>, not its chevron — which belongs
        /// to the expander chrome). The template's <c>DataContext</c> is the
        /// <see cref="System.Windows.Data.CollectionViewGroup"/> (bind <c>Name</c> for the group
        /// value, <c>ItemCount</c> for the row count). When unset, the grid falls back to the
        /// theme's default header — a value (via <see cref="GroupValueTemplate"/>) plus a count
        /// chip — so <see cref="GroupValueTemplate"/> remains the lighter-weight knob when only
        /// the value slot needs customizing; <see cref="GroupHeaderTemplate"/> replaces the whole
        /// header including the count chip.
        /// </summary>
        public static readonly DependencyProperty GroupHeaderTemplateProperty =
            DependencyProperty.Register(
                nameof(GroupHeaderTemplate),
                typeof(DataTemplate),
                typeof(GridColumn),
                new PropertyMetadata(null));

        public DataTemplate GroupHeaderTemplate
        {
            get => (DataTemplate)GetValue(GroupHeaderTemplateProperty);
            set => SetValue(GroupHeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="DataTemplateSelector"/> that chooses the whole-header
        /// template per group. Takes precedence over <see cref="GroupHeaderTemplate"/> when it
        /// returns a non-null template. <see cref="ActualGroupHeaderTemplateSelector"/> exposes
        /// the resolved value (mirrors this today; reserved for future grid-level default
        /// resolution).
        /// </summary>
        public static readonly DependencyProperty GroupHeaderTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(GroupHeaderTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(GridColumn),
                new PropertyMetadata(null, OnGroupHeaderTemplateSelectorChanged));

        public DataTemplateSelector GroupHeaderTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(GroupHeaderTemplateSelectorProperty);
            set => SetValue(GroupHeaderTemplateSelectorProperty, value);
        }

        private static readonly DependencyPropertyKey ActualGroupHeaderTemplateSelectorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualGroupHeaderTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(GridColumn),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="ActualGroupHeaderTemplateSelector"/> for bindings.</summary>
        public static readonly DependencyProperty ActualGroupHeaderTemplateSelectorProperty = ActualGroupHeaderTemplateSelectorPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved group-header template selector. Mirrors <see cref="GroupHeaderTemplateSelector"/>
        /// today; reserved for future grid-level default resolution (matching the
        /// <c>HeaderTemplateSelector</c> → <c>ActualHeaderTemplateSelector</c> pattern).
        /// </summary>
        public DataTemplateSelector ActualGroupHeaderTemplateSelector => (DataTemplateSelector)GetValue(ActualGroupHeaderTemplateSelectorProperty);

        private static void OnGroupHeaderTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn col)
                col.SetValue(ActualGroupHeaderTemplateSelectorPropertyKey, e.NewValue);
        }

        #endregion

        #region Total Summaries

        /// <summary>
        /// Summary definitions computed over the grid's <em>filtered</em> rows and rendered in
        /// THIS column's cell of the total summary row (stacked vertically). Each entry may
        /// target another column's field via <see cref="SummaryItem.FieldName"/> — foreign
        /// targets render caption-qualified (<c>Min(Discount)=…</c>), so one cell can mix
        /// aggregates from several columns (the "Totals for 'X'" editor configures this).
        /// Seeded with an empty collection at construction; the runtime picker on the summary
        /// cell writes here too. The grid recomputes on filter / source / grouping changes and
        /// on cell edit commits — see <see cref="SearchDataGrid.RefreshSummaries"/> for the
        /// explicit trigger.
        /// </summary>
        public static readonly DependencyProperty TotalSummariesProperty =
            DependencyProperty.Register(
                nameof(TotalSummaries),
                typeof(FreezableCollection<SummaryItem>),
                typeof(GridColumn),
                new PropertyMetadata(null, OnTotalSummariesChanged));

        public FreezableCollection<SummaryItem> TotalSummaries
        {
            get => (FreezableCollection<SummaryItem>)GetValue(TotalSummariesProperty);
            set => SetValue(TotalSummariesProperty, value);
        }

        private static void OnTotalSummariesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;
            // Freezable.Changed fires for both collection mutations and item DP edits, so a
            // single subscription keeps the computed results in step with the definitions.
            if (e.OldValue is FreezableCollection<SummaryItem> oldItems)
                oldItems.Changed -= col.OnTotalSummaryDefinitionsChanged;
            if (e.NewValue is FreezableCollection<SummaryItem> newItems)
                newItems.Changed += col.OnTotalSummaryDefinitionsChanged;
            col.OnTotalSummaryDefinitionsChanged(col, System.EventArgs.Empty);
        }

        private void OnTotalSummaryDefinitionsChanged(object sender, System.EventArgs e)
        {
            SetValue(HasTotalSummariesPropertyKey, TotalSummaries is { Count: > 0 });
            View?.OnColumnTotalSummariesChanged(this);
        }

        private static readonly DependencyPropertyKey HasTotalSummariesPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HasTotalSummaries),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="HasTotalSummaries"/> for bindings.</summary>
        public static readonly DependencyProperty HasTotalSummariesProperty = HasTotalSummariesPropertyKey.DependencyProperty;

        /// <summary>True when <see cref="TotalSummaries"/> holds at least one definition.</summary>
        public bool HasTotalSummaries => (bool)GetValue(HasTotalSummariesProperty);

        /// <summary>
        /// Which aggregate functions the runtime summary picker (the total-summary cell's
        /// context menu) offers for this column. Defaults to <see cref="AllowedSummaries.All"/>;
        /// functions the column's <see cref="ColumnDataBase.FieldType"/> can't compute (e.g. Sum
        /// on a string column) are gated off regardless. Declarative <see cref="TotalSummaries"/>
        /// entries are not validated against this — it gates the runtime UX only.
        /// </summary>
        public static readonly DependencyProperty AllowedTotalSummariesProperty =
            DependencyProperty.Register(
                nameof(AllowedTotalSummaries),
                typeof(AllowedSummaries),
                typeof(GridColumn),
                new PropertyMetadata(AllowedSummaries.All));

        public AllowedSummaries AllowedTotalSummaries
        {
            get => (AllowedSummaries)GetValue(AllowedTotalSummariesProperty);
            set => SetValue(AllowedTotalSummariesProperty, value);
        }

        private static readonly DependencyPropertyKey TotalSummaryTextPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TotalSummaryText),
                typeof(string),
                typeof(GridColumn),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="TotalSummaryText"/> for bindings.</summary>
        public static readonly DependencyProperty TotalSummaryTextProperty = TotalSummaryTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The formatted text of every computed total summary for this column, joined for
        /// display in the total summary cell (e.g. <c>"Sum=1,234.50  Max=99"</c>). Null when
        /// the column defines no total summaries. Pushed by the grid's summary engine.
        /// </summary>
        public string TotalSummaryText => (string)GetValue(TotalSummaryTextProperty);

        internal void SetTotalSummaryText(string value) => SetValue(TotalSummaryTextPropertyKey, value);

        private static readonly DependencyPropertyKey TotalSummaryTextInfoPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TotalSummaryTextInfo),
                typeof(IReadOnlyList<SummaryResult>),
                typeof(GridColumn),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="TotalSummaryTextInfo"/> for bindings.</summary>
        public static readonly DependencyProperty TotalSummaryTextInfoProperty = TotalSummaryTextInfoPropertyKey.DependencyProperty;

        /// <summary>
        /// The computed total summaries as structured per-item results (function, raw value,
        /// formatted text) — the data behind <see cref="TotalSummaryText"/>, for templates that
        /// render summaries individually. Null when the column defines no total summaries.
        /// </summary>
        public IReadOnlyList<SummaryResult> TotalSummaryTextInfo => (IReadOnlyList<SummaryResult>)GetValue(TotalSummaryTextInfoProperty);

        internal void SetTotalSummaryTextInfo(IReadOnlyList<SummaryResult> value) => SetValue(TotalSummaryTextInfoPropertyKey, value);

        /// <summary>
        /// Style applied to each summary-entry <see cref="System.Windows.Controls.TextBlock"/>
        /// in this column's total summary cell (entries stack vertically, one per
        /// <see cref="SummaryItem"/>). When unset, inherits the grid's
        /// <see cref="SearchDataGrid.TotalSummaryContentStyle"/>; the resolved value is exposed
        /// by <see cref="ActualTotalSummaryContentStyle"/>.
        /// </summary>
        public static readonly DependencyProperty TotalSummaryContentStyleProperty =
            DependencyProperty.Register(
                nameof(TotalSummaryContentStyle),
                typeof(Style),
                typeof(GridColumn),
                new PropertyMetadata(null, OnTotalSummaryContentStyleChanged));

        public Style TotalSummaryContentStyle
        {
            get => (Style)GetValue(TotalSummaryContentStyleProperty);
            set => SetValue(TotalSummaryContentStyleProperty, value);
        }

        private static readonly DependencyPropertyKey ActualTotalSummaryContentStylePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualTotalSummaryContentStyle),
                typeof(Style),
                typeof(GridColumn),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="ActualTotalSummaryContentStyle"/> for bindings.</summary>
        public static readonly DependencyProperty ActualTotalSummaryContentStyleProperty = ActualTotalSummaryContentStylePropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved total-summary content style: the column-level
        /// <see cref="TotalSummaryContentStyle"/> when set, otherwise the grid's
        /// <see cref="SearchDataGrid.TotalSummaryContentStyle"/>.
        /// </summary>
        public Style ActualTotalSummaryContentStyle => (Style)GetValue(ActualTotalSummaryContentStyleProperty);

        /// <summary>
        /// Recomputes <see cref="ActualTotalSummaryContentStyle"/>. Called on the column change,
        /// on grid attach (<see cref="OnViewChanged"/>), and when the grid's default changes
        /// (the grid walks columns).
        /// </summary>
        internal void RefreshActualTotalSummaryContentStyle()
            => SetValue(ActualTotalSummaryContentStylePropertyKey,
                TotalSummaryContentStyle ?? View?.TotalSummaryContentStyle);

        private static void OnTotalSummaryContentStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn col)
                col.RefreshActualTotalSummaryContentStyle();
        }

        private static readonly DependencyPropertyKey IsSortedBySummaryPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsSortedBySummary),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="IsSortedBySummary"/> for bindings.</summary>
        public static readonly DependencyProperty IsSortedBySummaryProperty = IsSortedBySummaryPropertyKey.DependencyProperty;

        /// <summary>
        /// True when the grid's groups are ordered by a summary aggregate over this column's
        /// field — see <see cref="SearchDataGrid.SortGroupsBySummary"/>. Pushed by the grid when
        /// the summary sort is set or cleared.
        /// </summary>
        public bool IsSortedBySummary => (bool)GetValue(IsSortedBySummaryProperty);

        internal void SetIsSortedBySummary(bool value) => SetValue(IsSortedBySummaryPropertyKey, value);

        /// <summary>
        /// Resolves the property path summaries aggregate on: <see cref="ColumnDataBase.FieldName"/>,
        /// then the cell <c>Binding</c>'s path for binding-only columns. Deliberately NOT
        /// <see cref="ColumnDataBase.SortMemberPath"/> — a column sorted by a surrogate member
        /// still summarizes its displayed value.
        /// </summary>
        internal string ResolveSummaryPath()
        {
            if (!string.IsNullOrEmpty(FieldName)) return FieldName;
            return ResolveValuePath();
        }

        #endregion

        #region Group Footer Summaries

        /// <summary>
        /// Summary definitions computed per group over that group's leaf rows and rendered in
        /// THIS column's cell of the group's footer row (stacked vertically) — the per-group
        /// counterpart of <see cref="TotalSummaries"/>. The footer row docks at the bottom of an
        /// expanded group and pins directly beneath the header of a collapsed one; each cell
        /// aligns under its column and scrolls horizontally with the data. Seeded with an empty
        /// collection at construction; the runtime picker on a footer cell writes here too. The
        /// grid recomputes on filter / source / grouping changes and on cell edit commits.
        /// </summary>
        public static readonly DependencyProperty GroupFooterSummariesProperty =
            DependencyProperty.Register(
                nameof(GroupFooterSummaries),
                typeof(FreezableCollection<SummaryItem>),
                typeof(GridColumn),
                new PropertyMetadata(null, OnGroupFooterSummariesChanged));

        public FreezableCollection<SummaryItem> GroupFooterSummaries
        {
            get => (FreezableCollection<SummaryItem>)GetValue(GroupFooterSummariesProperty);
            set => SetValue(GroupFooterSummariesProperty, value);
        }

        private static void OnGroupFooterSummariesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;
            // Freezable.Changed fires for both collection mutations and item DP edits, so a
            // single subscription keeps the projected footers in step with the definitions.
            if (e.OldValue is FreezableCollection<SummaryItem> oldItems)
                oldItems.Changed -= col.OnGroupFooterSummaryDefinitionsChanged;
            if (e.NewValue is FreezableCollection<SummaryItem> newItems)
                newItems.Changed += col.OnGroupFooterSummaryDefinitionsChanged;
            col.OnGroupFooterSummaryDefinitionsChanged(col, System.EventArgs.Empty);
        }

        private void OnGroupFooterSummaryDefinitionsChanged(object sender, System.EventArgs e)
        {
            SetValue(HasGroupFooterSummariesPropertyKey, GroupFooterSummaries is { Count: > 0 });
            View?.OnColumnGroupFooterSummariesChanged(this);
        }

        private static readonly DependencyPropertyKey HasGroupFooterSummariesPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HasGroupFooterSummaries),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="HasGroupFooterSummaries"/> for bindings.</summary>
        public static readonly DependencyProperty HasGroupFooterSummariesProperty = HasGroupFooterSummariesPropertyKey.DependencyProperty;

        /// <summary>True when <see cref="GroupFooterSummaries"/> holds at least one definition.</summary>
        public bool HasGroupFooterSummaries => (bool)GetValue(HasGroupFooterSummariesProperty);

        /// <summary>
        /// Which aggregate functions the runtime picker on this column's footer cell offers.
        /// Defaults to <see cref="AllowedSummaries.All"/>; functions the column's
        /// <see cref="ColumnDataBase.FieldType"/> can't compute are gated off regardless.
        /// Mirrors <see cref="AllowedTotalSummaries"/> for the footer surface.
        /// </summary>
        public static readonly DependencyProperty AllowedGroupFooterSummariesProperty =
            DependencyProperty.Register(
                nameof(AllowedGroupFooterSummaries),
                typeof(AllowedSummaries),
                typeof(GridColumn),
                new PropertyMetadata(AllowedSummaries.All));

        public AllowedSummaries AllowedGroupFooterSummaries
        {
            get => (AllowedSummaries)GetValue(AllowedGroupFooterSummariesProperty);
            set => SetValue(AllowedGroupFooterSummariesProperty, value);
        }

        #endregion

        #region View Attach

        /// <inheritdoc/>
        protected override void OnViewChanged()
        {
            base.OnViewChanged();
            // ActualAllowGrouping / ActualShowGroupedColumn fall back to grid-level values — resolve
            // them once the grid back-pointer is wired.
            RefreshActualAllowGrouping();
            RefreshActualShowGroupedColumn();
            // Seed the group-value selector mirror so the group header selector reads a resolved value.
            SetValue(ActualGroupValueTemplateSelectorPropertyKey, GroupValueTemplateSelector);
            // Same for the whole-header selector mirror.
            SetValue(ActualGroupHeaderTemplateSelectorPropertyKey, GroupHeaderTemplateSelector);
            // Summary content style inherits a grid-level default — resolve on attach, and let
            // the engine pick up any declaratively-defined summaries.
            RefreshActualTotalSummaryContentStyle();
            if (HasTotalSummaries)
                View?.OnColumnTotalSummariesChanged(this);
            if (HasGroupFooterSummaries)
                View?.OnColumnGroupFooterSummariesChanged(this);
        }

        #endregion
    }
}
