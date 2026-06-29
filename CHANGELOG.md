# Changelog

## [Unreleased]

### Changed — Best fit (column auto-width engine)
- **Best Fit is measurement-based now.** The context-menu Best Fit / Best Fit All Columns no
  longer flip the column through `SizeToHeader` / `SizeToCells` with full `UpdateLayout()`
  passes (realized-rows-only, scroll-position-dependent, slow across many columns). The new
  engine (`SearchDataGrid.BestFit.cs`) measures realized cells and the column header directly
  with an infinite constraint (re-invalidating after, so live layout is undisturbed), and by
  default extends past the viewport: every filtered leaf row's display text is formatted
  through the same pipeline the cell renders with (mask > converter > edit-settings mask >
  string format > combo lookup) and measured as `FormattedText` (distinct strings only),
  calibrated with cell chrome derived from the realized cells. Result is clamped to
  Min/MaxWidth and frozen as a pixel width via the descriptor.
- **Public API**: `SearchDataGrid.BestFitColumn(GridColumn)` / `BestFitAllColumns()`, with
  overloads taking `BestFitArea` (All / Header / Rows), `BestFitMode` (Default / VisibleRows /
  AllRows), and a max-row-count cap. Columns that can't be text-measured (user cell templates,
  checkbox columns) automatically degrade to realized-only measurement.
- **`ColumnLayoutBase.ActualDataWidth`** (read-only DP): width of the widest measured data
  content including cell chrome; populated by best-fit runs (not live-tracked).
- The static `ContextMenuCommands.BestFitColumn(DataGrid, DataGridColumn)` helper was removed;
  both commands route through the grid API.
- **Configurable surface.** Column-level (`ColumnLayoutBase`): `AllowBestFit` (`bool?`,
  null = inherit) + read-only `ActualAllowBestFit`, `BestFitMode` (`Default` = inherit),
  `BestFitArea`, `BestFitMaxRowCount` (negative = unlimited). Grid-level: `AllowBestFit`
  (default `true`; runtime toggles re-resolve every column) and `BestFitMode` (default
  `AllRows` — explicit best-fit actions are accurate-by-default and don't depend on scroll
  position). `BestFitColumn(GridColumn)` / `BestFitAllColumns()` read the column DPs; the
  explicit-options overloads override them. `BestFitAllColumns` skips columns whose
  `ActualAllowBestFit` is `false` (it backs the "Best Fit (all columns)" menu); the
  single-column API runs regardless — the flag gates UI gestures, not code.
- **Gripper double-click is best-fit now.** Double-clicking a column-resize gripper runs the
  measurement-based best-fit instead of WPF's stock `Width = Auto` (realized-cells-only). The
  left gripper targets the previous visible column, matching stock semantics. Columns with
  `ActualAllowBestFit = false` keep the stock auto-size (opt-out disables the smarter fit, not
  resizing). The context-menu items are **removed** (collapsed), not disabled, when gated off —
  "Best Fit Column" per column via `ActualAllowBestFit`, "Best Fit All Columns" via grid
  `AllowBestFit` — in both the column-header menu and the group-panel pill menu; the section
  separator collapses with them. `CanExecute` stays as a backstop.
- **Auto best-fit on data load.** `SearchDataGrid.BestFitModeOnSourceChange` (default
  `Default` = off): when set to `VisibleRows` / `AllRows`, every real `ItemsSource` change
  schedules a coalesced best-fit pass over all columns at dispatcher idle (after column
  generation, filters, the group projection, and row realization settle; not-yet-loaded grids
  defer to `Loaded`). The value acts as the pass's grid default — an explicit column-level
  `BestFitMode` still wins. Internal source swaps (the grouping projection) and
  `Items.Refresh()` don't re-fire it; opted-out columns are skipped.
