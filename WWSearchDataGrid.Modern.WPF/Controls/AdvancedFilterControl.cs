using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
    /// Enhanced advanced filter control with tabbed interface
    /// </summary>
    public class AdvancedFilterControl : Control, INotifyPropertyChanged
    {
        #region Fields

        private ListBox groupsListBox;
        private Button applyButton;
        private Button clearButton;
        private Button closeButton;
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
        
        // Timer for debouncing operator visibility updates
        private DispatcherTimer _operatorVisibilityUpdateTimer;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty FilterValueViewModelProperty =
            DependencyProperty.Register(nameof(FilterValueViewModel),
                typeof(FilterValueViewModel),
                typeof(AdvancedFilterControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Attached property for specifying the GroupBy column for filter values
        /// </summary>
        public static readonly DependencyProperty GroupByColumnProperty =
            DependencyProperty.RegisterAttached("GroupByColumn", typeof(string), typeof(AdvancedFilterControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Dependency property for controlling operator ComboBox visibility
        /// </summary>
        public static readonly DependencyProperty IsOperatorVisibleProperty =
            DependencyProperty.Register(nameof(IsOperatorVisible), typeof(bool), typeof(AdvancedFilterControl),
                new PropertyMetadata(false));

        private static readonly DependencyPropertyKey ValueSelectionSummaryPropertyKey =
            DependencyProperty.RegisterReadOnly("ValueSelectionSummary", typeof(string), typeof(AdvancedFilterControl),
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
                        System.Diagnostics.Debug.WriteLine($"GroupOperatorName: Changing from '{currentValue}' to '{value}'");
                        
                        // Log filter state BEFORE change
                        var firstGroup = SearchTemplateController.SearchGroups[0];
                        System.Diagnostics.Debug.WriteLine($"BEFORE operator change - Group has {firstGroup.SearchTemplates.Count} templates:");
                        for (int i = 0; i < firstGroup.SearchTemplates.Count; i++)
                        {
                            var template = firstGroup.SearchTemplates[i];
                            System.Diagnostics.Debug.WriteLine($"  Template {i}: SearchType={template.SearchType}, HasCustomFilter={template.HasCustomFilter}, SelectedValue={template.SelectedValue}");
                        }
                        System.Diagnostics.Debug.WriteLine($"BEFORE - SearchTemplateController.HasCustomExpression = {SearchTemplateController.HasCustomExpression}");
                        
                        SearchTemplateController.SearchGroups[0].OperatorName = value;
                        
                        // Force update the filter expression to ensure HasCustomExpression is recalculated
                        SearchTemplateController.UpdateFilterExpression();
                        
                        OnPropertyChanged(nameof(GroupOperatorName));
                        
                        // Log filter state AFTER change
                        System.Diagnostics.Debug.WriteLine($"AFTER operator change - Group has {firstGroup.SearchTemplates.Count} templates:");
                        for (int i = 0; i < firstGroup.SearchTemplates.Count; i++)
                        {
                            var template = firstGroup.SearchTemplates[i];
                            System.Diagnostics.Debug.WriteLine($"  Template {i}: SearchType={template.SearchType}, HasCustomFilter={template.HasCustomFilter}, SelectedValue={template.SelectedValue}");
                        }
                        System.Diagnostics.Debug.WriteLine($"AFTER - SearchTemplateController.HasCustomExpression = {SearchTemplateController.HasCustomExpression}");
                        System.Diagnostics.Debug.WriteLine($"GroupOperatorName: Changed successfully. SearchGroups[0].OperatorName = {SearchTemplateController.SearchGroups[0].OperatorName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GroupOperatorName: Cannot set value '{value}' - no SearchGroups available");
                }
            }
        }

        /// <summary>
        /// Gets the list of available data columns from the parent SearchDataGrid
        /// </summary>
        public IEnumerable<SearchControl> DataColumns
        {
            get
            {
                if (DataContext is SearchControl searchControl && searchControl.SourceDataGrid != null)
                {
                    return searchControl.SourceDataGrid.DataColumns;
                }
                else if (DataContext is SearchDataGrid dataGrid)
                {
                    return dataGrid.DataColumns;
                }
                return Enumerable.Empty<SearchControl>();
            }
        }

        /// <summary>
        /// Determines if there are active filters in columns that precede the current column
        /// </summary>
        private bool HasPrecedingColumnFilters()
        {
            if (!(DataContext is SearchControl currentSearchControl) || currentSearchControl.CurrentColumn == null)
            {
                System.Diagnostics.Debug.WriteLine("HasPrecedingColumnFilters: No current search control or column");
                return false;
            }

            try
            {
                var currentDisplayIndex = currentSearchControl.CurrentColumn.DisplayIndex;
                var dataColumns = DataColumns.ToList();

                System.Diagnostics.Debug.WriteLine($"HasPrecedingColumnFilters: Current column '{currentSearchControl.CurrentColumn.Header}' has DisplayIndex {currentDisplayIndex}");

                var precedingColumnsWithFilters = dataColumns
                    .Where(col => col.CurrentColumn != null && 
                                  col.CurrentColumn.DisplayIndex < currentDisplayIndex &&
                                  col.HasActiveFilter)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"HasPrecedingColumnFilters: Found {precedingColumnsWithFilters.Count} preceding columns with filters");
                foreach (var col in precedingColumnsWithFilters)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Column '{col.CurrentColumn.Header}' (DisplayIndex: {col.CurrentColumn.DisplayIndex}, HasActiveFilter: {col.HasActiveFilter})");
                }

                return precedingColumnsWithFilters.Any();
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

        #endregion

        #region Commands

        /// <summary>
        /// Add search group command
        /// </summary>
        public ICommand AddSearchGroupCommand => new RelayCommand(p =>
        {
            SearchTemplateGroup group = p as SearchTemplateGroup;
            SearchTemplateController?.AddSearchGroup(true, true, group);
            UpdateOperatorVisibility();
        });

        /// <summary>
        /// Remove search group command
        /// </summary>
        public ICommand RemoveSearchGroupCommand => new RelayCommand(p =>
        {
            SearchTemplateGroup group = p as SearchTemplateGroup;
            SearchTemplateController?.RemoveSearchGroup(group);
            UpdateOperatorVisibility();
        });

        /// <summary>
        /// Add search template command
        /// </summary>
        public ICommand AddSearchTemplateCommand => new RelayCommand(p =>
        {
            SearchTemplate template = p as SearchTemplate;

            // When adding new template, ensure it's the smart type
            var newTemplate = new SearchTemplate(ColumnDataType);
            newTemplate.LoadAvailableValues(SearchTemplateController.ColumnValues);

            var group = SearchTemplateController.SearchGroups.First(g => g.SearchTemplates.Contains(template));
            var index = group.SearchTemplates.IndexOf(template);
            group.SearchTemplates.Insert(index + 1, newTemplate);

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
        /// Initializes a new instance of the AdvancedFilterControl class
        /// </summary>
        public AdvancedFilterControl()
        {
            _filterApplicationService = new FilterApplicationService();
            Loaded += OnControlLoaded;
            Unloaded += OnUnloaded;
        }
        
        /// <summary>
        /// Initializes a new instance with custom filter application service for testing
        /// </summary>
        /// <param name="filterApplicationService">Filter application service</param>
        internal AdvancedFilterControl(IFilterApplicationService filterApplicationService)
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

            if (DataContext is SearchControl searchControl && searchControl.SourceDataGrid != null)
            {
                searchControl.SourceDataGrid.CollectionChanged += OnSourceDataGridCollectionChanged;
                searchControl.SourceDataGrid.ItemsSourceChanged += OnItemsSourceChanged;
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
            // Clean up existing ListBox event handlers
            if (groupsListBox != null)
            {
                groupsListBox.AllowDrop = false;
                groupsListBox.PreviewMouseLeftButtonDown -= OnListBoxPreviewMouseLeftButtonDown;
                groupsListBox.Drop -= OnListBoxDrop;
                groupsListBox = null;
            }

            // Only try to find and setup ListBox if AllowMultipleGroups is true
            if (SearchTemplateController?.AllowMultipleGroups == true)
            {
                // The ListBox is created dynamically in the DataTemplate, so we need to wait for it
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    groupsListBox = FindNameInVisualTree("PART_GroupsListBox") as ListBox;
                    if (groupsListBox != null)
                    {
                        groupsListBox.AllowDrop = true;
                        groupsListBox.PreviewMouseLeftButtonDown += OnListBoxPreviewMouseLeftButtonDown;
                        groupsListBox.Drop += OnListBoxDrop;
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Finds a named element in the visual tree (including DataTemplates)
        /// </summary>
        private DependencyObject FindNameInVisualTree(string name)
        {
            return FindChildByName(this, name);
        }

        /// <summary>
        /// Recursively searches for a child element by name in the visual tree
        /// </summary>
        private DependencyObject FindChildByName(DependencyObject parent, string name)
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
            if (DataContext is SearchControl searchControl)
            {
                return $"{searchControl.CurrentColumn?.Header}_{searchControl.BindingPath}";
            }
            else if (DataContext is SearchDataGrid dataGrid)
            {
                return "GlobalFilter";
            }

            return "Unknown";
        }

        /// <summary>
        /// Loads column values asynchronously for better performance
        /// </summary>
        private async Task LoadColumnValuesAsync()
        {
            if (SearchTemplateController == null || string.IsNullOrEmpty(_columnKey))
                return;

            // Check if we're in per-column or global mode
            if (DataContext is SearchControl searchControl)
            {
                var bindingPath = searchControl.BindingPath;
                if (!string.IsNullOrEmpty(bindingPath) && searchControl.SourceDataGrid?.OriginalItemsSource != null)
                {
                    // Use the original unfiltered items source to preserve all possible values
                    var items = searchControl.SourceDataGrid.OriginalItemsSource.Cast<object>().ToList();
                    await _cache.UpdateColumnValuesAsync(_columnKey, items, bindingPath);

                    // Update controller with cached values
                    var metadata = _cache.GetOrCreateMetadata(_columnKey, bindingPath);
                    SearchTemplateController.ColumnValues = new HashSet<object>(metadata.Values);
                    SearchTemplateController.ColumnDataType = metadata.DataType;

                    // Create and set up the FilterValueViewModel first
                    await SetupFilterValueViewModel(searchControl, items, metadata);
                }
            }
            else if (DataContext is SearchDataGrid dataGrid)
            {
                // Global mode - aggregate all column values
                var tasks = new List<Task>();

                foreach (var column in dataGrid.DataColumns)
                {
                    if (!string.IsNullOrEmpty(column.BindingPath))
                    {
                        var columnKey = $"{column.CurrentColumn?.Header}_{column.BindingPath}";
                        var bindingPath = column.BindingPath;
                        var items = dataGrid.Items.Cast<object>().ToList();

                        tasks.Add(_cache.UpdateColumnValuesAsync(columnKey, items, bindingPath));
                    }
                }

                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Sets up the FilterValueViewModel with proper data
        /// </summary>
        private async Task SetupFilterValueViewModel(SearchControl searchControl, List<object> items, dynamic metadata)
        {
            // Check if GroupBy column is specified
            var groupByColumn = GetGroupByColumn(searchControl.CurrentColumn);

            if (!string.IsNullOrEmpty(groupByColumn))
            {
                // Create grouped tree view model
                var groupedViewModel = new GroupedTreeViewFilterValueViewModel();
                groupedViewModel.GroupByColumn = groupByColumn;

                // Load grouped data
                LoadGroupedDataForViewModel(groupedViewModel, searchControl, items, groupByColumn);

                FilterValueViewModel = groupedViewModel;

                // Load the data with counts
                if (metadata.Values != null && metadata.Values.Count > 0)
                {
                    FilterValueViewModel.LoadValuesWithCounts(metadata.Values, metadata.ValueCounts);
                }
            }
            else
            {
                // Use regular filter value view model from cache
                FilterValueViewModel = _cache.GetOrCreateFilterViewModel(
                    _columnKey,
                    ColumnDataType,
                    FilterValueConfiguration);

                // Load the data with counts
                if (metadata.Values != null && metadata.Values.Count > 0)
                {
                    FilterValueViewModel.LoadValuesWithCounts(metadata.Values, metadata.ValueCounts);
                }
            }
        }

        /// <summary>
        /// Loads grouped data for the filter values view model
        /// </summary>
        private void LoadGroupedDataForViewModel(GroupedTreeViewFilterValueViewModel groupedViewModel,
            SearchControl searchControl, List<object> items, string groupByColumn)
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
                    var value = ReflectionHelper.GetPropValue(item, searchControl.BindingPath);

                    // Group the current column's values by the GroupBy column's values
                    if (!groupedData.ContainsKey(groupKey))
                    {
                        groupedData[groupKey] = new List<object>();
                    }

                    // Only add unique values to each group
                    if (!groupedData[groupKey].Contains(value))
                    {
                        groupedData[groupKey].Add(value);
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
            if (SearchTemplateController == null)
            {
                // Get the mode from the source SearchDataGrid
                if (DataContext is SearchControl searchControl && searchControl.SourceDataGrid != null)
                {
                    SearchTemplateController = searchControl.SearchTemplateController;
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
                            newTemplate.LoadAvailableValues(oldTemplate.AvailableValues);
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
            if (SearchTemplateController != null && SearchTemplateController.ColumnValues.Any())
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
                    if (metadata.Values.Any())
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
            if (string.IsNullOrEmpty(_columnKey) || DataContext is not SearchControl searchControl)
                return;

            try
            {
                // Try incremental update first
                if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
                {
                    await _cache.UpdateColumnValuesIncrementalAsync(_columnKey, e, searchControl.BindingPath);
                }
                else
                {
                    // Fall back to full reload for complex changes
                    var items = searchControl.SourceDataGrid.OriginalItemsSource?.Cast<object>().ToList();
                    if (items?.Any() == true)
                    {
                        await _cache.UpdateColumnValuesAsync(_columnKey, items, searchControl.BindingPath);
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
            if (FilterValueViewModel != null && DataContext is SearchControl searchControl)
            {
                var metadata = _cache.GetOrCreateMetadata(_columnKey, searchControl.BindingPath);

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
                
                System.Diagnostics.Debug.WriteLine($"UpdateOperatorVisibility: IsOperatorVisible = {IsOperatorVisible}");
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
            if (!(DataContext is SearchControl currentSearchControl) || currentSearchControl.SourceDataGrid == null)
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
                currentSearchControl.SourceDataGrid.ItemsSourceFiltered += OnFilteringCompleted;
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
        /// Forces an update of operator visibility (can be called externally)
        /// </summary>
        public void RefreshOperatorVisibility()
        {
            UpdateOperatorVisibility();
        }

        /// <summary>
        /// Updates template-level operator visibility (for templates within groups)
        /// </summary>
        private void UpdateTemplateOperatorVisibility()
        {
            if (SearchTemplateController != null)
            {
                var groups = SearchTemplateController.SearchGroups;
                System.Diagnostics.Debug.WriteLine($"UpdateTemplateOperatorVisibility: Found {groups.Count} groups");

                // Use a single pass to update all templates
                for (int g = 0; g < groups.Count; g++)
                {
                    var group = groups[g];
                    var templates = group.SearchTemplates;
                    System.Diagnostics.Debug.WriteLine($"UpdateTemplateOperatorVisibility: Group {g} has {templates.Count} templates");

                    // Template-level operators: show for all templates after the first in each group
                    for (int t = 0; t < templates.Count; t++)
                    {
                        var shouldBeVisible = t > 0;
                        templates[t].IsOperatorVisible = shouldBeVisible;
                        System.Diagnostics.Debug.WriteLine($"UpdateTemplateOperatorVisibility: Template {t} in group {g} - IsOperatorVisible = {shouldBeVisible}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("UpdateTemplateOperatorVisibility: SearchTemplateController is null");
            }
        }

        /// <summary>
        /// Handle mouse down on the list box for drag and drop
        /// </summary>
        private void OnListBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element)
            {
                // Find the drag handle
                if (FindVisualParent<TextBlock>(element) is TextBlock dragHandle &&
                    dragHandle.Name == "DragHandle")
                {
                    // Get the item and start drag
                    var listBoxItem = FindVisualParent<ListBoxItem>(element);
                    if (listBoxItem != null)
                    {
                        var data = listBoxItem.DataContext;
                        DragDrop.DoDragDrop(listBoxItem, data, DragDropEffects.Move);
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Handle drop on the list box
        /// </summary>
        private void OnListBoxDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(SearchTemplate)))
            {
                var template = e.Data.GetData(typeof(SearchTemplate)) as SearchTemplate;
                var targetElement = e.OriginalSource as FrameworkElement;

                if (template != null && targetElement != null)
                {
                    // Find the target group and position
                    var targetGroup = FindVisualParent<GroupBox>(targetElement)?.DataContext as SearchTemplateGroup;
                    var sourceGroup = FindParentGroup(template);

                    if (targetGroup != null && sourceGroup != null)
                    {
                        // Find the target index
                        int targetIndex = 0;
                        var targetItem = FindVisualParent<ListBoxItem>(targetElement);
                        if (targetItem != null)
                        {
                            var targetTemplate = targetItem.DataContext as SearchTemplate;
                            if (targetTemplate != null)
                            {
                                targetIndex = targetGroup.SearchTemplates.IndexOf(targetTemplate);
                            }
                        }

                        // Move the template
                        SearchTemplateController.MoveSearchTemplate(sourceGroup, targetGroup, template, targetIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Find the parent group for a template
        /// </summary>
        private SearchTemplateGroup FindParentGroup(SearchTemplate template)
        {
            foreach (var group in SearchTemplateController.SearchGroups)
            {
                if (group.SearchTemplates.Contains(template))
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a visual parent of the specified type
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }

            return child as T;
        }

        /// <summary>
        /// Apply the filter and close the window using intelligent filter selection
        /// </summary>
        private void ApplyFilter()
        {
            if (SearchTemplateController == null) return;
            
            // Debug logging before applying filter
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: About to apply filter");
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: SearchGroups.Count = {SearchTemplateController.SearchGroups.Count}");
            if (SearchTemplateController.SearchGroups.Count > 0)
            {
                var firstGroup = SearchTemplateController.SearchGroups[0];
                System.Diagnostics.Debug.WriteLine($"ApplyFilter: First group OperatorName = {firstGroup.OperatorName}");
                System.Diagnostics.Debug.WriteLine($"ApplyFilter: First group has {firstGroup.SearchTemplates.Count} templates");
                
                for (int i = 0; i < firstGroup.SearchTemplates.Count; i++)
                {
                    var template = firstGroup.SearchTemplates[i];
                    System.Diagnostics.Debug.WriteLine($"ApplyFilter: Template {i} - SearchType = {template.SearchType}, HasCustomFilter = {template.HasCustomFilter}, SelectedValue = {template.SelectedValue}");
                }
            }
            
            // INTELLIGENT FILTER APPLICATION: Use content-based decision making instead of tab-based
            var selectedTabIndex = tabControl?.SelectedIndex ?? -1;
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: tabControl.SelectedIndex = {selectedTabIndex} (provided as context only)");
            
            var result = _filterApplicationService.ApplyIntelligentFilter(
                FilterValueViewModel, SearchTemplateController, ColumnDataType, selectedTabIndex);
            
            if (!result.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Filter application failed: {result.ErrorMessage}");
                return;
            }
            
            // Debug logging after applying filter
            System.Diagnostics.Debug.WriteLine($"ApplyFilter: Filter applied successfully using {result.FilterType}. HasCustomExpression = {result.HasCustomExpression}");

            // Determine if we're in per-column or global mode
            if (DataContext is SearchControl searchControl)
            {
                // Per-column mode
                if (searchControl.SourceDataGrid != null)
                {
                    searchControl.HasAdvancedFilter = result.HasCustomExpression;
                    
                    // Handle grouped filtering if needed
                    if (result.FilterType == FilterApplicationType.GroupedValueBased)
                    {
                        searchControl.GroupedFilterCombinations = SearchTemplateController.GroupedFilterCombinations;
                        searchControl.GroupByColumnPath = SearchTemplateController.GroupByColumnPath;
                    }
                    
                    searchControl.SourceDataGrid.FilterItemsSource();
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
            if (DataContext is SearchControl searchControl)
            {
                // Per-column mode
                searchControl.SearchText = string.Empty;
                searchControl.HasAdvancedFilter = false;

                if (searchControl.SourceDataGrid != null)
                {
                    searchControl.SourceDataGrid.FilterItemsSource();
                }
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

            if (DataContext is SearchControl searchControl && searchControl.SourceDataGrid != null)
            {
                searchControl.SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                searchControl.SourceDataGrid.ItemsSourceChanged -= OnItemsSourceChanged;
                
                // Unsubscribe from filter completion events
                searchControl.SourceDataGrid.ItemsSourceFiltered -= OnFilteringCompleted;
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

            // Clean up ListBox event handlers
            if (groupsListBox != null)
            {
                groupsListBox.AllowDrop = false;
                groupsListBox.PreviewMouseLeftButtonDown -= OnListBoxPreviewMouseLeftButtonDown;
                groupsListBox.Drop -= OnListBoxDrop;
                groupsListBox = null;
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