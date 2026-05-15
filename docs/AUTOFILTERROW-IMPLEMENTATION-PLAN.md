# AutoFilterRow Spec-Conformance — Phased Implementation Plan

## Context

A documentation spec for an **Automatic Filter Row** (DevExpress-style API) was supplied and the current `WWSearchDataGrid.Modern` implementation was flagged as "missing and incorrectly implementing some aspects." An audit confirmed gaps across grid-level DPs, column-level DPs, default-criteria behavior, template plumbing, and the absence of an `EditGridCellData`-style data-context hierarchy for template authors.

The work is split across **separate chat sessions** by phase, so each phase is independently shippable, bounded to one session, and clearly preceded by its prerequisites. The only external consumer of the affected API surface is the in-solution `WWSearchDataGrid.Modern.SampleApp`, so deprecation ceremony is internal-facing only: `[Obsolete]` markings serve to track migration scope rather than to warn third-party consumers, and they get removed in a dedicated cleanup phase (Phase 5) once the sample app is verified working.

Confirmed direction:

| Decision | Choice |
|---|---|
| Naming where the spec collides with existing API | **Spec names, deprecate old** — add new DPs with the spec's names; mark existing equivalents `[Obsolete]` where the spec name supersedes them |
| String column default filter criteria | **Change `Contains` → `StartsWith` (`BeginsWith`)** — breaking; accepted |
| Templates + `EditGridCellData` hierarchy | **Full spec** — phased into Phase 3 |
| `ColumnFilterMode` (Value vs DisplayText) | **Verify first** — `IDisplayValueProvider` is already wired end-to-end (`SearchTemplateController.DisplayValueProvider`, `UseRawComparison` already drives raw vs display predicate paths). May only need public-API formalization, not a new pipeline |

---

## Architectural decisions (apply across phases)

These resolve recurring ambiguities once so individual phases don't relitigate them.

### D1. `ShowCriteriaInAutoFilterRow` override shape
**`bool?` on `GridColumn`; `null` = inherit from grid.** Mirrors the existing `IsLiveFilteringOverride` precedent at `ColumnFilterControl.cs:130-133`. One property, one DP — no separate `IsOverridden` flag.

### D2. `AutoFilterRowClearButtonMode` enum
**New file:** `WWSearchDataGrid.Modern.WPF\Controls\Enums\AutoFilterRowClearButtonMode.cs`

```csharp
public enum AutoFilterRowClearButtonMode
{
    Never = 0,    // hidden always
    Always = 1,   // visible whenever HasActiveFilter is true, on either surface
    Display = 2,  // visible when HasActiveFilter and !IsFilterCellEditing (read-only display surface)
    Edit = 3,     // visible when HasActiveFilter and IsFilterCellEditing (edit surface)
}
```

Display / edit are the same display/edit split that drives the row's actual data-cell editor — see D9. Grid default is `Always`.

### D9. Filter-row display / edit surface state machine
The auto-filter row mirrors `DataGridCell`'s display/edit promotion. `ColumnFilterControl` exposes a read-only `IsFilterCellEditing` DP that tracks `IsKeyboardFocusWithin`; `RefreshEditor` picks between `BaseEditSettings.CreateFilterDisplay` (read-only TextBlock, no decoration buttons) and `BaseEditSettings.CreateFilterEditor` (full editor with chrome) based on its value. Click on the read-only surface or tab into the cell promotes to edit; focus leaving the cell demotes back to display. A `_isSwappingSurfaces` guard masks the brief focus dip during the swap so the transition doesn't oscillate.

### D3. `AllowAutoFilter` vs existing `AllowFiltering`
**Keep both — distinct semantics.**
- `AllowFiltering=false` (existing) → cell **hidden** (`Visibility=Collapsed`, takes no space). Currently the only mechanism.
- `AllowAutoFilter=false` (new, spec) → cell **disabled** (`IsEnabled=false`, greyed but space preserved).

This matches the spec's "disable the AutomaticFilter Row cell" wording. Do not mark `AllowFiltering` `[Obsolete]` — it serves a different visual-layout role.

### D4. `DefaultSearchType` vs existing `DefaultSearchMode`
**Same concept. Add `DefaultSearchType` as the spec-named DP backed by the same storage as `DefaultSearchMode`. Mark `DefaultSearchMode` `[Obsolete("Use DefaultSearchType")]`.**

**Pick:** single shared `DependencyProperty` (rename storage to `DefaultSearchTypeProperty`, keep `DefaultSearchModeProperty` as a `[Obsolete]` static alias pointing at the same DP). Both CLR wrappers read/write the same DP — no synchronization needed.

### D5. `EditGridCellData` hierarchy location
**WPF assembly only.** The hierarchy derives from `DispatcherObject`/`DependencyObject` (WPF). Keep all 8 types in `WWSearchDataGrid.Modern.WPF\Data\` (new folder). No core-side abstraction needed; consumers binding to these types are XAML template authors who already live in WPF.

### D6. `EditGridCellData` for filter-row context
**Use the full type as-is; `RowData` is `null` in filter-row context.** Spec describes it for cell editors generally; XAML bindings against `RowData.Row.X` will simply fall silent (binding error suppressed by `BindingValidationError` setup). Do not introduce a separate `FilterCellData` subclass — the same template should work in both filter and cell contexts.

### D7. Template plumbing model
**When a template is set, it owns the visual tree. The host still exposes `Value` via `EditGridCellData.Value` (which routes to `ColumnFilterControl.SearchText`/`SearchValue`/`FilterCheckboxState` per editor shape).** Templates that don't bind to `Value` simply won't filter — that's the template author's responsibility, matching DevExpress behavior.

### D8. `FilterRowDelay` × `ImmediateUpdateAutoFilter` interaction
- `ImmediateUpdateAutoFilter=false` → delay is **ignored**; filter fires only on Enter/Tab/lost-focus.
- `ImmediateUpdateAutoFilter=true` and `FilterRowDelay=0` → fire on every keystroke (current behavior).
- `ImmediateUpdateAutoFilter=true` and `FilterRowDelay>0` → debounce N ms before firing.

The `DispatcherTimer` is reset on each keystroke; commit handlers (Enter/Tab/blur) cancel the timer and fire immediately.

---

## Phase 1 — Spec-conformance surface (no template work)

### Goal
Add the spec's DP surface for grid- and column-level settings that don't require pipeline changes. Fix the string default. Implement the spec rule that a column whose default criteria is excluded from `AllowedSearchTypes` has its filter row cell disabled.

### Files modified
- `WWSearchDataGrid.Modern.WPF\Controls\SearchDataGrid\SearchDataGrid.cs`
- `WWSearchDataGrid.Modern.WPF\Controls\GridColumn.cs`
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\ColumnFilterControl.cs`
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\IColumnFilterHost.cs` (mirror DPs if applicable)
- `WWSearchDataGrid.Modern.WPF\Themes\Controls\FilterRow\AutoFilterRow.xaml`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\Filtering\SearchModesSampleView.xaml` (smoke-test the new DPs in the sample app)

### Detailed changes

#### 1.1 Grid-level: `ShowCriteriaInAutoFilterRow` (bool, default `false`)
On `SearchDataGrid`:
```csharp
public static readonly DependencyProperty ShowCriteriaInAutoFilterRowProperty =
    DependencyProperty.Register(nameof(ShowCriteriaInAutoFilterRow), typeof(bool), typeof(SearchDataGrid),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
public bool ShowCriteriaInAutoFilterRow { get => (bool)GetValue(...); set => SetValue(...); }
```
`Inherits` propagates the grid setting into descendant `ColumnFilterControl`s without manual wiring.

#### 1.2 Column-level: `ShowCriteriaInAutoFilterRow` override (bool?, default `null`)
On `GridColumn` (per D1). When non-null, takes precedence over the grid value.

#### 1.3 XAML: bind `PART_SearchTypeSelector` visibility
In `AutoFilterRow.xaml` lines 211-232, add a `Visibility` setter driven by an effective-bool resolved per cell. The cleanest path: add an `EffectiveShowCriteria` read-only DP on `ColumnFilterControl` (computed as `column.ShowCriteriaInAutoFilterRow ?? grid.ShowCriteriaInAutoFilterRow`), and bind:
```xml
<sdg:SearchTypeSelector x:Name="PART_SearchTypeSelector"
                        Visibility="{Binding EffectiveShowCriteria, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource BoolToVisibilityConverter}}"
                        ... existing attrs ...>
```
Recompute `EffectiveShowCriteria` in `RefreshEditor()` and when the grid- or column-level DP changes.