- **Fill the viewport.** Grid-level `BestFitFillViewport` (default `false`): when `true`, a
  best-fit-_all_ pass (the `BestFitAllColumns()` API, the "Best Fit All Columns" menu, and an
  auto best-fit on source change) sizes each column to its content and then makes the
  participating columns fill the horizontal viewport instead of leaving empty space on the
  right. The layout is **regime-aware**, re-evaluated live as the viewport width changes:
  - **Content fits the viewport** → columns are star-sized weighted by content width so they
    stretch to fill it; the slack keeps them resizable (dragging one redistributes among the
    rest).
  - **Content exceeds the viewport** → columns are frozen at their content pixel width, so the
    grid overflows to a horizontal scrollbar with every column at full width and resizable as a
    normal pixel column — rather than shrinking columns below their content (what star sizing
    alone does) or jamming them rigidly against the viewport edge.

  The regime flips at the fit/overflow boundary as the grid resizes (driven off the scroll
  viewport's viewport-width change), without re-measuring. Opted-out / skipped columns keep
  their own width and the participating columns fill around them. Single-column `BestFitColumn`
  always freezes a pixel width and pins that column out of fill management. Changing the flag
  does not itself resize — call `BestFitAllColumns()` to apply.
- **Sample**: Columns → "Best Fit (Auto-Width)" — 2,000 rows with long Customer/Job outliers
  planted past the first viewport (VisibleRows vs AllRows comparison), per-column
  `BestFitMode` / `AllowBestFit` / `BestFitArea` overrides, a live `ActualDataWidth` readout,
  a "Fill viewport" toggle (`BestFitFillViewport`) over a splitter so the fill/overflow behavior
  is visible as the grid widens and narrows, and a Reload Data button with alternating outlier
  widths demonstrating `BestFitModeOnSourceChange`.

### Added — Summaries (total summary row + group summaries)
- **Summary model.** `SummaryItemType` enum (Count / Sum / Min / Max / Average) and
  `SummaryCalculator` (null-skipping aggregates, decimal-first with double overflow fallback,
  `IsTypeSupported` capability gate) in Core; `SummaryItem` Freezable (SummaryType +
  `DisplayFormat` — composite `{0:…}` replaces the whole text, a plain specifier formats the
  value, unset falls back to the column's `DisplayStringFormat`) and `SummaryResult`
  (type / raw value / formatted text) in WPF.
- **Column surface** (`GridColumn`): `TotalSummaries` + `GroupSummaries` (ctor-seeded
  `FreezableCollection<SummaryItem>`; item edits and collection mutations recompute via the
  Freezable `Changed` event), read-only `HasTotalSummaries` / `TotalSummaryText` /
  `TotalSummaryTextInfo`, `AllowedTotalSummaries` (`AllowedSummaries` flags, default `All` —
  gates the runtime picker only), `TotalSummaryContentStyle` + `ActualTotalSummaryContentStyle`
  (inherits the grid default), and a read-only `IsSortedBySummary` stub (sort-groups-by-summary
  is a later phase).
- **Total summary row.** Pinned beneath the data area (above the horizontal scrollbar), aligned
  per-column by the same header-mirroring panel the filter row uses —
  `FilterRowPresenter`'s tracking/layout core was extracted into a shared
  `ColumnAlignedRowPresenter` base, with `TotalSummaryRowPresenter` + per-column
  `TotalSummaryCell` as the second consumer. Grid surface: `ShowTotalSummary` (default `false`,
  explicit opt-in like the fixed panel — no content-based auto-collapse, so a shown row stays
  visible even with no summaries defined and the per-cell right-click picker remains reachable;
  resolved with `TotalSummaryPosition ≠ None` into read-only `ActualShowTotalSummaryRow`), plus
  grid-level `TotalSummaryContentStyle`. Theme: `Grid/TotalSummaryRow.xaml`
  (`GridSearchDataGridTotalSummaryRow` / `…TotalSummaryCell` / `…TotalSummaryCellContextMenu`).
- **Totals compute over the filtered leaf rows** — the same set `FilteredItemCount` reports
  (grouping-aware, collapse-independent, never the header sentinels). Recompute is coalesced
  per dispatcher tick and triggered by every filter / source / projection change, cell edit
  commits, and summary-definition edits; `RefreshSummaries()` is the explicit synchronous
  trigger.
- **Runtime summary picker.** Right-click a totals cell → toggle Count / Sum / Min / Max /
  Average (check glyph marks active aggregates) or Clear Summaries. Items disable when blocked
  by `AllowedTotalSummaries` or unsupported by the column's `FieldType`
  (`SummaryCalculator.IsTypeSupported`).
- **Group summaries are one grid-level set, shared by every level.**
  `SearchDataGrid.GroupSummaries` (ctor-seeded `FreezableCollection<SummaryItem>`) defines the
  summaries rendered identically in EVERY group header at every level; each `SummaryItem`
  declares its aggregation target via **`FieldName`** (caption-qualified
  `Function(Caption)=value`), so the set can span many columns. Computed per `GroupNode` at
  projection time, surfaced as `GroupSummaryLeftText` / `GroupSummaryRightText` on both
  `GroupHeaderRow` and `FixedGroupHeaderEntry` so the in-body header and the pinned strip
  render identically. Entries are **right-aligned at the header row's right edge by default**
  (per-item `SummaryItem.Alignment` can put them inline after the header content instead),
  ordered by `SummaryItem.OrderIndex`. Grids with neither group summaries nor the row count
  skip the per-node aggregation entirely. (There is no per-column group-summary property.)
  A committed cell edit (and any summary-definition edit) recomputes the texts **in place** —
  the live `GroupNode`s update and both header surfaces refresh via `INotifyPropertyChanged`
  with no reflatten, so row containers, focus, and scroll position survive the edit.
- **Totals cells can mix columns.** A `GridColumn.TotalSummaries` entry may target another
  column's field via `SummaryItem.FieldName` — it computes over that field but renders under
  the owning column's cell, caption-qualified (`Min(Discount)=…`); own-column entries stay
  bare (`Min=…`). Value extraction is cached per distinct target path per recompute.
  The cell's quick-pick menu (Count / Sum / Min / Max / Average) reads and toggles ONLY the
  column's own entries — a foreign-target entry like `Max(OrderDate)` rendered under the cell
  neither checks the cell's Max item nor gets removed by toggling it; cross-column entries
  are managed through the Customize editor. (Clear Summaries still clears the whole cell.)
- **Group header row count is a summary now, not chrome (behavior change).** The hardcoded
  `(N)` count chip is gone from the default group header. Row count is the opt-in
  `SearchDataGrid.ShowGroupRowCount` summary entry ("Show row count" in the View Totals
  editor; default `false` → default `Count=N`, right-aligned), with formatting / placement
  configurable via `SearchDataGrid.GroupRowCountSummary`. Consumer `GroupHeaderTemplate`s that
  render their own counts are unaffected (`ItemCount` still exposed).
- **Group header content is viewport-pinned.** The in-body group header's content (gutter,
  chevron, text, summary runs) clamps to the viewport width and counter-translates during
  horizontal scroll, so the header text stays visible and the right-aligned summary run sits
  at the visible right edge — not at the column-extent edge offscreen.
- **`SummaryItem` formatting/placement surface**: `Prefix` / `Suffix` (entry renders
  `Prefix + value + Suffix` when either is set), `Alignment` (Left/Right, default Right),
  `OrderIndex` (run position, editor-written).
- **Summary editor, three modes.** Items tab: column list (names render **bold** when the
  column carries configured summaries in the editor's scope) + Max / Min / Average / Sum
  toggles per selected column (gated by `SummaryCalculator.IsTypeSupported`) + "Show row
  count" (hidden in column-totals mode). Order and Alignment tab: Left side / Right side
  entry lists, ▲▼ reorder + ◀▶ re-side arrows, per-entry Prefix / Display format / Suffix
  with a live `Example:` preview. In column-totals mode the tab is just "Order" — alignment
  doesn't apply to a vertically-stacked cell, so the left side and ◀▶ arrows disappear and a
  single full-width "Order:" list reorders with ▲▼ only. Each side list binds its own selection (a single shared
  SelectedItem across two ListBoxes self-clears in WPF — this was wiping the selection,
  leaving the editing fields blank and the move arrows permanently disabled); the first
  configured entry pre-selects on open. The Display-format combo's presets are built per
  selected entry — its current custom format first (so existing values like
  `"Latest: {0:MM/dd/yyyy}"` show and stay re-pickable), then the target column's
  `DisplayStringFormat`, then date- or numeric-type suggestions — and it stays editable for
  anything else. Edits a working copy; OK applies (one coalesced recompute / projection
  rebuild), Cancel discards.
  - `GroupSummaryEditor.ShowGroupDialog(grid)` ("View Totals") — the shared group-header set.
    Opened from the group-header and pinned-strip menus.
  - `GroupSummaryEditor.ShowColumnTotalsDialog(grid, column)` (**"Totals for 'X'"**) — one
    column's totals cell, mixing aggregation targets from any column. Opened from the
    totals-cell **Customize…** and the column header's **Customize Totals…**.
  - `GroupSummaryEditor.ShowFixedTotalsDialog(grid)` ("Customize Fixed Totals") — the fixed
    panel's own set; "Show row count" maps to the no-FieldName Count entry.
- **Fixed total summary panel owns its definitions.**
  `SearchDataGrid.FixedTotalSummaries` (ctor-seeded collection, `FieldName`-targeted entries;
  a no-FieldName Count entry is the grid row count) replaces the old derive-from-column-totals
  behavior. The panel stays visible while empty when `ShowFixedTotalSummary` is on, so its
  right-click menu — **Count** (check-marked toggle of the row-count entry) and
  **Customize…** — is always reachable. (`ActualShowFixedTotalSummary` removed.)
- **Totals context-menu coverage.** The totals-cell menu gains **Customize…** ("Totals for
  'X'"). The column-header menu gains a **Total Summaries** submenu — Show/Hide Total Summary
  Row (re-arms `TotalSummaryPosition` when it was `None`), Show/Hide Fixed Total Summary, and
  Customize Totals… (column-scoped) — so every summary surface stays reachable while hidden
  or collapsed.
- **Fixed total summary panel.** `SearchDataGrid.ShowFixedTotalSummary` (default `false`) shows
  one horizontal, non-scrolling run beneath the items combining every column's total summaries
  (caption-qualified entries, split left/right per `SummaryItem.Alignment`; read-only
  `FixedTotalSummaryLeftText` / `FixedTotalSummaryRightText`, resolved visibility
  `ActualShowFixedTotalSummary`).
- **`TotalSummaryPosition`** (`None` / `Top` / `Bottom`, default `Bottom`) docks the
  column-aligned totals row beneath the filter row or above the horizontal scrollbar; folded
  into `ActualShowTotalSummaryRow`.
- **Total summary cells stack vertically.** Multiple summaries on one column render one line
  per entry (bound to `TotalSummaryTextInfo`); `TotalSummaryContentStyle` /
  `ActualTotalSummaryContentStyle` now target the per-entry `TextBlock`.
- **Sample:** the planned "Total Summaries" slot (Data Shaping) is now a real
  `TotalSummariesSampleView` — declarative totals (Count / Sum / composite-formatted Average /
  Max date), `AllowedTotalSummaries` restriction, the runtime picker, `ShowTotalSummary` +
  `TotalSummaryPosition` + `ShowFixedTotalSummary` + `ShowGroupRowCount` knobs, and a
  "View Totals…" launcher. `BasicGroupingSampleView` opts into `ShowGroupRowCount` to keep its
  group counts.
- **Sort groups by summary value (3.2.d).** `SearchDataGrid.SortGroupsBySummary(summaryType,
  fieldName, direction)` orders the groups at every level by a summary aggregate over each
  group's leaf rows (default Descending — largest first) instead of by the group key; the
  group-key order survives as the stable tie-breaker, and leaf rows inside each group keep
  their order. `ClearGroupSummarySort()` restores key ordering; `IsGroupSummarySortActive`
  reports state; the target column's read-only `GridColumn.IsSortedBySummary` (formerly a
  stub) lights while active. `Count` with no `fieldName` sorts by group row count. The sort
  persists across reflattens (filters, toggles, regrouping). Value comparison reuses the
  summary engine's ranking (`SummaryCalculator.CompareValues`, now public — numerics across
  widths, same-type `IComparable`, string-form fallback, nulls first).
- **"Sort By Summary" on the group-panel pill menu.** Right-clicking a grouped column's pill
  now offers a Sort By Summary submenu built from the grid's configured group-summary
  content: an Ascending/Descending pair per `GroupSummaries` aggregate (e.g.
  `Sum by 'Total' - Ascending`, captioned by the target column), a Row Count pair when
  `ShowGroupRowCount` is on, and a trailing Clear Summary Sort (enabled while a summary sort
  is active). The active option renders check-marked; the submenu hides entirely when no
  group summaries are configured. Backed by the new read-only
  `SearchDataGrid.ActiveGroupSummarySort` (a `GroupSummarySortDescriptor` — fresh instance
  per change, so menu bindings re-evaluate) and
  `ContextMenuCommands.SortGroupsBySummaryCommand` over `GroupSummarySortOption` items.
  A direct sort on the grouped column — header click, menu Sort Ascending/Descending, or the
  pill's click-to-flip — supersedes the summary sort (clears it and restores key ordering;
  without this the click would look dead, since the summary sort overrides the key order).
  "Clear Sorting" clears it too. Sorts on non-grouped columns only reorder leaves within
  their groups and leave the summary sort in place.
