# GridColumn Implementation Plan

> Custom column class to replace attached-property configuration with a clean, Xceed-inspired API.
> Designed to be worked session-by-session. Each work item is scoped to ~1-2 hours.

## Goal

Replace this:
```xml
<DataGridTextColumn Width="80"
                    sdg:GridColumn.FilterMemberPath="OrderNumber"
                    sdg:GridColumn.DefaultSearchMode="StartsWith"
                    sdg:GridColumn.EnableRuleFiltering="False"
                    sdg:GridColumn.DisplayStringFormat="N0"
                    SortMemberPath="OrderNumber"
                    Header="Order #"
                    Binding="{Binding OrderNumber}" />
```

With this:
```xml
<sdg:GridColumn FieldName="OrderNumber" Header="Order #" Width="80"
                DefaultSearchMode="StartsWith" EnableRuleFiltering="False"
                DisplayStringFormat="N0" />
```

`FieldName` auto-generates `Binding`, `SortMemberPath`, and `FilterMemberPath`. The grid auto-detects the data type and selects the appropriate internal WPF column type.

---

## Architecture Decision

**Approach: Column Descriptor Pattern**

`GridColumn` is a FrameworkContentElement that *describes* a column. `SearchDataGrid` reads these descriptors and generates the real WPF `DataGridColumn` instances internally. This gives us the clean API without rewriting WPF's cell rendering pipeline.

```
User XAML           Internal
─────────────       ────────────────────────
GridColumn    ───►  DataGridTextColumn
  FieldName         + Binding
  Header            + SortMemberPath
  Width             + FilterMemberPath
  ...               + attached properties
```

**Why not extend DataGridColumn directly?**
- DataGridColumn has sealed/internal members that make subclassing painful
- FrameworkContentElement gives us DependencyProperty support, DataContext, and binding
- Matches the Xceed/DevExpress model and keeps the door open for future custom rendering

**Why flat (no BaseColumn/ColumnBase split) initially?**
- One class is simpler to iterate on
- The hierarchy adds value only when we have *different column types* or *reusable bases*
- Extract the hierarchy in Phase 3 after the API stabilizes

---

## Phase 1 — GridColumn Class (Core Migration)

### 1.1 GridColumn Class Skeleton

**Description:** Create the `GridColumn` class with all properties currently on the static `GridColumn` attached-property class, plus new layout/data properties. No behavior yet — just the class with dependency properties.

**Files to create:**
- `WPF/Controls/GridColumn.cs` — Replace the current static class (rename old to `GridColumnLegacy.cs` or merge)

**Properties to include:**

*Layout (from Xceed BaseColumn):*
| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `FieldName` | `string` | null | **Primary key** — drives Binding, Sort, Filter paths |
| `Header` | `object` | null | Falls back to FieldName if null |
| `HeaderCaption` | `string` | read-only | Resolved display text of Header |
| `Width` | `DataGridLength` | Auto | Column width |
| `MinWidth` | `double` | 20 | Minimum column width |
| `MaxWidth` | `double` | double.PositiveInfinity | Maximum column width |
| `Visible` | `bool` | true | Column visibility |
| `VisibleIndex` | `int` | -1 | Position among visible columns (-1 = auto) |
| `Fixed` | `bool` | false | Frozen column |
| `AllowMoving` | `bool` | true | User can drag-reorder |
| `AllowResizing` | `bool` | true | User can resize |
| `ShowInColumnChooser` | `bool` | true | Appears in column chooser |
| `ReadOnly` | `bool` | false | Prevents editing |

*Data/Display (migrated from current attached properties):*
| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `FilterMemberPath` | `string` | null | Overrides FieldName for filtering |
| `SortMemberPath` | `string` | null | Overrides FieldName for sorting |
| `ColumnDisplayName` | `string` | null | Overrides Header in filter UI |
| `DisplayStringFormat` | `string` | null | .NET format string |
| `DisplayValueConverter` | `IValueConverter` | null | Custom display converter |
| `DisplayConverterParameter` | `object` | null | Converter parameter |
| `DisplayMask` | `string` | null | Mask pattern |
| `FieldType` | `Type` | null | Auto-detected or explicit override |

