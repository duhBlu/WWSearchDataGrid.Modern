using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
        private CheckBox filterCheckBox;
        private Popup _filterPopup;
        private ColumnFilterEditor _filterContent;
        private Timer _changeTimer;
        /// <summary>
        /// Track temporary template for real-time updates to the filter panel.
        /// </summary>
        private SearchTemplate _temporarySearchTemplate;
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

        public static readonly DependencyProperty FilterCheckboxStateProperty =
            DependencyProperty.Register("FilterCheckboxState", typeof(bool?), typeof(ColumnSearchBox),
                new PropertyMetadata(null, OnFilterCheckboxStateChanged));

        public static readonly DependencyProperty IsCheckboxColumnProperty =
            DependencyProperty.Register("IsCheckboxColumn", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.Register("HasActiveFilter", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsComplexFilteringEnabledProperty =
            DependencyProperty.Register("IsComplexFilteringEnabled", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(true));

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets the command to clear search text (only clears search text and temporary template)
        /// </summary>
        public ICommand ClearSearchTextCommand => new RelayCommand(_ => ClearSearchTextAndTemporaryFilter());
        public ICommand ShowRuleFilterEditorCommand => new RelayCommand(_ => 
        {
            if(SourceDataGrid != null)
            {
                SourceDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            }
            ShowFilterPopup();
        });

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
        /// Gets or sets whether complex filtering is actually enabled for this column,
        /// taking into account both grid-level and column-level settings.
        /// </summary>
        public bool IsComplexFilteringEnabled
        {
            get => (bool)GetValue(IsComplexFilteringEnabledProperty);
            private set => SetValue(IsComplexFilteringEnabledProperty, value);
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

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            InitializeSearchTemplateController();
            UpdateIsComplexFilteringEnabled();
        }

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
            
            if (searchTextBox != null)
            {
                searchTextBox.TextChanged -= OnSearchTextBoxTextChanged;
                searchTextBox.KeyDown -= OnSearchTextBoxKeyDown;
            }

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
            control.UpdateIsComplexFilteringEnabled();
        }


        private void OnSourceDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // NOTE: SearchDataGrid.OnCollectionChanged already handles incremental cache updates
            // for all columns via UpdateColumnCachesForAddedItems/UpdateColumnCachesForRemovedItems.
            // This handler only needs to deal with Reset actions that require full refresh.

            // Check if we have valid data to process
            if (string.IsNullOrEmpty(BindingPath) || SearchTemplateController == null)
                return;

            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        // Only handle Reset - full refresh is required
                        SearchTemplateController.RefreshColumnValues();
                        break;

                    // For Add/Remove/Replace actions, do nothing - SearchDataGrid already handled
                    // the cache updates incrementally via UpdateColumnCachesForAddedItems/Removed
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        // Cache is already updated by SearchDataGrid
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSourceDataGridCollectionChanged: {ex.Message}");
                SearchTemplateController?.RefreshColumnValues();
            }
        }
        
        // NOTE: Batch update methods removed - SearchDataGrid now handles all cache updates centrally

        private static void OnCurrentColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnSearchBox control && e.NewValue is DataGridColumn column)
            {
                control.InitializeSearchTemplateController();
                control.UpdateIsComplexFilteringEnabled();
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
                // Only create permanent filters if complex filtering is enabled
                if (IsComplexFilteringEnabled)
                {
                    // Ctrl+Enter creates permanent filter and refocuses textbox
                    CreatePermanentFilterAndRefocus();
                }
                // In simple mode, Enter key does nothing - text persists and filters continue
            }
            else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true; // stop DataGrid from stealing it
                var req = new TraversalRequest(FocusNavigationDirection.Previous);
                // move focus the ColumnSearchBox to its previous peer
                (this as UIElement).MoveFocus(req);
            }
        }

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
            // When the ItemsSource changes completely, we need to re-setup the column data provider
            // This ensures column values are available after data source changes or cleanup
            try
            {
                if (SearchTemplateController == null)
                {
                    InitializeSearchTemplateController();
                }
                else if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath))
                {
                    // Re-initialize the column data provider in case it was cleared during cleanup
                    // This ensures column values are available after data source changes
                    if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                    {
                        // Re-setup the lazy loading provider
                        SearchTemplateController.SetupColumnDataLazy(GridColumn.GetEffectiveColumnDisplayName(CurrentColumn), GetColumnValuesFromDataGrid, BindingPath);

                        SearchTemplateController.RefreshColumnValues();

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
                            }
                            
                            if (sampleValues.Count > 0)
                            {
                                var detectedType = ReflectionHelper.DetermineColumnDataType(sampleValues);
                                SearchTemplateController.ColumnDataType = detectedType;
                            }
                        }
                    }
                    else
                    {
                        // No items yet - just set up basic structure
                        SearchTemplateController.SetupColumnDataLazy(GridColumn.GetEffectiveColumnDisplayName(CurrentColumn), GetColumnValuesFromDataGrid, BindingPath);

                        // Only refresh if cache exists - keep lazy loading for empty sources
                        //SearchTemplateController.RefreshColumnValues();
                    }

                    // Only re-determine column type based on definition, not data
                    DetermineCheckboxColumnTypeFromColumnDefinition();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSourceDataGridItemsSourceChanged: {ex.Message}");
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
            }), DispatcherPriority.Background);
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
                Debug.WriteLine($"Error in OnCheckboxPreviewKeyDown: {ex.Message}");
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
                Debug.WriteLine($"Error in OnCheckboxPreviewMouseDown: {ex.Message}");
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
                Debug.WriteLine($"Error in OnCheckboxFilterChanged: {ex.Message}");
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

                if (!hasFilter)
                {
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
        /// Updates the IsComplexFilteringEnabled property based on grid and column settings.
        /// Column-level explicit setting takes precedence over grid-level setting to allow per-column overrides.
        /// Uses ReadLocalValue to distinguish explicit column values from inherited grid values.
        /// </summary>
        internal void UpdateIsComplexFilteringEnabled()
        {
            bool isEnabled = true;

            if (CurrentColumn != null)
            {
                // Check if the column has an explicitly set local value (not inherited)
                var localValue = CurrentColumn.ReadLocalValue(GridColumn.EnableRuleFilteringProperty);

                if (localValue != DependencyProperty.UnsetValue)
                {
                    // Column has an explicit value set - use it (this is the override)
                    isEnabled = (bool)localValue;
                }
                else if (SourceDataGrid != null)
                {
                    // Column is using inherited value - use grid-level setting
                    isEnabled = SourceDataGrid.EnableRuleFiltering;
                }
                else
                {
                    // No grid and no explicit column value - use default
                    isEnabled = GridColumn.GetEnableRuleFiltering(CurrentColumn);
                }
            }
            else if (SourceDataGrid != null)
            {
                // No column set - fall back to grid-level setting
                isEnabled = SourceDataGrid.EnableRuleFiltering;
            }

            IsComplexFilteringEnabled = isEnabled;
        }

        /// <summary>
        /// Cycles the checkbox state forward based on current state and column properties
        /// PERFORMANCE: Triggers null status determination on first cycle - acceptable delay for user interaction
        /// </summary>
        private void CycleCheckboxStateForward()
        {
            try
            {
                _isInitialState = false; // We're now cycling, not in initial state

                // Ensure null status is determined before cycling
                // This will load cache if not already loaded, which is acceptable
                // since user is actively interacting with the filter
                SearchTemplateController?.EnsureNullStatusDetermined();

                var allowsNullValues = SearchTemplateController?.ContainsNullValues ?? false;
                var nextState = GetNextCycleState(_checkboxCycleState, allowsNullValues);
                SetCheckboxCycleState(nextState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CycleCheckboxStateForward: {ex.Message}");
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
                Debug.WriteLine($"Error in ResetCheckboxToInitialState: {ex.Message}");
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
                
                UpdateVisualCheckboxState(state);
                ApplyCheckboxCycleFilter(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SetCheckboxCycleState: {ex.Message}");
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
            bool? checkboxValue = state switch
            {
                CheckboxCycleState.Intermediate => null,
                CheckboxCycleState.Checked => true,
                CheckboxCycleState.Unchecked => false,
                _ => null
            };

            FilterCheckboxState = checkboxValue;
        }

        /// <summary>
        /// Applies the appropriate filter based on the current cycle state
        /// PERFORMANCE: Assumes null status already determined by CycleCheckboxStateForward
        /// </summary>
        /// <param name="state">The state to apply filter for</param>
        private void ApplyCheckboxCycleFilter(CheckboxCycleState state)
        {
            // Null status should already be determined by CycleCheckboxStateForward
            // but check again to be safe
            SearchTemplateController?.EnsureNullStatusDetermined();

            var allowsNullValues = SearchTemplateController?.ContainsNullValues ?? false;
            switch (state)
            {
                case CheckboxCycleState.Intermediate:
                    if (_isInitialState || !allowsNullValues)
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

                // Bind to property changes for auto-apply monitoring
                SearchTemplateController.SubscribeToTemplateChanges(template);

                // Add the template
                group.SearchTemplates.Add(template);

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyCheckboxBooleanFilter: {ex.Message}");
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

                // Bind to property changes for auto-apply monitoring
                SearchTemplateController.SubscribeToTemplateChanges(template);

                // Add the template
                group.SearchTemplates.Add(template);

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyCheckboxIsNullFilter: {ex.Message}");
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
                Debug.WriteLine($"Error in CreatePermanentFilterAndRefocus: {ex.Message}");
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
                if (SourceDataGrid == null || CurrentColumn == null)
                    return;

                if (SearchTemplateController == null)
                {
                    SearchTemplateController = new SearchTemplateController();
                }
                SearchTemplateController.ColumnName = GridColumn.GetEffectiveColumnDisplayName(CurrentColumn);

                // Three-tier fallback logic for determining the binding path:
                // 1. FilterMemberPath (explicit override via GridColumn attached property)
                // 2. SortMemberPath (standard DataGrid column property)
                // 3. Binding path (extracted from DataGridBoundColumn.Binding)
                string resolvedPath = GridColumn.GetFilterMemberPath(CurrentColumn);

                if (string.IsNullOrEmpty(resolvedPath))
                {
                    resolvedPath = CurrentColumn.SortMemberPath;
                }

                if (string.IsNullOrEmpty(resolvedPath) && CurrentColumn is DataGridBoundColumn boundColumn)
                {
                    resolvedPath = (boundColumn.Binding as Binding)?.Path?.Path;
                }

                BindingPath = resolvedPath;

                // Debug logging when FilterMemberPath is explicitly used
                DetermineCheckboxColumnTypeFromColumnDefinition();

                if (!SourceDataGrid.DataColumns.Contains(this))
                    SourceDataGrid.DataColumns.Add(this);

                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                SourceDataGrid.CollectionChanged += OnSourceDataGridCollectionChanged;

                SourceDataGrid.ItemsSourceChanged -= OnSourceDataGridItemsSourceChanged;
                SourceDataGrid.ItemsSourceChanged += OnSourceDataGridItemsSourceChanged;

                SearchTemplateController.SetupColumnDataLazy(GridColumn.GetEffectiveColumnDisplayName(CurrentColumn), GetColumnValuesFromDataGrid, BindingPath);

                if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                {
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
                }
                else
                {
                    // No items yet - just set up basic structure
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeSearchTemplateController: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if checkbox filtering should be used based on GridColumn.UseCheckBoxInSearchBox property
        /// </summary>
        private void DetermineCheckboxColumnTypeFromColumnDefinition()
        {
            try
            {
                if (CurrentColumn == null)
                {
                    SetCheckboxColumnState(false);
                    return;
                }

                // Check the explicit property on GridColumn
                bool isCheckboxType = GridColumn.GetUseCheckBoxInSearchBox(CurrentColumn);

                // Set the UI state immediately
                SetCheckboxColumnState(isCheckboxType);

                // If this is a checkbox column, set the appropriate column data type
                if (isCheckboxType && SearchTemplateController != null)
                {
                    SearchTemplateController.ColumnDataType = ColumnDataType.Boolean;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error determining column type from definition: {ex.Message}");
                SetCheckboxColumnState(false);
            }
        }

        /// <summary>
        /// Sets the checkbox column state and UI properties
        /// </summary>
        private void SetCheckboxColumnState(bool isCheckboxColumn)
        {
            var previousIsCheckboxColumn = IsCheckboxColumn;

            IsCheckboxColumn = isCheckboxColumn;

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
        /// Adds an incremental filter with OR logic, preserving the search type from DefaultSearchMode
        /// </summary>
        private void AddIncrementalContainsFilter()
        {
            try
            {
                if (SearchTemplateController == null || SourceDataGrid == null)
                    return;

                if (SearchTemplateController.SearchGroups.Count == 0)
                {
                    SearchTemplateController.AddSearchGroup();
                }

                var firstGroup = SearchTemplateController.SearchGroups[0];

                // Get the search type from temporary template (if it exists) to preserve DefaultSearchMode
                var searchType = SearchType.Contains; // Default fallback
                if (_temporarySearchTemplate != null)
                {
                    searchType = _temporarySearchTemplate.SearchType;
                    firstGroup.SearchTemplates.Remove(_temporarySearchTemplate);
                    _temporarySearchTemplate = null;
                }
                else
                {
                    // If no temporary template exists, get from column property
                    var defaultMode = CurrentColumn != null
                        ? GridColumn.GetDefaultSearchMode(CurrentColumn)
                        : DefaultSearchMode.Contains;
                    searchType = MapDefaultSearchModeToSearchType(defaultMode);
                }

                RemoveDefaultEmptyTemplates(firstGroup);

                // Find existing templates with same search type for OR logic
                var existingTemplatesOfSameType = firstGroup.SearchTemplates
                    .Where(t => t.SearchType == searchType && t.HasCustomFilter)
                    .ToList();

                // Create new confirmed template with the preserved search type
                var newTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType);
                newTemplate.SearchType = searchType;
                newTemplate.SelectedValue = SearchText;
                newTemplate.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference

                // Bind to property changes for auto-apply monitoring
                SearchTemplateController.SubscribeToTemplateChanges(newTemplate);

                // If this is not the first template of this type, set OR operator
                if (existingTemplatesOfSameType.Any())
                {
                    newTemplate.OperatorName = "Or";
                }

                firstGroup.SearchTemplates.Add(newTemplate);

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update the filter expression
                SearchTemplateController.UpdateFilterExpression();

                // Apply the filter to the grid
                SourceDataGrid.FilterItemsSource();

                // Update HasActiveFilter state
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddIncrementalContainsFilter: {ex.Message}");
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
                Debug.WriteLine($"Error in ClearSearchTextOnly: {ex.Message}");
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
                Debug.WriteLine($"Error in ClearSearchTextAndTemporaryFilter: {ex.Message}");
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
                    // Get search mode from column or use default Contains
                    var defaultMode = CurrentColumn != null
                        ? GridColumn.GetDefaultSearchMode(CurrentColumn)
                        : DefaultSearchMode.Contains;

                    // Map DefaultSearchMode to SearchType
                    var searchType = MapDefaultSearchModeToSearchType(defaultMode);

                    // Create new temporary template
                    _temporarySearchTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType);
                    _temporarySearchTemplate.SearchType = searchType;
                    _temporarySearchTemplate.SelectedValue = SearchText;
                    _temporarySearchTemplate.SearchTemplateController = SearchTemplateController; // Ensure template has controller reference

                    // Bind to property changes for auto-apply monitoring
                    SearchTemplateController.SubscribeToTemplateChanges(_temporarySearchTemplate);

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

                // Update operator visibility for all templates
                SearchTemplateController.UpdateOperatorVisibility();

                // Update HasActiveFilter state immediately
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateTemporaryTemplateImmediate: {ex.Message}");
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
                Debug.WriteLine($"Error in ClearTemporaryTemplate: {ex.Message}");
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
                Debug.WriteLine($"Error in UpdateSimpleFilter: {ex.Message}");
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
                Debug.WriteLine($"Error in RemoveDefaultEmptyTemplates: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps the DefaultSearchMode enum to the corresponding SearchType enum value.
        /// This provides a type-safe way to convert from the restricted set of default modes
        /// to the full SearchType enum used by the filtering engine.
        /// </summary>
        /// <param name="mode">The default search mode to map</param>
        /// <returns>The corresponding SearchType enum value</returns>
        private SearchType MapDefaultSearchModeToSearchType(DefaultSearchMode mode)
        {
            return mode switch
            {
                DefaultSearchMode.Contains => SearchType.Contains,
                DefaultSearchMode.StartsWith => SearchType.StartsWith,
                DefaultSearchMode.EndsWith => SearchType.EndsWith,
                DefaultSearchMode.Equals => SearchType.Equals,
                _ => SearchType.Contains // Fallback to Contains for any unexpected values
            };
        }

        /// <summary>
        /// Internal implementation of clear filter
        /// </summary>
        private void ClearFilterInternal()
        {
            try
            {
                if (SearchTemplateController == null)
                    return;

                _temporarySearchTemplate = null;

                SearchTemplateController.ClearAndReset();

                HasAdvancedFilter = false;

                // Apply the updated (empty) filter to the grid
                SourceDataGrid?.FilterItemsSource();

                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearFilterInternal: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the advanced filter popup
        /// </summary>
        private void ShowFilterPopup()
        {
            try
            {
                if (SourceDataGrid == null)
                    return;

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

                // Calculate the vertical offset to position below the column header
                double verticalOffset = CalculateVerticalOffsetForColumnHeader();

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
                        MaxHeight = 600,
                        HorizontalOffset = _filterContent.HorizontalOffset,
                        VerticalOffset = verticalOffset
                    };

                    _filterPopup.KeyDown += OnPopupKeyDown;
                    _filterPopup.Closed += OnPopupClosed;
                }
                else
                {
                    _filterPopup.PlacementTarget = this;
                    _filterPopup.HorizontalOffset = _filterContent.HorizontalOffset;
                    _filterPopup.VerticalOffset = verticalOffset;
                }

                _filterPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowFilterPopup: {ex.Message}");
                if (_filterPopup != null)
                {
                    _filterPopup.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Calculates the vertical offset needed to position the popup below the column header
        /// </summary>
        private double CalculateVerticalOffsetForColumnHeader()
        {
            try
            {
                // Start with the base offset from the ColumnFilterEditor style
                double offset = _filterContent.VerticalOffset;

                // Find the parent DataGridColumnHeader
                var columnHeader = VisualTreeHelperMethods.FindAncestor<DataGridColumnHeader>(this);
                if (columnHeader != null)
                {
                    // Get the total height of the column header (includes both search box and header content)
                    double headerHeight = columnHeader.ActualHeight;

                    // Get the height of this ColumnSearchBox
                    double searchBoxHeight = this.ActualHeight;

                    // Calculate the additional offset needed to place the popup below the header content
                    // This is the height of the header content (headerHeight - searchBoxHeight)
                    double headerContentHeight = headerHeight - searchBoxHeight - 1;

                    // Add the header content height to the base offset
                    offset += headerContentHeight;
                }

                return offset;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating vertical offset: {ex.Message}");
                // Return the base offset from the style if calculation fails
                return _filterContent.VerticalOffset;
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
        /// Uses OriginalItemsSource to ensure all values are included, not just filtered ones
        /// </summary>
        private IEnumerable<object> GetColumnValuesFromDataGrid()
        {
            // Use OriginalItemsSource to get all values, not just the filtered ones
            var dataSource = SourceDataGrid?.OriginalItemsSource ?? SourceDataGrid?.Items;

            if (dataSource == null || string.IsNullOrEmpty(BindingPath))
                return Enumerable.Empty<object>();

            var values = new List<object>();
            foreach (var item in dataSource)
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
                _changeTimer?.Stop();

                SearchText = string.Empty;
                if (searchTextBox != null)
                    searchTextBox.Text = string.Empty;

                if (IsCheckboxColumn)
                {
                    ResetCheckboxToInitialState();
                }

                ClearFilterInternal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearFilter: {ex.Message}");
            }
        }
        #endregion
    }
}