- **Column-aligned group summaries (`GroupSummaryDisplayMode`).** Grid-level
  `GroupSummaryDisplayMode` (`Header` default / `AlignByColumns`): in AlignByColumns mode the
  group summaries render **in the group header row itself, each value aligned under its
  target column** — no separate summary row. The header template gains an extent-space layer
  beneath the viewport-pinned content: a `GroupSummaryCellsPresenter` hosting one
  `GroupSummaryCell` per visible column (width bound to the column, display order; the same
  cumulative layout the data cells use, so the values scroll with their columns while the
  group caption stays pinned). Cells rebuild on column reorder/visibility/mode flips via a
  grid-side presenter registry hooked into the column-state refresh; in Header mode the
  presenter builds no cells, so header rows pay nothing. The opt-in row count and entries
  that don't resolve to a column stay in the header runs. Per-node results live on the group
  tree (`GroupNode.AlignedSummaryResults`, read through `GroupHeaderRow`) and refresh in
  place with the other summary surfaces — no reflatten, ever (the row set never changes).
  Theme: `GridSearchDataGridGroupSummaryCell` / `…GroupSummaryCellsPresenter` in
  `GroupStyle.xaml`. The pinned fixed-group strip renders the same aligned values: its
  entries carry a `FixedGroupSummaryCellsPresenter` layer — built on the
  filter-row/totals-row `ColumnAlignedRowPresenter` base (header mirroring + horizontal
  scroll sync), since the strip is pinned chrome rather than scrolled content — hosting the
  same `GroupSummaryCell`s, fed by `FixedGroupHeaderEntry` through the shared
  `IAlignedGroupSummarySource`. The layer collapses entirely outside AlignByColumns mode.