#### 1.4 Column-level: `AllowAutoFilter` (bool, default `true`)
On `GridColumn` (per D3). Wires through `IColumnFilterHost.IsFilterEnabled` (new — mirror of the existing `IsFilterVisible`) and into `ColumnFilterControl`:
```csharp
// ColumnFilterControl.cs near line 683
if (GridColumn != null && !GridColumn.AllowFiltering)
{
    Visibility = Visibility.Collapsed;
    return;
}
IsEnabled = GridColumn?.AllowAutoFilter ?? true;
```
Property-changed callback on `GridColumn.AllowAutoFilterProperty` writes through to the matching host.

#### 1.5 Cell + Style: `AutoFilterRowCellStyle`
On `SearchDataGrid`:
```csharp
public static readonly DependencyProperty AutoFilterRowCellStyleProperty =
    DependencyProperty.Register(nameof(AutoFilterRowCellStyle), typeof(Style), typeof(SearchDataGrid),
        new FrameworkPropertyMetadata(null));
```
On `GridColumn`: same DP, default `null`. In XAML, the `ColumnFilterControl` style is selected via:
```xml
<Setter Property="Style"
        Value="{Binding GridColumn.AutoFilterRowCellStyle, RelativeSource={RelativeSource Self},
                FallbackValue={Binding (sdg:SearchDataGrid.AutoFilterRowCellStyle), RelativeSource={RelativeSource Self}}}"/>
```
Or — simpler — resolve in `ColumnFilterControl.OnApplyTemplate` and assign `Style` programmatically with the column override taking precedence over the grid setting; fall back to the theme key when both are null.

#### 1.6 `DefaultSearchType` (per D4)
- Rename `DefaultSearchModeProperty` storage internally to `DefaultSearchTypeProperty`.
- Keep `DefaultSearchModeProperty` as a `public static readonly DependencyProperty DefaultSearchModeProperty = DefaultSearchTypeProperty;` alias with `[Obsolete]` on the CLR wrapper.
- Add `public DefaultSearchType DefaultSearchType { ... }` CLR wrapper.
- The `DefaultSearchType` enum is the new name for `DefaultSearchMode`; add it in `WWSearchDataGrid.Modern.WPF\Controls\Enums\DefaultSearchType.cs` (or core) with values `Contains`, `StartsWith` (alias `BeginsWith` static field), `EndsWith`, `Equals`. Mark the old `DefaultSearchMode` enum `[Obsolete]` if it's a separate type, or keep it and add the new enum as a structural duplicate with bidirectional cast operators.

**Recommended:** keep `DefaultSearchMode` enum, add `DefaultSearchType` as an alias type with implicit conversions. The DP storage is unified.

#### 1.7 Fix string default: `Contains` → `StartsWith`
In `GridColumn.cs:497` (`ApplyTypeBasedDefaults`) add an explicit string branch:
```csharp
else if (underlying == typeof(string))
{
    if (!IsDefaultSearchModeExplicit)
        SetAutoDefaultSearchMode(DefaultSearchMode.StartsWith);
}
```
Also change the registered default at line 564 from `DefaultSearchMode.Contains` to `DefaultSearchMode.StartsWith`. Update the XML doc comment at lines 490-495 to reflect the new mapping. Update sample app text in `SearchModesSampleView.xaml:28` and `SampleCatalog.cs:62`.

#### 1.8 Cell-disable when default criteria is excluded
Per spec: "If you hide a column's default filter criteria, the SDG disables the corresponding cell."

In `ColumnFilterControl.OnSelectedSearchTypeChanged` and `RefreshEditor`:
```csharp
private void UpdateEffectiveIsCellEnabled()
{
    var resolved = ResolveDefaultSearchMode();
    var allowed = SupportedSearchTypes ?? Enumerable.Empty<SearchType>();
    var mapped = MapDefaultSearchModeToSearchType(resolved);
    bool defaultAllowed = allowed.Contains(mapped);

    // Per spec: when default criteria is excluded, disable the cell —
    // unless the user explicitly opted into a different SelectedSearchType.
    IsEnabled = defaultAllowed
                || SelectedSearchType != mapped
                || (GridColumn?.AllowAutoFilter ?? true) == false;
}
```
Invoke from `RefreshEditor`, from `OnSupportedSearchTypesChanged`, and from `OnDefaultSearchModePropertyChanged` (via the column→host write-through).

### XAML changes summary
- `AutoFilterRow.xaml:211-232` — add `Visibility` binding on `PART_SearchTypeSelector` keyed to `EffectiveShowCriteria`
- `AutoFilterRow.xaml:166-351` — style resolution honoring `AutoFilterRowCellStyle`

### Verification
1. Run `WWSearchDataGrid.Modern.SampleApp`, open `Filtering / Search Modes`. Add columns with mixed `ShowCriteriaInAutoFilterRow` settings; verify the selector button appears/disappears per cell.
2. Bind `<SearchDataGrid ShowCriteriaInAutoFilterRow="True">` and verify all columns show criteria selector by default; override one column with `ShowCriteriaInAutoFilterRow="False"` and verify only that cell hides its selector.
3. Set `AllowAutoFilter="False"` on one column and verify the cell is greyed (takes space) — distinct from `AllowFiltering="False"` (cell collapsed entirely).
4. Set `AutoFilterRowCellStyle` on grid and on one column; verify column override wins.
5. Confirm string column now defaults to `StartsWith`: open the sample, type "A" in a name column, only `A*` matches should appear (not `*A*`).
6. Set `AllowedSearchTypes` (existing `SupportedSearchTypes`) on a column to a list that excludes the column's default; confirm cell is disabled per spec.
7. Build the WPF project — no XAML or C# compilation errors. Run any existing unit tests in `WWSearchDataGrid.Modern.Tests` (if present).

### Out of scope
- `FilterRowDelay`, debounce, `AutoFilterRowClearButtonMode`, `ImmediateUpdateAutoFilter` — Phase 2.
- `AutoFilterRowDisplayTemplate`, `AutoFilterRowEditTemplate`, `EditGridCellData` hierarchy — Phase 3.
- `ColumnFilterMode` enum decision — Phase 4.
- Removing deprecated symbols — Phase 5.

### Depends on
- Nothing. Phase 1 is the starting point.

---

## Phase 2 — Behavioral wiring

### Goal
Replace the dead `System.Timers.Timer` infrastructure with a working `DispatcherTimer`-based debounce keyed off `FilterRowDelay`. Promote `IsLiveFilteringOverride` to the spec-named `ImmediateUpdateAutoFilter`. Replace hardcoded clear-button visibility with `AutoFilterRowClearButtonMode`.

### Files modified
- `WWSearchDataGrid.Modern.WPF\Controls\SearchDataGrid\SearchDataGrid.cs` — add `FilterRowDelayProperty`, `AutoFilterRowClearButtonModeProperty`
- `WWSearchDataGrid.Modern.WPF\Controls\GridColumn.cs` — add `ImmediateUpdateAutoFilter`
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\ColumnFilterControl.cs` — replace dead `_changeTimer` with `DispatcherTimer`; mark `IsLiveFilteringOverride` `[Obsolete]`
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\ColumnFilterControlTextFilter.cs` — wire debounce into the keystroke path; remove `Application.Current?.Dispatcher.Invoke` from `OnChangeTimerElapsed`
- `WWSearchDataGrid.Modern.WPF\Controls\Enums\AutoFilterRowClearButtonMode.cs` — new file (D2)
- `WWSearchDataGrid.Modern.WPF\Converters\ClearButtonModeToVisibilityConverter.cs` — new file (multi-value converter)
- `WWSearchDataGrid.Modern.WPF\Themes\Controls\FilterRow\AutoFilterRow.xaml` — replace hardcoded clear-button triggers with binding to the mode enum

### Detailed changes

#### 2.1 `FilterRowDelay` (int ms, default `0`)
On `SearchDataGrid`:
```csharp
public static readonly DependencyProperty FilterRowDelayProperty =
    DependencyProperty.Register(nameof(FilterRowDelay), typeof(int), typeof(SearchDataGrid),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits));
```
`Inherits` lets per-cell `ColumnFilterControl` read it without explicit wiring.

