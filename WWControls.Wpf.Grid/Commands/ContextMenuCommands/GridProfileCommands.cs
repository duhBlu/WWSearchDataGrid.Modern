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
        /// Saves the current grid view (layout today; filters added in a later phase) to a file the
        /// user chooses, defaulting to the grid's configured preset directory.
        /// </summary>
        private static ICommand _saveCurrentProfileCommand;
        public static ICommand SaveCurrentProfileCommand => _saveCurrentProfileCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            if (grid == null) return;

            var initialDir = ResolveInitialSaveDirectory(grid);
            var dialog = new SaveFileDialog
            {
                Title = "Save grid view",
                DefaultExt = ViewFileExtension,
                Filter = ViewFileFilter,
                AddExtension = true,
                InitialDirectory = initialDir,
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var state = grid.CaptureViewState();
                state.Name = Path.GetFileNameWithoutExtension(dialog.FileName);
                var json = GridViewStateSerializer.Serialize(state);
                GridViewStateFile.WriteAtomic(dialog.FileName, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save grid view failed: {ex.Message}");
                MessageBox.Show(
                    $"Could not save the grid view.\n\n{ex.Message}",
                    "Save grid view", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }, grid => grid != null);

        /// <summary>
        /// Loads a saved grid view from a file and applies whichever sections it contains
        /// (layout / filters / both).
        /// </summary>
        private static ICommand _loadProfileCommand;
        public static ICommand LoadProfileCommand => _loadProfileCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            if (grid == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "Load grid view",
                DefaultExt = ViewFileExtension,
                Filter = ViewFileFilter,
                CheckFileExists = true,
                InitialDirectory = ResolveInitialLoadDirectory(grid),
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var json = GridViewStateFile.ReadText(dialog.FileName);
                var state = GridViewStateSerializer.Deserialize(json);
                if (state == null)
                {
                    MessageBox.Show(
                        "The selected file is empty or not a valid grid view.",
                        "Load grid view", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                grid.ApplyViewState(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load grid view failed: {ex.Message}");
                MessageBox.Show(
                    $"Could not load the grid view.\n\n{ex.Message}",
                    "Load grid view", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }, grid => grid != null);

        /// <summary>
        /// Opens the profile management dialog. Not implemented — there is no managed preset catalog
        /// in the file-based model; views are saved and loaded as individual files.
        /// </summary>
        private static ICommand _manageProfilesCommand;
        public static ICommand ManageProfilesCommand => _manageProfilesCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Manage Profiles - not applicable to the file-based model");
        }, grid => false);

        /// <summary>
        /// Resolves (and creates) the directory the Save dialog opens to. Returns null when the
        /// directory can't be resolved, letting the dialog fall back to its own default.
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
