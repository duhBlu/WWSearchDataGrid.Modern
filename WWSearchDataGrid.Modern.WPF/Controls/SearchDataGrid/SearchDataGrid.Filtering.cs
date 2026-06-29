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

        /// <summary>
        /// The Filter Editor's composed tree, when it's the authoritative filter source.
        /// Per-column controllers still hold per-column slices so the per-column popup and
        /// FilterSummaryPanel chip strip stay populated, but when this tree is set, row-level
        /// evaluation uses the compiled tree predicate so cross-column OR groupings work
        /// correctly. Cleared when any non-editor filter path mutates state.
        /// </summary>
        internal FilterGroupNode GridFilterTree { get; set; }

        /// <summary>
        /// Called by non-editor filter paths (auto-filter row, per-column popup, clear-all) to
        /// invalidate the Filter Editor's tree so the per-column AND join resumes as the
        /// authoritative predicate. Safe to call when no tree is set.
        /// </summary>
        internal void InvalidateGridFilterTree()
        {
            GridFilterTree = null;
        }

        private bool HasActiveColumnFilters()
        {
            return DataColumns?.Any(d => d.SearchTemplateController?.HasCustomExpression == true) == true ||
                   DataColumns?.Any(d => d.HasActiveFilter) == true;
        }

        /// <summary>
        /// Apply filters to the items source with performance optimization for large datasets.
        /// Public entry point — invalidates any active Filter Editor tree so the per-column AND
        /// path becomes authoritative again. The editor's own Apply uses
        /// <see cref="FilterItemsSourceFromFilterEditor"/> instead so its just-written tree
        /// survives the call.
        /// </summary>
        /// <param name="delay">Optional delay before filtering</param>
        public void FilterItemsSource(int delay = 0)
        {
            InvalidateGridFilterTree();
            FilterItemsSourceCore(delay);
        }

        /// <summary>
        /// The Filter Editor's Apply path — keeps the tree intact through the call so the
        /// compiler can use it as the authoritative predicate.
        /// </summary>
        internal void FilterItemsSourceFromFilterEditor()
        {
            FilterItemsSourceCore(0);
        }

        private async void FilterItemsSourceCore(int delay)
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

                // Check if filters are enabled before applying - respects FilterSummaryPanel checkbox
                if (FilterSummaryPanel?.FiltersEnabled == true)
                {
                    // Editor-tree path: when the Filter Editor produced a composed tree (e.g.
                    // cross-column OR), compile it into a single row predicate and use that
                    // instead of the per-column AND join. Per-column controllers still hold the
                    // per-column slices so the per-column popup and FilterSummaryPanel chip strip stay
                    // populated; they're a derived view in this mode.
                    var treePredicate = GridFilterTree != null
                        ? GridFilterTreeCompiler.Compile(GridFilterTree)
                        : null;

                    if (treePredicate != null)
                    {
                        ApplyDisplayFilter(item => treePredicate(item));
                    }
                    else
                    {
                        // Iterate GridColumn descriptors, not ColumnSearchBox instances — header
                        // virtualization recycles boxes between columns, but descriptors persist
                        // with their controller and authoritative DisplayIndex.
                        var activeFilters = (GridColumns ?? Enumerable.Empty<GridColumn>())
                            .Where(d => d.SearchTemplateController?.HasCustomExpression == true)
                            .OrderBy(d => d.InternalColumn?.DisplayIndex >= 0 ? d.InternalColumn.DisplayIndex : int.MaxValue)
                            .ToList();

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
                                ApplyDisplayFilter(item => EvaluateUnifiedFilter(item, activeFilters));
                            }
                        }
                        else
                        {
                            ApplyDisplayFilter(null);
                        }
                    }
                }
                else
                {
                    // Filters are disabled - clear filter but preserve definitions
                    ApplyDisplayFilter(null);
                }

                UpdateFilterSummaryPanel();

                // Ensure horizontal scrollbar stays usable when filter produces zero rows
                InjectPlaceholderRowIfEmpty();

                // Update select-all checkbox states after filtering
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RefreshAllSelectAllHeaders();
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
        /// Evaluates regular + collection-context filters together with their AND/OR operators.
        /// </summary>
        /// <param name="item">The item to evaluate</param>
        /// <param name="activeFilters">Descriptors for columns with active filters, in evaluation order.</param>
        /// <returns>True if the item passes all filters according to their logical operators</returns>
        private bool EvaluateUnifiedFilter(object item, List<GridColumn> activeFilters)
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
        /// Column-binding path for a descriptor — used to read the row's value during filtering.
        /// </summary>
        internal static string ResolveBindingPathInternal(GridColumn descriptor) => ResolveBindingPath(descriptor);

        private static string ResolveBindingPath(GridColumn descriptor)
        {
            if (descriptor == null) return null;
            if (!string.IsNullOrEmpty(descriptor.FilterMemberPath)) return descriptor.FilterMemberPath;
            if (!string.IsNullOrEmpty(descriptor.FieldName)) return descriptor.FieldName;

            // Fallback to the generated DataGridColumn's SortMemberPath / Binding.Path.
            var col = descriptor.InternalColumn;
            if (col != null)
            {
                if (!string.IsNullOrEmpty(col.SortMemberPath)) return col.SortMemberPath;
                if (col is DataGridBoundColumn boundColumn)
                    return (boundColumn.Binding as Binding)?.Path?.Path;
            }
            return null;
        }

        /// <summary>
        /// Evaluates a single filter against an item using cached collection contexts for optimal performance.
        /// When a display value provider is configured on the column, text-based filters compare against
        /// the formatted display value instead of the raw value.
        /// </summary>
        private bool EvaluateFilterWithContext(object item, GridColumn filter)
        {
            try
            {
                string bindingPath = ResolveBindingPath(filter);

                // Get the raw property value for this filter
                object propertyValue = ReflectionHelper.GetPropValue(item, bindingPath);

                var controller = filter.SearchTemplateController;

                // Check if this filter requires collection context
                bool needsCollectionContext = DoesFilterRequireCollectionContext(filter);

                if (needsCollectionContext)
                {
                    var collectionContext = GetOrCreateCollectionContext(bindingPath);
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
                Debug.WriteLine($"Error evaluating filter for column {ResolveBindingPath(filter)}: {ex.Message}");
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

                // NotAnd / NotOr: invert the combined group body before joining to the running result.
                if (group.IsNegated) groupResult = !groupResult;

                if (groupIndex == 0)
                {
                    overallResult = groupResult;
                }
                else
                {
                    var groupOp = LogicalOperatorExtensions.Parse(group.OperatorName);
                    bool isOrJoin = groupOp == LogicalOperator.Or || groupOp == LogicalOperator.NotOr;
                    overallResult = isOrJoin
                        ? overallResult || groupResult
                        : overallResult && groupResult;
                }
            }

            return overallResult;
        }

        /// <summary>
        /// Evaluates a search group, choosing display- or raw-value comparison per template
        /// based on the search type AND the type of the stored selected values.
        /// </summary>
        private bool EvaluateGroupWithDisplayValues(object rawValue, string displayValue, SearchTemplateGroup group)
        {
            if (group.SearchTemplates == null || group.SearchTemplates.Count == 0)
                return true;

            bool groupResult = false;

            for (int i = 0; i < group.SearchTemplates.Count; i++)
            {
                var template = group.SearchTemplates[i];

                bool templateResult;
                if (template.SearchType == SearchType.IsAnyOf || template.SearchType == SearchType.IsNoneOf)
                {
                    // IsAnyOf/IsNoneOf keep their operands in SelectedValues, not in SearchCondition's
                    // single RawPrimaryValue — so SearchEngine.EvaluateCondition can't see them and
                    // would reject every row. Match the raw cell value's string form against the
                    // stored value strings, identical to SearchTemplate.BuildIsAnyOfExpression, so
                    // value-picker selections on a display-provider column (e.g. a foreign-key
                    // lookup) filter the same way they do without a provider.
                    templateResult = EvaluateValueSetMembership(rawValue, template);
                }
                else
                {
                    // Raw vs display per template: non-text types always raw; text types use display
                    // for string-typed selected values, raw for typed-object values (FilterValues/picker
                    // store typed objects, search box stores display strings).
                    object valueToEvaluate;
                    if (SearchEngine.IsTextBasedSearchType(template.SearchType) && !TemplateStoresRawValues(template))
                        valueToEvaluate = displayValue;
                    else
                        valueToEvaluate = rawValue;

                    templateResult = template.SearchCondition != null
                        ? SearchEngine.EvaluateCondition(valueToEvaluate, template.SearchCondition)
                        : true;
                }

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
        /// Membership test for IsAnyOf/IsNoneOf that mirrors
        /// <c>SearchTemplate.BuildIsAnyOfExpression</c>: compares the raw cell value's string form
        /// against the template's stored value strings. IsAnyOf is true when the value is present
        /// (a null value is never "any of"); IsNoneOf is true when it is absent (a null value is
        /// always "none of").
        /// </summary>
        private static bool EvaluateValueSetMembership(object rawValue, WWSearchDataGrid.Modern.Core.SearchTemplate template)
        {
            bool contains = false;
            if (rawValue != null && template.SelectedValues != null)
            {
                string valueString = rawValue.ToString();
                foreach (var item in template.SelectedValues)
                {
                    if (item != null && !string.IsNullOrEmpty(item.Value) && string.Equals(item.Value, valueString))
                    {
                        contains = true;
                        break;
                    }
                }
            }

            return template.SearchType == SearchType.IsAnyOf ? contains : !contains;
        }

        /// <summary>
        /// True when the template's selected values are typed (non-string) objects — i.e. came
        /// from a value picker, not the search textbox.
        /// </summary>
        private static bool TemplateStoresRawValues(WWSearchDataGrid.Modern.Core.SearchTemplate template)
        {
            if (template == null) return false;

            if (template.SelectedValues != null && template.SelectedValues.Count > 0)
            {
                foreach (var item in template.SelectedValues)
                {
                    if (item?.Value != null && !(item.Value is string))
                        return true;
                }
                return false;
            }

            return template.SelectedValue != null && !(template.SelectedValue is string);
        }

        /// <summary>
        /// Determines if a filter requires collection context for evaluation
        /// </summary>
        private bool DoesFilterRequireCollectionContext(GridColumn filter)
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
        private bool ShouldUseAsyncFiltering(List<GridColumn> activeFilters)
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
            List<GridColumn> activeFilters,
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
                        GetOrCreateCollectionContext(ResolveBindingPath(filter));
                    }
                }, cancellationToken);

                ApplyDisplayFilter(item => EvaluateUnifiedFilter(item, activeFilters));
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
            InvalidateGridFilterTree();

            foreach (var control in DataColumns)
            {
                control.ClearFilter();
            }

            ApplyDisplayFilter(null);

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
            
            // Release the SearchFilter dependency property reference, but do NOT touch
            // Items.Filter here. Setting Items.Filter = null on a ListCollectionView raises
            // CollectionChanged.Reset, which our own OnCollectionChanged handler responds to
            // by calling ClearAllCachedData — that's an infinite cycle (caused intermittent
            // stack overflows when ItemsSource was a ListCollectionView over a DataView).
            // Items.Filter is reassigned by FilterItemsSource when filter state changes; if
            // ItemsSource itself changes, the old delegate becomes unreachable on its own.
            SearchFilter = null;

            // Trigger cache manager cleanup
            ColumnValueCacheManager.Instance.Cleanup(clearAll: false);
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

            // Iterate descriptors so recycling can't affect chip output. The temporary-template
            // chip case needs a live box per descriptor — that state is per-instance UI state.
            var descriptors = (GridColumns ?? Enumerable.Empty<GridColumn>()).ToList();
            var orderedActive = descriptors
                .Select(desc => new
                {
                    Descriptor = desc,
                    Box = DataColumns.FirstOrDefault(b => b.GridColumn == desc)
                })
                .Where(x =>
                    x.Descriptor.SearchTemplateController?.HasCustomExpression == true
                    || (x.Box != null && x.Box.HasTemporaryTemplate
                        && !string.IsNullOrWhiteSpace(x.Box.SearchText)))
                .OrderBy(x => x.Descriptor.InternalColumn?.DisplayIndex >= 0
                    ? x.Descriptor.InternalColumn.DisplayIndex
                    : int.MaxValue);

            foreach (var entry in orderedActive)
            {
                var descriptor = entry.Descriptor;
                var box = entry.Box;
                var controller = descriptor.SearchTemplateController;

                // Extract the actual operator from the SearchTemplateController
                string logicalOperator = string.Empty;
                if (!isFirstFilter)
                {
                    if (controller?.SearchGroups?.Count > 0)
                    {
                        logicalOperator = controller.SearchGroups[0].OperatorName?.ToUpper() ?? "AND";
                    }
                    else
                    {
                        logicalOperator = "AND";
                    }
                }

                var filterInfo = new ColumnFilterInfo
                {
                    ColumnName = box?.ResolveColumnDisplayName()
                        ?? descriptor.ColumnDisplayName
                        ?? descriptor.HeaderCaption
                        ?? "Unknown",
                    BindingPath = ResolveBindingPath(descriptor),
                    IsActive = true,
                    FilterData = (object)box ?? descriptor,
                    Operator = logicalOperator
                };

                // Determine filter type and display text
                // PRIORITY: Always check SearchTemplateController first (handles incremental Contains filters)
                if (controller?.HasCustomExpression == true)
                {
                    // Get structured components from SearchTemplateController
                    var components = controller.GetTokenizedFilterComponents();
                    filterInfo.SearchTypeText = components.SearchTypeText;
                    filterInfo.PrimaryValue = components.PrimaryValue;
                    filterInfo.SecondaryValue = components.SecondaryValue;
                    filterInfo.ValueOperatorText = components.ValueOperatorText;
                    filterInfo.IsDateInterval = components.IsDateInterval;
                    filterInfo.HasNoInputValues = components.HasNoInputValues;

                    // Get all components for complex filters (including multiple Contains templates).
                    // Stamp the column name on each component so the per-template column token
                    // emitted by FilterTokenConverter renders the right label — today every
                    // component in a filter shares one column, but the field is per-component
                    // so future mixed-column groups can override it without further plumbing.
                    var allComponents = controller.GetAllTokenizedFilterComponents();
                    filterInfo.FilterComponents.Clear();
                    foreach (var component in allComponents)
                    {
                        component.ColumnName = filterInfo.ColumnName;
                        filterInfo.FilterComponents.Add(component);
                    }
                }
                else if (box != null && !string.IsNullOrWhiteSpace(box.SearchText) && box.HasTemporaryTemplate)
                {
                    // Set component properties for simple filters
                    filterInfo.SearchTypeText = "Contains";
                    filterInfo.PrimaryValue = box.SearchText;
                    filterInfo.HasNoInputValues = false;
                    filterInfo.IsDateInterval = false;

                    // Add single component to collection
                    var simpleComponent = new FilterChipComponents
                    {
                        ColumnName = filterInfo.ColumnName,
                        SearchTypeText = "Contains",
                        PrimaryValue = box.SearchText,
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
        /// Updates the filter panel with current filter state. When the editor composed a
        /// multi-column tree (<see cref="GridFilterTree"/> is non-null), renders the tree's
        /// conditions as a single grouped chip so the visual matches the editor's intent.
        /// Otherwise falls back to the per-column chip layout.
        /// </summary>
        public void UpdateFilterSummaryPanel()
        {
            if (FilterSummaryPanel == null) return;

            IEnumerable<ColumnFilterInfo> activeFilters = GridFilterTree != null
                ? GetActiveColumnFiltersFromTree(GridFilterTree)
                : GetActiveColumnFilters();

            FilterSummaryPanel.UpdateActiveFilters(activeFilters);
        }

        /// <summary>
        /// Projects <see cref="GridFilterTree"/> into the FilterSummaryPanel chip model so the chip
        /// strip mirrors the editor's grouping. Two layouts:
        /// <list type="bullet">
        /// <item><description>Or / NotOr / NotAnd root → one unified bracket containing every
        ///   leaf, joined by per-leaf connector tokens. The whole tree shares fate and the
        ///   chip's × clears the entire tree.</description></item>
        /// <item><description>And root → one bracket per top-level child. A
        ///   <see cref="FilterGroupNode"/> child becomes its own bracket (flattening leaves
        ///   inside); a run of consecutive same-column <see cref="FilterConditionNode"/> children
        ///   merges into one bracket so e.g. <c>Total BETWEEN x AND y</c> shows as one chip.
        ///   Each bracket's × removes only that bracket's subtree(s) from the root; the
        ///   remaining tree is reapplied via
        ///   <see cref="FilterEditorTreeBuilder.WriteBackToGrid"/>.</description></item>
        /// </list>
        /// </summary>
        private IEnumerable<ColumnFilterInfo> GetActiveColumnFiltersFromTree(FilterGroupNode root)
        {
            if (root == null || root.Children.Count == 0)
                return Enumerable.Empty<ColumnFilterInfo>();

            // Non-And root: the whole tree shares fate semantically, so render it as a single
            // bracket. Splitting an Or root into per-child brackets would silently change the
            // meaning (each bracket would AND to its neighbor). The chip's × clears the entire
            // tree — passing null subtrees routes ClearAll through the whole-tree clear path,
            // because the root has no parent to detach it from.
            if (root.Operator != LogicalOperator.And)
            {
                var bracket = BuildUnifiedBracket(root, subtreesToRemove: null, isLeadingBracket: true);
                return bracket == null
                    ? Enumerable.Empty<ColumnFilterInfo>()
                    : new[] { bracket };
            }

            var brackets = new List<ColumnFilterInfo>();
            int i = 0;
            while (i < root.Children.Count)
            {
                var child = root.Children[i];

                if (child is FilterGroupNode subgroup)
                {
                    var bracket = BuildUnifiedBracket(subgroup, new FilterEditorNode[] { subgroup }, isLeadingBracket: brackets.Count == 0);
                    if (bracket != null)
                    {
                        if (brackets.Count > 0) bracket.Operator = "AND";
                        brackets.Add(bracket);
                    }
                    i++;
                }
                else if (child is FilterConditionNode firstCond)
                {
                    // Greedy-merge consecutive same-column conditions under the AND root so a
                    // column with two rules (e.g. Total BETWEEN x AND y) renders as one chip.
                    int runEnd = i + 1;
                    while (runEnd < root.Children.Count
                           && root.Children[runEnd] is FilterConditionNode nextCond
                           && nextCond.Column == firstCond.Column)
                    {
                        runEnd++;
                    }

                    var bracket = BuildBracketForConditionRun(root, i, runEnd);
                    if (bracket != null)
                    {
                        if (brackets.Count > 0) bracket.Operator = "AND";
                        brackets.Add(bracket);
                    }
                    i = runEnd;
                }
                else
                {
                    i++;
                }
            }

            return brackets;
        }

        /// <summary>
        /// Builds a single chip containing every leaf under <paramref name="group"/>, joined by
        /// per-leaf connector tokens derived from each leaf's enclosing group operator. The
        /// chip's × removes <paramref name="subtreesToRemove"/> from the tree's parent.
        /// </summary>
        private ColumnFilterInfo BuildUnifiedBracket(FilterGroupNode group, IReadOnlyList<FilterEditorNode> subtreesToRemove, bool isLeadingBracket)
        {
            var leaves = new List<(FilterConditionNode condition, LogicalOperator joinOp)>();
            CollectLeaves(group, group.Operator, leaves);

            if (leaves.Count == 0) return null;

            var touchedColumns = new List<GridColumn>();
            var filterInfo = new ColumnFilterInfo
            {
                ColumnName = string.Empty,
                IsActive = true,
                Operator = string.Empty
            };

            var firstLeaf = leaves[0].condition;
            filterInfo.BindingPath = firstLeaf.Column != null ? ResolveBindingPath(firstLeaf.Column) : null;

            for (int i = 0; i < leaves.Count; i++)
            {
                var (condition, joinOp) = leaves[i];
                if (condition.Column != null && !touchedColumns.Contains(condition.Column))
                    touchedColumns.Add(condition.Column);

                var component = BuildComponentForTreeLeaf(condition);
                if (component == null) continue;

                component.ColumnName = ResolveColumnDisplayNameForTree(condition.Column);
                component.GroupIndex = 0;
                component.TemplateIndex = i;

                if (i > 0)
                {
                    component.Operator = joinOp.ToTokenString().ToUpperInvariant();
                    component.IsGroupLevelOperator = false;
                }

                filterInfo.FilterComponents.Add(component);
            }

            if (filterInfo.FilterComponents.Count == 0) return null;

            var head = filterInfo.FilterComponents[0];
            filterInfo.ColumnName = head.ColumnName;
            filterInfo.SearchTypeText = head.SearchTypeText;
            filterInfo.PrimaryValue = head.PrimaryValue;
            filterInfo.SecondaryValue = head.SecondaryValue;
            filterInfo.ValueOperatorText = head.ValueOperatorText;
            filterInfo.IsDateInterval = head.IsDateInterval;
            filterInfo.HasNoInputValues = head.HasNoInputValues;
            filterInfo.FilterData = new MultiColumnFilterGroupHandle(this, touchedColumns, subtreesToRemove);

            return filterInfo;
        }

        /// <summary>
        /// Builds a chip for a run of consecutive same-column <see cref="FilterConditionNode"/>
        /// children of an AND root. The children's templates appear inside the bracket joined by
        /// inner AND connectors; the × removes the whole run from the tree at once.
        /// </summary>
        private ColumnFilterInfo BuildBracketForConditionRun(FilterGroupNode root, int startIndex, int endExclusive)
        {
            if (endExclusive <= startIndex) return null;

            var first = root.Children[startIndex] as FilterConditionNode;
            if (first == null) return null;

            var touchedColumns = first.Column != null ? new List<GridColumn> { first.Column } : new List<GridColumn>();
            var filterInfo = new ColumnFilterInfo
            {
                ColumnName = string.Empty,
                IsActive = true,
                Operator = string.Empty,
                BindingPath = first.Column != null ? ResolveBindingPath(first.Column) : null
            };

            var runNodes = new List<FilterEditorNode>();
            for (int k = startIndex; k < endExclusive; k++)
            {
                if (!(root.Children[k] is FilterConditionNode cond)) continue;

                runNodes.Add(cond);

                var component = BuildComponentForTreeLeaf(cond);
                if (component == null) continue;

                component.ColumnName = ResolveColumnDisplayNameForTree(cond.Column);
                component.GroupIndex = 0;
                component.TemplateIndex = k - startIndex;
                if (k > startIndex)
                {
                    component.Operator = "AND";
                    component.IsGroupLevelOperator = false;
                }

                filterInfo.FilterComponents.Add(component);
            }

            if (filterInfo.FilterComponents.Count == 0) return null;

            var head = filterInfo.FilterComponents[0];
            filterInfo.ColumnName = head.ColumnName;
            filterInfo.SearchTypeText = head.SearchTypeText;
            filterInfo.PrimaryValue = head.PrimaryValue;
            filterInfo.SecondaryValue = head.SecondaryValue;
            filterInfo.ValueOperatorText = head.ValueOperatorText;
            filterInfo.IsDateInterval = head.IsDateInterval;
            filterInfo.HasNoInputValues = head.HasNoInputValues;
            filterInfo.FilterData = new MultiColumnFilterGroupHandle(this, touchedColumns, runNodes);

            return filterInfo;
        }

        /// <summary>
        /// Depth-first walk of the editor tree that yields each leaf condition paired with the
        /// operator of its most-immediate enclosing group. Subgroups contribute their own
        /// operator for their leaves; the very first leaf carries its parent's op too — callers
        /// strip the operator for the chip's leading component.
        /// </summary>
        private static void CollectLeaves(FilterEditorNode node, LogicalOperator inheritedOp, List<(FilterConditionNode condition, LogicalOperator joinOp)> sink)
        {
            switch (node)
            {
                case FilterConditionNode condition:
                    sink.Add((condition, inheritedOp));
                    break;
                case FilterGroupNode group:
                    foreach (var child in group.Children)
                    {
                        CollectLeaves(child, group.Operator, sink);
                    }
                    break;
            }
        }

        /// <summary>
        /// Builds a <see cref="FilterChipComponents"/> for a single condition leaf using the
        /// owning column's controller for display-value provider context. Falls back to a
        /// minimal stub when the controller can't format the template (e.g. column resolution
        /// failed mid-render).
        /// </summary>
        private static FilterChipComponents BuildComponentForTreeLeaf(FilterConditionNode condition)
        {
            var controller = condition.Column?.SearchTemplateController;
            var template = condition.SearchTemplate;
            if (controller == null || template == null) return null;

            try
            {
                return controller.GetTemplateComponents(template);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BuildComponentForTreeLeaf: {ex.Message}");
                return new FilterChipComponents
                {
                    SearchTypeText = "Advanced filter",
                    HasNoInputValues = false
                };
            }
        }

        private static string ResolveColumnDisplayNameForTree(GridColumn column)
        {
            if (column == null) return "Unknown";
            return column.ColumnDisplayName ?? column.HeaderCaption ?? column.FieldName ?? "Unknown";
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

                    // Check if AllowFilterPopup was explicitly set on the descriptor
                    var localValue = columnSearchBox.GridColumn.ReadLocalValue(GridColumn.AllowFilterPopupProperty);
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

        #region FilterSummaryPanel Event Handlers


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
                    ApplyDisplayFilter(null);
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
                switch (e.FilterInfo?.FilterData)
                {
                    case IColumnFilterHost columnSearchBox:
                        columnSearchBox.ClearFilter();
                        FilterItemsSource();
                        UpdateFilterSummaryPanel();
                        break;
                    case MultiColumnFilterGroupHandle groupHandle:
                        groupHandle.ClearAll();
                        // ClearAll leaves GridFilterTree set when only a subtree was removed.
                        // Use the non-invalidating filter path in that case so the survived tree
                        // remains authoritative; otherwise fall back to the per-column path.
                        if (GridFilterTree != null) FilterItemsSourceFromFilterEditor();
                        else FilterItemsSource();
                        UpdateFilterSummaryPanel();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnFilterRemoved: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to remove a specific value from a filter token. Branches on the chip's
        /// <see cref="ColumnFilterInfo.FilterData"/>:
        /// <list type="bullet">
        /// <item><description><see cref="IColumnFilterHost"/> — the chip is per-column; mutate the
        ///   column's <see cref="SearchTemplateController"/> directly and reapply via the per-column
        ///   path.</description></item>
        /// <item><description><see cref="MultiColumnFilterGroupHandle"/> — the chip was rendered from
        ///   <see cref="GridFilterTree"/>. Resolve the owning column for the template, mutate it,
        ///   and (when the template becomes invalid) detach the corresponding
        ///   <see cref="FilterConditionNode"/> from the tree so the editor view stays in sync.</description></item>
        /// </list>
        /// </summary>
        private void OnValueRemovedFromToken(object sender, ValueRemovedFromTokenEventArgs e)
        {
            try
            {
                var removalContext = e.RemovableToken?.RemovalContext;
                var template = removalContext?.ParentTemplate;
                if (template == null) return;

                switch (e.RemovableToken.SourceFilter?.FilterData)
                {
                    case IColumnFilterHost columnSearchBox:
                        HandleValueRemovedFromColumnHost(columnSearchBox, template, removalContext);
                        break;
                    case MultiColumnFilterGroupHandle handle:
                        HandleValueRemovedFromTree(handle, template, removalContext);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnValueRemovedFromToken: {ex.Message}");
            }
        }

        private void HandleValueRemovedFromColumnHost(IColumnFilterHost columnSearchBox, SearchTemplate template, ValueRemovalContext removalContext)
        {
            var controller = columnSearchBox.SearchTemplateController;
            if (controller == null) return;

            // Count only templates that have actual filters, not default empty ones
            var activeTemplateCount = controller.SearchGroups
                .SelectMany(g => g.SearchTemplates)
                .Count(t => t.HasCustomFilter);
            var wouldBeInvalid = !template.WouldBeValidAfterValueRemoval(removalContext);

            if (activeTemplateCount <= 1 && wouldBeInvalid)
            {
                // If this is the last active template and it would become invalid, clear the entire filter
                columnSearchBox.ClearFilter();
            }
            else
            {
                // Otherwise, handle the value removal normally
                controller.HandleValueRemoval(template, removalContext);

                // HandleValueRemoval may flip HasCustomExpression to false; that
                // doesn't propagate to the box's HasActiveFilter on its own.
                columnSearchBox.UpdateHasActiveFilterState();
            }

            FilterItemsSource();
            UpdateFilterSummaryPanel();
        }

        /// <summary>
        /// Per-value removal for chips rendered from <see cref="GridFilterTree"/>. The chip's
        /// FilterData carries the touched columns; the value token's <see cref="ValueRemovalContext"/>
        /// references the actual <see cref="SearchTemplate"/> shared between the tree and its
        /// owning column's slice. We locate that owning column, then either mutate the template
        /// in place (when the removal keeps it valid — the tree picks up the change for free
        /// since it references the same instance) or detach the corresponding
        /// <see cref="FilterConditionNode"/> from the tree and re-derive per-column slices via
        /// <see cref="FilterEditorTreeBuilder.WriteBackToGrid"/>. Mirrors the pattern
        /// <see cref="MultiColumnFilterGroupHandle.ClearAll"/> uses for whole-chip removal so
        /// per-value removal stays in lockstep with full-chip removal.
        /// </summary>
        private void HandleValueRemovedFromTree(MultiColumnFilterGroupHandle handle, SearchTemplate template, ValueRemovalContext removalContext)
        {
            GridColumn owningColumn = null;
            foreach (var col in handle.TouchedColumns)
            {
                var ctrl = col?.SearchTemplateController;
                if (ctrl == null) continue;
                if (ctrl.SearchGroups.SelectMany(g => g.SearchTemplates).Any(t => ReferenceEquals(t, template)))
                {
                    owningColumn = col;
                    break;
                }
            }
            if (owningColumn == null) return;

            var controller = owningColumn.SearchTemplateController;
            var wouldBeInvalid = !template.WouldBeValidAfterValueRemoval(removalContext);

            if (wouldBeInvalid && GridFilterTree != null)
            {
                var nodeToRemove = FindConditionNodeByTemplate(GridFilterTree, template);
                if (nodeToRemove?.Parent != null)
                {
                    var parent = nodeToRemove.Parent;
                    parent.Children.Remove(nodeToRemove);
                    FilterEditorNormalizer.NormalizeAfterRemoval(parent);
                }

                if (GridFilterTree.Children.Count == 0)
                {
                    // Detachment emptied the tree — clear every touched column and drop the tree
                    // so the per-column AND path resumes authority. Same fallback shape as
                    // MultiColumnFilterGroupHandle.ClearAll's empty-tree branch.
                    foreach (var col in handle.TouchedColumns)
                    {
                        col?.SearchTemplateController?.ClearAndReset();
                    }
                    InvalidateGridFilterTree();
                    FilterItemsSource();
                }
                else
                {
                    // Tree still has structure — rewrite per-column slices so the per-column popup
                    // and chip strip see the new state, then reapply via the editor path so the
                    // tree (not the per-column AND join) remains authoritative.
                    FilterEditorTreeBuilder.WriteBackToGrid(GridFilterTree, this);
                    FilterItemsSourceFromFilterEditor();
                }
            }
            else
            {
                // Template stays valid — mutate values in place. The tree references the same
                // template instance, so its compiled predicate picks up the change automatically.
                controller.HandleValueRemoval(template, removalContext);

                if (GridFilterTree != null)
                    FilterItemsSourceFromFilterEditor();
                else
                    FilterItemsSource();
            }

            UpdateFilterSummaryPanel();
        }

        private static FilterConditionNode FindConditionNodeByTemplate(FilterEditorNode root, SearchTemplate template)
        {
            switch (root)
            {
                case FilterConditionNode c:
                    return ReferenceEquals(c.SearchTemplate, template) ? c : null;
                case FilterGroupNode g:
                    foreach (var child in g.Children)
                    {
                        var found = FindConditionNodeByTemplate(child, template);
                        if (found != null) return found;
                    }
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Handles operator toggle requests from the filter panel
        /// </summary>
        private void OnOperatorToggled(object sender, OperatorToggledEventArgs e)
        {
            try
            {
                // Tree-mode template-level toggle: the unified chip rendered from GridFilterTree
                // emits its inter-leaf joins as template-level connectors but its SourceFilter's
                // FilterData is a MultiColumnFilterGroupHandle, not a column host. Treat any
                // such toggle as a change to the synthesized tree's root operator (v1 supports
                // the simple flat-tree case the FilterSummaryPanel toggle can produce).
                if (e.Level == OperatorLevel.Template
                    && e.OperatorToken?.SourceFilter?.FilterData is MultiColumnFilterGroupHandle
                    && GridFilterTree != null)
                {
                    var newOp = LogicalOperatorExtensions.Parse(e.NewOperator);
                    GridFilterTree.Operator = newOp;

                    // Sync the per-column inter-group OperatorName so the per-column path agrees
                    // when (or if) the tree later invalidates.
                    foreach (var entry in EnumerateActiveColumnsForJoinSync())
                    {
                        entry.firstGroup.OperatorName = e.NewOperator;
                    }

                    if (newOp == LogicalOperator.Or || newOp == LogicalOperator.NotOr)
                    {
                        FilterItemsSourceFromFilterEditor();
                    }
                    else
                    {
                        // Root is now And — drop the tree and let the per-column path own again.
                        InvalidateGridFilterTree();
                        FilterItemsSourceCore(0);
                    }
                    UpdateFilterSummaryPanel();
                    return;
                }

                if (e.OperatorToken?.SourceFilter?.FilterData is IColumnFilterHost columnSearchBox)
                {
                    var controller = columnSearchBox.SearchTemplateController;
                    if (controller == null)
                        return;

                    bool synthesizeTree = false;

                    // Update the operator based on the level
                    if (e.Level == OperatorLevel.Group)
                    {
                        // Inter-column operator lives at SearchGroups[0].OperatorName (matching
                        // the read in GetActiveColumnFilters).
                        if (controller.SearchGroups.Count > 0)
                        {
                            controller.SearchGroups[0].OperatorName = e.NewOperator;
                        }
                        // Group-level toggle is the inter-column join — synthesize a grid-level
                        // tree so the editor and FilterSummaryPanel render cross-column groupings as a
                        // single OR group rather than nested per-column subgroups. Pure-AND
                        // state falls through to the per-column AND path.
                        synthesizeTree = HasAnyInterColumnOrJoin();
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

                    if (synthesizeTree)
                    {
                        GridFilterTree = FilterEditorTreeBuilder.SynthesizeFromColumnJoins(this);
                        FilterItemsSourceFromFilterEditor();
                    }
                    else
                    {
                        FilterItemsSource();
                    }
                    UpdateFilterSummaryPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnOperatorToggled: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns <c>true</c> when at least one non-first active column has an Or/NotOr
        /// <see cref="SearchTemplateGroup.OperatorName"/> on its leading group — i.e., the
        /// user has an inter-column OR join in play. Used by the toggle path to decide whether
        /// to synthesize the grid-level tree.
        /// </summary>
        private bool HasAnyInterColumnOrJoin()
        {
            var active = (GridColumns ?? Enumerable.Empty<GridColumn>())
                .Where(c => c?.SearchTemplateController?.HasCustomExpression == true)
                .OrderBy(c => c.InternalColumn?.DisplayIndex >= 0 ? c.InternalColumn.DisplayIndex : int.MaxValue)
                .ToList();

            for (int i = 1; i < active.Count; i++)
            {
                var firstGroup = active[i].SearchTemplateController?.SearchGroups?.FirstOrDefault();
                if (firstGroup == null) continue;
                if (string.Equals(firstGroup.OperatorName, "Or", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(firstGroup.OperatorName, "NotOr", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Yields the leading <see cref="SearchTemplateGroup"/> of each non-first active column,
        /// in display order. Used by the tree-mode root-operator toggle to mirror the change
        /// across every inter-column join so the per-column path stays consistent if the tree
        /// later invalidates.
        /// </summary>
        private IEnumerable<(GridColumn column, SearchTemplateGroup firstGroup)> EnumerateActiveColumnsForJoinSync()
        {
            var active = (GridColumns ?? Enumerable.Empty<GridColumn>())
                .Where(c => c?.SearchTemplateController?.HasCustomExpression == true)
                .OrderBy(c => c.InternalColumn?.DisplayIndex >= 0 ? c.InternalColumn.DisplayIndex : int.MaxValue)
                .ToList();

            for (int i = 1; i < active.Count; i++)
            {
                var firstGroup = active[i].SearchTemplateController?.SearchGroups?.FirstOrDefault();
                if (firstGroup != null) yield return (active[i], firstGroup);
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
                UpdateFilterSummaryPanel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnClearAllFiltersRequested: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to open the modal Filter Editor window. The panel's button raises
        /// the event; the grid owns the window lifecycle so the editor can reach grid-wide
        /// state (active filters, all columns, etc.).
        /// </summary>
        private void OnOpenFilterEditorRequested(object sender, EventArgs e)
        {
            try
            {
                FilterEditorDialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnOpenFilterEditorRequested: {ex.Message}");
            }
        }


        #endregion
    }
}