- **Group-header viewport pinning is code-owned.** The in-body header's counter-translation
  moved from a XAML `RelativeSource` binding on the `TranslateTransform` (a Freezable with no
  governing FrameworkElement — silently fails to resolve inside row templates) to the new
  `ViewportPinBehavior` attached behavior, which tracks the ancestor `ScrollViewer`'s
  `HorizontalOffset` in code.
- **Sample:** `TotalSummariesSampleView` adds a group-summary display-mode picker
  (Header / Align by columns) and Sort-groups-by-summary buttons (Sum(Total) / row count / clear).

### Changed — Unified window chrome (`ThemeKeys.PrimitivesWindow`)
- One generic window style now serves every window the library opens — Filter Editor, group
  summary editor ("View Totals"), and Column Chooser. The chrome is the SampleApp's window
  style promoted into the theme: borderless DWM window with rounded corners, drop shadow, and
  accent border (active `#0078D4`, inactive `#DDDDDD`), 30px caption, Segoe UI Variable, and an
  `AdornerDecorator` around the content (the old per-dialog templates lacked one, breaking
  adorner lookups inside the hosted content).
- `DwmWindowHelper` moved from the SampleApp into `WWSearchDataGrid.Modern.WPF` (attached
  properties for DWM shadow / border color / corner rounding / taskbar-respecting maximize);
  the SampleApp consumes the library copy and its `SampleAppWindowStyle` is gone —
  `LauncherWindow` uses `PrimitivesWindow`.
