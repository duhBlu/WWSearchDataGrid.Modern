using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Search control for filtering data grid columns
    /// </summary>
    public class SearchControl : Control
    {
        #region Fields

        private TextBox searchTextBox;
        private Button advancedFilterButton;
        private Window advancedFilterWindow;
        private bool isAdvancedFilterOpen;
        private Timer _changeTimer;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentColumnProperty =
            DependencyProperty.Register("CurrentColumn", typeof(DataGridColumn), typeof(SearchControl),
                new PropertyMetadata(null, OnCurrentColumnChanged));

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register("SourceDataGrid", typeof(SearchDataGrid), typeof(SearchControl),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(SearchControl),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public static readonly DependencyProperty HasAdvancedFilterProperty =
            DependencyProperty.Register("HasAdvancedFilter", typeof(bool), typeof(SearchControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty CustomSearchTemplateProperty =
            DependencyProperty.RegisterAttached("CustomSearchTemplate", typeof(Type), typeof(SearchControl),
                new FrameworkPropertyMetadata(typeof(SearchTemplate)));

        public static readonly DependencyProperty ShowInAdvancedFilterProperty =
            DependencyProperty.RegisterAttached("ShowInAdvancedFilter", typeof(bool), typeof(SearchControl),
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
        /// Gets the command to clear search text
        /// </summary>
        public ICommand ClearSearchTextCommand => new RelayCommand(_ => ClearFilter());

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
        /// Initializes a new instance of the SearchControl class
        /// </summary>
        public SearchControl()
        {
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
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
            var control = d as SearchControl;
            if (control == null) return;

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
            if (d is SearchControl control && e.NewValue is DataGridColumn column)
            {
                control.BindingPath = column.SortMemberPath;
                control.ShowInAdvancedFilter = GetShowInAdvancedFilter(control.CurrentColumn);
                control.InitializeSearchTemplateController();
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchControl control && !control.isAdvancedFilterOpen)
            {
                // If text is empty, clear the filter immediately
                if (string.IsNullOrWhiteSpace((string)e.NewValue))
                {
                    control.ClearFilterInternal();
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
                ClearFilter();
            else if (e.Key == Key.Enter)
            {
                if (_changeTimer != null)
                    _changeTimer.Stop();

                if (string.IsNullOrWhiteSpace(SearchText))
                    ClearFilterInternal();
                else
                    UpdateSimpleFilter();
            }
        }

        private void OnAdvancedFilterButtonClick(object sender, RoutedEventArgs e) => ShowInAdvancedFilterWindow();

        private void OnChangeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Execute on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!isAdvancedFilterOpen)
                {
                    if (string.IsNullOrWhiteSpace(SearchText))
                        ClearFilterInternal();
                    else
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
                _changeTimer = new Timer(250);
                _changeTimer.AutoReset = false;
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
                var defaultSearchType = AdvancedFilterControl.GetDefaultSearchType(CurrentColumn);
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
        /// Updates the filter with the simple text search
        /// </summary>
        private void UpdateSimpleFilter()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                // Clear existing search templates
                SearchTemplateController.SearchGroups.Clear();

                // Add a new search group
                SearchTemplateController.AddSearchGroup();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    // Create and configure the search template
                    var template = SearchTemplateController.SearchGroups[0].SearchTemplates[0];
                    template.SearchType = SearchType.Contains;
                    template.SelectedValue = SearchText;

                    // Update the filter expression
                    SearchTemplateController.UpdateFilterExpression();

                    // Apply the filter to the grid
                    SourceDataGrid.FilterItemsSource();
                }
                else
                {
                    ClearFilterInternal();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSimpleFilter: {ex.Message}");
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

                    var filterControl = new AdvancedFilterControl
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
                    if (SourceDataGrid != null)
                    {
                        SourceDataGrid.ProcessTransformationFilter(this);
                    }

                    // Adjust column width if a filter is applied
                    if (HasAdvancedFilter && CurrentColumn != null)
                    {
                        CurrentColumn.Width = DataGridLength.Auto;
                    }
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
                if (_changeTimer != null)
                    _changeTimer.Stop();

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

            var eventDelegate = eventField.GetValue(window) as Delegate;

            if (eventDelegate == null)
                return Array.Empty<EventHandler>();

            return eventDelegate.GetInvocationList().Cast<EventHandler>();
        }
    }
}