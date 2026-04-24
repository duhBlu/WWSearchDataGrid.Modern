using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class ColumnSearchBox
    {
        #region Checkbox Event Handlers


        /// <summary>
        /// Handles preview key events on the checkbox to intercept cycling before native behavior
        /// </summary>
        private void OnCheckboxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!IsCheckboxColumn)
                    return;

                // Handle Space and Enter key presses
                if (e.Key == Key.Space || e.Key == Key.Enter)
                {
                    // Cycle forward and prevent native checkbox behavior
                    CycleCheckboxStateForward();
                    
                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();
                    
                    // Mark as handled to prevent native cycling
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCheckboxPreviewKeyDown: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles preview mouse events on the checkbox to intercept cycling before native behavior
        /// </summary>
        private void OnCheckboxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!IsCheckboxColumn)
                    return;

                // Handle left mouse button clicks
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Cycle forward and prevent native checkbox behavior
                    CycleCheckboxStateForward();
                    
                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();
                    
                    // Mark as handled to prevent native cycling
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCheckboxPreviewMouseDown: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles changes to the FilterCheckboxState dependency property
        /// This is primarily used for external updates and synchronization
        /// </summary>
        private void OnCheckboxFilterChanged()
        {
            try
            {
                if (!IsCheckboxColumn)
                    return;

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCheckboxFilterChanged: {ex.Message}");
            }
        }


        #endregion

        #region Checkbox Cycling Logic

        private void CycleCheckboxStateForward()
        {
            try
            {
                _isInitialState = false; // We're now cycling, not in initial state

                // Ensure null status is determined before cycling
                // This will load cache if not already loaded, which is acceptable
                // since user is actively interacting with the filter
                SearchTemplateController?.EnsureNullStatusDetermined();

                var allowsNullValues = SearchTemplateController?.ContainsNullValues ?? false;
                var nextState = GetNextCycleState(_checkboxCycleState, allowsNullValues);
                SetCheckboxCycleState(nextState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CycleCheckboxStateForward: {ex.Message}");
                // Fallback to safe state
                ResetCheckboxToInitialState();
            }
        }

        /// <summary>
        /// Resets the checkbox to the initial intermediate state (no filter)
        /// </summary>
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

        /// <summary>
        /// Sets the checkbox cycle state programmatically
        /// </summary>
        /// <param name="state">The state to set</param>
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

        /// <summary>
        /// Gets the next state in the cycling sequence based on current state and column capabilities
        /// </summary>
        /// <param name="currentState">Current cycling state</param>
        /// <param name="allowsNullValues">Whether the column contains null values</param>
        /// <returns>The next state in the cycle</returns>
        private CheckboxCycleState GetNextCycleState(CheckboxCycleState currentState, bool allowsNullValues)
        {
            if (allowsNullValues)
            {
                // Columns WITH null values: Intermediate → Checked → Unchecked → Intermediate (cycle back for null filtering)
                return currentState switch
                {
                    CheckboxCycleState.Intermediate => CheckboxCycleState.Checked,
                    CheckboxCycleState.Checked => CheckboxCycleState.Unchecked,
                    CheckboxCycleState.Unchecked => CheckboxCycleState.Intermediate,
                    _ => CheckboxCycleState.Intermediate
                };
            }
            else
            {
                // Columns WITHOUT null values: Intermediate → Checked → Unchecked → Checked → Unchecked (skip intermediate)
                return currentState switch
                {
                    CheckboxCycleState.Intermediate => CheckboxCycleState.Checked,
                    CheckboxCycleState.Checked => CheckboxCycleState.Unchecked,
                    CheckboxCycleState.Unchecked => CheckboxCycleState.Checked,
                    _ => CheckboxCycleState.Checked
                };
            }
        }

        /// <summary>
        /// Updates the visual checkbox state to match the logical state
        /// </summary>
        /// <param name="state">The logical state</param>
        private void UpdateVisualCheckboxState(CheckboxCycleState state)
        {
            bool? checkboxValue = state switch
            {
                CheckboxCycleState.Intermediate => null,
                CheckboxCycleState.Checked => true,
                CheckboxCycleState.Unchecked => false,
                _ => null
            };

            FilterCheckboxState = checkboxValue;
        }

        /// <summary>
        /// Applies the appropriate filter based on the current cycle state
        /// PERFORMANCE: Assumes null status already determined by CycleCheckboxStateForward
        /// </summary>
        /// <param name="state">The state to apply filter for</param>
        private void ApplyCheckboxCycleFilter(CheckboxCycleState state)
        {
            // Null status should already be determined by CycleCheckboxStateForward
            // but check again to be safe
            SearchTemplateController?.EnsureNullStatusDetermined();

            var allowsNullValues = SearchTemplateController?.ContainsNullValues ?? false;
            switch (state)
            {
                case CheckboxCycleState.Intermediate:
                    if (_isInitialState || !allowsNullValues)
                    {
                        // Initial state or non-nullable columns: clear all filters
                        ClearFilterInternal();
                    }
                    else
                    {
                        // For nullable columns in intermediate state after cycling, show only null values
                        ApplyCheckboxIsNullFilter();
                    }
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

            // Update the data grid
            SourceDataGrid?.FilterItemsSource();
            SourceDataGrid?.UpdateFilterPanel();
        }

        /// <summary>
        /// Applies a boolean equals filter
        /// </summary>
        /// <param name="value">The boolean value to filter for</param>
        private void ApplyCheckboxBooleanFilter(bool value)
        {
            try
            {
                if (SearchTemplateController == null) return;

                // Clear existing groups
                SearchTemplateController.SearchGroups.Clear();

                // Create a new search group
                var group = new SearchTemplateGroup();
                SearchTemplateController.SearchGroups.Add(group);

                // Create Equals template for the boolean value
                var template = new SearchTemplate(ColumnDataType.Boolean);
                template.SearchType = SearchType.Equals;
                template.SelectedValue = value;
                template.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference

                // Bind to property changes for auto-apply monitoring
                SearchTemplateController.SubscribeToTemplateChanges(template);

                // Add the template
                group.SearchTemplates.Add(template);

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyCheckboxBooleanFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies an IsNull filter for showing only null values
        /// </summary>
        private void ApplyCheckboxIsNullFilter()
        {
            try
            {
                if (SearchTemplateController == null) return;

                // Clear existing groups
                SearchTemplateController.SearchGroups.Clear();

                // Create a new search group
                var group = new SearchTemplateGroup();
                SearchTemplateController.SearchGroups.Add(group);

                // Create IsNull template for null values
                var template = new SearchTemplate(ColumnDataType.Boolean);
                template.SearchType = SearchType.IsNull;
                template.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference

                // Bind to property changes for auto-apply monitoring
                SearchTemplateController.SubscribeToTemplateChanges(template);

                // Add the template
                group.SearchTemplates.Add(template);

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyCheckboxIsNullFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a permanent filter from current search text and refocuses the textbox
        /// Used for Ctrl+Enter behavior

        #endregion

        #region Checkbox Column Detection

        internal void DetermineCheckboxColumnTypeFromColumnDefinition()
        {
            try
            {
                if (CurrentColumn == null)
                {
                    SetCheckboxColumnState(false);
                    return;
                }

                // Resolve from descriptor first, then attached properties
                bool isCheckboxType = ResolveUseCheckBoxInSearchBox();

                // Set the UI state immediately
                SetCheckboxColumnState(isCheckboxType);

                // If this is a checkbox column, set the appropriate column data type
                if (isCheckboxType && SearchTemplateController != null)
                {
                    SearchTemplateController.ColumnDataType = ColumnDataType.Boolean;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error determining column type from definition: {ex.Message}");
                SetCheckboxColumnState(false);
            }
        }

        /// <summary>
        /// Sets the checkbox column state and UI properties
        /// </summary>
        private void SetCheckboxColumnState(bool isCheckboxColumn)
        {
            var previousIsCheckboxColumn = IsCheckboxColumn;

            IsCheckboxColumn = isCheckboxColumn;

            // Handle UI state changes when column type changes
            if (previousIsCheckboxColumn != isCheckboxColumn)
            {
                if (previousIsCheckboxColumn)
                {
                    // Was checkbox, now text - clear checkbox state
                    FilterCheckboxState = null;
                    if (filterCheckBox != null)
                    {
                        filterCheckBox.IsChecked = null;
                    }
                    _checkboxCycleState = CheckboxCycleState.Intermediate;
                    _isInitialState = true;
                }
                else
                {
                    // Was text, now checkbox - clear text search
                    SearchText = string.Empty;
                }
            }
        }

        /// <summary>
        /// Adds an incremental filter with OR logic, preserving the search type from DefaultSearchMode
        /// </summary>

        #endregion
    }
}
