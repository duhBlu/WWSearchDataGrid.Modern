using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.Core.Caching;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Filtering

        private bool HasActiveColumnFilters()
        {
            return DataColumns?.Any(d => d.SearchTemplateController?.HasCustomExpression == true) == true ||
                   DataColumns?.Any(d => d.HasActiveFilter) == true;
        }

        /// <summary>
        /// Apply filters to the items source with performance optimization for large datasets
        /// </summary>
        /// <param name="delay">Optional delay before filtering</param>
        public async void FilterItemsSource(int delay = 0)
        {
            try
            {
                // Cancel any existing filtering operation
                _filterCancellationTokenSource?.Cancel();
                _filterCancellationTokenSource = new CancellationTokenSource();
                
                var cancellationToken = _filterCancellationTokenSource.Token;

                // Wait for delay if requested
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                // If cancelled, return
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                CommitEdit(DataGridEditingUnit.Row, true);

                // Check if filters are enabled before applying - respects FilterPanel checkbox
                if (FilterPanel?.FiltersEnabled == true)
                {
                    var activeFilters = DataColumns.Where(d => d.SearchTemplateController?.HasCustomExpression == true).ToList();
                    
                    if (activeFilters.Count > 0)
                    {
                        // Determine if async filtering is needed based on dataset size and filter complexity
                        var shouldUseAsyncFiltering = ShouldUseAsyncFiltering(activeFilters);
                        
                        if (shouldUseAsyncFiltering)
                        {
                            await ApplyFiltersAsync(activeFilters, cancellationToken);
                        }
                        else
                        {
                            // Use synchronous filtering for small datasets
                            Items.Filter = item => EvaluateUnifiedFilter(item, activeFilters);
                            SearchFilter = Items.Filter;
                        }
                    }
                    else
                    {
                        Items.Filter = null;
                        SearchFilter = null;
                    }
                }
                else
                {
                    // Filters are disabled - clear filter but preserve definitions
                    Items.Filter = null;
                    SearchFilter = null;
                }

                UpdateFilterPanel();

                // Ensure horizontal scrollbar stays usable when filter produces zero rows
                InjectPlaceholderRowIfEmpty();

                // Update select-all checkbox states after filtering
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateAllSelectAllCheckboxStates();
                }), DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Filter operation was cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering items: {ex.Message}");
            }
        }

        /// <summary>
        /// Unified filter evaluation that handles both regular and collection-context filters with proper AND/OR logic
        /// Performance optimized with cached collection contexts
        /// </summary>
        /// <param name="item">The item to evaluate</param>
        /// <param name="activeFilters">List of all active column filters</param>
        /// <returns>True if the item passes all filters according to their logical operators</returns>
        private bool EvaluateUnifiedFilter(object item, List<ColumnSearchBox> activeFilters)
        {
            if (activeFilters.Count == 0)
                return true;

            try
            {
                // First filter is always included (no preceding operator)
                bool result = EvaluateFilterWithContext(item, activeFilters[0]);

                // Process remaining filters with their logical operators
                for (int i = 1; i < activeFilters.Count; i++)
                {
                    var filter = activeFilters[i];
                    bool filterResult = EvaluateFilterWithContext(item, filter);

                    // Apply the logical operator from this filter
                    // Get the operator from the first search group
                    string operatorName = "And"; // Default
                    if (filter.SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        operatorName = filter.SearchTemplateController.SearchGroups[0].OperatorName ?? "And";
                    }

                    if (operatorName == "Or")
                    {
                        result = result || filterResult;
                    }
                    else // AND is default
                    {
                        result = result && filterResult;
                    }

                    // Short-circuit optimization: if result is false and next operator is AND, we can stop
                    string nextOperator = "And";
                    if (i + 1 < activeFilters.Count && 
                        activeFilters[i + 1].SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        nextOperator = activeFilters[i + 1].SearchTemplateController.SearchGroups[0].OperatorName ?? "And";
                    }
                    if (!result && nextOperator != "Or")
                    {
                        break;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in unified filter evaluation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates a single filter against an item using cached collection contexts for optimal performance.
        /// When a display value provider is configured on the column, text-based filters compare against
        /// the formatted display value instead of the raw value.
        /// </summary>
        private bool EvaluateFilterWithContext(object item, ColumnSearchBox filter)
        {
            try
            {
                // Get the raw property value for this filter
                object propertyValue = ReflectionHelper.GetPropValue(item, filter.BindingPath);

                var controller = filter.SearchTemplateController;

                // Check if this filter requires collection context
                bool needsCollectionContext = DoesFilterRequireCollectionContext(filter);

                if (needsCollectionContext)
                {
                    var collectionContext = GetOrCreateCollectionContext(filter.BindingPath);
                    if (collectionContext != null)
                    {
                        return controller.EvaluateWithCollectionContext(propertyValue, collectionContext);
                    }
                    else
                    {
                        return controller.FilterExpression?.Invoke(propertyValue) ?? true;
                    }
                }

                // When a display value provider is configured, use display-aware evaluation
                // for text-based search types (Contains, StartsWith, Equals, etc.)
                if (controller.HasDisplayValueProvider)
                {
                    return EvaluateWithDisplayValues(propertyValue, controller);
                }

                // Standard evaluation without display transformation
                return controller.FilterExpression?.Invoke(propertyValue) ?? true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error evaluating filter for column {filter.BindingPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates a value against search templates using display values for text-based searches
        /// and raw values for numeric/statistical searches.
        /// For mask-based providers (UseRawComparison=true), compares raw string values
        /// so structural characters (parens, dashes) don't interfere with matching.
        /// </summary>
        private bool EvaluateWithDisplayValues(object rawValue, SearchTemplateController controller)
        {
            if (controller.SearchGroups == null || controller.SearchGroups.Count == 0)
                return true;

            // For mask providers: compare raw string values (unmasked digits/letters)
            // For format/converter providers: compare display-formatted values
            string textComparisonValue;
            if (controller.DisplayValueProvider.UseRawComparison)
                textComparisonValue = rawValue?.ToString() ?? string.Empty;
            else
                textComparisonValue = controller.GetDisplayValue(rawValue);

            bool overallResult = false;

            for (int groupIndex = 0; groupIndex < controller.SearchGroups.Count; groupIndex++)
            {
                var group = controller.SearchGroups[groupIndex];
                bool groupResult = EvaluateGroupWithDisplayValues(rawValue, textComparisonValue, group);

                if (groupIndex == 0)
                {
                    overallResult = groupResult;
                }
                else
                {
                    if (group.OperatorName == "Or")
                        overallResult = overallResult || groupResult;
                    else
                        overallResult = overallResult && groupResult;
                }
            }

            return overallResult;
        }

        /// <summary>
        /// Evaluates a search group using display values for text-based templates.
        /// </summary>
        private bool EvaluateGroupWithDisplayValues(object rawValue, string displayValue, SearchTemplateGroup group)
        {
            if (group.SearchTemplates == null || group.SearchTemplates.Count == 0)
                return true;

            bool groupResult = false;

            for (int i = 0; i < group.SearchTemplates.Count; i++)
            {
                var template = group.SearchTemplates[i];

                // Choose which value to evaluate based on the search type
                object valueToEvaluate = SearchEngine.IsTextBasedSearchType(template.SearchType)
                    ? displayValue   // Text searches compare against display value
                    : rawValue;      // Numeric/date/statistical use raw value

                bool templateResult = template.SearchCondition != null
                    ? SearchEngine.EvaluateCondition(valueToEvaluate, template.SearchCondition)
                    : true;

                if (i == 0)
                {
                    groupResult = templateResult;
                }
                else
                {
                    if (template.OperatorName == "Or")
                        groupResult = groupResult || templateResult;
                    else
                        groupResult = groupResult && templateResult;
                }
            }

            return groupResult;
        }

        /// <summary>
        /// Determines if a filter requires collection context for evaluation
        /// </summary>
        private bool DoesFilterRequireCollectionContext(ColumnSearchBox filter)
        {
            if (filter?.SearchTemplateController?.SearchGroups == null)
                return false;

            // Check if any search template in any group requires collection context
            return filter.SearchTemplateController.SearchGroups
                .SelectMany(g => g.SearchTemplates)
                .Any(t => SearchEngine.RequiresCollectionContext(t.SearchType));
        }

        /// <summary>
        /// Gets or creates a materialized data source for collection context operations
        /// </summary>
        private List<object> GetMaterializedDataSource()
        {
            if (_materializedDataSource == null && originalItemsSource != null)
            {
                _materializedDataSource = originalItemsSource.Cast<object>().ToList();
            }
            return _materializedDataSource;
        }

        /// <summary>
        /// Gets or creates a cached collection context for the specified column
        /// </summary>
        private CollectionContext GetOrCreateCollectionContext(string bindingPath)
        {
            lock (_contextCacheLock)
            {
                if (!_collectionContextCache.TryGetValue(bindingPath, out var context))
                {
                    var materializedData = GetMaterializedDataSource();
                    if (materializedData != null && materializedData.Count > 0)
                    {
                        context = new CollectionContext(materializedData, bindingPath);
                        _collectionContextCache[bindingPath] = context;
                    }
                }
                return context;
            }
        }

        /// <summary>
        /// Clears the collection context cache when the data source changes
        /// </summary>
        private void InvalidateCollectionContextCache()
        {
            lock (_contextCacheLock)
            {
                // Dispose of existing collection contexts to release their cached references
                foreach (var context in _collectionContextCache.Values)
                {
                    if (context is IDisposable disposableContext)
                    {
                        try
                        {
                            disposableContext.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error disposing collection context: {ex.Message}");
                        }
                    }
                }
                
                _collectionContextCache.Clear();
                _materializedDataSource = null;
            }
        }

        /// <summary>
        /// Determines if asynchronous filtering should be used based on dataset size and filter complexity
        /// </summary>
        private bool ShouldUseAsyncFiltering(List<ColumnSearchBox> activeFilters)
        {
            try
            {
                var itemCount = OriginalItemsCount;
                
                // Use async for large datasets (>10k items)
                if (itemCount > 10000)
                    return true;
                
                // Use async for medium datasets with collection context filters
                if (itemCount > 5000 && activeFilters.Any(f => DoesFilterRequireCollectionContext(f)))
                    return true;
                    
                return false;
            }
            catch
            {
                // Default to synchronous filtering if we can't determine
                return false;
            }
        }

        /// <summary>
        /// Applies filters asynchronously with progress reporting and cancellation support
        /// </summary>
        private async Task ApplyFiltersAsync(
            List<ColumnSearchBox> activeFilters, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Pre-build collection contexts on background thread if needed
                await Task.Run(() =>
                {
                    // Pre-create collection contexts for filters that need them
                    foreach (var filter in activeFilters.Where(f => DoesFilterRequireCollectionContext(f)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        GetOrCreateCollectionContext(filter.BindingPath);
                    }
                }, cancellationToken);

                Items.Filter = item => EvaluateUnifiedFilter(item, activeFilters);
                SearchFilter = Items.Filter;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyFiltersAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearAllFilters()
        {
            foreach (var control in DataColumns)
            {
                control.ClearFilter();
            }

            Items.Filter = null;
            SearchFilter = null;

            // Restore normal scrollbar if empty-state override was active
            InjectPlaceholderRowIfEmpty();
        }
        
        /// <summary>
        /// Clears all cached data references to prevent memory leaks when data is cleared
        /// This method should be called when the data source is cleared or replaced
        /// </summary>
        public void ClearAllCachedData()
        {
            // Clear collection context cache and materialized data
            InvalidateCollectionContextCache();

            // Clear cell value snapshots
            _cellValueSnapshots.Clear();
            
            // Dispose of all column controllers to release their cached data
            foreach (var control in DataColumns)
            {
                if (control.SearchTemplateController is IDisposable disposableController)
                {
                    try
                    {
                        disposableController.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing SearchTemplateController: {ex.Message}");
                    }
                }
            }
            
            // Clear filters to release any remaining references
            Items.Filter = null;
            SearchFilter = null;
            
            // Trigger cache manager cleanup
            ColumnValueCacheManager.Instance.Cleanup(clearAll: false);
            
            // Note: Do not force GC.Collect() in library code - let the consumer application
            // manage its own garbage collection timing.
        }

        /// <summary>
        /// Extracts the text content from a column header, handling both simple strings and template headers
        /// </summary>
        /// <param name="column">The DataGrid column</param>
        /// <returns>The extracted header text, or null if no text could be found</returns>
        public static string ExtractColumnHeaderText(DataGridColumn column)
        {
            if (column == null)
                return null;

            var header = column.Header;
            if (header == null)
                return null;

            // If header is already a string, return it directly
            if (header is string headerString)
                return headerString;

            // If header is a FrameworkElement (template), extract text from it
            if (header is FrameworkElement element)
            {
                return ExtractTextFromVisualTree(element);
            }

            // Fallback: try ToString()
            return header.ToString();
        }

        /// <summary>
        /// Recursively extracts text content from a visual tree, prioritizing common text-bearing controls
        /// </summary>
        /// <param name="element">The root element to search</param>
        /// <returns>The extracted text, or null if no text could be found</returns>
        internal static string ExtractTextFromVisualTree(DependencyObject element)
        {
            if (element == null)
                return null;

            // Check common text-bearing controls first
            switch (element)
            {
                case TextBlock textBlock:
                    if (!string.IsNullOrWhiteSpace(textBlock.Text))
                        return textBlock.Text;
                    break;

                case Label label:
                    if (label.Content is string labelText && !string.IsNullOrWhiteSpace(labelText))
                        return labelText;
                    else if (label.Content is FrameworkElement labelContent)
                        return ExtractTextFromVisualTree(labelContent);
                    break;

                case TextBox textBox:
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                        return textBox.Text;
                    break;

                case ContentControl contentControl:
                    if (contentControl.Content is string contentText && !string.IsNullOrWhiteSpace(contentText))
                        return contentText;
                    else if (contentControl.Content is FrameworkElement contentElement)
                        return ExtractTextFromVisualTree(contentElement);
                    break;
            }

            // Recursively search children in the visual tree
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var text = ExtractTextFromVisualTree(child);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return null;
        }

        /// <summary>
        /// Gets the active column filters for the filter panel
        /// </summary>
        /// <returns>Collection of active filter information</returns>
        public IEnumerable<ColumnFilterInfo> GetActiveColumnFilters()
        {
            var activeFilters = new List<ColumnFilterInfo>();
            bool isFirstFilter = true;

            foreach (var column in DataColumns.Where(c => c.HasActiveFilter))
            {
                // Extract the actual operator from the SearchTemplateController
                string logicalOperator = string.Empty;
                if (!isFirstFilter)
                {
                    // Use the first SearchTemplateGroup's OperatorName as the operator for this column
                    if (column.SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        logicalOperator = column.SearchTemplateController.SearchGroups[0].OperatorName?.ToUpper() ?? "AND";
                    }
                    else
                    {
                        // Default to "AND" if no groups exist
                        logicalOperator = "AND";
                    }
                }

                var filterInfo = new ColumnFilterInfo
                {
                    ColumnName = column.ResolveColumnDisplayName() ?? "Unknown",
                    BindingPath = column.BindingPath,
                    IsActive = true,
                    FilterData = column,
                    Operator = logicalOperator
                };

                // Determine filter type and display text
                // PRIORITY: Always check SearchTemplateController first (handles incremental Contains filters)
                if (column.SearchTemplateController?.HasCustomExpression == true)
                {
                    // Get structured components from SearchTemplateController
                    var components = column.SearchTemplateController.GetTokenizedFilterComponents();
                    filterInfo.SearchTypeText = components.SearchTypeText;
                    filterInfo.PrimaryValue = components.PrimaryValue;
                    filterInfo.SecondaryValue = components.SecondaryValue;
                    filterInfo.ValueOperatorText = components.ValueOperatorText;
                    filterInfo.IsDateInterval = components.IsDateInterval;
                    filterInfo.HasNoInputValues = components.HasNoInputValues;
                    
                    // Get all components for complex filters (including multiple Contains templates)
                    var allComponents = column.SearchTemplateController.GetAllTokenizedFilterComponents();
                    filterInfo.FilterComponents.Clear();
                    foreach (var component in allComponents)
                    {
                        filterInfo.FilterComponents.Add(component);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(column.SearchText) && column.HasTemporaryTemplate)
                {
                    // Set component properties for simple filters
                    filterInfo.SearchTypeText = "Contains";
                    filterInfo.PrimaryValue = column.SearchText;
                    filterInfo.HasNoInputValues = false;
                    filterInfo.IsDateInterval = false;
                    
                    // Add single component to collection
                    var simpleComponent = new FilterChipComponents
                    {
                        SearchTypeText = "Contains",
                        PrimaryValue = column.SearchText,
                        HasNoInputValues = false,
                        IsDateInterval = false
                    };
                    simpleComponent.ParsePrimaryValueAsMultipleValues();
                    filterInfo.FilterComponents.Add(simpleComponent);
                }

                activeFilters.Add(filterInfo);
                isFirstFilter = false;
            }

            return activeFilters;
        }

        /// <summary>
        /// Updates the filter panel with current filter state
        /// </summary>
        public void UpdateFilterPanel()
        {
            if (FilterPanel != null)
            {
                var activeFilters = GetActiveColumnFilters();
                FilterPanel.UpdateActiveFilters(activeFilters);
            }
        }

        /// <summary>
        /// Refreshes the filter state for all column search boxes when grid-level EnableRuleFiltering changes.
        /// Only updates columns that don't have explicit EnableRuleFiltering values set.
        /// </summary>
        private void RefreshColumnFilterStates()
        {
            try
            {
                // Only update columns that are inheriting the grid value (not explicitly set)
                foreach (var columnSearchBox in DataColumns)
                {
                    if (columnSearchBox?.GridColumn == null)
                        continue;

                    // Check if EnableRuleFiltering was explicitly set on the descriptor
                    var localValue = columnSearchBox.GridColumn.ReadLocalValue(GridColumn.EnableRuleFilteringProperty);
                    if (localValue == DependencyProperty.UnsetValue)
                    {
                        // Column is inheriting — update it to reflect the new grid value
                        columnSearchBox.UpdateIsComplexFilteringEnabled();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshColumnFilterStates: {ex.Message}");
            }
        }

        #endregion

        #region FilterPanel Event Handlers


        /// <summary>
        /// Handles changes to the filters enabled state
        /// </summary>
        private void OnFiltersEnabledChanged(object sender, FilterEnabledChangedEventArgs e)
        {
            try
            {
                if (e.Enabled)
                {
                    FilterItemsSource();
                }
                else
                {
                    Items.Filter = null;
                    SearchFilter = null;
                    ClearPlaceholderState();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnFiltersEnabledChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to remove a specific filter
        /// </summary>
        private void OnFilterRemoved(object sender, RemoveFilterEventArgs e)
        {
            try
            {
                if (e.FilterInfo?.FilterData is ColumnSearchBox columnSearchBox)
                {
                    columnSearchBox.ClearFilter();
                    FilterItemsSource();
                    UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnFilterRemoved: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to remove a specific value from a filter token
        /// </summary>
        private void OnValueRemovedFromToken(object sender, ValueRemovedFromTokenEventArgs e)
        {
            try
            {
                if (e.RemovableToken?.RemovalContext != null && e.RemovableToken.SourceFilter?.FilterData is ColumnSearchBox columnSearchBox)
                {
                    var template = e.RemovableToken.RemovalContext.ParentTemplate;
                    var controller = columnSearchBox.SearchTemplateController;

                    if (controller != null && template != null)
                    {
                        // Count only templates that have actual filters, not default empty ones
                        var activeTemplateCount = controller.SearchGroups
                            .SelectMany(g => g.SearchTemplates)
                            .Count(t => t.HasCustomFilter);
                        var wouldBeInvalid = !template.WouldBeValidAfterValueRemoval(e.RemovableToken.RemovalContext);

                        if (activeTemplateCount <= 1 && wouldBeInvalid)
                        {
                            // If this is the last active template and it would become invalid, clear the entire filter
                            columnSearchBox.ClearFilter();
                        }
                        else
                        {
                            // Otherwise, handle the value removal normally
                            controller.HandleValueRemoval(template, e.RemovableToken.RemovalContext);

                            // Sync the column's HasActiveFilter state after the controller update.
                            // HandleValueRemoval may remove the template entirely, setting
                            // HasCustomExpression = false, but that doesn't propagate to
                            // the ColumnSearchBox's HasActiveFilter on its own.
                            columnSearchBox.UpdateHasActiveFilterState();
                        }
                    }

                    // Reapply filters and update UI
                    FilterItemsSource();
                    UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnValueRemovedFromToken: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles operator toggle requests from the filter panel
        /// </summary>
        private void OnOperatorToggled(object sender, OperatorToggledEventArgs e)
        {
            try
            {
                if (e.OperatorToken?.SourceFilter?.FilterData is ColumnSearchBox columnSearchBox)
                {
                    var controller = columnSearchBox.SearchTemplateController;
                    if (controller == null)
                        return;

                    // Update the operator based on the level
                    if (e.Level == OperatorLevel.Group)
                    {
                        // Group-level operator: This represents the operator between different columns
                        // The column-level operator is always stored in SearchGroups[0].OperatorName
                        // (See GetActiveColumnFilters line 1208)
                        if (controller.SearchGroups.Count > 0)
                        {
                            controller.SearchGroups[0].OperatorName = e.NewOperator;
                        }
                    }
                    else if (e.Level == OperatorLevel.Template)
                    {
                        // Update the SearchTemplate operator
                        if (e.OperatorToken is TemplateLogicalConnectorToken templateToken)
                        {
                            var groupIndex = templateToken.GroupIndex;
                            var templateIndex = templateToken.TemplateIndex;

                            if (groupIndex >= 0 && groupIndex < controller.SearchGroups.Count)
                            {
                                var group = controller.SearchGroups[groupIndex];
                                if (templateIndex >= 0 && templateIndex < group.SearchTemplates.Count)
                                {
                                    group.SearchTemplates[templateIndex].OperatorName = e.NewOperator;
                                }
                            }
                        }
                    }

                    // Trigger filter update
                    controller.UpdateFilterExpression();

                    // Reapply filters and update UI
                    FilterItemsSource();
                    UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnOperatorToggled: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to clear all filters
        /// </summary>
        private void OnClearAllFiltersRequested(object sender, EventArgs e)
        {
            try
            {
                ClearAllFilters();
                UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnClearAllFiltersRequested: {ex.Message}");
            }
        }


        #endregion
    }
}