In `ColumnFilterControl.cs:68`, replace:
```csharp
private Timer _changeTimer;  // System.Timers.Timer — dead code
```
with:
```csharp
private DispatcherTimer _changeTimer;
```
Remove `using System.Timers;`. Initialize lazily on first SearchText change:
```csharp
private void EnsureChangeTimer()
{
    if (_changeTimer != null) return;
    _changeTimer = new DispatcherTimer(DispatcherPriority.Background);
    _changeTimer.Tick += OnChangeTimerTick;
}
```
Rewrite `OnChangeTimerElapsed` → `OnChangeTimerTick` (no Dispatcher.Invoke needed since DispatcherTimer fires on UI thread):
```csharp
private void OnChangeTimerTick(object sender, EventArgs e)
{
    _changeTimer.Stop();
    if (_filterPopup?.IsOpen == true) return;
    if (!string.IsNullOrWhiteSpace(SearchText))
        UpdateSimpleFilter();
}
```
In `OnSearchTextChanged` (ColumnFilterControlTextFilter.cs:24-50) replace immediate apply with:
```csharp
if (ctl.EffectiveIsLiveFilteringEnabled)
{
    ctl.CreateTemporaryTemplateImmediate();
    int delay = ctl.SourceDataGrid?.FilterRowDelay ?? 0;
    if (delay <= 0)
    {
        ctl.UpdateSimpleFilter();
    }
    else
    {
        ctl.EnsureChangeTimer();
        ctl._changeTimer.Interval = TimeSpan.FromMilliseconds(delay);
        ctl._changeTimer.Stop();
        ctl._changeTimer.Start();
    }
}
```
In `CommitSearchText()` (line 333), stop the timer (so a pending tick doesn't double-apply): `_changeTimer?.Stop();` — already present at lines 243/292/358, audit those callsites for correctness against the new model.

In `OnControlUnloaded` (line 441), replace `_changeTimer.Elapsed -= OnChangeTimerElapsed; _changeTimer.Dispose();` with `_changeTimer.Tick -= OnChangeTimerTick; _changeTimer = null;` (DispatcherTimer is not `IDisposable`).

#### 2.2 `ImmediateUpdateAutoFilter` (bool, default `true`)
On `GridColumn`:
```csharp
public static readonly DependencyProperty ImmediateUpdateAutoFilterProperty =
    DependencyProperty.Register(nameof(ImmediateUpdateAutoFilter), typeof(bool), typeof(GridColumn),
        new PropertyMetadata(true, OnImmediateUpdateAutoFilterChanged));
public bool ImmediateUpdateAutoFilter { ... }
```
Callback writes through to `ColumnFilterControl.IsLiveFilteringOverride`:
```csharp
private static void OnImmediateUpdateAutoFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var gc = (GridColumn)d;
    var host = gc.Owner?.DataColumns?.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn) as ColumnFilterControl;
    if (host == null) return;
    host.IsLiveFilteringOverride = (bool)e.NewValue;
}
```
Mark `ColumnFilterControl.IsLiveFilteringOverride` `[Obsolete("Use GridColumn.ImmediateUpdateAutoFilter")]`. Keep functioning.

#### 2.3 `AutoFilterRowClearButtonMode` (enum, default `Always`)
New file per D2. On `SearchDataGrid`:
```csharp
public static readonly DependencyProperty AutoFilterRowClearButtonModeProperty =
    DependencyProperty.Register(nameof(AutoFilterRowClearButtonMode), typeof(AutoFilterRowClearButtonMode), typeof(SearchDataGrid),
        new FrameworkPropertyMetadata(AutoFilterRowClearButtonMode.Always, FrameworkPropertyMetadataOptions.Inherits));
```
The clear button's `Visibility` in `AutoFilterRow.xaml` resolves via the multi-value `ClearButtonModeToVisibilityConverter` against `(mode, HasActiveFilter, IsFilterCellEditing)`. The third input is the focus-state DP added by D9 — true while focus is inside the control (edit surface rendering), false while focus is outside (display surface rendering).

Returns:
- `Never` → Collapsed
- `Always` → Visible iff `HasActiveFilter`
- `Display` → Visible iff `HasActiveFilter && !IsFilterCellEditing`
- `Edit` → Visible iff `HasActiveFilter && IsFilterCellEditing`

### XAML changes summary
- `AutoFilterRow.xaml:284-343` — replace hardcoded triggers with mode-driven converter binding

### Verification
1. Set `FilterRowDelay="500"` on the grid. Type quickly in a filter cell; verify the grid filter applies once 500ms after the last keystroke (not on every keystroke).
2. Set `FilterRowDelay="500"` and `ImmediateUpdateAutoFilter="False"` on a column; verify delay is **ignored** — filter fires only on Enter/Tab/blur.
3. Set `ImmediateUpdateAutoFilter="False"` on a column; verify deprecation warning on `IsLiveFilteringOverride` build path is the only emitted analyzer note; type → no filter; press Enter → filter applies.
4. Set `AutoFilterRowClearButtonMode="Always"`; type into a text cell, select a combobox value, pick a date — verify clear button appears in every editor surface as soon as the filter applies, and disappears when cleared.
5. Set `AutoFilterRowClearButtonMode="Never"`; verify clear button is never visible.
6. Set `AutoFilterRowClearButtonMode="Display"`; apply a filter, then click outside the cell — verify the clear button appears on the read-only display surface. Click back into the cell — the clear button disappears as the edit surface materializes.
7. Set `AutoFilterRowClearButtonMode="Edit"`; apply a filter, click into the cell — verify the clear button appears alongside the editor. Click out — clear button hides along with the editor chrome.
8. Tab through columns with `ShowCriteriaInAutoFilterRow=true` — verify the display surface transitions to the edit surface (and the decoration buttons appear) as focus arrives.
9. Use a `ComboBoxEditSettings` column: select a value, click outside — verify the cell shows the selected item's display text (not the raw value / id) with no dropdown chevron. Click in — full ComboBox with chevron returns.
10. Use a `DateEditSettings` column: pick a date, click outside — verify formatted date text (no calendar glyph). Click in — segmented editor with calendar glyph returns.

### Out of scope
- Templates and `EditGridCellData` — Phase 3.
- `ColumnFilterMode` enum decision — Phase 4.
- Removing `[Obsolete]` markers — Phase 5.
- `SearchTemplateController`-side date predicate rewrite beyond the `RoundDateTime` flag is fair game in Phase 2; if it expands beyond a single-flag plumbing, defer the deeper refactor.

### Depends on
- **Phase 1** completed (uses the new property changed callback registration patterns; `EffectiveShowCriteria` recomputation is unchanged).

---

## Phase 3 — Templates + `EditGridCellData` hierarchy

### Goal
Introduce the full `EditGridCellData` data-context hierarchy. Add `AutoFilterRowDisplayTemplate` and `AutoFilterRowEditTemplate` DPs on `GridColumn`. Refactor `ColumnFilterControl.RefreshEditor()` to honor a user-supplied template (replacing the `BaseEditSettings.CreateFilterEditor()` UIElement) and to set the new `EditGridCellData` instance as the cell content's `DataContext`.

### Files added
- `WWSearchDataGrid.Modern.WPF\Data\DataObjectBase.cs`
- `WWSearchDataGrid.Modern.WPF\Data\EditableDataObject.cs`
- `WWSearchDataGrid.Modern.WPF\Data\GridDataBase.cs`
- `WWSearchDataGrid.Modern.WPF\Data\GridColumnData.cs`
- `WWSearchDataGrid.Modern.WPF\Data\GridCellData.cs`
- `WWSearchDataGrid.Modern.WPF\Data\EditGridCellData.cs`

### Files modified
- `WWSearchDataGrid.Modern.WPF\Controls\GridColumn.cs` — add `AutoFilterRowDisplayTemplate`, `AutoFilterRowEditTemplate` DPs
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\ColumnFilterControl.cs` — `RefreshEditor()` template branch; expose `FilterCellData` (the `EditGridCellData` instance) as a read-only DP for binding
- `WWSearchDataGrid.Modern.WPF\Themes\Controls\FilterRow\AutoFilterRow.xaml` — `PART_EditorHost` honors `ContentTemplate` when set

### Detailed changes

#### 3.1 Class hierarchy (per D5, D6)

```csharp
// DataObjectBase.cs
public abstract class DataObjectBase : DependencyObject { }

// EditableDataObject.cs
public abstract class EditableDataObject : DataObjectBase
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(EditableDataObject),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
}

// GridDataBase.cs
public abstract class GridDataBase : EditableDataObject
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(GridDataBase));
    public object Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }
}

// GridColumnData.cs
public abstract class GridColumnData : GridDataBase
{
    public static readonly DependencyProperty ColumnProperty =
        DependencyProperty.Register(nameof(Column), typeof(GridColumn), typeof(GridColumnData));
    public GridColumn Column { get => (GridColumn)GetValue(ColumnProperty); set => SetValue(ColumnProperty, value); }
}

// GridCellData.cs
public class GridCellData : GridColumnData
{
    public static readonly DependencyProperty RowDataProperty =
        DependencyProperty.Register(nameof(RowData), typeof(object), typeof(GridCellData));
    public object RowData { get => GetValue(RowDataProperty); set => SetValue(RowDataProperty, value); }

