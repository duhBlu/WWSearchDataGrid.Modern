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
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        #region Visibility & Layout Commands

        private static ICommand _hideSelectedColumnCommand;
        /// <summary>
        /// Hides the selected column
        /// </summary>
        public static ICommand HideSelectedColumnCommand => _hideSelectedColumnCommand ??= new RelayCommand<DataGridColumn>(column =>
        {
            try
            {
                if (column != null)
                {
                    // Set the column visibility to collapsed
                    column.Visibility = Visibility.Collapsed;

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.Content is ColumnChooser columnChooser)
                        {
                            var columnInfo = columnChooser.Columns.FirstOrDefault(c => c.Column == column);
                            if (columnInfo != null)
                            {
                                columnInfo.IsVisible = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding column '{column?.Header}': {ex.Message}");
            }
        }, column => column != null && column.Visibility == Visibility.Visible);

        #endregion

        #region Fixed (Pinned) Column Commands

        private static ICommand _pinColumnLeftCommand;
        /// <summary>
        /// Pins the context column to the left edge of the grid by setting its
        /// <see cref="GridColumn.Fixed"/> descriptor to <see cref="FixedColumnPosition.Left"/>.
        /// </summary>
        public static ICommand PinColumnLeftCommand => _pinColumnLeftCommand ??=
            new RelayCommand<ContextMenuContext>(
                context => SetColumnFixedPosition(context, FixedColumnPosition.Left),
                context => CanSetColumnFixedPosition(context, FixedColumnPosition.Left));

        private static ICommand _pinColumnRightCommand;
        /// <summary>
        /// Pins the context column to the right end of the grid by setting its
        /// <see cref="GridColumn.Fixed"/> descriptor to <see cref="FixedColumnPosition.Right"/>.
        /// </summary>
        public static ICommand PinColumnRightCommand => _pinColumnRightCommand ??=
            new RelayCommand<ContextMenuContext>(
                context => SetColumnFixedPosition(context, FixedColumnPosition.Right),
                context => CanSetColumnFixedPosition(context, FixedColumnPosition.Right));

        private static ICommand _unpinColumnCommand;
        /// <summary>
        /// Clears the context column's <see cref="GridColumn.Fixed"/> descriptor
        /// (sets it to <see cref="FixedColumnPosition.None"/>).
        /// </summary>
        public static ICommand UnpinColumnCommand => _unpinColumnCommand ??=
            new RelayCommand<ContextMenuContext>(
                context => SetColumnFixedPosition(context, FixedColumnPosition.None),
                context => CanSetColumnFixedPosition(context, FixedColumnPosition.None));

        private static void SetColumnFixedPosition(ContextMenuContext context, FixedColumnPosition position)
        {
            try
            {
                var descriptor = ResolveDescriptor(context);
                if (descriptor == null) return;
                descriptor.Fixed = position;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting column Fixed={position}: {ex.Message}");
            }
        }

        private static bool CanSetColumnFixedPosition(ContextMenuContext context, FixedColumnPosition position)
        {
            if (context?.Grid == null || context.Column == null) return false;
            var descriptor = ResolveDescriptor(context);
            if (descriptor == null) return false;
            return descriptor.Fixed != position;
        }

        private static GridColumn ResolveDescriptor(ContextMenuContext context)
            => context?.Grid?.FindGridColumnDescriptor(context.Column);

        #endregion

        #region Sizing & Alignment Commands

        #region Best Fit Commands

        private static ICommand _bestFitColumnCommand;
        /// <summary>
        /// Auto-sizes the current column to fit content. Disabled when the column resolves
        /// <see cref="ColumnLayoutBase.ActualAllowBestFit"/> to <c>false</c>.
        /// </summary>
        public static ICommand BestFitColumnCommand => _bestFitColumnCommand ??= new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                var descriptor = ResolveDescriptor(context);
                if (descriptor != null)
                    context.Grid.BestFitColumn(descriptor);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BestFitColumn error: {ex}");
            }
        }, context => context?.Grid != null && context.Column != null
            && ResolveDescriptor(context) is { ActualAllowBestFit: true });

        private static ICommand _bestFitAllColumnsCommand;
        /// <summary>
        /// Auto-sizes all columns to fit content (columns opted out via
        /// <see cref="ColumnLayoutBase.AllowBestFit"/> are skipped). Disabled when the grid-level
        /// <see cref="SearchDataGrid.AllowBestFit"/> is <c>false</c>.
        /// </summary>
        public static ICommand BestFitAllColumnsCommand => _bestFitAllColumnsCommand ??= new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                context.Grid.BestFitAllColumns();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BestFitAllColumns error: {ex}");
            }
        }, context => context?.Grid is { AllowBestFit: true });

        #endregion

        #endregion

        #region Sorting

        #region Sorting Commands Implementation

        private static ICommand _sortAscendingCommand;
        /// <summary>
        /// Sorts the column in ascending order
        /// </summary>
        public static ICommand SortAscendingCommand => _sortAscendingCommand ??= new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                ApplySorting(context.Grid, context.Column, ListSortDirection.Ascending);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SortAscendingCommand: {ex.Message}");
            }
        }, context => context.Column != null
            && context.Column.SortDirection != ListSortDirection.Ascending
            && CanSortColumn(context.Column)
            && IsSortDirectionAllowed(context, AllowedSortOrders.Ascending));

        private static ICommand _sortDescendingCommand;
        /// <summary>
        /// Sorts the column in descending order
        /// </summary>
        public static ICommand SortDescendingCommand => _sortDescendingCommand ??= new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                ApplySorting(context.Grid, context.Column, ListSortDirection.Descending);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SortDescendingCommand: {ex.Message}");
            }
        }, context => context.Column != null
            && context.Column.SortDirection != ListSortDirection.Descending
            && CanSortColumn(context.Column)
            && IsSortDirectionAllowed(context, AllowedSortOrders.Descending));

        /// <summary>
        /// True when the resolved <see cref="GridColumn.AllowedSortOrders"/> includes
        /// <paramref name="direction"/>. Columns without a descriptor (manually added
        /// <see cref="DataGridColumn"/> instances) default to allowed.
        /// </summary>
        private static bool IsSortDirectionAllowed(ContextMenuContext context, AllowedSortOrders direction)
        {
            var descriptor = ResolveDescriptor(context);
            if (descriptor == null) return true;
            return (descriptor.AllowedSortOrders & direction) != 0;
        }
        
        private static ICommand _clearSortingCommand;
        /// <summary>
        /// Sorts the column in descending order
        /// </summary>
        public static ICommand ClearSortingCommand => _clearSortingCommand ??= new RelayCommand<ContextMenuContext>(context =>
        {
            try
            {
                // Grouping owns ordering through its projection; clear user sorts there and
                // leave grouped columns' group sort intact (D2).
                if (context.Grid.IsGroupingActive)
                {
                    context.Grid.ClearColumnSortsFromMenu();
                    return;
                }

                var view = CollectionViewSource.GetDefaultView(context.Grid.ItemsSource);
                if(view != null)
                {
                    view.SortDescriptions.Clear();
                    view.Refresh();
                }
                else
                {
                    context.Grid.Items.SortDescriptions.Clear();
                    context.Grid.Items.Refresh();

                }
                foreach (var c in context.Grid.Columns)
                    c.SortDirection = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SortDescendingCommand: {ex.Message}");
            }
        }, context => context.Column != null && context.Grid != null && context.Column.SortDirection != null);

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

            // Grouping shapes order upstream — mutating the displayed view's SortDescriptions
            // would re-sort the header sentinels. Route through the projection instead.
            if (grid.IsGroupingActive)
            {
                grid.SortColumnFromMenu(column, direction);
                return;
            }

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
