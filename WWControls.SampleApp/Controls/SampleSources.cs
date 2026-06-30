using System.Collections.Generic;
using WWControls.SampleApp.SampleData;

namespace WWControls.SampleApp.Controls
{
    /// <summary>
    /// One source-file list per sample, exposed as <c>x:Static</c>-friendly properties so XAML can
    /// bind <c>SampleHostControl.Sources</c> without per-sample resource-loading boilerplate. The
    /// combobox in the Source tab walks the list; each entry has its own AvalonEdit syntax
    /// highlighting (XML for .xaml, C# for .cs).
    /// </summary>
    public static class SampleSources
    {
        // ── Data Binding ─────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> AutoColumns { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/DataBinding/AutoColumnsSampleView.xaml",
            "Views/Samples/DataBinding/AutoColumnsSampleView.xaml.cs",
            "Views/Samples/DataBinding/AutoColumnsSampleViewModel.cs",
            "Models/Customer.cs",
            "SampleData/Generators/CustomerGenerator.cs",
            "SampleData/Generators/VendorTableGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> DataTableManual { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/DataBinding/DataTableManualSampleView.xaml",
            "Views/Samples/DataBinding/DataTableManualSampleView.xaml.cs",
            "Views/Samples/DataBinding/DataTableManualSampleViewModel.cs",
            "SampleData/Generators/VendorTableGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> BindingToDynamicObject { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/DataBinding/BindingToDynamicObjectSampleView.xaml",
            "Views/Samples/DataBinding/BindingToDynamicObjectSampleView.xaml.cs",
            "Views/Samples/DataBinding/BindingToDynamicObjectSampleViewModel.cs");

