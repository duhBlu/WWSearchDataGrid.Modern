using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.Core.Common.Utilities;
using WWSearchDataGrid.Modern.Core.Performance;
using WWSearchDataGrid.Modern.WPF.Services;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Details filter control with tabbed interface
    /// </summary>
    public class ColumnFilterEditor : Control, INotifyPropertyChanged
    {
        #region Fields

        private readonly Button applyButton;
        private readonly Button clearButton;
        private readonly Button closeButton;
        private TabControl tabControl;
        private TextBox valueSearchBox;
        private TextBlock valuesSummary;
        private ColumnDataType columnDataType;
        private ComboBox operatorComboBox;

        private readonly ColumnValueCache _cache = ColumnValueCache.Instance;
        private readonly IFilterApplicationService _filterApplicationService;
        private DispatcherTimer _tabSwitchTimer;
        private TabItem _pendingTab;
        private bool _isInitialized;
        private string _columnKey;
        private CancellationTokenSource _loadingCancellationTokenSource;
        
        // Timer for debouncing operator visibility updates
        private DispatcherTimer _operatorVisibilityUpdateTimer;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty FilterValueViewModelProperty =
            DependencyProperty.Register(nameof(FilterValueViewModel),
                typeof(FilterValueViewModel),
                typeof(ColumnFilterEditor),
                new PropertyMetadata(null));

        /// <summary>
        /// Attached property for specifying the GroupBy column for filter values
        /// </summary>
        public static readonly DependencyProperty GroupByColumnProperty =
            DependencyProperty.RegisterAttached("GroupByColumn", typeof(string), typeof(ColumnFilterEditor),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Attached property for specifying the default search type for filter templates
        /// </summary>
        public static readonly DependencyProperty DefaultSearchTypeProperty =
            DependencyProperty.RegisterAttached("DefaultSearchType", typeof(SearchType), typeof(ColumnFilterEditor),
                new FrameworkPropertyMetadata(SearchType.Contains, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Dependency property for controlling operator ComboBox visibility
        /// </summary>
        public static readonly DependencyProperty IsOperatorVisibleProperty =
            DependencyProperty.Register(nameof(IsOperatorVisible), typeof(bool), typeof(ColumnFilterEditor),
                new PropertyMetadata(false));

        private static readonly DependencyPropertyKey ValueSelectionSummaryPropertyKey =
            DependencyProperty.RegisterReadOnly("ValueSelectionSummary", typeof(string), typeof(ColumnFilterEditor),
                new PropertyMetadata("No values selected"));

        public static readonly DependencyProperty ValueSelectionSummaryProperty = ValueSelectionSummaryPropertyKey.DependencyProperty;

        #endregion Dependency Properties

        #region Properties

        /// <summary>
        /// Gets or sets the search controller for this control
        /// </summary>
        public SearchTemplateController SearchTemplateController { get; set; }

        /// <summary>
        /// Gets or sets the column data type
        /// </summary>
        public ColumnDataType ColumnDataType
        {
            get => columnDataType;
            set
            {
                columnDataType = value;
                // Don't call UpdateFilterValueViewModelFromCache here as it's handled in LoadColumnValuesAsync
            }
        }

        /// <summary>
        /// Gets or sets the filter value configuration
        /// </summary>
        public FilterValueConfiguration FilterValueConfiguration { get; set; }

        /// <summary>
        /// Gets the filter value view model
        /// </summary>
        public FilterValueViewModel FilterValueViewModel
        {
            get => (FilterValueViewModel)GetValue(FilterValueViewModelProperty);
            set => SetValue(FilterValueViewModelProperty, value);
        }

        /// <summary>
        /// Gets the value selection summary
        /// </summary>
        public string ValueSelectionSummary => FilterValueViewModel?.GetSelectionSummary() ?? "No values selected";

        /// <summary>
        /// Gets or sets whether the operator ComboBox is visible
        /// </summary>
        public bool IsOperatorVisible
        {
            get => (bool)GetValue(IsOperatorVisibleProperty);
            set => SetValue(IsOperatorVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the operator name for the first search group (safe access)
        /// </summary>
        public string GroupOperatorName
        {
            get
            {
                if (SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    return SearchTemplateController.SearchGroups[0].OperatorName ?? "And";
                }
                return "And";
            }
            set
            {
                if (SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    var currentValue = SearchTemplateController.SearchGroups[0].OperatorName;
                    if (currentValue != value)
                    {
                        SearchTemplateController.SearchGroups[0].OperatorName = value;
                        
                        // Force update the filter expression to ensure HasCustomExpression is recalculated
                        SearchTemplateController.UpdateFilterExpression();
                        
                        OnPropertyChanged(nameof(GroupOperatorName));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of available data columns from the parent SearchDataGrid
        /// </summary>
        public IEnumerable<ColumnSearchBox> DataColumns
        {
            get
            {
                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    return columnSearchBox.SourceDataGrid.DataColumns;
                }
                else if (DataContext is SearchDataGrid dataGrid)
                {
                    return dataGrid.DataColumns;
                }
                return Enumerable.Empty<ColumnSearchBox>();
            }
        }

        /// <summary>
        /// Determines if there are active filters in columns that precede the current column
        /// </summary>
        private bool HasPrecedingColumnFilters()
        {
            if (!(DataContext is ColumnSearchBox currentColumnSearchBox) || currentColumnSearchBox.CurrentColumn == null)
            {
                return false;
            }

            try
            {
                var currentDisplayIndex = currentColumnSearchBox.CurrentColumn.DisplayIndex;
                var dataColumns = DataColumns.ToList();

                var precedingColumnsWithFilters = dataColumns
                    .Where(col => col.CurrentColumn != null && 
                                  col.CurrentColumn.DisplayIndex < currentDisplayIndex &&
                                  col.HasActiveFilter)
                    .ToList();

                return precedingColumnsWithFilters.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking preceding column filters: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Attached Property Methods

        /// <summary>
        /// Gets the GroupBy column for an element
        /// </summary>
        public static string GetGroupByColumn(DependencyObject element) =>
            (string)element.GetValue(GroupByColumnProperty);

        /// <summary>
        /// Sets the GroupBy column for an element
        /// </summary>
        public static void SetGroupByColumn(DependencyObject element, string value) =>
            element.SetValue(GroupByColumnProperty, value);

        /// <summary>
        /// Gets the default search type for an element
        /// </summary>
        public static SearchType GetDefaultSearchType(DependencyObject element) =>
            (SearchType)element.GetValue(DefaultSearchTypeProperty);

        /// <summary>
        /// Sets the default search type for an element
        /// </summary>
        public static void SetDefaultSearchType(DependencyObject element, SearchType value) =>
            element.SetValue(DefaultSearchTypeProperty, value);

        #endregion

        #region Commands

        /// <summary>
        /// Add search template command
        /// </summary>
        public ICommand AddSearchTemplateCommand => new RelayCommand(p =>
        {
            // Get the default search type from the column (if any)
            SearchType? defaultSearchType = null;
            if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.CurrentColumn != null)
            {
                defaultSearchType = GetDefaultSearchType(columnSearchBox.CurrentColumn);
            }

            // Simply call AddSearchTemplate without specifying group - it will use the default group for the column
            SearchTemplateController.AddSearchTemplate(true, null, null);

            UpdateOperatorVisibility();
        });

        /// <summary>
        /// Remove search template command
        /// </summary>
        public ICommand RemoveSearchTemplateCommand => new RelayCommand(p =>
        {
            SearchTemplate template = p as SearchTemplate;
            SearchTemplateController?.RemoveSearchTemplate(template);
            UpdateOperatorVisibility();
        });

        /// <summary>
        /// Apply filter command
        /// </summary>
        public ICommand ApplyFilterCommand => new RelayCommand(_ => ApplyFilter());

        /// <summary>
        /// Clear filter command
        /// </summary>
        public ICommand ClearFilterCommand => new RelayCommand(_ => ClearFilter());

        /// <summary>
        /// Close window command
        /// </summary>
        public ICommand CloseWindowCommand => new RelayCommand(_ => CloseWindow());

        /// <summary>
        /// Select all values command
        /// </summary>
        public ICommand SelectAllValuesCommand => new RelayCommand(_ =>
        {
            FilterValueViewModel?.SelectAll();
            UpdateValueSelectionSummary();
        });

        /// <summary>
        /// Clear all values command
        /// </summary>
        public ICommand ClearAllValuesCommand => new RelayCommand(_ =>
        {
            FilterValueViewModel?.ClearAll();
            UpdateValueSelectionSummary();
        });

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ColumnFilterEditor class
        /// </summary>
        public ColumnFilterEditor()
        {
            _filterApplicationService = new FilterApplicationService();
            Loaded += OnControlLoaded;
            Unloaded += OnUnloaded;
        }
        
        /// <summary>
        /// Initializes a new instance with custom filter application service for testing
        /// </summary>
        /// <param name="filterApplicationService">Filter application service</param>
        internal ColumnFilterEditor(IFilterApplicationService filterApplicationService)
        {
            _filterApplicationService = filterApplicationService ?? throw new ArgumentNullException(nameof(filterApplicationService));
            Loaded += OnControlLoaded;
            Unloaded += OnUnloaded;
        }

        #endregion

        #region Methods

        /// <summary>
        /// When the control template is applied, find and hook up the child controls
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // The ListBox is now conditionally created based on AllowMultipleGroups
            // We'll set it up when needed in SetupListBoxReference method
            SetupListBoxReference();

            // Find operator combo box
            operatorComboBox = GetTemplateChild("PART_OperatorComboBox") as ComboBox;

            // Find tab control
            tabControl = GetTemplateChild("PART_TabControl") as TabControl;

            // Find value search box
            valueSearchBox = GetTemplateChild("PART_ValueSearchBox") as TextBox;
            if (valueSearchBox != null)
            {
                valueSearchBox.TextChanged += OnValueSearchBoxTextChanged;
            }

            // Find values summary
            valuesSummary = GetTemplateChild("PART_ValuesSummary") as TextBlock;

            // Initial operator visibility update
            UpdateOperatorVisibility();
        }

        /// <summary>
        /// When the control is loaded, setup bindings
        /// </summary>
        private async void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            // Initialize tab switch timer for deferred loading
            _tabSwitchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150),
                IsEnabled = false
            };
            _tabSwitchTimer.Tick += OnTabSwitchTimerTick;

            InitializeSearchTemplateController();
            DetermineColumnDataType();

            // Generate unique column key for caching
            _columnKey = GenerateColumnKey();

            // Load column values and set up FilterValueViewModel
            await LoadColumnValuesAsync();

            // Update the summary after everything is loaded
            UpdateValueSelectionSummary();

            // Setup ListBox reference and bindings if needed
            SetupListBoxReference();

            // Hook up tab control selection changed for lazy loading
            if (tabControl != null)
            {
                tabControl.SelectionChanged += OnTabControlSelectionChanged;
            }

            if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
            {
                columnSearchBox.SourceDataGrid.CollectionChanged += OnSourceDataGridCollectionChanged;
                columnSearchBox.SourceDataGrid.ItemsSourceChanged += OnItemsSourceChanged;
            }

            // Hook up AllowMultipleGroups property change notification
            if (SearchTemplateController != null)
            {
                SearchTemplateController.PropertyChanged += OnSearchTemplateControllerPropertyChanged;
            }

            // Set up filter change monitoring and initial operator visibility  
            SetupFilterChangeMonitoring();
            UpdateOperatorVisibility();
            
            // Ensure template operators are visible after everything is loaded
            UpdateTemplateOperatorVisibility();
        }

        /// <summary>
        /// Handles property changes on SearchTemplateController to respond to AllowMultipleGroups changes
        /// </summary>
        private void OnSearchTemplateControllerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchTemplateController.AllowMultipleGroups))
            {
                // Re-setup ListBox reference when AllowMultipleGroups changes
                SetupListBoxReference();
            }
        }

        /// <summary>
        /// Sets up the ListBox reference dynamically based on AllowMultipleGroups
        /// </summary>
        private void SetupListBoxReference()
        {
            // Only try to find and setup ListBox if AllowMultipleGroups is true
            if (SearchTemplateController?.AllowMultipleGroups == true)
            {
                // The ListBox is created dynamically in the DataTemplate, so we need to wait for it
                Dispatcher.BeginInvoke(new Action(() =>
                {
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Recursively searches for a child element by name in the visual tree
        /// </summary>
        private static DependencyObject FindChildByName(DependencyObject parent, string name)
        {
            if (parent == null) return null;

            // Check if this element has the target name
            if (parent is FrameworkElement fe && fe.Name == name)
                return parent;

            // Search through all children
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindChildByName(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Generates a unique key for the column
        /// </summary>
        private string GenerateColumnKey()
        {
            if (DataContext is ColumnSearchBox columnSearchBox)
            {
                return $"{columnSearchBox.CurrentColumn?.Header}_{columnSearchBox.BindingPath}";
            }
            else if (DataContext is SearchDataGrid)
            {
                return "GlobalFilter";
            }

            return "Unknown";
        }

        /// <summary>
        /// Loads column values asynchronously using the high-performance provider
        /// </summary>
        private async Task LoadColumnValuesAsync()
        {
            if (SearchTemplateController == null || string.IsNullOrEmpty(_columnKey))
                return;

            // Cancel any existing loading operation
            _loadingCancellationTokenSource?.Cancel();
            _loadingCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _loadingCancellationTokenSource.Token;

            try
            {
                // Check if we're in per-column or global mode
                if (DataContext is ColumnSearchBox columnSearchBox)
                {
                    await LoadColumnValuesForSingleColumnAsync(columnSearchBox, cancellationToken);
                }
                else if (DataContext is SearchDataGrid dataGrid)
                {
                    await LoadColumnValuesForGlobalModeAsync(dataGrid, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Loading was cancelled, which is expected
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading column values: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads column values for a single column using high-performance provider
        /// </summary>
        private async Task LoadColumnValuesForSingleColumnAsync(ColumnSearchBox columnSearchBox, CancellationToken cancellationToken)
        {
            var bindingPath = columnSearchBox.BindingPath;
            if (string.IsNullOrEmpty(bindingPath) || columnSearchBox.SourceDataGrid?.OriginalItemsSource == null)
                return;

            // Show progress indicator for large datasets
            var itemsSource = columnSearchBox.SourceDataGrid.OriginalItemsSource;
            var itemCount = itemsSource.Cast<object>().Count();
            var showProgress = itemCount > 5000;

            if (showProgress)
            {
                await ShowProgressIndicatorAsync("Loading column values...", cancellationToken);
            }

            // Use the original unfiltered items source to preserve all possible values
            var items = itemsSource.Cast<object>().ToList();
            
            // Update cache with background processing
            await _cache.UpdateColumnValuesAsync(_columnKey, items, bindingPath);
            
            // Update high-performance provider in the background
            var highPerformanceProvider = _cache.HighPerformanceProvider;
            await highPerformanceProvider.UpdateColumnValuesAsync(_columnKey, items, bindingPath);

            cancellationToken.ThrowIfCancellationRequested();

            // Update controller with cached values on UI thread
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var metadata = _cache.GetOrCreateMetadata(_columnKey, bindingPath);
                SearchTemplateController.ColumnValues = new HashSet<object>(metadata.Values);
                SearchTemplateController.ColumnDataType = metadata.DataType;

                // Connect SearchTemplateController to high-performance value provider
                SearchTemplateController.ConnectToValueProvider(_columnKey, _cache.HighPerformanceProvider);
                
                // Initialize existing templates with provider (fire and forget)
                _ = InitializeSearchTemplatesAsync();
            }));

            cancellationToken.ThrowIfCancellationRequested();

            // Create and set up the FilterValueViewModel
            await SetupFilterValueViewModelAsync(columnSearchBox, items, cancellationToken);

            if (showProgress)
            {
                await HideProgressIndicatorAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Loads column values for global mode using high-performance provider
        /// </summary>
        private async Task LoadColumnValuesForGlobalModeAsync(SearchDataGrid dataGrid, CancellationToken cancellationToken)
        {
            var columns = dataGrid.DataColumns.Where(c => !string.IsNullOrEmpty(c.BindingPath)).ToList();
            
            if (columns.Count == 0)
                return;

            // Show progress for global mode
            await ShowProgressIndicatorAsync($"Loading values for {columns.Count} columns...", cancellationToken);

            var highPerformanceProvider = _cache.HighPerformanceProvider;
            var tasks = new List<Task>();

            foreach (var column in columns)
            {
                var columnKey = $"{column.CurrentColumn?.Header}_{column.BindingPath}";
                var bindingPath = column.BindingPath;
                var items = dataGrid.Items.Cast<object>().ToList();

                // Update both cache and high-performance provider
                tasks.Add(_cache.UpdateColumnValuesAsync(columnKey, items, bindingPath));
                tasks.Add(highPerformanceProvider.UpdateColumnValuesAsync(columnKey, items, bindingPath));
            }

            await Task.WhenAll(tasks);

            cancellationToken.ThrowIfCancellationRequested();
            
            await HideProgressIndicatorAsync(cancellationToken);
        }

        /// <summary>
        /// Shows progress indicator for large dataset loading
        /// </summary>
        private async Task ShowProgressIndicatorAsync(string message, CancellationToken cancellationToken)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                if (valuesSummary != null)
                {
                    valuesSummary.Text = message;
                    valuesSummary.Visibility = Visibility.Visible;
                }
            }));
        }

        /// <summary>
        /// Hides progress indicator
        /// </summary>
        private async Task HideProgressIndicatorAsync(CancellationToken cancellationToken)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                if (valuesSummary != null)
                {
                    valuesSummary.Visibility = Visibility.Collapsed;
                }
            }));
        }

        /// <summary>
        /// Sets up the FilterValueViewModel with proper data using high-performance provider
        /// </summary>
        private async Task SetupFilterValueViewModelAsync(ColumnSearchBox columnSearchBox, List<object> items, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get values from high-performance provider
            var highPerformanceProvider = _cache.HighPerformanceProvider;
            var request = new ColumnValueRequest
            {
                ColumnKey = _columnKey,
                Skip = 0,
                Take = 10000, // Load first 10k for UI
                IncludeNull = true,
                IncludeEmpty = true,
                SortAscending = true,
                GroupByFrequency = false
            };

            var response = await highPerformanceProvider.GetValuesAsync(request);
            cancellationToken.ThrowIfCancellationRequested();

            // Convert to required formats
            var values = new HashSet<object>(response.Values.Select(v => v.Value));
            var valueCounts = response.Values.ToDictionary(v => v.Value, v => v.Count);

            await Dispatcher.BeginInvoke(new Action(() =>
            {
                // Check if GroupBy column is specified
                var groupByColumn = GetGroupByColumn(columnSearchBox.CurrentColumn);

                if (!string.IsNullOrEmpty(groupByColumn))
                {
                    // Create grouped tree view model
                    var groupedViewModel = new GroupedTreeViewFilterValueViewModel
                    {
                        GroupByColumn = groupByColumn
                    };

                    // Load grouped data
                    LoadGroupedDataForViewModel(groupedViewModel, columnSearchBox, items, groupByColumn);

                    FilterValueViewModel = groupedViewModel;

                    // Load the data with counts from high-performance provider
                    if (values.Count > 0)
                    {
                        FilterValueViewModel.LoadValuesWithCounts(values, valueCounts);
                    }
                }
                else
                {
                    // Use regular filter value view model from cache
                    FilterValueViewModel = _cache.GetOrCreateFilterViewModel(
                        _columnKey,
                        ColumnDataType,
                        FilterValueConfiguration);

                    // Load the data with counts from high-performance provider
                    if (values.Count > 0)
                    {
                        FilterValueViewModel.LoadValuesWithCounts(values, valueCounts);
                    }
                }
            }));
        }

        /// <summary>
        /// Loads grouped data for the filter values view model
        /// </summary>
        private static void LoadGroupedDataForViewModel(GroupedTreeViewFilterValueViewModel groupedViewModel,
            ColumnSearchBox columnSearchBox, List<object> items, string groupByColumn)
        {
            // Create grouped data based on the GroupBy column
            var groupedData = new Dictionary<object, List<object>>();
            var displayNames = new Dictionary<string, string>();

            foreach (var item in items)
            {
                try
                {
                    // Get the group key from the GroupBy column
                    var groupKey = ReflectionHelper.GetPropValue(item, groupByColumn);

                    // Get the actual value from the current column (the column being filtered)
                    var value = ReflectionHelper.GetPropValue(item, columnSearchBox.BindingPath);

                    // Group the current column's values by the GroupBy column's values
                    if (!groupedData.TryGetValue(groupKey, out var list))
                    {
                        list = new List<object>();
                        groupedData[groupKey] = list;
                    }

                    // Only add unique values to each group
                    if (!list.Contains(value))
                    {
                        list.Add(value);
                    }


                    // Store display name for the group key
                    var displayName = groupKey?.ToString() ?? "(No Value)";
                    var keyStr = groupKey?.ToString() ?? "null";
                    if (!displayNames.ContainsKey(keyStr))
                    {
                        displayNames[keyStr] = displayName;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing grouped data: {ex.Message}");
                }
            }

            // Set the grouped data in the view model
            groupedViewModel.SetGroupedData(groupByColumn, groupedData, displayNames);
        }

        /// <summary>
        /// Initialize the search template controller
        /// </summary>
        private void InitializeSearchTemplateController()
        {
            InitializeSearchTemplateControllerCore();
        }
        
        /// <summary>
        /// Initializes existing search templates with the provider
        /// </summary>
        private async Task InitializeSearchTemplatesAsync()
        {
            if (SearchTemplateController == null || string.IsNullOrEmpty(_columnKey))
                return;

            var provider = _cache.HighPerformanceProvider;
            
            foreach (var group in SearchTemplateController.SearchGroups)
            {
                foreach (var template in group.SearchTemplates)
                {
                    await template.LoadValuesFromProvider(provider, _columnKey);
                }
            }
        }

        /// <summary>
        /// Core initialization logic for search template controller
        /// </summary>
        private void InitializeSearchTemplateControllerCore()
        {
            if (SearchTemplateController == null)
            {
                // Get the mode from the source SearchDataGrid
                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    SearchTemplateController = columnSearchBox.SearchTemplateController;
                }
                else if (DataContext is SearchDataGrid dataGrid)
                {
                    // For global mode, create a new controller if one doesn't exist
                    if (SearchTemplateController == null)
                    {
                        SearchTemplateController = new SearchTemplateController(typeof(SearchTemplate));

                        // Initialize with first column if available
                        var firstColumn = dataGrid.DataColumns.FirstOrDefault();
                        if (firstColumn != null)
                        {
                            SearchTemplateController.ColumnName = firstColumn.CurrentColumn.Header;
                            SearchTemplateController.ColumnValues = new HashSet<object>(firstColumn.SearchTemplateController.ColumnValues);
                        }
                    }
                }
            }

            // Update existing templates to SmartSearchTemplate
            if (SearchTemplateController != null)
            {
                SearchTemplateController.SearchTemplateType = typeof(SearchTemplate);

                if (SearchTemplateController.SearchGroups.Count == 0)
                {
                    SearchTemplateController.AddSearchGroup(true, false);
                }

                // Convert existing templates
                foreach (var group in SearchTemplateController.SearchGroups)
                {
                    for (int i = 0; i < group.SearchTemplates.Count; i++)
                    {
                        if (!(group.SearchTemplates[i] is SearchTemplate))
                        {
                            var oldTemplate = group.SearchTemplates[i];
                            var newTemplate = new SearchTemplate(ColumnDataType)
                            {
                                SearchType = oldTemplate.SearchType,
                                SelectedValue = oldTemplate.SelectedValue,
                                SelectedSecondaryValue = oldTemplate.SelectedSecondaryValue,
                                OperatorName = oldTemplate.OperatorName,
                                IsOperatorVisible = oldTemplate.IsOperatorVisible
                            };
                            // Connect to provider
                            if (!string.IsNullOrEmpty(_columnKey))
                            {
                                var provider = _cache.HighPerformanceProvider;
                                _ = newTemplate.ConnectToProviderAsync(provider, _columnKey);
                            }
                            group.SearchTemplates[i] = newTemplate;
                        }
                    }
                }

                // Update template operator visibility after initialization
                UpdateTemplateOperatorVisibility();
            }
        }

        /// <summary>
        /// Determines the column data type based on column values
        /// </summary>
        private void DetermineColumnDataType()
        {
            if (SearchTemplateController != null && SearchTemplateController.ColumnValues.Count > 0)
            {
                ColumnDataType = ReflectionHelper.DetermineColumnDataType(SearchTemplateController.ColumnValues);
            }
            else
            {
                ColumnDataType = ColumnDataType.String;
            }
        }

        /// <summary>
        /// Updates the value selection summary
        /// </summary>
        private void UpdateValueSelectionSummary()
        {
            if (valuesSummary != null)
            {
                valuesSummary.Text = FilterValueViewModel?.GetSelectionSummary() ?? "No values selected";
            }
            SetValue(ValueSelectionSummaryPropertyKey, FilterValueViewModel?.GetSelectionSummary() ?? "No values selected");
        }

        /// <summary>
        /// Debounced value search to avoid excessive filtering
        /// </summary>
        private DispatcherTimer _searchDebounceTimer;

        private void OnValueSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_searchDebounceTimer == null)
            {
                _searchDebounceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                _searchDebounceTimer.Tick += (s, args) =>
                {
                    _searchDebounceTimer.Stop();
                    ApplyValueSearch();
                };
            }

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void ApplyValueSearch()
        {
            var searchText = valueSearchBox?.Text;

            if (FilterValueViewModel is FlatListFilterValueViewModel flatList)
            {
                flatList.SearchText = searchText;
            }
            else if (FilterValueViewModel is GroupedTreeViewFilterValueViewModel groupedList)
            {
                groupedList.SearchText = searchText;
            }
            // Add search support for other view model types as needed
        }

        /// <summary>
        /// Handle tab control selection changed with deferred loading
        /// </summary>
        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem newTab)
            {
                // Use timer to defer tab content loading
                _tabSwitchTimer.Stop();
                _pendingTab = newTab;
                _tabSwitchTimer.Start();
            }
        }

        /// <summary>
        /// Handle deferred tab switching
        /// </summary>
        private void OnTabSwitchTimerTick(object sender, EventArgs e)
        {
            _tabSwitchTimer.Stop();

            if (_pendingTab?.Header?.ToString() == "Filter Values" && FilterValueViewModel != null)
            {
                // Ensure filter values are loaded when switching to the values tab
                if (!FilterValueViewModel.IsLoaded)
                {
                    var metadata = _cache.GetOrCreateMetadata(_columnKey, string.Empty);
                    if (metadata.Values.Count > 0)
                    {
                        FilterValueViewModel.LoadValuesWithCounts(metadata.Values, metadata.ValueCounts);
                    }
                }

                UpdateValueSelectionSummary();
            }

            _pendingTab = null;
        }

        private async void OnSourceDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_columnKey) || DataContext is not ColumnSearchBox columnSearchBox)
                return;

            try
            {
                // Try incremental update first
                if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
                {
                    await _cache.UpdateColumnValuesIncrementalAsync(_columnKey, e, columnSearchBox.BindingPath);
                }
                else
                {
                    // Fall back to full reload for complex changes
                    var items = columnSearchBox.SourceDataGrid.OriginalItemsSource?.Cast<object>().ToList();
                    if (items?.Count > 0)
                    {
                        await _cache.UpdateColumnValuesAsync(_columnKey, items, columnSearchBox.BindingPath);
                    }
                }

                RefreshFilterValueViewModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating filter values: {ex.Message}");
            }
        }

        private async void OnItemsSourceChanged(object sender, EventArgs e)
        {
            // Reload all column values when the entire ItemsSource changes
            await LoadColumnValuesAsync();
            RefreshFilterValueViewModel();
        }

        private void RefreshFilterValueViewModel()
        {
            if (FilterValueViewModel != null && DataContext is ColumnSearchBox columnSearchBox)
            {
                var metadata = _cache.GetOrCreateMetadata(_columnKey, columnSearchBox.BindingPath);

                // Reload the view model with updated values
                FilterValueViewModel.LoadValuesWithCounts(metadata.Values, metadata.ValueCounts);

                // Update UI
                UpdateValueSelectionSummary();
            }
        }

        /// <summary>
        /// Updates operator visibility for both group-level and template-level operators
        /// </summary>
        private void UpdateOperatorVisibility()
        {
            try
            {
                var hasPrecedingFilters = HasPrecedingColumnFilters();
                
                // Update the control-level operator visibility
                IsOperatorVisible = hasPrecedingFilters;
                
                // Always ensure we have a SearchTemplateController and at least one group
                if (SearchTemplateController == null)
                    return;

                // Ensure we have at least one group
                if (SearchTemplateController.SearchGroups.Count == 0)
                {
                    SearchTemplateController.AddSearchGroup(true, false);
                }

                // Update template-level operator visibility
                UpdateTemplateOperatorVisibility();
                
                // Notify that the GroupOperatorName might have changed (in case the SearchGroups were just created)
                OnPropertyChanged(nameof(GroupOperatorName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating operator visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up monitoring for filter changes to update operator visibility
        /// </summary>
        private void SetupFilterChangeMonitoring()
        {
            if (!(DataContext is ColumnSearchBox currentColumnSearchBox) || currentColumnSearchBox.SourceDataGrid == null)
                return;

            try
            {
                // Set up a timer for debounced updates (in case multiple filters change quickly)
                _operatorVisibilityUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                _operatorVisibilityUpdateTimer.Tick += (s, e) =>
                {
                    _operatorVisibilityUpdateTimer.Stop();
                    UpdateOperatorVisibility();
                };

                // Subscribe to the SearchDataGrid's filter events if available
                // This is a much simpler approach than trying to monitor individual SearchTemplateControllers
                currentColumnSearchBox.SourceDataGrid.ItemsSourceFiltered += OnFilteringCompleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up filter change monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles completion of filtering operations to update operator visibility
        /// </summary>
        private void OnFilteringCompleted(object sender, EventArgs e)
        {
            // Debounce the update to avoid excessive calls
            _operatorVisibilityUpdateTimer?.Stop();
            _operatorVisibilityUpdateTimer?.Start();
        }

        /// <summary>
        /// Updates template-level operator visibility (for templates within groups)
        /// </summary>
        private void UpdateTemplateOperatorVisibility()
        {
            if (SearchTemplateController != null)
            {
                var groups = SearchTemplateController.SearchGroups;

                // Use a single pass to update all templates
                for (int g = 0; g < groups.Count; g++)
                {
                    var group = groups[g];
                    var templates = group.SearchTemplates;

                    // Template-level operators: show for all templates after the first in each group
                    for (int t = 0; t < templates.Count; t++)
                    {
                        var shouldBeVisible = t > 0;
                        templates[t].IsOperatorVisible = shouldBeVisible;
                    }
                }
            }
        }

        /// <summary>
        /// Apply the filter and close the window using intelligent filter selection
        /// </summary>
        private void ApplyFilter()
        {
            if (SearchTemplateController == null) return;
            
            // Debug logging before applying filter
            if (SearchTemplateController.SearchGroups.Count > 0)
            {
                var firstGroup = SearchTemplateController.SearchGroups[0];
                for (int i = 0; i < firstGroup.SearchTemplates.Count; i++)
                {
                    var template = firstGroup.SearchTemplates[i];
                }
            }
            
            // INTELLIGENT FILTER APPLICATION: Use content-based decision making instead of tab-based
            var selectedTabIndex = tabControl?.SelectedIndex ?? -1;
            
            var result = _filterApplicationService.ApplyIntelligentFilter(
                FilterValueViewModel, SearchTemplateController, ColumnDataType, selectedTabIndex);
            
            if (!result.IsSuccess)
            {
                return;
            }

            // Determine if we're in per-column or global mode
            if (DataContext is ColumnSearchBox columnSearchBox)
            {
                // Per-column mode
                if (columnSearchBox.SourceDataGrid != null)
                {
                    columnSearchBox.HasAdvancedFilter = result.HasCustomExpression;
                    
                    // Handle grouped filtering if needed
                    if (result.FilterType == FilterApplicationType.GroupedValueBased)
                    {
                        columnSearchBox.GroupedFilterCombinations = SearchTemplateController.GroupedFilterCombinations;
                        columnSearchBox.GroupByColumnPath = SearchTemplateController.GroupByColumnPath;
                    }

                    columnSearchBox.SourceDataGrid.FilterItemsSource();
                    CloseWindow();
                }
            }
            else if (DataContext is SearchDataGrid dataGrid)
            {
                // Global mode
                dataGrid.FilterItemsSource();
                CloseWindow();
            }
        }

        /// <summary>
        /// Clear the filter using the filter application service
        /// </summary>
        private void ClearFilter()
        {
            if (SearchTemplateController == null) return;
            
            var result = _filterApplicationService.ClearAllFilters(SearchTemplateController);
            
            if (!result.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Filter clearing failed: {result.ErrorMessage}");
                return;
            }
            
            // Add a default group back
            SearchTemplateController.AddSearchGroup();

            // Determine if we're in per-column or global mode
            if (DataContext is ColumnSearchBox columnSearchBox)
            {
                // Per-column mode
                columnSearchBox.SearchText = string.Empty;
                columnSearchBox.HasAdvancedFilter = false;

                columnSearchBox.SourceDataGrid?.FilterItemsSource();
            }
            else if (DataContext is SearchDataGrid dataGrid)
            {
                // Global mode - clear all filters
                dataGrid.ClearAllFilters();
            }
        }

        /// <summary>
        /// Override to clean up resources
        /// </summary>
        protected void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Cancel any ongoing loading operations
            _loadingCancellationTokenSource?.Cancel();
            _loadingCancellationTokenSource?.Dispose();
            _loadingCancellationTokenSource = null;

            // Clean up timers
            if (_tabSwitchTimer != null)
            {
                _tabSwitchTimer.Stop();
                _tabSwitchTimer.Tick -= OnTabSwitchTimerTick;
                _tabSwitchTimer = null;
            }

            // Unhook events
            if (tabControl != null)
            {
                tabControl.SelectionChanged -= OnTabControlSelectionChanged;
            }

            if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
            {
                columnSearchBox.SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                columnSearchBox.SourceDataGrid.ItemsSourceChanged -= OnItemsSourceChanged;

                // Unsubscribe from filter completion events
                columnSearchBox.SourceDataGrid.ItemsSourceFiltered -= OnFilteringCompleted;
            }

            // Unhook SearchTemplateController property changes
            if (SearchTemplateController != null)
            {
                SearchTemplateController.PropertyChanged -= OnSearchTemplateControllerPropertyChanged;
            }

            // Clean up operator visibility update timer
            if (_operatorVisibilityUpdateTimer != null)
            {
                _operatorVisibilityUpdateTimer.Stop();
                _operatorVisibilityUpdateTimer.Tick -= null;
                _operatorVisibilityUpdateTimer = null;
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Close the window
        /// </summary>
        private void CloseWindow()
        {
            Window.GetWindow(this)?.Close();
        }

        /// <summary>
        /// Raises property changed event
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Event raised when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}