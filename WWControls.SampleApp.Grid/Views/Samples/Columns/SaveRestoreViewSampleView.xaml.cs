using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WWControls.Core;

namespace WWControls.SampleApp.Grid.Views.Samples.Columns
{
    public partial class SaveRestoreViewSampleView : UserControl
    {
        public SaveRestoreViewSampleView() => InitializeComponent();

        // Selecting a built-in view loads its embedded .sdgview and applies it to the live grid.
        // ApplyViewState only touches the sections the file carries, so a filters-only preset leaves
        // the columns where they are, and a layout-only preset leaves the filters untouched.
        private void PresetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetList.SelectedItem is not ViewPreset preset) return;
            var state = LoadEmbeddedPreset(preset.ResourceFile);
            if (state != null) Grid.ApplyViewState(state);
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e) => Grid.ResetLayoutToDefaults();

        // Clearing filters is just applying an empty filter set — ApplyViewState clears the existing
        // filters before applying, so an empty Filters section leaves the grid unfiltered.
        private void ClearFilters_Click(object sender, RoutedEventArgs e) =>
            Grid.ApplyViewState(new GridViewState { Filters = new GridFilterState() }, applyLayout: false, applyFilters: true);

        /// <summary>Reads a bundled <c>.sdgview</c> file (embedded resource) and deserializes it.</summary>
        private static GridViewState LoadEmbeddedPreset(string fileName)
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceName = Array.Find(
                asm.GetManifestResourceNames(),
                n => n.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) return null;

            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return GridViewStateSerializer.Deserialize(reader.ReadToEnd());
        }
    }
}
