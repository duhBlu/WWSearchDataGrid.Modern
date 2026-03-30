# Changelog

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
