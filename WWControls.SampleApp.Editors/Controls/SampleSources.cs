using System.Collections.Generic;
using WWControls.SampleApp.Editors.SampleData;

namespace WWControls.SampleApp.Editors.Controls
{
    /// <summary>
    /// One source-file list per sample, exposed as <c>x:Static</c>-friendly properties so XAML can
    /// bind <c>SampleHostControl.Sources</c> without per-sample resource-loading boilerplate. The
    /// combobox in the Source tab walks the list; each entry has its own AvalonEdit syntax
    /// highlighting (XML for .xaml, C# for .cs).
    /// </summary>
    public static class SampleSources
    {
        // ── Overview ─────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> EditorGallery { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editors/EditorGallerySampleView.xaml",
            "Views/Samples/Editors/EditorGallerySampleView.xaml.cs",
            "Views/Samples/Editors/EditorGallerySampleViewModel.cs");

        // ── Editors ──────────────────────────────────────────────────────────
        public static IReadOnlyList<SampleSourceFile> TextBox { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editors/TextBoxSampleView.xaml",
            "Views/Samples/Editors/TextBoxSampleView.xaml.cs",
            "Views/Samples/Editors/TextBoxSampleViewModel.cs");

        public static IReadOnlyList<SampleSourceFile> NumericUpDown { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editors/NumericUpDownSampleView.xaml",
            "Views/Samples/Editors/NumericUpDownSampleView.xaml.cs",
            "Views/Samples/Editors/NumericUpDownSampleViewModel.cs");

        public static IReadOnlyList<SampleSourceFile> ComboBox { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editors/ComboBoxSampleView.xaml",
            "Views/Samples/Editors/ComboBoxSampleView.xaml.cs",
            "Views/Samples/Editors/ComboBoxSampleViewModel.cs");

        public static IReadOnlyList<SampleSourceFile> DatePicker { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editors/DatePickerSampleView.xaml",
            "Views/Samples/Editors/DatePickerSampleView.xaml.cs",
            "Views/Samples/Editors/DatePickerSampleViewModel.cs");

        public static IReadOnlyList<SampleSourceFile> CheckBox { get; } = SampleSourceLoader.LoadFiles(
            "Views/Samples/Editors/CheckBoxSampleView.xaml",
            "Views/Samples/Editors/CheckBoxSampleView.xaml.cs",
            "Views/Samples/Editors/CheckBoxSampleViewModel.cs");
    }
}