        // ── Columns ──────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> ColumnConfiguration { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Columns/ColumnConfigurationSampleView.xaml",
            "Views/Samples/Columns/ColumnConfigurationSampleView.xaml.cs",
            "Views/Samples/Columns/ColumnConfigurationSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> DisplayFormatting { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Columns/DisplayFormattingSampleView.xaml",
            "Views/Samples/Columns/DisplayFormattingSampleView.xaml.cs",
            "Views/Samples/Columns/DisplayFormattingSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> ColumnChooser { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Columns/ColumnChooserSampleView.xaml",
            "Views/Samples/Columns/ColumnChooserSampleView.xaml.cs",
            "Views/Samples/Columns/ColumnChooserSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> FixedColumns { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Columns/FixedColumnsSampleView.xaml",
            "Views/Samples/Columns/FixedColumnsSampleView.xaml.cs",
            "Views/Samples/Columns/FixedColumnsSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> BestFit { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Columns/BestFitSampleView.xaml",
            "Views/Samples/Columns/BestFitSampleView.xaml.cs",
            "Views/Samples/Columns/BestFitSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        // ── Selection and Usability ──────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> CopyPaste { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Usability/CopyPasteSampleView.xaml",
            "Views/Samples/Usability/CopyPasteSampleView.xaml.cs",
            "Views/Samples/Usability/CopyPasteSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> ContextMenus { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Usability/ContextMenusSampleView.xaml",
            "Views/Samples/Usability/ContextMenusSampleView.xaml.cs",
            "Views/Samples/Usability/ContextMenusSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        // ── Filtering ────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> SearchModes { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Filtering/SearchModesSampleView.xaml",
            "Views/Samples/Filtering/SearchModesSampleView.xaml.cs",
            "Views/Samples/Filtering/SearchModesSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> CustomPredicate { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Filtering/CustomPredicateSampleView.xaml",
            "Views/Samples/Filtering/CustomPredicateSampleView.xaml.cs",
            "Views/Samples/Filtering/CustomPredicateSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> FilterString { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Filtering/FilterStringSampleView.xaml",
            "Views/Samples/Filtering/FilterStringSampleView.xaml.cs",
            "Views/Samples/Filtering/FilterStringSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> CustomFilterElements { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Filtering/CustomFilterElementsSampleView.xaml",
            "Views/Samples/Filtering/CustomFilterElementsSampleView.xaml.cs",
            "Views/Samples/Filtering/CustomFilterElementsSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> MultiTabFilterPopup { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Filtering/MultiTabFilterPopupSampleView.xaml",
            "Views/Samples/Filtering/MultiTabFilterPopupSampleView.xaml.cs",
            "Views/Samples/Filtering/MultiTabFilterPopupSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        // ── Filter Row ───────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> OptionsPlayground { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/FilterRow/OptionsPlaygroundSampleView.xaml",
            "Views/Samples/FilterRow/OptionsPlaygroundSampleView.xaml.cs",
            "Views/Samples/FilterRow/OptionsPlaygroundSampleViewModel.cs",
            "Views/Samples/FilterRow/ColumnPlaygroundConfig.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> CustomTemplates { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/FilterRow/CustomTemplatesSampleView.xaml",
            "Views/Samples/FilterRow/CustomTemplatesSampleView.xaml.cs",
            "Views/Samples/FilterRow/CustomTemplatesSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        // ── Grouping ─────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> BasicGrouping { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Grouping/BasicGroupingSampleView.xaml",
            "Views/Samples/Grouping/BasicGroupingSampleView.xaml.cs",
            "Views/Samples/Grouping/BasicGroupingSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        // ── Summaries ────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> TotalSummaries { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Summaries/TotalSummariesSampleView.xaml",
            "Views/Samples/Summaries/TotalSummariesSampleView.xaml.cs",
            "Views/Samples/Summaries/TotalSummariesSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        // ── Editing ──────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> EditorTypes { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/EditorTypesSampleView.xaml",
            "Views/Samples/Editing/EditorTypesSampleView.xaml.cs",
            "Views/Samples/Editing/EditorTypesSampleViewModel.cs",
            "Models/TaskItem.cs",
            "SampleData/Generators/TaskGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> EditorCustomization { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/EditorCustomizationSampleView.xaml",
            "Views/Samples/Editing/EditorCustomizationSampleView.xaml.cs",
            "Views/Samples/Editing/EditorCustomizationSampleViewModel.cs",
            "Models/TaskItem.cs",
            "SampleData/Generators/TaskGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> InputMasking { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/InputMaskingSampleView.xaml",
            "Views/Samples/Editing/InputMaskingSampleView.xaml.cs",
            "Views/Samples/Editing/InputMaskingSampleViewModel.cs",
            "Models/Contact.cs",
            "SampleData/Generators/ContactGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> DataValidation { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/DataValidationSampleView.xaml",
            "Views/Samples/Editing/DataValidationSampleView.xaml.cs",
            "Views/Samples/Editing/DataValidationSampleViewModel.cs",
            "Models/ValidationData.cs");

        public static IReadOnlyList<SampleSourceFile> DataErrorIndication { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/DataErrorIndicationSampleView.xaml",
            "Views/Samples/Editing/DataErrorIndicationSampleView.xaml.cs",
            "Views/Samples/Editing/DataErrorIndicationSampleViewModel.cs",
            "Models/PersonInfo.cs");

        public static IReadOnlyList<SampleSourceFile> NewItemRow { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/NewItemRowSampleView.xaml",
            "Views/Samples/Editing/NewItemRowSampleView.xaml.cs",
            "Views/Samples/Editing/NewItemRowSampleViewModel.cs",
            "Models/TaskItem.cs");

        public static IReadOnlyList<SampleSourceFile> EditEntireRow { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/EditEntireRowSampleView.xaml",
            "Views/Samples/Editing/EditEntireRowSampleView.xaml.cs",
            "Views/Samples/Editing/EditEntireRowSampleViewModel.cs",
            "Models/EditRowItem.cs");

        public static IReadOnlyList<SampleSourceFile> EditForm { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/EditFormSampleView.xaml",
            "Views/Samples/Editing/EditFormSampleView.xaml.cs",
            "Views/Samples/Editing/EditFormSampleViewModel.cs",
            "Models/EditRowItem.cs");

        // ── Animation & Performance ──────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> ScrollingAnimation { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/AnimationPerformance/ScrollingAnimationSampleView.xaml",
            "Views/Samples/AnimationPerformance/ScrollingAnimationSampleView.xaml.cs",
            "Views/Samples/AnimationPerformance/ScrollingAnimationSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> LargeDatasets { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/AnimationPerformance/LargeDatasetsSampleView.xaml",
            "Views/Samples/AnimationPerformance/LargeDatasetsSampleView.xaml.cs",
            "Views/Samples/AnimationPerformance/LargeDatasetsSampleViewModel.cs",
            "SampleData/GeneratableSampleViewModel.cs",
            "Models/OrderItem.cs",
            "SampleData/Generators/OrderGenerator.cs");
    }
}
