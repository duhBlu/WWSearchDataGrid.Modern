# Filter Editor — Implementation Plan

## Context

The `FilterEditor` modal dialog in `WWSearchDataGrid.Modern.WPF` currently exists only as a scaffold: it shows the read-only `FilterTokens` from `FilterPanel` as a placeholder, with the subtitle "Multi-column filter composition coming soon." It needs to become an authoring surface where the user composes multi-column, multi-group filter expressions visually — using chip-style tokens that swap between Display and Edit modes — matching the two reference screenshots at `C:\Users\jacobjthieret\Desktop\Temp\ClaudeImages\sdg\`.

Distinct chip token types: **Group Operator** (And/Or/NotAnd/NotOr), **Column Name** (orange), **Search Type** (light blue), **Value** (light green). Each chip's Display Template hides editor affordances when not focused, mirroring the existing `IsKeyboardFocusWithin` pattern used in `AutoFilterRow.xaml`.

User-confirmed decisions:

1. Extend Core to support all 4 logical operators (And/Or/NotAnd/NotOr) — not just And/Or.
2. True nested groups — a group can contain child groups recursively.
3. Aggregate across all column controllers. On Apply/OK, push back to each column's per-column `SearchTemplateController.SearchGroups`.
4. Value tokens reuse the existing `FilterInputTemplate` editors (SingleSearchTextBox / DualSearchTextBox / DualDateTimePicker / NumericUpDown / SearchTextBoxList / DateIntervalCheckList / etc.).
5. "Add Custom Expression" — show as disabled in the add popup; defer to a future pass.
6. Each group renders its own operator chip at its start (top-level and nested).
7. Removal: hover-shows-× pattern (reuse `TokenConfirmationBehavior`).
8. Cross-column OR groups: warn but allow (lossy round-trip flagged inline).
9. Operator chip interaction: dropdown popup listing 4 options (matches the chevron in the screenshot).

Project: `WWSearchDataGrid.Modern` (this solution). SampleApp is the only consumer so deprecation cycles can be short.

---

## Architecture

**Editor-time tree (Option 2 — picked).** The editor builds an editor-time view model tree on open from each column's `SearchTemplateController.SearchGroups`, mutates that tree in-memory, and writes a per-column slice back to each controller on Apply/OK. Per-column `SearchTemplateController` remains authoritative for persistence and evaluation; `SearchDataGridFiltering.EvaluateUnifiedFilter` is unchanged.

Cancel discards the tree without touching per-column state. Apply is non-closing; OK = Apply + close.

**Cross-column OR limitation (accepted):** because each column's controller holds only its own conditions, an editor group with `Or`/`NotOr` operator that spans multiple columns cannot round-trip; per-column results are AND-joined at the grid level. The editor renders an inline warning banner on the offending group; user can still Apply (predicate evaluates to the lossy AND form). True fix is a Phase-2 grid-level tree.

**Core extensions (small, surgical):**

- New `LogicalOperator` enum: `And, Or, NotAnd, NotOr`.
- `SearchTemplateGroup.OperatorName` setter accepts the four token strings; gains an `IsNegated` derived property and a new `ChildGroups: ObservableCollection<SearchTemplateGroup>` sibling collection to support nested groups in persistence.
- `FilterExpressionBuilder.BuildFilterExpression` is refactored to recurse `ChildGroups` and wrap a group's body in `Expression.Not(...)` when `IsNegated`.

These Core changes are invisible to the existing per-column popup `ColumnFilterEditor` (it only iterates `SearchTemplates`, never `ChildGroups`).

---

## File changes

### Created (Core)

- `WWSearchDataGrid.Modern.Core/Data/Enums/LogicalOperator.cs` — `enum LogicalOperator { And, Or, NotAnd, NotOr }`.
- `WWSearchDataGrid.Modern.Core/Services/LogicalOperatorExtensions.cs` — `Parse(string)`, `ToTokenString()`, `InnerComposer()` returning `Expression.AndAlso` for And/NotAnd, `Expression.OrElse` for Or/NotOr, `IsNegated()`, `DisplayText()` returning `"And"`/`"Or"`/`"Not And"`/`"Not Or"`.

### Created (WPF — editor-time view models)

Folder: `WWSearchDataGrid.Modern.WPF/Controls/FilterEditor/`

- `FilterEditorNode.cs` — abstract `INotifyPropertyChanged` base with `Parent: FilterGroupNode`, `Id: Guid`, `Depth: int`.
- `FilterGroupNode.cs` — derives from `FilterEditorNode`. Has `Operator: LogicalOperator`, `Children: ObservableCollection<FilterEditorNode>` (heterogeneous), `AddConditionCommand`, `AddGroupCommand`, `RemoveCommand`. Exposes `HasMixedColumnsWithOrOperator: bool` (drives the inline warning banner — true when `Operator` is `Or` or `NotOr` and `Children` contains conditions from more than one column).
- `FilterConditionNode.cs` — derives from `FilterEditorNode`. Has `Column: GridColumn` (setter resolves `ColumnDataType`, refreshes `SearchTemplate.ValidSearchTypes` via `SearchTypeRegistry.GetFiltersForDataType`), `SearchTemplate: SearchTemplate` (created when `Column` is first set), `AvailableColumns: ObservableCollection<GridColumn>` (passed in from `FilterEditor`), `RemoveCommand`. Pass-through properties: `SearchType`, `SelectedValue`, `SelectedSecondaryValue`, `InputTemplate`.
- `FilterEditorTreeBuilder.cs` — static helpers:
  - `FilterGroupNode BuildFromGrid(SearchDataGrid grid)` — open-time. Walks each `GridColumns[i]` controller's `SearchGroups`; emits a `FilterConditionNode` per `SearchTemplate` and a `FilterGroupNode` per `SearchTemplateGroup` (recursing into `ChildGroups`). Returns a single root `FilterGroupNode { Operator = And }` containing the per-column groups.
  - `void WriteBackToGrid(FilterGroupNode root, SearchDataGrid grid)` — Apply-time. For each leaf `FilterConditionNode`, group by `Column`. For each column: `controller.ClearAndReset()`, then walk the editor tree top-down, creating one `SearchTemplateGroup` per editor group that contains at least one condition for this column (preserving the editor group's `OperatorName`/`IsNegated` via `LogicalOperator.ToTokenString()`); creating `SearchTemplate` rows for each matching condition; recursing into `ChildGroups`. After walking, call `controller.UpdateFilterExpression()` for every touched column.
  - `FilterGroupNode DeepClone(FilterGroupNode root)` — optional snapshot (reserved for a future undo capability).
- `FilterEditorNodeTemplateSelector.cs` — `DataTemplateSelector` returning `FilterGroupTemplate` if `item is FilterGroupNode` else `FilterConditionRowTemplate`. Both templates are referenced as static resources by key.

### Created (WPF — chip controls)

All under `WWSearchDataGrid.Modern.WPF/Controls/FilterEditor/`, with `.xaml` partners in `WWSearchDataGrid.Modern.WPF/Themes/Controls/FilterEditor/`.

- `EditableTokenBase.cs` — shared base `Control` with DPs: `DisplayText: string`, `ChipBackground: Brush`, `IsEditing: bool` (driven by `IsKeyboardFocusWithin` via a template trigger; settable externally for testing). Template parts `PART_Display` (TextBlock) and `PART_Editor` (ContentPresenter). Applies `TokenConfirmationBehavior.IsEnabled="True"` to its outer Grid for hover-× removal.
- `ColumnNameTokenEditor.cs` (orange, `#ffc69b`) — DPs: `SelectedColumn: GridColumn` (two-way), `AvailableColumns: IEnumerable<GridColumn>`. `DisplayText` derived from `SelectedColumn?.HeaderCaption ?? SelectedColumn?.ColumnDisplayName`. Editor template = `ComboBox` styled with `{x:Static sdg:SdgThemeKeys.PrimitivesComboBox}`, `ItemTemplate` shows header text. + XAML partner.
- `SearchTypeTokenEditor.cs` (light blue, `#cee8fb`) — DPs: `SelectedSearchType: SearchType` (two-way), `ValidSearchTypes: IEnumerable<SearchType>`. `DisplayText` from `SearchTypeRegistry.GetMetadata(SelectedSearchType).DisplayName`. Editor template = `ComboBox` whose `ItemTemplate` mirrors `AdvancedFilterSearchGroupContentTemplate`'s combo (`SearchTypeToIconConverter` icon + `EnumToStringConverter` text — see `ColumnFilterEditor.xaml` lines 66-77). + XAML partner.
- `ValueTokenEditor.cs` (light green, `#bbe2c5`) — DP: `SearchTemplate: SearchTemplate`. Editor template's `ContentControl.Content="{TemplateBinding SearchTemplate}"`; the `Style.Triggers` block from `ColumnFilterEditor.xaml` lines 87-114 is copied into this control's template so each `FilterInputTemplate` value selects the matching DataTemplate from `SharedFilterRuleTemplates.xaml` (`SingleSearchTextBoxTemplate`, `DualSearchTextBoxTemplate`, `DualDateTimePickerTemplate`, `NumericUpDownTemplate`, `NoInputTemplate`, `SearchTextBoxListTemplate`, `DateTimePickerListTemplate`, `DateIntervalCheckListTemplate`). Display mode `DisplayText` reuses `SearchTemplateController.GetTokenizedFilterComponents` against a single-template scratch group to summarize the value(s). + XAML partner.
- `GroupOperatorChip.cs` (4-state dropdown chip) — DP: `Operator: LogicalOperator` (two-way). Template = `ToggleButton` whose content is the chip Border + DisplayText + chevron glyph, with a `Popup` containing a `ListBox` of the 4 operators. Selecting an item sets `Operator` and closes the popup. + XAML partner.

