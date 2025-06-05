# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

Build the entire solution:
```bash
dotnet build WWSearchDataGrid.Modern.sln
```

Build specific projects:
```bash
dotnet build WWSearchDataGrid.Modern.Core/WWSearchDataGrid.Modern.Core.csproj
dotnet build WWSearchDataGrid.Modern.WPF/WWSearchDataGrid.Modern.WPF.csproj
dotnet build WWSearchDataGrid.Modern.SampleApp/WWSearchDataGrid.Modern.SampleApp.csproj
```

Run the sample application:
```bash
dotnet run --project WWSearchDataGrid.Modern.SampleApp/WWSearchDataGrid.Modern.SampleApp.csproj
```

## Architecture Overview

This is a modern WPF data grid library with advanced search and filtering capabilities, consisting of three main projects:

### Core Architecture (Core Project)
- **SearchEngine**: Static class providing expression evaluation logic for various search types (Contains, Between, DateInterval, etc.)
- **SearchTemplateController**: Central coordinator managing search groups, templates, and filter expressions 
- **SearchCondition**: Model representing individual search criteria with type-specific comparison logic
- **SearchTemplate**: Configurable filter template supporting complex search operations

### WPF Implementation (WPF Project) 
- **SearchDataGrid**: Main control extending DataGrid with filtering capabilities, supports both per-column and global filtering modes
- **SearchControl**: Individual column filter control with simple text search and advanced filter dialog
- **AdvancedFilterControl**: Complex UI for building multi-criteria filters with logical operators

### Data Flow
1. SearchControl captures user input and manages SearchTemplateController instances
2. SearchTemplateController builds filter expressions using SearchTemplate configurations
3. SearchEngine evaluates individual conditions against data items
4. SearchDataGrid applies the compiled filter expressions to ItemsSource

### Key Design Patterns
- **MVVM**: Uses ObservableObject base class and data binding throughout
- **Template Pattern**: SearchTemplate provides extensible filter configuration
- **Expression Trees**: Dynamic filter compilation for performance
- **Command Pattern**: RelayCommand for UI interactions

## Target Frameworks
- Core: .NET Standard 2.0 (for maximum compatibility)
- WPF: .NET 9.0-windows
- Sample App: .NET 9.0-windows

## Key Dependencies
- Core: Newtonsoft.Json, System.Text.Json
- WPF: Microsoft.Xaml.Behaviors.Wpf  
- Sample: CommunityToolkit.Mvvm

## Common Development Scenarios
- Filter logic modifications should be made in SearchEngine.cs:EvaluateCondition
- New search types require updates to SearchType enum and corresponding evaluation logic
- UI customization primarily involves XAML templates in Themes/ directory
- Performance-critical operations use expression compilation in SearchTemplateController.UpdateFilterExpression