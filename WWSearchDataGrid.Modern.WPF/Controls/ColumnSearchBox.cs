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
    public partial class ColumnSearchBox : Control
    {
        #region Fields

        private SearchTextBox searchTextBox;
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
                new PropertyMetadata(false, OnIsCheckboxColumnChanged));

        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.Register("HasActiveFilter", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsComplexFilteringEnabledProperty =
            DependencyProperty.Register("IsComplexFilteringEnabled", typeof(bool), typeof(ColumnSearchBox),
                new PropertyMetadata(true));

        #endregion
        
        #region Properties

        private ICommand _clearSearchTextCommand;
        private ICommand _showRuleFilterEditorCommand;

        /// <summary>
        /// Gets the command to clear search text (only clears search text and temporary template)
        /// </summary>
        public ICommand ClearSearchTextCommand => _clearSearchTextCommand ??= new RelayCommand(_ => ClearSearchTextAndTemporaryFilter());
        public ICommand ShowRuleFilterEditorCommand => _showRuleFilterEditorCommand ??= new RelayCommand(_ =>
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
            
            searchTextBox = GetTemplateChild("PART_SearchTextBox") as SearchTextBox;
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

        private static void OnIsCheckboxColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnSearchBox control)
            {
                // Force template to refresh by re-applying it
                control.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var template = control.Template;
                    control.Template = null;
                    control.Template = template;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
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