# WWControls

A modern WPF control library centered on `SearchDataGrid` — a drop-in `DataGrid` replacement with built-in filtering, grouping, summaries, pinned columns, and rich editing — plus a family of standalone editors and primitive controls. Everything ships through a single XAML namespace and one swappable theme assembly.

## Quick Start

```xml
<!-- One xmlns reaches every control in the package -->
xmlns:ww="http://schemas.wwcontrols.com/wpf"

<!-- Replace <DataGrid> with <ww:SearchDataGrid> -->
<ww:SearchDataGrid ItemsSource="{Binding Items}"
                   AutoGenerateColumns="False"
                   IsColumnChooserEnabled="True">
    <ww:SearchDataGrid.GridColumns>
        <ww:GridColumn FieldName="Name" Header="Name" />
        <ww:GridColumn FieldName="Status" Header="Status" DefaultSearchType="Equals" />
        <ww:GridColumn FieldName="Amount" Header="Amount" DisplayStringFormat="C2" />
        <ww:GridColumn FieldName="OrderDate" Header="Date" DisplayStringFormat="MM/dd/yyyy" />
    </ww:SearchDataGrid.GridColumns>
</ww:SearchDataGrid>
```

That's it. `FieldName` auto-generates the Binding, SortMemberPath, and FilterMemberPath. The filter row, header context menus, and grouping panel come free.

The editors also work standalone, outside any grid:

```xml
<ww:WWTextBox  Value="{Binding Phone, Mode=TwoWay}" Mask="(000) 000-0000" />
<ww:WWDatePicker  Value="{Binding ShipDate, Mode=TwoWay}" />
<ww:WWComboBox ItemsSource="{Binding Statuses}" DisplayMemberPath="Value"
                SelectedValuePath="Key" Value="{Binding Status, Mode=TwoWay}" />
```

## Features

- **Filter row** with per-column, data-type-matched editors, plus advanced rule filtering (28 search types: Between, TopN, AboveAverage, IsLike, DateInterval, …) and a cross-column filter editor dialog
- **Filter summary panel** showing active filters as removable chips with AND/OR toggling and a master enable/disable
- **Multi-level grouping** with date/alphabetical bucketing, group headers, sticky headers, and drag-to-group panel
- **Summaries**: total row, pinned totals strip, and per-group footers (Count/Sum/Min/Max/Average) with runtime picker menus
- **Editing**: click-to-edit policies, full-row edit strip, inline edit forms, new-item row at top or bottom, data-annotation validation with severity badges
- **Editor controls** (`WWTextBox`, `WWComboBox`, `WWDatePicker`, `WWNumericUpDown`, `WWCheckBox`) usable in cells or standalone, with keystroke-validated input masking (simple, numeric, date/time engines)
- **Columns**: pinning (left/right), best-fit sizing, column chooser with drag-drop reordering, smart auto-generation from data annotations
- **Built-in context menus** on every surface: headers, cells, group panel, summaries
- **Expression-tree-compiled filtering** for native-speed evaluation on large datasets
- **Full theming** via typed resource keys in a swappable satellite theme assembly

## Solution Structure

```
WWControls.sln
  WWControls.Core/                netstandard2.0    filtering engine, mask engines, models (no UI)
  WWControls.Wpf.Primitives/      net9.0-windows    primitive controls, icon system, base theme keys
  WWControls.Wpf.Editors/         net9.0-windows    editor controls + EditSettings adapters
  WWControls.Wpf.Grid/            net9.0-windows    SearchDataGrid, columns, filtering, grouping, summaries
  WWControls.Wpf.Themes.Default/  net9.0-windows    all default templates and brushes (satellite theme)
  WWControls.SampleApp.Grid/      net9.0-windows    runnable grid sample catalog (~25 samples)
  WWControls.SampleApp.Editors/   net9.0-windows    runnable standalone-editor sample catalog
  WWControls.Core.Tests/          net9.0            xunit tests for the Core layer
```

Dependencies flow strictly downward: Core → Primitives → Editors → Grid. Reference `WWControls.Wpf.Grid` and `WWControls.Wpf.Themes.Default` and the rest comes transitively. The default theme applies automatically; merge `/WWControls.Wpf.Themes.Default;component/Theme.xaml` into App.xaml only when overriding styles.

### XAML namespace note

All assemblies map their public namespaces into `http://schemas.wwcontrols.com/wpf` via `[XmlnsDefinition]`, so consumer XAML uses one prefix (`ww`) no matter which assembly defines the type. For the same reason, most types in `WWControls.Wpf.Grid` deliberately live in the CLR namespace `WWControls.Wpf` (with `SearchDataGrid` in `WWControls.Wpf.Grids`) rather than mirroring their folder paths — when contributing there, match the namespace of neighboring files, not the folder.

## Exploring

Run **WWControls.SampleApp.Grid** — a launcher window catalogs the grid samples by category (Columns, Editing, Filtering, FilterRow, Grouping, Summaries, DataBinding, Usability, AnimationPerformance). A good first stop: *Column Configuration* (live GridColumn property inspector). For the editor controls on their own — no grid — run **WWControls.SampleApp.Editors**, which catalogs the five standalone editors (WWTextBox / WWNumericUpDown / WWComboBox / WWDatePicker / WWCheckBox) the same way.

## Documentation

- **[Getting Started](docs/getting-started.md)** - Setup, prerequisites, common scenarios
- **[API Reference](docs/api-reference.md)** - Properties, attached properties, events
- **[CHANGELOG](CHANGELOG.md)** - Version history

## Requirements

- .NET 9.0 SDK; Windows (WPF)
- `WWControls.Core` alone targets .NET Standard 2.0 and runs anywhere

## License

See LICENSE file for details.
