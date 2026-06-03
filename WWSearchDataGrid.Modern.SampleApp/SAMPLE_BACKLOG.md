# Sample Backlog (Phase 4+)

Work remaining to turn the "Planned" stub samples into live ones. Each item lists the **library**
work (control features that don't exist yet) and the **sample** work (the view that demonstrates
them). Ordered by dependency. Stub views live in the catalog today so the target layout is visible;
replacing a stub means building the real view and pointing `SampleCatalog` at it (drop the
`PlannedSampleView.Planned(...)` factory).

See `MIGRATION_PLAN.md` for the full category map.

## Dependency order

1. **Grouping** — gates Save/Restore Layout, Allow-Fixed-Groups, and Group Filters.
2. **Total Summaries** — gates Save/Restore Layout.
3. New Item Row.
4. Data Validation + Data Error Indication (related pair).
5. Custom context-menu item injection.
6. Cascade Update · Inline Edit Form · Edit Entire Row · Save/Restore Layout (last; depend on 1–2).

---

## Data Shaping

### Grouping  *(Data Shaping › Grouping)*
- [ ] Library: grouping engine + group panel control.
- [ ] Library: options — Allow Fixed Groups, Show Group Panel, Show Grouped Columns, auto-expand all,
      animate expand, expand recursively.
- [ ] Library: pre-set group-by presets.
- [ ] Sample: grid with the group panel + a toggle for each option above; a few preset group-bys.

### Total Summaries  *(Data Shaping › Total Summaries)*
- [ ] Library: totals/summary row.
- [ ] Library: aggregate functions (sum / avg / count / min / max).
- [ ] Library: group-level summary footers (depends on Grouping).
- [ ] Library: wire up `ContextMenuCommands.ToggleTotalsRowCommand` (currently a placeholder).
- [ ] Sample: grid with a totals row, per-column aggregate pickers.

### Group Filters  *(Data Shaping › Filtering hub › Excel-Style Drop-Down › Group Filters)*
- [ ] Library: grouped value listing inside the Excel-style filter popup (depends on Grouping).
- [ ] Sample: filtering against a grouped dropdown (add as a hub leaf).

---

## Data Binding

### Binding to Dynamic Object  *(Data Binding › Binding to Dynamic Object)*
- [ ] Library: bind to a dynamic / expando-style item source.
- [ ] Library: add/remove columns at runtime; add-new-row support.
- [ ] Sample: grid + buttons to add rows and add/remove columns. Reuse `DataTableManualSampleView`
      as the DataTable variant (two-for-one).

---

## Editing

### New Item Row  *(Editing › New Item Row)*
- [ ] Library: `NewRowPosition` (Top / Bottom / None).
- [ ] Library: harden the add-new-row commit flow.
- [ ] Sample: position toggle (top / bottom / none) over a writable grid.

### Data Validation  *(Editing › Data Validation)*
- [ ] Library: validation-attribute evaluation.
- [ ] Library: error icons on cells and the row header.
- [ ] Library: option — allow commit despite validation errors.
- [ ] Library: option — enable/disable validation (ignore errors when off).
- [ ] Sample: grid that loads with invalid cells + the two option toggles.

### Data Error Indication  *(Editing › Data Error Indication)*
- [ ] Library: severity model (Error / Warning / Info).
- [ ] Library: per-severity cell/row indicators.
- [ ] Sample: rows demonstrating each severity.

### Inline Edit Form  *(Editing › Inline Edit Form)*
- [ ] Library: inline edit-form host + field layout/template support.
- [ ] Sample: edit the focused row via an inline form region.

### Edit Entire Row  *(Editing › Edit Entire Row)*
- [ ] Library: row-level edit mode with row-scoped commit/cancel.
- [ ] Sample: toggle a row into full edit, commit/cancel as a unit.

---

## Layout and Customization

### Save and Restore Layout  *(Layout and Customization › Save and Restore Layout)*
- [ ] Library: serialize/restore column order, width, visibility, pinning (+ grouping + totals).
- [ ] Depends on: Grouping, Total Summaries.
- [ ] Sample: save / load buttons with a couple of named layout slots.

---

## Selection and Usability

### Custom context-menu item injection  *(Selection and Usability › Built-In Context Menus)*
- [ ] Library: API to inject custom items into the built-in cell/header/row menus.
- [ ] Library (nice-to-have): per-item enable/disable beyond the existing `AllowFixedColumnMenu` /
      `IsColumnChooserEnabled` gates — the rest are context-gated, not opt-out.
- [ ] Sample: extend `ContextMenusSampleView` with a custom item once the API lands.

### Cascade Update + Allow Fixed Groups  *(Performance › Vertical Scrolling Options)*
- [ ] Library: Cascade Update behavior.
- [ ] Library: Allow Fixed Groups while scrolling (depends on Grouping).
- [ ] Sample: enable the two greyed toggles in `ScrollingAnimationSampleView`.

---

## Library follow-ups (not blocking a sample)

- [ ] **Auto-columns regeneration on schema swap.** When `AutoGenerateColumns=True` and the
      ItemsSource is replaced with a *different schema* (e.g. POCO → DataTable), the grid keeps its
      first-generated columns — `OnItemsSourceChanged` only regenerates when
      `ResolveFieldTypesFromItemsSource()` reports a type change, not a wholesale field-set change.
      The Auto Columns sample works around this with two grids. Consider regenerating when the field
      *set* changes, not just field types.
- [ ] `ExportToCsvCommand` / `ExportToExcelCommand` are placeholders in `CopyCommands.cs`.
