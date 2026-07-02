using System.Collections.Generic;
using WWControls.SampleApp.Editors.Views.Samples.Editors;

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
                }),
        };
    }
}
