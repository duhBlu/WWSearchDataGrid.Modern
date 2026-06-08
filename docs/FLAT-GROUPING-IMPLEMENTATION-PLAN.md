# Flat Grouping Implementation Plan

> Replace WPF's `GroupItem`/`Expander` hierarchical virtualization with a flattened row
> projection so grouped scrolling stays smooth at any depth and collapse state.
> Designed to be worked phase-by-phase; each phase leaves the grid functional.

## Status

- **COMPLETE (all phases 0–6 shipped).** The flat projection is now the *only* grouping engine —
  the legacy `GroupDescriptions`/`GroupItem`/`Expander` path, `GroupExpansionAnimator`,
  `TrackGroupExpansion`, `UseGroupExpansionAnimation`, the throwaway spike sample, and the
  transitional `UseFlatGrouping` flag have all been removed. Per-project convention, the engine
  carries no "Flat" naming: files/types/members read as the grouping engine
  (`SearchDataGrid.GroupProjection.cs`, `SearchDataGrid.GroupNavigation.cs`, `GroupRowItems.cs`,
  `GroupRowCollection`, `RebuildGroupingProjection`, `RebuildRowProjection`, `ToggleGroup`, etc.).
  See the CHANGELOG entry "Grouping rebuilt on a flat row projection".
- **Diagnosis: confirmed.** Grouped-scroll lag is WPF's per-`GroupItem` realize/measure cost on every group-boundary crossing. Worst case (reproduced live): top-level groups expanded, nested groups **collapsed** — every ~28px of scroll crosses a fresh collapsed `GroupItem`, so every scroll step pays the full hierarchical-virtualization cost. Independent of header-template richness, the sticky strip (`AllowFixedGroups` on/off), and `VirtualizationCacheLength` (all measured).
- **Spike: passed.** `SampleApp → Performance → "Flat Grouping Spike (throwaway)"`. Same 2,500 orders, grouped Status→Employee, collapsed nested groups. The flat list (one recycling `VirtualizingStackPanel`) held smooth in the exact worst case while the real grid dropped to ~1 FPS. The "Ungroup baseline" control isolated the cost to the `GroupItem` hierarchy, not cell rendering (the same grid with identical cell weight is smooth ungrouped).
- **Decision:** flat projection **inside the existing `SearchDataGrid : DataGrid`** — not a from-scratch custom rows panel. The spike showed flatness (not a custom panel) is what removes the cost, and staying on the DataGrid preserves cell rendering, selection, editing, column virtualization, fixed columns, and the filter row for data rows.

---

## Goal

A collapsed/nested grouped grid scrolls as smoothly as an ungrouped one. Replace the
`CollectionView` grouping (`Items.GroupDescriptions` → `GroupStyle`/`GroupItem`/`Expander`)
with a **flat list of typed rows** — group-header sentinels interleaved with data rows —
rendered by the DataGrid's existing `DataGridRowsPresenter`. Collapse = splice a group's
data rows out of the flat list. No `GroupItem`, no nested panels, no per-boundary realize cost.

All current grouping behaviors must survive: declarative `GroupIndex`, `GroupBy`/`Ungroup`/
`ClearGrouping`, group-leads-sorting (D2), `ShowGroupedColumns` (D3), `GroupInterval`
buckets, per-group expansion persistence, Expand/Collapse all, recursive expand, the sticky
fixed-group strip, the group panel, and the filter row working while grouped.

---

## Architecture Decision

### The core change: ownership inversion of the shaping pipeline

Today the DataGrid's `Items` `CollectionView` owns all shaping, and grouping drives rendering:

```
ItemsSource (user data)
   └─ DataGrid.Items (ICollectionView)
        ├─ Filter         (Items.Filter / SearchFilter)        SearchDataGrid.Filtering.cs
        ├─ Sort           (Items.SortDescriptions)             SearchDataGrid.Sorting.cs
        └─ Group          (Items.GroupDescriptions)            SearchDataGrid.Grouping.cs
              └─ renders as GroupItem/Expander  ← THE LAG
```

An `ItemsControl` renders exactly its `Items`, so to interleave header sentinels with data
rows the sentinels must BE elements of the rendered collection — which means the rendered
collection can no longer be a `CollectionView` that filters/sorts/groups the user's data
objects. The shaping must move **upstream** of the rendered collection:

```
ItemsSource (user data)
   └─ internal shaping view (ICollectionView over the user data, NOT bound to the grid)
        ├─ Filter   (reuse existing predicate / SearchFilter)
        └─ Sort     (reuse existing comparers + group-leading sorts)
   └─ GroupProjector: partition filtered+sorted rows into the group tree,
        flatten honoring per-group expand state, emit ──►
   └─ FlatRows : ObservableCollection<IRowItem>   ← DataGrid.ItemsSource
        [HeaderRow Status:Submitted (971)]
        [HeaderRow   Employee:Amanda Foster (87)]   (collapsed → its data rows omitted)
        [HeaderRow   Employee:Brian O'Neill (55)]
        [HeaderRow Status:Cancelled (144)]
        [DataRow  order…]  [DataRow order…] …
   └─ DataGrid does NO filter/sort/group itself — it renders a pre-shaped flat list.
```