### Created (WPF — XAML templates)

Folder: `WWSearchDataGrid.Modern.WPF/Themes/Controls/FilterEditor/`

- `ColumnNameTokenEditor.xaml` — control template (display + editor swap via `IsEditing` trigger).
- `SearchTypeTokenEditor.xaml` — same pattern.
- `ValueTokenEditor.xaml` — same pattern; merges `SharedFilterRuleTemplates.xaml`.
- `GroupOperatorChip.xaml` — chip with chevron + popup ListBox.
- `FilterEditorTemplates.xaml` — contains:
  - `FilterGroupTemplate` (DataTemplate for `FilterGroupNode`): outer vertical `StackPanel`; header `StackPanel Orientation="Horizontal"` containing the `GroupOperatorChip` + a `Button` opening the Add popup; inline warning `Border` (visible when `HasMixedColumnsWithOrOperator`); body `ItemsControl` bound to `Children` with `ItemTemplateSelector="{StaticResource FilterEditorNodeTemplateSelector}"`. Indent nested groups via a `Border` `Margin="24,4,0,4"` wrapping the inner `ContentControl` that re-applies `FilterGroupTemplate`.
  - `FilterConditionRowTemplate` (DataTemplate for `FilterConditionNode`): horizontal `StackPanel` with `ColumnNameTokenEditor` + `SearchTypeTokenEditor` + `ValueTokenEditor`, plus a hover-revealed × button.
  - `AddPopupTemplate`: `Popup` content with three buttons — "Add Condition" → `AddConditionCommand`, "Add Group" → `AddGroupCommand`, "Add Custom Expression" with `IsEnabled="False"` and tooltip "Coming soon".
  - `FilterEditorNodeTemplateSelector` instance keyed for use.

