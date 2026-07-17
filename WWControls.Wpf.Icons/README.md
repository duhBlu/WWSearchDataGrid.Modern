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

## Status

Infrastructure only. This assembly has no project references, and nothing references it yet.
The existing icon resources and keys in `WWControls.Wpf.Controls`
(`Primitives/Resources/Icons/*Keys.cs`) and `WWControls.Wpf.Themes.Default`
(`Resources/Icons/*.xaml`) have not been moved. Resource keys are added separately.
