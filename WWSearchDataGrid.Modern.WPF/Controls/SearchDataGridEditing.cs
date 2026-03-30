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
        #region Editing Event Handlers


        /// <summary>
        /// Handles the beginning of edit operations
        /// </summary>
        private void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                // Capture original value for change detection
                if (e.Row?.Item != null && e.Column != null)
                {
                    var bindingPath = GetColumnBindingPath(e.Column);
                    if (!string.IsNullOrEmpty(bindingPath))
                    {
                        var snapshotKey = CreateSnapshotKey(e.Row.Item, bindingPath);
                        if (!string.IsNullOrEmpty(snapshotKey))
                        {
                            var originalValue = ReflectionHelper.GetPropValue(e.Row.Item, bindingPath);
                            _cellValueSnapshots[snapshotKey] = originalValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnBeginningEdit: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles row edit ending
        /// </summary>
        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            try
            {
                // Standard row editing - no special handling needed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnRowEditEnding: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles cell edit ending
        /// </summary>
        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                // Skip processing if edit was cancelled
                if (e.EditAction == DataGridEditAction.Cancel)
                    return;

                // Get binding path and snapshot key
                var bindingPath = GetColumnBindingPath(e.Column);
                if (string.IsNullOrEmpty(bindingPath) || e.Row?.Item == null)
                    return;

                var snapshotKey = CreateSnapshotKey(e.Row.Item, bindingPath);
                if (string.IsNullOrEmpty(snapshotKey) || !_cellValueSnapshots.TryGetValue(snapshotKey, out var originalValue))
                    return;

                var rowIndex = e.Row.GetIndex();
                var columnIndex = e.Column.DisplayIndex;

                // Force the binding to update the source (this makes focus loss behave like Enter)
                ForceBindingUpdate(e.EditingElement);

                // Try to get the edited value from the editing element with type conversion
                object editedValue = null;
                try
                {
                    editedValue = GetEditedValueFromElement(e.EditingElement, originalValue);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting edited value: {ex.Message}");
                }

                // Use Dispatcher.BeginInvoke to process after binding updates complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        object finalValue;

                        if (editedValue != null)
                        {
                            finalValue = editedValue;
                        }
                        else
                        {
                            try
                            {
                                CommitEdit(DataGridEditingUnit.Cell, true);
                            }
                            catch
                            {
                            }
                            finalValue = ReflectionHelper.GetPropValue(e.Row.Item, bindingPath);
                        }

                        if (!EqualityComparer<object>.Default.Equals(originalValue, finalValue))
                        {
                            OnCellValueChangedInternal(e.Row.Item, e.Column, bindingPath,
                                originalValue, finalValue, rowIndex, columnIndex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in delayed cell edit processing: {ex.Message}");
                    }
                    finally
                    {
                        // Clean up snapshot after processing/error
                        _cellValueSnapshots.Remove(snapshotKey);
                    }
                }), DispatcherPriority.DataBind);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCellEditEnding: {ex.Message}");
            }
        }


        #endregion

        #region Cell Value Change Detection


        /// <summary>
        /// Gets the binding path for a DataGrid column
        /// </summary>
        private string GetColumnBindingPath(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding)
                {
                    return binding.Path.Path;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a snapshot key for tracking cell values during editing
        /// </summary>
        private string CreateSnapshotKey(object item, string bindingPath)
        {
            if (item == null || string.IsNullOrEmpty(bindingPath))
                return null;

            var itemIndex = Items.IndexOf(item);
            return $"{itemIndex}_{bindingPath}";
        }

        /// <summary>
        /// Extracts the edited value from the editing element and converts it to the correct type
        /// </summary>
        private object GetEditedValueFromElement(FrameworkElement editingElement, object originalValue)
        {
            if (editingElement == null)
                return null;

            object rawValue = null;

            // Handle TextBox (most common case)
            if (editingElement is TextBox textBox)
            {
                rawValue = textBox.Text;
            }
            // Handle CheckBox
            else if (editingElement is CheckBox checkBox)
            {
                rawValue = checkBox.IsChecked;
            }
            // Handle ComboBox
            else if (editingElement is ComboBox comboBox)
            {
                rawValue = comboBox.SelectedItem ?? comboBox.Text;
            }
            // Handle DatePicker
            else if (editingElement is DatePicker datePicker)
            {
                rawValue = datePicker.SelectedDate;
            }
            else
            {
                // For other custom controls, try to get the value from common value properties
                var properties = new[] { "Value", "SelectedItem", "Text", "Content" };
                foreach (var propName in properties)
                {
                    var prop = editingElement.GetType().GetProperty(propName);
                    if (prop != null && prop.CanRead)
                    {
                        try
                        {
                            rawValue = prop.GetValue(editingElement);
                            break;
                        }
                        catch
                        {
                            // Continue to next property
                        }
                    }
                }
            }

            // Convert the raw value to match the original value's type
            if (rawValue != null && originalValue != null)
            {
                try
                {
                    var targetType = originalValue.GetType();

                    // Handle nullable types
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        targetType = Nullable.GetUnderlyingType(targetType);
                    }

                    // Convert to target type
                    if (targetType == typeof(string))
                    {
                        return rawValue.ToString();
                    }
                    else if (rawValue is string stringValue)
                    {
                        // Convert string to target type
                        if (targetType == typeof(int))
                            return int.TryParse(stringValue, out int intVal) ? intVal : originalValue;
                        else if (targetType == typeof(double))
                            return double.TryParse(stringValue, out double doubleVal) ? doubleVal : originalValue;
                        else if (targetType == typeof(decimal))
                            return decimal.TryParse(stringValue, out decimal decimalVal) ? decimalVal : originalValue;
                        else if (targetType == typeof(DateTime))
                            return DateTime.TryParse(stringValue, out DateTime dateVal) ? dateVal : originalValue;
                        else if (targetType == typeof(bool))
                            return bool.TryParse(stringValue, out bool boolVal) ? boolVal : originalValue;
                        else
                        {
                            // Try using Convert.ChangeType for other types
                            return Convert.ChangeType(stringValue, targetType);
                        }
                    }
                    else
                    {
                        // Raw value is already the correct type or convertible
                        return Convert.ChangeType(rawValue, targetType);
                    }
                }
                catch
                {
                    // If conversion fails, return the raw value
                    return rawValue;
                }
            }

            return rawValue;
        }

        /// <summary>
        /// Forces the editing element to update its binding source
        /// </summary>
        private void ForceBindingUpdate(FrameworkElement editingElement)
        {
            if (editingElement == null) return;

            try
            {
                // For TextBox, update the Text binding
                if (editingElement is TextBox textBox)
                {
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                }
                // For CheckBox, update the IsChecked binding
                else if (editingElement is CheckBox checkBox)
                {
                    var binding = checkBox.GetBindingExpression(CheckBox.IsCheckedProperty);
                    binding?.UpdateSource();
                }
                // For ComboBox, update the SelectedItem/SelectedValue binding
                else if (editingElement is ComboBox comboBox)
                {
                    var binding = comboBox.GetBindingExpression(ComboBox.SelectedItemProperty) ??
                                 comboBox.GetBindingExpression(ComboBox.SelectedValueProperty);
                    binding?.UpdateSource();
                }
                // For DatePicker, update the SelectedDate binding
                else if (editingElement is DatePicker datePicker)
                {
                    var binding = datePicker.GetBindingExpression(DatePicker.SelectedDateProperty);
                    binding?.UpdateSource();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error forcing binding update: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal handler for cell value changes that updates caches and raises events.
        /// NOTE: When a column uses GridColumn.FilterMemberPath that differs from its Binding.Path,
        /// this method prioritizes finding the ColumnSearchBox by column reference to ensure
        /// cache updates work correctly even when the paths differ.
        /// </summary>
        private void OnCellValueChangedInternal(object item, DataGridColumn column, string bindingPath,
            object oldValue, object newValue, int rowIndex, int columnIndex)
        {
            try
            {
                // Find the corresponding column search box
                // Priority 1: Match by column reference (handles FilterMemberPath != Binding.Path scenarios)
                var columnSearchBox = DataColumns.FirstOrDefault(d => d.CurrentColumn == column);

                // Priority 2: Fallback to BindingPath matching (for backward compatibility)
                if (columnSearchBox == null)
                {
                    columnSearchBox = DataColumns.FirstOrDefault(d => d.BindingPath == bindingPath);
                }
                if (columnSearchBox?.SearchTemplateController != null)
                {
                    // Update column value caches
                    columnSearchBox.SearchTemplateController.RemoveColumnValue(oldValue);
                    columnSearchBox.SearchTemplateController.AddOrUpdateColumnValue(newValue);
                }

                // Invalidate collection context cache to refresh statistical calculations
                InvalidateCollectionContextCache();

                // Raise public event for external subscribers
                var eventArgs = new CellValueChangedEventArgs(item, column, bindingPath,
                    oldValue, newValue, rowIndex, columnIndex);
                CellValueChanged?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCellValueChangedInternal: {ex.Message}");
            }
        }


        #endregion
    }
}
