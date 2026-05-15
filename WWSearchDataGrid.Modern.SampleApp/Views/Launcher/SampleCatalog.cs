using System.Collections.Generic;
using WWSearchDataGrid.Modern.SampleApp.Views.Samples.AnimationPerformance;
using WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow;
using WWSearchDataGrid.Modern.SampleApp.Views.Samples.Columns;
using WWSearchDataGrid.Modern.SampleApp.Views.Samples.DataBinding;
using WWSearchDataGrid.Modern.SampleApp.Views.Samples.Editing;
using WWSearchDataGrid.Modern.SampleApp.Views.Samples.Filtering;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Launcher
{
    public static class SampleCatalog
    {
        public static IReadOnlyList<SampleCategory> Categories { get; } = new[]
        {
            new SampleCategory(
                "Data Binding",
                "How rows reach the grid — POCO vs DataTable, manual vs auto-generated columns.",
                new SampleDefinition[]
                {
                    new("POCO + Attributes",
                        "[Display] / [Browsable] attributes drive auto-generated columns from a plain CLR collection.",
                        new[] { "POCO", "Attributes", "Auto-gen" },
                        () => new PocoAttributesSampleView()),

                    new("DataTable, Manual Columns",
                        "DataTable / DataView with manually declared GridColumns. DBNull, computed expression, name-with-space.",
                        new[] { "DataTable", "Manual" },
                        () => new DataTableManualSampleView()),

                    new("DataTable, Auto-gen",
                        "Same DataTable bound with AutoGenerateColumns=True — columns come from ITypedList descriptors.",
                        new[] { "DataTable", "Auto-gen" },
                        () => new DataTableAutoGenSampleView()),
                }),

            new SampleCategory(
                "Columns",
                "Per-column knobs and grid-level features that shape what each column looks like.",
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
                }),

            new SampleCategory(
                "Filtering",
                "Search modes, custom predicates, and the rule-filter popup.",
                new SampleDefinition[]
                {
                    new("Search Modes",
                        "DefaultSearchType (StartsWith / EndsWith / Contains / Equals — string columns default to StartsWith), AllowFiltering=False, EnableRuleFiltering toggle.",
                        new[] { "Search", "Modes" },
                        () => new SearchModesSampleView()),

                    new("Custom Predicate & Events",
                        "A Predicate<object> SearchFilter applied alongside column filters, with a live event log of every FilterPanel event.",
                        new[] { "Predicate", "Events" },
                        () => new CustomPredicateSampleView()),

                    new("Rule Filter Popup",
                        "ColumnFilterEditor — multi-criteria rules joined with AND / OR plus a Filter Values tab.",
                        new[] { "Rules", "Popup" },
                        () => new RuleFilterPopupSampleView()),
                }),

            new SampleCategory(
                "Auto Filter Row",
                "Per-column quick-search row across the top of the grid. Grid- and column-level DPs control its behavior.",
                new SampleDefinition[]
                {
                    new("Options Playground",
                        "Every grid-level and column-level auto-filter-row DP exposed as runtime toggles — pick a column, tweak its settings, watch the grid react.",
                        new[] { "Playground", "DPs" },
                        () => new OptionsPlaygroundSampleView()),

                    new("Custom Templates",
                        "Replace the default filter editor per column: numeric Slider, DatePicker, and a RadioButton group via GridColumn.AutoFilterRowEditTemplate + EditGridCellData.",
                        new[] { "Templates", "EditGridCellData" },
                        () => new CustomTemplatesSampleView()),

                    new("Debounce & Live Filter",
                        "FilterRowDelay × ImmediateUpdateAutoFilter × LiveFilteringRowCountThreshold (100k). Switch between 1k / 100k / 1M rows and feel each setting interact.",
                        new[] { "Debounce", "Live filtering", "Threshold" },
                        () => new DebounceBehaviorSampleView()),
                }),

            new SampleCategory(
                "Editing",
                "EditSettings types, customization layers, select-all, and input masking.",
                new SampleDefinition[]
                {
                    new("Editor Types",
                        "Every EditSettings type — Text, ComboBox, Spin, id-based ComboBox, CheckBox, Date. EditorShowMode grid + per-editor override; toggle BooleanEditor and EditorButtonShowMode at runtime.",
                        new[] { "EditSettings", "EditorShowMode", "EditorButtonShowMode" },
                        () => new EditorTypesSampleView()),

                    new("Editor Customization",
                        "Two layers — EditorStyle (re-style without replacing) and EditTemplate / DisplayTemplate (full takeover).",
                        new[] { "EditorStyle", "Templates" },
                        () => new EditorCustomizationSampleView()),

                    new("Select-All Columns",
                        "IsSelectAllColumn header checkbox + switchable SelectAllScope (FilteredRows / SelectedRows / AllItems).",
                        new[] { "Select-All", "Scope" },
                        () => new SelectAllSampleView()),

                    new("Input Masking",
                        "Mask grammar — phone, SSN, ZIP+4, plate, account, date, Numeric C / P2 / F2, TimeSpan.",
                        new[] { "Masking", "Grammar" },
                        () => new InputMaskingSampleView()),
                }),

            new SampleCategory(
                "Animation & Performance",
                "Make scrolling feel right, then make it scale.",
                new SampleDefinition[]
                {
                    new("Scrolling Animation",
                        "Mouse-wheel momentum / inertia, per-pixel scrolling, ScrollAnimationMode (EaseOut / EaseInOut / Linear / Custom Storyboard).",
                        new[] { "Scrolling", "Momentum" },
                        () => new ScrollingAnimationSampleView()),

                    new("Large Datasets",
                        "Virtualization tuning, column virtualization, live scrolling. Quick scale buttons up to 1M rows.",
                        new[] { "Virtualization", "Scale" },
                        () => new LargeDatasetsSampleView()),
                }),
        };
    }
}