- Caption buttons are taskbar-aware: Close always; Max/Restore when the window is resizable;
  Min only when `ShowInTaskbar` is `true` (the library's dialogs aren't taskbar windows, so
  they get Close — plus Max when resizable — instead of a minimizable modal). The buttons
  invoke `SystemCommands`; the library wires per-window command bindings on its own hosts
  (`WindowHostHelper`), so no app-level registration is needed.
- Closing a dialog via the chrome's X is equivalent to Cancel (`DialogResult` stays false);
  the Column Chooser's X closes the window exactly as its old `CloseCommand` chrome did.

### Removed
- `ThemeKeys.FilterEditorWindow` and `ThemeKeys.ColumnChooserWindow` (and their near-duplicate
  styles) — superseded by `ThemeKeys.PrimitivesWindow`. `ColumnChooser.WindowStyle` remains the
  per-instance override hook.

### Changed — Grouping rebuilt on a flat row projection (replaces `GroupItem`/`Expander`)
- Grouping no longer uses WPF's `CollectionView` `GroupDescriptions` → `GroupStyle`/`GroupItem`/
  `Expander` hierarchical virtualization. When a grid is grouped, the group tree is now projected
  into a single flat list of group-header sentinels (`GroupHeaderRow`) interleaved with the raw
  data rows and set as the DataGrid's effective `ItemsSource`, so the rows panel virtualizes it as
  one uniform list. This removes the per-`GroupItem` realize/measure cost that made grouped
  scrolling stutter at depth and with collapsed nested groups; grouped scrolling now matches
  ungrouped smoothness.