    public static readonly DependencyProperty IsFocusedCellProperty =
        DependencyProperty.Register(nameof(IsFocusedCell), typeof(bool), typeof(GridCellData));
    public bool IsFocusedCell { get => (bool)GetValue(IsFocusedCellProperty); set => SetValue(IsFocusedCellProperty, value); }

    public bool IsSelected { get; internal set; }
    public SelectionState SelectionState { get; internal set; }

    public static readonly DependencyProperty DisplayMemberBindingValueProperty =
        DependencyProperty.Register(nameof(DisplayMemberBindingValue), typeof(object), typeof(GridCellData));
    public object DisplayMemberBindingValue { get => GetValue(DisplayMemberBindingValueProperty); set => SetValue(DisplayMemberBindingValueProperty, value); }

    public SearchDataGrid View { get; internal set; }  // exposes View.DataContext binding path
}

// EditGridCellData.cs
public sealed class EditGridCellData : GridCellData, IDataErrorInfo
{
    public string Error => string.Empty;
    public string this[string columnName] => string.Empty;
}
```

`SelectionState` enum: add to `WWSearchDataGrid.Modern.WPF\Controls\Enums\SelectionState.cs` (None, Selected, Focused) if it doesn't already exist.

#### 3.2 `AutoFilterRowDisplayTemplate`, `AutoFilterRowEditTemplate` on `GridColumn`
```csharp
public static readonly DependencyProperty AutoFilterRowDisplayTemplateProperty =
    DependencyProperty.Register(nameof(AutoFilterRowDisplayTemplate), typeof(DataTemplate), typeof(GridColumn),
        new PropertyMetadata(null, OnAutoFilterRowTemplateChanged));
public DataTemplate AutoFilterRowDisplayTemplate { ... }

public static readonly DependencyProperty AutoFilterRowEditTemplateProperty =
    DependencyProperty.Register(nameof(AutoFilterRowEditTemplate), typeof(DataTemplate), typeof(GridColumn),
        new PropertyMetadata(null, OnAutoFilterRowTemplateChanged));
public DataTemplate AutoFilterRowEditTemplate { ... }
```

#### 3.3 `RefreshEditor()` rewrite (per D7)
In `ColumnFilterControl.cs:380-400`:
```csharp
private void RefreshEditor()
{
    if (_editorHost == null) return;
    DetachFilterEditor();

    var template = GridColumn?.AutoFilterRowEditTemplate ?? GridColumn?.AutoFilterRowDisplayTemplate;
    if (template != null)
    {
        EnsureFilterCellData();
        _editorHost.ContentTemplate = template;
        _editorHost.Content = _filterCellData;  // EditGridCellData
        _filterEditor = null;  // editor is template-managed; no PreviewKeyDown hook
    }
    else
    {
        var settings = GridColumn?.EditSettings ?? new TextEditSettings();
        var editor = settings.CreateFilterEditor(this);
        _editorHost.ContentTemplate = null;
        _editorHost.Content = editor;
        _filterEditor = editor;
        if (_filterEditor != null)
            _filterEditor.PreviewKeyDown += OnFilterEditorPreviewKeyDown;
    }

    SupportedSearchTypes = (GridColumn?.EditSettings ?? new TextEditSettings())
        .GetSupportedFilterSearchTypes(ColumnDataType, true);
}

private EditGridCellData _filterCellData;
private void EnsureFilterCellData()
{
    if (_filterCellData != null) return;
    _filterCellData = new EditGridCellData
    {
        Column = GridColumn,
        RowData = null,
        View = SourceDataGrid,
    };
    // Two-way-bind EditGridCellData.Value to ColumnFilterControl.SearchValue (or SearchText)
    BindingOperations.SetBinding(_filterCellData, EditableDataObject.ValueProperty,
        new Binding(nameof(SearchValue)) { Source = this, Mode = BindingMode.TwoWay });
}
```

Choose `SearchValue` for the binding by default (object); for pure-text scenarios, the user binds `Value` and parses themselves, or sets `EditSettings` to align text/value handling. (D7 documents this responsibility.)

#### 3.4 XAML: `PART_EditorHost` honors `ContentTemplate`
Already supported by `ContentPresenter` natively when both `Content` and `ContentTemplate` are set; no XAML change needed beyond ensuring that the existing `IsCheckboxColumn` visibility trigger doesn't fire for template-driven cells. Add a DataTrigger that suppresses the trigger when `GridColumn.AutoFilterRowEditTemplate != null`.

### Verification
1. Define an `<DataTemplate>` with a `Slider` bound to `{Binding Value, Mode=TwoWay}` and assign to a numeric column's `AutoFilterRowEditTemplate`. Verify the slider appears in the filter row and that moving it filters the grid.
2. In the template, bind to `{Binding Column.Header, Mode=OneWay}` — verify column metadata is reachable.
3. In the template, bind to `{Binding View.DataContext.MyVm, Mode=OneWay}` — verify the grid's DataContext flows through.
4. Verify `IDataErrorInfo` interface is callable via reflection (no runtime exceptions even though we return empty strings).
5. Verify default-template columns (no template set) behave exactly as Phase-2-end behavior (no regression on standard editors).

### Out of scope
- `ColumnFilterMode` enum — Phase 4.
- Removing `[Obsolete]` markers — Phase 5.
- Sophisticated `IDataErrorInfo` implementations beyond no-op (the spec doesn't define what errors filter cells emit).
- Per-cell display vs edit mode transitions (the filter row is always-edit; we treat `DisplayTemplate` and `EditTemplate` as fallbacks: `EditTemplate` wins, `DisplayTemplate` is fallback. Future polish could swap on focus.)

### Depends on
- **Phase 1** completed (uses the same property changed wiring patterns).
- **Phase 2** completed (otherwise `RefreshEditor()` rewrite collides with the debounce changes).

---

## Phase 4 — `ColumnFilterMode` review & decision

### Goal
Decide whether to add `ColumnFilterMode` (Value / DisplayText) as a new public-API DP on `GridColumn`, or to document the existing `IDisplayValueProvider` chain as the answer. Either way, ship a single deliverable that closes the spec gap.

### Files modified (if enum is added)
- `WWSearchDataGrid.Modern.WPF\Controls\Enums\ColumnFilterMode.cs` (new)
- `WWSearchDataGrid.Modern.WPF\Controls\GridColumn.cs`
- `WWSearchDataGrid.Modern.WPF\Display\DisplayValueProviderFactory.cs` — honor explicit `ColumnFilterMode` override
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\ColumnFilterControl.cs:717-718` — pass mode to factory

### Audit checklist (do this first)

1. Read `WWSearchDataGrid.Modern.Core\Display\IDisplayValueProvider.cs` (full interface).
2. Read `WWSearchDataGrid.Modern.WPF\Display\DisplayValueProviderFactory.cs` (the priority chain: DisplayMask > DisplayValueConverter > DisplayStringFormat > ComboBoxLookup > null).
3. Read `WWSearchDataGrid.Modern.WPF\Controls\SearchDataGrid\SearchDataGridFiltering.cs:259-280` — the predicate path that consults `controller.DisplayValueProvider.UseRawComparison`.
4. Trace `SearchTemplateController.cs:1161` and surrounding lines — confirm mask-based providers correctly compare raw values while text-formatter providers compare display values.
5. Set a column's `DisplayStringFormat="{}{0:C}"` (currency). Type "$1,000" in the filter cell. Confirm display-text matching works **today** without `ColumnFilterMode`.

### Decision rule

- **If the audit shows `IDisplayValueProvider` is fully wired and `UseRawComparison` correctly routes between raw and display predicate paths** → **do not add `ColumnFilterMode`.** Instead, add a documentation page (`docs/column-filter-mode.md` or extend `docs/api-reference.md`) describing how to opt into display-text filtering via `DisplayStringFormat` / `DisplayValueConverter` / `DisplayMask`. Close the spec gap with documentation.
- **If the audit reveals broken edge cases** (e.g., `UseRawComparison=false` doesn't actually invert the comparison, or no opt-out exists for columns that have a DisplayValueProvider but want raw filtering) → **add `ColumnFilterMode`** with values `Value` (force raw, ignore provider) and `DisplayText` (use provider's formatted output). Default `Value` for parity, but `DisplayText` whenever a provider is configured.

### If adding the enum

```csharp
public enum ColumnFilterMode { Value = 0, DisplayText = 1 }

// GridColumn.cs
public static readonly DependencyProperty ColumnFilterModeProperty =
    DependencyProperty.Register(nameof(ColumnFilterMode), typeof(ColumnFilterMode), typeof(GridColumn),
        new PropertyMetadata(ColumnFilterMode.Value, OnColumnFilterModeChanged));
```

`DisplayValueProviderFactory.Create()` reads the mode: if `Value`, returns `null` (force raw comparison); if `DisplayText`, runs the existing priority chain. Callback writes through to the matching `SearchTemplateController.DisplayValueProvider`.

