using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class ColumnSearchBox
    {
        #region Search Prefix State

        // Set by GetEffectiveSearchValue() when a prefix shortcut is detected
        private SearchType? _prefixSearchType;
        private string _prefixSecondaryValue;

        #endregion

        #region Text Filter Management

        /// <summary>
        /// Gets the effective search value for filter creation.
        /// Parses prefix shortcuts (e.g., ">100" → GreaterThan, "=john" → Equals).
        /// For mask-based columns, also strips mask literal characters.
        /// Sets _prefixSearchType and _prefixSecondaryValue as side effects.
        /// </summary>
        private string GetEffectiveSearchValue()
        {
            string text = SearchText;

            // Strip mask literals if applicable
            if (SearchTemplateController?.DisplayValueProvider is Core.Display.MaskDisplayProvider maskProvider
                && !string.IsNullOrEmpty(text))
            {
                text = maskProvider.StripLiterals(text);
            }

            // Parse search prefix shortcuts (filtered by column data type)
            var columnDataType = SearchTemplateController?.ColumnDataType ?? Core.ColumnDataType.Unknown;
            var (searchType, value, secondaryValue) = SearchPrefixParser.Parse(text, columnDataType);
            _prefixSearchType = searchType;
            _prefixSecondaryValue = secondaryValue;

            return value ?? string.Empty;
        }

        private void AddIncrementalContainsFilter()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                if (SearchTemplateController.SearchGroups.Count == 0)
                {
                    SearchTemplateController.AddSearchGroup();
                }

                var firstGroup = SearchTemplateController.SearchGroups[0];

                // Get the search type from temporary template (if it exists) to preserve DefaultSearchMode
                var searchType = SearchType.Contains; // Default fallback
                if (_temporarySearchTemplate != null)
                {
                    searchType = _temporarySearchTemplate.SearchType;
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                }
                else
                {
                    // If no temporary template exists, get from column property
                    var defaultMode = CurrentColumn != null
                        ? GridColumn.GetDefaultSearchMode(CurrentColumn)
                        : DefaultSearchMode.Contains;
                    searchType = MapDefaultSearchModeToSearchType(defaultMode);
                }

                RemoveDefaultEmptyTemplates(firstGroup);

                // Find existing templates with same search type for OR logic
                var existingTemplatesOfSameType = firstGroup.SearchTemplates
                    .Where(t => t.SearchType == searchType && t.HasCustomFilter)
                    .ToList();

                // Create new confirmed template with the preserved search type
                var newTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType);
                newTemplate.SearchType = searchType;
                newTemplate.SelectedValue = GetEffectiveSearchValue();
                newTemplate.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference

                // Bind to property changes for auto-apply monitoring
                SearchTemplateController.SubscribeToTemplateChanges(newTemplate);

                // If this is not the first template of this type, set OR operator
                if (existingTemplatesOfSameType.Any())
                {
                    newTemplate.OperatorName = "Or";
                }

                firstGroup.SearchTemplates.Add(newTemplate);

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();

                // Apply the filter to the grid
                SourceDataGrid.FilterItemsSource();

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddIncrementalContainsFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the search textbox only, preserving existing filters
        /// </summary>
        private void ClearSearchTextOnly()
        {
            try
            {
                SearchText = string.Empty;
                if (searchTextBox != null)
                    searchTextBox.Text = string.Empty;

                HasAdvancedFilter = SearchTemplateController.HasCustomExpression;

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearSearchTextOnly: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the search text and removes only the temporary template (not confirmed filters)
        /// For checkbox columns, this completely clears the filter
        /// This is used by the X button in the search box
        /// </summary>
        private void ClearSearchTextAndTemporaryFilter()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                if (IsCheckboxColumn)
                {
                    // For checkbox columns, reset to initial state (no filter)
                    ResetCheckboxToInitialState();
                }
                else
                {
                    // For text columns, clear the search text
                    SearchText = string.Empty;
                    if (searchTextBox != null)
                        searchTextBox.Text = string.Empty;

                    // Remove only the temporary template if it exists
                    if (_temporarySearchTemplate != null && SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        var firstGroup = SearchTemplateController.SearchGroups[0];
                        firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                        _temporarySearchTemplate = null;
                        
                        // Update the filter expression and apply to grid
                        SearchTemplateController.UpdateFilterExpression();
                        SourceDataGrid?.FilterItemsSource();
                    }

                    // Update HasAdvancedFilter state
                    HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                }
                
                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
                
                // Update filter panel
                SourceDataGrid?.UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a temporary template immediately for state synchronization
        /// This ensures HasActiveFilter state is accurate without waiting for timer
        /// </summary>
        private void CreateTemporaryTemplateImmediate()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || string.IsNullOrWhiteSpace(SearchText))
                    return;

                // Ensure we have a search group
                if (SearchTemplateController.SearchGroups.Count == 0)
                {
                    SearchTemplateController.AddSearchGroup();
                }

                var firstGroup = SearchTemplateController.SearchGroups[0];
                
                // Remove any default empty templates before adding our Contains template
                RemoveDefaultEmptyTemplates(firstGroup);
                
                // Get the effective value first (this also parses prefix shortcuts and sets _prefixSearchType)
                string effectiveValue = GetEffectiveSearchValue();

                // Determine search type: prefix shortcut overrides column default
                var defaultMode = CurrentColumn != null
                    ? GridColumn.GetDefaultSearchMode(CurrentColumn)
                    : DefaultSearchMode.Contains;
                var searchType = _prefixSearchType ?? MapDefaultSearchModeToSearchType(defaultMode);

                // Update existing temporary template or create new one
                if (_temporarySearchTemplate != null)
                {
                    // Update existing temporary template with new value and possibly new search type
                    _temporarySearchTemplate.SearchType = searchType;
                    _temporarySearchTemplate.SelectedValue = effectiveValue;
                    if (_prefixSecondaryValue != null)
                        _temporarySearchTemplate.SelectedSecondaryValue = _prefixSecondaryValue;
                }
                else
                {
                    // Create new temporary template
                    _temporarySearchTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType);
                    _temporarySearchTemplate.SearchType = searchType;
                    _temporarySearchTemplate.SelectedValue = effectiveValue;
                    if (_prefixSecondaryValue != null)
                        _temporarySearchTemplate.SelectedSecondaryValue = _prefixSecondaryValue;
                    _temporarySearchTemplate.SearchTemplateController = SearchTemplateController;

                    // Bind to property changes for auto-apply monitoring
                    SearchTemplateController.SubscribeToTemplateChanges(_temporarySearchTemplate);

                    // Check if we have existing confirmed Contains templates
                    var existingContainsTemplates = firstGroup.SearchTemplates
                        .Where(t => t.SearchType == SearchType.Contains && t.HasCustomFilter)
                        .ToList();

                    // If this is not the first template, set OR operator
                    if (existingContainsTemplates.Any())
                    {
                        _temporarySearchTemplate.OperatorName = "Or";
                    }

                    firstGroup.SearchTemplates.Add(_temporarySearchTemplate);
                }

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update HasActiveFilter state immediately
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateTemporaryTemplateImmediate: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears only the temporary template when search text becomes empty
        /// This is used when user manually backspaces all text
        /// </summary>
        private void ClearTemporaryTemplate()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                // Remove only the temporary template if it exists
                if (_temporarySearchTemplate != null && SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    var firstGroup = SearchTemplateController.SearchGroups[0];
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                    
                    // Update the filter expression and apply to grid
                    SearchTemplateController.UpdateFilterExpression();
                    SourceDataGrid?.FilterItemsSource();
                    
                    // Update HasAdvancedFilter state
                    HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                    
                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();

                    // Update filter panel
                    SourceDataGrid?.UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearTemporaryTemplate: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the filter to the grid (used for debounced/timer-based filter application)
        /// Template creation is now immediate, this method only handles the actual filtering
        /// </summary>
        private void UpdateSimpleFilter()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                if (!string.IsNullOrWhiteSpace(SearchText) && _temporarySearchTemplate != null)
                {
                    // Template should already exist from immediate creation
                    // Re-parse to get the prefix-stripped value (in case of rapid typing)
                    string effectiveValue = GetEffectiveSearchValue();
                    _temporarySearchTemplate.SelectedValue = effectiveValue;
                    if (_prefixSearchType.HasValue)
                        _temporarySearchTemplate.SearchType = _prefixSearchType.Value;
                    if (_prefixSecondaryValue != null)
                        _temporarySearchTemplate.SelectedSecondaryValue = _prefixSecondaryValue;

                    // Update the filter expression
                    SearchTemplateController.UpdateFilterExpression();

                    // Apply the filter to the grid
                    SourceDataGrid.FilterItemsSource();

                    // Update HasAdvancedFilter state
                    HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                    
                    // Update filter panel
                    SourceDataGrid.UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateSimpleFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes default empty templates that get automatically added by the framework
        /// </summary>
        /// <param name="group">The search group to clean up</param>
        private void RemoveDefaultEmptyTemplates(SearchTemplateGroup group)
        {
            try
            {
                // Remove templates that are empty/default and not our specific Contains templates
                var templatesToRemove = group.SearchTemplates
                    .Where(t => t != _temporarySearchTemplate && !t.HasCustomFilter)
                    .ToList();

                foreach (var template in templatesToRemove)
                {
                    group.SearchTemplates.Remove(template);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RemoveDefaultEmptyTemplates: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps the DefaultSearchMode enum to the corresponding SearchType enum value.
        /// This provides a type-safe way to convert from the restricted set of default modes
        /// to the full SearchType enum used by the filtering engine.
        /// </summary>
        /// <param name="mode">The default search mode to map</param>
        /// <returns>The corresponding SearchType enum value</returns>
        private SearchType MapDefaultSearchModeToSearchType(DefaultSearchMode mode)
        {
            return mode switch
            {
                DefaultSearchMode.Contains => SearchType.Contains,
                DefaultSearchMode.StartsWith => SearchType.StartsWith,
                DefaultSearchMode.EndsWith => SearchType.EndsWith,
                DefaultSearchMode.Equals => SearchType.Equals,
                _ => SearchType.Contains // Fallback to Contains for any unexpected values
            };
        }

        /// <summary>
        /// Internal implementation of clear filter
        /// </summary>

        #endregion

        #region Filter Popup Management

        private void ShowFilterPopup()
        {
            try
            {
                if (SourceDataGrid == null)
                    return;

                if (SearchTemplateController == null)
                    InitializeSearchTemplateController();

                if (SearchTemplateController == null)
                    return;

                // Create filter content if none exists
                if (_filterContent == null)
                {
                    _filterContent = new ColumnFilterEditor
                    {
                        SearchTemplateController = SearchTemplateController,
                        DataContext = this
                    };

                    // Subscribe to filter events
                    _filterContent.FiltersApplied += OnFiltersApplied;
                    _filterContent.FiltersCleared += OnFiltersCleared;
                }

                // Calculate the vertical offset to position below the column header
                double verticalOffset = CalculateVerticalOffsetForColumnHeader();

                // Create popup if none exists
                if (_filterPopup == null)
                {
                    _filterPopup = new Popup
                    {
                        Child = _filterContent,
                        PlacementTarget = this,
                        Placement = PlacementMode.Bottom,
                        AllowsTransparency = true,
                        PopupAnimation = PopupAnimation.Fade,
                        StaysOpen = false,
                        MaxWidth = 500,
                        MaxHeight = 600,
                        HorizontalOffset = _filterContent.HorizontalOffset,
                        VerticalOffset = verticalOffset
                    };

                    _filterPopup.KeyDown += OnPopupKeyDown;
                    _filterPopup.Closed += OnPopupClosed;
                }
                else
                {
                    _filterPopup.PlacementTarget = this;
                    _filterPopup.HorizontalOffset = _filterContent.HorizontalOffset;
                    _filterPopup.VerticalOffset = verticalOffset;
                }

                _filterPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowFilterPopup: {ex.Message}");
                if (_filterPopup != null)
                {
                    _filterPopup.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Calculates the vertical offset needed to position the popup below the column header
        /// </summary>
        private double CalculateVerticalOffsetForColumnHeader()
        {
            try
            {
                // Start with the base offset from the ColumnFilterEditor style
                double offset = _filterContent.VerticalOffset;

                // Find the parent DataGridColumnHeader
                var columnHeader = VisualTreeHelperMethods.FindAncestor<DataGridColumnHeader>(this);
                if (columnHeader != null)
                {
                    // Get the total height of the column header (includes both search box and header content)
                    double headerHeight = columnHeader.ActualHeight;

                    // Get the height of this ColumnSearchBox
                    double searchBoxHeight = this.ActualHeight;

                    // Calculate the additional offset needed to place the popup below the header content
                    // This is the height of the header content (headerHeight - searchBoxHeight)
                    double headerContentHeight = headerHeight - searchBoxHeight - 1;

                    // Add the header content height to the base offset
                    offset += headerContentHeight;
                }

                return offset;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating vertical offset: {ex.Message}");
                // Return the base offset from the style if calculation fails
                return _filterContent.VerticalOffset;
            }
        }

        /// <summary>
        /// Handle popup key down events (Escape to close)
        /// </summary>
        private void OnPopupKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _filterPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle popup closed event for cleanup
        /// </summary>
        private void OnPopupClosed(object sender, EventArgs e)
        {
            // Additional cleanup if needed when popup closes
        }

        /// <summary>
        /// Handles filter editor filters applied event
        /// </summary>
        private void OnFiltersApplied(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterPanel();
        }

        /// <summary>
        /// Handles filter editor filters cleared event
        /// </summary>
        private void OnFiltersCleared(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterPanel();
        }


        #endregion
    }
}
