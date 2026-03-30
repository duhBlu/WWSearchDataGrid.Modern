# Getting Started

## Prerequisites

- .NET 9.0 SDK
- A WPF application project

## Installation

Add project references to both `WWSearchDataGrid.Modern.Core` and `WWSearchDataGrid.Modern.WPF`, or reference the NuGet packages when available.

## Basic Setup

### 1. Add the XML namespace

```xml
<Window xmlns:sdg="clr-namespace:WWSearchDataGrid.Modern.WPF;assembly=WWSearchDataGrid.Modern.WPF">
```

### 2. Replace your DataGrid

```xml
<sdg:SearchDataGrid ItemsSource="{Binding Items}"
                    AutoGenerateColumns="False">
    <sdg:SearchDataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" />
        <DataGridTextColumn Header="City" Binding="{Binding City}" />
        <DataGridTextColumn Header="Amount" Binding="{Binding Amount}" />
    </sdg:SearchDataGrid.Columns>
</sdg:SearchDataGrid>
```

Every column automatically gets a search box. Users type to filter -- no additional code required.

---

## Common Scenarios

### Enable Advanced Rule Filtering

By default, columns use simple text search. Enable rule filtering to let users build multi-criteria filters (Between, TopN, IsLike, etc.):

```xml
<sdg:SearchDataGrid EnableRuleFiltering="True"
                    ItemsSource="{Binding Items}">
    <sdg:SearchDataGrid.Columns>
        <!-- This column gets the advanced filter button -->
        <DataGridTextColumn Header="Amount"
                            Binding="{Binding Amount}"
                            sdg:GridColumn.EnableRuleFiltering="True" />

        <!-- Disable rule filtering on specific columns -->
        <DataGridTextColumn Header="Name"
                            Binding="{Binding Name}"
                            sdg:GridColumn.EnableRuleFiltering="False" />
    </sdg:SearchDataGrid.Columns>
</sdg:SearchDataGrid>
```

### Boolean Checkbox Columns

For boolean columns, use checkbox filtering instead of text search:

```xml
<DataGridCheckBoxColumn Header="Active"
                        Binding="{Binding IsActive}"
                        sdg:GridColumn.UseCheckBoxInSearchBox="True"
                        sdg:GridColumn.FilterMemberPath="IsActive" />
```

The search box becomes a checkbox that cycles through: true -> false -> null (show all).

### Select-All Checkbox in Header

Add a header checkbox to toggle all boolean values at once:

```xml
<DataGridCheckBoxColumn Header="Selected"
                        Binding="{Binding IsSelected}"
                        sdg:GridColumn.UseCheckBoxInSearchBox="True"
                        sdg:GridColumn.IsSelectAllColumn="True"
                        sdg:GridColumn.SelectAllScope="FilteredRows" />
```

Available scopes:
- `FilteredRows` - Only currently visible rows
- `SelectedRows` - Only selected rows (shows count)
- `AllItems` - All items regardless of filters

### Custom Column Display Names

Override the header text shown in the filter panel and column chooser:

```xml
<DataGridTextColumn Header="Cust. Name"
                    Binding="{Binding CustomerName}"
                    sdg:GridColumn.ColumnDisplayName="Customer Name" />
```

### FilterMemberPath for Template Columns

For `DataGridTemplateColumn` or when the filter path differs from the display binding:

```xml
<DataGridTemplateColumn Header="Status"
                        sdg:GridColumn.FilterMemberPath="StatusCode"
                        SortMemberPath="StatusCode">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding StatusDescription}" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### Default Search Mode

Change how the simple text search box matches per-column:

```xml
<!-- Match from the beginning (good for IDs, part numbers) -->
<DataGridTextColumn Header="Part #"
                    Binding="{Binding PartNumber}"
                    sdg:GridColumn.DefaultSearchMode="StartsWith" />

<!-- Exact match only -->
<DataGridTextColumn Header="Status"
                    Binding="{Binding Status}"
                    sdg:GridColumn.DefaultSearchMode="Equals" />
```

Options: `Contains` (default), `StartsWith`, `EndsWith`, `Equals`

### Column Chooser

Enable the built-in column visibility manager:

```xml
<sdg:SearchDataGrid IsColumnChooserEnabled="True"
                    IsColumnChooserConfinedToGrid="False"
                    ItemsSource="{Binding Items}" />
```

Users access it via the context menu. The chooser supports drag-drop column reordering.

### Auto-Size Columns

Dynamically size columns to fit visible content:

```xml
<sdg:SearchDataGrid AutoSizeColumns="True"
                    ItemsSource="{Binding Items}" />
```

Columns resize as users scroll, respecting `MinWidth` and `MaxWidth` constraints.

### Cell Value Change Tracking

Subscribe to the `CellValueChanged` event to detect edits:

```csharp
searchDataGrid.CellValueChanged += (sender, e) =>
{
    Console.WriteLine($"Column '{e.BindingPath}' changed from '{e.OldValue}' to '{e.NewValue}'");
};
```

---

## Styling

The control uses WPF's Generic.xaml pattern. Override styles in your app's resource dictionaries. See the SampleApp's `Styles/` folder for examples of custom theming for every control.

## Supported Search Types

| Category | Types |
|----------|-------|
| Text | Contains, DoesNotContain, StartsWith, EndsWith, Equals, NotEquals |
| Comparison | LessThan, LessThanOrEqualTo, GreaterThan, GreaterThanOrEqualTo |
| Range | Between, NotBetween, BetweenDates |
| Null | IsNull, IsNotNull |
| Pattern | IsLike, IsNotLike (SQL LIKE syntax) |
| Multi-Value | IsAnyOf, IsNoneOf, IsOnAnyOfDates |
| Date | Yesterday, Today, Tomorrow, DateInterval (13 interval types) |
| Statistical | TopN, BottomN, AboveAverage, BelowAverage, Unique, Duplicate |