### Verification
1. Confirm the audit-result claim against the sample app (try a currency column, a converter-bound column, a mask column).
2. If the enum was added, verify `ColumnFilterMode="Value"` forces raw comparison even when `DisplayStringFormat` is set.
3. If documentation only, verify the new docs page lives alongside `docs/api-reference.md` and is discoverable.

### Out of scope
- Refactoring `IDisplayValueProvider` itself.
- Server-mode / RealTimeSource changes (the spec mentions these but they're WPF-DataGrid-server-binding concerns out of scope for this library).
- Removing `[Obsolete]` markers — Phase 5.

### Depends on
- **Phase 1, 2, 3** — Phase 4 should run after all template work has stabilized so that the `Value` binding on `EditGridCellData` reflects the correct (raw vs display) string.

---

## Phase 5 — Remove deprecated APIs

### Goal
Delete the `[Obsolete]` symbols introduced in earlier phases. The only consumer of the old API surface is the in-solution sample app, which gets migrated to the new names in each prior phase. After Phase 4 ships, the deprecated members serve no further purpose; this phase removes them in one clean diff so the library's public surface matches the spec exactly.

### Files modified
- `WWSearchDataGrid.Modern.WPF\Controls\GridColumn.cs` — remove `DefaultSearchMode` CLR wrapper, `DefaultSearchModeProperty` alias field, `SetAutoDefaultSearchMode`/`IsDefaultSearchModeExplicit` if their names changed; remove the `DefaultSearchMode` enum if it duplicates `DefaultSearchType`
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\ColumnFilterControl.cs` — remove `IsLiveFilteringOverride` CLR wrapper and `IsLiveFilteringOverrideProperty` DP; `EffectiveIsLiveFilteringEnabled` now reads `GridColumn.ImmediateUpdateAutoFilter` directly (or its writethrough target on the host)
- `WWSearchDataGrid.Modern.WPF\Controls\FilterRow\IColumnFilterHost.cs` — drop any obsoleted interface members; rename to match the new API surface
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\Filtering\SearchModesSampleView.xaml` — verify zero residual references to old names (should already be migrated in Phase 1/2)
- `WWSearchDataGrid.Modern.SampleApp\Views\Launcher\SampleCatalog.cs` — same
- `docs\getting-started.md` — replace `AllowFiltering` examples with `AllowAutoFilter` where the intent matches the spec; remove residual `DefaultSearchMode` references
- `docs\GRIDCOLUMN-IMPLEMENTATION-PLAN.md` — update the table at line 106 to reflect renamed properties
- `docs\api-reference.md` — update table at line 115 and any other DP references

### Pre-phase verification (do this first; it gates Phase 5)
1. `dotnet build` the entire solution at zero `[Obsolete]` warnings. Every consumer must already be on the new API.
2. `Grep` the entire repo (including `WWSearchDataGrid.Modern.SampleApp` and `docs/`) for the deprecated symbols:
   - `\bDefaultSearchMode\b` (both enum and property name)
   - `\bIsLiveFilteringOverride\b`
   - Any other `[Obsolete]` markings introduced in Phases 1-4 (search the `Controls/` tree for `\[Obsolete\b`)
3. Confirm each hit is either in:
   - The declaration site about to be removed, or
   - A comment / XML doc that references the old name historically (rewrite or remove)
4. If any hit is in code that still uses the old name, **stop and migrate that callsite first**. Phase 5 is a no-runtime-behavior-change diff.

### Detailed changes

#### 5.1 Remove `DefaultSearchMode`
- Delete the `[Obsolete]` CLR wrapper `public DefaultSearchMode DefaultSearchMode { ... }`.
- Delete the `public static readonly DependencyProperty DefaultSearchModeProperty = DefaultSearchTypeProperty;` alias.
- If the `DefaultSearchMode` enum was kept as an alias type with implicit conversions to `DefaultSearchType` (per D4), delete the enum entirely.
- Rename `_isAutoDefaultSearchMode` → `_isAutoDefaultSearchType`; `SetAutoDefaultSearchMode` → `SetAutoDefaultSearchType`; `IsDefaultSearchModeExplicit` → `IsDefaultSearchTypeExplicit`. These are `internal`, so the rename is contained.
- Update `MapDefaultSearchModeToSearchType` in `ColumnFilterControlTextFilter.cs:466-473` → `MapDefaultSearchTypeToSearchType`, and adjust its parameter type.

#### 5.2 Remove `IsLiveFilteringOverride`
- Delete the `[Obsolete]` CLR wrapper and `IsLiveFilteringOverrideProperty` DP from `ColumnFilterControl.cs:130-133`.
- Rewrite `EffectiveIsLiveFilteringEnabled` (lines 297-306) to read `GridColumn?.ImmediateUpdateAutoFilter` directly, combined with the row-count threshold. Use the explicit-set flag (`_isImmediateUpdateAutoFilterExplicit` introduced in Phase 2 via `ReadLocalValue`) to preserve the previous override semantics: column setting wins when explicit; otherwise the row-count threshold drives the decision.

```csharp
public bool EffectiveIsLiveFilteringEnabled
{
    get
    {
        bool autoOk = (SourceDataGrid?.OriginalItemsCount ?? 0) < SearchDataGrid.LiveFilteringRowCountThreshold;
        if (GridColumn != null
            && GridColumn.ReadLocalValue(GridColumn.ImmediateUpdateAutoFilterProperty) != DependencyProperty.UnsetValue)
        {
            return GridColumn.ImmediateUpdateAutoFilter;
        }
        return autoOk;
    }
}
```

#### 5.3 Verify `AllowFiltering` is not deprecated
Per Phase 1's D3 decision, `AllowFiltering` was kept distinct from `AllowAutoFilter` — there's nothing to remove. Verify no `[Obsolete]` slipped onto it during implementation.

#### 5.4 Documentation cleanup
- `docs/getting-started.md:146` — example `<sdg:GridColumn AllowFiltering="False" AllowSorting="False" />` is still valid; verify the prose around it. If the example was meant to convey "disable filter UI for this column", change to `AllowAutoFilter="False"` (greyed cell) to match the spec idiom.
- `docs/GRIDCOLUMN-IMPLEMENTATION-PLAN.md:106` — the line `| AllowFiltering | bool | true | New — completely disable filtering |` is historical (it's a plan doc); leave unless backfilling the renames. Recommend: add a note pointing to this plan.
- `docs/api-reference.md:115` — the line `| AllowFiltering | bool | true | When false, hides the search box entirely. |` — keep `AllowFiltering` row, add a new `AllowAutoFilter` row, add new rows for every DP added in Phases 1-4. **This is the canonical API reference** — make sure it reflects the final shipped surface.

### Verification
1. `dotnet build` — zero `[Obsolete]` warnings (because zero `[Obsolete]` symbols remain).
2. `Grep` the entire repo for the deprecated symbol names — zero hits.
3. Run `WWSearchDataGrid.Modern.SampleApp` end-to-end smoke test (same scenarios as Phases 1-4 verification). Behavior must match exactly — Phase 5 is a no-runtime-change diff.
4. `Grep` `\[Obsolete\b` across the WPF project — zero hits (every deprecation introduced in this plan has been cleaned up).

### Out of scope
- Pre-existing `[Obsolete]` markings unrelated to this plan (if any exist in the repo before Phase 1 starts). Leave them alone unless they're collateral damage of a Phase 1-4 rename.
- Refactoring beyond the symbol removals — Phase 5 is mechanical cleanup, not opportunistic improvement.

### Depends on
- **Phase 1, 2, 3, 4** — all of them. Phase 5 runs last because every prior phase contributes at least one `[Obsolete]` symbol whose consumers must be migrated before removal.

---

# Sample-app coverage (Phases 6-8)

Phases 1-5 land the library surface. The existing `WWSearchDataGrid.Modern.SampleApp` `Filtering` category (`Search Modes`, `Custom Predicate & Events`, `Rule Filter Popup`) demonstrates the *pre-existing* search-mode and rule-popup concepts, but none of them expose the new auto-filter-row DPs at runtime. Without runtime tweakability, the new surface is invisible to anyone exploring the sample app.

Phases 6-8 add a dedicated `Auto Filter Row` sample category with three focused sample views, each pairing a `<sdg:SearchDataGrid>` with a right-side `<sdg:SimpleStackPanel DockPanel.Dock="Right">` settings sidebar that mutates the new DPs as the user interacts. The pattern matches `EditorTypesSampleView` (sidebar of `GroupBox`-wrapped `ComboBox` / `CheckBox` / `Slider` controls bound to a view-model exposing the runtime state).

These phases are sample-only — they touch no library code. Each phase is independently shippable; they share zero state and zero files except the catalog entry.

## Shared scaffolding (applies to Phases 6-8)

Each new sample is a triplet under `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\`:
- `<Name>SampleView.xaml` — UserControl wrapped in `<sampleControls:SampleHostControl>`, DockPanel layout (top blurb / right sidebar / center grid).
- `<Name>SampleView.xaml.cs` — minimal code-behind. Hosts a `Loaded` handler that wires the VM to the grid's `GridColumns` collection (see "GridColumn lookup pattern" below).
- `<Name>SampleViewModel.cs` — `INotifyPropertyChanged` VM exposing runtime state + choice collections for the sidebar bindings.

Catalog wiring:
- New category `Auto Filter Row` added to `SampleCatalog.Categories` in `WWSearchDataGrid.Modern.SampleApp\Views\Launcher\SampleCatalog.cs`. Place after `Filtering` so the auto-filter-row work groups visually alongside the related basic filtering samples without polluting their concise definitions.
- One `SampleSources` entry per sample in `WWSearchDataGrid.Modern.SampleApp\Controls\SampleSources.cs` (file paths for the code-viewer panel — at minimum, the View / VM / data model used).
- No `using` aliases to invent; reuse the existing `WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow` namespace registered in the new files.

**GridColumn lookup pattern.** The new DPs live on `GridColumn` descriptors declared inline inside `<sdg:SearchDataGrid.GridColumns>` — they aren't reachable via `ElementName` from the sidebar without either naming each `GridColumn` with `x:Name` or finding them at runtime. The recommended approach (cleaner VM, no XAML clutter):

1. Give each tweakable `GridColumn` an `x:Name` matching its `FieldName` (e.g. `x:Name="OrderNumberColumn"`).
2. In `<View>.xaml.cs` `OnLoaded`, hand each named `GridColumn` reference to the VM (e.g. `_vm.RegisterColumn("OrderNumber", OrderNumberColumn)`).
3. The VM stores them in a `Dictionary<string, GridColumn>` and writes per-column DP changes through directly (`_columns[name].AllowAutoFilter = newValue`).
4. Grid-level DPs (e.g. `FilterRowDelay`, `AutoFilterRowClearButtonMode`) bind two-way directly via `ElementName` to the named `<sdg:SearchDataGrid x:Name="Grid">` — no VM round-trip needed.

**Why not pure binding?** A bidirectional `Mode=TwoWay` binding from a sidebar control directly into a `GridColumn` DP is possible (use `ElementName` after naming the column) but multiplies XAML noise when there are 5+ columns × 4+ DPs. The code-behind hand-off above keeps the VM authoritative and the XAML clean.

---

## Phase 6 — Auto-filter-row options playground

### Goal
A single grid plus a settings sidebar exposing **every** grid-level and column-level auto-filter-row DP added in Phases 1-2 at runtime. The user picks a target column from a dropdown, then sees / tweaks that column's per-column settings; grid-level settings live in a separate sidebar group always bound to the grid. Covers ~80% of the new public-API surface in one view.

### Files added
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\OptionsPlaygroundSampleView.xaml`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\OptionsPlaygroundSampleView.xaml.cs`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\OptionsPlaygroundSampleViewModel.cs`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\ColumnPlaygroundConfig.cs` — POCO with `INotifyPropertyChanged`, holds runtime state for one column.

### Files modified
- `WWSearchDataGrid.Modern.SampleApp\Views\Launcher\SampleCatalog.cs` — register the new `Auto Filter Row` category with this sample as its first entry.
- `WWSearchDataGrid.Modern.SampleApp\Controls\SampleSources.cs` — add `OptionsPlayground` source list.

### Detailed changes

#### 6.1 Data
Reuse the existing `OrderItem` sample data generator already referenced by `RuleFilterPopupSampleView` (or whichever generator backs the multi-column order-items table — verify via the existing VM). The dataset must include columns of types: `string`, `int`, `decimal`, `DateTime`, `bool` so the playground can exercise type-driven defaults. Target row count: 500-2000 (large enough for filtering to feel real, small enough to side-step the live-filter threshold so `ImmediateUpdateAutoFilter=true` actually does fire on keystroke).

#### 6.2 Grid columns
Six columns, each `x:Name`d so the code-behind can hand references to the VM:

| Column | `FieldName` | `x:Name` | Type | Purpose |
|---|---|---|---|---|
| Order # | `OrderNumber` | `OrderNumberColumn` | int | Default `StartsWith`, shows `DefaultSearchType` flip |
| Customer | `CustomerName` | `CustomerNameColumn` | string | String column; exercise `DefaultSearchType` |
| Status | `OrderStatusName` | `StatusColumn` | string | Defaults to Equals-feeling content; toggle search type |
| Order Date | `OrderDate` | `OrderDateColumn` | DateTime | DateTime column; exercise `DefaultSearchType` |
| Total | `OrderItemsTotalPrice` | `TotalColumn` | decimal | Numeric; exercises non-text search |
| Cancelled | `OrderCancelled` | `CancelledColumn` | bool | Exercises checkbox cell vs `AllowAutoFilter=false` |

#### 6.3 Grid-level sidebar group
A `GroupBox` titled `Grid settings` containing controls bound via `ElementName=Grid` to the grid's DPs:

| Control | Bound DP | Notes |
|---|---|---|
| `<CheckBox>` | `ShowCriteriaInAutoFilterRow` | Toggle the inline search-type selector across all cells |
| `<Slider Minimum="0" Maximum="2000" TickFrequency="250" IsSnapToTickEnabled="True">` | `FilterRowDelay` | Live label showing the current ms value beside it |
| `<ComboBox>` bound to `AutoFilterRowClearButtonModeChoices` (VM-provided) | `AutoFilterRowClearButtonMode` | Items: `Never`, `Always`, `Display`, `Edit` |
| `<Button Content="Clear all filters">` | `Command="{Binding ClearAllFiltersCommand, ElementName=Grid}"` | Quality-of-life reset between experiments |

#### 6.4 Selected-column sidebar group
A `GroupBox` titled `Selected column settings` with a `<ComboBox>` at the top listing the six columns (bound to VM's `SelectedColumnConfig`). Below that, the column-specific controls bind to properties on the currently-selected `ColumnPlaygroundConfig`:

| Control | `ColumnPlaygroundConfig` property | Effect |
|---|---|---|
| `<CheckBox>` "Allow filtering (cell collapsed when off)" | `AllowFiltering` (bool) | Writes through to `GridColumn.AllowFiltering` |
| `<CheckBox>` "Allow auto-filter (cell greyed when off)" | `AllowAutoFilter` (bool) | Writes through to `GridColumn.AllowAutoFilter` |
| `<ComboBox>` three-state for nullable bool | `ShowCriteriaOverride` (`bool?`) | `Inherit (null)` / `Show (true)` / `Hide (false)` |
| `<CheckBox>` "Immediate update (live filter)" | `ImmediateUpdateAutoFilter` | Writes through to `GridColumn.ImmediateUpdateAutoFilter` |
| `<ComboBox>` | `DefaultSearchType` enum | Items: `Contains`, `StartsWith`, `EndsWith`, `Equals` |

Selection switching: when `SelectedColumnConfig` changes (user picks a different column), the controls in the group re-bind to the newly-selected config via the standard DataContext-style binding inheritance (no per-control logic). Make sure the inheritance is rooted on the `GroupBox`, e.g. `<GroupBox DataContext="{Binding SelectedColumnConfig}">` — controls inside use plain bindings like `{Binding AllowAutoFilter, Mode=TwoWay}`.

#### 6.5 ViewModel responsibilities
- `ObservableCollection<ColumnPlaygroundConfig> Columns` — one entry per registered column. Each entry holds the runtime-tweakable state plus a `GridColumn _backingColumn` (set when the view's code-behind calls `RegisterColumn`).
- Each `ColumnPlaygroundConfig` property setter writes the new value through to `_backingColumn` directly. No reflection — explicit property writes.
- `SelectedColumnConfig` — `ColumnPlaygroundConfig` reference for the dropdown; defaults to the first entry.
- Choice collections: `AutoFilterRowClearButtonModeChoices` (`AutoFilterRowClearButtonMode[]`), `DefaultSearchTypeChoices` (`DefaultSearchType[]`), `ShowCriteriaOverrideChoices` (a small POCO list with `Label` / `Value` to represent `null` / `true` / `false`).

#### 6.6 Top blurb
One paragraph explaining what the sample demonstrates plus a short "Try this" list:
- Toggle `ShowCriteriaInAutoFilterRow` and watch every cell sprout a search-type selector.
- Set the `OrderDate` column's `ImmediateUpdateAutoFilter` to false, then type — filter waits for Enter.
- Set the `Cancelled` column's `AllowAutoFilter` to false — cell greys but keeps its space.
- Set the same column's `AllowFiltering` to false — cell collapses entirely.

### Verification
1. Build the solution. Open the sample. Top blurb renders with the four "Try this" bullets.
2. Pick `CustomerName` in the column dropdown; change `DefaultSearchType` from `StartsWith` to `Equals`. Type a partial name into the filter cell — no matches. Type an exact name — matches. Flip back to `StartsWith` — partial matches return.
3. Move `FilterRowDelay` slider to 1000ms. Type rapidly into a string column. Filter applies once, ~1s after the last keystroke.
4. Set the grid's `AutoFilterRowClearButtonMode` to `Always`. Every cell shows a clear button regardless of input. Set to `Never` — buttons all disappear.
5. Pick `Cancelled`. Set `AllowAutoFilter=false` — cell greys, takes space. Set `AllowFiltering=false` — cell collapses. Reset both — cell restores.

### Out of scope
- `AutoFilterRowCellStyle` runtime swap — styling is more authoring-time than runtime-tweaking; defer.
- Custom templates (`AutoFilterRowDisplayTemplate` / `AutoFilterRowEditTemplate`) — Phase 7.
- Dataset-size toggle for threshold experimentation — Phase 8.

### Depends on
- Phases 1-2 shipped (grid-level + column-level DPs in place). Phase 5 not required, but recommended — if Phase 5 hasn't happened, the playground will trip CS0618 warnings on `DefaultSearchType` because the obsolete name still exists in the type system. Building atop the Phase 5 cleanup is cleaner.

---

## Phase 7 — Custom auto-filter-row templates

### Goal
Demonstrate `GridColumn.AutoFilterRowDisplayTemplate` / `AutoFilterRowEditTemplate` plus the `EditGridCellData` data-context hierarchy via three template-driven filter cells, each replacing the default editor with a different WPF control. The sample is read-only — the value here is showing the *recipe*, not tweaking it at runtime.

### Files added
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\CustomTemplatesSampleView.xaml`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\CustomTemplatesSampleView.xaml.cs`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\CustomTemplatesSampleViewModel.cs`

### Files modified
- `WWSearchDataGrid.Modern.SampleApp\Views\Launcher\SampleCatalog.cs` — second entry in the `Auto Filter Row` category.
- `WWSearchDataGrid.Modern.SampleApp\Controls\SampleSources.cs` — add `CustomTemplates` source list.

### Detailed changes

#### 7.1 Three template recipes
Define three `<DataTemplate>` resources in the view's `<UserControl.Resources>` and attach each to a different column via `AutoFilterRowEditTemplate`:

**Template 1: Numeric slider for `OrderItemsTotalPrice`**
```xml
<DataTemplate x:Key="TotalSliderFilterTemplate">
    <DockPanel>
        <TextBlock DockPanel.Dock="Right" Width="56" Margin="4,0,0,0"
                   VerticalAlignment="Center"
                   Text="{Binding Value, StringFormat='C0'}" />
        <Slider Minimum="0" Maximum="10000"
                Value="{Binding Value, Mode=TwoWay}"
                TickFrequency="500"
                IsSnapToTickEnabled="True"
                VerticalAlignment="Center" />
    </DockPanel>
</DataTemplate>
```
DataContext is the `EditGridCellData` instance; the `Value` two-way binding routes to `ColumnFilterControl.SearchValue` via the Phase 3 wiring. With the slider, the filter is "Total >= slider value" — but with the default `SearchType` (per column's `DefaultSearchType`) the comparison is equality, not range. To get >= semantics, **also** set `<sdg:GridColumn DefaultSearchType="Equals">` and pre-configure the column's `SelectedSearchType` to `GreaterThanOrEqualTo` if achievable from XAML; if not, accept that this template demonstrates the *editor* swap and the filter semantics are equality. Verify which path works during implementation — adjust the demo wording to match reality.

**Template 2: DatePicker for `OrderDate`**
```xml
<DataTemplate x:Key="DatePickerFilterTemplate">
    <DatePicker SelectedDate="{Binding Value, Mode=TwoWay}"
                VerticalAlignment="Center" />
</DataTemplate>
```
Straightforward — picking a date sets `EditGridCellData.Value` to a `DateTime?`; the filter pipeline uses `Equals` against the formatted display string (`DisplayStringFormat="MM/dd/yyyy"` strips time-of-day from the comparison).

**Template 3: Radio button group for `OrderStatusName`**
```xml
<DataTemplate x:Key="StatusRadioFilterTemplate">
    <ItemsControl ItemsSource="{Binding Column.Tag}"
                  HorizontalAlignment="Center">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Horizontal" />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <RadioButton Content="{Binding}"
                             GroupName="StatusFilter"
                             Margin="0,0,8,0"
                             Tag="{Binding}"
                             Click="StatusRadio_Click" />
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</DataTemplate>
```
Radio groups don't bind their `IsChecked` back to a shared `Value` cleanly — use a click handler in code-behind that walks up to the `EditGridCellData` and sets `Value`. The `Column.Tag` binding reaches the column's `Tag` property (the column descriptor's `Tag` holds a string array of status names — set in XAML on the GridColumn).

#### 7.2 Grid layout
A `<sdg:SearchDataGrid x:Name="Grid" EnableRuleFiltering="True" AutoGenerateColumns="False">` with all six `OrderItem` columns. Three columns get the templates above; the rest use defaults to show the contrast. Set `Height="500"` on the grid so the filter row visibly stretches to accommodate the slider / datepicker / radios without scrolling.

#### 7.3 Right sidebar
A read-only sidebar showing the XAML for each template inside a syntax-highlighted view. Reuse `AvalonEditHelper` (already in `Controls\AvalonEditHelper.cs`) — the existing `SampleHostControl` may already provide code-viewing infrastructure, in which case wire the template definitions into `SampleSources` instead and let the host's "Sources" tab render them. **Verify which approach works** before authoring the sidebar — if `SampleSources` can list multiple files including the view's XAML and a snippet file, no custom sidebar is needed.

#### 7.4 Top blurb
- Explain that the auto-filter row replaces the default editor when a template is set.
- Mention that the `EditGridCellData` data context exposes `Value` (two-way), `Column` (read-only), and `View` (the grid).
- Caveat: filter *semantics* (which `SearchType` runs) are unchanged by the template — the template controls only the *editor*, not the predicate.

### Verification
1. Build the solution. Open the sample. Three template-driven cells render their custom controls in the filter row; the other cells render the default text editor.
2. Move the `Total` slider — the rows filter to those with totals matching the slider value (or `>=`, depending on which semantics ship — verify and document the actual behavior in the blurb).
3. Pick a date in the `OrderDate` filter cell — rows narrow to that date.
4. Click a radio button in the `Status` filter — rows narrow to that status. Click another — rows update.
5. Clear filters with the grid's `ClearAllFiltersCommand` — every template control resets to its empty state.
6. Resize the grid horizontally — template controls reflow without overflow.

### Out of scope
- Composite range filters (min/max in a single template) — `EditGridCellData.Value` is a single object, so composing a multi-input range cleanly is a separate exercise that would require either a custom value type or a wrapper VM. Note in the top blurb.
- `AutoFilterRowDisplayTemplate` vs `AutoFilterRowEditTemplate` distinction — Phase 3 treats edit as the winner and display as fallback for filter-row context. Demonstrate only `AutoFilterRowEditTemplate`.
- Runtime template swapping (toggle to flip between templates and defaults) — possible but adds VM complexity for limited didactic value.

### Depends on
- Phase 3 shipped (`EditGridCellData` hierarchy + `AutoFilterRowDisplayTemplate` / `AutoFilterRowEditTemplate` DPs on `GridColumn`).
- Phase 6 not required but recommended (provides the `AutoFilterRow` category container the sample registers into).

---

## Phase 8 — Debounce and live-filter behavior

### Goal
Demonstrate how `SearchDataGrid.FilterRowDelay`, `GridColumn.ImmediateUpdateAutoFilter`, and `SearchDataGrid.LiveFilteringRowCountThreshold` interact across dataset sizes. The signature demo: feel the difference between debounce off, debounce 500ms, and immediate-update off — on the same dataset — then swap to a 100k-row dataset and watch the auto-threshold engage.

### Files added
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\DebounceBehaviorSampleView.xaml`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\DebounceBehaviorSampleView.xaml.cs`
- `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AutoFilterRow\DebounceBehaviorSampleViewModel.cs`

### Files modified
- `WWSearchDataGrid.Modern.SampleApp\Views\Launcher\SampleCatalog.cs` — third entry in the `Auto Filter Row` category.
- `WWSearchDataGrid.Modern.SampleApp\Controls\SampleSources.cs` — add `DebounceBehavior` source list.

### Detailed changes

#### 8.1 Data
Three datasets generated on demand:
- 1,000 rows (below auto-threshold; immediate-update active by default)
- 100,000 rows (auto-threshold triggers; immediate-update auto-off unless overridden)
- 1,000,000 rows (heavy; immediate-update off; explicit override demonstrates the cost)

Reuse `LargeDatasetsSampleView`'s generator (in `WWSearchDataGrid.Modern.SampleApp\Views\Samples\AnimationPerformance\`) — confirm the generator is shared or copy-adapt it. The dataset switch must not freeze the UI; do the generation on a background thread and swap the `ItemsSource` on completion. Show a `SampleLoadingOverlay` during regeneration.

#### 8.2 Right sidebar
A single `GroupBox` titled `Filter behavior` containing:

| Control | Bound state | Effect |
|---|---|---|
| `<RadioButton>` × 3 | `SelectedDatasetSize` (enum: `Small` / `Medium` / `Large`) | Swaps `ItemsSource` and prompts regeneration |
| `<Slider Minimum="0" Maximum="2000" TickFrequency="100">` | `FilterRowDelay` (via `ElementName=Grid`) | Live label `{N} ms` |
| `<CheckBox>` "Immediate update auto-filter" | `ImmediateUpdateAutoFilter` applied to every column | When unchecked, walks every `GridColumn` and sets it to `false`; checked reverts to `true` |
| `<TextBlock>` | `EffectiveLiveFilteringLabel` | VM-computed: "Live filtering ACTIVE" vs "Live filtering INACTIVE (auto-disabled at >100k rows)" |

The "Immediate update" checkbox applies globally rather than per-column because the demo's point is the *behavior interaction*, not per-column granularity. Per-column override is already covered in Phase 6.

#### 8.3 Telemetry strip below the sidebar
A small `<Border>` with two `TextBlock`s:
- `Last keystroke: {time}` — updated by an attached event handler on the filter-row's editor `TextChanged`.
- `Last filter apply: {time}` — updated by hooking the grid's `Filtered` event (or similar — verify event name during implementation; if no such event exists, listen to `SearchTemplateController.PropertyChanged` for `FilterExpression` changes).
- A third line: `Delta: {ms}` — the diff between the two timestamps.

This is the moneyshot: as the user types into a filter cell, the strip shows the live debounce in action. With `FilterRowDelay=0` and `ImmediateUpdateAutoFilter=true`, delta hovers near 0; with `FilterRowDelay=500`, delta climbs to 500ms; with immediate-update off, delta stays empty until Enter.

If the necessary events don't exist, drop the telemetry strip — the radio / slider / checkbox interaction is still demonstrative on its own. **Verify event availability before authoring the strip.**

#### 8.4 Top blurb
- Explain the three interacting settings.
- Note the auto-threshold (`LiveFilteringRowCountThreshold`, currently 100k) and what happens above it.
- Mention that the slider has no effect when `ImmediateUpdateAutoFilter` is off (keystrokes don't trigger a debounce; user must commit).

### Verification
1. Build the solution. Open the sample. Default state: 1k rows, immediate-update on, delay 0.
2. Type into a filter cell. Telemetry strip shows delta ≈ 0ms (or sub-frame).
3. Slide `FilterRowDelay` to 500. Type. Delta jumps to ~500ms.
4. Uncheck `Immediate update`. Type. No filter applies; telemetry's `Last filter apply` doesn't update. Press Enter — filter applies; telemetry catches up. Slider has no visible effect.
5. Re-check `Immediate update`. Switch dataset to 100k. Regeneration overlay shows briefly. After settle: typing applies filter, but the `EffectiveLiveFilteringLabel` reads "INACTIVE (auto-disabled at >100k rows)" if no override is in play. With override on, label flips and filter is live again (slower; that's the point).
6. Switch dataset to 1M. With immediate-update off, scrolling and typing feel responsive. With immediate-update on, typing visibly stalls between keystrokes — demonstrating the cost the threshold is trying to avoid.

### Out of scope
- Per-column live-filter override granularity — covered by Phase 6.
- Changing the `LiveFilteringRowCountThreshold` constant at runtime — the threshold is a `const` on `SearchDataGrid`; mention this in the blurb but don't expose it as a knob.
- Background loading optimization — generation can stutter at 1M; if it's egregious, crib `LargeDatasetsSampleView`'s approach (likely a precomputed cached collection).

### Depends on
- Phase 2 shipped (`FilterRowDelay` grid DP, `ImmediateUpdateAutoFilter` column DP, `DispatcherTimer` debounce path).
- Phases 6 / 7 not required.

---

| File | Lines | Role |
|---|---|---|
| `Controls\SearchDataGrid\SearchDataGrid.cs` | 150-350 | Grid-level DP additions (Phases 1, 2) |
| `Controls\GridColumn.cs` | 306-367, 460-540, 559-690, 1150-1190 | Column-level DPs, type-based defaults, property change wire-through (all phases) |
| `Controls\FilterRow\ColumnFilterControl.cs` | 50-330, 380-400, 680-750 | Cell host: DPs, RefreshEditor, init (all phases) |
| `Controls\FilterRow\ColumnFilterControlTextFilter.cs` | 24-50, 139-147, 243-358, 441 | SearchText callback, dead timer, CommitSearchText, unload cleanup (Phase 2) |
| `Themes\Controls\FilterRow\AutoFilterRow.xaml` | 49-147, 166-351, 211-232, 241-255, 284-343 | SearchTypeSelector style, ColumnFilterControl template, PART_SearchTypeSelector, PART_EditorHost, PART_ClearFilterButton (Phases 1, 2, 3) |
| `Controls\EditSettings\BaseEditSettings.cs` | 224-289 | `CreateFilterEditor`/`BuildDefaultTextEditor` (Phase 3 — template fallback path) |
| `Display\DisplayValueProviderFactory.cs` | 10-50 | Provider priority chain (Phase 4) |
| `Core\Display\IDisplayValueProvider.cs` | full | Provider interface (Phase 4) |
| `Core\Data\Models\Search\SearchTemplateController.cs` | 1100-1200 | `DisplayValueProvider` consumption (Phase 2 date rounding, Phase 4 mode review) |
| `Controls\SearchDataGrid\SearchDataGridFiltering.cs` | 259-280 | Predicate path consulting `UseRawComparison` (Phase 4) |

## Reusable functions / patterns (no need to reinvent)

- **`IsExplicitlySet` + `_isAuto…` flag pattern** (`GridColumn.cs:572-593`) — already in place for `DefaultSearchMode`. Reuse for any new auto-configured defaults.
- **`OnFilterPropertyChanged` callback** (`GridColumn.cs:1150-1169`) — already wires column DP changes through to the matching `ColumnFilterControl`. Extend it for new column DPs in Phases 1-2 rather than adding parallel callbacks.
- **`SetAutoDefaultSearchMode` / `IsDefaultSearchModeExplicit`** — extend with `SetAutoXxx`/`IsXxxExplicit` pairs for any new auto-configured DPs.
- **`DisplayValueProviderFactory.Create`** — keep its priority chain; Phase 4 may add a mode-aware short-circuit at the top, never a parallel implementation.

## Verification across all phases

After each phase:
1. `dotnet build` the entire solution at `WWSearchDataGrid.Modern.sln` — no compilation errors.
2. Run `WWSearchDataGrid.Modern.SampleApp` (Windows Desktop). Walk the sample views, especially `Filtering / Search Modes` and any view that uses `AllowFiltering=False`, to spot regressions.
3. Run existing tests if any are present in `WWSearchDataGrid.Modern.Tests` (check via `Glob` for `*Tests.csproj`).
4. Check that any `docs/*.md` referenced by the changes (e.g., `docs/GRIDCOLUMN-IMPLEMENTATION-PLAN.md:106`, `docs/api-reference.md:115`) reflect the new behavior — these are existing documentation that needs to track DP renames.
5. Verify `[Obsolete]` build warnings appear when consumers use deprecated names (intentional — drives migration). Phase 5 closes this loop.

## Out of scope (entire plan)

- Header-position AutoFilterRow (`AutoFilterRowPosition.Header`) — not in the spec; leave the existing implementation untouched.
- `ColumnSearchBox` (a separate in-header text box mentioned in `SearchDataGrid.cs:336`) — not part of the AutoFilterRow contract; ignore unless a Phase 2 change touches it incidentally.
- Removing the dead `System.Timers.Timer` `using` directive can wait — clean it up in Phase 2 alongside the DispatcherTimer rewrite to keep diffs together.
- Rewriting the rule-filter popup (`EnableRuleFiltering`) — unrelated surface.