### Modified (Core)

- `WWSearchDataGrid.Modern.Core/Data/Models/Search/SearchTemplateGroup.cs`
  - Add `public ObservableCollection<SearchTemplateGroup> ChildGroups { get; } = new();`
  - Extend `OperatorName` setter to accept `"NotAnd"` / `"NotOr"` (case-insensitive). `OperatorFunction` continues to hold only the inner combiner (`Expression.AndAlso` for And/NotAnd, `Expression.OrElse` for Or/NotOr).
  - Add `public bool IsNegated => OperatorName?.Equals("NotAnd", StringComparison.OrdinalIgnoreCase) == true || OperatorName?.Equals("NotOr", StringComparison.OrdinalIgnoreCase) == true;`
- `WWSearchDataGrid.Modern.Core/Services/FilterExpressionBuilder.cs`
  - Extract per-group body construction into a private `BuildGroupExpression(SearchTemplateGroup group, Type targetColumnType, bool forceTargetTypeAsString)` method.
  - Inside it: build the existing templates' combined expression; then recursively call `BuildGroupExpression` for each `ChildGroups` entry and combine via that child's `OperatorFunction`. After combining, if `group.IsNegated`, wrap the combined body in `Expression.Not(...)`.
  - The existing outer loop (around lines 37-91) calls `BuildGroupExpression` for top-level groups.

