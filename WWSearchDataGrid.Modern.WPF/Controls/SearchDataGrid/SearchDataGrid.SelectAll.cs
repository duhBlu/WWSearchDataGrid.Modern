using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Select All Column Support

        /// <summary>
        /// Pushes a fresh checkbox state and row-count to every <see cref="GridColumn"/>
        /// descriptor that has <see cref="ColumnDataBase.ShowCheckBoxInHeader"/> set. Called
        /// after filter changes, selection changes, items-source changes, and on initial
        /// template apply. The header's <c>PART_SelectAllCheckBox</c> is bound to
        /// <see cref="ColumnDataBase.IsChecked"/>, so updating that DP via
        /// <see cref="ColumnDataBase.SetIsCheckedFromGrid"/> propagates to the UI without
        /// any visual-tree walking on the checkbox itself. The row-count display still
        /// requires a tree walk because its presenter (<c>PART_SelectAllRowCount</c>) lives
        /// inside the column-header template and isn't bound to the descriptor.
        /// </summary>
        internal void RefreshAllSelectAllHeaders()
        {
            if (GridColumns == null) return;
            foreach (var descriptor in GridColumns)
            {
                RefreshSelectAllHeader(descriptor);
            }
        }

        /// <summary>
        /// Pushes a fresh <see cref="ColumnDataBase.IsChecked"/> and row-count for one
        /// descriptor.
        /// </summary>
        internal void RefreshSelectAllHeader(ColumnDataBase descriptor)
        {
            if (descriptor == null) return;
            if (!descriptor.ShowCheckBoxInHeader) return;
            descriptor.RefreshActualShowCheckBoxInHeader();

            var column = descriptor.InternalColumn;
            if (column == null) return;

            // Push the current resolved state to the descriptor — the bound checkbox picks
            // it up automatically. SetIsCheckedFromGrid suppresses the apply-to-rows callback
            // so we don't re-mutate the data.
            descriptor.SetIsCheckedFromGrid(CalculateSelectAllCheckboxState(column));

            // Row count display for SelectedRows scope.
            if (descriptor.SelectAllScope == SelectAllScope.SelectedRows)
            {
                var items = GetItemsForSelectAllScope(descriptor.SelectAllScope);
                UpdateSelectAllRowCountDisplay(column, items?.Count() ?? 0, show: true);
            }
            else
            {
                UpdateSelectAllRowCountDisplay(column, 0, show: false);
            }
        }

        /// <summary>
        /// Applies a definite boolean value to every row in the descriptor's
        /// <see cref="ColumnDataBase.SelectAllScope"/>. Invoked by the
        /// <see cref="ColumnDataBase.IsChecked"/> DP callback when the bound header checkbox
        /// is toggled (or when consumer code writes <c>IsChecked</c> directly). After
        /// mutation, the resolved state is pushed back to the descriptor via
        /// <see cref="ColumnDataBase.SetIsCheckedFromGrid"/>.
        /// </summary>
        internal void ApplyHeaderCheckedValue(ColumnDataBase descriptor, bool newValue)
        {
            if (descriptor == null) return;
            var column = descriptor.InternalColumn;
            if (column == null) return;

            try
            {
                string bindingPath = GetColumnBindingPathForSelectAll(column);
                if (string.IsNullOrEmpty(bindingPath)) return;

                SelectAllScope scope = descriptor.SelectAllScope;
                var itemsToToggle = GetItemsForSelectAllScope(scope);
                if (itemsToToggle == null || !itemsToToggle.Any()) return;

                List<object> savedSelectedItems = null;
                List<DataGridCellInfo> savedSelectedCells = null;

                if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                {
                    savedSelectedCells = SelectedCells.ToList();
                }
                else
                {
                    savedSelectedItems = SelectedItems.Cast<object>().ToList();
                }

                foreach (var item in itemsToToggle)
                {
                    var value = ReflectionHelper.GetPropValue(item, bindingPath);
                    // Only mutate non-null bool / bool? fields; leave reference-type columns alone.
                    if (value == null || value is bool?)
                    {
                        ReflectionHelper.SetPropValue(item, bindingPath, newValue);
                    }
                }

                Items.Refresh();

                if (HasActiveColumnFilters())
                {
                    InvalidateCollectionContextCache();
                }

                FilterItemsSource();
                UpdateFilterSummaryPanel();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                        {
                            if (savedSelectedCells != null && savedSelectedCells.Count > 0)
                            {
                                SelectedCells.Clear();
                                foreach (var cellInfo in savedSelectedCells)
                                {
                                    if (cellInfo.Item != null && Items.Contains(cellInfo.Item))
                                    {
                                        SelectedCells.Add(cellInfo);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (savedSelectedItems != null && savedSelectedItems.Count > 0)
                            {
                                SelectedItems.Clear();
                                foreach (var item in savedSelectedItems)
                                {
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

                SyncColumnSearchBoxFilterWithSelectAll(column, newValue);

                // Push the resolved state back so the descriptor.IsChecked reflects what's
                // actually in the data (handles e.g. read-only rows that didn't accept the
                // mutation). Suppresses re-entry into ApplyHeaderCheckedValue.
                descriptor.SetIsCheckedFromGrid(CalculateSelectAllCheckboxState(column));

                if (scope == SelectAllScope.SelectedRows)
                {
                    UpdateSelectAllRowCountDisplay(column, itemsToToggle.Count(), show: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying header checked value: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles selection change events to update row count displays and tri-state for
        /// columns scoped to <see cref="SelectAllScope.SelectedRows"/>.
        /// </summary>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { RefreshSelectedRowsSelectAllHeaders(); }
            catch (Exception ex) { Debug.WriteLine($"Error in OnSelectionChanged: {ex.Message}"); }
        }

        /// <summary>
        /// Handles selected cells change events to update row count displays and tri-state
        /// for columns scoped to <see cref="SelectAllScope.SelectedRows"/>.
        /// </summary>
        private void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try { RefreshSelectedRowsSelectAllHeaders(); }
            catch (Exception ex) { Debug.WriteLine($"Error in OnSelectedCellsChanged: {ex.Message}"); }
        }

        /// <summary>
        /// Refresh path for selection-change handlers — only walks descriptors that actually
        /// care about the selection (<see cref="SelectAllScope.SelectedRows"/>).
        /// </summary>
        private void RefreshSelectedRowsSelectAllHeaders()
        {
            if (GridColumns == null) return;
            foreach (var descriptor in GridColumns)
            {
                if (!descriptor.ShowCheckBoxInHeader) continue;
                if (descriptor.SelectAllScope != SelectAllScope.SelectedRows) continue;
                RefreshSelectAllHeader(descriptor);
            }
        }

        /// <summary>
        /// Determines if a column is boolean by descriptor metadata, generated column type,
        /// or runtime data-type detection. Kept for back-compat callers; the descriptor
        /// itself owns the equivalent check via <see cref="ColumnDataBase.ActualShowCheckBoxInHeader"/>.
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

            var columnSearchBox = DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
            if (columnSearchBox?.SearchTemplateController != null)
                return columnSearchBox.SearchTemplateController.ColumnDataType == Core.ColumnDataType.Boolean;

            return false;
        }

        /// <summary>
        /// Resolves the binding path used for select-all reads/writes. Priority:
        /// FilterMemberPath → SortMemberPath → Binding.Path.
        /// </summary>
        internal string GetColumnBindingPathForSelectAll(DataGridColumn column)
        {
            if (column == null)
                return null;

            string bindingPath = FindGridColumnDescriptor(column)?.FilterMemberPath;

            if (string.IsNullOrEmpty(bindingPath))
                bindingPath = column.SortMemberPath;

            if (string.IsNullOrEmpty(bindingPath) && column is DataGridBoundColumn boundColumn)
            {
                bindingPath = (boundColumn.Binding as Binding)?.Path?.Path;
            }

            return bindingPath;
        }

        /// <summary>
        /// Checkbox state for the column's <see cref="SelectAllScope"/>: <c>true</c>/<c>false</c>
        /// for uniform non-null values, <c>null</c> for mixed or all-null.
        /// </summary>
        internal bool? CalculateSelectAllCheckboxState(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return null;

                string bindingPath = GetColumnBindingPathForSelectAll(column);
                if (string.IsNullOrEmpty(bindingPath))
                    return null;

                SelectAllScope scope = (FindGridColumnDescriptor(column)?.SelectAllScope ?? SelectAllScope.FilteredRows);

                var itemsToCheck = GetItemsForSelectAllScope(scope);
                if (itemsToCheck == null || !itemsToCheck.Any())
                    return null;

                return CalculateSelectAllCheckboxStateForItems(itemsToCheck, bindingPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating select-all checkbox state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Items in scope for the given <see cref="SelectAllScope"/>.
        /// </summary>
        private IEnumerable<object> GetItemsForSelectAllScope(SelectAllScope scope)
        {
            switch (scope)
            {
                case SelectAllScope.FilteredRows:
                    return Items.Cast<object>();

                case SelectAllScope.SelectedRows:
                    if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                    {
                        var selectedItems = new HashSet<object>();
                        foreach (var cell in SelectedCells)
                        {
                            if (cell.Item != null)
                                selectedItems.Add(cell.Item);
                        }
                        return selectedItems;
                    }
                    return SelectedItems.Cast<object>();

                case SelectAllScope.AllItems:
                    if (originalItemsSource != null)
                        return originalItemsSource.Cast<object>();
                    return Enumerable.Empty<object>();

                default:
                    return Items.Cast<object>();
            }
        }

        /// <summary>
        /// Tri-state aggregation of a property across a set of items.
        /// </summary>
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
                    if (value == null) continue;

                    totalNonNull++;

                    if (value is bool boolValue)
                    {
                        if (boolValue) trueCount++;
                        else falseCount++;
                    }
                }

                if (totalNonNull == 0) return null;
                if (trueCount == totalNonNull) return true;
                if (falseCount == totalNonNull) return false;
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating checkbox state for items: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronizes the <c>ColumnSearchBox</c> checkbox filter with the new select-all
        /// state when <see cref="ColumnDataBase.UseCheckBoxInSearchBox"/> is set and a
        /// filter is active.
        /// </summary>
        private void SyncColumnSearchBoxFilterWithSelectAll(DataGridColumn column, bool newValue)
        {
            try
            {
                if (!(FindGridColumnDescriptor(column)?.UseCheckBoxInSearchBox ?? false))
                    return;

                var columnSearchBox = DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                if (columnSearchBox == null) return;
                if (!columnSearchBox.HasActiveFilter) return;

                columnSearchBox.FilterCheckboxState = newValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing ColumnSearchBox filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the row count display TextBlock (<c>PART_SelectAllRowCount</c>) in the
        /// column header.
        /// </summary>
        private void UpdateSelectAllRowCountDisplay(DataGridColumn column, int count, bool show)
        {
            try
            {
                var columnHeader = FindColumnHeader(column);
                if (columnHeader == null) return;

                var countTextBlock = VisualTreeHelperMethods.FindVisualDescendant<TextBlock>(columnHeader, "PART_SelectAllRowCount");
                if (countTextBlock == null) return;

                if (show)
                {
                    countTextBlock.Visibility = Visibility.Visible;
                    countTextBlock.Text = $"({count})";
                }
                else
                {
                    countTextBlock.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating select-all row count display: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the <see cref="DataGridColumnHeader"/> for a specific column by walking the
        /// header presenter's children.
        /// </summary>
        private DataGridColumnHeader FindColumnHeader(DataGridColumn column)
        {
            try
            {
                if (column == null) return null;

                var headersPresenter = VisualTreeHelperMethods.FindVisualDescendant<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null) return null;

                var headers = VisualTreeHelperMethods.FindVisualDescendants<DataGridColumnHeader>(headersPresenter);
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
