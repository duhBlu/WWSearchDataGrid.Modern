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

### 2. Define columns with GridColumn descriptors

```xml
<sdg:SearchDataGrid ItemsSource="{Binding Items}"
                    AutoGenerateColumns="False">
    <sdg:SearchDataGrid.GridColumns>
        <sdg:GridColumn FieldName="Name" Header="Name" />
        <sdg:GridColumn FieldName="City" Header="City" />
        <sdg:GridColumn FieldName="Amount" Header="Amount" />
    </sdg:SearchDataGrid.GridColumns>
</sdg:SearchDataGrid>
```

`FieldName` is the only required property -- it auto-generates the Binding, SortMemberPath, and FilterMemberPath. Every column automatically gets a search box. Users type to filter -- no additional code required.

---

## Common Scenarios

### Enable Advanced Rule Filtering

By default, columns use simple text search. Enable rule filtering to let users build multi-criteria filters (Between, TopN, IsLike, etc.):

```xml
<sdg:SearchDataGrid EnableRuleFiltering="True"
                    ItemsSource="{Binding Items}"
                    AutoGenerateColumns="False">
    <sdg:SearchDataGrid.GridColumns>
        <!-- This column gets the advanced filter button -->
        <sdg:GridColumn FieldName="Amount" Header="Amount" />

        <!-- Disable rule filtering on specific columns -->
        <sdg:GridColumn FieldName="Name" Header="Name"
                        EnableRuleFiltering="False" />
    </sdg:SearchDataGrid.GridColumns>
</sdg:SearchDataGrid>
```

### Boolean Checkbox Columns

For boolean columns, set `FieldType` and enable checkbox filtering:

```xml
<sdg:GridColumn FieldName="IsActive" Header="Active"
                FieldType="{x:Type sys:Boolean}"
                UseCheckBoxInSearchBox="True" />
```

The search box becomes a checkbox that cycles through: true -> false -> null (show all).

### Select-All Checkbox in Header

Add a header checkbox to toggle all boolean values at once:

```xml
<sdg:GridColumn FieldName="IsSelected" Header="Selected"
                FieldType="{x:Type sys:Boolean}"
                UseCheckBoxInSearchBox="True"
                IsSelectAllColumn="True"
                SelectAllScope="FilteredRows" />
```

Available scopes:
- `FilteredRows` - Only currently visible rows
- `SelectedRows` - Only selected rows (shows count)
- `AllItems` - All items regardless of filters

### Custom Column Display Names

Override the header text shown in the filter panel and column chooser:

```xml
<sdg:GridColumn FieldName="CustomerName" Header="Cust. Name"
                ColumnDisplayName="Customer Name" />
```

### FilterMemberPath Override

When the filter path differs from the field name:

```xml
<sdg:GridColumn FieldName="StatusDescription" Header="Status"
                FilterMemberPath="StatusCode"
                SortMemberPath="StatusCode" />
```

### Default Search Mode

Change how the simple text search box matches per-column:

```xml
<!-- Match from the beginning (good for IDs, part numbers) -->
<sdg:GridColumn FieldName="PartNumber" Header="Part #"
                DefaultSearchMode="StartsWith" />

<!-- Exact match only -->
<sdg:GridColumn FieldName="Status" Header="Status"
                DefaultSearchMode="Equals" />
```

Options: `Contains` (default), `StartsWith`, `EndsWith`, `Equals`

### Display Formatting

Format column values for display and filtering:

```xml
<!-- Currency -->
<sdg:GridColumn FieldName="Amount" Header="Amount"
                DisplayStringFormat="C2" />

<!-- Date -->
<sdg:GridColumn FieldName="OrderDate" Header="Order Date"
                DisplayStringFormat="MM/dd/yyyy" />

<!-- Custom converter -->
<sdg:GridColumn FieldName="Submitted" Header="Submitted"
                DisplayValueConverter="{StaticResource BoolToYesNoConverter}" />
```

### Hide Filtering or Sorting

Disable filtering or sorting on specific columns:

```xml
<sdg:GridColumn FieldName="Notes" Header="Notes"
                AllowFiltering="False" AllowSorting="False" />
```

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
