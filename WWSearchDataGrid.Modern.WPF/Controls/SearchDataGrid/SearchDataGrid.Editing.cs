using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
                // Group-header sentinels are not user rows — never editable.
                if (e.Row?.Item is GroupHeaderRow)
                {
                    e.Cancel = true;
                    return;
                }

                // Cancel stock DataGridCell mouse-down BeginEdit when EditorShowMode wants edit
                // deferred (MouseUp/MouseUpFocused) or gated on prior focus (MouseDownFocused) or
                // suppressed entirely (Default/None). Keyboard and programmatic BeginEdit fall through.
                if (e.EditingEventArgs is MouseButtonEventArgs me
                    && me.ChangedButton == MouseButton.Left
                    && me.ButtonState == MouseButtonState.Pressed)
                {
                    var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(me.OriginalSource as DependencyObject);
                    if (cell != null)
                    {
                        var mode = ResolveEditorShowMode(cell);
                        bool allowMouseDownEdit = mode == EditorShowMode.MouseDown
                            || (mode == EditorShowMode.MouseDownFocused && _wasCellFocusedAtMouseDown);
                        if (!allowMouseDownEdit)
                        {
                            e.Cancel = true;
                            return;
                        }
                        // Stash click point so the editor caret lands at the click index, not select-all.
                        StashMouseEditPoint(cell, me.GetPosition(cell));
                    }
                }

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

                // unless commit-on-error is allowed, push the editor's
                // bindings to source (which runs the data-annotation validation rules) and cancel
                // the commit when any error remains — the editor stays open with its error chrome.
                if (!AllowCommitOnValidationAttributeError && e.EditingElement != null)
                {
                    ForceBindingUpdate(e.EditingElement);
                    if (HasValidationError(e.EditingElement))
                    {
                        e.Cancel = true;
                        return;
                    }
                }

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

        #region Validation Edit Lock

        /// <summary>
        /// True while the grid must trap the user inside the cell they're editing: commit-on-error
        /// is off (<see cref="AllowCommitOnValidationAttributeError"/> == false) and the editor
        /// currently holds an unresolved data-annotation error. Re-evaluated live on every
        /// navigation attempt, so fixing the value — or a runtime change to the bound property —
        /// releases the lock immediately. Consumed by the keyboard / mouse / arrow-exit handlers
        /// and by <see cref="BaseEditSettings.ExitCellViaArrow"/>.
        /// </summary>
        internal bool IsEditLockActive() => TryGetLockedEditingCell(out _);

        /// <summary>
        /// Resolves the currently-editing cell and reports whether it is locked (see
        /// <see cref="IsEditLockActive"/>). <paramref name="editingCell"/> is the cell when locked,
        /// otherwise null.
        /// </summary>
        private bool TryGetLockedEditingCell(out DataGridCell editingCell)
        {
            editingCell = null;

            // Commit-on-error mode lets the user roam freely — no lock.
            if (AllowCommitOnValidationAttributeError)
                return false;

            var cell = FindEditingCell();
            if (cell == null)
                return false;

            if (!EditingCellHasValidationError(cell))
                return false;

            editingCell = cell;
            return true;
        }

        /// <summary>The cell that currently owns keyboard focus and is in edit mode, or null.</summary>
        private static DataGridCell FindEditingCell()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(focused);
            return cell != null && cell.IsEditing ? cell : null;
        }

        /// <summary>
        /// Pushes the cell editor's bindings to source — which runs the data-annotation rules —
        /// and reports whether any validation error remains. Pushing is non-destructive: a failed
        /// rule never writes to the source, and a value that now passes simply commits early (it
        /// would on the next legitimate cell-leave anyway).
        /// </summary>
        private bool EditingCellHasValidationError(DataGridCell cell)
        {
            if (cell == null)
                return false;
            ForceBindingUpdate(cell);
            return HasValidationError(cell);
        }

        #endregion

        /// <summary>
        /// True when <paramref name="root"/> or any element in its visual subtree carries a
        /// <see cref="Validation"/> error. The error binding/rule lives on the inner editor (e.g.
        /// the TextBox), not the cell's content host, so the commit gate has to look down the tree.
        /// </summary>
        private static bool HasValidationError(DependencyObject root)
        {
            if (root == null)
                return false;
            if (Validation.GetHasError(root))
                return true;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                if (HasValidationError(VisualTreeHelper.GetChild(root, i)))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Surfaces a data-annotation validation error message as a tooltip on the offending
        /// editor, and clears it when the error resolves. Raised because the cell binding sets
        /// <see cref="Binding.NotifyOnValidationError"/>; the event bubbles to the grid.
        /// </summary>
        private void OnValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (e.OriginalSource is not FrameworkElement element)
                return;

            if (e.Action == ValidationErrorEventAction.Added)
                element.ToolTip = e.Error?.ErrorContent?.ToString();
            else if (e.Action == ValidationErrorEventAction.Removed)
                element.ClearValue(FrameworkElement.ToolTipProperty);
        }


        #endregion

        #region Cell Value Change Detection


        /// <summary>
        /// Property path for a column. Descriptor-first chain (FilterMemberPath → FieldName →
        /// SortMemberPath → Binding.Path) so DataGridTemplateColumn resolves via its descriptor.
        /// </summary>
        private string GetColumnBindingPath(DataGridColumn column)
        {
            if (column == null) return null;

            var descriptor = FindGridColumnDescriptor(column);
            string bindingPath = descriptor?.FilterMemberPath;
            if (string.IsNullOrEmpty(bindingPath)) bindingPath = descriptor?.FieldName;
            if (string.IsNullOrEmpty(bindingPath)) bindingPath = column.SortMemberPath;
            if (string.IsNullOrEmpty(bindingPath) && column is DataGridBoundColumn boundColumn)
            {
                bindingPath = (boundColumn.Binding as Binding)?.Path?.Path;
            }
            return bindingPath;
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
        /// Unwraps the ContentPresenter WPF generates for DataGridTemplateColumn edit templates
        /// to the actual editor (TextBox/CheckBox/ComboBox/DatePicker). Breadth-first, first match wins.
        /// </summary>
        private static FrameworkElement UnwrapEditingElement(FrameworkElement editingElement)
        {
            if (editingElement is TextBox || editingElement is CheckBox
                || editingElement is ComboBox || editingElement is DatePicker)
                return editingElement;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(editingElement);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                int count = VisualTreeHelper.GetChildrenCount(node);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(node, i);
                    if (child is TextBox || child is CheckBox || child is ComboBox || child is DatePicker)
                        return (FrameworkElement)child;
                    queue.Enqueue(child);
                }
            }

            return editingElement;
        }

        /// <summary>
        /// Extracts the edited value from the editing element and converts it to the correct type
        /// </summary>
        private object GetEditedValueFromElement(FrameworkElement editingElement, object originalValue)
        {
            if (editingElement == null)
                return null;

            // Every column in this library is a DataGridTemplateColumn — WPF hands us the
            // ContentPresenter wrapper, not our editor. Without the unwrap, the type checks below
            // miss and the row item lands in the column-values cache via reflection fallback.
            editingElement = UnwrapEditingElement(editingElement);

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

            editingElement = UnwrapEditingElement(editingElement);

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
        /// Updates column caches and raises <see cref="CellValueChanged"/>. Matches the
        /// ColumnSearchBox by column reference first so FilterMemberPath ≠ Binding.Path still resolves.
        /// </summary>
        private void OnCellValueChangedInternal(object item, DataGridColumn column, string bindingPath,
            object oldValue, object newValue, int rowIndex, int columnIndex)
        {
            try
            {
                // Column reference first; fall back to BindingPath match.
                var columnSearchBox = DataColumns.FirstOrDefault(d => d.CurrentColumn == column);
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
