# API Reference

## SearchDataGrid

Extends `System.Windows.Controls.DataGrid`. All standard DataGrid properties, methods, and events are available.

### Dependency Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableRuleFiltering` | `bool` | `false` | Enables advanced multi-criteria filtering UI. Inherits to child columns. |
| `AutoSizeColumns` | `bool` | `false` | Dynamically sizes columns to fit visible content during scrolling. |
| `IsColumnChooserEnabled` | `bool` | `false` | Enables the column chooser feature (accessible via context menu). |
| `IsColumnChooserVisible` | `bool` | `false` | Shows/hides the column chooser window. Requires `IsColumnChooserEnabled="True"`. |
| `IsColumnChooserConfinedToGrid` | `bool` | `false` | Constrains the column chooser window to the grid's viewport bounds. |
| `SearchFilter` | `Predicate<object>` | `null` | The current compiled filter predicate applied to items. |
| `ActualHasItems` | `bool` | `false` | Read-only. Whether the original (unfiltered) data source has any items. |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `DataColumns` | `ObservableCollection<ColumnSearchBox>` | Collection of column search box controls, one per column. |
| `OriginalItemsSource` | `IEnumerable` | The original unfiltered items source. |
| `FilterPanel` | `FilterPanel` | The filter panel control showing active filter chips. |
| `OriginalItemsCount` | `int` | Count of items in the original data source. |

### Events

| Event | Args Type | Description |
|-------|-----------|-------------|
| `CellValueChanged` | `CellValueChangedEventArgs` | Raised when a cell value changes through editing. Includes old/new values, column, and row info. |
| `ItemsSourceChanged` | `EventArgs` | Raised when the items source is replaced. |
| `CollectionChanged` | `NotifyCollectionChangedEventArgs` | Raised when items are added/removed from the collection. |

### Methods

| Method | Description |
|--------|-------------|
| `FilterItemsSource(int delay = 0)` | Applies all active filters to the items source. Optional delay in ms. |
| `ClearAllFilters()` | Clears all column filters and resets the view. |
| `ClearAllCachedData()` | Clears all cached column values and collection contexts. Call when replacing data sources. |
| `UpdateFilterPanel()` | Refreshes the filter panel to reflect current filter state. |

---

## GridColumn (Attached Properties)

Static class providing attached properties for configuring individual columns. Apply to any `DataGridColumn` type.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableRuleFiltering` | `bool` | `true` | Enables/disables advanced filter UI for this column. Inherits from grid. |
| `UseCheckBoxInSearchBox` | `bool` | `false` | Replaces the text search box with a checkbox filter for boolean columns. |
| `FilterMemberPath` | `string` | `null` | Property path for filter value retrieval. Falls back to `SortMemberPath`, then `Binding.Path`. |
| `ColumnDisplayName` | `string` | `null` | Display name for filter panel and column chooser. Falls back to `Header` text. |
| `DefaultSearchMode` | `DefaultSearchMode` | `Contains` | Search type for simple textbox input: `Contains`, `StartsWith`, `EndsWith`, `Equals`. |
| `IsSelectAllColumn` | `bool` | `false` | Adds a select-all checkbox to the column header. Only works on boolean columns. |
| `SelectAllScope` | `SelectAllScope` | `AllItems` | Scope for select-all: `FilteredRows`, `SelectedRows`, `AllItems`. |
| `CustomSearchTemplate` | `Type` | `typeof(SearchTemplate)` | Custom search template type for advanced scenarios. |

### Static Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetEffectiveColumnDisplayName(column)` | `string` | Gets the display name using fallback logic (explicit name -> header text). |
| `IsColumnBooleanType(column, grid)` | `bool` | Determines if a column is boolean via multiple detection methods. |

---

## FilterPanel

Displays active filters as chips with enable/disable toggle.

### Events

| Event | Args Type | Description |
|-------|-----------|-------------|
| `FiltersEnabledChanged` | `FilterEnabledChangedEventArgs` | Raised when filters are toggled on/off. |
| `FilterRemoved` | `RemoveFilterEventArgs` | Raised when a filter chip is removed. |
| `ValueRemovedFromToken` | `ValueRemovedFromTokenEventArgs` | Raised when a value is removed from a multi-value filter. |
| `OperatorToggled` | `OperatorToggledEventArgs` | Raised when AND/OR operator is toggled. |
| `ClearAllFiltersRequested` | `EventArgs` | Raised when "Clear All" is clicked. |

---

## CellValueChangedEventArgs

| Property | Type | Description |
|----------|------|-------------|
| `Item` | `object` | The data item (row) that was edited. |
| `Column` | `DataGridColumn` | The column that was edited. |
| `BindingPath` | `string` | The property path of the edited value. |
| `OldValue` | `object` | The value before editing. |
| `NewValue` | `object` | The value after editing. |
| `RowIndex` | `int` | Visual row index of the edited cell. |
| `ColumnIndex` | `int` | Visual column index of the edited cell. |

---

## Enums

### DefaultSearchMode
- `Contains` - Match anywhere in value (default)
- `StartsWith` - Match from beginning
- `EndsWith` - Match at end
- `Equals` - Exact match only

### SelectAllScope
- `FilteredRows` - Currently visible/filtered rows
- `SelectedRows` - Currently selected rows (shows count in header)
- `AllItems` - All items in ItemsSource regardless of filters

### SearchType (Core)
Text: `Contains`, `DoesNotContain`, `StartsWith`, `EndsWith`, `Equals`, `NotEquals`
Comparison: `LessThan`, `LessThanOrEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`
Range: `Between`, `NotBetween`, `BetweenDates`
Null: `IsNull`, `IsNotNull`
Pattern: `IsLike`, `IsNotLike`
Multi-value: `IsAnyOf`, `IsNoneOf`, `IsOnAnyOfDates`
Date: `Yesterday`, `Today`, `Tomorrow`, `DateInterval`
Statistical: `TopN`, `BottomN`, `AboveAverage`, `BelowAverage`, `Unique`, `Duplicate`
