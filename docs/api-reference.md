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
| `FindGridColumnDescriptor(column)` | Returns the `GridColumn` descriptor that generated the given `DataGridColumn`, or null. |

### Scrolling & Animation

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AllowPerPixelScrolling` | `bool` | `false` | Switches scrolling from item-based (one row per step) to pixel-based (smooth sub-row positioning). Required for `AllowScrollAnimation`. **See performance warning below.** |
| `AllowScrollAnimation` | `bool` | `false` | Enables momentum/inertia scrolling on mouse wheel input. Auto-enables `AllowPerPixelScrolling`. **See performance warning below.** |
| `ScrollAnimationMode` | `ScrollAnimationMode` | `EaseOut` | Deceleration curve for the scroll animation (`EaseOut`, `EaseInOut`, `Linear`, `Custom`). |
| `ScrollAnimationDuration` | `double` (ms) | `800` | How long the scroll animation takes to fully decelerate after the last wheel input. |
| `AllowCascadeUpdate` | `bool` | `false` | Master switch for cascading row-reveal animations (rows fade in as they enter the viewport). |
| `RowAnimationKind` | `RowAnimationKind` | `None` | Type of row reveal animation (`None`, `Opacity`, `Custom`). |
| `RowAnimationEasing` | `RowAnimationEasing` | `EaseOut` | Easing curve for the row opacity animation. |
| `RowOpacityAnimationDuration` | `double` (ms) | `200` | Duration of each row's fade-in. |
| `CascadeStagger` | `double` (ms) | `20` | Delay between consecutive rows in a cascade burst. Set to `0` to disable stagger. |
| `VirtualizationCacheLength` | `double` (pages) | `2.0` | How many pages of rows to pre-realize above and below the viewport. |
| `ShowHorizontalGridLines` | `bool` | `true` | Horizontal gridlines between rows. Rendered outside the cells, so row fade animations don't affect them. |
| `ShowVerticalGridLines` | `bool` | `true` | Vertical gridlines between columns. Rendered inside the cell area and fade with row animations. |

> ⚠ **Performance warning — per-pixel scrolling and scroll animation on large datasets**
>
> `AllowPerPixelScrolling` and `AllowScrollAnimation` are intended for grids up to roughly **100k rows**. Enabling them on larger datasets produces noticeably choppy scrolling.
>
> The root cause is a WPF limitation: `VirtualizingStackPanel` in pixel-scroll mode invalidates its measure pass on every fractional-pixel scroll offset change, where item-scroll mode short-circuits until the visible row set actually changes. The smooth-scroll behavior calls `ScrollToVerticalOffset` once per frame (~60 Hz), which amplifies the cost. Row realization (template apply, bindings, cell layout across N columns) compounds it further.
>
> For datasets in the hundreds-of-thousands-to-millions range, leave both properties at their default `false`. Item-mode scrolling scales cleanly to millions of rows and is also better for keyboard navigation and accessibility.

---

## GridColumn (Column Descriptor)

A `FrameworkContentElement` that describes a column. Declared inside `SearchDataGrid.GridColumns`; the grid generates internal `DataGridColumn` instances from each descriptor automatically.

### Layout Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FieldName` | `string` | `null` | **Primary key.** Property name on the data source. Auto-generates Binding, SortMemberPath, FilterMemberPath. |
| `Header` | `object` | `null` | Column header content. Falls back to `FieldName`. |
| `Width` | `DataGridLength` | `Auto` | Column width. |
| `MinWidth` | `double` | `20` | Minimum column width. |
| `MaxWidth` | `double` | `+Infinity` | Maximum column width. |
| `Visible` | `bool` | `true` | Column visibility. |
| `VisibleIndex` | `int` | `-1` | Position among visible columns (-1 = auto). |
| `Fixed` | `bool` | `false` | Frozen column. |
| `AllowMoving` | `bool` | `true` | User can drag-reorder. |
| `AllowResizing` | `bool` | `true` | User can resize. |
| `ShowInColumnChooser` | `bool` | `true` | Appears in column chooser. |
| `ReadOnly` | `bool` | `false` | Prevents editing. |

