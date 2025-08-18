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
    /// Defines the logical states for checkbox cycling
    /// </summary>
    public enum CheckboxCycleState
    {
        /// <summary>
        /// Shows all data (no filter) - initial state and manual clear state
        /// </summary>
        Intermediate,
        
        /// <summary>
        /// Shows only true values
        /// </summary>
        Checked,
        
        /// <summary>
        /// Shows only false values  
        /// </summary>
        Unchecked
    }

    /// <summary>
    /// Search control for filtering data grid columns
    /// </summary>
    public class ColumnSearchBox : Control
    {
        #region Fields

        private TextBox searchTextBox;
        private Button advancedFilterButton;
        private CheckBox filterCheckBox;
        private Window advancedFilterWindow;
        private bool isAdvancedFilterOpen;
        private Timer _changeTimer;
        private SearchTemplate _temporarySearchTemplate; // Track temporary template for real-time updates
        private CheckboxCycleState _checkboxCycleState = CheckboxCycleState.Intermediate; // Current logical cycling state
        private bool _isInitialState = true; // Tracks if we're in the initial uncycled state

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

        public static readonly DependencyProperty AllowRuleValueFilteringProperty =
            DependencyProperty.RegisterAttached("AllowRuleValueFiltering", typeof(bool), typeof(ColumnSearchBox),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty FilterCheckboxStateProperty =
            DependencyProperty.Register("FilterCheckboxState", typeof(bool?), typeof(ColumnSearchBox),
                new PropertyMetadata(null, OnFilterCheckboxStateChanged));

        public static readonly DependencyProperty IsCheckboxColumnProperty =
            DependencyProperty.Register("IsCheckboxColumn", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty AllowsNullValuesProperty =
            DependencyProperty.Register("AllowsNullValues", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.Register("HasActiveFilter", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

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
        public bool AllowRuleValueFiltering
        {
            get => (bool)GetValue(AllowRuleValueFilteringProperty);
            set => SetValue(AllowRuleValueFilteringProperty, value);
        }

        /// <summary>
        /// Gets or sets the checkbox filter state (null = indeterminate, true = checked, false = unchecked)
        /// </summary>
        public bool? FilterCheckboxState
        {
            get => (bool?)GetValue(FilterCheckboxStateProperty);
            set => SetValue(FilterCheckboxStateProperty, value);
        }

        /// <summary>
        /// Gets whether this column should show checkbox filtering instead of text search
        /// </summary>
        public bool IsCheckboxColumn
        {
            get => (bool)GetValue(IsCheckboxColumnProperty);
            private set => SetValue(IsCheckboxColumnProperty, value);
        }

        /// <summary>
        /// Gets whether the column allows null values (affects cycling behavior)
        /// </summary>
        public bool AllowsNullValues
        {
            get => (bool)GetValue(AllowsNullValuesProperty);
            private set => SetValue(AllowsNullValuesProperty, value);
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
            get => (bool)GetValue(HasActiveFilterProperty);
            private set => SetValue(HasActiveFilterProperty, value);
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
            //Focusable = false;
            
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
        public static void SetAllowRuleValueFiltering(DependencyObject element, bool value) =>
            element.SetValue(AllowRuleValueFilteringProperty, value);

        /// <summary>
        /// Gets whether to show the advanced filter
        /// </summary>
        public static bool GetAllowRuleValueFiltering(DependencyObject element) =>
            (bool)element.GetValue(AllowRuleValueFilteringProperty);

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

            filterCheckBox = GetTemplateChild("PART_FilterCheckBox") as CheckBox;
            if (filterCheckBox != null)
            {
                // Use proactive event handling instead of reactive state change events
                filterCheckBox.PreviewKeyDown += OnCheckboxPreviewKeyDown;
                filterCheckBox.PreviewMouseDown += OnCheckboxPreviewMouseDown;
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

            // Clean up checkbox event handlers
            if (filterCheckBox != null)
            {
                filterCheckBox.PreviewKeyDown -= OnCheckboxPreviewKeyDown;
                filterCheckBox.PreviewMouseDown -= OnCheckboxPreviewMouseDown;
            }
            
            // Clean up container event handlers
            LostFocus -= OnColumnSearchBoxLostFocus;
            GotFocus -= OnColumnSearchBoxGotFocus;

            if (SourceDataGrid != null)
            {
                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                SourceDataGrid.ItemsSourceChanged -= OnSourceDataGridItemsSourceChanged;
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
                oldGrid.ItemsSourceChanged -= control.OnSourceDataGridItemsSourceChanged;
            }

            // Register events with new grid and initialize
            if (control.SourceDataGrid != null)
            {
                control.SourceDataGrid.ItemsSourceChanged += control.OnSourceDataGridItemsSourceChanged;
            }
            
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
                    // The SearchTemplateController will handle updating all templates with the provider
                    // No need to update individual templates here
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
                    var wasEmpty = SearchTemplateController.ColumnValues.Count == 0;
                    
                    foreach (var item in e.NewItems)
                    {
                        var value = ReflectionHelper.GetPropValue(item, BindingPath);
                        SearchTemplateController.ColumnValues.Add(value);
                    }
                    
                    // If the collection was previously empty, recheck checkbox settings
                    if (wasEmpty && SearchTemplateController.ColumnValues.Count > 0)
                    {
                        DetermineCheckboxColumnSettings();
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
                    
                    // For Reset (which happens when ItemsSource changes), recheck checkbox settings
                    if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        // Delay the checkbox settings check to ensure data is loaded
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (SearchTemplateController != null && SearchTemplateController.ColumnValues.Count > 0)
                            {
                                DetermineCheckboxColumnSettings();
                            }
                        }), DispatcherPriority.Background);
                    }
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
                control.AllowRuleValueFiltering = GetAllowRuleValueFiltering(control.CurrentColumn);
                control.InitializeSearchTemplateController();
            }
        }

        private static void OnFilterCheckboxStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnSearchBox control)
            {
                control.OnCheckboxFilterChanged();
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnSearchBox control && !control.isAdvancedFilterOpen)
            {
                // If text is empty, clear only the temporary template
                if (string.IsNullOrWhiteSpace((string)e.NewValue))
                {
                    control.ClearTemporaryTemplate();
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
            else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;                         // stop DataGrid from stealing it
                var req = new TraversalRequest(FocusNavigationDirection.Previous);
                // move focus *from* the ColumnSearchBox to its previous peer
                (this as UIElement).MoveFocus(req);
            }
        }

        private void OnAdvancedFilterButtonClick(object sender, RoutedEventArgs e) => AllowRuleValueFilteringWindow();

        private void OnColumnSearchBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // Redirect focus to the textbox when the container gets focus
            if (e.OriginalSource == this && searchTextBox != null && !searchTextBox.IsFocused)
            {
                searchTextBox.Focus();
                e.Handled = true;
            }
        }
        
        private void OnSourceDataGridItemsSourceChanged(object sender, EventArgs e)
        {
            // When the ItemsSource changes completely, we need to reload column values and recheck checkbox settings
            try
            {
                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath) && SearchTemplateController != null)
                {
                    // Delay the update to ensure the data has been loaded into the grid
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LoadColumnValues();
                    }), DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSourceDataGridItemsSourceChanged: {ex.Message}");
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

        #region Checkbox Event Handlers

        /// <summary>
        /// Handles preview key events on the checkbox to intercept cycling before native behavior
        /// </summary>
        private void OnCheckboxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!IsCheckboxColumn)
                    return;

                // Handle Space and Enter key presses
                if (e.Key == Key.Space || e.Key == Key.Enter)
                {
                    // Cycle forward and prevent native checkbox behavior
                    CycleCheckboxStateForward();
                    
                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();
                    
                    // Mark as handled to prevent native cycling
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnCheckboxPreviewKeyDown: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles preview mouse events on the checkbox to intercept cycling before native behavior
        /// </summary>
        private void OnCheckboxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!IsCheckboxColumn)
                    return;

                // Handle left mouse button clicks
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Cycle forward and prevent native checkbox behavior
                    CycleCheckboxStateForward();
                    
                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();
                    
                    // Mark as handled to prevent native cycling
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnCheckboxPreviewMouseDown: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles changes to the FilterCheckboxState dependency property
        /// This is primarily used for external updates and synchronization
        /// </summary>
        private void OnCheckboxFilterChanged()
        {
            try
            {
                if (!IsCheckboxColumn)
                    return;

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnCheckboxFilterChanged: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the HasActiveFilter property based on current filter state
        /// </summary>
        private void UpdateHasActiveFilterState()
        {
            bool hasFilter = false;

            if (SearchTemplateController != null)
            {
                // Check if we have a checkbox filter (including IsEmpty filter for indeterminate state)
                if (IsCheckboxColumn)
                {
                    if (FilterCheckboxState.HasValue)
                    {
                        hasFilter = true;
                    }
                    else if (!FilterCheckboxState.HasValue && SearchTemplateController.HasCustomExpression)
                    {
                        // Check if indeterminate state has an IsEmpty filter
                        var firstGroup = SearchTemplateController.SearchGroups.FirstOrDefault();
                        var firstTemplate = firstGroup?.SearchTemplates.FirstOrDefault();
                        if (firstTemplate?.SearchType == SearchType.IsNull)
                            hasFilter = true;
                    }
                }

                // Check if we have a simple text filter
                if (!hasFilter && !string.IsNullOrWhiteSpace(SearchText))
                {
                    hasFilter = true;
                }

                // Check if we have an advanced filter
                if (!hasFilter)
                {
                    hasFilter = SearchTemplateController.HasCustomExpression;
                }
            }

            HasActiveFilter = hasFilter;
        }

        /// <summary>
        /// Determines if this column should use checkbox filtering and sets related properties
        /// </summary>
        private void DetermineCheckboxColumnSettings()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null)
                {
                    IsCheckboxColumn = false;
                    AllowsNullValues = false;
                    return;
                }

                var previousIsCheckboxColumn = IsCheckboxColumn;

                // Check if column data type is Boolean
                var isCheckboxType = SearchTemplateController.ColumnDataType == ColumnDataType.Boolean;

                // If not explicitly Boolean, check if all non-null values are booleans
                if (!isCheckboxType && SearchTemplateController.ColumnValues.Count > 0)
                {
                    var nonNullValues = SearchTemplateController.ColumnValues.Where(v => v != null).ToList();
                    if (nonNullValues.Count > 0)
                    {
                        isCheckboxType = nonNullValues.All(v => v is bool);
                    }
                }

                IsCheckboxColumn = isCheckboxType;

                // Determine if null values are present
                if (IsCheckboxColumn)
                {
                    AllowsNullValues = SearchTemplateController.ColumnValues.Contains(null);
                    
                }
                else
                {
                    AllowsNullValues = false;
                }

                // If the column type changed, reset any existing filters
                if (previousIsCheckboxColumn != IsCheckboxColumn)
                {
                    // Clear any existing filters since the UI mode is changing
                    if (previousIsCheckboxColumn)
                    {
                        // Was checkbox, now text - clear checkbox state
                        FilterCheckboxState = null;
                        if (filterCheckBox != null)
                            filterCheckBox.IsChecked = null;
                    }
                    else
                    {
                        // Was text, now checkbox - clear text search
                        SearchText = string.Empty;
                        if (searchTextBox != null)
                            searchTextBox.Text = string.Empty;
                    }
                    
                    // Clear any active filters
                    ClearFilterInternal();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DetermineCheckboxColumnSettings: {ex.Message}");
                IsCheckboxColumn = false;
                AllowsNullValues = false;
            }
        }

        /// <summary>
        /// Cycles the checkbox state forward based on current state and column properties
        /// </summary>
        private void CycleCheckboxStateForward()
        {
            try
            {
                _isInitialState = false; // We're now cycling, not in initial state
                var nextState = GetNextCycleState(_checkboxCycleState, AllowsNullValues);
                SetCheckboxCycleState(nextState);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CycleCheckboxStateForward: {ex.Message}");
                // Fallback to safe state
                ResetCheckboxToInitialState();
            }
        }

        /// <summary>
        /// Resets the checkbox to the initial intermediate state (no filter)
        /// </summary>
        private void ResetCheckboxToInitialState()
        {
            try
            {
                _isInitialState = true;
                SetCheckboxCycleState(CheckboxCycleState.Intermediate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ResetCheckboxToInitialState: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the checkbox cycle state programmatically
        /// </summary>
        /// <param name="state">The state to set</param>
        private void SetCheckboxCycleState(CheckboxCycleState state)
        {
            try
            {
                _checkboxCycleState = state;
                
                // Update the visual checkbox state
                UpdateVisualCheckboxState(state);
                
                // Apply the appropriate filter
                ApplyCheckboxCycleFilter(state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetCheckboxCycleState: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the next state in the cycling sequence based on current state and column capabilities
        /// </summary>
        /// <param name="currentState">Current cycling state</param>
        /// <param name="allowsNullValues">Whether the column contains null values</param>
        /// <returns>The next state in the cycle</returns>
        private CheckboxCycleState GetNextCycleState(CheckboxCycleState currentState, bool allowsNullValues)
        {
            if (allowsNullValues)
            {
                // Columns WITH null values: Intermediate → Checked → Unchecked → Intermediate (cycle back for null filtering)
                return currentState switch
                {
                    CheckboxCycleState.Intermediate => CheckboxCycleState.Checked,
                    CheckboxCycleState.Checked => CheckboxCycleState.Unchecked,
                    CheckboxCycleState.Unchecked => CheckboxCycleState.Intermediate,
                    _ => CheckboxCycleState.Intermediate
                };
            }
            else
            {
                // Columns WITHOUT null values: Intermediate → Checked → Unchecked → Checked → Unchecked (skip intermediate)
                return currentState switch
                {
                    CheckboxCycleState.Intermediate => CheckboxCycleState.Checked,
                    CheckboxCycleState.Checked => CheckboxCycleState.Unchecked,
                    CheckboxCycleState.Unchecked => CheckboxCycleState.Checked,
                    _ => CheckboxCycleState.Checked
                };
            }
        }

        /// <summary>
        /// Updates the visual checkbox state to match the logical state
        /// </summary>
        /// <param name="state">The logical state</param>
        private void UpdateVisualCheckboxState(CheckboxCycleState state)
        {
            // Convert logical state to nullable bool for FilterCheckboxState property
            bool? checkboxValue = state switch
            {
                CheckboxCycleState.Intermediate => null,
                CheckboxCycleState.Checked => true,
                CheckboxCycleState.Unchecked => false,
                _ => null
            };

            // Update the dependency property (this will sync with the visual checkbox)
            FilterCheckboxState = checkboxValue;
        }

        /// <summary>
        /// Applies the appropriate filter based on the current cycle state
        /// </summary>
        /// <param name="state">The state to apply filter for</param>
        private void ApplyCheckboxCycleFilter(CheckboxCycleState state)
        {
            switch (state)
            {
                case CheckboxCycleState.Intermediate:
                    if (_isInitialState || !AllowsNullValues)
                    {
                        // Initial state or non-nullable columns: clear all filters
                        ClearFilterInternal();
                    }
                    else
                    {
                        // For nullable columns in intermediate state after cycling, show only null values
                        ApplyCheckboxIsNullFilter();
                    }
                    break;

                case CheckboxCycleState.Checked:
                    ApplyCheckboxBooleanFilter(true);
                    break;

                case CheckboxCycleState.Unchecked:
                    ApplyCheckboxBooleanFilter(false);
                    break;

                default:
                    ClearFilterInternal();
                    break;
            }

            // Update the data grid
            SourceDataGrid?.FilterItemsSource();
            SourceDataGrid?.UpdateFilterPanel();
        }

        /// <summary>
        /// Applies a boolean equals filter
        /// </summary>
        /// <param name="value">The boolean value to filter for</param>
        private void ApplyCheckboxBooleanFilter(bool value)
        {
            try
            {
                if (SearchTemplateController == null) return;

                // Clear existing groups
                SearchTemplateController.SearchGroups.Clear();
                
                // Create a new search group
                var group = new SearchTemplateGroup();
                SearchTemplateController.SearchGroups.Add(group);

                // Create Equals template for the boolean value
                var template = new SearchTemplate(ColumnDataType.Boolean);
                template.SearchType = SearchType.Equals;
                template.SelectedValue = value;

                // Add the template
                group.SearchTemplates.Add(template);

                // Clear any data transformations for this column
                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath))
                {
                    SourceDataGrid.ClearDataTransformation(BindingPath);
                }

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyCheckboxBooleanFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies an IsNull filter for showing only null values
        /// </summary>
        private void ApplyCheckboxIsNullFilter()
        {
            try
            {
                if (SearchTemplateController == null) return;

                // Clear existing groups
                SearchTemplateController.SearchGroups.Clear();
                
                // Create a new search group
                var group = new SearchTemplateGroup();
                SearchTemplateController.SearchGroups.Add(group);

                // Create IsNull template for null values
                var template = new SearchTemplate(ColumnDataType.Boolean);
                template.SearchType = SearchType.IsNull;
                
                // Add the template
                group.SearchTemplates.Add(template);

                // Clear any data transformations for this column
                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath))
                {
                    SourceDataGrid.ClearDataTransformation(BindingPath);
                }

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyCheckboxIsNullFilter: {ex.Message}");
            }
        }

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
                var defaultSearchType = ColumnFilterEditor.GetDefaultSearchType(CurrentColumn);
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

                // Hook into items source changed events for initial loading detection
                SourceDataGrid.ItemsSourceChanged -= OnSourceDataGridItemsSourceChanged;
                SourceDataGrid.ItemsSourceChanged += OnSourceDataGridItemsSourceChanged;

                // Initial load if we have items
                if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                {
                    LoadColumnValues();
                    
                    // Determine checkbox column settings after loading values
                    DetermineCheckboxColumnSettings();
                    
                }
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
                var newTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType);
                newTemplate.SearchType = SearchType.Contains;
                newTemplate.SelectedValue = SearchText;
                
                // If this is not the first template, set OR operator
                if (existingContainsTemplates.Any())
                {
                    newTemplate.OperatorName = "Or";
                }
                
                firstGroup.SearchTemplates.Add(newTemplate);

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();

                // Apply the filter to the grid
                SourceDataGrid.FilterItemsSource();

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
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

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearSearchTextOnly: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the search text and removes only the temporary template (not confirmed filters)
        /// For checkbox columns, this completely clears the filter
        /// This is used by the X button in the search box
        /// </summary>
        private void ClearSearchTextAndTemporaryFilter()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                if (IsCheckboxColumn)
                {
                    // For checkbox columns, reset to initial state (no filter)
                    ResetCheckboxToInitialState();
                }
                else
                {
                    // For text columns, clear the search text
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
                }
                
                // Update filter panel
                SourceDataGrid?.UpdateFilterPanel();

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears only the temporary template when search text becomes empty
        /// This is used when user manually backspaces all text
        /// </summary>
        private void ClearTemporaryTemplate()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                // Remove only the temporary template if it exists
                if (_temporarySearchTemplate != null && SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    var firstGroup = SearchTemplateController.SearchGroups[0];
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                    
                    // Update the filter expression and apply to grid
                    SearchTemplateController.UpdateFilterExpression();
                    SourceDataGrid?.FilterItemsSource();
                    
                    // Update HasAdvancedFilter state
                    HasAdvancedFilter = SearchTemplateController?.HasCustomExpression ?? false;
                    
                    // Update filter panel
                    SourceDataGrid?.UpdateFilterPanel();

                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearTemporaryTemplate: {ex.Message}");
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
                        _temporarySearchTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType);
                        _temporarySearchTemplate.SearchType = SearchType.Contains;
                        _temporarySearchTemplate.SelectedValue = SearchText;
                        
                        // Check if we have existing confirmed Contains templates
                        var existingContainsTemplates = firstGroup.SearchTemplates
                            .Where(t => t.SearchType == SearchType.Contains && t.HasCustomFilter)
                            .ToList();
                        
                        // If this is not the first template, set OR operator
                        if (existingContainsTemplates.Any())
                        {
                            _temporarySearchTemplate.OperatorName = "Or";
                        }
                        
                        firstGroup.SearchTemplates.Add(_temporarySearchTemplate);
                    }

                    // Update the filter expression
                    SearchTemplateController.UpdateFilterExpression();

                    // Apply the filter to the grid
                    SourceDataGrid.FilterItemsSource();

                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();
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

                        // Update HasActiveFilter state
                        UpdateHasActiveFilterState();
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

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearFilterInternal: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the advanced filter window
        /// </summary>
        private void AllowRuleValueFilteringWindow()
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

                    var filterControl = new ColumnFilterEditor
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
                System.Diagnostics.Debug.WriteLine($"Error in AllowRuleValueFilteringWindow: {ex.Message}");
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
                    
                    // Determine checkbox column settings after loading values
                    DetermineCheckboxColumnSettings();
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

                // Clear checkbox state if this is a checkbox column
                if (IsCheckboxColumn)
                {
                    ResetCheckboxToInitialState();
                }

                // Clear the filter internally
                ClearFilterInternal();

                // Update HasActiveFilter state (ClearFilterInternal already does this, but just to be safe)
                UpdateHasActiveFilterState();
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
                // Check if we have a checkbox filter
                if (IsCheckboxColumn)
                {
                    if (FilterCheckboxState.HasValue)
                    {
                        return $"Equals '{(FilterCheckboxState.Value ? "True" : "False")}'";
                    }
                    else if (SearchTemplateController?.HasCustomExpression == true)
                    {
                        // Check if this is an IsNull filter (indeterminate state with filter)
                        var firstGroup = SearchTemplateController.SearchGroups.FirstOrDefault();
                        var firstTemplate = firstGroup?.SearchTemplates.FirstOrDefault();
                        if (firstTemplate?.SearchType == SearchType.IsEmpty)
                        {
                            return "Is Null";
                        }
                    }
                }

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