*Filtering/Search (migrated from current attached properties):*
| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `EnableRuleFiltering` | `bool` | true | Complex filter UI |
| `DefaultSearchMode` | `DefaultSearchMode` | Contains | Simple search behavior |
| `UseCheckBoxInSearchBox` | `bool` | false | Force checkbox mode |
| `CustomSearchTemplate` | `Type` | null | Custom template type |
| `AllowFiltering` | `bool` | true | **New** — completely disable filtering |
| `AllowSorting` | `bool` | true | **New** — disable sorting |

*Select-All (migrated):*
| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `IsSelectAllColumn` | `bool` | false | Show select-all checkbox |
| `SelectAllScope` | `SelectAllScope` | FilteredRows | Scope of select-all |

**Internal properties (not in XAML, set by SearchDataGrid):**
| Property | Type | Notes |
|----------|------|-------|
| `ActualWidth` | `double` | Resolved rendered width |
| `ActualVisibleIndex` | `int` | Resolved position |
| `IsAutoGenerated` | `bool` | Whether grid auto-created this column |
| `InternalColumn` | `DataGridColumn` | The generated WPF column |
| `Owner` | `SearchDataGrid` | Parent grid reference |

**Acceptance criteria:**
- [ ] `GridColumn` class compiles, inherits `FrameworkContentElement`
- [ ] All dependency properties declared with correct types and defaults
- [ ] PropertyChanged callbacks stubbed (no behavior yet)
- [ ] Old `GridColumn` static class renamed to `GridColumnAttached` (or similar) to avoid conflict
- [ ] Solution builds with no errors

---

### 1.2 GridColumns Collection + Column Generation Factory

**Description:** Add a `GridColumns` collection to `SearchDataGrid`. When populated, the grid generates internal `DataGridColumn` instances from each `GridColumn` descriptor and adds them to the real `Columns` collection.

**Files to modify:**
- `WPF/Controls/SearchDataGrid.cs` — Add `GridColumns` DP, collection changed handler
- `WPF/Controls/GridColumn.cs` — Add `CreateDataGridColumn()` method

**Behavior:**
1. `SearchDataGrid.GridColumns` is a `FreezableCollection<GridColumn>` (supports XAML collection syntax)
2. On `Loaded` or when `GridColumns` changes, iterate and call `GridColumn.CreateDataGridColumn()`
3. Each `GridColumn` creates the appropriate WPF column:
   - `bool` FieldType → `DataGridCheckBoxColumn`
   - Everything else → `DataGridTextColumn`
4. Generated column gets: `Binding` from `FieldName`, `Header`, `Width`, `SortMemberPath`
5. Generated column gets attached properties set: `FilterMemberPath`, `DefaultSearchMode`, etc. (reuse existing attached properties internally during migration)
6. Generated columns added to `SearchDataGrid.Columns`
7. `GridColumn.InternalColumn` stores reference to the generated column

**Edge cases to handle:**
- `GridColumns` and `Columns` both populated → error or GridColumns wins?
  - **Decision:** If `GridColumns.Count > 0`, `Columns` is managed by the grid. Log warning if user manually adds to `Columns`.
- Column added/removed at runtime → sync to internal `Columns`
- `FieldName` changed after generation → regenerate that column

**Acceptance criteria:**
- [ ] `<sdg:SearchDataGrid.GridColumns>` works in XAML
- [ ] `<sdg:GridColumn FieldName="X" Header="Y" />` produces a visible, sortable, bound column
- [ ] Column appears with correct header text and data binding
- [ ] Adding/removing GridColumn items at runtime updates the grid
- [ ] GridColumn.InternalColumn is set after generation

---

### 1.3 ColumnSearchBox Integration

**Description:** Currently `ColumnSearchBox` reads configuration from `GridColumn` attached properties on the `DataGridColumn`. After 1.2, the internal `DataGridColumn` still has those attached properties set (bridge approach). This work item adds a direct path: `ColumnSearchBox` can resolve its configuration from the `GridColumn` descriptor.

**Files to modify:**
- `WPF/Controls/ColumnSearchBox.cs` — Add `GridColumnDescriptor` property, read config from it
- `WPF/Controls/SearchDataGrid.cs` — Wire `ColumnSearchBox.GridColumnDescriptor` during column init

**Behavior:**
1. When `SearchDataGrid` initializes a `ColumnSearchBox` for a column, check if that column was generated from a `GridColumn`
2. If yes, set `ColumnSearchBox.GridColumnDescriptor = gridColumn`
3. `ColumnSearchBox` property resolution order: `GridColumnDescriptor` → attached properties → defaults
4. This is a transitional step — both paths work simultaneously