The DataGrid keeps generating `DataGridRow` containers from `FlatRows`. A `RowStyleSelector`
renders header sentinels as full-width header rows (cells hidden) and data rows normally.

**Why this is the right scope:** the hard-to-replicate machinery (cell generation, column
virtualization, selection, editing, fixed columns, filter-row host) is per-row/per-cell and
keeps working unchanged for data rows. The risk concentrates in one place — rerouting the
*shaping triggers* (filter row, header-click sort, group config) to rebuild the projection
instead of mutating `Items.Filter`/`SortDescriptions`/`GroupDescriptions`. The existing
predicates and comparers are reused as-is; only *where* they're applied moves.

**A bonus simplification:** the sticky fixed-group strip stops needing `WalkForTopmost` and
`GroupItem` expand-state probing. With flat rows, the pinned header is simply the last
header row whose index is at/above the first visible row — a cheap index lookup.

### Row item model

```
IRowItem                       (marker; DataGrid.ItemsSource is ObservableCollection<IRowItem>)
 ├─ GroupHeaderRow             Level, GroupKey, DisplayValue, Count, IsExpanded, owning GridColumn,
 │                             back-ref to the GroupNode (for toggle)
 └─ DataRow                    wraps the user's item (the real OrderItem, etc.)
```

`DataRow` must expose the underlying item so existing cell bindings (`{Binding FieldName}`),
selection, editing, and copy/paste resolve against the real object. Two sub-options to settle
in Phase 0 (see Open Questions): wrap the item (`DataRow.Item`, cell bindings retargeted) vs.
put the raw user item directly in the flat list and use a sentinel type only for headers
(cell bindings unchanged; headers detected by type). **Lean: raw user item + header sentinel**
— it keeps every existing `{Binding FieldName}` cell binding and selection/edit path untouched;
only header rows are a new type to special-case.

---

## Phase 0 — Foundations & safety switch

### 0.1 Row-item types
- Add `GroupHeaderRow` (and confirm the raw-item approach for data rows).
- `GroupNode` tree type (Level, Key, DisplayValue, recursive Count, IsExpanded, Children, leaf Items) — promote the spike's `SpikeGroup`.

### 0.2 Engine selection flag
- Internal `bool UseFlatGrouping` (default OFF initially) so the legacy `GroupDescriptions`
  path and the new flat path can coexist during development and be A/B'd in the SampleApp.
- **Acceptance:** flag OFF → current behavior byte-for-byte; flag ON → grid still loads
  (even if grouping is a no-op stub at this point).

---

## Phase 1 — Projection engine (kills the lag)

### 1.1 Internal shaping view
- Build an internal `ICollectionView`/`ListCollectionView` over `originalItemsSource` that the grid does NOT bind to. Apply the existing filter predicate (`SearchFilter`) and sort (the same `SortDescription` set the engine builds today, including group-leading sorts per D2) to THIS view.
- **Acceptance:** internal view yields the same filtered+sorted sequence the visible grid shows today (verify against the legacy path with identical filter+sort).

### 1.2 GroupProjector
- From the internal view's output, partition into the `GroupNode` tree by the grouped `GridColumn`s (honoring `GroupInterval` buckets — reuse `IntervalGroupDescription` logic or port its key function). Compute recursive counts. Flatten to `ObservableCollection<IRowItem>` honoring each node's `IsExpanded`.
- Rebuild triggers: source `CollectionChanged`, filter change, sort change, group-config change, expand/collapse. Coalesce rebuilds (single dispatcher pass) to avoid thrashing.

