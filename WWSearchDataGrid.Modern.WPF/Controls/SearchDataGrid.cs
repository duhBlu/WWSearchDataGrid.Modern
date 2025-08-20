using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using WWSearchDataGrid.Modern.Core;
using System.Linq.Expressions;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.WPF
{

    /// <summary>
    /// Modern implementation of the SearchDataGrid
    /// </summary>
    public class SearchDataGrid : DataGrid
    {
        #region Fields

        private readonly TokenSource tokenSource = new TokenSource();
        private readonly ObservableCollection<ColumnSearchBox> dataColumns = new ObservableCollection<ColumnSearchBox>();
        private System.Collections.IEnumerable originalItemsSource;
        private System.Collections.IEnumerable transformedItemsSource;
        private bool initialUpdateLayoutCompleted;
        private SearchTemplateController globalFilterController;
        private readonly Dictionary<string, IEnumerable<object>> _columnTransformationResults = new Dictionary<string, IEnumerable<object>>();
        private bool _isApplyingTransformation = false;
        private bool _isEditingTransformedData = false;

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
                    globalFilterController = new SearchTemplateController(typeof(SearchTemplate));

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
        /// Gets the dictionary of column property info
        /// </summary>
        internal Dictionary<string, System.Reflection.PropertyInfo> ColumnPropertyInfo { get; } = new Dictionary<string, System.Reflection.PropertyInfo>();
        
        /// <summary>
        /// Gets the original unfiltered items source
        /// </summary>
        public System.Collections.IEnumerable OriginalItemsSource => originalItemsSource;

        /// <summary>
        /// Gets the transformed items source (after data transformations but before traditional filtering)
        /// </summary>
        public System.Collections.IEnumerable TransformedItemsSource => transformedItemsSource ?? originalItemsSource;

        /// <summary>
        /// Gets the active data transformations by column path
        /// </summary>
        public Dictionary<string, DataTransformation> ActiveTransformations
        {
            get
            {
                // Create a dictionary showing which columns have active transformations
                var result = new Dictionary<string, DataTransformation>();
                foreach (var columnPath in _columnTransformationResults.Keys)
                {
                    // Create a dummy transformation to indicate this column has active transformations
                    result[columnPath] = new DataTransformation(DataTransformationType.None, columnPath, null, ColumnDataType.String, "Active Transformation");
                }
                return result;
            }
        }

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
                if (originalItemsSource is System.Collections.ICollection collection) return collection.Count;
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
        /// Handles the beginning of edit operations to track if we're editing transformed data
        /// </summary>
        private void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                _isEditingTransformedData = HasActiveTransformations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnBeginningEdit: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles row edit ending to sync changes back to original data if needed
        /// </summary>
        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            try
            {
                if (_isEditingTransformedData && e.EditAction == DataGridEditAction.Commit)
                {
                    // The change will be automatically reflected in the original data
                    // since both collections contain references to the same objects
                    // No additional synchronization needed for reference types
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnRowEditEnding: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles cell edit ending to sync changes back to original data if needed
        /// </summary>
        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (_isEditingTransformedData && e.EditAction == DataGridEditAction.Commit)
                {
                    // For reference types, changes are automatically synchronized
                    // For value types or complex scenarios, additional synchronization might be needed
                    // This can be extended based on specific data model requirements
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnCellEditEnding: {ex.Message}");
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
                        null,
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
        /// When items source changes, notify controls
        /// </summary>
        protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            // Only store the original items source if we're not currently applying a transformation
            // This prevents overwriting the original data when we set ItemsSource to transformed data
            if (!_isApplyingTransformation)
            {
                originalItemsSource = newValue;
                
                // Clear any existing transformations when new data is loaded
                if (_columnTransformationResults.Count > 0)
                {
                    _columnTransformationResults.Clear();
                }
            }

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
                // If items source is null, set ActualHasItems to false
                ActualHasItems = false;
            }
        }

        private void RegisterCollectionChangedEvent(System.Collections.IEnumerable collection)
        {
            if (collection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void UnregisterCollectionChangedEvent(System.Collections.IEnumerable collection)
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
                if (originalItemsSource is System.Collections.ICollection collection)
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
        /// Apply filters to the items source
        /// </summary>
        /// <param name="delay">Optional delay before filtering</param>
        public async void FilterItemsSource(int delay = 0)
        {
            try
            {
                // Create token source for cancellation
                var cts = tokenSource.GetNewCancellationTokenSource();

                // Wait for delay if requested
                if (delay > 0)
                {
                    await System.Threading.Tasks.Task.Delay(delay);
                }

                // If cancelled, return
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                // Commit any edits
                CommitEdit(DataGridEditingUnit.Row, true);

                // Step 1: Apply data transformations first
                ApplyDataTransformations();

                // Step 2: Apply traditional filters to transformed data
                // Check if filters are enabled before applying - respects FilterPanel checkbox
                if (FilterPanel?.FiltersEnabled == true)
                {
                    var activeFilters = DataColumns.Where(d => d.SearchTemplateController?.HasCustomExpression == true && !IsTransformationFilter(d)).ToList();
                    
                    if (activeFilters.Count > 0)
                    {
                        Items.Filter = item => EvaluateMultiColumnFilter(item, activeFilters);
                    }
                    else
                    {
                        Items.Filter = null;
                    }
                    SearchFilter = Items.Filter;
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

                // Remove token source
                tokenSource.RemoveCancellationTokenSource(cts);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering items: {ex.Message}");
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

        private static bool EvaluateMultiColumnFilter(object item, System.Collections.Generic.List<ColumnSearchBox> activeFilters)
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
            // Clear per-column filters
            foreach (var control in DataColumns)
            {
                control.ClearFilter();
            }

            // Clear global filter if applicable
            globalFilterController?.ClearAndReset();

            // Clear all data transformations
            ClearAllDataTransformations();

            // Clear the filter
            Items.Filter = null;
            SearchFilter = null;

            // Clear all cache layers for complete memory cleanup
            ColumnValueCache.Instance.ClearAllCaches();

            // Force restoration of original data
            ForceRestoreOriginalData();

            // Notify that items have been filtered
            ItemsSourceFiltered?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Forces restoration of the original data source
        /// </summary>
        public void ForceRestoreOriginalData()
        {
            if (originalItemsSource != null)
            {
                transformedItemsSource = originalItemsSource;
                
                var currentFilter = Items.Filter;
                Items.Filter = null;
                
                _isApplyingTransformation = true;
                ItemsSource = originalItemsSource;
                _isApplyingTransformation = false;
                
                Items.Filter = currentFilter;
                
                System.Diagnostics.Debug.WriteLine($"After restore: ItemsSource count = {Items.Count}");
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
                    
                    // Also restore original data if transformations are active
                    if (HasActiveTransformations())
                    {
                        RestoreOriginalDataQuick();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnFiltersEnabledChanged: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error in OnFilterRemoved: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Error in OnEditFiltersRequested: {ex.Message}");
                MessageBox.Show("Error opening filter editor.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                System.Diagnostics.Debug.WriteLine($"Error in OnClearAllFiltersRequested: {ex.Message}");
            }
        }

        #endregion

        #region Data Transformation Methods

        /// <summary>
        /// Sets a data transformation for a specific column
        /// </summary>
        /// <param name="columnPath">The column property path</param>
        /// <param name="transformation">The transformation to apply</param>
        public void SetDataTransformation(string columnPath, DataTransformation transformation)
        {
            if (string.IsNullOrEmpty(columnPath))
                return;

            if (transformation == null || transformation.Type == DataTransformationType.None)
            {
                ClearDataTransformation(columnPath);
                return;
            }

            // Validate the transformation
            if (!transformation.IsValid())
            {
                MessageBox.Show($"Invalid transformation: {transformation.GetDescription()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Set the column path if not already set
            if (string.IsNullOrEmpty(transformation.ColumnPath))
                transformation.ColumnPath = columnPath;

            // Apply the transformation directly
            var objectData = originalItemsSource.Cast<object>();
            var result = DataTransformationEngine.ApplyTransformation(objectData, transformation);
            _columnTransformationResults[columnPath] = result;

            // Apply the transformations and filtering
            FilterItemsSource();
        }

        /// <summary>
        /// Clears the data transformation for a specific column
        /// </summary>
        /// <param name="columnPath">The column property path</param>
        public void ClearDataTransformation(string columnPath)
        {
            if (string.IsNullOrEmpty(columnPath))
                return;

            if (_columnTransformationResults.ContainsKey(columnPath))
            {
                System.Diagnostics.Debug.WriteLine($"ClearDataTransformation: Clearing transformation for column {columnPath}");
                _columnTransformationResults.Remove(columnPath);
                
                // If no transformations remain, restore original data
                if (_columnTransformationResults.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No transformations remain - restoring original data");
                    RestoreOriginalData();
                }
                else
                {
                    // Re-apply remaining transformations
                    FilterItemsSource();
                }
            }
        }

        /// <summary>
        /// Clears all data transformations
        /// </summary>
        public void ClearAllDataTransformations()
        {
            if (_columnTransformationResults.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"ClearAllDataTransformations: Clearing {_columnTransformationResults.Count} transformations");
                _columnTransformationResults.Clear();
                
                // Immediately restore original data
                RestoreOriginalData();
            }
        }

        /// <summary>
        /// Gets the active data transformation for a column
        /// </summary>
        /// <param name="columnPath">The column property path</param>
        /// <returns>The active transformation or null if none</returns>
        public DataTransformation GetDataTransformation(string columnPath)
        {
            if (string.IsNullOrEmpty(columnPath))
                return null;

            // Return a dummy transformation if this column has active transformations
            if (_columnTransformationResults.ContainsKey(columnPath))
            {
                return new DataTransformation(DataTransformationType.None, columnPath, null, ColumnDataType.String, "Active Transformation");
            }

            return null;
        }

        /// <summary>
        /// Determines if there are any active data transformations
        /// </summary>
        /// <returns>True if any transformations are active</returns>
        public bool HasActiveTransformations()
        {
            return _columnTransformationResults.Count > 0;
        }

        /// <summary>
        /// Applies all active data transformations to create the transformed ItemsSource
        /// </summary>
        private void ApplyDataTransformations()
        {
            if (originalItemsSource == null)
                return;

            try
            {
                if (_columnTransformationResults.Count == 0)
                {
                    // No transformations - restore original data
                    if (transformedItemsSource != originalItemsSource)
                    {
                        transformedItemsSource = originalItemsSource;
                        
                        // Restore the original ItemsSource
                        var savedFilter = Items.Filter;
                        Items.Filter = null;
                        
                        _isApplyingTransformation = true;
                        ItemsSource = originalItemsSource;
                        _isApplyingTransformation = false;
                        
                        Items.Filter = savedFilter;
                    }
                    return;
                }

                // Combine all transformation results across columns
                IEnumerable<object> finalResult = null;
                
                if (_columnTransformationResults.Count == 1)
                {
                    // Single column transformation
                    finalResult = _columnTransformationResults.Values.First();
                }
                else
                {
                    // Multiple column transformations - intersect them (AND logic between columns)
                    finalResult = _columnTransformationResults.Values.First();
                    foreach (var result in _columnTransformationResults.Values.Skip(1))
                    {
                        finalResult = finalResult.Intersect(result);
                    }
                }

                // Convert to editable collection to support DataGrid editing
                if (finalResult is System.Collections.IList editableResult)
                {
                    transformedItemsSource = editableResult;
                }
                else
                {
                    // Only convert if necessary, use List instead of ObservableCollection for better performance
                    transformedItemsSource = finalResult.ToList();
                }

                // Update the ItemsSource to use transformed data
                var currentFilter = Items.Filter;
                Items.Filter = null;
                
                _isApplyingTransformation = true;
                ItemsSource = transformedItemsSource;
                _isApplyingTransformation = false;
                
                Items.Filter = currentFilter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying data transformations: {ex.Message}");
                MessageBox.Show($"Error applying data transformations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fall back to original data
                RestoreOriginalData();
            }
        }

        /// <summary>
        /// Restores the original data when transformations are cleared or error occurs
        /// </summary>
        private void RestoreOriginalData()
        {
            try
            {
                transformedItemsSource = originalItemsSource;
                
                var currentFilter = Items.Filter;
                Items.Filter = null;
                
                _isApplyingTransformation = true;
                ItemsSource = originalItemsSource;
                _isApplyingTransformation = false;
                
                Items.Filter = currentFilter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring original data: {ex.Message}");
            }
        }

        /// <summary>
        /// Fast restore without expensive conversions - temporarily bypasses transformations without clearing them
        /// </summary>
        private void RestoreOriginalDataQuick()
        {
            try
            {
                if (originalItemsSource == null) return;
                
                // DON'T clear transformation results - just temporarily bypass them
                // This preserves transformation definitions for when filters are re-enabled
                
                // Direct assignment - no conversions
                transformedItemsSource = originalItemsSource;
                
                var currentFilter = Items.Filter;
                Items.Filter = null;
                
                _isApplyingTransformation = true;
                ItemsSource = originalItemsSource;  // Direct reference
                _isApplyingTransformation = false;
                
                Items.Filter = currentFilter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in fast restore: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if a ColumnSearchBox represents a transformation filter
        /// </summary>
        /// <param name="ColumnSearchBox">The search control to check</param>
        /// <returns>True if it's a transformation filter</returns>
        private static bool IsTransformationFilter(ColumnSearchBox columnSearchBox)
        {
            if (columnSearchBox?.SearchTemplateController?.SearchGroups == null)
                return false;

            // Check if any search template in any group is a transformation type
            return columnSearchBox.SearchTemplateController.SearchGroups
                .SelectMany(g => g.SearchTemplates)
                .Any(t => DataTransformationEngine.IsTransformationType(t.SearchType));
        }

        /// <summary>
        /// Processes a ColumnSearchBox to extract and apply any data transformations
        /// </summary>
        /// <param name="columnSearchBox">The search control to process</param>
        public void ProcessTransformationFilter(ColumnSearchBox columnSearchBox)
        {
            if (columnSearchBox?.SearchTemplateController?.SearchGroups == null || string.IsNullOrEmpty(columnSearchBox.BindingPath))
                return;

            var columnPath = columnSearchBox.BindingPath;
            var hasAnyTransformations = false;
            
            // Process all search groups and build a combined transformation
            var transformationResults = new List<IEnumerable<object>>();

            foreach (var group in columnSearchBox.SearchTemplateController.SearchGroups)
            {
                var groupTransformations = new List<DataTransformation>();
                
                // Collect all transformations in this group
                foreach (var template in group.SearchTemplates)
                {
                    if (DataTransformationEngine.IsTransformationType(template.SearchType) && template.HasCustomFilter)
                    {
                        var transformationType = DataTransformationEngine.SearchTypeToTransformationType(template.SearchType);
                        var transformation = new DataTransformation(
                            transformationType,
                            columnPath,
                            template.SelectedValue,
                            columnSearchBox.SearchTemplateController.ColumnDataType,
                            columnSearchBox.CurrentColumn?.Header?.ToString()
                        );

                        if (transformation.IsValid())
                        {
                            groupTransformations.Add(transformation);
                            hasAnyTransformations = true;
                        }
                    }
                }

                // Apply transformations within this group (OR logic for templates within a group)
                if (groupTransformations.Count > 0)
                {
                    var groupResult = ApplyGroupTransformations(originalItemsSource, groupTransformations, group);
                    if (groupResult != null)
                        transformationResults.Add(groupResult);
                }
            }

            if (hasAnyTransformations && transformationResults.Count > 0)
            {
                // Combine results from all groups (AND logic between groups)
                var combinedResult = CombineTransformationResults(transformationResults, 
                    columnSearchBox.SearchTemplateController.SearchGroups.Count > 1);
                
                // Store the combined transformation result for this column
                SetColumnTransformationResult(columnPath, combinedResult);
            }
            else
            {
                // No transformations found - clear any existing transformation for this column
                ClearDataTransformation(columnPath);
            }
        }

        /// <summary>
        /// Applies transformations within a single search group (OR logic)
        /// </summary>
        private static IEnumerable<object> ApplyGroupTransformations(System.Collections.IEnumerable originalData, 
            List<DataTransformation> transformations, SearchTemplateGroup group)
        {
            if (transformations.Count == 0)
                return null;

            // Convert IEnumerable to IEnumerable<object> for DataTransformationEngine
            var objectData = originalData.Cast<object>();

            if (transformations.Count == 1)
            {
                // Single transformation
                return DataTransformationEngine.ApplyTransformation(objectData, transformations[0]);
            }

            // Multiple transformations - combine with OR logic
            var allResults = new HashSet<object>();
            
            foreach (var transformation in transformations)
            {
                var result = DataTransformationEngine.ApplyTransformation(objectData, transformation);
                foreach (var item in result)
                {
                    allResults.Add(item);
                }
            }

            return allResults;
        }

        /// <summary>
        /// Combines transformation results from multiple groups
        /// </summary>
        private static IEnumerable<object> CombineTransformationResults(List<IEnumerable<object>> results, bool useAndLogic)
        {
            if (results.Count == 0)
                return Enumerable.Empty<object>();

            if (results.Count == 1)
                return results[0];

            if (useAndLogic)
            {
                // AND logic between groups - intersection
                var result = results[0];
                for (int i = 1; i < results.Count; i++)
                {
                    result = result.Intersect(results[i]);
                }
                return result;
            }
            else
            {
                // OR logic - union
                var allResults = new HashSet<object>();
                foreach (var result in results)
                {
                    foreach (var item in result)
                    {
                        allResults.Add(item);
                    }
                }
                return allResults;
            }
        }

        /// <summary>
        /// Sets the transformation result for a specific column
        /// </summary>
        private void SetColumnTransformationResult(string columnPath, IEnumerable<object> transformedData)
        {
            // Store the actual transformed data
            _columnTransformationResults[columnPath] = transformedData;

            // Trigger the filtering update
            FilterItemsSource();
        }

        #endregion

        #endregion
    }
}