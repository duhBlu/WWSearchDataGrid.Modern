using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Checkbox cycling for boolean / boolean-styled columns: three-state cycle
    /// (Intermediate → Checked → Unchecked), with the indeterminate state mapping to
    /// IsNull on nullable columns.
    /// </summary>
    public partial class ColumnFilterControl
    {
        private void OnCheckboxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!IsCheckboxColumn) return;
                if (e.Key == Key.Space || e.Key == Key.Enter)
                {
                    CycleCheckboxStateForward();
                    UpdateHasActiveFilterState();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCheckboxPreviewKeyDown: {ex.Message}");
            }
        }

        private void OnCheckboxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!IsCheckboxColumn) return;
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    CycleCheckboxStateForward();
                    UpdateHasActiveFilterState();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCheckboxPreviewMouseDown: {ex.Message}");
            }
        }

        private void OnCheckboxFilterChanged()
        {
            try
            {
                if (!IsCheckboxColumn) return;
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCheckboxFilterChanged: {ex.Message}");
            }
        }

        private void CycleCheckboxStateForward()
        {
            try
            {
                _isInitialState = false;
                SearchTemplateController?.EnsureNullStatusDetermined();
                var allowsNull = SearchTemplateController?.ContainsNullValues ?? false;
                var next = GetNextCycleState(_checkboxCycleState, allowsNull);
                SetCheckboxCycleState(next);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CycleCheckboxStateForward: {ex.Message}");
                ResetCheckboxToInitialState();
            }
        }

        private void ResetCheckboxToInitialState()
        {
            try
            {
                _isInitialState = true;
                SetCheckboxCycleState(CheckboxCycleState.Intermediate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ResetCheckboxToInitialState: {ex.Message}");
            }
        }

        private void SetCheckboxCycleState(CheckboxCycleState state)
        {
            try
            {
                _checkboxCycleState = state;
                UpdateVisualCheckboxState(state);
                ApplyCheckboxCycleFilter(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SetCheckboxCycleState: {ex.Message}");
            }
        }

        private CheckboxCycleState GetNextCycleState(CheckboxCycleState current, bool allowsNull)
        {
            if (allowsNull)
            {
                return current switch
                {
                    CheckboxCycleState.Intermediate => CheckboxCycleState.Checked,
                    CheckboxCycleState.Checked => CheckboxCycleState.Unchecked,
                    CheckboxCycleState.Unchecked => CheckboxCycleState.Intermediate,
                    _ => CheckboxCycleState.Intermediate,
                };
            }
            return current switch
            {
                CheckboxCycleState.Intermediate => CheckboxCycleState.Checked,
                CheckboxCycleState.Checked => CheckboxCycleState.Unchecked,
                CheckboxCycleState.Unchecked => CheckboxCycleState.Checked,
                _ => CheckboxCycleState.Checked,
            };
        }

        private void UpdateVisualCheckboxState(CheckboxCycleState state)
        {
            FilterCheckboxState = state switch
            {
                CheckboxCycleState.Intermediate => null,
                CheckboxCycleState.Checked => true,
                CheckboxCycleState.Unchecked => false,
                _ => null,
            };
        }

        private void ApplyCheckboxCycleFilter(CheckboxCycleState state)
        {
            SearchTemplateController?.EnsureNullStatusDetermined();
            var allowsNull = SearchTemplateController?.ContainsNullValues ?? false;

            switch (state)
            {
                case CheckboxCycleState.Intermediate:
                    if (_isInitialState || !allowsNull)
                        ClearFilterInternal();
                    else
                        ApplyCheckboxIsNullFilter();
                    break;
                case CheckboxCycleState.Checked:
                    ApplyCheckboxBooleanFilter(true);
                    break;
                case CheckboxCycleState.Unchecked:
                    ApplyCheckboxBooleanFilter(false);
                    break;
                default:
                    ClearFilterInternal();
                    break;
            }

            SourceDataGrid?.FilterItemsSource();
            SourceDataGrid?.UpdateFilterPanel();
        }

        private void ApplyCheckboxBooleanFilter(bool value)
        {
            try
            {
                if (SearchTemplateController == null) return;
                SearchTemplateController.SearchGroups.Clear();
                var group = new SearchTemplateGroup();
                SearchTemplateController.SearchGroups.Add(group);

                var template = new SearchTemplate(ColumnDataType.Boolean)
                {
                    SearchType = SearchType.Equals,
                    SelectedValue = value,
                    SearchTemplateController = SearchTemplateController,
                };
                SearchTemplateController.SubscribeToTemplateChanges(template);
                group.SearchTemplates.Add(template);

                SearchTemplateController.UpdateOperatorVisibility();
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyCheckboxBooleanFilter: {ex.Message}");
            }
        }

        private void ApplyCheckboxIsNullFilter()
        {
            try
            {
                if (SearchTemplateController == null) return;
                SearchTemplateController.SearchGroups.Clear();
                var group = new SearchTemplateGroup();
                SearchTemplateController.SearchGroups.Add(group);

                var template = new SearchTemplate(ColumnDataType.Boolean)
                {
                    SearchType = SearchType.IsNull,
                    SearchTemplateController = SearchTemplateController,
                };
                SearchTemplateController.SubscribeToTemplateChanges(template);
                group.SearchTemplates.Add(template);

                SearchTemplateController.UpdateOperatorVisibility();
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyCheckboxIsNullFilter: {ex.Message}");
            }
        }

        public void DetermineCheckboxColumnTypeFromColumnDefinition()
        {
            try
            {
                if (CurrentColumn == null)
                {
                    SetCheckboxColumnState(false);
                    return;
                }

                bool isCheckboxType = ResolveUseCheckBoxInSearchBox();
                SetCheckboxColumnState(isCheckboxType);

                if (isCheckboxType && SearchTemplateController != null)
                    SearchTemplateController.ColumnDataType = ColumnDataType.Boolean;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DetermineCheckboxColumnTypeFromColumnDefinition: {ex.Message}");
                SetCheckboxColumnState(false);
            }
        }

        private void SetCheckboxColumnState(bool isCheckboxColumn)
        {
            var previous = IsCheckboxColumn;
            IsCheckboxColumn = isCheckboxColumn;

            if (previous == isCheckboxColumn) return;

            if (previous)
            {
                FilterCheckboxState = null;
                if (_filterCheckBox != null) _filterCheckBox.IsChecked = null;
                _checkboxCycleState = CheckboxCycleState.Intermediate;
                _isInitialState = true;
            }
            else
            {
                _suppressSearchTextSync = true;
                try
                {
                    SearchText = string.Empty;
                    SearchValue = null;
                }
                finally { _suppressSearchTextSync = false; }
            }
        }
    }
}