**Acceptance criteria:**
- [ ] Filtering works identically whether using `GridColumn` or old attached properties
- [ ] `DefaultSearchMode`, `EnableRuleFiltering`, `UseCheckBoxInSearchBox` all respected from `GridColumn`
- [ ] `DisplayStringFormat`, `DisplayValueConverter`, `DisplayMask` all produce correct display values
- [ ] `AllowFiltering=False` hides the search box for that column
- [ ] `AllowSorting=False` disables sort click on the header

---

### 1.4 Column Chooser + Filter Panel Integration

**Description:** `ColumnChooser` and `FilterPanel` currently read column info from `DataGridColumn` + attached properties. Update them to use `GridColumn` descriptors when available.

**Files to modify:**
- `WPF/Controls/ColumnChooser.cs` — Read `Visible`, `ShowInColumnChooser`, `ColumnDisplayName` from `GridColumn`
- `WPF/Controls/FilterPanel.cs` — Read display names from `GridColumn`

**Behavior:**
1. Column Chooser shows/hides based on `GridColumn.ShowInColumnChooser`
2. Toggling visibility in Column Chooser sets `GridColumn.Visible`
3. `GridColumn.Visible` change propagates to `DataGridColumn.Visibility`
4. Filter Panel uses `GridColumn.ColumnDisplayName` (falling back to `Header` → `FieldName`)

**Acceptance criteria:**
- [ ] Column Chooser respects `ShowInColumnChooser` from GridColumn
- [ ] Hiding/showing columns through Chooser updates GridColumn.Visible
- [ ] Filter Panel shows correct column display names
- [ ] `GridColumn.Visible = false` in XAML → column hidden on load

---

### 1.5 SampleApp Migration

**Description:** Convert the SampleApp's `MainWindow.xaml` from attached-property columns to `GridColumn` syntax. This validates the full pipeline end-to-end.

**Files to modify:**
- `SampleApp/Views/MainWindow.xaml` — Replace all column declarations

**Before (24 columns using attached properties):**
```xml
<DataGridTextColumn Width="80"
                    sdg:GridColumn.FilterMemberPath="OrderNumber"
                    sdg:GridColumn.DefaultSearchMode="StartsWith"
                    sdg:GridColumn.EnableRuleFiltering="False"
                    SortMemberPath="OrderNumber"
                    Header="Order #"
                    Binding="{Binding OrderNumber}" />
```

**After:**
```xml
<sdg:GridColumn FieldName="OrderNumber" Header="Order #" Width="80"
                DefaultSearchMode="StartsWith" EnableRuleFiltering="False" />
```

**Acceptance criteria:**
- [ ] All 24 columns converted to GridColumn syntax
- [ ] Grid displays identically to before (data, headers, widths)
- [ ] All filtering modes work (text search, checkbox, rule editor)
- [ ] Display formatting works (currency, dates, percentages, Yes/No converter)
- [ ] Sorting works on all columns
- [ ] Column Chooser works
- [ ] Filter Panel shows correct names and active filters
- [ ] Select-all checkbox column works
- [ ] Copy commands copy formatted display values

---

### 1.6 Backwards Compatibility Layer

**Description:** Ensure the old attached-property syntax still works for existing consumers. The library is a NuGet package — breaking existing XAML would be unacceptable.

**Files to modify:**
- `WPF/Controls/GridColumnAttached.cs` (renamed from old `GridColumn.cs`)
- Mark all attached properties with `[Obsolete]` pointing to `GridColumn` class

**Behavior:**
1. Old syntax `sdg:GridColumnAttached.FilterMemberPath="X"` continues to work
2. `SearchDataGrid` checks for attached properties when no `GridColumnDescriptor` is set
3. Compiler warnings guide users to migrate

**Open question:** Keep the class name `GridColumn` for the new class and rename the old one? Or use a new name like `DataColumn`?
- **Recommendation:** Keep `GridColumn` for the new class (matches Xceed naming, cleaner API). Rename old static class to `ColumnSettings` or `GridColumnSettings` since that's what attached properties conceptually are — settings applied to a column.

**Acceptance criteria:**
- [ ] Old attached-property XAML compiles and runs without changes
- [ ] `[Obsolete]` warnings appear when using old syntax
- [ ] No runtime behavioral differences between old and new syntax
- [ ] CLAUDE.md updated to document both approaches

