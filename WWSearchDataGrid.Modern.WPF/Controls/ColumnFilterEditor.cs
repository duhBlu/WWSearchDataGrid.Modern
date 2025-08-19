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

        private TabControl tabControl;
        private TextBox valueSearchBox;
        private TextBlock valuesSummary;
        private ColumnDataType columnDataType;

        private readonly ColumnValueCache _cache = ColumnValueCache.Instance;
        private readonly IFilterApplicationService _filterApplicationService;
        private readonly IRuleToValueSynchronizationService _ruleToValueSyncService;
        private DispatcherTimer _tabSwitchTimer;
        private TabItem _pendingTab;
        private bool _isInitialized;
        private string _columnKey;
        private CancellationTokenSource _loadingCancellationTokenSource;
        
        // Timer for debouncing operator visibility updates
        private DispatcherTimer _operatorVisibilityUpdateTimer;
        
        // Synchronization and live filtering
        private bool _isSynchronizing = false;
        private DispatcherTimer _liveFilterTimer;
        private bool _enableLiveFiltering = true;

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
        /// Gets or sets whether live filtering is enabled (filters apply immediately)
        /// </summary>
        public bool EnableLiveFiltering
        {
            get => _enableLiveFiltering;
            set => _enableLiveFiltering = value;
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
            _ruleToValueSyncService = new RuleToValueSynchronizationService();
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
            _ruleToValueSyncService = new RuleToValueSynchronizationService();
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
            
            // Set up synchronization and live filtering
            SetupSynchronization();
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
        /// Loads column values asynchronously using the column value provider
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
        /// Loads column values for a single column using column value provider
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
            
            // Update column value provider in the background
            var columnValueProvider = _cache.ColumnValueProvider;
            await columnValueProvider.UpdateColumnValuesAsync(_columnKey, items, bindingPath);

            cancellationToken.ThrowIfCancellationRequested();

            // Update controller with cached values on UI thread
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var metadata = _cache.GetOrCreateMetadata(_columnKey, bindingPath);
                SearchTemplateController.ColumnValues = new HashSet<object>(metadata.Values);
                SearchTemplateController.ColumnDataType = metadata.DataType;

                // Connect SearchTemplateController to column value value provider
                SearchTemplateController.ConnectToValueProvider(_columnKey, _cache.ColumnValueProvider);
                
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
        /// Loads column values for global mode using column value provider
        /// </summary>
        private async Task LoadColumnValuesForGlobalModeAsync(SearchDataGrid dataGrid, CancellationToken cancellationToken)
        {
            var columns = dataGrid.DataColumns.Where(c => !string.IsNullOrEmpty(c.BindingPath)).ToList();
            
            if (columns.Count == 0)
                return;

            // Show progress for global mode
            await ShowProgressIndicatorAsync($"Loading values for {columns.Count} columns...", cancellationToken);

            var columnValueProvider = _cache.ColumnValueProvider;
            var tasks = new List<Task>();

            foreach (var column in columns)
            {
                var columnKey = $"{column.CurrentColumn?.Header}_{column.BindingPath}";
                var bindingPath = column.BindingPath;
                var items = dataGrid.Items.Cast<object>().ToList();

                // Update both cache and column value provider
                tasks.Add(_cache.UpdateColumnValuesAsync(columnKey, items, bindingPath));
                tasks.Add(columnValueProvider.UpdateColumnValuesAsync(columnKey, items, bindingPath));
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
        /// Sets up the FilterValueViewModel with proper data using column value provider
        /// </summary>
        private async Task SetupFilterValueViewModelAsync(ColumnSearchBox columnSearchBox, List<object> items, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get values from column value provider
            var columnValueProvider = _cache.ColumnValueProvider;
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

            var response = await columnValueProvider.GetValuesAsync(request);
            cancellationToken.ThrowIfCancellationRequested();

            // Use ValueAggregateMetadata directly from response
            var metadataList = response.Values;

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

                    // Load the data with metadata from column value provider
                    if (metadataList.Any())
                    {
                        FilterValueViewModel.LoadValuesWithMetadata(metadataList);
                    }
                }
                else
                {
                    // Use regular filter value view model from cache
                    FilterValueViewModel = _cache.GetOrCreateFilterViewModel(
                        _columnKey,
                        ColumnDataType);

                    // Load the data with metadata from column value provider
                    if (metadataList.Any())
                    {
                        FilterValueViewModel.LoadValuesWithMetadata(metadataList);
                    }
                }

                // Subscribe to selection changed events for synchronization
                if (FilterValueViewModel != null)
                {
                    FilterValueViewModel.SelectionChanged += OnFilterValueSelectionChanged;
                }

                // Ensure synchronization is setup for reopened dialogs
                SetupSynchronization();
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

            var provider = _cache.ColumnValueProvider;
            
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
                                var provider = _cache.ColumnValueProvider;
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
                        // Create ValueAggregateMetadata from the ColumnValueMetadata
                        var metadataList = metadata.Values.Select(value => new ValueAggregateMetadata
                        {
                            Value = value,
                            Count = metadata.ValueCounts.ContainsKey(value) ? metadata.ValueCounts[value] : 1
                        });
                        
                        FilterValueViewModel.LoadValuesWithMetadata(metadataList);
                    }
                }

                // Synchronize rules to values when switching to values tab
                SynchronizeRulesToValues();
                UpdateValueSelectionSummary();
            }
            else if (_pendingTab?.Header?.ToString() == "Filter Rules" && SearchTemplateController != null)
            {
                // When switching to Rules tab, sync values to rules only if no meaningful rules exist
                // This preserves user-created rules while allowing value-based rule generation when appropriate
                if (!HasAnyMeaningfulRules())
                {
                    SynchronizeValuesToRules();
                }
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

                // Create ValueAggregateMetadata from the ColumnValueMetadata
                var metadataList = metadata.Values.Select(value => new ValueAggregateMetadata
                {
                    Value = value,
                    Count = metadata.ValueCounts.ContainsKey(value) ? metadata.ValueCounts[value] : 1
                });

                // Reload the view model with updated values
                FilterValueViewModel.LoadValuesWithMetadata(metadataList);

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

        #endregion

        #region Synchronization and Live Filtering

        /// <summary>
        /// Sets up the synchronization between rules and values
        /// </summary>
        private void SetupSynchronization()
        {
            // Clean up existing timers first
            CleanupSynchronizationTimers();

            // Initialize live filtering timer for debouncing (only for live filtering, not sync)
            _liveFilterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250),
                IsEnabled = false
            };
            _liveFilterTimer.Tick += OnLiveFilterTimerTick;

            // Subscribe to FilterValueViewModel selection changes (remove first to avoid duplicates)
            if (FilterValueViewModel != null)
            {
                FilterValueViewModel.SelectionChanged -= OnFilterValueSelectionChanged;
                FilterValueViewModel.SelectionChanged += OnFilterValueSelectionChanged;
            }

            // Subscribe to SearchTemplateController changes (remove first to avoid duplicates)
            if (SearchTemplateController != null)
            {
                SearchTemplateController.PropertyChanged -= OnSearchTemplateControllerChanged;
                SearchTemplateController.PropertyChanged += OnSearchTemplateControllerChanged;
            }
        }

        /// <summary>
        /// Cleans up synchronization timers
        /// </summary>
        private void CleanupSynchronizationTimers()
        {
            if (_liveFilterTimer != null)
            {
                _liveFilterTimer.Stop();
                _liveFilterTimer.Tick -= OnLiveFilterTimerTick;
                _liveFilterTimer = null;
            }
        }

        /// <summary>
        /// Ensures live filtering timer is initialized (safety check for reopened dialogs)
        /// </summary>
        private void EnsureLiveFilterTimerInitialized()
        {
            if (_liveFilterTimer == null)
            {
                _liveFilterTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(250),
                    IsEnabled = false
                };
                _liveFilterTimer.Tick += OnLiveFilterTimerTick;
            }
        }

        /// <summary>
        /// Handles changes in filter value selections (Values tab)
        /// </summary>
        private void OnFilterValueSelectionChanged(object sender, EventArgs e)
        {
            if (_isSynchronizing || FilterValueViewModel?.IsSynchronizing == true)
                return;

            // Synchronize immediately when values change
            SynchronizeValuesToRules();

            // Also trigger live filtering if enabled
            if (_enableLiveFiltering)
            {
                EnsureLiveFilterTimerInitialized();
                _liveFilterTimer.Stop();
                _liveFilterTimer.Start();
            }
        }

        /// <summary>
        /// Handles changes in search template controller (Rules tab)
        /// </summary>
        private void OnSearchTemplateControllerChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isSynchronizing)
                return;

            // Look for property changes that indicate rule modifications
            if (e.PropertyName == nameof(SearchTemplateController.HasCustomExpression) ||
                e.PropertyName == "FilterExpression" ||
                e.PropertyName == "SearchGroups" ||
                string.IsNullOrEmpty(e.PropertyName)) // Bulk changes
            {
                // Synchronize immediately when rules change (only if rules have meaningful content)
                SynchronizeRulesToValues();

                // Also trigger live filtering if enabled
                if (_enableLiveFiltering)
                {
                    EnsureLiveFilterTimerInitialized();
                    _liveFilterTimer.Stop();
                    _liveFilterTimer.Start();
                }
            }
        }


        /// <summary>
        /// Handles debounced live filtering
        /// </summary>
        private void OnLiveFilterTimerTick(object sender, EventArgs e)
        {
            _liveFilterTimer.Stop();
            ApplyLiveFilter();
        }

        /// <summary>
        /// Synchronizes rules tab changes to values tab
        /// </summary>
        private void SynchronizeRulesToValues()
        {
            if (_isSynchronizing || SearchTemplateController == null || FilterValueViewModel == null)
                return;

            // Only sync if we have meaningful rules to evaluate
            if (!HasAnyMeaningfulRules())
                return;

            try
            {
                _isSynchronizing = true;

                // Get all available values
                var allValues = FilterValueViewModel.GetAllValues().Select(v => v.Value);
                
                // Evaluate which values should be selected based on current rules
                var selectedValues = _ruleToValueSyncService.EvaluateRulesAgainstValues(SearchTemplateController, allValues);
                
                // Update the FilterValueViewModel selections
                FilterValueViewModel.UpdateSelectionsFromRules(selectedValues);
                
                // Update the summary display
                UpdateValueSelectionSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error synchronizing rules to values: {ex.Message}");
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Synchronizes values tab changes to rules tab (preserves meaningful user rules)
        /// </summary>
        private void SynchronizeValuesToRules()
        {
            if (_isSynchronizing || FilterValueViewModel == null || SearchTemplateController == null)
                return;

            try
            {
                _isSynchronizing = true;
                
                // Only sync if we don't have meaningful configured rules
                // This preserves user-created rules while allowing value-based rule generation
                if (HasAnyMeaningfulRules())
                    return;
                
                // Use existing FilterApplicationService logic to convert values to rules
                var result = _filterApplicationService.ApplyValueBasedFilter(
                    FilterValueViewModel, SearchTemplateController, ColumnDataType);
                
                if (!result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"Error synchronizing values to rules: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error synchronizing values to rules: {ex.Message}");
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Checks if there are any meaningful rules configured
        /// </summary>
        private bool HasAnyMeaningfulRules()
        {
            if (SearchTemplateController?.SearchGroups == null)
                return false;

            return SearchTemplateController.SearchGroups
                .SelectMany(g => g.SearchTemplates ?? Enumerable.Empty<SearchTemplate>())
                .Any(t => HasMeaningfulSearchCriteria(t));
        }

        /// <summary>
        /// Checks if a search template has meaningful criteria (helper method)
        /// </summary>
        private bool HasMeaningfulSearchCriteria(SearchTemplate template)
        {
            if (template == null)
                return false;

            // Check if template has a non-default search type with values
            switch (template.SearchType)
            {
                case SearchType.Contains:
                case SearchType.DoesNotContain:
                case SearchType.StartsWith:
                case SearchType.EndsWith:
                case SearchType.Equals:
                case SearchType.NotEquals:
                case SearchType.LessThan:
                case SearchType.LessThanOrEqualTo:
                case SearchType.GreaterThan:
                case SearchType.GreaterThanOrEqualTo:
                case SearchType.IsLike:
                case SearchType.IsNotLike:
                    return !string.IsNullOrEmpty(template.SelectedValue?.ToString());

                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    return template.SelectedValue != null && template.SelectedSecondaryValue != null;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                case SearchType.IsOnAnyOfDates:
                    return template.SelectedValues?.Any() == true;

                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.IsEmpty:
                case SearchType.IsNotEmpty:
                case SearchType.Yesterday:
                case SearchType.Today:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    return true; // These don't need values

                case SearchType.DateInterval:
                    return template.SelectedValue != null; // Should have interval type

                case SearchType.TopN:
                case SearchType.BottomN:
                    return template.SelectedValue != null && int.TryParse(template.SelectedValue.ToString(), out var n) && n > 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Applies filters immediately to the DataGrid (live filtering)
        /// </summary>
        private void ApplyLiveFilter()
        {
            if (!_enableLiveFiltering || SearchTemplateController == null)
                return;

            try
            {
                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    // Apply intelligent filtering without closing the dialog
                    var selectedTabIndex = tabControl?.SelectedIndex ?? -1;
                    var result = _filterApplicationService.ApplyIntelligentFilter(
                        FilterValueViewModel, SearchTemplateController, ColumnDataType, selectedTabIndex);

                    if (result.IsSuccess)
                    {
                        // Update the column search box state
                        columnSearchBox.HasAdvancedFilter = result.HasCustomExpression;
                        
                        // Handle grouped filtering if needed
                        if (result.FilterType == FilterApplicationType.GroupedValueBased)
                        {
                            columnSearchBox.GroupedFilterCombinations = SearchTemplateController.GroupedFilterCombinations;
                            columnSearchBox.GroupByColumnPath = SearchTemplateController.GroupByColumnPath;
                        }

                        // Apply the filter to the DataGrid
                        columnSearchBox.SourceDataGrid.FilterItemsSource();
                        
                        // Update filter panel
                        columnSearchBox.SourceDataGrid.UpdateFilterPanel();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying live filter: {ex.Message}");
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

            if (DataContext is ColumnSearchBox columnSearchBox)
            {
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

            // Clean up synchronization and live filtering timers
            CleanupSynchronizationTimers();

            // Unsubscribe from FilterValueViewModel events
            if (FilterValueViewModel != null)
            {
                FilterValueViewModel.SelectionChanged -= OnFilterValueSelectionChanged;
            }

            // Unsubscribe from additional SearchTemplateController events
            if (SearchTemplateController != null)
            {
                SearchTemplateController.PropertyChanged -= OnSearchTemplateControllerChanged;
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