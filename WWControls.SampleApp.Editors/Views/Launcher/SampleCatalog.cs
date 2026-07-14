using System.Collections.Generic;
using WWControls.SampleApp.Editors.Views.Samples.Buttons;
using WWControls.SampleApp.Editors.Views.Samples.Editors;
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
                        "Chrome (flat / bordered / read-only), TextAlignment, Simple masks (phone, SSN, plate), and Numeric masks (C2 / P0 / F2) — the full masking grammar on a standalone editor.",
                        new[] { "Text", "Masking", "Chrome" },
                        () => new TextBoxSampleView()),

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
                "Trees",
                "The WWTreeView primitive — a themed tree with selection binding, drag-and-drop, and expand/collapse-all.",
                new SampleDefinition[]
                {
                    new("WWTreeView",
                        "Two-way SelectedObject binding, ExpandOnLoad, per-item expand/collapse buttons (ExpandCollapseButtonMode), the tree-level ExpandAll / CollapseAll commands, and drag-drop reparenting via OnDropCommand (payload is a (target, dragged) tuple; CanExecute rejects illegal moves). Structural roots opt out of dragging through IWWTreeViewDragItem.",
                        new[] { "Tree", "Selection", "DragDrop", "Expand" },
                        () => new TreeViewSampleView()),
                }),
        };
    }
}
