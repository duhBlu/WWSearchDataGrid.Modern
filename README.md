# WWSearchDataGrid.Modern

A WPF DataGrid control with built-in per-column search, advanced multi-criteria filtering, filter panel, column chooser, and context menus. Drop-in replacement for the standard `DataGrid`.

## Quick Start

```xml
<!-- Add the namespace -->
xmlns:sdg="clr-namespace:WWSearchDataGrid.Modern.WPF;assembly=WWSearchDataGrid.Modern.WPF"

<!-- Replace <DataGrid> with <sdg:SearchDataGrid> -->
<sdg:SearchDataGrid ItemsSource="{Binding Items}"
                    EnableRuleFiltering="True"
                    IsColumnChooserEnabled="True">
    <sdg:SearchDataGrid.Columns>
        <DataGridTextColumn Header="Name"
                            Binding="{Binding Name}"
                            sdg:GridColumn.FilterMemberPath="Name" />
    </sdg:SearchDataGrid.Columns>
</sdg:SearchDataGrid>
```

That's it. Every column automatically gets a search box. Type to filter.

## Features

- **Per-column text search** with configurable search modes (Contains, StartsWith, EndsWith, Equals)
- **Advanced rule filtering** with 25+ search types (Between, TopN, AboveAverage, IsLike, DateInterval, etc.)
- **Filter panel** showing active filters as chips with enable/disable toggle
- **Column chooser** for showing/hiding columns with drag-drop reordering
- **Checkbox columns** with three-state cycling (true/false/null) and select-all header checkbox
- **Built-in context menus** for sorting, copying, best-fit sizing, and filter management
- **Auto-size columns** that dynamically fit visible content during scrolling
- **Cell value change tracking** with events for edit detection
- **Expression tree compilation** for high-performance filtering on large datasets
- **Full theming support** via WPF Generic.xaml pattern

## Project Structure

```
WWSearchDataGrid.Modern.sln
  WWSearchDataGrid.Modern.Core/     .NET Standard 2.0 - search engine, evaluators, models
  WWSearchDataGrid.Modern.WPF/      .NET 9.0-windows  - controls, themes, converters
  WWSearchDataGrid.Modern.SampleApp/ .NET 9.0-windows  - demo application
```

## Documentation

- **[Getting Started](docs/getting-started.md)** - Setup, prerequisites, common scenarios
- **[API Reference](docs/api-reference.md)** - Properties, attached properties, events
- **[CHANGELOG](CHANGELOG.md)** - Version history

## Requirements

- .NET 9.0 SDK (WPF project) or .NET Standard 2.0+ (Core project)
- Windows (WPF is Windows-only)

## License

See LICENSE file for details.
