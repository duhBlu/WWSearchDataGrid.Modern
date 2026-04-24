using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Select All Column Support

        /// Sets up select-all checkboxes in column headers
        /// </summary>
        internal void SetupSelectAllColumnHeaders()
        {
            try
            {
                // Find all column headers
                var headersPresenter = VisualTreeHelperMethods.FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return;

                var headers = VisualTreeHelperMethods.FindVisualChildren<DataGridColumnHeader>(headersPresenter).ToList();

                foreach (var header in headers)
                {
                    var column = header.Column;
                    if (column == null)
                        continue;

                    // Check if this column has IsSelectAllColumn enabled and is boolean type
                    bool isSelectAllColumn = (FindGridColumnDescriptor(column)?.IsSelectAllColumn ?? false);
                    bool isBooleanColumn = IsColumnBooleanType(column);

                    // Find the select-all checkbox in the header
                    var checkbox = VisualTreeHelperMethods.FindVisualChild<CheckBox>(header, "PART_SelectAllCheckBox");
                    if (checkbox == null)
                        continue;

                    // Find the row count TextBlock in the header
                    var rowCountTextBlock = VisualTreeHelperMethods.FindVisualChild<TextBlock>(header, "PART_SelectAllRowCount");

                    // Show checkbox only if both conditions are met
                    if (isSelectAllColumn && isBooleanColumn)
                    {
                        checkbox.Visibility = Visibility.Visible;

                        // Wire up click event (remove old handler first to prevent duplicates)
                        checkbox.Click -= OnSelectAllCheckboxClicked;
                        checkbox.Click += OnSelectAllCheckboxClicked;

                        // Set initial state
                        checkbox.IsChecked = CalculateSelectAllCheckboxState(column);

                        // Show/hide row count based on scope
                        var scope = (FindGridColumnDescriptor(column)?.SelectAllScope ?? SelectAllScope.FilteredRows);
                        if (rowCountTextBlock != null)
                        {
                            if (scope == SelectAllScope.SelectedRows)
                            {
                                rowCountTextBlock.Visibility = Visibility.Visible;
                                // Initialize the count
                                var items = GetItemsForSelectAllScope(scope);
                                rowCountTextBlock.Text = $"({items?.Count() ?? 0})";
                            }
                            else
                            {
                                rowCountTextBlock.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        checkbox.Visibility = Visibility.Collapsed;
                        checkbox.Click -= OnSelectAllCheckboxClicked;

                        if (rowCountTextBlock != null)
                            rowCountTextBlock.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up select-all column headers: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles click events on select-all checkboxes
        /// </summary>
        private void OnSelectAllCheckboxClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not CheckBox checkbox)
                    return;

                // Find the column header that contains this checkbox
                var header = FindVisualParent<DataGridColumnHeader>(checkbox);
                if (header?.Column == null)
                    return;

                // Toggle all values in the column
                ToggleSelectAllColumn(header.Column);

                // Update the checkbox state to reflect the new data state
                checkbox.IsChecked = CalculateSelectAllCheckboxState(header.Column);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling select-all checkbox click: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds a parent of a specific type in the visual tree
        /// </summary>
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            try
            {
                DependencyObject parentObject = VisualTreeHelper.GetParent(child);

                if (parentObject == null)
                    return null;

                if (parentObject is T parent)
                    return parent;

                return FindVisualParent<T>(parentObject);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Handles selection change events to update row count displays
        /// </summary>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Update row count displays for columns using SelectedRows scope
                UpdateSelectAllRowCountForAllColumns();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSelectionChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles selected cells change events to update row count displays
        /// </summary>
        private void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                // Update row count displays for columns using SelectedRows scope
                UpdateSelectAllRowCountForAllColumns();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSelectedCellsChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates row count displays and checkbox states for all columns using SelectedRows scope
        /// </summary>
        private void UpdateSelectAllRowCountForAllColumns()
        {
            try
            {
                // Find all column headers
                var headersPresenter = VisualTreeHelperMethods.FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return;

                var headers = VisualTreeHelperMethods.FindVisualChildren<DataGridColumnHeader>(headersPresenter).ToList();

                foreach (var header in headers)
                {
                    var column = header.Column;
                    if (column == null)
                        continue;

                    // Check if this column has IsSelectAllColumn enabled
                    bool isSelectAllColumn = (FindGridColumnDescriptor(column)?.IsSelectAllColumn ?? false);
                    if (!isSelectAllColumn)
                        continue;

                    // Get the scope for this column
                    var scope = (FindGridColumnDescriptor(column)?.SelectAllScope ?? SelectAllScope.FilteredRows);

                    // Update row count display if using SelectedRows scope
                    if (scope == SelectAllScope.SelectedRows)
                    {
                        // Find the row count TextBlock
                        var countTextBlock = VisualTreeHelperMethods.FindVisualChild<TextBlock>(header, "PART_SelectAllRowCount");
                        if (countTextBlock != null && countTextBlock.Visibility == Visibility.Visible)
                        {
                            // Update the count
                            var items = GetItemsForSelectAllScope(scope);
                            countTextBlock.Text = $"({items?.Count() ?? 0})";
                        }
                    }

                    // Update checkbox state for SelectedRows scope (checkbox state should reflect selected items)
                    if (scope == SelectAllScope.SelectedRows)
                    {
                        // Find the select-all checkbox in the header
                        var checkbox = VisualTreeHelperMethods.FindVisualChild<CheckBox>(header, "PART_SelectAllCheckBox");
                        if (checkbox != null && checkbox.Visibility == Visibility.Visible)
                        {
                            // Update the checkbox state based on the selected rows
                            checkbox.IsChecked = CalculateSelectAllCheckboxState(column);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating select-all row counts: {ex.Message}");
            }
        }



        /// <summary>
        /// Determines if a column is a boolean type by checking the descriptor's FieldType,
        /// the column type (DataGridCheckBoxColumn), or the UseCheckBoxInSearchBox property.
        /// </summary>
        private bool IsColumnBooleanType(DataGridColumn column)
        {
            if (column is DataGridCheckBoxColumn)
                return true;

            var descriptor = FindGridColumnDescriptor(column);
            if (descriptor != null)
            {
                if (descriptor.FieldType == typeof(bool) || descriptor.FieldType == typeof(bool?))
                    return true;
                if (descriptor.UseCheckBoxInSearchBox)
                    return true;
            }

            // Check SearchTemplateController for data-detected type
            var columnSearchBox = DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
            if (columnSearchBox?.SearchTemplateController != null)
                return columnSearchBox.SearchTemplateController.ColumnDataType == Core.ColumnDataType.Boolean;

            return false;
        }

        /// <summary>
        /// Gets the binding path for a column, respecting FilterMemberPath, SortMemberPath, and Binding.Path priority
        /// </summary>
        /// <param name="column">The column to get the binding path for</param>
        /// <returns>The resolved binding path, or null if none can be determined</returns>
        internal string GetColumnBindingPathForSelectAll(DataGridColumn column)
        {
            if (column == null)
                return null;

            // Priority 1: FilterMemberPath
            string bindingPath = FindGridColumnDescriptor(column)?.FilterMemberPath;

            // Priority 2: SortMemberPath
            if (string.IsNullOrEmpty(bindingPath))
                bindingPath = column.SortMemberPath;

            // Priority 3: Binding path
            if (string.IsNullOrEmpty(bindingPath) && column is DataGridBoundColumn boundColumn)
            {
                bindingPath = (boundColumn.Binding as Binding)?.Path?.Path;
            }

            return bindingPath;
        }

        /// <summary>
        /// Calculates the checkbox state for a select-all column based on the SelectAllScope setting.
        /// Returns: true (all non-null values are true), false (all non-null values are false),
        /// or null (mixed state or all null values)
        /// </summary>
        /// <param name="column">The column to calculate state for</param>
        /// <returns>The checkbox state: true, false, or null for indeterminate</returns>
        internal bool? CalculateSelectAllCheckboxState(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return null;

                string bindingPath = GetColumnBindingPathForSelectAll(column);
                if (string.IsNullOrEmpty(bindingPath))
                    return null;

                // Get the scope for this column to determine which items to evaluate
                SelectAllScope scope = (FindGridColumnDescriptor(column)?.SelectAllScope ?? SelectAllScope.FilteredRows);

                // Get items based on scope
                var itemsToCheck = GetItemsForSelectAllScope(scope);
                if (itemsToCheck == null || !itemsToCheck.Any())
                    return null;

                // Calculate state using the scoped items
                return CalculateSelectAllCheckboxStateForItems(itemsToCheck, bindingPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating select-all checkbox state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Toggles all non-null boolean values in a column to the opposite state.
        /// Null values are preserved unchanged.
        /// Respects the SelectAllScope property to determine which items are affected.
        /// </summary>
        /// <param name="column">The column to toggle values in</param>
        internal void ToggleSelectAllColumn(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return;

                string bindingPath = GetColumnBindingPathForSelectAll(column);
                if (string.IsNullOrEmpty(bindingPath))
                    return;

                // Get the scope for this column
                SelectAllScope scope = (FindGridColumnDescriptor(column)?.SelectAllScope ?? SelectAllScope.FilteredRows);

                // Get the items to operate on based on scope
                var itemsToToggle = GetItemsForSelectAllScope(scope);
                if (itemsToToggle == null || !itemsToToggle.Any())
                    return;

                // Calculate current state based on scoped items
                var currentState = CalculateSelectAllCheckboxStateForItems(itemsToToggle, bindingPath);

                // Determine new value:
                // - If all true (currentState == true), set all to false
                // - If all false (currentState == false), set all to true
                // - If mixed (currentState == null), set all to true (consistent behavior)
                bool newValue = currentState != true;

                // Save current selection state before making changes
                List<object> savedSelectedItems = null;
                List<DataGridCellInfo> savedSelectedCells = null;

                if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                {
                    // Save selected cells
                    savedSelectedCells = SelectedCells.ToList();
                }
                else
                {
                    // Save selected items
                    savedSelectedItems = SelectedItems.Cast<object>().ToList();
                }

                // Toggle all non-null values in the scoped items
                foreach (var item in itemsToToggle)
                {
                    var value = ReflectionHelper.GetPropValue(item, bindingPath);

                    // Only modify boolean values
                    if (value == null || value is bool?)
                    {
                        ReflectionHelper.SetPropValue(item, bindingPath, newValue);
                    }
                }

                // Refresh the view to show changes
                Items.Refresh();

                // Update filter caches if filtering is active
                if (HasActiveColumnFilters())
                {
                    InvalidateCollectionContextCache();
                }

                // Refilter the datagrid to apply the new values
                FilterItemsSource();
                UpdateFilterPanel();

                // Restore selection after all refresh operations complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                        {
                            // Restore cell selection
                            if (savedSelectedCells != null && savedSelectedCells.Count > 0)
                            {
                                SelectedCells.Clear();
                                foreach (var cellInfo in savedSelectedCells)
                                {
                                    // Only restore cells that still exist in the filtered view
                                    if (cellInfo.Item != null && Items.Contains(cellInfo.Item))
                                    {
                                        SelectedCells.Add(cellInfo);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Restore row selection
                            if (savedSelectedItems != null && savedSelectedItems.Count > 0)
                            {
                                SelectedItems.Clear();
                                foreach (var item in savedSelectedItems)
                                {
                                    // Only restore items that still exist in the filtered view
                                    if (Items.Contains(item))
                                    {
                                        SelectedItems.Add(item);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error restoring selection: {ex.Message}");
                    }
                }), DispatcherPriority.DataBind);

                // Sync ColumnSearchBox filter if UseCheckBoxInSearchBox is true and a filter is active
                SyncColumnSearchBoxFilterWithSelectAll(column, newValue);

                // Update row count display if using SelectedRows scope
                if (scope == SelectAllScope.SelectedRows)
                {
                    UpdateSelectAllRowCountDisplay(column, itemsToToggle.Count());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling select-all column: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the items to operate on based on the SelectAllScope
        /// </summary>
        /// <param name="scope">The scope defining which items to include</param>
        /// <returns>Collection of items to operate on</returns>
        private IEnumerable<object> GetItemsForSelectAllScope(SelectAllScope scope)
        {
            switch (scope)
            {
                case SelectAllScope.FilteredRows:
                    // Return currently visible/filtered items
                    return Items.Cast<object>();

                case SelectAllScope.SelectedRows:
                    // Return selected items (handles both row and cell selection)
                    if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                    {
                        // For cell-based selection, get unique row items from selected cells
                        var selectedItems = new HashSet<object>();
                        foreach (var cell in SelectedCells)
                        {
                            if (cell.Item != null)
                                selectedItems.Add(cell.Item);
                        }
                        return selectedItems;
                    }
                    else
                    {
                        // For row-based selection, return selected items
                        return SelectedItems.Cast<object>();
                    }

                case SelectAllScope.AllItems:
                    // Return all items from the original unfiltered source
                    if (originalItemsSource != null)
                        return originalItemsSource.Cast<object>();
                    return Enumerable.Empty<object>();

                default:
                    return Items.Cast<object>();
            }
        }

        /// <summary>
        /// Calculates the checkbox state for a specific set of items
        /// </summary>
        /// <param name="items">The items to evaluate</param>
        /// <param name="bindingPath">The property path to check</param>
        /// <returns>The checkbox state: true, false, or null for indeterminate</returns>
        private bool? CalculateSelectAllCheckboxStateForItems(IEnumerable<object> items, string bindingPath)
        {
            try
            {
                if (items == null || !items.Any())
                    return null;

                if (string.IsNullOrEmpty(bindingPath))
                    return null;

                int trueCount = 0;
                int falseCount = 0;
                int totalNonNull = 0;

                foreach (var item in items)
                {
                    var value = ReflectionHelper.GetPropValue(item, bindingPath);

                    if (value == null)
                        continue; // Skip null values

                    totalNonNull++;

                    if (value is bool boolValue)
                    {
                        if (boolValue)
                            trueCount++;
                        else
                            falseCount++;
                    }
                }

                // If no non-null values, return indeterminate
                if (totalNonNull == 0)
                    return null;

                // All non-null values are true
                if (trueCount == totalNonNull)
                    return true;

                // All non-null values are false
                if (falseCount == totalNonNull)
                    return false;

                // Mixed state
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating checkbox state for items: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronizes the ColumnSearchBox checkbox filter with the new select-all state
        /// </summary>
        /// <param name="column">The column to sync</param>
        /// <param name="newValue">The new boolean value that was applied</param>
        private void SyncColumnSearchBoxFilterWithSelectAll(DataGridColumn column, bool newValue)
        {
            try
            {
                // Check if UseCheckBoxInSearchBox is enabled for this column
                if (!(FindGridColumnDescriptor(column)?.UseCheckBoxInSearchBox ?? false))
                    return;

                // Find the ColumnSearchBox for this column
                var columnSearchBox = DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                if (columnSearchBox == null)
                    return;

                // Check if there's an active filter
                if (!columnSearchBox.HasActiveFilter)
                    return;

                // Update the FilterCheckboxState to match the new value
                // Setting this property will automatically update the filter
                if (newValue)
                    columnSearchBox.FilterCheckboxState = true;
                else
                    columnSearchBox.FilterCheckboxState = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing ColumnSearchBox filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the row count display next to the select-all checkbox
        /// </summary>
        /// <param name="column">The column to update</param>
        /// <param name="count">The count of affected rows</param>
        private void UpdateSelectAllRowCountDisplay(DataGridColumn column, int count)
        {
            try
            {
                var columnHeader = FindColumnHeader(column);
                if (columnHeader == null)
                    return;

                // Find the row count TextBlock in the header template
                var countTextBlock = VisualTreeHelperMethods.FindVisualChild<TextBlock>(columnHeader, "PART_SelectAllRowCount");
                if (countTextBlock != null)
                {
                    countTextBlock.Text = $"({count})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating select-all row count display: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the state of all select-all checkboxes across all columns
        /// </summary>
        private void UpdateAllSelectAllCheckboxStates()
        {
            try
            {
                // Find all column headers
                var headersPresenter = VisualTreeHelperMethods.FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return;

                var headers = VisualTreeHelperMethods.FindVisualChildren<DataGridColumnHeader>(headersPresenter).ToList();

                foreach (var header in headers)
                {
                    var column = header.Column;
                    if (column == null)
                        continue;

                    // Check if this column has IsSelectAllColumn enabled
                    bool isSelectAllColumn = (FindGridColumnDescriptor(column)?.IsSelectAllColumn ?? false);
                    if (!isSelectAllColumn)
                        continue;

                    // Find the select-all checkbox in the header
                    var checkbox = VisualTreeHelperMethods.FindVisualChild<CheckBox>(header, "PART_SelectAllCheckBox");
                    if (checkbox == null || checkbox.Visibility != Visibility.Visible)
                        continue;

                    // Update the checkbox state
                    checkbox.IsChecked = CalculateSelectAllCheckboxState(column);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating all select-all checkbox states: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds a DataGridColumnHeader for a specific column
        /// </summary>
        private DataGridColumnHeader FindColumnHeader(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return null;

                // Get the column headers presenter
                var headersPresenter = VisualTreeHelperMethods.FindAncestor<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return null;

                // Find all column headers
                var headers = VisualTreeHelperMethods.FindVisualChildren<DataGridColumnHeader>(headersPresenter);

                // Find the header for this specific column
                return headers.FirstOrDefault(h => h.Column == column);
            }
            catch
            {
                return null;
            }
        }


        #endregion
    }
}
