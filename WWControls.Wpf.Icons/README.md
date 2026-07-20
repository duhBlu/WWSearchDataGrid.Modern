# WWControls.Wpf.Icons

Dedicated assembly for the WWControls icon library. Icons are authored as monochrome
`DrawingImage` resources in the Lucide style — a 24×24 viewport, stroke-based, with a 2px
`Pen` using round caps and joins — and tinted at render time by
`WWControls.Wpf.Controls.Primitives.Icon`.

## Layout

| Folder | Contents |
|--------|----------|
| `Lucide/` | The stock Lucide icon set (<https://lucide.dev>). |
| `SearchType/` | Datagrid-specific glyphs — one per `SearchType` / `DateInterval` filter. |
| `Custom/` | In-house icons drawn to match the Lucide style. |

`Icons.xaml` at the root is the public merge entry point; it merges the three sets so a
consumer pulls the whole library with a single dictionary reference:

```xml
<ResourceDictionary Source="/WWControls.Wpf.Icons;component/Icons.xaml"/>
```

## Keys

Each folder pairs its dictionary with a placeholder key class exposing the resource keys as
`ComponentResourceKey` members — `LucideIconKeys`, `SearchTypeIconKeys`, `CustomIconKeys` (all in
the `WWControls.Wpf.Icons` namespace) — matching the `IconKeys` / `SearchTypeIconKeys` convention
in the Controls assembly. Member names are the PascalCase form of the Lucide SVG file name
(`a-arrow-down.svg` → `AArrowDown`). Dictionaries reference the keys through the local
`icons` xmlns (`clr-namespace:WWControls.Wpf.Icons`).

## Status

Infrastructure only. This assembly has no project references, and nothing references it yet.
The existing icon resources and keys in `WWControls.Wpf.Controls`
(`Primitives/Resources/Icons/*Keys.cs`) and `WWControls.Wpf.Themes.Default`
(`Resources/Icons/*.xaml`) have not been moved.
