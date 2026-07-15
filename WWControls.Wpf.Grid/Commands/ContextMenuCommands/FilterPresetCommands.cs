using System.Linq;
using System.Windows;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        /// <summary>
        /// Saves the current set of filters (only) to a <c>.sdgview</c> file. Layout is not included,
        /// so the preset can be applied to any grid without disturbing its columns.
        /// </summary>
        private static ICommand _saveFilterPresetCommand;
        public static ICommand SaveFilterPresetCommand => _saveFilterPresetCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            if (grid == null) return;
            SaveViewStateToFile(grid, grid.CaptureViewState(includeLayout: false, includeFilters: true), "Save filter preset");
        }, grid => grid?.DataColumns?.Any(c => c.HasActiveFilter) == true);

        /// <summary>
        /// Loads a saved preset and applies only its filters, leaving the grid's layout untouched.
        /// </summary>
        private static ICommand _loadFilterPresetCommand;
        public static ICommand LoadFilterPresetCommand => _loadFilterPresetCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            if (grid == null) return;
            var state = LoadViewStateFromFile(grid, "Load filter preset");
            if (state == null) return;
            if (state.Filters == null)
            {
                MessageBox.Show(
                    "The selected file does not contain any saved filters.",
                    "Load filter preset", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            grid.ApplyViewState(state, applyLayout: false, applyFilters: true);
        }, grid => grid != null);
    }
}
