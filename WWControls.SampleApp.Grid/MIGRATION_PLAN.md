# Sample App Re-organization Plan

Migrating the existing flat sample catalog into a new, intent-driven category layout.
Reuse existing sample views wherever possible; everything not yet built is surfaced as a
**fully interactive stub** (a real, navigable view that explains what the feature will do and
what's required to implement it) rather than being hidden.

## Goals

- Re-bucket the current 18 samples into 6 task-oriented categories.
- Make unbuilt features **visible** in the launcher as stub views, so the backlog is a live checklist.
- Reuse existing view classes; avoid rewrites. Merges are deferred to a later phase.
- Collapse the filtering samples into a single **Filtering hub** with an inner tree of mini-samples.

## Status legend

- **Done** — existing view, drops into its new home unchanged.
- **Partial** — feature works; sample needs new toggles or a merge.
- **Planned** — feature not implemented in the library; shown as a stub view.

---

## Target layout

### 1. Data Binding
| Sample | Source | Status | Notes |
|---|---|---|---|
| Auto Columns Generation | `AutoColumnsSampleView` (merged) | Done | POCO ⇄ DataTable source-type switch on one auto-gen grid. Superseded `PocoAttributes` + `DataTableAutoGen`. |
| Binding to Dynamic Object | new; absorb `DataTableManualSampleView` (add/remove columns + add rows) | Planned | Two-for-one: dynamic object **and** the DataTable add/remove columns/rows demo. |

### 2. Layout and Customization
| Sample | Source | Status | Notes |
|---|---|---|---|
| Column Configuration | `ColumnConfigurationSampleView` | Done | General per-column inspector playground. |
| Display Formatting | `DisplayFormattingSampleView` | Done | (Not in original spec — best-judgment home.) |
| Column Chooser | `ColumnChooserSampleView` | Done | Auto-show + show/hide button + confine-to-grid already present. |
| Fixed Columns | `FixedColumnsSampleView` | Done | Left/right pin via `GridColumn.Fixed`; per-column radios + header Fixed menu (`AllowFixedColumnMenu`). |
| Save and Restore Layout | stub | Planned | Prereq: column grouping + totals row. |

### 3. Selection and Usability
| Sample | Source | Status | Notes |
|---|---|---|---|
| Copy / Paste operations | `CopyPasteSampleView` | Done | Grid + paste box; Ctrl+C / Ctrl+Shift+C + buttons via `ContextMenuCommands`. |
| Built-In Context Menus | `ContextMenusSampleView` | Done | Default menus on right-click; toggles bind `AllowFixedColumnMenu` / `IsColumnChooserEnabled`. Custom-item injection still Planned. |

### 4. Editing
| Sample | Source | Status | Notes |
|---|---|---|---|
| Cell Editors | `EditorTypesSampleView` | Done | |
| Cell Editor Customization | `EditorCustomizationSampleView` | Done | |
| Editor Input Masking | `InputMaskingSampleView` | Done | |
| New Item Row | new | Planned | `NewRowPosition` top/bottom/none not well supported yet. |
| Data Validation | stub | Planned | Invalid cells, error icons, commit-on-error toggle, enable/disable validation. |
| Data Error Indication | stub | Planned | Error / Warning / Info severities. |
| Inline Edit Form | stub | Planned | |
| Edit Entire Row | stub | Planned | |

### 5. Data Shaping
Hosted by a single **Filtering hub** view (inner tree on the left, mini-sample on the right) plus two
top-level Planned entries.

| Entry | Inner node | Source | Status |
|---|---|---|---|
| Filtering (hub) | Excel-Style Drop-Down → Custom Popup Content | `CustomFilterElementsSampleView` | Done |
| | Excel-Style Drop-Down → Multi-Tab Popup | `MultiTabFilterPopupSampleView` | Done |
| | Excel-Style Drop-Down → Group Filters | stub | Planned |
| | Filter Editor | `FilterStringSampleView` | Done |
| | Filter Row ▸ Options Playground | `OptionsPlaygroundSampleView` | Done |
| | Filter Row ▸ Custom Templates | `CustomTemplatesSampleView` | Done |
| | Filter Panel | `CustomPredicateSampleView` | Done |
| | Search Modes | `SearchModesSampleView` | Done |
| Grouping | — | stub | Planned |
| Total Summaries | — | stub | Planned |

### 6. Performance
| Sample | Source | Status | Notes |
|---|---|---|---|
| Vertical Scrolling Options | `ScrollingAnimationSampleView` | Partial | Add Allow-Fixed-Groups (Planned, needs grouping) + Cascade-Update (Planned) toggles. |
| Large Datasets | `LargeDatasetsSampleView` | Done | Virtualization / scale (moved here from Animation & Performance). |

---

## Library backlog (dependency-ordered)

1. **Grouping** — gates Save/Restore Layout, Allow-Fixed-Groups, Group Filters.
2. **Total Summaries** — gates Save/Restore Layout.
3. **New Item Row** (`NewRowPosition`).
4. **Data Validation** + **Data Error Indication** (Error/Warning/Info).
5. **Custom context-menu item injection.**
6. **Cascade Update**, **Inline Edit Form**, **Edit Entire Row**, **Save/Restore Layout** (depends on 1–2).

---

## Phasing

- **Phase 0 (scaffolding)** — `PlannedSampleView` stub control; `FilteringHubSampleView` tree shell
  reusing existing filtering views; rewrite `SampleCatalog` into the 6 categories with stubs for all
  Planned samples. *Low risk — no existing view is modified.*
- **Phase 1** — polish stub copy; confirm every category renders and every existing view still works in its new home.
- **Phase 2 (merges)** — ✅ Auto Columns POCO⇄DataTable toggle (`AutoColumnsSampleView`); Filter Row merged under one hub branch (Options Playground + Custom Templates leaves).
- **Phase 3 (new on shipped features)** — ✅ Fixed Columns, Copy/Paste, Built-In Context Menus.
- **Phase 4+** — implement the library backlog, converting stubs to live samples one at a time.

---

## Architectural notes

- `SampleDefinition` / `SampleCategory` stay as-is; the new layout is expressed entirely in
  `SampleCatalog`. No launcher rework needed.
- The Filtering hub's inner tree lives **inside** the sample view (per the spec's "inner treeview in
  the left panel"), not in the launcher nav tree. Existing filtering views are reused verbatim as the
  hub's right-panel content — each keeps its own source-tab host.
- Planned stubs are plain `UserControl`s built by `PlannedSampleView`; they don't use
  `SampleHostControl` (no source tabs to show yet).
