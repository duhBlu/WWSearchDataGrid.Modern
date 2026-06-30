using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// The summary engine. Three independently-configured surfaces, all computed over the
    /// grid's filtered leaf rows (the same set <see cref="FilteredItemCount"/> counts —
    /// grouping-aware, collapse-independent, never the header sentinels):
    /// <list type="bullet">
    /// <item><b>Column-aligned totals row</b> — each column's
    /// <see cref="GridColumn.TotalSummaries"/>, rendered in that column's cell (entries may
    /// target other columns' fields via <see cref="SummaryItem.FieldName"/>; foreign targets
    /// render caption-qualified). Docked per <see cref="TotalSummaryPosition"/>.</item>
    /// <item><b>Fixed total summary panel</b> — the grid's own
    /// <see cref="FixedTotalSummaries"/> definitions, one horizontal non-scrolling run beneath
    /// the items (a no-FieldName Count entry is the grid row count).</item>
    /// <item><b>Group headers</b> — the grid's <see cref="GroupSummaries"/> definitions plus
    /// the opt-in <see cref="ShowGroupRowCount"/> entry, the SAME set at every group level,
    /// computed per group node during the projection and recomputed in place (no reflatten)
    /// by <see cref="RefreshGroupSummaryTexts"/> between projections.</item>
    /// </list>
    /// Recompute triggers: every <c>UpdateFilteredItemCount</c> call site (filter / source /
    /// projection changes), cell edit commits, and definition edits — coalesced to one pass per
    /// dispatcher tick. <see cref="RefreshSummaries"/> recomputes synchronously.
    /// </summary>
    public partial class SearchDataGrid
    {
        #region Grid surface — total summary row

        /// <summary>
        /// Shows the column-aligned total summary row. Like <see cref="ShowFixedTotalSummary"/>,
        /// this is an explicit opt-in with no content-based auto-collapse: a shown row stays
        /// visible even while no column defines a summary, so its per-cell right-click picker
        /// (the only runtime way to add totals) remains reachable. Default <c>false</c>;
        /// <see cref="TotalSummaryPosition"/> = <c>None</c> and <see cref="AllowTotalSummary"/> =
        /// <c>false</c> both still suppress it — see <see cref="ActualShowTotalSummaryRow"/>, which
        /// the template binds.
        /// </summary>
        public static readonly DependencyProperty ShowTotalSummaryProperty =
            DependencyProperty.Register(
                nameof(ShowTotalSummary),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false, OnSummaryVisibilityInputChanged));

        public bool ShowTotalSummary
        {
            get => (bool)GetValue(ShowTotalSummaryProperty);
            set => SetValue(ShowTotalSummaryProperty, value);
        }

        /// <summary>
        /// Which edge the column-aligned total summary row docks to — <c>Bottom</c> (default,
        /// above the horizontal scrollbar), <c>Top</c> (beneath the filter row), or <c>None</c>
        /// to suppress it. The grid template repositions the row via triggers on this value.
        /// </summary>
        public static readonly DependencyProperty TotalSummaryPositionProperty =
            DependencyProperty.Register(
                nameof(TotalSummaryPosition),
                typeof(TotalSummaryPosition),
                typeof(SearchDataGrid),
                new PropertyMetadata(TotalSummaryPosition.Bottom, OnSummaryVisibilityInputChanged));

        public TotalSummaryPosition TotalSummaryPosition
        {
            get => (TotalSummaryPosition)GetValue(TotalSummaryPositionProperty);
            set => SetValue(TotalSummaryPositionProperty, value);
        }

        /// <summary>
        /// Master feature gate for the column-aligned total summary row (default <c>true</c>).
        /// When <c>false</c> the row never shows even if <see cref="ShowTotalSummary"/> is set, the
        /// column-header menu's total-summary entries (show/hide toggle, Customize Totals…) are
        /// removed, and its show/customize commands report <c>CanExecute=false</c>.
        /// <see cref="ShowTotalSummary"/> remains the per-instance visibility toggle <em>within</em>
        /// an enabled feature; the resolved value the template binds is
        /// <see cref="ActualShowTotalSummaryRow"/>. The fixed-panel counterpart is
        /// <see cref="AllowFixedTotalSummary"/>.
        /// </summary>
        public static readonly DependencyProperty AllowTotalSummaryProperty =
            DependencyProperty.Register(
                nameof(AllowTotalSummary),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true, OnSummaryVisibilityInputChanged));

        public bool AllowTotalSummary
        {
            get => (bool)GetValue(AllowTotalSummaryProperty);
            set => SetValue(AllowTotalSummaryProperty, value);
        }

        private static void OnSummaryVisibilityInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
                grid.RefreshSummaryRowVisibility();
        }

        private static readonly DependencyPropertyKey ActualShowTotalSummaryRowPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualShowTotalSummaryRow),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="ActualShowTotalSummaryRow"/> for bindings.</summary>
        public static readonly DependencyProperty ActualShowTotalSummaryRowProperty = ActualShowTotalSummaryRowPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved visibility of the column-aligned total summary row:
        /// <see cref="AllowTotalSummary"/> AND <see cref="ShowTotalSummary"/> AND
        /// <see cref="TotalSummaryPosition"/> ≠ None. Deliberately NOT content-based — an empty
        /// row must stay visible so summaries can be added through it. The grid template binds the
        /// row's Visibility here.
        /// </summary>
        public bool ActualShowTotalSummaryRow => (bool)GetValue(ActualShowTotalSummaryRowProperty);

        /// <summary>
        /// Grid-level default style for the per-entry <see cref="System.Windows.Controls.TextBlock"/>s
        /// inside total summary cells. Columns inherit it unless they set their own
        /// <see cref="GridColumn.TotalSummaryContentStyle"/>.
        /// </summary>
        public static readonly DependencyProperty TotalSummaryContentStyleProperty =
            DependencyProperty.Register(
                nameof(TotalSummaryContentStyle),
                typeof(Style),
                typeof(SearchDataGrid),
                new PropertyMetadata(null, OnGridTotalSummaryContentStyleChanged));

        public Style TotalSummaryContentStyle
        {
            get => (Style)GetValue(TotalSummaryContentStyleProperty);
            set => SetValue(TotalSummaryContentStyleProperty, value);
        }

        private static void OnGridTotalSummaryContentStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid || grid.GridColumns == null) return;
            foreach (var descriptor in grid.GridColumns)
                (descriptor as GridColumn)?.RefreshActualTotalSummaryContentStyle();
        }

        #endregion

        #region Grid surface — fixed total summary panel

        /// <summary>
        /// Shows the fixed total summary panel — a single horizontal, non-scrolling run beneath
        /// the items rendering the grid's own <see cref="FixedTotalSummaries"/> definitions.
        /// Unlike the column-aligned row there is no auto-collapse: an explicitly shown panel
        /// stays visible even while empty, so its right-click menu (Count toggle, Customize…)
        /// remains reachable. Default <c>false</c>.
        /// </summary>
        public static readonly DependencyProperty ShowFixedTotalSummaryProperty =
            DependencyProperty.Register(
                nameof(ShowFixedTotalSummary),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false, OnFixedTotalSummaryGateChanged));

        public bool ShowFixedTotalSummary
        {
            get => (bool)GetValue(ShowFixedTotalSummaryProperty);
            set => SetValue(ShowFixedTotalSummaryProperty, value);
        }

        /// <summary>
        /// Master feature gate for the fixed total summary panel (default <c>true</c>). When
        /// <c>false</c> the panel never shows even if <see cref="ShowFixedTotalSummary"/> is set,
        /// the column-header menu's "Show Fixed Total Summary" entry is removed, and its
        /// show/customize commands report <c>CanExecute=false</c>. <see cref="ShowFixedTotalSummary"/>
        /// remains the per-instance visibility toggle <em>within</em> an enabled feature; the
        /// resolved value the template binds is <see cref="ActualShowFixedTotalSummary"/>.
        /// </summary>
        public static readonly DependencyProperty AllowFixedTotalSummaryProperty =
            DependencyProperty.Register(
                nameof(AllowFixedTotalSummary),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true, OnFixedTotalSummaryGateChanged));

        public bool AllowFixedTotalSummary
        {
            get => (bool)GetValue(AllowFixedTotalSummaryProperty);
            set => SetValue(AllowFixedTotalSummaryProperty, value);
        }

        private static void OnFixedTotalSummaryGateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
                grid.RefreshActualShowFixedTotalSummary();
        }

        private static readonly DependencyPropertyKey ActualShowFixedTotalSummaryPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualShowFixedTotalSummary),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false));

        /// <summary>Read-only dependency property exposing <see cref="ActualShowFixedTotalSummary"/> for bindings.</summary>
        public static readonly DependencyProperty ActualShowFixedTotalSummaryProperty = ActualShowFixedTotalSummaryPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved visibility of the fixed total summary panel:
        /// <see cref="ShowFixedTotalSummary"/> AND <see cref="AllowFixedTotalSummary"/>. The grid
        /// template binds the panel's Visibility here.
        /// </summary>
        public bool ActualShowFixedTotalSummary => (bool)GetValue(ActualShowFixedTotalSummaryProperty);

        private void RefreshActualShowFixedTotalSummary()
            => SetValue(ActualShowFixedTotalSummaryPropertyKey, ShowFixedTotalSummary && AllowFixedTotalSummary);

        /// <summary>
        /// Master gate for the summary right-click menus (default <c>true</c>). When <c>false</c>,
        /// the total-summary cell, fixed panel, and group-footer cell context menus are suppressed
        /// and the summary-editing entries are removed from the column-header and group-header
        /// menus — so the configured summaries become read-only to the end user. Purely a UX gate;
        /// declarative and programmatic summary configuration is unaffected.
        /// </summary>
        public static readonly DependencyProperty SummaryContextMenusEnabledProperty =
            DependencyProperty.Register(
                nameof(SummaryContextMenusEnabled),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true));

        public bool SummaryContextMenusEnabled
        {
            get => (bool)GetValue(SummaryContextMenusEnabledProperty);
            set => SetValue(SummaryContextMenusEnabledProperty, value);
        }

        /// <summary>
        /// The fixed panel's own summary definitions — independent of any column's
        /// <see cref="GridColumn.TotalSummaries"/>. Each entry aggregates its
        /// <see cref="SummaryItem.FieldName"/> target (rendered caption-qualified); an entry
        /// with no FieldName and <see cref="SummaryItemType.Count"/> is the grid row count.
        /// Seeded with an empty collection at construction; the panel's Customize… editor and
        /// Count menu item write here.
        /// </summary>
        public static readonly DependencyProperty FixedTotalSummariesProperty =
            DependencyProperty.Register(
                nameof(FixedTotalSummaries),
                typeof(FreezableCollection<SummaryItem>),
                typeof(SearchDataGrid),
                new PropertyMetadata(null, OnFixedTotalSummariesChanged));

        public FreezableCollection<SummaryItem> FixedTotalSummaries
        {
            get => (FreezableCollection<SummaryItem>)GetValue(FixedTotalSummariesProperty);
            set => SetValue(FixedTotalSummariesProperty, value);
        }

        private static void OnFixedTotalSummariesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            if (e.OldValue is FreezableCollection<SummaryItem> oldItems)
                oldItems.Changed -= grid.OnFixedTotalSummaryDefinitionsChanged;
            if (e.NewValue is FreezableCollection<SummaryItem> newItems)
                newItems.Changed += grid.OnFixedTotalSummaryDefinitionsChanged;
            grid.ScheduleSummaryUpdate();
        }

        private void OnFixedTotalSummaryDefinitionsChanged(object sender, EventArgs e)
            => ScheduleSummaryUpdate();

        private static readonly DependencyPropertyKey FixedTotalSummaryLeftTextPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(FixedTotalSummaryLeftText),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="FixedTotalSummaryLeftText"/> for bindings.</summary>
        public static readonly DependencyProperty FixedTotalSummaryLeftTextProperty = FixedTotalSummaryLeftTextPropertyKey.DependencyProperty;

        /// <summary>Left-side run of the fixed total summary panel (entries with <see cref="SummaryItemAlignment.Left"/>).</summary>
        public string FixedTotalSummaryLeftText => (string)GetValue(FixedTotalSummaryLeftTextProperty);

        private static readonly DependencyPropertyKey FixedTotalSummaryRightTextPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(FixedTotalSummaryRightText),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="FixedTotalSummaryRightText"/> for bindings.</summary>
        public static readonly DependencyProperty FixedTotalSummaryRightTextProperty = FixedTotalSummaryRightTextPropertyKey.DependencyProperty;

        /// <summary>Right-side run of the fixed total summary panel (entries with <see cref="SummaryItemAlignment.Right"/>, the default).</summary>
        public string FixedTotalSummaryRightText => (string)GetValue(FixedTotalSummaryRightTextProperty);

        private static readonly DependencyPropertyKey FixedTotalSummaryLeftInfoPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(FixedTotalSummaryLeftInfo),
                typeof(IReadOnlyList<SummaryResult>),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="FixedTotalSummaryLeftInfo"/> for bindings.</summary>
        public static readonly DependencyProperty FixedTotalSummaryLeftInfoProperty = FixedTotalSummaryLeftInfoPropertyKey.DependencyProperty;

        /// <summary>
        /// Structured per-entry results behind <see cref="FixedTotalSummaryLeftText"/> — the data
        /// the fixed panel renders as styled per-segment runs.
        /// </summary>
        public IReadOnlyList<SummaryResult> FixedTotalSummaryLeftInfo => (IReadOnlyList<SummaryResult>)GetValue(FixedTotalSummaryLeftInfoProperty);

        private static readonly DependencyPropertyKey FixedTotalSummaryRightInfoPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(FixedTotalSummaryRightInfo),
                typeof(IReadOnlyList<SummaryResult>),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="FixedTotalSummaryRightInfo"/> for bindings.</summary>
        public static readonly DependencyProperty FixedTotalSummaryRightInfoProperty = FixedTotalSummaryRightInfoPropertyKey.DependencyProperty;

        /// <summary>
        /// Structured per-entry results behind <see cref="FixedTotalSummaryRightText"/> — the data
        /// the fixed panel renders as styled per-segment runs.
        /// </summary>
        public IReadOnlyList<SummaryResult> FixedTotalSummaryRightInfo => (IReadOnlyList<SummaryResult>)GetValue(FixedTotalSummaryRightInfoProperty);

        #endregion

        #region Grid surface — group summaries

        /// <summary>
        /// Group-header summary definitions, rendered identically in EVERY group header at
        /// every level. Each entry aggregates its <see cref="SummaryItem.FieldName"/> target
        /// over the group's leaf rows (caption-qualified <c>Function(Caption)=value</c>).
        /// Seeded with an empty collection at construction; the group header's View Totals…
        /// editor writes here.
        /// </summary>
        public static readonly DependencyProperty GroupSummariesProperty =
            DependencyProperty.Register(
                nameof(GroupSummaries),
                typeof(FreezableCollection<SummaryItem>),
                typeof(SearchDataGrid),
                new PropertyMetadata(null, OnGridGroupSummariesChanged));

        public FreezableCollection<SummaryItem> GroupSummaries
        {
            get => (FreezableCollection<SummaryItem>)GetValue(GroupSummariesProperty);
            set => SetValue(GroupSummariesProperty, value);
        }

        private static void OnGridGroupSummariesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            if (e.OldValue is FreezableCollection<SummaryItem> oldItems)
                oldItems.Changed -= grid.OnGroupSummaryDefinitionsChanged;
            if (e.NewValue is FreezableCollection<SummaryItem> newItems)
                newItems.Changed += grid.OnGroupSummaryDefinitionsChanged;
            grid.RequestGroupSummaryRefresh();
        }

        private void OnGroupSummaryDefinitionsChanged(object sender, EventArgs e)
            => RequestGroupSummaryRefresh();

        /// <summary>
        /// Where <see cref="GroupSummaries"/> render: <see cref="GroupSummaryDisplayMode.Header"/>
        /// (default) keeps every entry inline in the group headers' left/right runs;
        /// <see cref="GroupSummaryDisplayMode.AlignByColumns"/> moves the entries that target a
        /// column into the header row's aligned layer — each value sits under its target column
        /// and scrolls with it. The row count (<see cref="ShowGroupRowCount"/>) and entries that
        /// don't resolve to a column stay in the header runs either way.
        /// </summary>
        public static readonly DependencyProperty GroupSummaryDisplayModeProperty =
            DependencyProperty.Register(
                nameof(GroupSummaryDisplayMode),
                typeof(GroupSummaryDisplayMode),
                typeof(SearchDataGrid),
                new PropertyMetadata(GroupSummaryDisplayMode.Header, OnGroupSummaryDisplayModeChanged));

        public GroupSummaryDisplayMode GroupSummaryDisplayMode
        {
            get => (GroupSummaryDisplayMode)GetValue(GroupSummaryDisplayModeProperty);
            set => SetValue(GroupSummaryDisplayModeProperty, value);
        }

        /// <summary>
        /// Opt-in row count summary for group headers ("Show row count" in the View Totals
        /// editor). When <c>true</c>, every group header renders a count entry (default
        /// <c>Count=N</c>, right-aligned) alongside <see cref="GroupSummaries"/>;
        /// <see cref="GroupRowCountSummary"/> carries its prefix / format / suffix / side /
        /// order. Default <c>false</c> — the count is a summary the user adds, not default
        /// chrome.
        /// </summary>
        public static readonly DependencyProperty ShowGroupRowCountProperty =
            DependencyProperty.Register(
                nameof(ShowGroupRowCount),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false, OnGroupSummaryConfigChanged));

        public bool ShowGroupRowCount
        {
            get => (bool)GetValue(ShowGroupRowCountProperty);
            set => SetValue(ShowGroupRowCountProperty, value);
        }

        /// <summary>
        /// Formatting / placement configuration for the <see cref="ShowGroupRowCount"/> entry —
        /// a <see cref="SummaryItem"/> whose <c>SummaryType</c> is implicitly Count. Null (the
        /// default) renders <c>Count=N</c> right-aligned at order 0. The View Totals editor
        /// writes one here when the user customizes the count entry.
        /// </summary>
        public static readonly DependencyProperty GroupRowCountSummaryProperty =
            DependencyProperty.Register(
                nameof(GroupRowCountSummary),
                typeof(SummaryItem),
                typeof(SearchDataGrid),
                new PropertyMetadata(null, OnGroupRowCountSummaryChanged));

        public SummaryItem GroupRowCountSummary
        {
            get => (SummaryItem)GetValue(GroupRowCountSummaryProperty);
            set => SetValue(GroupRowCountSummaryProperty, value);
        }

        private static void OnGroupRowCountSummaryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            // Track in-place edits (prefix / format / side) the same way the collections do —
            // Freezable.Changed fires for item DP edits.
            if (e.OldValue is SummaryItem oldItem)
                oldItem.Changed -= grid.OnGroupRowCountSummaryItemChanged;
            if (e.NewValue is SummaryItem newItem)
                newItem.Changed += grid.OnGroupRowCountSummaryItemChanged;
            grid.RequestGroupSummaryRefresh();
        }

        private void OnGroupRowCountSummaryItemChanged(object sender, EventArgs e)
            => RequestGroupSummaryRefresh();

        private static void OnGroupSummaryConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
                grid.RequestGroupSummaryRefresh();
        }

        private static void OnGroupSummaryDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            // The aligned layer's cells exist only in AlignByColumns mode — rebuild them on a
            // mode flip, then recompute which surface each entry renders on.
            grid.InvalidateGroupSummaryPresenters();
            grid.RequestGroupSummaryRefresh();
        }

        #endregion

        #region Recompute scheduling

        private bool _summaryUpdateScheduled;
        private bool _groupSummaryRefreshScheduled;

        /// <summary>Synchronously recomputes the totals row + fixed panel. The explicit public trigger.</summary>
        public void RefreshSummaries() => UpdateTotalSummaries();

        /// <summary>
        /// Coalesced recompute — many triggers (collection churn, filter passes, edit commits)
        /// fire in bursts, and one pass per dispatcher tick at Background priority is enough.
        /// Background also lets a just-committed cell edit land in the row item before the
        /// recompute reads it.
        /// </summary>
        internal void ScheduleSummaryUpdate()
        {
            if (_summaryUpdateScheduled) return;
            _summaryUpdateScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _summaryUpdateScheduled = false;
                UpdateTotalSummaries();
            }), DispatcherPriority.Background);
        }

        /// <summary>Descriptor callback: a column's <see cref="GridColumn.TotalSummaries"/> changed.</summary>
        internal void OnColumnTotalSummariesChanged(GridColumn column) => ScheduleSummaryUpdate();

        /// <summary>
        /// Descriptor callback: a column's <see cref="GridColumn.GroupFooterSummaries"/> changed
        /// (declarative, the footer cell's runtime picker, or the footer editor). Footer presence
        /// and per-group values both live in the projection, so a reflatten adds/removes the
        /// footer rows and recomputes every group's results in one cheap pass. A no-op while
        /// ungrouped — footers exist only in grouped mode.
        /// </summary>
        internal void OnColumnGroupFooterSummariesChanged(GridColumn column)
        {
            if (_groupingActive) RebuildRowProjection();
        }

        /// <summary>
        /// Coalesced in-place group-summary recompute — definition edits and committed cell
        /// edits both land here. Texts refresh through the live <see cref="GroupNode"/>s (see
        /// <see cref="RefreshGroupSummaryTexts"/>) rather than a reflatten, so the row
        /// containers — and any in-progress editing session — survive untouched. Background
        /// priority runs after DataBind, so a just-committed edit has landed in the row item.
        /// </summary>
        private void RequestGroupSummaryRefresh()
        {
            if (!_groupingActive || _groupSummaryRefreshScheduled) return;
            _groupSummaryRefreshScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _groupSummaryRefreshScheduled = false;
                if (_groupingActive) RefreshGroupSummaryTexts();
            }), DispatcherPriority.Background);
        }

        #endregion

        #region Total summary computation

        /// <summary>
        /// Recomputes the totals row (each column's <see cref="GridColumn.TotalSummaries"/>,
        /// honoring per-item <see cref="SummaryItem.FieldName"/> targets) and the fixed panel
        /// (<see cref="FixedTotalSummaries"/>) over the filtered leaf rows, then refreshes the
        /// row's resolved visibility. Rows are materialized once; values are extracted once per
        /// distinct target path and shared across every summary item.
        /// </summary>
        private void UpdateTotalSummaries()
        {
            var descriptors = GridColumns;
            if (descriptors == null)
            {
                RefreshSummaryRowVisibility();
                return;
            }

            var culture = CultureInfo.CurrentCulture;
            List<object> rows = null;
            var valueCache = new Dictionary<string, List<object>>();

            List<object> Rows() => rows ??= EnumerateFilteredLeafRows().ToList();

            foreach (var entry in descriptors)
            {
                if (entry is not GridColumn descriptor) continue;

                if (descriptor.TotalSummaries is { Count: > 0 } items)
                {
                    string ownPath = descriptor.ResolveSummaryPath();
                    var results = new List<SummaryResult>(items.Count);
                    foreach (var item in items)
                    {
                        if (item == null) continue;

                        string targetPath = string.IsNullOrEmpty(item.FieldName) ? ownPath : item.FieldName;
                        var values = GetColumnValues(valueCache, Rows(), targetPath);
                        var value = SummaryCalculator.Compute(item.SummaryType, values);

                        // Entries aggregating the cell's own column render bare (Min=…); foreign
                        // targets carry their caption (Min(Discount)=…) to stay readable.
                        bool foreign = !string.Equals(targetPath ?? string.Empty, ownPath ?? string.Empty, StringComparison.Ordinal);
                        var target = foreign
                            ? FindDescriptorByFieldPath(targetPath) ?? descriptor
                            : descriptor;
                        results.Add(BuildSummaryResult(target, item, value, culture, includeCaption: foreign));
                    }

                    descriptor.SetTotalSummaryTextInfo(results);
                    descriptor.SetTotalSummaryText(string.Join("   ", results.Select(r => r.Text)));
                }
                else if (descriptor.TotalSummaryTextInfo != null)
                {
                    // Definitions were cleared — drop the stale computed state.
                    descriptor.SetTotalSummaryTextInfo(null);
                    descriptor.SetTotalSummaryText(null);
                }
            }

            // Fixed panel: its own definition set, independent of the column cells.
            var fixedItems = FixedTotalSummaries;
            if (fixedItems is { Count: > 0 })
            {
                var fixedEntries = BuildTargetedEntries(fixedItems, Rows(), valueCache, culture);
                var run = BuildRun(fixedEntries, "   ");
                SetValue(FixedTotalSummaryLeftTextPropertyKey, run.LeftText);
                SetValue(FixedTotalSummaryRightTextPropertyKey, run.RightText);
                SetValue(FixedTotalSummaryLeftInfoPropertyKey, run.LeftInfo);
                SetValue(FixedTotalSummaryRightInfoPropertyKey, run.RightInfo);
            }
            else
            {
                SetValue(FixedTotalSummaryLeftTextPropertyKey, null);
                SetValue(FixedTotalSummaryRightTextPropertyKey, null);
                SetValue(FixedTotalSummaryLeftInfoPropertyKey, null);
                SetValue(FixedTotalSummaryRightInfoPropertyKey, null);
            }

            RefreshSummaryRowVisibility();
        }

        private void RefreshSummaryRowVisibility()
        {
            SetValue(ActualShowTotalSummaryRowPropertyKey,
                AllowTotalSummary && ShowTotalSummary && TotalSummaryPosition != TotalSummaryPosition.None);
        }

        /// <summary>
        /// Reveals the column-aligned total summary row when it's currently suppressed. Called
        /// when a summary is added through a surface that isn't itself visible — the header
        /// menu's "Customize Totals…" editor can add a column total while the row is collapsed,
        /// and the new total would otherwise land on a hidden surface. Mirrors the show branch
        /// of <see cref="Commands.ContextMenuCommands.ToggleTotalSummaryRowCommand"/>: re-arms
        /// <see cref="TotalSummaryPosition"/> from <c>None</c> so the opt-in actually surfaces.
        /// </summary>
        internal void EnsureTotalSummaryRowVisible()
        {
            if (!AllowTotalSummary) return;
            ShowTotalSummary = true;
            if (TotalSummaryPosition == TotalSummaryPosition.None)
                TotalSummaryPosition = TotalSummaryPosition.Bottom;
        }

        /// <summary>
        /// The rows summaries aggregate over — the filtered leaf set, matching
        /// <see cref="FilteredItemCount"/>: grouped → recursive leaves of the group tree
        /// (collapse-independent, never the header sentinels); ungrouped → the source through
        /// the active filter.
        /// </summary>
        private IEnumerable<object> EnumerateFilteredLeafRows()
        {
            if (originalItemsSource == null) yield break;

            if (_groupingActive)
            {
                foreach (var root in _groupRoots)
                {
                    foreach (var leaf in EnumerateLeafRows(root))
                        yield return leaf;
                }
                yield break;
            }

            var filter = SearchFilter;
            foreach (var item in originalItemsSource)
            {
                if (filter == null || SafeFilter(filter, item))
                    yield return item;
            }
        }

        private static IEnumerable<object> EnumerateLeafRows(GroupNode node)
        {
            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    foreach (var leaf in EnumerateLeafRows(child))
                        yield return leaf;
                }
            }
            else
            {
                foreach (var item in node.Items)
                    yield return item;
            }
        }

        /// <summary>
        /// One value per row at <paramref name="path"/>, cached per distinct path so multiple
        /// summary items over the same field share one extraction. An empty path yields a
        /// null-per-row list (Count counts rows; value aggregates read null).
        /// </summary>
        private static List<object> GetColumnValues(
            Dictionary<string, List<object>> cache, List<object> rows, string path)
        {
            string key = path ?? string.Empty;
            if (cache.TryGetValue(key, out var values))
                return values;

            values = ExtractColumnValues(rows, path);
            cache[key] = values;
            return values;
        }

        private static List<object> ExtractColumnValues(List<object> rows, string path)
        {
            if (!string.IsNullOrEmpty(path))
                return SummaryCalculator.ExtractValues(rows, path);

            var values = new List<object>(rows.Count);
            for (int i = 0; i < rows.Count; i++) values.Add(null);
            return values;
        }

        /// <summary>
        /// Builds run entries from a grid-level definition collection (fixed panel / group
        /// headers): each item aggregates its <see cref="SummaryItem.FieldName"/> target,
        /// rendered caption-qualified when the target resolves to a column. A no-FieldName
        /// <see cref="SummaryItemType.Count"/> renders as the bare row count.
        /// </summary>
        private List<SummaryRunEntry> BuildTargetedEntries(
            IList<SummaryItem> items,
            IReadOnlyList<object> rows,
            Dictionary<string, List<object>> valueCache,
            CultureInfo culture)
        {
            var entries = new List<SummaryRunEntry>(items.Count);
            var rowsList = rows as List<object> ?? rows.ToList();

            int itemIndex = -1;
            foreach (var item in items)
            {
                itemIndex++;
                if (item == null) continue;

                var values = GetColumnValues(valueCache, rowsList, item.FieldName);
                var value = SummaryCalculator.Compute(item.SummaryType, values);
                var target = FindDescriptorByFieldPath(item.FieldName);
                entries.Add(new SummaryRunEntry(
                    item.OrderIndex, 0, itemIndex, item.Alignment,
                    BuildSummaryResult(target, item, value, culture, includeCaption: target != null)));
            }
            return entries;
        }

        /// <summary>
        /// Resolves a <see cref="SummaryItem.FieldName"/> aggregation target back to its column
        /// descriptor (for caption + display-format fallback). Matches on
        /// <see cref="ColumnDataBase.FieldName"/> first, then the resolved summary path for
        /// binding-only columns. Null when no column carries the field.
        /// </summary>
        internal GridColumn FindDescriptorByFieldPath(string fieldPath)
        {
            if (string.IsNullOrEmpty(fieldPath)) return null;
            var descriptors = GridColumns;
            if (descriptors == null) return null;

            foreach (var entry in descriptors)
            {
                if (entry is GridColumn descriptor
                    && (descriptor.FieldName == fieldPath || descriptor.ResolveSummaryPath() == fieldPath))
                    return descriptor;
            }
            return null;
        }

        #endregion

        #region Entry formatting

        /// <summary>
        /// Formats one summary entry. Resolution: a composite <see cref="SummaryItem.DisplayFormat"/>
        /// (contains <c>{0</c>) produces the value text wholesale; otherwise the value formats
        /// through the item format, falling back to the target column's <c>DisplayStringFormat</c>
        /// for value aggregates (never Count — it's a row count, not a column value). When the
        /// item carries a <see cref="SummaryItem.Prefix"/> / <see cref="SummaryItem.Suffix"/>,
        /// the text is <c>Prefix + value + Suffix</c>; otherwise the default
        /// <c>Function=value</c> (or <c>Function(Caption)=value</c> when caption-qualified).
        /// </summary>
        /// <summary>The three display segments of one summary entry — prefix, value, suffix.</summary>
        private readonly struct SummarySegments
        {
            public SummarySegments(string prefix, string value, string suffix)
            {
                Prefix = prefix;
                Value = value;
                Suffix = suffix;
            }

            public string Prefix { get; }
            public string Value { get; }
            public string Suffix { get; }
        }

        private static SummarySegments FormatSummaryEntry(
            GridColumn descriptor, SummaryItem item, object value, CultureInfo culture, bool includeCaption)
        {
            string format = item.DisplayFormat;
            bool hasFix = !string.IsNullOrEmpty(item.Prefix) || !string.IsNullOrEmpty(item.Suffix);

            string valueText = null;
            if (!string.IsNullOrEmpty(format) && format.IndexOf("{0", StringComparison.Ordinal) >= 0)
            {
                try { valueText = string.Format(culture, format, value); }
                catch (FormatException) { valueText = null; }
                // A composite format without prefix/suffix IS the whole value text (legacy behavior).
                if (valueText != null && !hasFix)
                    return new SummarySegments(string.Empty, valueText, string.Empty);
            }

            if (valueText == null)
            {
                if (string.IsNullOrEmpty(format) && item.SummaryType != SummaryItemType.Count)
                    format = descriptor?.DisplayStringFormat;
                valueText = FormatValue(value, format, culture);
            }

            if (hasFix)
                return new SummarySegments(item.Prefix ?? string.Empty, valueText, item.Suffix ?? string.Empty);

            string function = FunctionCaption(item.SummaryType);
            string caption = includeCaption ? descriptor?.HeaderCaption : null;
            string label = string.IsNullOrEmpty(caption) ? function : function + "(" + caption + ")";
            return new SummarySegments(label + "=", valueText, string.Empty);
        }

        /// <summary>
        /// Builds the structured <see cref="SummaryResult"/> for one entry: the formatted prefix /
        /// value / suffix segments (<see cref="FormatSummaryEntry"/>) paired with the item's three
        /// per-segment <see cref="SummaryTextStyle"/>s.
        /// </summary>
        private static SummaryResult BuildSummaryResult(
            GridColumn descriptor, SummaryItem item, object value, CultureInfo culture, bool includeCaption)
        {
            var segments = FormatSummaryEntry(descriptor, item, value, culture, includeCaption);
            return new SummaryResult(
                item.SummaryType, value,
                segments.Prefix, segments.Value, segments.Suffix,
                item.PrefixStyle, item.ValueStyle, item.SuffixStyle);
        }

        internal static string FunctionCaption(SummaryItemType type)
        {
            switch (type)
            {
                case SummaryItemType.Count: return "Count";
                case SummaryItemType.Sum: return "Sum";
                case SummaryItemType.Min: return "Min";
                case SummaryItemType.Max: return "Max";
                case SummaryItemType.Average: return "Avg";
                default: return type.ToString();
            }
        }

        internal static string FormatValue(object value, string format, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
            {
                try { return formattable.ToString(format, culture); }
                catch (FormatException) { /* fall through to the culture default */ }
            }
            return Convert.ToString(value, culture);
        }

        #endregion

        #region Summary runs (ordering + left/right split)

        /// <summary>One formatted entry in a horizontal summary run, carrying its sort keys and side.</summary>
        private readonly struct SummaryRunEntry
        {
            public SummaryRunEntry(int orderIndex, int columnIndex, int itemIndex, SummaryItemAlignment alignment, SummaryResult result)
            {
                OrderIndex = orderIndex;
                ColumnIndex = columnIndex;
                ItemIndex = itemIndex;
                Alignment = alignment;
                Result = result;
            }

            public int OrderIndex { get; }
            public int ColumnIndex { get; }
            public int ItemIndex { get; }
            public SummaryItemAlignment Alignment { get; }
            public SummaryResult Result { get; }
        }

        /// <summary>
        /// A horizontal summary run split by side, in both representations: the joined strings
        /// (tooltips / flat fallback) and the ordered structured entries (styled per-segment
        /// rendering). Either side is null when it carries no entries, so templates collapse the slot.
        /// </summary>
        private readonly struct SummaryRun
        {
            public SummaryRun(
                string leftText, string rightText,
                IReadOnlyList<SummaryResult> leftInfo, IReadOnlyList<SummaryResult> rightInfo)
            {
                LeftText = leftText;
                RightText = rightText;
                LeftInfo = leftInfo;
                RightInfo = rightInfo;
            }

            public string LeftText { get; }
            public string RightText { get; }
            public IReadOnlyList<SummaryResult> LeftInfo { get; }
            public IReadOnlyList<SummaryResult> RightInfo { get; }
        }

        /// <summary>
        /// Sorts by <c>OrderIndex</c> (the editor-written run position), tie-breaking by column
        /// order then declaration order, and splits into left / right runs — each as both the
        /// joined string (tooltip / flat fallback) and the ordered structured entries (styled
        /// rendering). Either side is null when it has no entries, so templates collapse the slot.
        /// </summary>
        private static SummaryRun BuildRun(List<SummaryRunEntry> entries, string separator)
        {
            if (entries == null || entries.Count == 0) return new SummaryRun(null, null, null, null);

            entries.Sort((a, b) =>
            {
                int c = a.OrderIndex.CompareTo(b.OrderIndex);
                if (c != 0) return c;
                c = a.ColumnIndex.CompareTo(b.ColumnIndex);
                if (c != 0) return c;
                return a.ItemIndex.CompareTo(b.ItemIndex);
            });

            StringBuilder left = null, right = null;
            List<SummaryResult> leftInfo = null, rightInfo = null;
            foreach (var entry in entries)
            {
                var result = entry.Result;
                if (result == null) continue;
                if (entry.Alignment == SummaryItemAlignment.Left)
                {
                    left ??= new StringBuilder();
                    if (left.Length > 0) left.Append(separator);
                    left.Append(result.Text);
                    (leftInfo ??= new List<SummaryResult>()).Add(result);
                }
                else
                {
                    right ??= new StringBuilder();
                    if (right.Length > 0) right.Append(separator);
                    right.Append(result.Text);
                    (rightInfo ??= new List<SummaryResult>()).Add(result);
                }
            }
            return new SummaryRun(left?.ToString(), right?.ToString(), leftInfo, rightInfo);
        }

        #endregion

        #region Group summary computation

        /// <summary>
        /// True while the current grouping projection computes per-node summary text. Resolved
        /// once per <c>RebuildRowProjection</c> pass (row count enabled or any
        /// <see cref="GroupSummaries"/> defined) so grids with neither skip the per-node leaf
        /// walks entirely.
        /// </summary>
        private bool _projectGroupSummaries;

        /// <summary>
        /// True while group summaries render column-aligned in the header rows —
        /// <see cref="GroupSummaryDisplayMode"/> is AlignByColumns and at least one
        /// <see cref="GroupSummaries"/> entry targets a column. Resolved once per
        /// <c>RebuildRowProjection</c> pass and per in-place refresh.
        /// </summary>
        private bool _alignGroupSummariesByColumns;

        /// <summary>
        /// True while any column defines <see cref="GridColumn.GroupFooterSummaries"/> — the
        /// projection emits a footer row per group and computes per-column footer results.
        /// Resolved once per <c>RebuildRowProjection</c> pass and per in-place refresh.
        /// </summary>
        private bool _projectGroupFooterSummaries;

        /// <summary>Whether any group-header summary content is configured.</summary>
        internal bool HasAnyGroupSummaryContent()
            => ShowGroupRowCount || GroupSummaries is { Count: > 0 };

        /// <summary>Whether any column defines group-footer summaries (drives the footer projection).</summary>
        internal bool HasAnyGroupFooterContent()
        {
            var descriptors = GridColumns;
            if (descriptors == null) return false;
            foreach (var entry in descriptors)
            {
                if (entry is GridColumn descriptor && descriptor.GroupFooterSummaries is { Count: > 0 })
                    return true;
            }
            return false;
        }

        /// <summary>Whether the current config calls for the column-aligned summary layer.</summary>
        private bool ResolveAlignGroupSummaries()
        {
            if (GroupSummaryDisplayMode != GroupSummaryDisplayMode.AlignByColumns) return false;
            if (GroupSummaries is not { Count: > 0 } items) return false;

            foreach (var item in items)
            {
                if (item != null && FindDescriptorByFieldPath(item.FieldName) != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Computes one group node's summary surfaces from its leaf rows: the header runs, and —
        /// in AlignByColumns mode — the column-aligned results (the header runs then keep only
        /// the row count and entries that don't resolve to a column).
        /// </summary>
        private void ComputeNodeSummaries(GroupNode node, IReadOnlyList<object> leafRows)
        {
            node.AlignedSummaryResults = _alignGroupSummariesByColumns ? BuildAlignedSummaryResults(leafRows) : null;
            var run = BuildGroupSummaryTexts(leafRows, excludeColumnTargets: _alignGroupSummariesByColumns);
            node.SummaryLeftText = run.LeftText;
            node.SummaryRightText = run.RightText;
            node.SummaryLeftInfo = run.LeftInfo;
            node.SummaryRightInfo = run.RightInfo;
            node.FooterSummaryResults = _projectGroupFooterSummaries ? BuildNodeFooterResults(leafRows) : null;
        }

        /// <summary>
        /// Computes one group's footer row from its leaf rows: every column's
        /// <see cref="GridColumn.GroupFooterSummaries"/> aggregated over the leaves, formatted the
        /// same way the total summary row formats its cells (own-column entries render bare,
        /// foreign <see cref="SummaryItem.FieldName"/> targets caption-qualified), bucketed by the
        /// owning column. Mirrors the per-column pass in <see cref="UpdateTotalSummaries"/> but
        /// scoped to this group. Null when no column carries footer entries.
        /// </summary>
        private Dictionary<GridColumn, List<SummaryResult>> BuildNodeFooterResults(IReadOnlyList<object> leafRows)
        {
            var descriptors = GridColumns;
            if (descriptors == null) return null;

            var culture = CultureInfo.CurrentCulture;
            var valueCache = new Dictionary<string, List<object>>();
            var rowsList = leafRows as List<object> ?? leafRows.ToList();
            Dictionary<GridColumn, List<SummaryResult>> results = null;

            foreach (var entry in descriptors)
            {
                if (entry is not GridColumn descriptor) continue;
                if (descriptor.GroupFooterSummaries is not { Count: > 0 } items) continue;

                string ownPath = descriptor.ResolveSummaryPath();
                var list = new List<SummaryResult>(items.Count);
                foreach (var item in items)
                {
                    if (item == null) continue;

                    string targetPath = string.IsNullOrEmpty(item.FieldName) ? ownPath : item.FieldName;
                    var values = GetColumnValues(valueCache, rowsList, targetPath);
                    var value = SummaryCalculator.Compute(item.SummaryType, values);

                    bool foreign = !string.Equals(targetPath ?? string.Empty, ownPath ?? string.Empty, StringComparison.Ordinal);
                    var target = foreign
                        ? FindDescriptorByFieldPath(targetPath) ?? descriptor
                        : descriptor;
                    list.Add(BuildSummaryResult(target, item, value, culture, includeCaption: foreign));
                }

                if (list.Count > 0)
                {
                    results ??= new Dictionary<GridColumn, List<SummaryResult>>();
                    results[descriptor] = list;
                }
            }

            return results;
        }

        /// <summary>
        /// Computes the column-aligned results for one group's leaf rows: every
        /// <see cref="GroupSummaries"/> entry whose <see cref="SummaryItem.FieldName"/> resolves
        /// to a column, bucketed by that column, formatted bare (no caption — the value sits
        /// under its column). Entries within a column order by <see cref="SummaryItem.OrderIndex"/>
        /// then declaration. Null when nothing resolves.
        /// </summary>
        private Dictionary<GridColumn, List<SummaryResult>> BuildAlignedSummaryResults(IReadOnlyList<object> leafRows)
        {
            if (GroupSummaries is not { Count: > 0 } items) return null;

            var culture = CultureInfo.CurrentCulture;
            var valueCache = new Dictionary<string, List<object>>();
            var rowsList = leafRows as List<object> ?? leafRows.ToList();
            Dictionary<GridColumn, List<(int order, int index, SummaryResult result)>> buckets = null;

            int itemIndex = -1;
            foreach (var item in items)
            {
                itemIndex++;
                if (item == null) continue;
                var target = FindDescriptorByFieldPath(item.FieldName);
                if (target == null) continue;

                var values = GetColumnValues(valueCache, rowsList, item.FieldName);
                var value = SummaryCalculator.Compute(item.SummaryType, values);
                var result = BuildSummaryResult(target, item, value, culture, includeCaption: false);

                buckets ??= new Dictionary<GridColumn, List<(int, int, SummaryResult)>>();
                if (!buckets.TryGetValue(target, out var list))
                    buckets[target] = list = new List<(int, int, SummaryResult)>();
                list.Add((item.OrderIndex, itemIndex, result));
            }

            if (buckets == null) return null;

            var resolved = new Dictionary<GridColumn, List<SummaryResult>>(buckets.Count);
            foreach (var pair in buckets)
            {
                pair.Value.Sort((a, b) =>
                {
                    int c = a.order.CompareTo(b.order);
                    return c != 0 ? c : a.index.CompareTo(b.index);
                });
                resolved[pair.Key] = pair.Value.Select(e => e.result).ToList();
            }
            return resolved;
        }

        /// <summary>
        /// Formats the group-summary runs for one group's leaf rows: the grid's
        /// <see cref="GroupSummaries"/> definitions — the SAME set at every level — plus the
        /// <see cref="ShowGroupRowCount"/> entry, ordered by <see cref="SummaryItem.OrderIndex"/>
        /// and split into (left, right) per each item's <see cref="SummaryItem.Alignment"/>.
        /// With <paramref name="excludeColumnTargets"/> (AlignByColumns mode), entries that
        /// resolve to a column are skipped — they render in the aligned layer instead. Returns
        /// (null, null) when nothing is configured so headers pay nothing.
        /// </summary>
        private SummaryRun BuildGroupSummaryTexts(
            IReadOnlyList<object> leafRows, bool excludeColumnTargets = false)
        {
            var culture = CultureInfo.CurrentCulture;
            List<SummaryRunEntry> entries;

            if (GroupSummaries is { Count: > 0 } allItems)
            {
                IList<SummaryItem> items = allItems;
                if (excludeColumnTargets)
                {
                    var headerOnly = new List<SummaryItem>();
                    foreach (var item in allItems)
                    {
                        if (item != null && FindDescriptorByFieldPath(item.FieldName) == null)
                            headerOnly.Add(item);
                    }
                    items = headerOnly;
                }

                var valueCache = new Dictionary<string, List<object>>();
                entries = BuildTargetedEntries(items, leafRows, valueCache, culture);
            }
            else
            {
                entries = new List<SummaryRunEntry>(1);
            }

            if (ShowGroupRowCount)
            {
                var config = GroupRowCountSummary;
                SummaryResult result = config != null
                    ? BuildSummaryResult(null, config, leafRows.Count, culture, includeCaption: false)
                    : new SummaryResult(
                        SummaryItemType.Count, leafRows.Count,
                        "Count=", leafRows.Count.ToString(culture), string.Empty,
                        null, null, null);
                // ColumnIndex -1 → the count leads any entry sharing its OrderIndex.
                entries.Add(new SummaryRunEntry(
                    config?.OrderIndex ?? 0, -1, 0,
                    config?.Alignment ?? SummaryItemAlignment.Right, result));
            }

            return BuildRun(entries, ",  ");
        }

        /// <summary>
        /// Recomputes every group node's summary runs in place and notifies the live header
        /// surfaces (in-body <see cref="GroupHeaderRow"/> sentinels + pinned
        /// <see cref="FixedGroupHeaderEntry"/> strip entries). No reflatten: group membership
        /// and counts can't change from a value edit, so only the texts move — and a projection
        /// reset here would recycle row containers mid-editing-session.
        /// </summary>
        private void RefreshGroupSummaryTexts()
        {
            // The display mode (or the entry set) may have changed since the last projection —
            // re-resolve which surface each entry renders on before recomputing.
            _alignGroupSummariesByColumns = ResolveAlignGroupSummaries();
            _projectGroupFooterSummaries = HasAnyGroupFooterContent();

            bool anyContent = HasAnyGroupSummaryContent() || _projectGroupFooterSummaries;
            RefreshNodeSummaryTexts(_groupRoots, anyContent);

            foreach (var row in _groupRows)
            {
                if (row is GroupHeaderRow header)
                    header.NotifySummaryTextsChanged();
                else if (row is GroupFooterRow footer)
                    footer.NotifyFooterResultsChanged();
            }
            foreach (var entry in _fixedGroupHeadersBacking)
                entry.NotifySummaryTextsChanged();
        }

        /// <summary>
        /// Walks the retained group tree — collapsed subtrees included, since their headers
        /// re-realize from these same nodes on expand — recomputing each node's summary runs
        /// (and aligned results in AlignByColumns mode), or clearing them when no group-summary
        /// content is configured anymore.
        /// </summary>
        private void RefreshNodeSummaryTexts(List<GroupNode> nodes, bool anyContent)
        {
            foreach (var node in nodes)
            {
                if (anyContent)
                {
                    var leaves = node.Children.Count > 0
                        ? EnumerateLeafRows(node).ToList()
                        : node.Items;
                    ComputeNodeSummaries(node, leaves);
                }
                else
                {
                    node.SummaryLeftText = null;
                    node.SummaryRightText = null;
                    node.SummaryLeftInfo = null;
                    node.SummaryRightInfo = null;
                    node.AlignedSummaryResults = null;
                    node.FooterSummaryResults = null;
                }

                if (node.Children.Count > 0)
                    RefreshNodeSummaryTexts(node.Children, anyContent);
            }
        }

        #endregion

        #region Sort groups by summary

        /// <summary>The active summary sort, or null when groups order by their key (the default).</summary>
        private GroupSummarySortDescriptor _groupSummarySort;

        private static readonly DependencyPropertyKey ActiveGroupSummarySortPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActiveGroupSummarySort),
                typeof(GroupSummarySortDescriptor),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="ActiveGroupSummarySort"/> for bindings.</summary>
        public static readonly DependencyProperty ActiveGroupSummarySortProperty = ActiveGroupSummarySortPropertyKey.DependencyProperty;

        /// <summary>
        /// The active summary sort (see <see cref="SortGroupsBySummary"/>), or null while groups
        /// order by their key. A fresh instance per change, so bindings — like the pill menu's
        /// Sort By Summary listing — re-evaluate on every set or clear.
        /// </summary>
        public GroupSummarySortDescriptor ActiveGroupSummarySort => (GroupSummarySortDescriptor)GetValue(ActiveGroupSummarySortProperty);

        /// <summary>
        /// Orders the groups at every level by a summary aggregate over each group's leaf rows
        /// instead of by the group key — e.g. largest <c>Sum(Total)</c> first. The group-key
        /// order (including the grouped column's sort direction) survives as the tie-breaker,
        /// and leaf rows inside each group keep their order. Applies on the next projection and
        /// persists across reflattens until <see cref="ClearGroupSummarySort"/>. The target
        /// column's <see cref="GridColumn.IsSortedBySummary"/> lights up while active.
        /// <see cref="SummaryItemType.Count"/> with a null <paramref name="fieldName"/> sorts by
        /// group row count.
        /// </summary>
        public void SortGroupsBySummary(
            SummaryItemType summaryType,
            string fieldName = null,
            ListSortDirection direction = ListSortDirection.Descending)
        {
            _groupSummarySort = new GroupSummarySortDescriptor(summaryType, fieldName, direction);
            SetValue(ActiveGroupSummarySortPropertyKey, _groupSummarySort);
            RefreshIsSortedBySummaryFlags();
            if (_groupingActive) RebuildRowProjection();
        }

        /// <summary>Restores group-key ordering (see <see cref="SortGroupsBySummary"/>).</summary>
        public void ClearGroupSummarySort()
        {
            if (!ClearGroupSummarySortCore()) return;
            if (_groupingActive) RebuildRowProjection();
        }

        /// <summary>
        /// Drops the summary-sort state without reflattening — for callers about to rebuild the
        /// projection themselves (a direct sort on a grouped column supersedes the summary
        /// sort, which would otherwise keep overriding the key order and make the user's click
        /// look dead). Returns <c>true</c> when there was a sort to clear.
        /// </summary>
        internal bool ClearGroupSummarySortCore()
        {
            if (_groupSummarySort == null) return false;
            _groupSummarySort = null;
            SetValue(ActiveGroupSummarySortPropertyKey, null);
            RefreshIsSortedBySummaryFlags();
            return true;
        }

        /// <summary>True while groups are ordered by a summary aggregate.</summary>
        public bool IsGroupSummarySortActive => _groupSummarySort != null;

        private void RefreshIsSortedBySummaryFlags()
        {
            var target = _groupSummarySort != null
                ? FindDescriptorByFieldPath(_groupSummarySort.FieldName)
                : null;

            var descriptors = GridColumns;
            if (descriptors == null) return;
            foreach (var entry in descriptors)
            {
                if (entry is GridColumn descriptor)
                    descriptor.SetIsSortedBySummary(ReferenceEquals(descriptor, target));
            }
        }

        /// <summary>
        /// Re-orders the freshly built group tree by the active summary sort. Stable, so the
        /// group-key ordering the projection produced remains the tie-breaker. Called from
        /// <c>RebuildRowProjection</c> between <c>BuildNodes</c> and the flatten.
        /// </summary>
        private void ApplyGroupSummarySort(List<GroupNode> nodes)
        {
            var spec = _groupSummarySort;
            if (spec == null || nodes.Count == 0) return;
            SortNodesBySummary(nodes, spec);
        }

        private void SortNodesBySummary(List<GroupNode> nodes, GroupSummarySortDescriptor spec)
        {
            if (nodes.Count > 1)
            {
                var keyed = new (GroupNode node, object key)[nodes.Count];
                for (int i = 0; i < nodes.Count; i++)
                    keyed[i] = (nodes[i], ComputeNodeSortKey(nodes[i], spec));

                // OrderBy is stable; List.Sort is not — stability is what keeps the group-key
                // order as the tie-breaker.
                int sign = spec.Direction == ListSortDirection.Descending ? -1 : 1;
                var ordered = keyed
                    .OrderBy(e => e.key, Comparer<object>.Create((x, y) => sign * SummaryCalculator.CompareValues(x, y)))
                    .ToList();

                nodes.Clear();
                foreach (var entry in ordered) nodes.Add(entry.node);
            }

            foreach (var node in nodes)
            {
                if (node.Children.Count > 0)
                    SortNodesBySummary(node.Children, spec);
            }
        }

        private object ComputeNodeSortKey(GroupNode node, GroupSummarySortDescriptor spec)
        {
            if (string.IsNullOrEmpty(spec.FieldName))
                return spec.SummaryType == SummaryItemType.Count ? node.Count : null;

            var leaves = node.Children.Count > 0
                ? EnumerateLeafRows(node).ToList()
                : node.Items;
            var values = SummaryCalculator.ExtractValues(leaves, spec.FieldName);
            return SummaryCalculator.Compute(spec.SummaryType, values);
        }

        /// <summary>
        /// Builds the "Sort By Summary" menu listing from the configured group-summary content:
        /// an Ascending + Descending pair per distinct <see cref="GroupSummaries"/> aggregate
        /// (e.g. <c>Sum by 'Total' - Ascending</c>; captioned by the target column, falling
        /// back to the field path), a Row Count pair when <see cref="ShowGroupRowCount"/> is on
        /// (or a no-FieldName Count entry exists), and a trailing Clear Summary Sort action.
        /// The active sort's option reports <see cref="GroupSummarySortOption.IsChecked"/>.
        /// Empty when nothing is configured — the submenu collapses.
        /// </summary>
        internal List<GroupSummarySortOption> BuildGroupSummarySortOptions()
        {
            var options = new List<GroupSummarySortOption>();
            var seen = new HashSet<(SummaryItemType type, string field)>();
            var active = _groupSummarySort;

            void AddPair(SummaryItemType type, string fieldName, string caption)
            {
                if (!seen.Add((type, fieldName ?? string.Empty))) return;

                string subject = fieldName == null
                    ? "Row Count"
                    : $"{FunctionCaption(type)} by '{caption}'";

                foreach (var direction in new[] { ListSortDirection.Ascending, ListSortDirection.Descending })
                {
                    options.Add(new GroupSummarySortOption(
                        this,
                        $"{subject} - {direction}",
                        isChecked: active?.Matches(type, fieldName, direction) == true,
                        isClear: false,
                        type, fieldName, direction));
                }
            }

            if (GroupSummaries is { Count: > 0 } items)
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    if (string.IsNullOrEmpty(item.FieldName))
                    {
                        // A no-FieldName entry only makes sense as Count — the row count.
                        if (item.SummaryType == SummaryItemType.Count)
                            AddPair(SummaryItemType.Count, null, null);
                        continue;
                    }

                    var target = FindDescriptorByFieldPath(item.FieldName);
                    string caption = target?.HeaderCaption;
                    if (string.IsNullOrEmpty(caption)) caption = item.FieldName;
                    AddPair(item.SummaryType, item.FieldName, caption);
                }
            }

            if (ShowGroupRowCount)
                AddPair(SummaryItemType.Count, null, null);

            if (options.Count > 0)
            {
                options.Add(new GroupSummarySortOption(
                    this, "Clear Summary Sort",
                    isChecked: false, isClear: true,
                    SummaryItemType.Count, null, ListSortDirection.Ascending));
            }

            return options;
        }

        #endregion

        #region Aligned group summaries (rendering registry)

        /// <summary>
        /// Realized aligned-summary presenters (one per realized group header row), registered
        /// on load so the grid can rebuild their per-column cells when the column layout
        /// changes (reorder, visibility, add/remove) or the display mode flips. Width changes
        /// flow through each cell's binding to its descriptor's ActualWidth and need no rebuild.
        /// </summary>
        private readonly List<GroupSummaryCellsPresenter> _groupSummaryPresenters = new();

        internal void RegisterGroupSummaryPresenter(GroupSummaryCellsPresenter presenter)
        {
            if (!_groupSummaryPresenters.Contains(presenter))
                _groupSummaryPresenters.Add(presenter);
        }

        internal void UnregisterGroupSummaryPresenter(GroupSummaryCellsPresenter presenter)
            => _groupSummaryPresenters.Remove(presenter);

        /// <summary>Rebuilds every live header row's aligned cells after a column-layout or mode change.</summary>
        internal void InvalidateGroupSummaryPresenters()
        {
            for (int i = _groupSummaryPresenters.Count - 1; i >= 0; i--)
                _groupSummaryPresenters[i].RebuildCells();
        }

        /// <summary>
        /// Realized footer presenters (one per realized group footer row), registered on load so
        /// the grid can rebuild their per-column cells when the column layout changes (reorder,
        /// visibility, add/remove). Width changes flow through each cell's width binding and need
        /// no rebuild.
        /// </summary>
        private readonly List<GroupFooterCellsPresenter> _groupFooterPresenters = new();

        internal void RegisterGroupFooterPresenter(GroupFooterCellsPresenter presenter)
        {
            if (!_groupFooterPresenters.Contains(presenter))
                _groupFooterPresenters.Add(presenter);
        }

        internal void UnregisterGroupFooterPresenter(GroupFooterCellsPresenter presenter)
            => _groupFooterPresenters.Remove(presenter);

        /// <summary>Rebuilds every live footer row's cells after a column-layout change.</summary>
        internal void InvalidateGroupFooterPresenters()
        {
            for (int i = _groupFooterPresenters.Count - 1; i >= 0; i--)
                _groupFooterPresenters[i].RebuildCells();
        }

        #endregion
    }
}
