using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private Popup _filterPopup;
        private ColumnFilterEditor _filterContent;
        private Timer _changeTimer;
        private SearchTemplate _temporarySearchTemplate; // Track temporary template for real-time updates
        private CheckboxCycleState _checkboxCycleState = CheckboxCycleState.Intermediate; // Current logical cycling state
        private bool _isInitialState = true; // Tracks if we're in the initial uncycled state
        private bool _isLoadingColumnValues = false; // Prevents infinite loop in LoadColumnValues
        private DateTime _lastLoadColumnValuesTime = DateTime.MinValue; // For debouncing LoadColumnValues
        private const int LOAD_COLUMN_VALUES_DEBOUNCE_MS = 100; // Minimum time between LoadColumnValues calls

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

        /// <summary>
        /// Gets whether this control has a temporary template (for synchronization with FilterPanel)
        /// </summary>
        public bool HasTemporaryTemplate => _temporarySearchTemplate != null;

        #endregion

        #region Constructors

        public ColumnSearchBox()
        {
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
            
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
        /// Sets whether to show the column editor filter button
        /// </summary>
        public static void SetAllowRuleValueFiltering(DependencyObject element, bool value) =>
            element.SetValue(AllowRuleValueFilteringProperty, value);

        /// <summary>
        /// Gets whether to show the column editor filter button
        /// </summary>
        public static bool GetAllowRuleValueFiltering(DependencyObject element) =>
            (bool)element.GetValue(AllowRuleValueFilteringProperty);

        #endregion

        #region Control Template Methods

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
            
            // Reset loading state flags
            _isLoadingColumnValues = false;
            _lastLoadColumnValuesTime = DateTime.MinValue;

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

            // Clean up popup and filter content
            if (_filterPopup != null)
            {
                _filterPopup.IsOpen = false;
                _filterPopup.KeyDown -= OnPopupKeyDown;
                _filterPopup.Closed -= OnPopupClosed;
                _filterPopup = null;
            }
            
            if (_filterContent != null)
            {
                _filterContent.FiltersApplied -= OnFiltersApplied;
                _filterContent.FiltersCleared -= OnFiltersCleared;
                _filterContent = null;
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


        private void OnSourceDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Check if we have valid data to process
            if (string.IsNullOrEmpty(BindingPath) || SearchTemplateController == null)
                return;

            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        HandleItemsAdded(e.NewItems);
                        break;
                        
                    case NotifyCollectionChangedAction.Remove:
                        HandleItemsRemoved(e.OldItems);
                        break;
                        
                    case NotifyCollectionChangedAction.Replace:
                        HandleItemsReplaced(e.OldItems, e.NewItems);
                        break;
                        
                    case NotifyCollectionChangedAction.Reset:
                        SearchTemplateController.RefreshColumnValues();
                        
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            DetermineCheckboxColumnSettings();
                        }), DispatcherPriority.Background);
                        break;
                        
                    case NotifyCollectionChangedAction.Move:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSourceDataGridCollectionChanged: {ex.Message}");
                // Fallback to full refresh if incremental update fails
                SearchTemplateController?.RefreshColumnValues();
            }
        }
        
        /// <summary>
        /// Handles items being added to the collection with incremental updates
        /// </summary>
        private void HandleItemsAdded(System.Collections.IList newItems)
        {
            if (newItems == null || newItems.Count == 0) return;
            
            foreach (var item in newItems)
            {
                var value = ReflectionHelper.GetPropValue(item, BindingPath);
                SearchTemplateController.AddOrUpdateColumnValue(value);
            }
        }
        
        /// <summary>
        /// Handles items being removed from the collection with incremental updates
        /// </summary>
        private void HandleItemsRemoved(System.Collections.IList oldItems)
        {
            if (oldItems == null || oldItems.Count == 0) return;
            
            // For removes, we need to check if the value still exists in other items
            // If not, remove it from column values
            foreach (var item in oldItems)
            {
                var value = ReflectionHelper.GetPropValue(item, BindingPath);
                
                // Check if this value still exists in the remaining items
                bool valueStillExists = false;
                foreach (var remainingItem in SourceDataGrid.Items)
                {
                    var remainingValue = ReflectionHelper.GetPropValue(remainingItem, BindingPath);
                    if (Equals(value, remainingValue))
                    {
                        valueStillExists = true;
                        break;
                    }
                }
                
                if (!valueStillExists)
                {
                    SearchTemplateController.RemoveColumnValue(value);
                }
            }
        }
        
        /// <summary>
        /// Handles items being replaced in the collection with incremental updates
        /// </summary>
        private void HandleItemsReplaced(System.Collections.IList oldItems, System.Collections.IList newItems)
        {
            if (oldItems != null)
            {
                HandleItemsRemoved(oldItems);
            }
            
            if (newItems != null)
            {
                HandleItemsAdded(newItems);
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
            if (d is ColumnSearchBox control && (control._filterPopup?.IsOpen != true))
            {
                // If text is empty, clear only the temporary template
                if (string.IsNullOrWhiteSpace((string)e.NewValue))
                {
                    control.ClearTemporaryTemplate();
                }
                else
                {
                    // FIXED: Create template immediately for state synchronization
                    control.CreateTemporaryTemplateImmediate();
                    
                    // Still use timer for debounced filter application
                    control.StartOrResetChangeTimer();
                }
            }
        }

        private void OnSearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_filterPopup?.IsOpen != true)
                SearchText = searchTextBox.Text;
        }

        private void OnSearchTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                ClearSearchTextAndTemporaryFilter();
            else if (e.Key == Key.Enter)
            {
                // Ctrl+Enter creates permanent filter and refocuses textbox
                CreatePermanentFilterAndRefocus();
            }
            else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;                         // stop DataGrid from stealing it
                var req = new TraversalRequest(FocusNavigationDirection.Previous);
                // move focus *from* the ColumnSearchBox to its previous peer
                (this as UIElement).MoveFocus(req);
            }
        }

        private void OnAdvancedFilterButtonClick(object sender, RoutedEventArgs e) => ShowFilterPopup();

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
            // When the ItemsSource changes completely, we just need to reset the lazy loading
            // Values will be loaded only when actually needed (e.g., opening filter dialogs)
            try
            {
                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath) && SearchTemplateController != null)
                {
                    // Refresh the lazy loading provider - no eager loading
                    SearchTemplateController.RefreshColumnValues();
                    
                    // Only re-determine column type based on definition, not data
                    DetermineCheckboxColumnTypeFromColumnDefinition();
                }
                else if (SearchTemplateController == null)
                {
                    InitializeSearchTemplateController();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSourceDataGridItemsSourceChanged: {ex.Message}");
            }
        }

        private void OnColumnSearchBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Only track focus changes for within-container checks
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
                        // Focus is still within this ColumnSearchBox, no action needed
                        return;
                    }
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
                if (_filterPopup?.IsOpen != true)
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

                // FIXED: Text filtering logic - check for actual templates, not just SearchText
                if (!hasFilter)
                {
                    // First check if we have actual search templates (confirmed filters)
                    hasFilter = SearchTemplateController.HasCustomExpression;
                    
                    // Only consider SearchText if we have a temporary template that exists
                    if (!hasFilter && _temporarySearchTemplate != null)
                    {
                        hasFilter = true;
                    }
                }
            }

            HasActiveFilter = hasFilter;
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
                template.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference

                // Add the template
                group.SearchTemplates.Add(template);

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
                template.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference
                
                // Add the template
                group.SearchTemplates.Add(template);

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyCheckboxIsNullFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a permanent filter from current search text and refocuses the textbox
        /// Used for Ctrl+Enter behavior
        /// </summary>
        private void CreatePermanentFilterAndRefocus()
        {
            try
            {
                // Stop the timer if it's running
                _changeTimer?.Stop();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    // Create permanent filter
                    AddIncrementalContainsFilter();
                    
                    // Clear search text
                    ClearSearchTextOnly();
                    
                    // Refocus the textbox for seamless workflow
                    searchTextBox?.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreatePermanentFilterAndRefocus: {ex.Message}");
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
                    SearchTemplateController = new SearchTemplateController();
                }

                SearchTemplateController.ColumnName = CurrentColumn.Header;
                BindingPath = CurrentColumn.SortMemberPath;
                
                // Set default search type from column's attached property
                SearchTemplateController.DefaultSearchType = ColumnFilterEditor.GetDefaultSearchType(CurrentColumn); 

                // This avoids any delay and provides instant UI feedback
                DetermineCheckboxColumnTypeFromColumnDefinition();

                // Add this control to the data grid's columns if not already there
                if (!SourceDataGrid.DataColumns.Contains(this))
                    SourceDataGrid.DataColumns.Add(this);

                // Hook into collection changed events
                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                SourceDataGrid.CollectionChanged += OnSourceDataGridCollectionChanged;

                // Hook into items source changed events for initial loading detection
                SourceDataGrid.ItemsSourceChanged -= OnSourceDataGridItemsSourceChanged;
                SourceDataGrid.ItemsSourceChanged += OnSourceDataGridItemsSourceChanged;

                // Set up lazy loading - no initial eager loading
                if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                {
                    // Set up the lazy loading provider
                    SearchTemplateController.SetupColumnDataLazy(CurrentColumn.Header, GetColumnValuesFromDataGrid, BindingPath);
                    
                    // Determine column data type from a small sample for immediate UI setup
                    var sampleSize = Math.Min(10, SourceDataGrid.Items.Count);
                    if (sampleSize > 0)
                    {
                        var sampleValues = new HashSet<object>();
                        var itemsArray = SourceDataGrid.Items.Cast<object>().Take(sampleSize);
                        
                        foreach (var item in itemsArray)
                        {
                            var value = ReflectionHelper.GetPropValue(item, BindingPath);
                            sampleValues.Add(value);
                            if (sampleValues.Count >= 5) break; // Small sample for type detection
                        }
                        
                        if (sampleValues.Any())
                        {
                            SearchTemplateController.ColumnDataType = ReflectionHelper.DetermineColumnDataType(sampleValues);
                        }
                    }
                    
                    // Determine checkbox column settings (doesn't require full data loading)
                    DetermineCheckboxColumnSettings();
                }
                else
                {
                    // No items yet - just set up basic structure
                    SearchTemplateController.SetupColumnDataLazy(CurrentColumn.Header, GetColumnValuesFromDataGrid, BindingPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InitializeSearchTemplateController: {ex.Message}");
            }
        }

        private void DetermineCheckboxColumnTypeFromColumnDefinition()
        {
            try
            {
                if (CurrentColumn == null)
                {
                    SetCheckboxColumnState(false, false);
                    return;
                }

                var isCheckboxType = false;

                // Check if it's explicitly a DataGridCheckBoxColumn
                if (CurrentColumn is DataGridCheckBoxColumn)
                {
                    isCheckboxType = true;
                }
                // Check binding property type for bound columns
                else if (CurrentColumn is DataGridBoundColumn boundColumn && boundColumn.Binding is System.Windows.Data.Binding binding)
                {
                    var bindingPath = binding.Path?.Path;
                    if (!string.IsNullOrEmpty(bindingPath))
                    {
                        isCheckboxType = DetermineIfBooleanPropertyFromDataContext(bindingPath);
                    }
                }
                // Check DependencyObjectType for template columns or custom scenarios
                else if (CurrentColumn is DataGridTemplateColumn)
                {
                    var dependencyObjectType = CurrentColumn.DependencyObjectType;
                    if (dependencyObjectType != null)
                    {
                        // Add logic based on dependencyObjectType.Name patterns you observe
                        isCheckboxType = IsCheckboxColumnByDependencyObjectType(dependencyObjectType);
                    }
                }

                // Set the UI state immediately
                SetCheckboxColumnState(isCheckboxType, false); // We don't know about nulls yet, will determine later

                // If this is a checkbox column, set the appropriate column data type
                if (isCheckboxType && SearchTemplateController != null)
                {
                    SearchTemplateController.ColumnDataType = ColumnDataType.Boolean;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining column type from definition: {ex.Message}");
                SetCheckboxColumnState(false, false);
            }
        }

        /// <summary>
        /// Determines if a binding path points to a boolean property by examining the data context
        /// </summary>
        private bool DetermineIfBooleanPropertyFromDataContext(string bindingPath)
        {
            try
            {
                // If we have data available, check the first item's property type
                if (SourceDataGrid?.Items?.Count > 0)
                {
                    var firstItem = SourceDataGrid.Items.Cast<object>().FirstOrDefault(item => item != null);
                    if (firstItem != null)
                    {
                        var propertyType = ReflectionHelper.GetPropertyType(firstItem, bindingPath);
                        return propertyType == typeof(bool) || propertyType == typeof(bool?);
                    }
                }

                // If no data available yet, check if we can infer from the ItemsSource type
                if (SourceDataGrid?.OriginalItemsSource != null)
                {
                    var itemsSourceType = SourceDataGrid.OriginalItemsSource.GetType();
                    if (itemsSourceType.IsGenericType)
                    {
                        var itemType = itemsSourceType.GetGenericArguments().FirstOrDefault();
                        if (itemType != null)
                        {
                            return ReflectionHelper.IsBooleanProperty(itemType, bindingPath);
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining boolean property from data context: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a column should be treated as checkbox based on DependencyObjectType
        /// </summary>
        private bool IsCheckboxColumnByDependencyObjectType(DependencyObjectType dependencyObjectType)
        {
            var typeName = dependencyObjectType.Name;

            if (typeName.Contains("CheckBox", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Boolean", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Toggle", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Add more patterns for custom if necessary
            // if (typeName == "YourCustomCheckboxColumnType")
            //     return true;

            return false;
        }

        /// <summary>
        /// Sets the checkbox column state and UI properties
        /// </summary>
        private void SetCheckboxColumnState(bool isCheckboxColumn, bool allowsNullValues)
        {
            var previousIsCheckboxColumn = IsCheckboxColumn;

            IsCheckboxColumn = isCheckboxColumn;
            AllowsNullValues = allowsNullValues;

            // Handle UI state changes when column type changes
            if (previousIsCheckboxColumn != isCheckboxColumn)
            {
                if (previousIsCheckboxColumn)
                {
                    // Was checkbox, now text - clear checkbox state
                    FilterCheckboxState = null;
                    if (filterCheckBox != null)
                    {
                        filterCheckBox.IsChecked = null;
                    }
                    _checkboxCycleState = CheckboxCycleState.Intermediate;
                    _isInitialState = true;
                }
                else
                {
                    // Was text, now checkbox - clear text search
                    SearchText = string.Empty;
                }
            }
        }

        /// <summary>
        /// Background analysis of null values for checkbox columns
        /// This runs after the UI has already been set to checkbox mode
        /// </summary>
        private void AnalyzeNullValuesInBackground()
        {
            if (!IsCheckboxColumn || SearchTemplateController == null)
                return;

            // Run this in background to avoid blocking UI
            Task.Run(() =>
            {
                try
                {
                    // Check if null values are present in the loaded data
                    bool allowsNulls = SearchTemplateController.ColumnValues.Contains(null);

                    // Update UI on main thread
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AllowsNullValues = allowsNulls;
                    }), DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error analyzing null values: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Updated method that now focuses on value loading and null analysis rather than column type detection
        /// Column type should already be determined by DetermineCheckboxColumnTypeFromColumnDefinition()
        /// </summary>
        private void DetermineCheckboxColumnSettings()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                // If we already determined this is a checkbox column, just analyze for null values
                if (IsCheckboxColumn)
                {
                    AnalyzeNullValuesInBackground();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in checkbox column settings analysis: {ex.Message}");
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
                newTemplate.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference
                
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
                
                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
                
                // Update filter panel
                SourceDataGrid?.UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a temporary template immediately for state synchronization
        /// This ensures HasActiveFilter state is accurate without waiting for timer
        /// </summary>
        private void CreateTemporaryTemplateImmediate()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || string.IsNullOrWhiteSpace(SearchText))
                    return;

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
                    _temporarySearchTemplate.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference
                    
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

                // Update HasActiveFilter state immediately
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateTemporaryTemplateImmediate: {ex.Message}");
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
                    
                    // Update HasActiveFilter state
                    UpdateHasActiveFilterState();

                    // Update filter panel
                    SourceDataGrid?.UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearTemporaryTemplate: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the filter to the grid (used for debounced/timer-based filter application)
        /// Template creation is now immediate, this method only handles the actual filtering
        /// </summary>
        private void UpdateSimpleFilter()
        {
            try
            {
                // Skip if controller is not available
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                if (!string.IsNullOrWhiteSpace(SearchText) && _temporarySearchTemplate != null)
                {
                    // Template should already exist from immediate creation
                    // Just ensure it has the latest search text (in case of rapid typing)
                    _temporarySearchTemplate.SelectedValue = SearchText;

                    // Update the filter expression
                    SearchTemplateController.UpdateFilterExpression();

                    // Apply the filter to the grid
                    SourceDataGrid.FilterItemsSource();

                    // Update HasAdvancedFilter state
                    HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                    
                    // Update filter panel
                    SourceDataGrid.UpdateFilterPanel();
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
        /// Shows the advanced filter popup
        /// </summary>
        private void ShowFilterPopup()
        {
            try
            {
                // Skip if source data grid is not available
                if (SourceDataGrid == null)
                    return;

                // Skip if controller is not available
                if (SearchTemplateController == null)
                    InitializeSearchTemplateController();

                if (SearchTemplateController == null)
                    return;

                // Create filter content if none exists
                if (_filterContent == null)
                {
                    _filterContent = new ColumnFilterEditor
                    {
                        SearchTemplateController = SearchTemplateController,
                        DataContext = this
                    };

                    // Subscribe to filter events
                    _filterContent.FiltersApplied += OnFiltersApplied;
                    _filterContent.FiltersCleared += OnFiltersCleared;
                }

                // Create popup if none exists
                if (_filterPopup == null)
                {
                    _filterPopup = new Popup
                    {
                        Child = _filterContent,
                        PlacementTarget = this,
                        Placement = PlacementMode.Bottom,
                        AllowsTransparency = true,
                        PopupAnimation = PopupAnimation.Fade,
                        StaysOpen = false,
                        MaxWidth = 500,
                        MaxHeight = 600
                    };

                    // Subscribe to popup events
                    _filterPopup.KeyDown += OnPopupKeyDown;
                    _filterPopup.Closed += OnPopupClosed;
                }
                else
                {
                    // Update placement target in case it changed
                    _filterPopup.PlacementTarget = this;
                }

                // Open the popup
                _filterPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowFilterPopup: {ex.Message}");
                if (_filterPopup != null)
                {
                    _filterPopup.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Handle popup key down events (Escape to close)
        /// </summary>
        private void OnPopupKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _filterPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle popup closed event for cleanup
        /// </summary>
        private void OnPopupClosed(object sender, EventArgs e)
        {
            // Additional cleanup if needed when popup closes
        }

        /// <summary>
        /// Handles filter editor filters applied event
        /// </summary>
        private void OnFiltersApplied(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterPanel();
        }

        /// <summary>
        /// Handles filter editor filters cleared event
        /// </summary>
        private void OnFiltersCleared(object sender, EventArgs e)
        {
            UpdateHasActiveFilterState();
            SourceDataGrid?.UpdateFilterPanel();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets column values for the current column (lazy loading approach)
        /// </summary>
        private IEnumerable<object> GetColumnValuesFromDataGrid()
        {
            if (SourceDataGrid?.Items == null || string.IsNullOrEmpty(BindingPath))
                return Enumerable.Empty<object>();
                
            var values = new List<object>();
            foreach (var item in SourceDataGrid.Items)
            {
                var value = ReflectionHelper.GetPropValue(item, BindingPath);
                values.Add(value);
            }
            return values;
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
                        if (firstTemplate?.SearchType == SearchType.IsBlank)
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

}