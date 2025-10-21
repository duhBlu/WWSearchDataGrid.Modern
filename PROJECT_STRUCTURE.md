# WWSearchDataGrid.Modern - Complete Project Structure & Architecture

## Overview
WWSearchDataGrid.Modern is a comprehensive WPF data grid library with advanced search and filtering capabilities. This document provides a complete architectural overview including class hierarchies, data flows, and integration patterns discovered through deep codebase analysis.

**Last Updated**: 2025-10-21
**Analysis Method**: Multi-agent deep architecture tracing

---

## Table of Contents
1. [Solution Structure](#solution-structure)
2. [Core Architecture (WWSearchDataGrid.Modern.Core)](#core-architecture)
3. [WPF Implementation (WWSearchDataGrid.Modern.WPF)](#wpf-implementation)
4. [Sample Application](#sample-application)
5. [Data Flow Architecture](#data-flow-architecture)
6. [Design Patterns](#design-patterns)
7. [Performance Optimizations](#performance-optimizations)

---

## Solution Structure

```
WWSearchDataGrid.Modern.sln
├── WWSearchDataGrid.Modern.Core/         (Core logic - .NET Standard 2.0)
│   ├── Common/
│   │   ├── Commands/RelayCommand.cs      (ICommand implementation)
│   │   └── Helpers/
│   │       ├── ReflectionHelper.cs       (Property access utilities)
│   │       └── TypeTranslatorHelper.cs   (Type conversion)
│   ├── Data/
│   │   ├── Models/
│   │   │   ├── Base/ObservableObject.cs  (INotifyPropertyChanged base)
│   │   │   └── Search/
│   │   │       ├── SearchCondition.cs     (Individual filter criteria)
│   │   │       ├── SearchTemplate.cs      (Filter template model)
│   │   │       ├── SearchTemplateGroup.cs (Logical grouping)
│   │   │       └── SearchTemplateController.cs (Central coordinator)
│   │   └── Enums/
│   │       ├── SearchType.cs             (27 search operations)
│   │       ├── ColumnDataType.cs         (Data type categories)
│   │       ├── DateInterval.cs           (13 date intervals)
│   │       └── FilterInputTemplate.cs    (UI template types)
│   ├── Search/
│   │   ├── Core/
│   │   │   ├── SearchEngine.cs           (Static evaluation dispatcher)
│   │   │   └── CollectionContext.cs      (Statistical context)
│   │   └── Evaluators/
│   │       ├── Base/SearchEvaluatorBase.cs
│   │       ├── SearchEvaluatorFactory.cs (Singleton factory)
│   │       └── [26+ evaluator implementations]
│   ├── Services/
│   │   └── FilterExpressionBuilder.cs    (Expression compilation)
│   └── Registry/
│       └── SearchTypeRegistry.cs         (Metadata registry)
│
├── WWSearchDataGrid.Modern.WPF/          (WPF controls - .NET 9.0-windows)
│   ├── Controls/                         (Custom controls - .cs only)
│   │   ├── SearchDataGrid.cs            (Main DataGrid control)
│   │   ├── ColumnSearchBox.cs           (Per-column filter)
│   │   ├── ColumnFilterEditor.cs        (Advanced filter dialog)
│   │   ├── FilterPanel.cs               (Active filter chips)
│   │   ├── ColumnChooser.cs             (Column visibility)
│   │   ├── GridColumn.cs                (Static attached properties)
│   │   └── Primitives/
│   │       ├── SearchTextBox.cs         (Custom search input)
│   │       └── NumericUpDown.cs         (Numeric spinner)
│   ├── Behaviors/                        (Attached property behaviors)
│   │   ├── HorizontalScrollBehavior.cs  (Shift+wheel scrolling)
│   │   ├── TokenConfirmationBehavior.cs (Two-step delete confirm)
│   │   └── TokenHoverBehavior.cs        (Filter token highlighting)
│   ├── Commands/                         (Context menu commands)
│   │   ├── ContextMenuCommands.cs       (Base partial class)
│   │   ├── ContextMenuExtensions.cs     (Dynamic menu builder)
│   │   └── ContextMenuCommands/
│   │       ├── ColumnModifierCommands.cs (Hide, sort, best-fit)
│   │       ├── CopyCommands.cs           (Clipboard operations)
│   │       ├── FilterCommands.cs         (Clear filters)
│   │       └── ExtensionVisibilityCommands.cs (Show dialogs)
│   ├── Converters/                       (18 IValueConverter implementations)
│   │   ├── BooleanToVisibilityCollapsedConverter.cs
│   │   ├── SearchTypeToIconConverter.cs
│   │   ├── ValueToCountConverter.cs
│   │   └── [15 more converters...]
│   ├── Helper/
│   │   └── VisualTreeHelperMethods.cs   (Visual tree traversal)
│   ├── Resources/
│   │   └── Icons/Icons.xaml             (37 DrawingImage icons)
│   └── Themes/
│       ├── Generic.xaml                  (Main theme merger)
│       └── Controls/                     (Control XAML templates)
│           ├── SearchDataGrid.xaml
│           ├── ColumnSearchBox.xaml
│           ├── FilterPanel.xaml
│           ├── ColumnFilterEditor.xaml
│           └── Primitives/
│               ├── SearchTextBox.xaml
│               └── NumericUpDown.xaml
│
└── WWSearchDataGrid.Modern.SampleApp/    (Demo app - .NET 9.0-windows)
    ├── Models/
    │   ├── DataItem.cs                   (25+ property types)
    │   └── Enums.cs                      (Priority, OrderStatus)
    ├── ViewModels/
    │   └── MainViewModel.cs              (MVVM with CommunityToolkit)
    ├── Views/
    │   └── MainWindow.xaml               (SearchDataGrid usage examples)
    └── Services/
        └── ThemeManager.cs               (Runtime theme switching)
```

---

## Core Architecture (WWSearchDataGrid.Modern.Core)

### Class Hierarchy

```
ObservableObject (abstract base) - INotifyPropertyChanged implementation
├── SearchTemplate                - Individual filter criterion
├── SearchTemplateGroup           - Logical grouping with AND/OR
├── SearchTemplateController      - Central coordinator (also IDisposable)
├── SelectableValueItem           - Multi-value filter item
└── DateIntervalItem              - Date interval selection item

SearchEvaluatorBase (abstract) - ISearchEvaluator
├── Text Evaluators (6)
│   ├── ContainsEvaluator
│   ├── DoesNotContainEvaluator
│   ├── StartsWithEvaluator
│   ├── EndsWithEvaluator
│   ├── EqualsEvaluator
│   └── NotEqualsEvaluator
├── Comparison Evaluators (6)
│   ├── LessThanEvaluator
│   ├── LessThanOrEqualToEvaluator
│   ├── GreaterThanEvaluator
│   ├── GreaterThanOrEqualToEvaluator
│   ├── BetweenEvaluator
│   └── NotBetweenEvaluator
├── Date Evaluators (4)
│   ├── DateIntervalEvaluator
│   ├── YesterdayEvaluator
│   ├── TodayEvaluator
│   └── BetweenDatesEvaluator
├── Collection Evaluators (3)
│   ├── IsAnyOfEvaluator
│   ├── IsNoneOfEvaluator
│   └── IsOnAnyOfDatesEvaluator
├── Null Evaluators (2)
│   ├── IsNullEvaluator
│   └── IsNotNullEvaluator
├── Pattern Evaluators (2)
│   ├── IsLikeEvaluator          (SQL LIKE with % and _)
│   └── IsNotLikeEvaluator
└── Statistical Evaluators (6) - RequiresCollectionContext = true
    ├── TopNEvaluator
    ├── BottomNEvaluator
    ├── AboveAverageEvaluator
    ├── BelowAverageEvaluator
    ├── UniqueEvaluator
    └── DuplicateEvaluator
```

### Key Classes Deep Dive

#### SearchEngine.cs (`/Search/Core/SearchEngine.cs`)
**Purpose**: Static dispatcher using Strategy pattern

**Key Methods**:
```csharp
static bool EvaluateCondition(object columnValue, SearchCondition searchCondition)
static bool EvaluateCondition(object columnValue, SearchCondition searchCondition, CollectionContext context)
static int CompareValues(object columnValue, SearchCondition searchCondition, object comparisonValue)
static bool RequiresCollectionContext(SearchType searchType)
```

**Evaluation Flow**:
1. Get evaluator from `SearchEvaluatorFactory.Instance.GetEvaluator(searchType)`
2. Check if requires collection context (TopN, AboveAverage, etc.)
3. Call appropriate `Evaluate()` method
4. Return boolean result

#### SearchTemplateController.cs (`/Data/Models/Search/SearchTemplateController.cs`)
**Purpose**: Central coordinator - Façade + Cache Manager + Orchestrator

**Key Responsibilities**:
- Manages `ObservableCollection<SearchTemplateGroup>` hierarchy
- Compiles filter expressions via `FilterExpressionBuilder`
- Caches column values using `ColumnValueCacheManager` singleton
- Provides lazy-loaded column data via `IReadOnlyList<object> ColumnValues`
- Handles incremental cache updates (`TryAddColumnValues`, `TryRemoveColumnValues`)
- Generates filter display text for UI (`GetTokenizedFilterComponents()`)

**Performance Optimizations**:
- Lazy loading: Data loaded only when first accessed
- Expression compilation: Compiled `Func<object, bool>` for fast evaluation
- Cache sharing: Global `ColumnValueCacheManager` prevents duplicate data
- Incremental updates: Add/remove without full refresh

**Key Properties**:
```csharp
Func<object, bool> FilterExpression          // Compiled filter delegate
ObservableCollection<SearchTemplateGroup> SearchGroups
IReadOnlyList<object> ColumnValues           // Cached column data
bool HasCustomExpression                     // Filter active flag
```

#### SearchTemplate.cs (`/Data/Models/Search/SearchTemplate.cs`)
**Purpose**: Individual filter criterion with UI-bindable properties

**Key Features**:
- Two-way data binding via `ObservableObject`
- Expression building: `BuildExpression(Type targetType)` returns `Expression<Func<object, bool>>`
- Multi-value support: `ObservableCollection<SelectableValueItem>` for IsAnyOf/IsNoneOf
- Value removal: Intelligent handling of partial filter deletion
- Validation: `IsValidFilter` ensures required values are present

**Specialized Expression Builders**:
```csharp
BuildIsAnyOfExpression()           // obj => values.Contains(obj.ToString())
BuildIsNoneOfExpression()          // obj => !values.Contains(obj.ToString())
BuildDateIntervalExpression()      // Complex date range with OR composition
BuildIsOnAnyOfDatesExpression()    // Date collection membership
```

#### CollectionContext.cs (`/Search/Core/CollectionContext.cs`)
**Purpose**: Lazy-computed statistical context for collection-aware evaluators

**Architecture**: IDisposable with lazy initialization

**Cached Properties**:
```csharp
ClearableLazy<double?> _average                                  // Column average
ClearableLazy<IEnumerable<object>> _sortedDescending            // Sorted by value desc
ClearableLazy<IEnumerable<object>> _sortedAscending             // Sorted by value asc
ClearableLazy<Dictionary<object, List<object>>> _valueGroups    // Value -> items map
```

**Performance**: Single reflection pass via `_extractedValues`, shared by all operations

### Search Types (27 Total)

#### Basic Comparisons (8)
```csharp
Equals, NotEquals, GreaterThan, GreaterThanOrEqualTo,
LessThan, LessThanOrEqualTo, Between, NotBetween
```

#### Text Operations (6)
```csharp
Contains, DoesNotContain, StartsWith, EndsWith, IsLike, IsNotLike
```

#### Set Membership (3)
```csharp
IsAnyOf, IsNoneOf, IsOnAnyOfDates
```

#### Null Checks (2)
```csharp
IsNull, IsNotNull
```

#### Date-Specific (4)
```csharp
Today, Yesterday, BetweenDates, DateInterval
```

#### Statistical (4) - Require Collection Context
```csharp
TopN, BottomN, AboveAverage, BelowAverage, Unique, Duplicate
```

### Expression Compilation Pipeline

```
SearchTemplateController.UpdateFilterExpression()
  ↓
FilterExpressionBuilder.BuildFilterExpression()
  ↓
FOR EACH SearchTemplateGroup:
  FOR EACH SearchTemplate:
    template.BuildExpression(targetType)
      ↓
    Creates: Expression<Func<object, bool>>
      ↓
    Compose with operators (AND/OR):
      ParameterRebinder.ReplaceParameters()
      Expression.AndAlso() or Expression.OrElse()
  ↓
COMPILE: Expression<Func<object, bool>>.Compile()
  ↓
RESULT: Func<object, bool> FilterExpression
```

**Benefits**:
- **Performance**: Compiled to IL, executed at native speed
- **Composability**: Expressions combined with logical operators
- **Flexibility**: Can be analyzed, transformed, or cached

---

## WPF Implementation (WWSearchDataGrid.Modern.WPF)

### Control Hierarchy

```
System.Windows.Controls.Control
│
├── SearchDataGrid : DataGrid
│   └── Main grid with integrated filtering
│
├── ColumnSearchBox : Control
│   └── Per-column filter with simple/advanced modes
│
├── ColumnFilterEditor : Control, INotifyPropertyChanged
│   └── Advanced multi-criteria filter dialog
│
├── FilterPanel : Control
│   └── Active filter chip display with toggle
│
├── ColumnChooser : Control
│   └── Column visibility management window
│
└── GridColumn (static class)
    └── Attached properties for all DataGridColumn types
```

### SearchDataGrid.cs - Main Control

**File**: `/Controls/SearchDataGrid.cs`
**Extends**: `System.Windows.Controls.DataGrid`

**Key Dependency Properties**:
- `SearchFilterProperty` - Active filter predicate
- `ActualHasItemsProperty` - Tracks unfiltered item count
- `EnableRuleFilteringProperty` - Grid-level filtering toggle

**Core Filtering Methods**:
```csharp
FilterItemsSource(int delay = 0)                                    // Async filtering with debounce
EvaluateUnifiedFilter(object item, List<ColumnSearchBox> filters)  // AND/OR between columns
EvaluateFilterWithContext(object item, ColumnSearchBox filter)     // Single column evaluation
```

**Collection Context Management**:
```csharp
GetOrCreateCollectionContext(string bindingPath)    // Thread-safe caching per column
InvalidateCollectionContextCache()                  // Clear on data changes
```

**Select-All Column Support**:
```csharp
SetupSelectAllColumnHeaders()                       // Initialize checkboxes
OnSelectAllCheckboxClicked(DataGridColumn)          // Toggle all boolean values
CalculateSelectAllCheckboxState(DataGridColumn)     // Compute checkbox state (true/false/null)
ToggleSelectAllColumn(DataGridColumn)               // Preserves nulls, respects scope
GetItemsForSelectAllScope(SelectAllScope)           // AllItems/FilteredRows/SelectedRows
```

**Incremental Cache Updates**:
```csharp
UpdateColumnCachesForAddedItems(IList newItems)     // Add to cache without full reload
UpdateColumnCachesForRemovedItems(IList oldItems)   // Remove from cache
ClearAllCachedData()                                // Full cleanup with GC
```

**Events**:
- `CollectionChanged` - Raised when items added/removed
- `ItemsSourceChanged` - Raised when data source changes
- `CellValueChanged` - Raised after cell edits with old/new values

### ColumnSearchBox.cs - Per-Column Filter

**File**: `/Controls/ColumnSearchBox.cs`
**Purpose**: Individual column filtering with dual modes

**Filtering Modes**:

**1. Text-Based Filtering**:
- **Temporary Filter**: Created instantly for UI sync (250ms debounce for application)
- **Permanent Filter**: Created with Enter key (incremental OR logic)
- **Default Search Modes**: Contains, StartsWith, EndsWith, Equals

**2. Checkbox-Based Filtering**:
- **Cycle States**: Intermediate → Checked → Unchecked → [Intermediate]
- **Null Handling**: Cycles back to Intermediate if nulls exist
- **Lazy Null Detection**: Only determines null status on first cycle

**Key Properties**:
```csharp
SearchTemplateController SearchTemplateController   // Core filtering logic
DataGridColumn CurrentColumn                        // Associated column
SearchDataGrid SourceDataGrid                       // Parent grid
string SearchText                                   // Simple filter text
bool HasAdvancedFilter                              // Complex filter active
bool? FilterCheckboxState                           // Checkbox state
bool HasActiveFilter                                // Combined filter state
```

**Key Methods**:
```csharp
InitializeSearchTemplateController()       // Setup with lazy loading
CreateTemporaryTemplateImmediate()         // Instant template for UI sync
AddIncrementalContainsFilter()             // Convert temporary → permanent
CycleCheckboxStateForward()                // Advance checkbox state
ShowFilterPopup()                          // Open advanced filter dialog
```

### FilterPanel.cs - Active Filter Display

**File**: `/Controls/FilterPanel.cs`
**Purpose**: Display active filters as removable chips

**Key Features**:
- **Token Display**: Converts filters to visual tokens
- **Overflow Management**: Expand/collapse with gradient fade
- **Master Toggle**: Enable/disable all filters without removing
- **Operator Toggling**: Click to change AND ↔ OR

**Token Types**:
1. **FilterChipToken**: Individual filter component
2. **GroupLogicalConnectorToken**: AND/OR between columns
3. **TemplateLogicalConnectorToken**: AND/OR between templates within column
4. **RemovableValueToken**: Individual value in multi-value filters

**Commands**:
```csharp
ToggleFiltersCommand            // Toggle FiltersEnabled
RemoveTokenFilterCommand        // Remove entire column filter
ClearAllFiltersCommand          // Clear all filters
ToggleExpandCommand             // Expand/collapse overflow
RemoveValueFromTokenCommand     // Remove single value from multi-value filter
ToggleOperatorCommand           // Toggle AND/OR operators
```

**Events**:
```csharp
FiltersEnabledChanged           // Master toggle changed
FilterRemoved                   // Filter chip removed
ValueRemovedFromToken           // Individual value removed
OperatorToggled                 // AND/OR toggled (includes level)
ClearAllFiltersRequested        // Clear all clicked
```

### GridColumn.cs - Static Attached Properties

**File**: `/Controls/GridColumn.cs`
**Purpose**: Non-invasive column configuration via attached properties

**Attached Properties**:
```csharp
EnableRuleFiltering              // Enable/disable complex filtering (default: true)
AllowRuleValueFiltering          // Show/hide advanced filter button (default: true)
UseCheckBoxInSearchBox           // Force checkbox filtering mode (default: false)
FilterMemberPath                 // Explicit property path for filtering
ColumnDisplayName                // Display name for UI (falls back to Header)
DefaultSearchMode                // Default search type (Contains/StartsWith/EndsWith/Equals)
IsSelectAllColumn                // Enable select-all checkbox in header (default: false)
SelectAllScope                   // Scope for select-all (FilteredRows/SelectedRows/AllItems)
CustomSearchTemplate             // Custom SearchTemplate type (default: typeof(SearchTemplate))
```

### Behaviors (Attached Property Pattern)

**HorizontalScrollBehavior.cs**:
- **Attaches To**: `ScrollViewer`
- **Purpose**: Enables Shift + MouseWheel → horizontal scrolling
- **Features**: Win32 hook for native WM_MOUSEHWHEEL support

**TokenConfirmationBehavior.cs**:
- **Attaches To**: `FrameworkElement`
- **Purpose**: Two-step "confirm to delete" interaction
- **Flow**: First click → confirmation state → Mouse leave starts 1s timer → Second click executes

**TokenHoverBehavior.cs**:
- **Attaches To**: Filter token chips
- **Purpose**: Highlights corresponding column on hover
- **Integration**: Finds parent `FilterPanel`, executes `SetHoveredFilterCommand`

### Commands Architecture

**Pattern**: Partial class organization with `RelayCommand<T>`

**ContextMenuCommands.cs** (partial classes):
- `ColumnModifierCommands.cs` - Hide, sort, best-fit
- `CopyCommands.cs` - Clipboard operations (with/without headers)
- `FilterCommands.cs` - Clear filters
- `ExtensionVisibilityCommands.cs` - Show dialogs

**ContextMenuExtensions.cs**:
- Dynamic context menu generation based on right-click location
- Four context types: ColumnHeader, Cell, Row, GridBody
- Auto-wired in `SearchDataGrid` constructor

**Command Execution Flow**:
```
Right-click → PreviewMouseRightButtonDown
  ↓
Visual tree walk determines context type
  ↓
ContextMenuContext populated (grid, column, row data, cell value)
  ↓
Appropriate builder method creates ContextMenu
  ↓
CommandManager.InvalidateRequerySuggested()
  ↓
User clicks MenuItem
  ↓
RelayCommand<T>.Execute(parameter)
  ↓
Command logic runs (often delegates to control methods)
```

### Converters (18 Total)

**Boolean Converters** (4):
- `BooleanToIntConverter`, `InverseBooleanConverter`
- `BooleanToVisibilityCollapsedConverter`, `NullOrEmptyToBooleanConverter`

**Visibility Converters** (5):
- `NullToVisibilityConverter`, `HasValueToVisibilityConverter`
- `StringToVisibilityConverter`, `IntToVisibilityConverter`, `CountToVisibilityConverter`

**Search-Specific Converters** (6):
- `SearchTypeToIconConverter` - Maps SearchType → DrawingImage icons
- `DateIntervalToIconConverter` - Maps DateInterval → icons
- `SearchTypeToBetweenVisibilityConverter` - Shows second input for ranges
- `FilterInputTemplateToVisibilityConverter` - Dynamic template selection
- `EnumToStringConverter` - SearchType → display names via `SearchTypeRegistry`
- `ValueToCountConverter` - Shows occurrence counts like " (123)"

**Multi-Value Converters** (3):
- `ColumnNameToControlConverter` - Finds ColumnSearchBox by name+path
- `StringEqualityConverter` - Case-insensitive string comparison
- `GreaterThanConverter` - Numeric comparison

### Themes & Styling

**Generic.xaml Pattern** (WPF Convention):
```xml
/Themes/Generic.xaml (Root)
├── Resources/Icons/Icons.xaml (37 vector icons)
├── Controls/SearchDataGrid.xaml
│   ├── GenericSearchDataGridStyle
│   ├── GenericSearchDataGridColumnHeaderStyle (2-row layout with ColumnSearchBox)
│   └── GenericSearchDataGridRowHeaderStyle
├── Controls/ColumnSearchBox.xaml
│   └── GenericColumnSearchBoxStyle (dual input mode)
├── Controls/FilterPanel.xaml
│   └── GenericFilterPanelStyle (expandable with overflow)
├── Controls/ColumnFilterEditor.xaml
│   └── GenericColumnFilterEditorStyle (dynamic input templates)
└── Primitives/
    ├── SearchTextBox.xaml (dropdown with highlighting)
    └── NumericUpDown.xaml (spinner control)
```

**Visual Design System**:
- **Colors**: White/Light gray backgrounds, Blue accents (#0078D4), Red danger (#CC0000)
- **Fonts**: Segoe UI (text), Segoe MDL2 Assets (icons), Segoe Fluent Icons (sort arrows)
- **Spacing**: 2px multiples (2, 4, 6, 8)
- **States**: Default, Hover, Pressed, Focused, Disabled

**Template Parts** (PART_* naming):
- `PART_FilterPanel`, `PART_SearchTextBox`, `PART_FilterCheckBox`
- `PART_ClearFilterButton`, `PART_AdvancedFilterButton`, `PART_Popup`

---

## Sample Application

**File**: `/WWSearchDataGrid.Modern.SampleApp/`

### Architecture

**Entry Point**: `App.xaml` → `MainWindow.xaml`
**Pattern**: MVVM with CommunityToolkit.Mvvm source generators
**Global Error Handling**: `DispatcherUnhandledException` handler in `App.xaml.cs`

### MainViewModel.cs

**Observable Properties** (source generated):
```csharp
ObservableCollection<DataItem> Items         // Main data collection
DataItem SelectedItem                        // Selected row
int ItemCount                                // Total items
int ItemsToGenerate                          // Generation count (default: 5000)
string CurrentThemeName                      // Active theme name
```

**Commands** (source generated):
```csharp
PopulateDataCommand          // Generate sample data (parallel)
AddItemCommand               // Add single item
RemoveItemCommand            // Remove last item
ClearDataCommand             // Clear all with aggressive GC
ToggleThemeCommand           // Switch Generic ↔ Custom theme
```

**Data Generation**:
- **Method**: `Parallel.For` for multi-threaded generation
- **Performance**: 5000+ items in ~6 seconds (optimized from ~2 minutes)
- **Null Distribution**: 10-25% nulls per field

### DataItem.cs - Comprehensive Type Matrix

**25+ Property Types**:
```csharp
// Boolean (2): bool, bool?
// Integer (8): int, int?, long, long?, short, short?, byte, byte?
// Floating-point (6): float, float?, double, double?, decimal, decimal?
// Text (3): string, char, char?
// DateTime (4): DateTime, DateTime?, TimeSpan, TimeSpan?
// Enum (2): Priority, Priority?
```

**Demonstrates**: Filtering for ALL major .NET data types including nullables

### MainWindow.xaml - Usage Examples

**Column Configuration Patterns**:

**1. Text Column with Custom Search Mode**:
```xml
<DataGridTextColumn
    sdg:GridColumn.FilterMemberPath="CustomerName"
    sdg:GridColumn.DefaultSearchMode="StartsWith"
    sdg:GridColumn.EnableRuleFiltering="False"
    sdg:GridColumn.ColumnDisplayName="Custom Display Name" />
```

**2. Boolean Column with Select All**:
```xml
<DataGridCheckBoxColumn
    sdg:GridColumn.UseCheckBoxInSearchBox="True"
    sdg:GridColumn.IsSelectAllColumn="True"
    sdg:GridColumn.SelectAllScope="SelectedRows" />
```

**3. Numeric/DateTime/Enum Columns**:
- Automatic type detection
- Custom search modes supported
- Nullable type handling

### ThemeManager.cs - Runtime Theme Switching

**Pattern**: Singleton with resource dictionary management

**Theme Types**:
- **Generic**: Default library styles (single Generic.xaml)
- **Custom**: Sample app overrides (8 style files)

**Key Methods**:
```csharp
void SwitchTheme(ThemeType targetTheme)   // Runtime theme change
void RemoveThemeResources()               // Cleanup old theme
void AddThemeResources(ThemeType)         // Load new theme
```

**Demonstrates**: Production-ready theme management without restart

---

## Data Flow Architecture

### Complete Filter Flow: User Input → Data Filtering

```
USER INPUT
  ↓
[Phase 1: WPF Input Capture]
TextBox.TextChanged → OnSearchTextBoxTextChanged()
  ↓
SearchText property updated
  ↓
OnSearchTextChanged()
  ├─→ CreateTemporaryTemplateImmediate() [Instant for UI state]
  └─→ StartOrResetChangeTimer() [250ms debounce]
  ↓
[Phase 2: Core Template Management]
SearchTemplate created with:
  - ColumnDataType
  - SearchType (mapped from DefaultSearchMode)
  - SelectedValue = SearchText
  ↓
SubscribeToTemplateChanges()
  ↓
SearchTemplate.PropertyChanged events
  ↓
SearchTemplateController.OnSearchTemplatePropertyChanged()
  ↓
InvokeAutoApplyFilter() [Debounced]
  ↓
[Phase 3: Expression Compilation]
UpdateFilterExpression()
  ↓
DetermineTargetColumnType()
  ↓
FilterExpressionBuilder.BuildFilterExpression()
  ↓
FOR EACH SearchTemplateGroup:
  FOR EACH SearchTemplate:
    template.BuildExpression(targetType)
      → Creates: Expression<Func<object, bool>>
    Compose with operators (AND/OR)
  ↓
Expression.Compile()
  ↓
RESULT: Func<object, bool> FilterExpression
  ↓
[Phase 4: Filter Application]
SearchDataGrid.FilterItemsSource()
  ↓
Get active filters: DataColumns.Where(HasCustomExpression)
  ↓
Determine async/sync based on dataset size:
  - >10k items → async
  - >5k items with collection context → async
  - Otherwise → sync
  ↓
Items.Filter = item => EvaluateUnifiedFilter(item, activeFilters)
  ↓
FOR EACH active filter:
  EvaluateFilterWithContext(item, filter)
    ↓
  Get column value: ReflectionHelper.GetPropValue(item, bindingPath)
    ↓
  IF requires collection context (TopN, AboveAverage, etc.):
    GetOrCreateCollectionContext(bindingPath) [Cached]
    SearchTemplateController.EvaluateWithCollectionContext(value, context)
  ELSE:
    SearchTemplateController.FilterExpression(value)
    ↓
  Apply logical operator (AND/OR)
  Short-circuit if possible
  ↓
[Phase 5: Core Evaluation]
SearchEngine.EvaluateCondition(value, condition, [context])
  ↓
SearchEvaluatorFactory.GetEvaluator(searchType)
  ↓
evaluator.Evaluate(value, condition, [context])
  ↓
RESULT: bool (match/no match)
  ↓
[Phase 6: UI Update]
CollectionView.Refresh() (WPF internal)
  ↓
UpdateFilterPanel()
  ↓
FilterPanel.UpdateActiveFilters()
  ↓
FilterTokenConverter.ConvertToTokens()
  ↓
UI UPDATES:
  - DataGrid rows filtered
  - Filter chips displayed
  - HasActiveFilter indicators updated
```

### Value Loading Flow (Lazy + Cached)

```
INITIAL SETUP
ColumnSearchBox.InitializeSearchTemplateController()
  ↓
SearchTemplateController.SetupColumnDataLazy()
  ↓
Sets: _columnValuesProvider = GetColumnValuesFromDataGrid
      _cacheKey = hash(ColumnName + provider + timestamp)
  ↓
NO DATA LOADED - Deferred until needed
  ↓
FIRST ACCESS (User opens filter editor or cycles checkbox)
EnsureColumnValuesLoaded()
  ↓
ColumnValueCacheManager.Instance.GetOrCreateColumnValues(cacheKey, provider)
  ↓
IF cache hit: Return ReadOnlyColumnValues
  ↓
ELSE: Execute _columnValuesProvider()
  ↓
GetColumnValuesFromDataGrid()
  ↓
FOR EACH item in OriginalItemsSource:
  ReflectionHelper.GetPropValue(item, BindingPath)
  ↓
Build ReadOnlyColumnValues:
  - UniqueValues: HashSet<object>
  - ValueCounts: Dictionary<object, int>
  - ContainsNullValues: bool
  - DataType: ColumnDataType
  ↓
Cache in ColumnValueCacheManager
  ↓
Return to SearchTemplateController
  ↓
INCREMENTAL UPDATES
Collection.Add event → UpdateColumnCachesForAddedItems()
  ↓
TryAddColumnValues() → Increment counts or add new values
  ↓
IF successful: Update cache
ELSE: Fall back to RefreshColumnValues()
```

### Event Propagation Chains

**Text Search Chain**:
```
TextBox.TextChanged → OnSearchTextBoxTextChanged → SearchText setter
  → OnSearchTextChanged → CreateTemporaryTemplateImmediate
  → UpdateHasActiveFilterState → StartOrResetChangeTimer → (250ms)
  → OnChangeTimerElapsed → UpdateSimpleFilter → UpdateFilterExpression
  → FilterItemsSource → UpdateFilterPanel
```

**Checkbox Cycle Chain**:
```
CheckBox.PreviewMouseDown → OnCheckboxPreviewMouseDown
  → CycleCheckboxStateForward → EnsureNullStatusDetermined
  → SetCheckboxCycleState → ApplyCheckboxCycleFilter
  → ApplyCheckboxBooleanFilter/IsNullFilter → UpdateFilterExpression
  → FilterItemsSource → UpdateFilterPanel
```

**Advanced Filter Dialog Chain**:
```
Filter icon click → ShowFilterPopup → ColumnFilterEditor loaded
  → User modifies templates → SearchTemplate.PropertyChanged
  → OnSearchTemplatePropertyChanged → InvokeAutoApplyFilter
  → (Popup closed) → OnFiltersApplied → UpdateHasActiveFilterState
  → UpdateFilterPanel
```

**Filter Panel Operator Toggle Chain**:
```
Operator token click → FilterPanel.ToggleOperatorCommand
  → ExecuteToggleOperator → OperatorToggled event
  → SearchDataGrid.OnOperatorToggled → Update template/group operator
  → UpdateFilterExpression → FilterItemsSource → UpdateFilterPanel
```

### Cross-Project Integration

**Dependency Flow**:
```
WWSearchDataGrid.Modern.WPF (UI Layer)
  ↓ References
WWSearchDataGrid.Modern.Core (Business Logic)
  ↓ References
.NET Standard 2.0 BCL + System.Text.Json
```

**Integration Patterns**:

1. **Direct Property Binding**: `ColumnSearchBox.SearchTemplateController` → Core model
2. **Event Subscription**: `SearchTemplate.PropertyChanged` → WPF handlers
3. **Delegate Injection**: `SetupColumnDataLazy(Func<IEnumerable<object>>)` → Lazy loading
4. **Expression Compilation**: Core returns `Func<object, bool>` → WPF applies to `CollectionView.Filter`
5. **Reflection Abstraction**: WPF provides binding paths → Core uses `ReflectionHelper`

---

## Design Patterns

### Creational Patterns

**Singleton**:
- `SearchEvaluatorFactory.Instance` - Evaluator registry
- `ColumnValueCacheManager.Instance` - Global value cache
- `ThemeManager.Instance` - Theme management

**Factory Method**:
- `SearchEvaluatorFactory.GetEvaluator(SearchType)` - Returns appropriate evaluator
- `FilterExpressionBuilder` - Creates compiled expressions

### Structural Patterns

**Façade**:
- `SearchTemplateController` - Simplifies complex filtering subsystem
- `SearchEngine` - Unified interface to evaluation logic

**Composite**:
- `SearchTemplateGroup` contains `SearchTemplate`s
- Supports hierarchical filter structures with logical operators

### Behavioral Patterns

**Strategy**:
- `ISearchEvaluator` with 26+ implementations
- Algorithm selection based on `SearchType` at runtime

**Observer**:
- `ObservableObject` base class for INotifyPropertyChanged
- `AutoApplyFilter` event for filter changes
- Collection change monitoring for incremental updates

**Command**:
- `RelayCommand<T>` for all user actions
- Enables MVVM pattern with CanExecute logic

**Template Method**:
- `SearchEvaluatorBase` defines evaluation skeleton
- `SearchTemplate.BuildExpression()` delegates to specialized builders

**Visitor**:
- `ParameterRebinder` (ExpressionVisitor) for expression tree transformation

### Architectural Patterns

**MVVM** (Model-View-ViewModel):
- Models: `SearchTemplate`, `SearchCondition`, `DataItem`
- ViewModels: `SearchTemplateController`, `MainViewModel`
- Views: XAML files with data binding

**Lazy Initialization**:
- `CollectionContext` with lazy-computed statistics
- `SearchTemplateController.ColumnValues` with lazy loading

**Repository/Cache**:
- `ColumnValueCacheManager` - Centralized cache
- `ReadOnlyColumnValues` - Immutable cached data

**Dependency Injection** (Manual):
- `SearchTemplateController` uses `FilterExpressionBuilder`
- Services passed via constructor or property injection

---

## Performance Optimizations

### Key Optimization Strategies

1. **Expression Compilation** (`SearchTemplateController.UpdateFilterExpression()`):
   - Compiles LINQ expressions to IL code
   - One-time compilation, reused for all rows
   - Native execution speed

2. **Lazy Loading** (`EnsureColumnValuesLoaded()`):
   - Column values loaded only when filter editor opened
   - Defers expensive reflection operations
   - Reduces initial load time

3. **Collection Context Caching** (`GetOrCreateCollectionContext()`):
   - Per-column statistics cached
   - Shared across all evaluations
   - Thread-safe with lock
   - Disposed after filtering

4. **Incremental Cache Updates** (`TryAddColumnValues()`, `TryRemoveColumnValues()`):
   - Add/remove single values without full reload
   - Maintains counts efficiently
   - Falls back to full refresh if needed

5. **Debounce Timer** (250ms for text search):
   - Prevents excessive filtering during typing
   - Temporary template created instantly for UI state
   - Actual filtering delayed

6. **Short-Circuit Evaluation** (`EvaluateUnifiedFilter()`):
   - Stops on first AND failure
   - Skips unnecessary column evaluations
   - Significant speedup for complex filters

7. **Async Filtering** (`FilterItemsSource()`):
   - Background thread for large datasets (>10k items)
   - Cancellable via `CancellationTokenSource`
   - Progress reporting support

8. **Null Status Deferral** (`EnsureNullStatusDetermined()`):
   - Only determines null presence when explicitly needed (checkbox cycle)
   - Avoids cache load for simple text operations

9. **Single Reflection Pass** (`CollectionContext._extractedValues`):
   - Extracts values once, shared by all statistical operations
   - Eliminates redundant reflection calls

10. **Virtualization** (WPF DataGrid):
    - Row virtualization with Recycling mode
    - SearchTextBox dropdown virtualization
    - Handles 5000+ items efficiently

### Performance Benchmarks (from CLAUDE.md)

- **Initial Data Generation**: 5000+ items in ~6 seconds (optimized from ~2 minutes)
- **Filtering**: Expression-compiled filters execute at native speed
- **Cache Loading**: Acceptable latency for user interactions (checkbox cycle)

---

## Quick Reference

### Common Development Scenarios

**Adding New Search Type**:
1. Add enum value to `SearchType` (Core/Data/Enums/SearchType.cs)
2. Create evaluator implementing `ISearchEvaluator` (Core/Search/Evaluators/)
3. Register in `SearchEvaluatorFactory` constructor
4. Add metadata to `SearchTypeRegistry.Registry`
5. Add icon to `Resources/Icons/Icons.xaml` (optional)
6. Update `SearchTypeToIconConverter` mapping (optional)

**Modifying Filter UI**:
- Control logic: `/WPF/Controls/[ControlName].cs`
- Visual appearance: `/WPF/Themes/Controls/[ControlName].xaml`
- No code-behind in XAML files

**Performance Tuning**:
- Expression compilation: `SearchTemplateController.UpdateFilterExpression()`
- Cache management: `ColumnValueCacheManager` class
- Async thresholds: `SearchDataGrid.ShouldUseAsyncFiltering()`

**Custom Styling**:
- Override `Generic[ControlName]Style` in application resources
- Use `BasedOn="{StaticResource Generic...}"` for incremental changes

### Key File Paths

**Core Logic**:
- Search engine: `Core/Search/Core/SearchEngine.cs`
- Controller: `Core/Data/Models/Search/SearchTemplateController.cs`
- Template: `Core/Data/Models/Search/SearchTemplate.cs`
- Evaluators: `Core/Search/Evaluators/`

**WPF Controls**:
- Main grid: `WPF/Controls/SearchDataGrid.cs`
- Column filter: `WPF/Controls/ColumnSearchBox.cs`
- Filter dialog: `WPF/Controls/ColumnFilterEditor.cs`
- Filter panel: `WPF/Controls/FilterPanel.cs`

**Themes**:
- Main merger: `WPF/Themes/Generic.xaml`
- Control templates: `WPF/Themes/Controls/`
- Icons: `WPF/Resources/Icons/Icons.xaml`

**Sample App**:
- Main view: `SampleApp/Views/MainWindow.xaml`
- ViewModel: `SampleApp/ViewModels/MainViewModel.cs`
- Data model: `SampleApp/Models/DataItem.cs`

### Build & Run

```bash
# Build entire solution
dotnet build WWSearchDataGrid.Modern.sln --configuration Release

# Run sample application
dotnet run --project WWSearchDataGrid.Modern.SampleApp

# Build specific project
dotnet build WWSearchDataGrid.Modern.Core/WWSearchDataGrid.Modern.Core.csproj
dotnet build WWSearchDataGrid.Modern.WPF/WWSearchDataGrid.Modern.WPF.csproj
```

---

## Summary

WWSearchDataGrid.Modern is a **professionally architected WPF data grid library** featuring:

- **27 search types** with extensible Strategy pattern
- **Expression compilation** for native-speed filtering
- **Lazy loading & caching** for optimal performance
- **Complete MVVM compliance** with clean separation of concerns
- **26+ evaluator implementations** with collection-aware statistical filters
- **18 value converters** for comprehensive data binding
- **37 vector icons** for consistent visual language
- **Production-ready patterns**: Error handling, memory management, theme switching
- **Comprehensive sample app** demonstrating all features with 25+ data types

**Architecture Strengths**:
- Extensible (Strategy + Factory patterns)
- Performant (Expression compilation, caching, async operations)
- Testable (Core has no UI dependencies)
- Maintainable (Clear responsibilities, documented patterns)
- Professional (WPF conventions, Generic.xaml, attached properties)

This document serves as a complete architectural reference for developers and AI agents working with the WWSearchDataGrid.Modern codebase.
