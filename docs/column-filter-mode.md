# Column Filter Mode

This page explains how `WWSearchDataGrid.Modern` decides whether to compare a user's filter input against a column's **raw value** or its **display-formatted value** — and how to opt into display-text filtering for a column.

The library does not expose a `ColumnFilterMode` enum. The mode is inferred automatically from the column's display configuration. Configure display formatting, and filtering follows along.

---

## How the mode is chosen

For each column, the grid builds at most one `IDisplayValueProvider`. The provider is selected by the first match in this priority chain:

| Priority | Provider | Triggered by | Comparison style |
|---|---|---|---|
| 1 | `MaskDisplayProvider` | `GridColumn.DisplayMask` set | **Raw** — structural characters (parens, dashes, spaces) are stripped from both sides |
| 2 | `ConverterDisplayProvider` | `GridColumn.DisplayValueConverter` set | **Display** — user-typed text is matched against converter output |
| 3 | `StringFormatDisplayProvider` | `GridColumn.DisplayStringFormat` set | **Display** — user-typed text is matched against formatted output |
| 4 | `ComboBoxLookupDisplayProvider` | `GridColumn.EditSettings` is a `ComboBoxEditSettings` with `DisplayMemberPath` | **Display** — user-typed text is matched against lookup result |
| — | _none_ | Column has no display configuration | **Raw** — direct comparison against the underlying property value |

The choice is per-column. Two columns bound to the same property can filter differently if their display configuration differs.

---

## Recipes

### Filter against a currency-formatted value

```xml
<sdg:GridColumn FieldName="OrderItemsTotalPrice"
                Header="Total"
                DisplayStringFormat="C2" />
```

Typing `$1,000` in the filter cell matches rows whose formatted total contains `$1,000`. The user matches what they see.

### Filter against a Yes / No converter

```xml
<sdg:GridColumn FieldName="IsActive"
                Header="Active"
                DisplayValueConverter="{StaticResource BoolToYesNoConverter}" />
```

Typing `yes` matches active rows; `no` matches inactive rows.

### Filter a masked phone column

```xml
<sdg:GridColumn FieldName="Phone"
                Header="Phone"
                DisplayMask="(000) 000-0000" />
```

The mask provider strips formatting characters from the stored value. Typing `5551234` matches `(555) 123-4XXX` — the user does not need to type the parens or dashes.

### Filter raw values (no display configuration)

```xml
<sdg:GridColumn FieldName="EmployeeId" Header="ID" />
```

Direct comparison against the raw property value. The default for any column without a display configuration.

---

## Stored-value awareness

Text-based search types (`Contains`, `StartsWith`, `EndsWith`, `Equals`, `IsLike`) compare against the display value **when the stored search value is a string** — i.e. typed by the user into the filter cell. When the stored value is a non-string typed object — for example, an `IsAnyOf` filter populated from the FilterValues tab containing `[true, false]` against a `BoolToYesNoConverter` column — the comparison falls back to the raw value automatically. Without this, the typed boolean would never match the converted `"Yes"` / `"No"` display strings.

Non-text search types (`Equal`, `Between`, `GreaterThan`, statistical operators, etc.) always compare against the raw value regardless of provider configuration.

You do not configure this. The grid detects it from the stored search value's runtime type.

---

## Filter-chip display

Filter chips on the `FilterSummaryPanel` reflect provider configuration:

- **Mask providers** show different chip text per search type — `StartsWith` shows the mask-formatted prefix `(555) ___-____`, `EndsWith` shows the end-aligned form `(___) ___-1234`, and `Contains` / `Equals` show the raw value `5551234`.
- **Format / converter providers** show the display-formatted value when the user picked a typed value (e.g. from a dropdown), and show the user's literal text when they typed it.
- **No provider** shows the raw value.

---

## Opting out of display-text filtering

There is currently no per-column override to force raw comparison on a column that has display formatting configured. If you need raw filtering on a column that displays a formatted value, the workaround is to expose a separate `FilterMemberPath`:

```xml
<sdg:GridColumn FieldName="DisplayTotal"
                FilterMemberPath="RawTotal"
                Header="Total"
                DisplayStringFormat="C2" />
```

The cell displays the `C2`-formatted `DisplayTotal`, but the filter pipeline reads `RawTotal` directly with no provider attached.

---

## See it work

Run `WWSearchDataGrid.Modern.SampleApp`, open **Columns / Display Formatting**. The sample places raw and formatted columns side-by-side over the same underlying property so the filtering difference is obvious — type `$1,000` into the `Total (C2)` column and only formatted matches pass; the raw `Total (raw)` column accepts only the raw decimal.

---

## API quick reference

| `GridColumn` property | Effect on filtering |
|---|---|
| `DisplayMask` | Mask provider — raw comparison with structural characters stripped |
| `DisplayValueConverter` | Converter provider — display-to-display comparison |
| `DisplayStringFormat` | StringFormat provider — display-to-display comparison |
| `EditSettings` (`ComboBoxEditSettings` with `DisplayMemberPath`) | Lookup provider — display-to-display comparison |
| `FilterMemberPath` | Overrides the binding path the filter reads, regardless of the display binding |
| _(none of the above)_ | Raw comparison against the property value |

See `docs/api-reference.md` for the full `GridColumn` API.
