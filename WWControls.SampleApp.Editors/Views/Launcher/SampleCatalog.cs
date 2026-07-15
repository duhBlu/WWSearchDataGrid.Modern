using System.Collections.Generic;
using WWControls.SampleApp.Editors.Views.Samples.Buttons;
using WWControls.SampleApp.Editors.Views.Samples.Dialogs;
using WWControls.SampleApp.Editors.Views.Samples.Editors;
using WWControls.SampleApp.Editors.Views.Samples.Primitives;
using WWControls.SampleApp.Editors.Views.Samples.Trees;

namespace WWControls.SampleApp.Editors.Views.Launcher
{
    public static class SampleCatalog
    {
        public static IReadOnlyList<SampleCategory> Categories { get; } = new[]
        {
            new SampleCategory(
                "Overview",
                "All five editor controls side by side — the standalone-form story at a glance.",
                new SampleDefinition[]
                {
                    new("Editor Gallery",
                        "All five editor controls (WWTextBox / WWNumericUpDown / WWComboBox / WWDatePicker / WWCheckBox) used directly on a form (no grid) — flat vs bordered chrome owned by WWEditorBase, each two-way bound.",
                        new[] { "Editors", "Gallery" },
                        () => new EditorGallerySampleView()),
                }),

            new SampleCategory(
                "Editors",
                "One focused sample per editor control — chrome, masking, bounds, selection, and state.",
                new SampleDefinition[]
                {
                    new("WWTextBox",
                        "An options-panel playground for one live editor — chrome (ShowBorder / flat), IsReadOnly, Watermark, ShowClearButton, an edge Glyph + placement, TextAlignment, MaxLength, and the UpdateDelay debounce.",
                        new[] { "Text", "Chrome", "Glyph" },
                        () => new TextBoxSampleView()),

                    new("Masking",
                        "Masked input across all four engines — Simple (phone / SSN / card / plate), Numeric (C2 / N0 / F2 / P1), DateTime / TimeSpan — plus PromptChar, the UnmaskedValue / IsMaskComplete readback, and the Simple mask grammar.",
                        new[] { "Text", "Masking", "Format" },
                        () => new MaskSampleView()),

                    new("WWNumericUpDown",
                        "Spinner buttons plus Ctrl+Up/Down keyboard stepping — Minimum / Maximum bounds, Increment, and the Ctrl+Shift LargeIncrement jump.",
                        new[] { "Spin", "Numeric", "Bounds" },
                        () => new NumericUpDownSampleView()),

                    new("WWComboBox",
                        "Selection editor — plain string items, object items with DisplayMemberPath, id-based selection via SelectedValuePath / SelectedValue, and the IsEditable type-to-select mode.",
                        new[] { "Combo", "Selection", "IsEditable" },
                        () => new ComboBoxSampleView()),

                    new("WWDatePicker",
                        "Date editor — default short-date mask, custom .NET date format masks, MinDate / MaxDate clamping, and read-only display.",
                        new[] { "Date", "Masking", "Bounds" },
                        () => new DatePickerSampleView()),

                    new("WWCheckBox",
                        "Boolean editor — two-state, three-state (nullable), and read-only. A glyph control, so it stays flat regardless of ShowBorder.",
                        new[] { "Check", "ThreeState" },
                        () => new CheckBoxSampleView()),

                    new("WWListBox",
                        "List control — SelectionMode (Single / Multiple click-toggle / Extended Ctrl+Shift), ItemKind selection glyphs (Default / Checked / Radio), and built-in drag reordering via AllowReorder with the traveling-hole animation.",
                        new[] { "List", "Selection", "Reorder" },
                        () => new ListBoxSampleView()),

                    new("WWColorPicker",
                        "HSV color picker — a swatch toggle dropping preset swatches, H / S / B sliders, and a hex box. Six pickers bound two-way to theme roles live-reskin a mock dashboard; shows SelectedColor, DisplayColorAndName, swappable PresetColors, and the color→brush projection.",
                        new[] { "Color", "HSV", "Palette" },
                        () => new ColorPickerSampleView()),
                }),

            new SampleCategory(
                "Property Grid",
                "The WWPropertyGrid — reflection-driven, category-grouped property editing with per-property editor templates.",
                new SampleDefinition[]
                {
                    new("WWPropertyGrid",
                        "Reflects a bound object's properties (grouped by [Category], labeled from [DisplayName] / [Description]), with custom editor templates supplied per property via EditorDefinitions and a read-only placeholder for the rest. Includes the search filter, category expanders, resizable name column, and the selected-row description panel.",
                        new[] { "PropertyGrid", "Reflection", "Editors" },
                        () => new PropertyGridSampleView()),
                }),

            new SampleCategory(
                "Buttons",
                "The WWButton primitive — one control covering simple, repeat, and toggle behaviors.",
                new SampleDefinition[]
                {
                    new("WWButton",
                        "ButtonKind (Simple / Repeat / Toggle with IsThreeState), Glyph docked on any side, per-instance CornerRadius, and the AsyncDisplayMode wait / wait-cancel wheel driven by an AsyncCommand.",
                        new[] { "Button", "Repeat", "Toggle", "Async" },
                        () => new ButtonSampleView()),
                }),

            new SampleCategory(
                "Dialogs",
                "The WWMessageBox — a modal message dialog whose buttons come from a UICommand list.",
                new SampleDefinition[]
                {
                    new("WWMessageBox",
                        "A message dialog in the library window chrome. Shows the standard MessageBoxButton / MessageBoxResult drop-in alongside custom UICommand lists (Save / Don't Save / Cancel with glyphs, and an arbitrary five-choice set), a severity icon per MessageBoxImage, and the Copy Message button.",
                        new[] { "Dialog", "MessageBox", "UICommand" },
                        () => new MessageBoxSampleView()),
                }),

            new SampleCategory(
                "Trees",
                "The WWTreeView primitive — a themed tree with selection binding, drag-and-drop, and expand/collapse-all.",
                new SampleDefinition[]
                {
                    new("WWTreeView",
                        "Two-way SelectedObject binding, ExpandOnLoad, per-item expand/collapse buttons (ExpandCollapseButtonMode), the tree-level ExpandAll / CollapseAll commands, and drag-drop reparenting via OnDropCommand (payload is a (target, dragged) tuple; CanExecute rejects illegal moves). Structural roots opt out of dragging through IWWTreeViewDragItem.",
                        new[] { "Tree", "Selection", "DragDrop", "Expand" },
                        () => new TreeViewSampleView()),
                }),

            new SampleCategory(
                "Primitives",
                "The small building-block primitives — a spacing-aware layout panel and a search-highlighting text block.",
                new SampleDefinition[]
                {
                    new("SimpleStackPanel",
                        "A lightweight single-line layout panel: Orientation (Horizontal / Vertical) plus a uniform Spacing gap that needs no per-child margins — and skips the gap entirely for Collapsed children so neighbours close up flush.",
                        new[] { "Layout", "Panel", "Spacing" },
                        () => new SimpleStackPanelSampleView()),

                    new("HighlightTextBlock",
                        "A TextBlock that emits the first (case-insensitive) match of a search term as its own Run: HighlightTextBlockText / HighlightText binding, MatchMode anchoring (Contains / StartsWith / EndsWith), and a live-composed HighlightRunStyle (weight / italic / underline / foreground / background fill) applied to the match.",
                        new[] { "Text", "Search", "Highlight" },
                        () => new HighlightTextBlockSampleView()),
                }),
        };
    }
}
