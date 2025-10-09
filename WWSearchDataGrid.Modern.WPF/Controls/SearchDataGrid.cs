using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.WPF.Commands;
using System.Windows.Controls.Primitives;

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
        private readonly Dictionary<string, CollectionContext> _collectionContextCache =
            new Dictionary<string, CollectionContext>();
        private List<object> _materializedDataSource;
        private readonly object _contextCacheLock = new object();

        // Asynchronous filtering support
        private CancellationTokenSource _filterCancellationTokenSource;

        // Cell value change detection support
        private readonly Dictionary<string, object> _cellValueSnapshots = new Dictionary<string, object>();

        // Grouping support
        private GroupPanel _groupPanel;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SearchFilterProperty =
            DependencyProperty.Register("SearchFilter", typeof(Predicate<object>), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHasItemsProperty =
            DependencyProperty.Register("ActualHasItems", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnActualHasItemsChanged));

        /// <summary>
        /// Dependency property for EnableComplexFiltering
        /// </summary>
        public static readonly DependencyProperty EnableComplexFilteringProperty =
            DependencyProperty.Register("EnableComplexFiltering", typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Dependency property for EnableGrouping
        /// </summary>
        public static readonly DependencyProperty EnableGroupingProperty =
            DependencyProperty.Register("EnableGrouping", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnEnableGroupingChanged));

        /// <summary>
        /// Attached property for default GroupStyle
        /// </summary>
        public static readonly DependencyProperty DefaultGroupStyleProperty =
            DependencyProperty.Register("DefaultGroupStyle", typeof(GroupStyle), typeof(SearchDataGrid),
                new PropertyMetadata(null, OnDefaultGroupStyleChanged));

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
        /// Gets or sets whether complex filtering is enabled at the grid level.
        /// When false, all columns use simple filtering mode only.
        /// When true, per-column EnableComplexFiltering settings are respected.
        /// </summary>
        public bool EnableComplexFiltering
        {
            get { return (bool)GetValue(EnableComplexFilteringProperty); }
            set { SetValue(EnableComplexFilteringProperty, value); }
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
        /// Gets or sets whether grouping functionality is enabled
        /// </summary>
        public bool EnableGrouping
        {
            get { return (bool)GetValue(EnableGroupingProperty); }
            set { SetValue(EnableGroupingProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets whether grouping functionality is enabled
        /// </summary>
        public GroupStyle DefaultGroupStyle
        {
            get { return (GroupStyle)GetValue(DefaultGroupStyleProperty); }
            set { SetValue(DefaultGroupStyleProperty, value); }
        }

        /// <summary>
        /// Gets the group panel control instance
        /// </summary>
        public GroupPanel GroupPanel
        {
            get { return _groupPanel; }
            private set { _groupPanel = value; }
        }

        /// <summary>
        /// Gets whether grouping is currently enabled
        /// </summary>
        public bool IsGroupingEnabled => EnableGrouping;

        /// <summary>
        /// Gets the current grouped columns
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<GroupColumnInfo> GroupedColumns =>
            GroupPanel?.GroupedColumns?.ToList() ?? new System.Collections.Generic.List<GroupColumnInfo>();

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
        /// Event raised when a cell value is changed through editing
        /// </summary>
        public event EventHandler<CellValueChangedEventArgs> CellValueChanged;

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
            FilterPanel.ValueRemovedFromToken += OnValueRemovedFromToken;
            FilterPanel.OperatorToggled += OnOperatorToggled;
            FilterPanel.ClearAllFiltersRequested += OnClearAllFiltersRequested;

            // Initialize context menu functionality
            this.InitializeContextMenu();

            // Add keyboard shortcuts for copy operations
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesCommand.Execute(this)), Key.C, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand.Execute(this)), Key.C, ModifierKeys.Control | ModifierKeys.Shift));
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
                templateFilterPanel.ValueRemovedFromToken += (s, e) => OnValueRemovedFromToken(s, e);
                templateFilterPanel.OperatorToggled += (s, e) => OnOperatorToggled(s, e);
                templateFilterPanel.ClearAllFiltersRequested += (s, e) => OnClearAllFiltersRequested(s, e);

                // Replace our FilterPanel property with the template instance so updates go to the right place
                FilterPanel = templateFilterPanel;
            }

            // Get the GroupPanel template part and connect it to our GroupPanel instance (if grouping is enabled)
            if (IsGroupingEnabled && GetTemplateChild("PART_GroupPanel") is GroupPanel templateGroupPanel)
            {
                // If we already have a GroupPanel instance with state, transfer it
                if (GroupPanel != null && GroupPanel.GroupedColumns != null && GroupPanel.GroupedColumns.Count > 0)
                {
                    // Copy grouped columns to template instance
                    foreach (var groupCol in GroupPanel.GroupedColumns)
                    {
                        templateGroupPanel.GroupedColumns.Add(groupCol);
                    }
                    templateGroupPanel.IsPanelVisible = GroupPanel.IsPanelVisible;
                    templateGroupPanel.Visibility = GroupPanel.Visibility;
                }

                // Wire up events from template GroupPanel
                templateGroupPanel.GroupingChanged += OnGroupingChanged;
                templateGroupPanel.ExpandCollapseAllRequested += OnExpandCollapseAllRequested;
                templateGroupPanel.ParentDataGrid = this;

                // Replace our GroupPanel property with the template instance
                GroupPanel = templateGroupPanel;
            }

            // Apply the default GroupStyle from attached property if not already set
            ApplyDefaultGroupStyle();
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

        #region Methods

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

                // Clear cell value snapshots when data source changes
                _cellValueSnapshots.Clear();

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

                    // Apply any existing filters - check for active column filters, not just Items.Filter
                    if (HasActiveColumnFilters() || Items.Filter != null)
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
        /// Checks if there are any active column filters that should be applied to new data
        /// </summary>
        /// <returns>True if there are active column filters</returns>
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
            
            // Dispose of global filter controller
            if (globalFilterController is IDisposable disposableGlobalController)
            {
                try
                {
                    disposableGlobalController.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disposing global filter controller: {ex.Message}");
                }
            }
            globalFilterController = null; // Clear reference after disposal
            
            // Clear filters to release any remaining references
            Items.Filter = null;
            SearchFilter = null;
            
            // Trigger cache manager cleanup
            WWSearchDataGrid.Modern.Core.Caching.ColumnValueCacheManager.Instance.Cleanup(clearAll: false);
            
            // Force garbage collection to reclaim memory immediately
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
        /// Extracts the text content from a column header, handling both simple strings and template headers
        /// </summary>
        /// <param name="column">The DataGrid column</param>
        /// <returns>The extracted header text, or null if no text could be found</returns>
        internal static string ExtractColumnHeaderText(DataGridColumn column)
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

                case System.Windows.Controls.Label label:
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
                    ColumnName = ExtractColumnHeaderText(column.CurrentColumn) ?? "Unknown",
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
                    FilterItemsSource();
                }
                else
                {
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
                        // Check if this is the last template in the controller and if removing the value would make it invalid
                        var totalTemplates = controller.SearchGroups.SelectMany(g => g.SearchTemplates).Count();
                        var wouldBeInvalid = !template.WouldBeValidAfterValueRemoval(e.RemovableToken.RemovalContext);

                        if (totalTemplates <= 1 && wouldBeInvalid)
                        {
                            // If this is the last template and it would become invalid, clear the entire filter
                            columnSearchBox.ClearFilter();
                        }
                        else
                        {
                            // Otherwise, handle the value removal normally
                            controller.HandleValueRemoval(template, e.RemovableToken.RemovalContext);
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

        #region Cell Value Change Detection

        /// <summary>
        /// Gets the binding path for a DataGrid column
        /// </summary>
        private string GetColumnBindingPath(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is System.Windows.Data.Binding binding)
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
        /// Internal handler for cell value changes that updates caches and raises events
        /// </summary>
        private void OnCellValueChangedInternal(object item, DataGridColumn column, string bindingPath,
            object oldValue, object newValue, int rowIndex, int columnIndex)
        {
            try
            {
                // Find the corresponding column search box
                var columnSearchBox = DataColumns.FirstOrDefault(d => d.BindingPath == bindingPath);
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

        #region Grouping Methods

        /// <summary>
        /// Callback handler for DefaultGroupStyle property changes
        /// </summary>
        private static void OnDefaultGroupStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                grid.GroupStyle.Clear();
                grid.ApplyDefaultGroupStyle();
            }
        }

        /// <summary>
        /// Applies the default GroupStyle from the attached property
        /// </summary>
        private void ApplyDefaultGroupStyle()
        {
            // Only apply if no custom GroupStyle is already set
            if (this.GroupStyle.Count == 0)
            {
                var defaultGroupStyle = DefaultGroupStyle;
                if (defaultGroupStyle != null)
                {
                    this.GroupStyle.Add(defaultGroupStyle);
                }
            }
        }

        /// <summary>
        /// Callback handler for EnableGrouping property changes
        /// </summary>
        private static void OnEnableGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                bool isEnabled = (bool)e.NewValue;

                if (isEnabled)
                {
                    // Initialize GroupPanel if not already created
                    if (grid.GroupPanel == null)
                    {
                        grid.GroupPanel = new GroupPanel();
                        grid.GroupPanel.ParentDataGrid = grid;
                        grid.GroupPanel.Visibility = Visibility.Collapsed; // Start collapsed until user groups a column
                        grid.GroupPanel.IsPanelVisible = false; // Explicitly set to false

                        // Subscribe to GroupPanel events
                        grid.GroupPanel.GroupingChanged += grid.OnGroupingChanged;
                        grid.GroupPanel.ExpandCollapseAllRequested += grid.OnExpandCollapseAllRequested;

                        Debug.WriteLine("GroupPanel initialized and ready (collapsed by default)");
                    }
                }
                else
                {
                    // Disable grouping - clear any existing groups and hide panel
                    if (grid.GroupPanel != null)
                    {
                        grid.ClearGrouping();
                        grid.GroupPanel.Visibility = Visibility.Collapsed;
                        grid.GroupPanel.IsPanelVisible = false;
                        Debug.WriteLine("Grouping disabled - panel hidden and groups cleared");
                    }
                }

                // Enable/disable drag-drop on column headers
                grid.UpdateColumnHeaderDragDrop(isEnabled);
            }
        }

        /// <summary>
        /// Applies grouping to the CollectionView.
        /// Called when GroupPanel.GroupingChanged fires.
        /// </summary>
        private void ApplyGrouping(System.Collections.Generic.IEnumerable<GroupColumnInfo> groupedColumns)
        {
            try
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(ItemsSource);
                if (view == null) return;

                using (view.DeferRefresh())
                {
                    // Clear existing grouping
                    view.GroupDescriptions.Clear();

                    // Add new grouping in hierarchical order
                    if (groupedColumns != null && groupedColumns.Any())
                    {
                        foreach (var groupCol in groupedColumns.OrderBy(g => g.GroupLevel))
                        {
                            if (!string.IsNullOrEmpty(groupCol.BindingPath))
                            {
                                view.GroupDescriptions.Add(
                                    new PropertyGroupDescription(groupCol.BindingPath));
                            }
                        }
                    }
                }

                // Hide/show columns based on grouping
                UpdateGroupedColumnVisibility(groupedColumns);

                // Refresh the view
                view.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying grouping: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates column visibility based on which columns are grouped
        /// </summary>
        private void UpdateGroupedColumnVisibility(System.Collections.Generic.IEnumerable<GroupColumnInfo> groupedColumns)
        {
            try
            {
                var groupedPaths = groupedColumns?.Select(g => g.BindingPath).ToHashSet() ?? new HashSet<string>();

                foreach (var column in Columns)
                {
                    var bindingPath = GetColumnBindingPath(column);
                    if (!string.IsNullOrEmpty(bindingPath))
                    {
                        // Hide column if it's grouped
                        if (groupedPaths.Contains(bindingPath))
                        {
                            // Store original visibility state if not already stored
                            if (!(bool)column.GetValue(OriginalVisibilityProperty))
                            {
                                column.SetValue(OriginalVisibilityProperty, column.Visibility);
                            }
                            column.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // Restore original visibility if it was hidden due to grouping
                            var originalVisibility = column.GetValue(OriginalVisibilityProperty);
                            if (originalVisibility is Visibility vis)
                            {
                                column.Visibility = vis;
                                column.ClearValue(OriginalVisibilityProperty);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating grouped column visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Attached property to store original column visibility
        /// </summary>
        private static readonly DependencyProperty OriginalVisibilityProperty =
            DependencyProperty.RegisterAttached("OriginalVisibility", typeof(Visibility), typeof(SearchDataGrid),
                new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Updates drag-drop capability on column headers
        /// </summary>
        private void UpdateColumnHeaderDragDrop(bool enable)
        {
            try
            {
                if (enable)
                {
                    // Subscribe to column header events for drag-drop
                    this.Loaded += OnSearchDataGridLoaded;
                }
                else
                {
                    // Unsubscribe from events
                    this.Loaded -= OnSearchDataGridLoaded;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating column header drag-drop: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the Loaded event to attach drag-drop handlers to column headers
        /// </summary>
        private void OnSearchDataGridLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Find all DataGridColumnHeader elements and attach mouse handlers
                AttachColumnHeaderHandlers(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSearchDataGridLoaded: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively finds and attaches mouse handlers to column headers
        /// </summary>
        private void AttachColumnHeaderHandlers(DependencyObject parent)
        {
            if (parent == null) return;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is DataGridColumnHeader header)
                {
                    // Detach first to avoid duplicate subscriptions
                    header.MouseMove -= OnColumnHeaderMouseMove;
                    header.MouseMove += OnColumnHeaderMouseMove;
                }

                // Recursively search children
                AttachColumnHeaderHandlers(child);
            }
        }

        /// <summary>
        /// Handles mouse move on column headers to initiate drag operation
        /// </summary>
        private void OnColumnHeaderMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is DataGridColumnHeader header)
            {
                try
                {
                    // Only allow drag if grouping is enabled
                    if (!IsGroupingEnabled || header.Column == null)
                        return;

                    // Create drag data
                    var dragData = new DataObject(typeof(DataGridColumn), header.Column);

                    // Initiate drag-drop operation
                    DragDrop.DoDragDrop(header, dragData, DragDropEffects.Copy);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in OnColumnHeaderMouseMove: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears all grouping and resets to flat view
        /// </summary>
        public void ClearGrouping()
        {
            GroupPanel?.ClearAllGrouping();
            ApplyGrouping(System.Linq.Enumerable.Empty<GroupColumnInfo>());
        }

        /// <summary>
        /// Event handler for GroupPanel.GroupingChanged
        /// </summary>
        private void OnGroupingChanged(object sender, EventArgs e)
        {
            if (GroupPanel?.GroupedColumns != null)
            {
                // Automatically show the GroupPanel when columns are grouped
                if (GroupPanel.GroupedColumns.Count > 0)
                {
                    GroupPanel.IsPanelVisible = true;
                    GroupPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    // Hide panel when all groups are removed
                    GroupPanel.Visibility = Visibility.Collapsed;
                }

                ApplyGrouping(GroupPanel.GroupedColumns);
            }
        }

        /// <summary>
        /// Event handler for GroupPanel.ExpandCollapseAllRequested
        /// </summary>
        private void OnExpandCollapseAllRequested(object sender, bool expand)
        {
            try
            {
                // Access the CollectionView and iterate through groups
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(ItemsSource);
                if (view?.Groups == null) return;

                ExpandOrCollapseGroups(view.Groups, expand);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnExpandCollapseAllRequested: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively expands or collapses all group levels
        /// </summary>
        private void ExpandOrCollapseGroups(ReadOnlyObservableCollection<object> groups, bool expand)
        {
            foreach (CollectionViewGroup group in groups)
            {
                // Set IsExpanded on the group container
                SetGroupExpanded(group, expand);

                // Recursively handle subgroups
                if (group.Items.Count > 0 && group.Items[0] is CollectionViewGroup)
                {
                    var subGroupsList = group.Items.Cast<object>().ToList();
                    var subGroupsCollection = new ObservableCollection<object>(subGroupsList);
                    var readOnlySubGroups = new ReadOnlyObservableCollection<object>(subGroupsCollection);
                    ExpandOrCollapseGroups(readOnlySubGroups, expand);
                }
            }
        }

        /// <summary>
        /// Sets the IsExpanded property on a group's visual container
        /// </summary>
        private void SetGroupExpanded(CollectionViewGroup group, bool expand)
        {
            try
            {
                // Find the GroupItem container for this group in the visual tree
                var groupItem = FindGroupItem(this, group);
                if (groupItem != null)
                {
                    // Find the Expander within the GroupItem
                    var expander = FindVisualChild<System.Windows.Controls.Expander>(groupItem);
                    if (expander != null)
                    {
                        expander.IsExpanded = expand;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting group expanded state: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the GroupItem container for a specific CollectionViewGroup
        /// </summary>
        private System.Windows.Controls.GroupItem FindGroupItem(DependencyObject parent, CollectionViewGroup group)
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.GroupItem groupItem && groupItem.Content == group)
                {
                    return groupItem;
                }

                // Recursively search children
                var result = FindGroupItem(child, group);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// Finds a child of a specific type in the visual tree
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                // Recursively search children
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }

            return null;
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Event arguments for cell value change notifications
    /// </summary>
    public class CellValueChangedEventArgs : EventArgs
    {
        public object Item { get; }
        public DataGridColumn Column { get; }
        public string BindingPath { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public int RowIndex { get; }
        public int ColumnIndex { get; }

        public CellValueChangedEventArgs(object item, DataGridColumn column, string bindingPath,
            object oldValue, object newValue, int rowIndex, int columnIndex)
        {
            Item = item;
            Column = column;
            BindingPath = bindingPath;
            OldValue = oldValue;
            NewValue = newValue;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}