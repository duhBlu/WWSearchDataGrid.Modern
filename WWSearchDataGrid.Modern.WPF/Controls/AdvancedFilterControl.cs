using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Enhanced advanced filter control with tabbed interface
    /// </summary>
    public class AdvancedFilterControl : Control
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

        private readonly ColumnValueCache _cache = ColumnValueCache.Instance;
        private DispatcherTimer _tabSwitchTimer;
        private TabItem _pendingTab;
        private bool _isInitialized;
        private string _columnKey;

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

        #region Dependency Properties

        private static readonly DependencyPropertyKey ValueSelectionSummaryPropertyKey =
            DependencyProperty.RegisterReadOnly("ValueSelectionSummary", typeof(string), typeof(AdvancedFilterControl),
                new PropertyMetadata("No values selected"));

        public static readonly DependencyProperty ValueSelectionSummaryProperty = ValueSelectionSummaryPropertyKey.DependencyProperty;

        #endregion

        #region Commands

        /// <summary>
        /// Add search group command
        /// </summary>
        public ICommand AddSearchGroupCommand => new RelayCommand(p =>
        {
            SearchTemplateGroup group = p as SearchTemplateGroup;
            SearchTemplateController?.AddSearchGroup(true, true, group);
        });

        /// <summary>
        /// Remove search group command
        /// </summary>
        public ICommand RemoveSearchGroupCommand => new RelayCommand(p =>
        {
            SearchTemplateGroup group = p as SearchTemplateGroup;
            SearchTemplateController?.RemoveSearchGroup(group);
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

            UpdateTemplateOperatorVisibility();
        });

        /// <summary>
        /// Remove search template command
        /// </summary>
        public ICommand RemoveSearchTemplateCommand => new RelayCommand(p =>
        {
            SearchTemplate template = p as SearchTemplate;
            SearchTemplateController?.RemoveSearchTemplate(template);
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

            // Find and hook up the buttons
            applyButton = GetTemplateChild("PART_ApplyButton") as Button;
            if (applyButton != null)
            {
                applyButton.Click += (s, e) => ApplyFilter();
            }

            clearButton = GetTemplateChild("PART_ClearButton") as Button;
            if (clearButton != null)
            {
                clearButton.Click += (s, e) => ClearFilter();
            }

            closeButton = GetTemplateChild("PART_CloseButton") as Button;
            if (closeButton != null)
            {
                closeButton.Click += (s, e) => CloseWindow();
            }
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
                await LoadGroupedDataForViewModel(groupedViewModel, searchControl, items, groupByColumn);

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
        private async Task LoadGroupedDataForViewModel(GroupedTreeViewFilterValueViewModel groupedViewModel,
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

                await RefreshFilterValueViewModel();
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
            await RefreshFilterValueViewModel();
        }

        private async Task RefreshFilterValueViewModel()
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
        /// Optimized template updating to reduce unnecessary operations
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

                    for (int t = 0; t < templates.Count; t++)
                    {
                        templates[t].IsOperatorVisible = t > 0;
                    }
                }
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
        /// Apply the filter and close the window
        /// </summary>
        private void ApplyFilter()
        {
            if (SearchTemplateController != null)
            {
                // Check which tab is active
                if (tabControl?.SelectedIndex == 1) // Filter Values tab
                {
                    // Apply filter based on selected values
                    ApplyValueBasedFilter();
                }
                else // Filter Rules tab
                {
                    // Apply rule-based filter
                    SearchTemplateController.UpdateFilterExpression();
                }

                // Determine if we're in per-column or global mode
                if (DataContext is SearchControl searchControl)
                {
                    // Per-column mode
                    if (searchControl.SourceDataGrid != null)
                    {
                        searchControl.HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
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
        }

        /// <summary>
        /// Apply filter based on selected values with better performance
        /// </summary>
        private void ApplyValueBasedFilter()
        {
            if (FilterValueViewModel != null)
            {
                // Check if this is grouped filtering
                if (FilterValueViewModel is GroupedTreeViewFilterValueViewModel groupedViewModel && groupedViewModel.IsGroupedFiltering)
                {
                    ApplyGroupedValueBasedFilter(groupedViewModel);
                }
                else
                {
                    // Standard flat filtering with optimization
                    var allValues = FilterValueViewModel.GetAllValues();
                    var selectedItems = allValues.Where(item => item.IsSelected).ToList();

                    // Only update if there are changes
                    if (selectedItems.Any())
                    {
                        // Use optimizer to determine best filter strategy
                        var optimizationResult = FilterSelectionOptimizer.OptimizeSelections(
                            allValues, selectedItems, ColumnDataType);

                        // Clear and recreate more efficiently
                        SearchTemplateController.SearchGroups.Clear();
                        var group = new SearchTemplateGroup();
                        SearchTemplateController.SearchGroups.Add(group);

                        var template = new SearchTemplate(ColumnDataType)
                        {
                            SearchType = optimizationResult.RecommendedSearchType
                        };

                        // Batch add values based on optimization result
                        template.SelectedValues.Clear();
                        foreach (var value in optimizationResult.FilterValues)
                        {
                            template.SelectedValues.Add(new FilterListValue { Value = value });
                        }

                        group.SearchTemplates.Add(template);
                        SearchTemplateController.UpdateFilterExpression();

                        // Optional: Log optimization info for debugging
                        System.Diagnostics.Debug.WriteLine($"Filter optimization: {optimizationResult.OptimizationReason}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply grouped filtering that respects group-child combinations
        /// </summary>
        private void ApplyGroupedValueBasedFilter(GroupedTreeViewFilterValueViewModel groupedViewModel)
        {
            var groupChildCombinations = groupedViewModel.GetSelectedGroupChildCombinations().ToList();
            
            if (!groupChildCombinations.Any())
            {
                // No selections - clear filters
                SearchTemplateController.SearchGroups.Clear();
                SearchTemplateController.AddSearchGroup();
                SearchTemplateController.HasCustomExpression = false;
                return;
            }
            
            // Get the binding paths for both columns
            string currentColumnPath = null;
            string groupByColumnPath = groupedViewModel.GroupByColumn;
            
            if (DataContext is SearchControl searchControl)
            {
                currentColumnPath = searchControl.BindingPath;
            }
            
            if (string.IsNullOrEmpty(currentColumnPath) || string.IsNullOrEmpty(groupByColumnPath))
            {
                // Fallback to regular filtering if we can't determine the paths
                var selectedValues = groupedViewModel.GetSelectedValues().ToList();
                if (selectedValues.Any())
                {
                    SearchTemplateController.SearchGroups.Clear();
                    var group = new SearchTemplateGroup();
                    SearchTemplateController.SearchGroups.Add(group);

                    var template = new SearchTemplate(ColumnDataType)
                    {
                        SearchType = SearchType.IsAnyOf
                    };

                    foreach (var value in selectedValues)
                    {
                        template.SelectedValues.Add(new FilterListValue { Value = value });
                    }

                    group.SearchTemplates.Add(template);
                    SearchTemplateController.UpdateFilterExpression();
                }
                return;
            }
            
            // Clear existing groups and create a custom filter expression
            SearchTemplateController.SearchGroups.Clear();
            
            // Create a custom filter expression that handles grouped logic
            CreateGroupedFilterExpression(groupChildCombinations, currentColumnPath, groupByColumnPath);
        }
        
        /// <summary>
        /// Creates a custom filter expression for grouped filtering
        /// </summary>
        private void CreateGroupedFilterExpression(List<(object GroupKey, object ChildValue)> combinations, 
            string currentColumnPath, string groupByColumnPath)
        {
            if (DataContext is SearchControl searchControl)
            {
                // Create a custom filter function that checks both the group column and current column
                Func<object, bool> groupedFilter = columnValue =>
                {
                    try
                    {
                        // We need to check against the entire data item, not just the column value
                        // Unfortunately, we only get the column value here, so we need a different approach
                        
                        // For now, return true if the column value matches any selected child value
                        // This is a limitation - we'll need to enhance the SearchTemplateController
                        return combinations.Any(c => Equals(c.ChildValue, columnValue));
                    }
                    catch
                    {
                        return false;
                    }
                };
                
                // Store the grouped combinations for use in a custom evaluation
                searchControl.GroupedFilterCombinations = combinations;
                searchControl.GroupByColumnPath = groupByColumnPath;
                
                // Set grouped filtering information on the SearchTemplateController
                SearchTemplateController.GroupedFilterCombinations = combinations;
                SearchTemplateController.GroupByColumnPath = groupByColumnPath;
                SearchTemplateController.CurrentColumnPath = currentColumnPath;
                
                // Try to get the grouped view model to extract all group data
                if (FilterValueViewModel is GroupedTreeViewFilterValueViewModel groupedViewModel)
                {
                    // Extract all group data for analysis
                    var allGroupData = new Dictionary<object, List<object>>();
                    foreach (var group in groupedViewModel.AllGroups)
                    {
                        var groupValues = group.Children.Select(c => c.Value).ToList();
                        allGroupData[group.GroupKey] = groupValues;
                    }
                    SearchTemplateController.AllGroupData = allGroupData;
                }
                
                // Set the filter expression on the controller
                SearchTemplateController.FilterExpression = groupedFilter;
                SearchTemplateController.HasCustomExpression = true;
            }
        }

        /// <summary>
        /// Clear the filter
        /// </summary>
        private void ClearFilter()
        {
            if (SearchTemplateController != null)
            {
                SearchTemplateController.SearchGroups.Clear();
                SearchTemplateController.AddSearchGroup();
                SearchTemplateController.HasCustomExpression = false;

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
            }

            // Unhook SearchTemplateController property changes
            if (SearchTemplateController != null)
            {
                SearchTemplateController.PropertyChanged -= OnSearchTemplateControllerPropertyChanged;
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
            // This would typically use INotifyPropertyChanged implementation
        }

        #endregion
    }
}