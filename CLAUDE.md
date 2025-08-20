# WWSearchDataGrid.Modern - Project Structure & Agent Navigation Guide

## Overview
WWSearchDataGrid.Modern is a comprehensive WPF data grid library with advanced search and filtering capabilities. This document provides a complete guide for AI agents to efficiently navigate and understand the codebase without reading the entire project.

## Solution Structure

```
WWSearchDataGrid.Modern.sln
├── WWSearchDataGrid.Modern.Core/         (Core logic - .NET Standard 2.0)
├── WWSearchDataGrid.Modern.WPF/          (WPF controls - .NET 9.0-windows)
├── WWSearchDataGrid.Modern.SampleApp/    (Demo application - .NET 9.0-windows)
└── Samples/                              (Solution folder)
```


## Core Architecture (WWSearchDataGrid.Modern.Core)

### Key Classes & Their Responsibilities

#### 🎯 **SearchEngine.cs** (`/Search/SearchEngine.cs`)
- **Purpose**: Static class providing core filter evaluation logic
- **Key Method**: `EvaluateCondition(object columnValue, SearchCondition searchCondition)`
- **Handles**: 25+ search types (Contains, Between, DateInterval, IsEmpty, etc.)
- **When to modify**: Adding new search types or changing comparison logic

#### 🎯 **SearchTemplateController.cs** (`/Data/Models/Search/SearchTemplateController.cs`)
- **Purpose**: Central coordinator managing filter expressions and search groups
- **Key Methods**: 
  - `UpdateFilterExpression()` - Compiles filter expressions for performance
  - `GetFilterDisplayText()` - Generates human-readable filter descriptions
- **Manages**: Search groups, templates, logical operators (AND/OR)
- **When to modify**: Complex filtering logic, expression compilation

#### 🎯 **SearchTemplate.cs** (`/Data/Models/Search/SearchTemplate.cs`)
- **Purpose**: Configurable filter template for individual search criteria
- **Key Methods**: 
  - `BuildExpression(Type targetType)` - Creates LINQ expressions
  - `LoadAvailableValues(HashSet<object> columnValues)` - Populates dropdown values
- **Handles**: Value selection, data type detection, expression building

#### 🎯 **SearchCondition.cs** (`/Data/Models/Search/SearchCondition.cs`)
- **Purpose**: Model representing individual search criteria
- **Contains**: SearchType, values, data type information
- **Used by**: SearchEngine for condition evaluation

### Search Types Supported
```csharp
Contains, DoesNotContain, StartsWith, EndsWith, Equals, NotEquals,
LessThan, LessThanOrEqualTo, GreaterThan, GreaterThanOrEqualTo,
Between, NotBetween, IsEmpty, IsNotEmpty, IsAnyOf, IsNoneOf,
DateInterval, BetweenDates, IsOnAnyOfDates, Yesterday, Today, Tomorrow,
TopN, BottomN, AboveAverage, BelowAverage, Unique, Duplicate
```

## WPF Implementation (WWSearchDataGrid.Modern.WPF)

### 📁 **Project Structure** (WPF Best Practices)

#### `/Controls/` - Custom Controls (Code-Only)
**⚠️ NO XAML FILES - Only .cs files**

- **`SearchDataGrid.cs`** - Main control extending DataGrid
  - **Features**: Per-column filtering, global filtering, filter panel integration
  - **Key Properties**: `FilteringMode`, `FilterPanel`, `OriginalItemsSource`
  - **Key Methods**: `ApplyFilters()`, `UpdateFilterPanel()`, `GetActiveColumnFilters()`

- **`ColumnSearchBox.cs`** - Individual column filter control
  - **Features**: Simple text search, advanced filter dialog
  - **Key Properties**: `SearchText`, `CurrentColumn`, `SourceDataGrid`
  - **Manages**: SearchTemplateController instances per column

- **`ColumnFilterEditor.cs`** - Complex multi-criteria filter UI
  - **Features**: Multiple search groups, drag-drop reordering, value selection
  - **Key Properties**: `SearchTemplateController`, `ValueSelectionSummary`
  - **Commands**: Add/Remove search groups and templates

- **`FilterPanel.cs`** - Active filter chips display
  - **Features**: Toggle filters, remove individual filters, edit filters
  - **Key Properties**: `ActiveFilters`, `FiltersEnabled`, `HasActiveFilters`
  - **Events**: Filter removal, clear all, edit requests

- **`NumericUpDown.cs`** - Custom numeric input control
- **`DataGridFilterEditor.cs`** - Multi-column filter editing dialog

#### `/Themes/Controls/` - XAML Styling (Presentation Only)
**⚠️ Only styling XAML - No code-behind**
- Corresponding .xaml files for each control in `/Controls/`
- Defines default styles and templates using WPF Generic.xaml pattern

#### `/Themes/Generic.xaml` - Main Theme Merger
- Standard WPF convention for custom control libraries
- Merges all control-specific theme files

