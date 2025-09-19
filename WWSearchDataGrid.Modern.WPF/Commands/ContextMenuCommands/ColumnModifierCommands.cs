using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    internal partial class ContextMenuCommands
    {
        #region Visibility & Layout Commands

        /// <summary>
        /// Opens the column management editor dialog
        /// </summary>
        public static ICommand ShowColumnEditorCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Column Editor - Not implemented");
            // TODO: Create and show ColumnEditor dialog
        }, grid => grid != null);

        /// <summary>
        /// Hides the selected column
        /// </summary>
        public static ICommand HideSelectedColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Hide Column: '{column?.Header}' - Not implemented");
            // TODO: Set column visibility to false
        }, column => column != null);

        /// <summary>
        /// Toggles the freeze state of a column (freezes if unfrozen, unfreezes if frozen)
        /// </summary>
        public static ICommand FreezeColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Freeze Column: '{column?.Header}' - Not implemented");
            // TODO: Set column to frozen state to keep it visible during horizontal scroll
        }, column => column != null);

        /// <summary>
        /// Toggles the freeze state of a column (unfreezes if frozen, freezes if unfrozen)
        /// </summary>
        public static ICommand UnfreezeColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Unfreeze Column: '{column?.Header}' - Not implemented");
            // TODO: Remove column from frozen state
        }, column => column != null);

        /// <summary>
        /// Toggles the pin state of a column (pins if unpinned, unpins if pinned)
        /// </summary>
        public static ICommand PinColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Pin Column: '{column?.Header}' - Not implemented");
            // TODO: Move column to pinned zone, persisting across layout changes
        }, column => column != null);

        /// <summary>
        /// Toggles the pin state of a column (unpins if pinned, pins if unpinned)
        /// </summary>
        public static ICommand UnpinColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Unpin Column: '{column?.Header}' - Not implemented");
            // TODO: Remove column from pinned zone
        }, column => column != null);

        #endregion

        #region Sizing & Alignment Commands

        /// <summary>
        /// Auto-sizes the current column to fit content
        /// </summary>
        public static ICommand BestFitColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Best Fit Column: '{column?.Header}' - Not implemented");
            // TODO: Auto-size column to fit content
        }, column => column != null);

        /// <summary>
        /// Auto-sizes all columns to fit content
        /// </summary>
        public static ICommand BestFitAllColumnsCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Best Fit All Columns - Not implemented");
            // TODO: Auto-size all columns to fit content
        }, grid => grid != null);

        /// <summary>
        /// Sets column alignment to left
        /// </summary>
        public static ICommand SetLeftAlignmentCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Set Left Alignment: Column '{column?.Header}' - Not implemented");
            // TODO: Set column text alignment to left
        }, column => column != null);

        /// <summary>
        /// Sets column alignment to center
        /// </summary>
        public static ICommand SetCenterAlignmentCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Set Center Alignment: Column '{column?.Header}' - Not implemented");
            // TODO: Set column text alignment to center
        }, column => column != null);

        /// <summary>
        /// Sets column alignment to right
        /// </summary>
        public static ICommand SetRightAlignmentCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Set Right Alignment: Column '{column?.Header}' - Not implemented");
            // TODO: Set column text alignment to right
        }, column => column != null);

        #endregion

        #region Sorting Commands

        #region Sorting Commands Implementation

        /// <summary>
        /// Sorts the column in ascending order
        /// </summary>
        public static ICommand SortAscendingCommand => new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                ApplySorting(context.Grid, context.Column, ListSortDirection.Ascending);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SortAscendingCommand: {ex.Message}");
                MessageBox.Show($"Error sorting column: {ex.Message}", "Sort Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }, context => CanSortColumn(context.Column));

        /// <summary>
        /// Sorts the column in descending order
        /// </summary>
        public static ICommand SortDescendingCommand => new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                ApplySorting(context.Grid, context.Column, ListSortDirection.Descending);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SortDescendingCommand: {ex.Message}");
                MessageBox.Show($"Error sorting column: {ex.Message}", "Sort Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }, context => CanSortColumn(context.Column));

        #endregion

        #region Helper Methods for Sorting

        /// <summary>
        /// Determines if a column can be sorted
        /// </summary>
        private static bool CanSortColumn(DataGridColumn column)
        {
            try
            {
                return column.CanUserSort && !string.IsNullOrEmpty(column.SortMemberPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Applies sorting to the specified column in the given direction
        /// </summary>
        private static void ApplySorting(SearchDataGrid grid, DataGridColumn column, ListSortDirection direction)
        {
            if (grid?.Items == null || column == null || string.IsNullOrEmpty(column.SortMemberPath))
                return;

            try
            {
                // Get the collection view
                var collectionView = CollectionViewSource.GetDefaultView(grid.Items);
                if (collectionView == null)
                    return;

                // Clear existing sort descriptions
                collectionView.SortDescriptions.Clear();

                // Add new sort description
                collectionView.SortDescriptions.Add(new SortDescription(column.SortMemberPath, direction));

                // Update the DataGrid's sort direction indicator
                foreach (var col in grid.Columns)
                {
                    col.SortDirection = null;
                }
                column.SortDirection = direction;

                // Refresh the view to apply sorting
                collectionView.Refresh();

                Debug.WriteLine($"Applied {direction} sort to column '{column.Header}' using path '{column.SortMemberPath}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying sort: {ex.Message}");
                throw;
            }
        }

        #endregion

        #endregion 

    }
}