---

### 1.7 Documentation + Cleanup

**Description:** Update all documentation, clean up dead code, verify no regressions.

**Files to modify:**
- `CLAUDE.md` — Update architecture section, GridColumn description
- `docs/api-reference.md` — Document GridColumn API
- `docs/getting-started.md` — Update column declaration examples
- `README.md` — Update usage examples

**Acceptance criteria:**
- [ ] All docs reference new GridColumn syntax as primary
- [ ] Old syntax documented as "legacy/deprecated"
- [ ] No compiler warnings in the solution (beyond intentional `[Obsolete]`)
- [ ] All SampleApp scenarios verified working

---

## Phase 2 — Type-Aware Column Behavior

> **Prerequisite:** Phase 1 complete

**Goal:** `GridColumn` auto-detects data types from `FieldName` and configures itself intelligently.

### 2.1 Type Resolution

**Description:** When `SearchDataGrid.ItemsSource` is set and `GridColumns` are present, resolve each `GridColumn.FieldName` to a CLR property type via reflection. Store in `GridColumn.FieldType`.

**Behavior:**
- Reflect on the first item or `IItemProperties` to get property types
- Map CLR types to `ColumnDataType`: `string`→String, `int/decimal/double`→Number, `DateTime`→DateTime, `bool`→Boolean, `enum`→Enum
- Set `FieldType` only if user hasn't explicitly set it

**Acceptance criteria:**
- [ ] `FieldType` auto-populated for all columns when ItemsSource is set
- [ ] Explicit `FieldType` on GridColumn overrides auto-detection
- [ ] Nullable types resolved correctly (e.g., `decimal?` → Number)

### 2.2 Auto-Configuration by Type

**Description:** Based on `FieldType`, auto-set sensible defaults for properties the user didn't explicitly set.

| Detected Type | Auto-Configuration |
|---------------|-------------------|
| `bool` | `UseCheckBoxInSearchBox=True`, generates `DataGridCheckBoxColumn` |
| `DateTime` | `DefaultSearchMode=Equals` |
| `enum` | `DefaultSearchMode=Equals`, populate search dropdown from enum values |
| `decimal`/`double` | (no auto-format — user must specify C2, P0, etc.) |
| `string` | `DefaultSearchMode=Contains` (already the default) |

**Acceptance criteria:**
- [ ] Bool columns auto-generate checkbox search without `UseCheckBoxInSearchBox="True"`
- [ ] User-set properties always win over auto-configuration
- [ ] SampleApp columns can be simplified (remove redundant property settings)

### 2.3 Auto-Generated Columns

**Description:** When `AutoGenerateColumns="True"` and `GridColumns` is empty, auto-create `GridColumn` instances from the data source properties.

**Acceptance criteria:**
- [ ] Setting `ItemsSource` with no `GridColumns` produces auto-generated columns
- [ ] Each auto-generated column has `IsAutoGenerated=True`
- [ ] Auto-generated columns respect `[Browsable(false)]` and `[Display]` attributes
- [ ] Explicit `GridColumns` suppress auto-generation entirely

---

## Phase 3 — Project Restructuring + Base Class Hierarchy

> **Prerequisite:** Phase 2 complete, API stable through real usage

**Goal:** Extract the inheritance hierarchy and create shared base project.

### 3.1 New Project: WWSearchDataGrid.Modern.Controls

**Description:** Create a new class library for base classes reusable outside the DataGrid.

**Classes to extract:**
- `WWFrameworkContentElement` — Property-hiding base
- `BaseColumn` — Layout properties (Width, Visible, Header, Fixed, AllowMoving, AllowResizing, ShowInColumnChooser)

**What stays in WPF project:**
- `ColumnBase : BaseColumn` — Data/filter/sort properties (FieldName, FieldType, AllowFiltering, AllowSorting, all display properties)
- `GridColumn : ColumnBase` — Grid-specific features (grouping, if added)

### 3.2 Hierarchy Implementation

**Final hierarchy:**
```
FrameworkContentElement
  └── WWFrameworkContentElement  (hides noise properties)
      └── BaseColumn             (layout: width, visibility, header)
          └── ColumnBase         (data: field binding, filtering, sorting, display)
              └── GridColumn     (grid-specific: future grouping, etc.)
```