#### `/Templates/` - Reusable Components
- **`FilterValueTemplates.xaml`** - Input templates for different data types
- **`FilterValueTemplateSelector.cs`** - Logic for template selection

#### `/Converters/` - Value Converters
- IValueConverter implementations for data binding

#### `/Resources/` - Static Assets
- Icons, images, and other static resources

### 🔄 **Data Flow Architecture**

```
User Input → columnSearchBox → SearchTemplateController → SearchEngine → FilterExpression → SearchDataGrid
     ↓              ↓                    ↓                    ↓              ↓              ↓
SearchText    Manages State     Builds Expressions    Evaluates Items   Compiles Logic   Filters Data
```

## Sample Application (WWSearchDataGrid.Modern.SampleApp)

### Key Files for Understanding Usage

- **`MainViewModel.cs`** - Demonstrates data binding and commands
- **`MainWindow.xaml`** - Shows SearchDataGrid integration
- **`DataItem.cs`** - Sample model class with various data types

### Sample Data Features
- 5000+ generated items by default
- Multiple data types (string, int, DateTime, decimal, enum)
- Demonstrates filtering performance

## 🛠️ **Common Development Scenarios**

### Adding New Search Types
1. **Add to enum**: `SearchType` in Core project
2. **Update evaluation**: `SearchEngine.EvaluateCondition()` method
3. **Add display text**: `SearchTemplateController.GetSearchTypeDisplayText()`

### Modifying Filter Logic
- **Core evaluation**: `SearchEngine.cs:EvaluateCondition`
- **Expression building**: `SearchTemplate.cs:BuildExpression`
- **UI display**: Template files in `/Themes/Controls/`

### Performance Optimization
- **Expression compilation**: `SearchTemplateController.UpdateFilterExpression`
- **Value caching**: Built-in caching for column values
- **Async loading**: `LoadColumnValuesAsync()` methods

### UI Customization
- **Control appearance**: Modify XAML in `/Themes/Controls/`
- **Templates**: Update `/Templates/FilterValueTemplates.xaml`
- **Styling**: Override styles in consuming applications

## 🎯 **Key Design Patterns**

- **MVVM**: ObservableObject base class throughout
- **Template Pattern**: SearchTemplate for extensible filtering
- **Expression Trees**: Dynamic compilation for performance
- **Command Pattern**: RelayCommand for UI interactions
- **Custom Controls**: WPF Generic.xaml pattern for reusability

## 📦 **Dependencies**

### Core (.NET Standard 2.0)
- Newtonsoft.Json
- System.Text.Json

### WPF (.NET 9.0-windows)
- Microsoft.Xaml.Behaviors.Wpf

### Sample App (.NET 9.0-windows)
- CommunityToolkit.Mvvm

## 🚨 **Important File Paths for Common Issues**

### Filter Logic Problems
- **Core evaluation**: `WWSearchDataGrid.Modern.Core/Search/SearchEngine.cs`
- **Expression building**: `WWSearchDataGrid.Modern.Core/Data/Models/Search/SearchTemplate.cs`
- **Controller logic**: `WWSearchDataGrid.Modern.Core/Data/Models/Search/SearchTemplateController.cs`

### UI Issues
- **Main DataGrid**: `WWSearchDataGrid.Modern.WPF/Controls/SearchDataGrid.cs`
- **Column filters**: `WWSearchDataGrid.Modern.WPF/Controls/ColumnSearchBox.cs`
- **Advanced dialog**: `WWSearchDataGrid.Modern.WPF/Controls/ColumnFilterEditor.cs`
- **Filter chips**: `WWSearchDataGrid.Modern.WPF/Controls/FilterPanel.cs`

### Styling Problems
- **Default styles**: `WWSearchDataGrid.Modern.WPF/Themes/Generic.xaml`
- **Individual controls**: `WWSearchDataGrid.Modern.WPF/Themes/Controls/*.xaml`
- **Templates**: `WWSearchDataGrid.Modern.WPF/Templates/FilterValueTemplates.xaml`

### Performance Issues
- **Expression compilation**: `SearchTemplateController.UpdateFilterExpression()`
- **Value caching**: Methods with `LoadColumnValuesAsync` or `Cache` in name
- **Collection filtering**: `SearchDataGrid.ApplyFilters()` method

## 📋 **Feature Checklist**

### ✅ **Implemented Features**
- Per-column and global filtering modes
- 25+ search types with extensible architecture
- Advanced multi-criteria filtering with logical operators
- Real-time filter chips with enable/disable toggle
- Drag-drop filter reordering
- Async value loading and caching
- Expression tree compilation for performance
- Date interval filtering
- Custom search templates
- MVVM pattern throughout

### 🎯 **Architecture Benefits**
- **Extensible**: Easy to add new search types
- **Performant**: Expression compilation and caching
- **Customizable**: WPF Generic.xaml pattern
- **Maintainable**: Clear separation of concerns
- **Professional**: Behaves like built-in WPF controls

This README serves as a navigation guide for AI agents to quickly locate relevant code sections without reading the entire project. Use the file paths and class descriptions to efficiently target specific functionality areas.