using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.Core.Strategies;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.WPF
{

    /// <summary>
    /// Modern implementation of the SearchDataGrid
    /// </summary>
    public class SearchDataGrid : DataGrid
    {
        #region Fields

        private readonly ObservableCollection<ColumnSearchBox> dataColumns = new ObservableCollection<ColumnSearchBox>();
        private IEnumerable originalItemsSource;
        private bool initialUpdateLayoutCompleted;
        private SearchTemplateController globalFilterController;
        
        // Collection context caching for performance optimization
        private readonly Dictionary<string, ICollectionContext> _collectionContextCache = 
            new Dictionary<string, ICollectionContext>();
        private List<object> _materializedDataSource;
        private readonly object _contextCacheLock = new object();
        
        // Asynchronous filtering support
        private CancellationTokenSource _filterCancellationTokenSource;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SearchFilterProperty =
            DependencyProperty.Register("SearchFilter", typeof(Predicate<object>), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHasItemsProperty =
            DependencyProperty.Register("ActualHasItems", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnActualHasItemsChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data columns collection
        /// </summary>
        public ObservableCollection<ColumnSearchBox> DataColumns
        {
            get { return dataColumns; }
        }

        /// <summary>
        /// Gets or sets the search filter
        /// </summary>
        public Predicate<object> SearchFilter
        {
            get { return (Predicate<object>)GetValue(SearchFilterProperty); }
            set { SetValue(SearchFilterProperty, value); }
        }

        /// <summary>
        /// Gets whether the data source has any items, regardless of filtering
        /// </summary>
        public bool ActualHasItems
        {
            get { return (bool)GetValue(ActualHasItemsProperty); }
            private set { SetValue(ActualHasItemsProperty, value); }
        }

        /// <summary>
        /// Gets the global filter controller
        /// </summary>
        public SearchTemplateController GlobalFilterController
        {
            get
            {
                if (globalFilterController == null)
                {
                    globalFilterController = new SearchTemplateController();

                    // Initialize with first column if available
                    var firstColumn = DataColumns.FirstOrDefault();
                    if (firstColumn != null)
                    {
                        globalFilterController.ColumnName = "Global Filter";
                    }
                }
                return globalFilterController;
            }
        }

        /// <summary>
        /// Gets the original unfiltered items source
        /// </summary>
        public IEnumerable OriginalItemsSource => originalItemsSource;



        /// <summary>
        /// Gets the filter panel control
        /// </summary>
        public FilterPanel FilterPanel { get; private set; }

        /// <summary>
        /// Gets the count of original items for debugging purposes
        /// </summary>
        public int OriginalItemsCount 
        { 
            get 
            { 
                if (originalItemsSource == null) return 0;
                if (originalItemsSource is ICollection collection) return collection.Count;
                return originalItemsSource.Cast<object>().Count();
            } 
        }

        #endregion

        #region Commands

        public ICommand OpenGlobalFilterCommand => new RelayCommand(_ => ShowGlobalFilterWindow());

        #endregion Commands

        #region Events

        /// <summary>
        /// Raised when items are added or removed from the collection
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Event raised when items source is changed
        /// </summary>
        public event EventHandler ItemsSourceChanged;

        /// <summary>
        /// Event raised when items source is filtered
        /// </summary>
        public event EventHandler ItemsSourceFiltered;

        #endregion

        #region Constructor

        public SearchDataGrid() : base()
        {
            // Add binding for DataGrid.Items attached property changes
            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(SearchDataGrid))
                .AddValueChanged(this, (s, e) => UpdateHasItemsProperty());

            // Initialize FilterPanel
            FilterPanel = new FilterPanel();
            
            // Subscribe to FilterPanel events
            FilterPanel.FiltersEnabledChanged += OnFiltersEnabledChanged;
            FilterPanel.FilterRemoved += OnFilterRemoved;
            FilterPanel.EditFiltersRequested += OnEditFiltersRequested;
            FilterPanel.ClearAllFiltersRequested += OnClearAllFiltersRequested;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// When applying the template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            // Wire up editing events to handle transformed data editing
            this.BeginningEdit += OnBeginningEdit;
            this.RowEditEnding += OnRowEditEnding;
            this.CellEditEnding += OnCellEditEnding;
            
            // Get the FilterPanel template part and connect it to our FilterPanel instance
            if (GetTemplateChild("PART_FilterPanel") is FilterPanel templateFilterPanel && FilterPanel != null)
            {
                // Copy the current state from our FilterPanel to the template FilterPanel
                templateFilterPanel.FiltersEnabled = FilterPanel.FiltersEnabled;
                templateFilterPanel.UpdateActiveFilters(FilterPanel.ActiveFilters);
                
                // Wire up events from template FilterPanel to our FilterPanel events
                templateFilterPanel.FiltersEnabledChanged += (s, e) => OnFiltersEnabledChanged(s, e);
                templateFilterPanel.FilterRemoved += (s, e) => OnFilterRemoved(s, e);
                templateFilterPanel.EditFiltersRequested += (s, e) => OnEditFiltersRequested(s, e);
                templateFilterPanel.ClearAllFiltersRequested += (s, e) => OnClearAllFiltersRequested(s, e);
                
                // Replace our FilterPanel property with the template instance so updates go to the right place
                FilterPanel = templateFilterPanel;
            }
        }

        #endregion

        #region Editing Event Handlers

        /// <summary>
        /// Handles the beginning of edit operations
        /// </summary>
        private void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                // Edit handling is simplified - no special transformation tracking needed
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
                // Standard cell editing - no special handling needed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCellEditEnding: {ex.Message}");
            }
        }

        #endregion

        #region Methods

        private static void OnAdvancedFilterModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                // Update the visibility of the advanced filter button in column headers
                grid.UpdateColumnHeaderFilterVisibility();
            }
        }

        /// <summary>
        /// Initialize the global filter controller with column data
        /// </summary>
        private void InitializeGlobalFilterController()
        {
            var controller = GlobalFilterController;

            // Load column data for each column
            foreach (var column in DataColumns)
            {
                if (!string.IsNullOrEmpty(column.BindingPath))
                {
                    var columnValues = new HashSet<object>();
                    foreach (var item in Items)
                    {
                        var value = ReflectionHelper.GetPropValue(item, column.BindingPath);
                        columnValues.Add(value);
                    }

                    controller.LoadColumnData(
                        column.CurrentColumn.Header,
                        columnValues,
                        column.BindingPath);
                }
            }
        }

        /// <summary>
        /// Updates the visibility of advanced filter buttons in column headers
        /// </summary>
        private void UpdateColumnHeaderFilterVisibility()
        {
            // This method will be called when the AdvancedFilterMode changes
            // In a real implementation, you might want to update the XAML bindings instead
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        protected override void OnInitializingNewItem(InitializingNewItemEventArgs e)
        {
            base.OnInitializingNewItem(e);
        }

        protected override void OnAddingNewItem(AddingNewItemEventArgs e)
        {
            base.OnAddingNewItem(e);

            if (Items.Filter != null)
            {
                FilterItemsSource();
            }

            ItemsSourceChanged?.Invoke(this, EventArgs.Empty);
            UpdateLayout();

            // Update ActualHasItems property after item is added
            UpdateHasItemsProperty();
        }

        protected override void OnLoadingRow(DataGridRowEventArgs e)
        {
            base.OnLoadingRow(e);
            if (!initialUpdateLayoutCompleted)
            {
                ItemsSourceChanged?.Invoke(this, null);
                UpdateLayout();
                initialUpdateLayoutCompleted = true;
            }
        }

        private static void OnActualHasItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                // Force column headers to update
                grid.InvalidateVisual();

                // If we now have items and didn't before, we may need to adjust layout
                if ((bool)e.NewValue && !(bool)e.OldValue)
                {
                    // Ensure column headers update their layout
                    grid.UpdateLayout();
                }
            }
        }

        /// <summary>
        /// When items source changes, notify controls with safeguards against recursive calls
        /// </summary>
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            try
            {
                base.OnItemsSourceChanged(oldValue, newValue);

                // Clear cached data from the old data source to prevent memory leaks
                if (oldValue != null && newValue != oldValue)
                {
                    ClearAllCachedData();
                }

                originalItemsSource = newValue;
                
                // Invalidate collection context cache when data source changes
                InvalidateCollectionContextCache();

                // Register for collection changed events if the source supports it
                UnregisterCollectionChangedEvent(oldValue);
                RegisterCollectionChangedEvent(newValue);

                if (newValue != null)
                {
                    // Update ActualHasItems property
                    UpdateHasItemsProperty();

                    // Notify controls that items source has changed
                    ItemsSourceChanged?.Invoke(this, EventArgs.Empty);

                    // Apply any existing filters
                    if (Items.Filter != null)
                    {
                        FilterItemsSource();
                    }
                    UpdateLayout();
                }
                else
                {
                    // If items source is null, set ActualHasItems to false and clear cached data
                    ActualHasItems = false;
                    ClearAllCachedData();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnItemsSourceChanged: {ex.Message}");
            }
        }

        private void RegisterCollectionChangedEvent(IEnumerable collection)
        {
            if (collection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void UnregisterCollectionChangedEvent(IEnumerable collection)
        {
            if (collection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged -= OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update ActualHasItems property when collection changes
            UpdateHasItemsProperty();
            
            // Invalidate collection context cache when items are added/removed
            // This ensures statistical calculations reflect the current data
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                InvalidateCollectionContextCache();
            }
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearAllCachedData();
            }

            CollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Updates the ActualHasItems property based on the original items source
        /// </summary>
        private void UpdateHasItemsProperty()
        {
            bool hasAnyItems = false;

            // Check if the original items source has any items
            if (originalItemsSource != null)
            {
                // Different ways to check if collection has items
                if (originalItemsSource is ICollection collection)
                {
                    hasAnyItems = collection.Count > 0;
                }
                else
                {
                    // For other enumerable types, check if there's at least one item
                    var enumerator = originalItemsSource.GetEnumerator();
                    hasAnyItems = enumerator.MoveNext();

                    // Dispose the enumerator if it's disposable
                    if (enumerator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            // Update property if it changed
            if (ActualHasItems != hasAnyItems)
            {
                ActualHasItems = hasAnyItems;
            }
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

                // Commit any edits
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

                // Notify that items have been filtered
                ItemsSourceFiltered?.Invoke(this, EventArgs.Empty);

                // Update filter panel
                UpdateFilterPanel();
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
        /// Evaluates a single filter against an item using cached collection contexts for optimal performance
        /// </summary>
        private bool EvaluateFilterWithContext(object item, ColumnSearchBox filter)
        {
            try
            {
                // Get the property value for this filter
                object propertyValue = ReflectionHelper.GetPropValue(item, filter.BindingPath);

                // Check if this filter requires collection context
                bool needsCollectionContext = DoesFilterRequireCollectionContext(filter);

                if (needsCollectionContext)
                {
                    // Use cached collection context for this column
                    var collectionContext = GetOrCreateCollectionContext(filter.BindingPath);
                    if (collectionContext != null)
                    {
                        return filter.SearchTemplateController.EvaluateWithCollectionContext(propertyValue, collectionContext);
                    }
                    else
                    {
                        // Fallback to standard evaluation if context creation failed
                        return filter.SearchTemplateController.FilterExpression(propertyValue);
                    }
                }
                else
                {
                    // Standard evaluation without collection context
                    return filter.SearchTemplateController.FilterExpression(propertyValue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error evaluating filter for column {filter.BindingPath}: {ex.Message}");
                return false;
            }
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
        private ICollectionContext GetOrCreateCollectionContext(string bindingPath)
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
        /// Shows the global filter window
        /// </summary>
        private void ShowGlobalFilterWindow()
        {
            // Initialize the global filter controller if needed
            InitializeGlobalFilterController();

            var window = new Window
            {
                Title = "Advanced Filter (Global)",
                Width = 800,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var filterControl = new ColumnFilterEditor
            {
                SearchTemplateController = GlobalFilterController,
                DataContext = this
            };

            window.Content = filterControl;
            window.Closed += (s, e) =>
            {
                // Apply filters after the window is closed
                if (GlobalFilterController.HasCustomExpression)
                {
                    FilterItemsSource();
                }
            };

            window.ShowDialog();
        }

        /// <summary>
        /// Evaluate a filter against an item for per-column filtering
        /// </summary>
        private static bool EvaluateFilter(object item, ColumnSearchBox filter)
        {
            try
            {
                // Standard filtering: get the property value and evaluate
                object propertyValue = ReflectionHelper.GetPropValue(item, filter.BindingPath);
                return filter.SearchTemplateController.FilterExpression(propertyValue);
            }
            catch
            {
                return false;
            }
        }

        private static bool EvaluateMultiColumnFilter(object item, List<ColumnSearchBox> activeFilters)
        {
            if (!(activeFilters.Count > 0))
                return true;

            // First filter is always included (no preceding operator)
            bool result = EvaluateFilter(item, activeFilters[0]);

            // Process remaining filters with their logical operators
            for (int i = 1; i < activeFilters.Count; i++)
            {
                var filter = activeFilters[i];
                bool filterResult = EvaluateFilter(item, filter);

                // Get the logical operator for this column from its SearchTemplateController
                // The operator defines how this column should be combined with previous results
                string operatorName = "AND"; // Default to AND
                if (filter.SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    operatorName = filter.SearchTemplateController.SearchGroups[0].OperatorName?.ToUpper() ?? "AND";
                }

                // Apply the logical operator
                if (operatorName == "OR")
                {
                    result = result || filterResult;
                }
                else // AND or any other value
                {
                    result = result && filterResult;
                }
            }

            return result;
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

            globalFilterController?.ClearAndReset();

            Items.Filter = null;
            SearchFilter = null;

            ForceRestoreOriginalData();

            // Notify that items have been filtered
            ItemsSourceFiltered?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Clears all cached data references to prevent memory leaks when data is cleared
        /// This method should be called when the data source is cleared or replaced
        /// </summary>
        public void ClearAllCachedData()
        {
            // Clear collection context cache and materialized data
            InvalidateCollectionContextCache();
            
            // Clear data references in all column controllers
            foreach (var control in DataColumns)
            {
                control.SearchTemplateController?.ClearDataReferences();
            }
            
            // Clear global filter controller data references
            globalFilterController?.ClearDataReferences();
            
            // Clear filters to release any remaining references
            Items.Filter = null;
            SearchFilter = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Forces restoration of the original data source
        /// </summary>
        public void ForceRestoreOriginalData()
        {
            if (originalItemsSource != null)
            {
                // No longer needed - using unified filtering approach
                
                // Clear the filter to show all items from original source
                Items.Filter = null;
                
                Debug.WriteLine($"ForceRestoreOriginalData: ItemsSource count = {Items.Count} (event connectivity preserved)");
            }
        }

        /// <summary>
        /// Save filters to file
        /// </summary>
        public void SaveFilters()
        {
            // Show save dialog
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".json",
                Filter = "Filter files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Create filter data to save
                    object filterData;

                     // Save global filter
                    filterData = new
                    {
                        FilterMode = "Global",
                        GlobalFilterController.SearchGroups
                    };

                    // Serialize to JSON
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(filterData,
                        Newtonsoft.Json.Formatting.Indented);

                    // Save to file
                    System.IO.File.WriteAllText(dialog.FileName, json);

                    MessageBox.Show("Filters saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving filters: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Load filters from file
        /// </summary>
        public void LoadFilters()
        {
            // Show open dialog
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "Filter files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Read the JSON file
                    string json = System.IO.File.ReadAllText(dialog.FileName);

                    // Try to determine if this is a global or per-column filter
                    bool isGlobal = json.Contains("\"FilterMode\":\"Global\"");


                    // Clear existing filters
                    ClearAllFilters();


                    // Deserialize from JSON
                    var filterData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(json);

                    // Apply filters to matching columns
                    foreach (var filter in filterData)
                    {
                        // Find matching column
                        var column = DataColumns.FirstOrDefault(c =>
                            c.CurrentColumn.Header.ToString() == (string)filter.ColumnName &&
                            c.BindingPath == (string)filter.BindingPath);

                        if (column != null)
                        {
                            // Apply the filter
                            // (Note: This needs more detailed implementation to restore search groups)
                            column.SearchTemplateController.HasCustomExpression = true;
                        }
                    }

                    // Apply the filters
                    FilterItemsSource();

                    MessageBox.Show("Filters loaded successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading filters: {ex.Message}");
                }
            }
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
                    ColumnName = column.CurrentColumn?.Header?.ToString() ?? "Unknown",
                    BindingPath = column.BindingPath,
                    IsActive = true,
                    FilterData = column,
                    Operator = logicalOperator
                };

                // Determine filter type and display text
                // PRIORITY: Always check SearchTemplateController first (handles incremental Contains filters)
                if (column.SearchTemplateController?.HasCustomExpression == true)
                {
                    filterInfo.DisplayText = column.SearchTemplateController.GetFilterDisplayText();
                    
                    // Get structured components from SearchTemplateController
                    var components = column.SearchTemplateController.GetTokenizedFilter();
                    filterInfo.SearchTypeText = components.SearchTypeText;
                    filterInfo.PrimaryValue = components.PrimaryValue;
                    filterInfo.SecondaryValue = components.SecondaryValue;
                    filterInfo.ValueOperatorText = components.ValueOperatorText;
                    filterInfo.IsDateInterval = components.IsDateInterval;
                    filterInfo.HasNoInputValues = components.HasNoInputValues;
                    
                    // Get all components for complex filters (including multiple Contains templates)
                    var allComponents = column.SearchTemplateController.GetAllFilterComponents();
                    filterInfo.FilterComponents.Clear();
                    foreach (var component in allComponents)
                    {
                        filterInfo.FilterComponents.Add(component);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(column.SearchText) && column.HasTemporaryTemplate)
                {
                    // FIXED: Only use SearchText fallback if we have an actual temporary template
                    // This ensures synchronization between HasActiveFilter state and actual template existence
                    filterInfo.DisplayText = $"Contains '{column.SearchText}'";
                    
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
                    // Re-apply existing filters
                    FilterItemsSource();
                }
                else
                {
                    // Clear ALL filtering - both regular filters AND transformations
                    Items.Filter = null;
                    SearchFilter = null;
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
        /// Handles requests to open the edit filters dialog
        /// </summary>
        private void OnEditFiltersRequested(object sender, EventArgs e)
        {
            try
            {
                // Create the DataGridFilterEditor custom control
                var DataGridFilterEditor = new DataGridFilterEditor
                {
                    SourceDataGrid = this
                };

                // Create a window to host the custom control
                var window = new Window
                {
                    Title = "Edit Filters",
                    Height = 600,
                    Width = 900,
                    MinHeight = 500,
                    MinWidth = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    Content = DataGridFilterEditor,
                    Background = System.Windows.Media.Brushes.White
                };

                // Handle dialog closing
                DataGridFilterEditor.DialogClosing += (s, args) =>
                {
                    window.Close();
                    
                    // Update filter panel if changes were accepted
                    if (args.Accepted)
                    {
                        UpdateFilterPanel();
                    }
                };

                // Show the dialog
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnEditFiltersRequested: {ex.Message}");
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

        #endregion
    }
}