### 1.3 Wire to the DataGrid when grouped
- When `UseFlatGrouping` and `GroupCount > 0`: set the DataGrid's effective `ItemsSource` to `FlatRows`; ensure the DataGrid's own `Items.Filter`/`SortDescriptions`/`GroupDescriptions` are cleared (shaping is upstream now). When ungrouped: bind straight to the user source (today's path).
- **Acceptance (the headline win):** with the spike dataset, flag ON, Status expanded / Employee collapsed, fast scroll holds smooth (target: no sustained dips, comparable to ungrouped); data rows still render via normal cells. Header rows may look unstyled at this phase.

---

## Phase 2 — Header-row rendering

### 2.1 Custom row + RowStyleSelector
- `RowStyleSelector` (and/or a `SearchDataGridRow : DataGridRow` subclass) that, for a `GroupHeaderRow`, swaps to a full-width header template (hide `DataGridCellsPresenter`, show a header presenter spanning all columns) and, for a data row, uses the normal row style.
- Reuse the existing group-header visuals: chevron, indent per level (`GroupIndentWidth`), the column-name prefix + `GroupValueTemplate`/`GroupHeaderTemplate` selectors, count chip. Port from `GroupStyle.xaml`.

### 2.2 Toggle interaction
- Click/space on a header row toggles `GroupNode.IsExpanded` → projector splices that group's descendants/data in or out. Chevron reflects state.
- **Acceptance:** headers visually match today's group headers (value templates, count, indent); clicking expands/collapses with no flicker; row-header gutter alignment preserved.

---

## Phase 3 — Reroute shaping triggers

Point every shaping affordance at the projection instead of the DataGrid's `Items`:

### 3.1 Grouping API
- `GroupBy`/`Ungroup`/`ClearGrouping`/`GroupIndex` rebuild the `GroupNode` tree + reflatten (replacing `RebuildGroupDescriptions`' `GroupDescriptions` mutation). Keep `GroupCount`, `GroupedColumns`, `GroupLevel`/`IsGrouped` projections, and `ShowGroupedColumns` (D3) column hiding.

### 3.2 Sorting (D2: grouping leads sorting)
- Header-click sort and group-leading sorts feed the internal shaping view's sort, then reflatten. Group order keyed by the same `DefaultGroupBySortDirection`/`SortOrder` logic as today (`ReconcileGroupSortDescriptions`).

### 3.3 Filtering
- Filter row, column-filter popups, `FilterString`, `SearchFilter`, and `EnableLiveFiltering` set the predicate on the internal shaping view, then reflatten. Recheck the `DataView → ListCollectionView` rewrap and empty-state-scroll filter swap against the new owner.
- **Acceptance:** filter row narrows rows while grouped (counts on headers update, empty groups drop); sorting reorders within groups and reorders groups; `GroupInterval=DateMonth` still buckets.

---

## Phase 4 — Selection / editing / copy correctness

- Header rows must be non-selectable, skipped by range/Shift selection, Ctrl+A, and copy/paste; non-editable; keyboard navigation steps over them.
- Verify `IsSynchronizedWithCurrentItem`, `SelectedItem`/`SelectedItems` return user items (never `GroupHeaderRow`s).
- **Acceptance:** the Usability (copy/paste, context menu) and Editing samples behave identically while grouped; selecting/copying never yields a header sentinel.

---

## Phase 5 — Group affordance parity

### 5.1 Expansion state
- Per-group expansion persistence across refresh/regroup (replace `_groupExpandState`/`TrackGroupExpansion` with `GroupNode.IsExpanded` keyed by the group path). `AutoExpandAllGroups`, `ExpandAllGroups`/`CollapseAllGroups`, `ExpandGroupsRecursively`.

### 5.2 Sticky fixed-group strip (simplified)
- Re-implement `AllowFixedGroups` against flat rows: pinned chain = the header rows that are ancestors of the first visible data row and scrolled above the top. This is an index lookup into `FlatRows`, replacing `WalkForTopmost`/`GroupItem` probing in `SearchDataGrid.FixedGroups.cs`.

### 5.3 Group panel
- `GroupPanel` pills bind to `GroupedColumns` (unchanged) — verify drag-to-group / pill sort still drive the new engine.
- **Acceptance:** AnimationPerformance + Grouping samples exercise all toggles; sticky strip pins correctly during fast scroll with collapsed nested groups (and stays smooth).

---

## Phase 6 — Cleanup & removal

- Flip `UseFlatGrouping` default ON; migrate SampleApp callsites.
- Remove the legacy `GroupDescriptions` path, `GroupStyle.xaml` `GroupItem`/`Expander` chrome, `GroupExpansionAnimator`, and the old `FixedGroups` `WalkForTopmost`. (Single in-tree consumer — short deprecation per project convention.)
- CHANGELOG entry; delete the throwaway spike sample.

---

## Risks & Open Questions

1. **Data-row representation (Phase 0 fork).** Raw user item + header sentinel (preferred — keeps all cell bindings/selection/edit untouched) vs. a `DataRow` wrapper (cleaner typing, but retargets every cell binding and selection path). Decide before Phase 1.
2. **Inversion blast radius.** Filtering/sorting currently assume the DataGrid's `Items`. Rerouting `Items.Filter`/`SortDescriptions` to an internal view touches `Filtering.cs`, `Sorting.cs`, `FilterRow.cs`, `FilterString.cs`, `EmptyStateScroll.cs`. The flag (0.2) lets us land this incrementally without breaking the ungrouped path.
3. **`EnableLiveFiltering` / live updates.** Reflattening on every source change must coalesce or large live datasets will thrash. Needs a debounced rebuild.
4. **Column virtualization vs. full-width headers.** A header row spanning all columns must not fight horizontal column virtualization; verify header rows opt out cleanly.
5. **Selection semantics with header rows present** (Phase 4) is the most likely place for subtle regressions — budget testing time.
6. **Totals/summary rows (future).** The flat model makes group footers natural (another `IRowItem` kind) — keep the row-item model open to it.

---

## Validation

- Reuse the spike's A/B harness (and "Ungroup baseline" control) as the perf gate: grouped + nested-collapsed fast scroll must match ungrouped smoothness.
- Per phase, diff behavior against the legacy path via the `UseFlatGrouping` flag.
- Gotcha: the SampleApp locks its theme DLL while running — stop it before each rebuild or the new theme silently won't copy.
