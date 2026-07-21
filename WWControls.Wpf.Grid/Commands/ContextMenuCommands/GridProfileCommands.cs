using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        #region Visibility & Layout Commands

        private const string ViewFileExtension = ".sdgview";
        private const string ViewFileFilter = "Grid view (*.sdgview)|*.sdgview|All files (*.*)|*.*";

        /// <summary>
        /// Resets the grid's columns (order, widths, visibility, pinning), sorting, and grouping to
        /// the layout captured when the grid first generated its columns.
        /// </summary>
        private static ICommand _resetLayoutCommand;
        public static ICommand ResetLayoutCommand => _resetLayoutCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            grid?.ResetLayoutToDefaults();
        }, grid => grid != null);

        /// <summary>
        /// Saves the current grid layout — column order/width/visibility/pinning, sorting, and
        /// grouping — to a file the user chooses, defaulting to the grid's configured preset
        /// directory. Filters are not included.
        /// </summary>
        private static ICommand _saveLayoutCommand;
        public static ICommand SaveLayoutCommand => _saveLayoutCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            if (grid == null) return;
            SaveViewStateToFile(grid, grid.CaptureViewState(includeLayout: true, includeFilters: false), "Save layout");
        }, grid => grid != null);

        /// <summary>
        /// Loads a saved layout from a file and applies it, leaving any active filters untouched.
        /// </summary>
        private static ICommand _loadLayoutCommand;
        public static ICommand LoadLayoutCommand => _loadLayoutCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            if (grid == null) return;
            var state = LoadViewStateFromFile(grid, "Load layout");
            if (state == null) return;
            if (state.Layout == null)
            {
                MessageBox.Show(
                    "The selected file does not contain a saved layout.",
                    "Load layout", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            grid.ApplyViewState(state, applyLayout: true, applyFilters: false);
        }, grid => grid != null);

        #endregion

        #region Shared view-state file I/O

        /// <summary>
        /// Prompts for a location and writes <paramref name="state"/> as a <c>.sdgview</c> file.
        /// Shared by the profile (layout+filters) and filter-preset commands.
        /// </summary>
        internal static void SaveViewStateToFile(SearchDataGrid grid, GridViewState state, string title)
        {
            if (grid == null || state == null) return;

            var dialog = new SaveFileDialog
            {
                Title = title,
                DefaultExt = ViewFileExtension,
                Filter = ViewFileFilter,
                AddExtension = true,
                InitialDirectory = ResolveInitialSaveDirectory(grid),
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                state.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
                var json = GridViewStateSerializer.Serialize(state);
                GridViewStateFile.WriteAtomic(dialog.FileName, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save grid view failed: {ex.Message}");
                MessageBox.Show(
                    $"Could not save the grid view.\n\n{ex.Message}",
                    title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Prompts for a <c>.sdgview</c> file and deserializes it. Returns <c>null</c> when the user
        /// cancels or the file can't be read/parsed (a message is shown in the latter case).
        /// </summary>
        internal static GridViewState LoadViewStateFromFile(SearchDataGrid grid, string title)
        {
            if (grid == null) return null;

            var dialog = new OpenFileDialog
            {
                Title = title,
                DefaultExt = ViewFileExtension,
                Filter = ViewFileFilter,
                CheckFileExists = true,
                InitialDirectory = ResolveInitialLoadDirectory(grid),
            };
            if (dialog.ShowDialog() != true) return null;

            try
            {
                var state = GridViewStateSerializer.Deserialize(GridViewStateFile.ReadText(dialog.FileName));
                if (state == null)
                {
                    MessageBox.Show(
                        "The selected file is empty or not a valid grid view.",
                        title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return state;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load grid view failed: {ex.Message}");
                MessageBox.Show(
                    $"Could not load the grid view.\n\n{ex.Message}",
                    title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        /// <summary>
        /// Resolves (and creates) the directory the Save dialog opens to. Returns null when it can't
        /// be resolved, letting the dialog fall back to its own default.
        /// </summary>
        private static string ResolveInitialSaveDirectory(SearchDataGrid grid)
        {
            try
            {
                var dir = grid.ResolveEffectivePresetDirectory();
                if (string.IsNullOrWhiteSpace(dir)) return null;
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Resolve preset directory failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>Resolves the Load dialog's starting directory, or null if it doesn't exist yet.</summary>
        private static string ResolveInitialLoadDirectory(SearchDataGrid grid)
        {
            try
            {
                var dir = grid.ResolveEffectivePresetDirectory();
                return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) ? dir : null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