**Acceptance criteria:**
- [ ] New project compiles and is referenced by WPF project
- [ ] `GridColumn` XAML usage unchanged (no consumer-facing breaks)
- [ ] Properties distributed across correct hierarchy levels
- [ ] Base classes usable independently of SearchDataGrid

---

## Phase 4 — EditSettings / InplaceEditors

> **Prerequisite:** Phase 3 complete (or can be done after Phase 1 if hierarchy is deferred)

**Goal:** Column-level editor configuration — the "better built-in editing" from the original Xceed inspiration.

### 4.1 BaseEditSettings Framework

**Description:** Abstract base class for editor configuration. Each concrete implementation knows how to produce a `DataTemplate` for display and editing.

```xml
<sdg:GridColumn FieldName="Status">
    <sdg:GridColumn.EditSettings>
        <sdg:ComboBoxEditSettings ItemsSource="{Binding Statuses}" DisplayMember="Name" ValueMember="Id" />
    </sdg:GridColumn.EditSettings>
</sdg:GridColumn>
```

**Classes to create:**
- `BaseEditSettings` (abstract) — `CreateDisplayTemplate()`, `CreateEditTemplate()`
- `TextEditSettings` — Plain text editor (default)
- `ComboBoxEditSettings` — Dropdown editor with `ItemsSource`, `DisplayMember`, `ValueMember`
- `CheckBoxEditSettings` — Checkbox editor
- `DateEditSettings` — Date picker
- `NumericEditSettings` — NumericUpDown with min/max/increment
- `MaskedEditSettings` — Masked text input

### 4.2 Cell Template Generation

**Description:** When `GridColumn.EditSettings` is set, use it to generate `CellTemplate` (display) and `CellEditingTemplate` (editing) on the internal `DataGridColumn`.

**Acceptance criteria:**
- [ ] `ComboBoxEditSettings` produces a working dropdown in edit mode, text display in view mode
- [ ] `DateEditSettings` produces a DatePicker in edit mode
- [ ] `NumericEditSettings` produces a NumericUpDown with validation
- [ ] Tab/Enter navigation works through editor cells
- [ ] Escape cancels editing and reverts value
- [ ] Cell value changes propagate to the bound data source

### 4.3 Default EditSettings by Type

**Description:** When no explicit `EditSettings` is provided, auto-create defaults based on `FieldType`:

| Type | Default EditSettings |
|------|---------------------|
| `string` | `TextEditSettings` |
| `bool` | `CheckBoxEditSettings` |
| `DateTime` | `DateEditSettings` |
| `int`/`decimal`/`double` | `NumericEditSettings` |
| `enum` | `ComboBoxEditSettings` (auto-populated from enum values) |

---

## Summary: Session Planning Guide

| Work Item | Phase | Dependencies | Complexity | Files Changed |
|-----------|-------|--------------|------------|---------------|
| 1.1 GridColumn skeleton | 1 | None | Low | 1 new, 1 renamed |
| 1.2 Collection + factory | 1 | 1.1 | Medium | 2 modified |
| 1.3 ColumnSearchBox integration | 1 | 1.2 | Medium | 2 modified |
| 1.4 Chooser + FilterPanel | 1 | 1.2 | Low | 2 modified |
| 1.5 SampleApp migration | 1 | 1.3, 1.4 | Low | 1 modified |
| 1.6 Backwards compat | 1 | 1.3 | Low | 1 renamed, obsoletion |
| 1.7 Documentation | 1 | 1.5, 1.6 | Low | 4 modified |
| 2.1 Type resolution | 2 | Phase 1 | Low | 2 modified |
| 2.2 Auto-configuration | 2 | 2.1 | Medium | 2 modified |
| 2.3 Auto-generated columns | 2 | 2.2 | Medium | 2 modified |
| 3.1 New project | 3 | Phase 2 | Medium | New project + refactor |
| 3.2 Hierarchy extraction | 3 | 3.1 | Medium | Multi-file refactor |
| 4.1 EditSettings framework | 4 | Phase 1+ | High | 6+ new files |
| 4.2 Cell template generation | 4 | 4.1 | High | 2 modified |
| 4.3 Default editors by type | 4 | 4.2, 2.1 | Medium | 1 modified |

**Start each session by stating which work item you're tackling.** The acceptance criteria tell you when you're done.