### Data/Display Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `FilterMemberPath` | `string` | `null` | Overrides `FieldName` for filtering. |
| `SortMemberPath` | `string` | `null` | Overrides `FieldName` for sorting. |
| `ColumnDisplayName` | `string` | `null` | Display name for filter panel and column chooser. |
| `DisplayStringFormat` | `string` | `null` | .NET format string (e.g., "C2", "MM/dd/yyyy"). |
| `DisplayValueConverter` | `IValueConverter` | `null` | Custom display converter. |
| `DisplayConverterParameter` | `object` | `null` | Converter parameter. |
| `DisplayMask` | `string` | `null` | Mask pattern for formatted display. |
| `FieldType` | `Type` | `null` | Data type. Auto-detected in Phase 2; set explicitly for now (e.g., `{x:Type sys:Boolean}` for checkbox columns). |

### Filtering/Search Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableRuleFiltering` | `bool` | `true` | Enables advanced filter UI. Inherits grid-level setting when not set. |
| `DefaultSearchMode` | `DefaultSearchMode` | `Contains` | Search type for simple textbox: `Contains`, `StartsWith`, `EndsWith`, `Equals`. |
| `UseCheckBoxInSearchBox` | `bool` | `false` | Checkbox filter for boolean columns. |
| `CustomSearchTemplate` | `Type` | `null` | Custom search template type. |
| `AllowFiltering` | `bool` | `true` | When false, hides the search box entirely. |
| `AllowSorting` | `bool` | `true` | When false, disables header sort click. |

### Select-All Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsSelectAllColumn` | `bool` | `false` | Adds select-all checkbox to header. Boolean columns only. |
| `SelectAllScope` | `SelectAllScope` | `FilteredRows` | Scope: `FilteredRows`, `SelectedRows`, `AllItems`. |

### Internal Properties (read-only, set by SearchDataGrid)

| Property | Type | Description |
|----------|------|-------------|
| `InternalColumn` | `DataGridColumn` | The generated WPF column. |
| `Owner` | `SearchDataGrid` | Parent grid reference. |
| `IsAutoGenerated` | `bool` | Whether the grid auto-created this column. |
| `ActualWidth` | `double` | Resolved rendered width. |
| `ActualVisibleIndex` | `int` | Resolved position. |

---

## GridColumnSettings (Legacy Attached Properties)

Static class providing attached properties for the legacy column configuration syntax. These properties are applied to `DataGridColumn` instances and are also used internally to bridge `GridColumn` descriptor values to the WPF column infrastructure.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableRuleFiltering` | `bool` | `true` | Enables/disables advanced filter UI for this column. |
| `UseCheckBoxInSearchBox` | `bool` | `false` | Checkbox filter for boolean columns. |
| `FilterMemberPath` | `string` | `null` | Property path for filter value retrieval. |
| `ColumnDisplayName` | `string` | `null` | Display name for filter panel and column chooser. |
| `DefaultSearchMode` | `DefaultSearchMode` | `Contains` | Search type for simple textbox input. |
| `IsSelectAllColumn` | `bool` | `false` | Adds select-all checkbox to header. |
| `SelectAllScope` | `SelectAllScope` | `AllItems` | Scope for select-all. |
| `DisplayStringFormat` | `string` | `null` | .NET format string. |
| `DisplayValueConverter` | `IValueConverter` | `null` | Custom display converter. |
| `DisplayConverterParameter` | `object` | `null` | Converter parameter. |
| `DisplayMask` | `string` | `null` | Mask pattern. |
| `CustomSearchTemplate` | `Type` | `typeof(SearchTemplate)` | Custom search template type. |

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
