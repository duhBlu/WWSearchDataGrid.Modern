using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;
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
        private ContentControl filterValuesContent;
        private TextBox valueSearchBox;
        private TextBlock valuesSummary;
        private ColumnDataType columnDataType;
        private FilterValueViewModel filterValueViewModel;

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
                UpdateFilterValueViewModelFromCache();
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
            ISearchTemplate template = p as ISearchTemplate;

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
            ISearchTemplate template = p as ISearchTemplate;
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

            // Find and hook up the ListBox
            groupsListBox = GetTemplateChild("PART_GroupsListBox") as ListBox;
            if (groupsListBox != null)
            {
                groupsListBox.AllowDrop = true;
                groupsListBox.PreviewMouseLeftButtonDown += OnListBoxPreviewMouseLeftButtonDown;
                groupsListBox.Drop += OnListBoxDrop;
            }

            // Find tab control
            tabControl = GetTemplateChild("PART_TabControl") as TabControl;

            // Find filter values content
            filterValuesContent = GetTemplateChild("PART_FilterValuesContent") as ContentControl;

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

            // Load column values asynchronously
            await LoadColumnValuesAsync();

            // Update filter value view model using cache
            UpdateFilterValueViewModelFromCache();

            // Bind to UI
            if (groupsListBox != null && SearchTemplateController != null)
            {
                groupsListBox.ItemsSource = SearchTemplateController.SearchGroups;
            }

            // Hook up tab control selection changed for lazy loading
            if (tabControl != null)
            {
                tabControl.SelectionChanged += OnTabControlSelectionChanged;
            }
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
                if (!string.IsNullOrEmpty(bindingPath) && searchControl.SourceDataGrid?.Items != null)
                {
                    var items = searchControl.SourceDataGrid.Items.Cast<object>().ToList();
                    await _cache.UpdateColumnValuesAsync(_columnKey, items, bindingPath);

                    // Update controller with cached values
                    var metadata = _cache.GetOrCreateMetadata(_columnKey, bindingPath);
                    SearchTemplateController.ColumnValues = new HashSet<object>(metadata.Values);
                    SearchTemplateController.ColumnDataType = metadata.DataType;
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
        /// Updates the filter value view model using cached data
        /// </summary>
        private void UpdateFilterValueViewModelFromCache()
        {
            if (string.IsNullOrEmpty(_columnKey))
                return;

            // Get or create cached view model
            FilterValueViewModel = _cache.GetOrCreateFilterViewModel(
                _columnKey,
                ColumnDataType,
                FilterValueConfiguration);

            // Ensure data is loaded
            var metadata = _cache.GetOrCreateMetadata(_columnKey, string.Empty);
            if (metadata.Values.Any())
            {
                FilterValueViewModel.LoadValuesWithCounts(metadata.Values, metadata.ValueCounts);
            }

            UpdateValueSelectionSummary();
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
            if (e.Data.GetDataPresent(typeof(ISearchTemplate)))
            {
                var template = e.Data.GetData(typeof(ISearchTemplate)) as ISearchTemplate;
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
                            var targetTemplate = targetItem.DataContext as ISearchTemplate;
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
        private SearchTemplateGroup FindParentGroup(ISearchTemplate template)
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
                var selectedValues = FilterValueViewModel.GetSelectedValues().ToList();

                // Only update if there are changes
                if (selectedValues.Any())
                {
                    // Clear and recreate more efficiently
                    SearchTemplateController.SearchGroups.Clear();
                    var group = new SearchTemplateGroup();
                    SearchTemplateController.SearchGroups.Add(group);

                    var template = new SearchTemplate(ColumnDataType)
                    {
                        SearchType = SearchType.IsAnyOf
                    };

                    // Batch add values
                    template.SelectedValues.Clear();
                    foreach (var value in selectedValues)
                    {
                        template.SelectedValues.Add(new FilterListValue { Value = value });
                    }

                    group.SearchTemplates.Add(template);
                    SearchTemplateController.UpdateFilterExpression();
                }
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

    /// <summary>
    /// Extension methods for performance
    /// </summary>
    public static class PerformanceExtensions
    {
        /// <summary>
        /// Executes an action after a delay, cancelling any previous pending execution
        /// </summary>
        public static void Debounce(this DispatcherTimer timer, TimeSpan delay, Action action)
        {
            timer.Stop();
            timer.Interval = delay;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                action();
            };
            timer.Start();
        }
    }
}