### Modified (WPF)

- `WWSearchDataGrid.Modern.WPF/Controls/FilterEditor.cs`
  - Replace the placeholder `ActiveFilters` / `FilterTokens` DPs with new DPs: `RootGroup: FilterGroupNode`, `AvailableColumns: ObservableCollection<GridColumn>`.
  - Remove `RebuildTokens()` and `OnActiveFiltersChanged`; replace with `BuildEditorTree()` that calls `FilterEditorTreeBuilder.BuildFromGrid(OwnerGrid)`.
  - In `ShowDialog(SearchDataGrid grid)`: set `OwnerGrid`, `AvailableColumns = new(grid.GridColumns)`, `RootGroup = FilterEditorTreeBuilder.BuildFromGrid(grid)`. If `RootGroup.Children.Count == 0`, prepend a default `FilterConditionNode` with the first grid column pre-selected.
  - `ExecuteApply()`: `FilterEditorTreeBuilder.WriteBackToGrid(RootGroup, OwnerGrid); OwnerGrid.FilterItemsSource();`. Does not close.
  - `OkCommand`: Apply + close. `CancelCommand`: close.
- `WWSearchDataGrid.Modern.WPF/Themes/Controls/FilterEditor.xaml`
  - Replace placeholder body (WrapPanel `ItemsControl`) with a `ScrollViewer` containing a `ContentControl` bound to `{TemplateBinding RootGroup}` using `FilterGroupTemplate`.
  - Keep header/footer rows; reorder footer buttons to OK / Cancel / Apply (right-aligned) per the screenshot.
- `WWSearchDataGrid.Modern.WPF/Themes/SdgThemeKeys.cs`
  - Add `ColumnNameTokenEditor`, `SearchTypeTokenEditor`, `ValueTokenEditor`, `GroupOperatorChip` ComponentResourceKey entries (matching existing pattern at lines 75-89).
- `WWSearchDataGrid.Modern.WPF/Themes/Generic.xaml`
  - Add merged dictionaries for the new XAML files in this order: `GroupOperatorChip.xaml`, `ColumnNameTokenEditor.xaml`, `SearchTypeTokenEditor.xaml`, `ValueTokenEditor.xaml`, `FilterEditorTemplates.xaml`.

---

## Reused functions / utilities (do not duplicate)

- `SearchTypeRegistry.GetFiltersForDataType(ColumnDataType, bool isNullable)` — column→valid search types mapping. Called from `FilterConditionNode.Column` setter to refresh `SearchTemplate.ValidSearchTypes`.
- `SearchTemplate.UpdateInputTemplate()` (already auto-fires on `SearchType` change) — drives the `InputTemplate` value that `ValueTokenEditor`'s editor `Style.Triggers` consume.
- `SearchTemplateController.GetTokenizedFilterComponents()` — used to format `ValueTokenEditor.DisplayText`.
- `SharedFilterRuleTemplates.xaml` — all 8 `FilterInputTemplate` DataTemplates already exist; `ValueTokenEditor.xaml` merges this dictionary and references the templates by static resource key.
- `TokenConfirmationBehavior` (attached behavior, used at `FilterTokenTemplates.xaml` lines 64-93) — reused for hover-× on rows and group headers.
- `ColumnFilterEditor.xaml` lines 87-114 (InputTemplate `Style.Triggers`) — copied verbatim into `ValueTokenEditor.xaml`'s editor template.
- `SearchTemplateController.ClearAndReset()` — called per column in `WriteBackToGrid` before pushing the new structure.
- `SearchDataGrid.FilterItemsSource()` — single call after write-back; rebuilds the unified filter and refreshes `FilterPanel`.

---

## Verification

Run the SampleApp (`WWSearchDataGrid.Modern.SampleApp`) and exercise the editor end-to-end:

