using System.Collections.Generic;
using WWSearchDataGrid.Modern.SampleApp.SampleData;

namespace WWSearchDataGrid.Modern.SampleApp.Controls
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
        public static IReadOnlyList<SampleSourceFile> PocoAttributes { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/DataBinding/PocoAttributesSampleView.xaml",
            "Views/Samples/DataBinding/PocoAttributesSampleView.xaml.cs",
            "Views/Samples/DataBinding/PocoAttributesSampleViewModel.cs",
            "Models/Customer.cs",
            "SampleData/Generators/CustomerGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> DataTableManual { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/DataBinding/DataTableManualSampleView.xaml",
            "Views/Samples/DataBinding/DataTableManualSampleView.xaml.cs",
            "Views/Samples/DataBinding/DataTableManualSampleViewModel.cs",
            "SampleData/Generators/VendorTableGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> DataTableAutoGen { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/DataBinding/DataTableAutoGenSampleView.xaml",
            "Views/Samples/DataBinding/DataTableAutoGenSampleView.xaml.cs",
            "Views/Samples/DataBinding/DataTableAutoGenSampleViewModel.cs",
            "SampleData/Generators/VendorTableGenerator.cs");

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

        public static IReadOnlyList<SampleSourceFile> RuleFilterPopup { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Filtering/RuleFilterPopupSampleView.xaml",
            "Views/Samples/Filtering/RuleFilterPopupSampleView.xaml.cs",
            "Views/Samples/Filtering/RuleFilterPopupSampleViewModel.cs",
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

        public static IReadOnlyList<SampleSourceFile> SelectAll { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/SelectAllSampleView.xaml",
            "Views/Samples/Editing/SelectAllSampleView.xaml.cs",
            "Views/Samples/Editing/SelectAllSampleViewModel.cs",
            "Models/TaskItem.cs",
            "SampleData/Generators/TaskGenerator.cs");

        public static IReadOnlyList<SampleSourceFile> InputMasking { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editing/InputMaskingSampleView.xaml",
            "Views/Samples/Editing/InputMaskingSampleView.xaml.cs",
            "Views/Samples/Editing/InputMaskingSampleViewModel.cs",
            "Models/Contact.cs",
            "SampleData/Generators/ContactGenerator.cs");

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
