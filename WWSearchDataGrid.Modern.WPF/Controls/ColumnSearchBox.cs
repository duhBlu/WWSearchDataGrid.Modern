using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Search control for filtering data grid columns
    /// </summary>
    public class ColumnSearchBox : Control
    {
        #region Fields

        private TextBox searchTextBox;
        private Button advancedFilterButton;
        private Window advancedFilterWindow;
        private bool isAdvancedFilterOpen;
        private Timer _changeTimer;
        private SearchTemplate _temporarySearchTemplate; // Track temporary template for real-time updates

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentColumnProperty =
            DependencyProperty.Register("CurrentColumn", typeof(DataGridColumn), typeof(ColumnSearchBox),
                new PropertyMetadata(null, OnCurrentColumnChanged));

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register("SourceDataGrid", typeof(SearchDataGrid), typeof(ColumnSearchBox),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(ColumnSearchBox),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public static readonly DependencyProperty HasAdvancedFilterProperty =
            DependencyProperty.Register("HasAdvancedFilter", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CustomSearchTemplateProperty =
            DependencyProperty.RegisterAttached("CustomSearchTemplate", typeof(Type), typeof(ColumnSearchBox),
                new FrameworkPropertyMetadata(typeof(SearchTemplate)));

        public static readonly DependencyProperty ShowInAdvancedFilterProperty =
            DependencyProperty.RegisterAttached("ShowInAdvancedFilter", typeof(bool), typeof(ColumnSearchBox),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        #endregion
        
        #region Grouped Filtering Properties
        
        /// <summary>
        /// Stores grouped filter combinations for custom filtering
        /// </summary>
        public List<(object GroupKey, object ChildValue)> GroupedFilterCombinations { get; set; }
        
        /// <summary>
        /// Gets or sets the GroupBy column path for grouped filtering
        /// </summary>
        public string GroupByColumnPath { get; set; }
        
        #endregion

        #region Properties

        /// <summary>
        /// Gets the command to clear search text (only clears search text and temporary template)
        /// </summary>
        public ICommand ClearSearchTextCommand => new RelayCommand(_ => ClearSearchTextAndTemporaryFilter());

        /// <summary>
        /// Gets or sets the current column being filtered
        /// </summary>
        public DataGridColumn CurrentColumn
        {
            get => (DataGridColumn)GetValue(CurrentColumnProperty);
            set => SetValue(CurrentColumnProperty, value);
        }

        /// <summary>
        /// Gets or sets the source data grid
        /// </summary>
        public SearchDataGrid SourceDataGrid
        {
            get => (SearchDataGrid)GetValue(SourceDataGridProperty);
            set => SetValue(SourceDataGridProperty, value);
        }

        /// <summary>
        /// Gets or sets the search text
        /// </summary>
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        /// <summary>
        /// Gets or sets whether an advanced filter is applied
        /// </summary>
        public bool HasAdvancedFilter
        {
            get => (bool)GetValue(HasAdvancedFilterProperty);
            set => SetValue(HasAdvancedFilterProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the advanced filter button
        /// </summary>
        public bool ShowInAdvancedFilter
        {
            get => (bool)GetValue(ShowInAdvancedFilterProperty);
            set => SetValue(ShowInAdvancedFilterProperty, value);
        }

        /// <summary>
        /// Gets the search template controller
        /// </summary>
        public SearchTemplateController SearchTemplateController { get; private set; }

        /// <summary>
        /// Gets the binding path for the column
        /// </summary>
        public string BindingPath { get; private set; }

        /// <summary>
        /// Gets whether this control has an active filter
        /// </summary>
        public bool HasActiveFilter
        {
            get
            {
                if (SearchTemplateController == null)
                    return false;

                // Check if we have a simple text filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                    return true;

                // Check if we have an advanced filter
                return SearchTemplateController.HasCustomExpression;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ColumnSearchBox class
        /// </summary>
        public ColumnSearchBox()
        {
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
            
            // Make the control non-focusable so focus goes directly to child elements
            Focusable = false;
            
            // Handle container-level focus events
            LostFocus += OnColumnSearchBoxLostFocus;
            GotFocus += OnColumnSearchBoxGotFocus;
        }

        #endregion

        #region Attached Property Methods

        /// <summary>
        /// Gets the custom search template for an object
        /// </summary>
        public static Type GetCustomSearchTemplate(DependencyObject obj) =>
            (Type)obj.GetValue(CustomSearchTemplateProperty);

        /// <summary>
        /// Sets the custom search template for an object
        /// </summary>
        public static void SetCustomSearchTemplate(DependencyObject obj, Type value) =>
            obj.SetValue(CustomSearchTemplateProperty, value);

        /// <summary>
        /// Sets whether to show the advanced filter
        /// </summary>
        public static void SetShowInAdvancedFilter(DependencyObject element, bool value) =>
            element.SetValue(ShowInAdvancedFilterProperty, value);

        /// <summary>
        /// Gets whether to show the advanced filter
        /// </summary>
        public static bool GetShowInAdvancedFilter(DependencyObject element) =>
            (bool)element.GetValue(ShowInAdvancedFilterProperty);

        #endregion

        #region Control Template Methods

        /// <summary>
        /// When the template is applied
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            searchTextBox = GetTemplateChild("PART_SearchTextBox") as TextBox;
            if (searchTextBox != null)
            {
                searchTextBox.TextChanged += OnSearchTextBoxTextChanged;
                searchTextBox.KeyDown += OnSearchTextBoxKeyDown;
            }
            
            advancedFilterButton = GetTemplateChild("PART_AdvancedFilterButton") as Button;
            if (advancedFilterButton != null)
            {
                advancedFilterButton.Click += OnAdvancedFilterButtonClick;
            }
        }

        #endregion

        #region Event Handlers

        private void OnControlLoaded(object sender, RoutedEventArgs e) => InitializeSearchTemplateController();

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            // Clean up timer when control is unloaded
            if (_changeTimer != null)
            {
                _changeTimer.Stop();
                _changeTimer.Elapsed -= OnChangeTimerElapsed;
                _changeTimer.Dispose();
                _changeTimer = null;
            }

            // Clean up temporary template reference
            _temporarySearchTemplate = null;

            // Clean up textbox event handlers
            if (searchTextBox != null)
            {
                searchTextBox.TextChanged -= OnSearchTextBoxTextChanged;
                searchTextBox.KeyDown -= OnSearchTextBoxKeyDown;
            }
            
            // Clean up container event handlers
            LostFocus -= OnColumnSearchBoxLostFocus;
            GotFocus -= OnColumnSearchBoxGotFocus;

            if (SourceDataGrid != null)
            {
                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
            }

            // Close and clean up the filter window if it's open
            if (advancedFilterWindow != null)
            {
                CloseAdvancedFilterWindow(false);
            }
        }

        private static void OnSourceDataGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnSearchBox control) return;

            // Unregister events from old grid
            if (e.OldValue is SearchDataGrid oldGrid)
            {
                oldGrid.CollectionChanged -= control.OnSourceDataGridCollectionChanged;
            }

            // Register events with new grid and initialize
            control.InitializeSearchTemplateController();
        }

        private DispatcherTimer _availableValuesUpdateTimer;

        private void OnSourceDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            void ApplyAvailableValuesUpdate(object sender, EventArgs e)
            {
                _availableValuesUpdateTimer?.Stop();

                // Check if SearchTemplateController is valid before updating
                if (SearchTemplateController != null)
                {
                    foreach (var group in SearchTemplateController.SearchGroups)
                    {
                        foreach (var template in group.SearchTemplates)
                        {
                            template.LoadAvailableValues(SearchTemplateController.ColumnValues);
                        }
                    }
                }
            }

            // Check if we have valid data to process
            if (string.IsNullOrEmpty(BindingPath) || SearchTemplateController == null)
                return;

            // Handle different collection changes
            try
            {
                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var value = ReflectionHelper.GetPropValue(item, BindingPath);
                        SearchTemplateController.ColumnValues.Add(value);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var value = ReflectionHelper.GetPropValue(item, BindingPath);
                        SearchTemplateController.ColumnValues.Remove(value);
                    }
                }
                else
                {
                    // For Replace, Reset, or Move, re-evaluate all values
                    LoadColumnValues();
                }

                // Delay full UI update to batch frequent changes
                if (_availableValuesUpdateTimer == null)
                {
                    _availableValuesUpdateTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(150),
                        IsEnabled = false
                    };
                }

                _availableValuesUpdateTimer.Tick -= ApplyAvailableValuesUpdate;
                _availableValuesUpdateTimer.Tick += ApplyAvailableValuesUpdate;
                _availableValuesUpdateTimer.Stop();
                _availableValuesUpdateTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSourceDataGridCollectionChanged: {ex.Message}");
            }
        }

        private static void OnCurrentColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnSearchBox control && e.NewValue is DataGridColumn column)
            {
                control.BindingPath = column.SortMemberPath;
                control.ShowInAdvancedFilter = GetShowInAdvancedFilter(control.CurrentColumn);
                control.InitializeSearchTemplateController();
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnSearchBox control && !control.isAdvancedFilterOpen)
            {
                // If text is empty, clear the filter immediately
                if (string.IsNullOrWhiteSpace((string)e.NewValue))
                {
                    //control.ClearFilterInternal();
                }
                else
                {
                    // Otherwise use the timer for debouncing
                    control.StartOrResetChangeTimer();
                }
            }
        }

        private void OnSearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isAdvancedFilterOpen)
                SearchText = searchTextBox.Text;
        }

        private void OnSearchTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                ClearSearchTextAndTemporaryFilter();
            else if (e.Key == Key.Enter)
            {
                _changeTimer?.Stop();

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    //ClearFilterInternal();
                }
                else
                {
                    // NEW: Add incremental filter and clear textbox
                    AddIncrementalContainsFilter();
                    ClearSearchTextOnly();
                }
            }
        }



        private void OnAdvancedFilterButtonClick(object sender, RoutedEventArgs e) => ShowInAdvancedFilterWindow();

        private void OnColumnSearchBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // Redirect focus to the textbox when the container gets focus
            if (searchTextBox != null && !searchTextBox.IsFocused)
            {
                searchTextBox.Focus();
                e.Handled = true;
            }
        }
        
        private void OnColumnSearchBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Only confirm filter if focus is leaving the entire ColumnSearchBox container
            // Use a small delay to allow focus to settle before checking
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var focusedElement = Keyboard.FocusedElement as DependencyObject;
                if (focusedElement != null)
                {
                    // Check if the newly focused element is within this ColumnSearchBox
                    var isWithinSearchBox = IsWithinSearchBox(focusedElement);
                    if (isWithinSearchBox)
                    {
                        // Focus is still within this ColumnSearchBox, don't confirm filter
                        return;
                    }
                }
                
                // Focus has left the ColumnSearchBox entirely, confirm any temporary filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    _changeTimer?.Stop();
                    AddIncrementalContainsFilter();
                    ClearSearchTextOnly();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        
        private bool IsWithinSearchBox(DependencyObject element)
        {
            try
            {
                while (element != null)
                {
                    if (element == this)
                        return true;
                    element = VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void OnChangeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Execute on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!isAdvancedFilterOpen)
                {
                    if (!string.IsNullOrWhiteSpace(SearchText))
                        UpdateSimpleFilter();
                }
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Starts or resets the debounce timer for search changes
        /// </summary>
        private void StartOrResetChangeTimer()
        {
            if (_changeTimer == null)
            {
                _changeTimer = new Timer(250)
                {
                    AutoReset = false
                };
                _changeTimer.Elapsed += OnChangeTimerElapsed;
            }

            _changeTimer.Stop();
            _changeTimer.Start();
        }

        /// <summary>
        /// Initializes the search template controller
        /// </summary>
        private void InitializeSearchTemplateController()
        {
            try
            {
                // Skip if we don't have the required data yet
                if (SourceDataGrid == null || CurrentColumn == null)
                    return;

                // Create a controller if none exists
                if (SearchTemplateController == null)
                {
                    var type = GetCustomSearchTemplate(CurrentColumn) ?? typeof(SearchTemplate);
                    SearchTemplateController = new SearchTemplateController(type);
                }

                // Set column properties
                SearchTemplateController.ColumnName = CurrentColumn.Header;
                BindingPath = CurrentColumn.SortMemberPath;
                
                // Set default search type from column's attached property
                var defaultSearchType = RuleValueFilterEditor.GetDefaultSearchType(CurrentColumn);
                if (defaultSearchType != SearchType.Contains) // Only set if different from default
                {
                    SearchTemplateController.DefaultSearchType = defaultSearchType;
                }

                // Add this control to the data grid's columns if not already there
                if (!SourceDataGrid.DataColumns.Contains(this))
                    SourceDataGrid.DataColumns.Add(this);

                // Hook into collection changed events
                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                SourceDataGrid.CollectionChanged += OnSourceDataGridCollectionChanged;

                // Initial load if we have items
                if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                    LoadColumnValues();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeSearchTemplateController: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an incremental Contains filter with OR logic
        /// </summary>
        private void AddIncrementalContainsFilter()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                // Ensure we have at least one search group
                if (SearchTemplateController.SearchGroups.Count == 0)
                {
                    SearchTemplateController.AddSearchGroup();
                }

                var firstGroup = SearchTemplateController.SearchGroups[0];
                
                // Remove temporary template if it exists
                if (_temporarySearchTemplate != null)
                {
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                }
                
                // Remove any default empty templates before adding our Contains template
                RemoveDefaultEmptyTemplates(firstGroup);
                
                // Check for existing confirmed Contains templates in the first search group
                var existingContainsTemplates = firstGroup.SearchTemplates
                    .Where(t => t.SearchType == SearchType.Contains && t.HasCustomFilter)
                    .ToList();

                // Create new confirmed Contains template
                var newTemplate = new SearchTemplate(SearchTemplateController.ColumnValues, SearchTemplateController.ColumnDataType);
                newTemplate.SearchType = SearchType.Contains;
                newTemplate.SelectedValue = SearchText;
                
                // If this is not the first template, set OR operator
                if (existingContainsTemplates.Any())
                {
                    newTemplate.OperatorName = "OR";
                }
                
                firstGroup.SearchTemplates.Add(newTemplate);

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();

                // Apply the filter to the grid
                SourceDataGrid.FilterItemsSource();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddIncrementalContainsFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the search textbox only, preserving existing filters
        /// </summary>
        private void ClearSearchTextOnly()
        {
            try
            {
                SearchText = string.Empty;
                if (searchTextBox != null)
                    searchTextBox.Text = string.Empty;

                HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearSearchTextOnly: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the search text and removes only the temporary template (not confirmed filters)
        /// This is used by the X button in the search box
        /// </summary>
        private void ClearSearchTextAndTemporaryFilter()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                // Clear the search text
                SearchText = string.Empty;
                if (searchTextBox != null)
                    searchTextBox.Text = string.Empty;

                // Remove only the temporary template if it exists
                if (_temporarySearchTemplate != null && SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    var firstGroup = SearchTemplateController.SearchGroups[0];
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                    
                    // Update the filter expression and apply to grid
                    SearchTemplateController.UpdateFilterExpression();
                    SourceDataGrid?.FilterItemsSource();
                }

                // Update HasAdvancedFilter state
                HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                
                // Update filter panel
                SourceDataGrid?.UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the filter with the simple text search (used for debounced/timer-based updates)
        /// This method creates/updates a temporary template for real-time preview
        /// </summary>
        private void UpdateSimpleFilter()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    // Ensure we have a search group
                    if (SearchTemplateController.SearchGroups.Count == 0)
                    {
                        SearchTemplateController.AddSearchGroup();
                    }

                    var firstGroup = SearchTemplateController.SearchGroups[0];
                    
                    // Remove any default empty templates before adding our Contains template
                    RemoveDefaultEmptyTemplates(firstGroup);
                    
                    // Update existing temporary template or create new one
                    if (_temporarySearchTemplate != null)
                    {
                        // Update existing temporary template
                        _temporarySearchTemplate.SelectedValue = SearchText;
                    }
                    else
                    {
                        // Create new temporary template
                        _temporarySearchTemplate = new SearchTemplate(SearchTemplateController.ColumnValues, SearchTemplateController.ColumnDataType);
                        _temporarySearchTemplate.SearchType = SearchType.Contains;
                        _temporarySearchTemplate.SelectedValue = SearchText;
                        
                        // Check if we have existing confirmed Contains templates
                        var existingContainsTemplates = firstGroup.SearchTemplates
                            .Where(t => t.SearchType == SearchType.Contains && t.HasCustomFilter)
                            .ToList();
                        
                        // If this is not the first template, set OR operator
                        if (existingContainsTemplates.Any())
                        {
                            _temporarySearchTemplate.OperatorName = "OR";
                        }
                        
                        firstGroup.SearchTemplates.Add(_temporarySearchTemplate);
                    }

                    // Update the filter expression
                    SearchTemplateController.UpdateFilterExpression();

                    // Apply the filter to the grid
                    SourceDataGrid.FilterItemsSource();
                }
                else
                {
                    // Clear temporary template when search text is empty
                    if (_temporarySearchTemplate != null)
                    {
                        var firstGroup = SearchTemplateController.SearchGroups[0];
                        firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                        _temporarySearchTemplate = null;
                        
                        // Update the filter expression
                        SearchTemplateController.UpdateFilterExpression();
                        
                        // Apply the filter to the grid
                        SourceDataGrid.FilterItemsSource();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSimpleFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes default empty templates that get automatically added by the framework
        /// </summary>
        /// <param name="group">The search group to clean up</param>
        private void RemoveDefaultEmptyTemplates(SearchTemplateGroup group)
        {
            try
            {
                // Remove templates that are empty/default and not our specific Contains templates
                var templatesToRemove = group.SearchTemplates
                    .Where(t => t != _temporarySearchTemplate && !t.HasCustomFilter)
                    .ToList();

                foreach (var template in templatesToRemove)
                {
                    group.SearchTemplates.Remove(template);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RemoveDefaultEmptyTemplates: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal implementation of clear filter
        /// </summary>
        private void ClearFilterInternal()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null)
                    return;

                // Clear temporary template reference
                _temporarySearchTemplate = null;

                // Clear the search template groups and add a default one back
                SearchTemplateController.ClearAndReset();

                HasAdvancedFilter = false;

                // Clear any data transformations for this column
                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath))
                {
                    SourceDataGrid.ClearDataTransformation(BindingPath);
                }

                // Apply the updated (empty) filter to the grid
                SourceDataGrid?.FilterItemsSource();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearFilterInternal: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the advanced filter window
        /// </summary>
        private void ShowInAdvancedFilterWindow()
        {
            try
            {
                // Skip if source data grid is not available or we're in global mode
                if (SourceDataGrid == null)
                    return;

                // Skip if controller is not available
                if (SearchTemplateController == null)
                    InitializeSearchTemplateController();

                if (SearchTemplateController == null)
                    return;

                // Create window if none exists
                if (advancedFilterWindow == null)
                {
                    advancedFilterWindow = new Window
                    {
                        Title = $"Advanced Filter: {CurrentColumn.Header}",
                        MinWidth = 400,
                        MinHeight = 600,
                        SizeToContent = SizeToContent.Width,
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    var filterControl = new RuleValueFilterEditor
                    {
                        SearchTemplateController = SearchTemplateController,
                        DataContext = this
                    };

                    advancedFilterWindow.Content = filterControl;
                    advancedFilterWindow.Closed += (_, _) => OnAdvancedFilterWindowClosed();
                }

                isAdvancedFilterOpen = true;
                advancedFilterWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowInAdvancedFilterWindow: {ex.Message}");
                CloseAdvancedFilterWindow(false);
            }
        }

        /// <summary>
        /// Handles the advanced filter window being closed
        /// </summary>
        private void OnAdvancedFilterWindowClosed()
        {
            try
            {
                isAdvancedFilterOpen = false;

                // Check if the controller is still valid
                if (SearchTemplateController != null)
                {
                    HasAdvancedFilter = SearchTemplateController.HasCustomExpression;

                    // Process any data transformation filters
                    SourceDataGrid?.ProcessTransformationFilter(this);
                }

                advancedFilterWindow = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnAdvancedFilterWindowClosed: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the advanced filter window
        /// </summary>
        private void CloseAdvancedFilterWindow(bool updateFilters)
        {
            try
            {
                if (advancedFilterWindow != null)
                {
                    // Remove the closed event handler to prevent actions during close
                    if (advancedFilterWindow is Window window)
                    {
                        foreach (var handler in window.GetClosedEventHandlers())
                        {
                            window.Closed -= handler;
                        }
                    }

                    // Update state before closing
                    isAdvancedFilterOpen = false;

                    if (updateFilters && SearchTemplateController != null)
                    {
                        HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                    }

                    // Close the window
                    advancedFilterWindow.Close();
                    advancedFilterWindow = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CloseAdvancedFilterWindow: {ex.Message}");
                advancedFilterWindow = null;
                isAdvancedFilterOpen = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads column values for filtering
        /// </summary>
        public void LoadColumnValues()
        {
            try
            {
                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath) && SearchTemplateController != null)
                {
                    var values = new HashSet<object>();
                    foreach (var item in SourceDataGrid.Items)
                    {
                        var val = ReflectionHelper.GetPropValue(item, BindingPath);
                        values.Add(val);
                    }
                    SearchTemplateController.LoadColumnData(CurrentColumn.Header, values);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadColumnValues: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the current filter
        /// </summary>
        public void ClearFilter()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                // Clear the search text
                SearchText = string.Empty;
                if (searchTextBox != null)
                    searchTextBox.Text = string.Empty;

                // Clear the filter internally
                ClearFilterInternal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a display text for the current filter state
        /// </summary>
        /// <returns>Human-readable description of the current filter</returns>
        public string GetFilterDisplayText()
        {
            try
            {
                // Check if we have a simple text filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    return $"Contains '{SearchText}'";
                }

                // Check if we have an advanced filter
                if (SearchTemplateController?.HasCustomExpression == true)
                {
                    // Get summary from SearchTemplateController
                    return SearchTemplateController.GetFilterDisplayText();
                }

                return "No filter";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetFilterDisplayText: {ex.Message}");
                return "Filter error";
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for window event handlers
    /// </summary>
    public static class WindowExtensions
    {
        /// <summary>
        /// Gets the Closed event handlers for a window
        /// </summary>
        public static IEnumerable<EventHandler> GetClosedEventHandlers(this Window window)
        {
            // Using reflection to get event handlers is generally not recommended,
            // but we need it for cleanup to prevent issues when closing windows
            var eventField = typeof(Window).GetField("Closed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            if (eventField == null)
                return Array.Empty<EventHandler>();


            if (eventField.GetValue(window) is not Delegate eventDelegate)
                return Array.Empty<EventHandler>();

            return eventDelegate.GetInvocationList().Cast<EventHandler>();
        }
    }
}