- This is the only grouping engine — there is no opt-in flag. (The transitional `UseFlatGrouping`
  switch added earlier in this unreleased cycle is gone.)
- All grouping behaviors are preserved: declarative `GroupIndex`, `GroupBy`/`Ungroup`/
  `ClearGrouping`, group-leads-sorting, `ShowGroupedColumns`, `GroupInterval` buckets, per-group
  expansion persistence, Expand/Collapse all, recursive expand (`ExpandGroupsRecursively`), the
  sticky fixed-group strip (`AllowFixedGroups`), the group panel, and the filter row while grouped.
- Group headers render as full-width `DataGridRow`s (via `SearchDataGridRow` + an `IsGroupHeader`
  trigger) and are non-selectable / non-editable / skipped by keyboard navigation and copy. The
  in-body header right-click menu is preserved (`ExpandGroupCommand` / `CollapseGroupCommand` /
  `ExpandAllAtLevelCommand` / `CollapseAllAtLevelCommand` / `UngroupAtLevelCommand`, taking a
  `GroupHeaderRow`). The sticky strip's resolver is now an index lookup into the projected rows
  rather than a visual-tree `GroupItem` walk.

### Removed
- `SearchDataGrid.UseGroupExpansionAnimation` and the animated group-`Expander` template /
  `GroupExpansionAnimator` — there is no `Expander` chrome to animate.
- `SearchDataGrid.TrackGroupExpansion` attached property — expansion state now persists in the
  engine's path-keyed map, not per-`Expander`.
