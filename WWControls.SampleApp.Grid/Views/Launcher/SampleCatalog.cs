using System.Collections.Generic;
using WWControls.SampleApp.Grid.Views.Samples;
using WWControls.SampleApp.Grid.Views.Samples.AnimationPerformance;
using WWControls.SampleApp.Grid.Views.Samples.Columns;
using WWControls.SampleApp.Grid.Views.Samples.DataBinding;
using WWControls.SampleApp.Grid.Views.Samples.Editing;
using WWControls.SampleApp.Grid.Views.Samples.Filtering;
using WWControls.SampleApp.Grid.Views.Samples.Grouping;
using WWControls.SampleApp.Grid.Views.Samples.Summaries;
using WWControls.SampleApp.Grid.Views.Samples.Usability;

namespace WWControls.SampleApp.Grid.Views.Launcher
{
    public static class SampleCatalog
    {
        public static IReadOnlyList<SampleCategory> Categories { get; } = new[]
        {
            new SampleCategory(
                "Data Binding",
                "How rows and columns reach the grid — POCO vs DataTable, static vs dynamic shapes.",
                new SampleDefinition[]
                {
                    new("Auto Columns Generation",
                        "AutoGenerateColumns=True with a POCO ⇄ DataTable source-type switch — attribute-driven columns vs DataTable schema, side by side.",
                        new[] { "POCO", "DataTable", "Auto-gen" },
                        () => new AutoColumnsSampleView()),

                    new("Smart Columns (Data Annotations)",
                        "IsSmart columns configure header, display format, editor, and mask from the bound property's annotations — [Display], [DisplayFormat] / [DataType], [GridEditor] / [DefaultEditor], and the mask attributes. Compare plain vs smart vs auto-generated with EnableSmartColumnsGeneration.",
                        new[] { "Smart", "Annotations", "IsSmart" },
                        () => new SmartColumnsSampleView()),

                    new("Binding to Dynamic Object",
                        "Bind a grid to ExpandoObject rows — every column reaches its value via an explicit GridColumn.Binding (no FieldName). Add typed columns and rows at runtime; filtering works off the binding path.",
                        new[] { "Dynamic", "Binding", "Runtime" },
                        () => new BindingToDynamicObjectSampleView()),
                }),

            new SampleCategory(
                "Layout and Customization",
                "Per-column knobs and grid-level layout — formatting, the column chooser, pinned columns, saved layouts.",
                new SampleDefinition[]
                {
                    new("Column Configuration",
                        "Click any column to focus it; tweak Visible / Fixed / AllowMoving / Resizing / Sorting / ReadOnly / paths from the inspector.",
                        new[] { "Layout", "Inspector", "Selection" },
                        () => new ColumnConfigurationSampleView()),

                    new("Display Formatting",
                        "DisplayStringFormat (C2 / N0 / P0 / dates), DisplayValueConverter (Yes/No), DisplayMask. Side-by-side raw vs formatted.",
                        new[] { "Format", "Converter", "Mask" },
                        () => new DisplayFormattingSampleView()),

                    new("Column Chooser",
                        "Floating chooser window with Enabled / Visible / ConfinedToGrid toggles. Plus per-column ShowInColumnChooser=False.",
                        new[] { "Chooser", "Visibility" },
                        () => new ColumnChooserSampleView()),

                    new("Fixed Columns",
                        "Left- and right-pinned columns via GridColumn.Fixed — pinned columns stay anchored while the rest scroll horizontally.",
                        new[] { "Fixed", "Pinned" },
                        () => new FixedColumnsSampleView()),

                    new("Column Bands",
                        "SearchDataGrid.Bands groups columns under caption rows above the headers. Bands nest — a band's children are columns or sub-bands — and flatten into the normal column pipeline, so the filter row and sorting still work.",
                        new[] { "Bands", "Grouped headers", "Nested" },
                        () => new ColumnBandsSampleView()),

                    new("Best Fit (Auto-Width)",
                        "Measurement-based column auto-sizing: VisibleRows vs AllRows, per-column AllowBestFit / BestFitArea overrides, gripper double-click, and auto-fit on source change.",
                        new[] { "BestFit", "Auto-width", "Sizing" },
                        () => new BestFitSampleView()),

                    new("Save and Restore Views",
                        "Save and reload the grid's layout (order, width, visibility, pinning, sort, grouping) and its filters as portable .sdgview files — separately or together. Apply a built-in preset live, or save/load your own from the grid's context menu.",
                        new[] { "Layout", "Filters", "Persistence" },
                        () => new SaveRestoreViewSampleView()),
                }),

            new SampleCategory(
                "Selection and Usability",
                "Interaction surfaces — clipboard, context menus.",
                new SampleDefinition[]
                {
                    new("Copy / Paste operations",
                        "Grid on top, paste TextBox below. Ctrl+C / Ctrl+Shift+C (or the buttons) copy tab-separated values, with headers optional.",
                        new[] { "Clipboard", "Copy" },
                        () => new CopyPasteSampleView()),

                    new("Built-In Context Menus",
                        "Right-click cells / headers / row headers for the default menus; toggle grid properties to gate items. Custom-item injection is Planned.",
                        new[] { "ContextMenu", "Customization" },
                        () => new ContextMenusSampleView()),
                }),

            new SampleCategory(
                "Editing",
                "EditSettings types, customization layers, masking, validation, and row-level editing.",
                new SampleDefinition[]
                {
                    new("Cell Editors",
                        "Every EditSettings type — Text, ComboBox, Spin, id-based ComboBox, CheckBox, Date. EditorShowMode grid + per-editor override.",
                        new[] { "EditSettings", "EditorShowMode" },
                        () => new EditorTypesSampleView()),

                    new("Cell Editor Customization",
                        "Two layers — DisplayStyle (re-style the display cell) and EditTemplate / DisplayTemplate / FilterRowEditTemplate (full takeover).",
                        new[] { "DisplayStyle", "Templates" },
                        () => new EditorCustomizationSampleView()),

                    new("Editor Input Masking",
                        "Mask grammar — phone, SSN, ZIP+4, plate, account, date, Numeric C / P2 / F2, TimeSpan.",
                        new[] { "Masking", "Grammar" },
                        () => new InputMaskingSampleView()),

                    new("New Item Row",
                        "NewRowPosition places the add-new-row at top / bottom / none (adding disabled). New rows auto-fill their Id; Delete removes a selected row.",
                        new[] { "NewRow", "AddRow" },
                        () => new NewItemRowSampleView()),

                    new("Data Validation",
                        "Smart columns validate edits against DataAnnotations (Required / Range / StringLength / RegularExpression / CustomValidation). Invalid edits show a red border + message tooltip and are blocked; toggle commit-on-error and validation on/off.",
                        new[] { "Validation", "Errors", "Smart" },
                        () => new DataValidationSampleView()),

                    new("Data Error Indication",
                        "A self-reporting row (ObservableValidator → INotifyDataErrorInfo) drives severity-aware badges — blocking Error (red), advisory Info (blue), and Warning (amber) — via IValidationSeverityProvider. Edit a cell to clear or raise a badge.",
                        new[] { "Severity", "Warnings" },
                        () => new DataErrorIndicationSampleView()),

                    new("Inline Edit Form",
                        "Edit a whole row through a caption/editor form (EditFormShowMode = Inline / InlineHideRow) hosted in the row's details area. Auto-generates a layout from the columns or uses a custom EditFormTemplate of EditFormEditor fields; per-column EditFormCaption / EditFormColumnSpan tune it. Reuses the row's IEditableObject transaction; an optional focus-leave confirmation guards unsaved changes.",
                        new[] { "Form", "Inline" },
                        () => new EditFormSampleView()),

                    new("Edit Entire Row",
                        "Click a cell and the whole row opens for editing behind a dimming overlay with a row-scoped Update / Cancel bar. RowEditTrigger gates when it engages; Cancel reverts every field via IEditableObject.",
                        new[] { "Row", "Batch" },
                        () => new EditEntireRowSampleView()),
                }),

            new SampleCategory(
                "Data Shaping",
                "Filtering, grouping, and summaries.",
                new SampleDefinition[]
                {
                    new("Filtering",
                        "Hub of filtering mini-samples — Excel-style dropdown, filter editor, filter row, filter panel, search modes.",
                        new[] { "Filtering", "Hub" },
                        () => new FilteringHubSampleView()),

                    new("Grouping",
                        "Group rows by one or more columns — declarative GroupIndex, the GroupBy / Ungroup API, and the header context menu. Each group is an expander with a value + row count; grouping leads sorting and coexists with the filter row. (Drag-to-group panel and group-value templates are planned.)",
                        new[] { "Grouping", "GroupIndex" },
                        () => new BasicGroupingSampleView()),

                    new("Total Summaries",
                        "Aggregate summaries across four surfaces — the column-aligned total summary row, the fixed total summary panel (feature-gated by AllowFixedTotalSummary), per-group GroupSummaries in the headers (inline or aligned-by-column), and per-group GroupFooterSummaries that dock at the bottom of a group and pin beneath the header when collapsed. Each carries a runtime right-click picker + Customize editor, all suppressible via SummaryContextMenusEnabled.",
                        new[] { "Totals", "Aggregate", "Footer" },
                        () => new TotalSummariesSampleView()),
                }),

            new SampleCategory(
                "Performance",
                "Make scrolling feel right, then make it scale.",
                new SampleDefinition[]
                {
                    new("Vertical Scrolling Options",
                        "Mouse-wheel momentum / inertia, per-pixel scrolling, ScrollAnimationMode. (Allow-Fixed-Groups and Cascade-Update toggles are Planned.)",
                        new[] { "Scrolling", "Momentum" },
                        () => new ScrollingAnimationSampleView()),

                    new("Large Datasets",
                        "Virtualization tuning, column virtualization, live scrolling, plus FilterRowDelay × EnableLiveFiltering. Quick scale buttons up to 1M rows.",
                        new[] { "Virtualization", "Scale", "Debounce" },
                        () => new LargeDatasetsSampleView()),
                }),
        };
    }
}