1. **Open flow** — Apply a single-column auto-filter on the grid; confirm a chip appears in `FilterPanel`. Click the FilterPanel button that fires `OpenFilterEditorRequested`. The editor opens with that filter visible as a `FilterConditionNode` inside a `FilterGroupNode { Operator = And }`.
2. **Add a condition** — Click `[+]` → "Add Condition". A new row appears with the first column pre-selected. The orange chip is in Display mode showing the column name.
3. **Edit Column chip** — Click the orange chip; it swaps to Edit mode (ComboBox). Pick a different column. Confirm the blue SearchType chip's dropdown content updates to the new column's valid types (`SearchTypeRegistry.GetFiltersForDataType` was invoked via `Column` setter).
4. **Edit SearchType chip** — Click blue chip; pick `Between`. Confirm the green Value chip's editor changes from `SingleSearchTextBox` to `DualSearchTextBox` (driven by `SearchTemplate.InputTemplate`).
5. **Edit Value chip** — Type values into both inputs. Tab away; chip collapses to Display mode showing `"X and Y"`.
6. **Operator dropdown** — Click the operator chip's chevron. Popup shows 4 options. Pick `Not And`. Chip Display reads "Not And".
7. **Add nested group** — Click `[+]` inside an existing group → "Add Group". A child group renders indented (24px left margin) with its own operator chip and `[+]` button. Add conditions inside.
8. **Cross-column OR warning** — Add a group, set its operator to `Or`, add two conditions targeting different columns. The inline warning banner appears explaining the lossy round-trip.
9. **Apply** — Click Apply (window stays open). Grid filters per the new criteria. FilterPanel chip strip updates. Confirm via local IDE that per-column `SearchTemplateController.SearchGroups` now contain the expected slices including any `ChildGroups`.
10. **Reopen** — Close the editor (OK). Reopen via the FilterPanel button. The tree reconstructs from per-column controllers and matches what was applied (limited by cross-column-OR slicing per item 8).
11. **Cancel** — Make edits; click Cancel. Confirm nothing changed in per-column controllers or grid filtering.
12. **NotAnd / NotOr in evaluation** — Build a filter `Not(ColumnA = X AND ColumnA = Y)` using a nested group with `Operator = NotAnd`. Apply. Confirm grid rows where the inner predicate is true are EXCLUDED (`FilterExpressionBuilder.BuildGroupExpression` honored the `Expression.Not` wrap).
13. **Tests** — Run `WWSearchDataGrid.Modern.Core.Tests` to confirm no regressions in `FilterExpressionBuilder` against the existing And/Or-only suite. Add focused unit tests for: `BuildGroupExpression` with `NotAnd` produces a NOT-wrapped AndAlso; `BuildGroupExpression` recurses into `ChildGroups`; `LogicalOperatorExtensions.Parse` handles the 4 strings + unknown.
14. **Per-column popup smoke** — Open the auto-filter row's per-column popup (`ColumnFilterEditor`) on a column whose controller now contains `ChildGroups` from an editor write-back. Confirm the popup still renders only the top-level `SearchTemplates` (its XAML doesn't iterate `ChildGroups`) and the user can still add/edit rules there without crashes.

---

## Open risks / follow-ups

- **Cross-column OR is lossy** — the warning banner is the v1 mitigation. Phase 2 should introduce a grid-level `FilterRoot` tree on `SearchDataGrid` and migrate `SearchDataGridFiltering.EvaluateUnifiedFilter` to compile from it; per-column controllers become derived views.
- **FilterPanel chip strip is unaware of NotAnd/NotOr** — `GetActiveColumnFilters` will emit `"NOTAND"`/`"NOTOR"` strings; `FilterPanel`'s click-to-toggle (`OnOperatorToggled` line 878) only handles And↔Or. Until taught the 4-state cycle, the chip strip is advisory; authoritative UI is the editor.
- **"Add Custom Expression"** — currently disabled. Future: introduce `FilterCustomExpressionNode` + parser; out of scope here.
- **No in-editor undo** — Apply is destructive. If the team wants undo later, layer a `Stack<FilterGroupNode>` checkpoint using `FilterEditorTreeBuilder.DeepClone` before Apply.
- **AutoFilterRow concurrent edits** — the editor is modal (`ShowDialog`), so the AutoFilterRow shouldn't be reachable while it's open. Verify modal behavior actually disables the grid behind the dialog; if not, disable the filter row in the editor's `Loaded` handler and restore on close.
- **Localization** — operator chip text is hardcoded English in `LogicalOperatorExtensions.DisplayText`. Existing FilterPanel has the same limitation. Defer.