- The default `GroupStyle` (`GroupItem`/`Expander`) chrome and its legacy group-header context menu.

### Added — Allow Fixed Groups (sticky group headers)
- `SearchDataGrid.AllowFixedGroups` (bool, default `false`). When `true` on a grouped grid the
  group header(s) for the topmost visible row stay pinned to the top of the data area; as the
  next sibling rises into the strip, the topmost pinned header slides up to make room (push
  transition). Outer-to-inner stacking matches the in-place stair-step indent.
- `FixedGroupHeadersPresenter` (new `ItemsControl` in `Controls/Grouping/`) overlaid in the
  grid template; `FixedGroupHeaderEntry` (per-pinned-level view-model with TwoWay `IsExpanded`);
  `FixedGroupIndentConverter` (per-level `Margin.Left` matching `GroupHeaderMarginConverter`'s
  output formula).
- Pinned chrome carries its own context menu: `ContextMenuCommands.ExpandFixedGroupCommand` /
  `CollapseFixedGroupCommand` / `ExpandAllAtFixedLevelCommand` / `CollapseAllAtFixedLevelCommand` /
  `UngroupAtFixedLevelCommand`. Sibling set to the in-place commands but takes a
  `FixedGroupHeaderEntry` instead of an `Expander`.
- Click on a pinned header routes through `SearchDataGrid.ApplyFixedGroupExpansion`, which
  toggles the realized real `Expander` (firing the existing class handler that updates
  `_groupExpandState`) or writes straight to the persistence map when the represented group is
  virtualized — so the in-place chrome and the persistence layer stay coherent.
- Sample: `BasicGroupingSampleView` (Data Shaping › Grouping) ships an `Allow fixed groups`
  toggle in the OPTIONS group alongside the rest of the grouping switches. Use the GROUP BY
  buttons in the sample to add nested levels and watch the strip stair-step.

### Added — AutoFilterRow spec conformance
- Grid-level: `ShowCriteriaInAutoFilterRow`, `AutoFilterRowCellStyle`, `FilterRowDelay`, `AutoFilterRowClearButtonMode`, `EnableLiveFiltering`
- Column-level: `AllowAutoFilter`, `ShowCriteriaInAutoFilterRow`, `AutoFilterRowCellStyle`, `AutoFilterRowDisplayTemplate`, `AutoFilterRowEditTemplate`, `DefaultSearchType`, `RoundDateTime`
- `EditGridCellData` hierarchy (`DataObjectBase` → `EditableDataObject` → `GridDataBase` → `GridColumnData` → `GridCellData` → `EditGridCellData`) for template authors
- `DispatcherTimer`-based keystroke debounce replacing the dead `System.Timers.Timer` infrastructure
- `docs/column-filter-mode.md` documenting how display properties drive raw vs display-text filter comparison

### Changed
- String columns default to `DefaultSearchType.StartsWith` (was `Contains`). Spec-aligned.
- Live-filtering is now a single grid-level DP, `SearchDataGrid.EnableLiveFiltering` (default `true`). When `false`, popup edits and auto-filter-row typing defer until commit (Enter / Tab / focus loss / popup close). Replaces the popup's per-session "Live filter" checkbox, the per-column `GridColumn.ImmediateUpdateAutoFilter` DP, and the 100,000-row auto-disable threshold (`SearchDataGrid.LiveFilteringRowCountThreshold`).
- `AutoFilterRowClearButtonMode` enum reshaped to `Never` / `Always` / `Display` / `Edit` (was `Default` / `WhenFilterApplied` / `Always` / `Never`). Default is now `Always`. `Display` / `Edit` now resolve from the filter cell's display/edit state — see below.
- **Auto-filter row now has a display / edit state machine** mirroring the row's data-cell editor. `ColumnFilterControl.IsFilterCellEditing` tracks `IsKeyboardFocusWithin`; the host swaps between a read-only `TextBlock`-shaped display surface (no decoration buttons) and the full editor produced by `BaseEditSettings.CreateFilterEditor` (with chevron / calendar / spinner chrome). Click into a filter cell or tab to it to enter edit mode; focus out demotes back to display. `BaseEditSettings` gains a `CreateFilterDisplay(host)` virtual method overridden by `ComboBoxEditSettings`, `DateEditSettings`, and `SpinEditSettings` for type-appropriate formatting.

