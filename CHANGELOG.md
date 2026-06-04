# Changelog

## [Unreleased]

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