### Fixed
- Clear button now appears on all editor surfaces (combobox, date, checkbox via `EditSettings`) when a filter is active — previously visibility only tracked `SearchText.Length`, so typed-value editors never surfaced the button
- DateTime column `=` (and the rest of `<` / `<=` / `>` / `>=` / `Between`) now honors `SearchCondition.RoundDateTime`. Previously only `BetweenDates` / `NotBetweenDates` rounded, so picking a date in the auto-filter row never matched column values that carried a non-zero time-of-day. `SearchEngine.CompareValues` now compares via `.Date` when the flag is set. The column-level `RoundDateTime` DP (nullable `bool`) overrides the auto-detection that samples bound values for time-of-day; `null` (default) rounds when the column has no times and keeps the full instant when it does — matching the editor shape produced by `BuildAutoDateTimeEditSettings`

### Removed
- Deprecated `DefaultSearchMode` enum and CLR property (use `DefaultSearchType`)
- Deprecated `ColumnFilterControl.IsLiveFilteringOverride` DP (use `SearchDataGrid.EnableLiveFiltering`)
- `GridColumn.ImmediateUpdateAutoFilter` DP, `SearchDataGrid.LiveFilteringRowCountThreshold` const, and the popup footer's "Live filter" checkbox — all replaced by `SearchDataGrid.EnableLiveFiltering`

## [0.2.0] - 2026-03-30

### Added
- Column chooser with drag-drop reordering and show/hide toggles
- `IsColumnChooserEnabled`, `IsColumnChooserVisible`, `IsColumnChooserConfinedToGrid` properties
- Select-all checkbox in boolean column headers with `IsSelectAllColumn` and `SelectAllScope`
- `ColumnDisplayName` attached property for custom display names in filter panel and column chooser
- `DefaultSearchMode` attached property (Contains, StartsWith, EndsWith, Equals)

### Changed
- Split `SearchDataGrid.cs` (2,700 lines) into 5 focused partial class files
- Split `ColumnSearchBox.cs` (1,700 lines) into 3 focused partial class files
- Cached all RelayCommand instances (45 properties) to eliminate per-access allocations
- Added reflection caching in `ReflectionHelper.GetPropValue` for filtering performance
- Added compiled Regex caching in `IsLikeEvaluator`
- Improved thread safety in `SearchEvaluatorFactory` and `CommandManager`

### Fixed
- Event handler memory leaks in `SearchDataGrid.OnApplyTemplate` (events never unsubscribed on re-template)
- `DependencyPropertyDescriptor.AddValueChanged` memory leak in constructor
- `FilterExpressionBuilder` returning pass-all expression for collection-context-only filters
- Empty catch block in `ColumnFilterEditor.OnAutoApplyFilter` silently swallowing all exceptions
- Removed forced `GC.Collect()` from library code in `ClearAllCachedData`
- Eliminated duplicate `IsNumericType` and `ConvertToDouble` implementations

## [0.1.0] - Initial Development

### Added
- `SearchDataGrid` control extending WPF DataGrid with per-column filtering
- 25+ search types via pluggable evaluator architecture
- `ColumnSearchBox` with simple text search and advanced filter popup
- `ColumnFilterEditor` for multi-criteria filter rule building
- `FilterPanel` displaying active filters as chips
- Expression tree compilation for high-performance filtering
- `GridColumn` attached properties for column configuration
- Checkbox column support with three-state cycling
- Built-in context menus (copy, sort, best-fit, filter management)
- Auto-size columns feature
- Cell value change tracking with `CellValueChanged` event
- Full WPF theming via Generic.xaml pattern
- Sample application demonstrating all features
