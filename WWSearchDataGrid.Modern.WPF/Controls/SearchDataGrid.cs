using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.WPF.Commands;
using WWSearchDataGrid.Modern.Core.Caching;
using System.Windows.Controls.Primitives;

namespace WWSearchDataGrid.Modern.WPF
{

    /// <summary>
    /// Modern implementation of the SearchDataGrid
    /// </summary>
    public class SearchDataGrid : DataGrid
    {
        #region Fields

        private readonly ObservableCollection<ColumnSearchBox> dataColumns = new ObservableCollection<ColumnSearchBox>();
        private IEnumerable originalItemsSource;
        private bool initialUpdateLayoutCompleted;

        // Collection context caching for performance optimization
        private readonly Dictionary<string, CollectionContext> _collectionContextCache =
            new Dictionary<string, CollectionContext>();
        private List<object> _materializedDataSource;
        private readonly object _contextCacheLock = new object();

        // Asynchronous filtering support
        private CancellationTokenSource _filterCancellationTokenSource;

        // Cell value change detection support
        private readonly Dictionary<string, object> _cellValueSnapshots = new Dictionary<string, object>();


        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SearchFilterProperty =
            DependencyProperty.Register("SearchFilter", typeof(Predicate<object>), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHasItemsProperty =
            DependencyProperty.Register("ActualHasItems", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnActualHasItemsChanged));

        /// <summary>
        /// Dependency property for EnableComplexFiltering
        /// </summary>
        public static readonly DependencyProperty EnableComplexFilteringProperty =
            DependencyProperty.Register("EnableComplexFiltering", typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data columns collection
        /// </summary>
        public ObservableCollection<ColumnSearchBox> DataColumns
        {
            get { return dataColumns; }
        }

        /// <summary>
        /// Gets or sets the search filter
        /// </summary>
        public Predicate<object> SearchFilter
        {
            get { return (Predicate<object>)GetValue(SearchFilterProperty); }
            set { SetValue(SearchFilterProperty, value); }
        }

        /// <summary>
        /// Gets whether the data source has any items, regardless of filtering
        /// </summary>
        public bool ActualHasItems
        {
            get { return (bool)GetValue(ActualHasItemsProperty); }
            private set { SetValue(ActualHasItemsProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether complex filtering is enabled at the grid level.
        /// When false, all columns use simple filtering mode only.
        /// When true, per-column EnableComplexFiltering settings are respected.
        /// </summary>
        public bool EnableComplexFiltering
        {
            get { return (bool)GetValue(EnableComplexFilteringProperty); }
            set { SetValue(EnableComplexFilteringProperty, value); }
        }

        /// <summary>
        /// Gets the original unfiltered items source
        /// </summary>
        public IEnumerable OriginalItemsSource => originalItemsSource;

        /// <summary>
        /// Gets the filter panel control
        /// </summary>
        public FilterPanel FilterPanel { get; private set; }

        /// <summary>
        /// Gets the count of original items for debugging purposes
        /// </summary>
        public int OriginalItemsCount
        {
            get
            {
                if (originalItemsSource == null) return 0;
                if (originalItemsSource is ICollection collection) return collection.Count;
                return originalItemsSource.Cast<object>().Count();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when items are added or removed from the collection
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Event raised when items source is changed
        /// </summary>
        public event EventHandler ItemsSourceChanged;

        /// <summary>
        /// Event raised when a cell value is changed through editing
        /// </summary>
        public event EventHandler<CellValueChangedEventArgs> CellValueChanged;

        #endregion

        #region Constructor

        public SearchDataGrid() : base()
        {
            // Add binding for DataGrid.Items attached property changes
            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(SearchDataGrid))
                .AddValueChanged(this, (s, e) => UpdateHasItemsProperty());

            // Initialize FilterPanel
            FilterPanel = new FilterPanel();

            // Subscribe to FilterPanel events
            FilterPanel.FiltersEnabledChanged += OnFiltersEnabledChanged;
            FilterPanel.FilterRemoved += OnFilterRemoved;
            FilterPanel.ValueRemovedFromToken += OnValueRemovedFromToken;
            FilterPanel.OperatorToggled += OnOperatorToggled;
            FilterPanel.ClearAllFiltersRequested += OnClearAllFiltersRequested;

            // Initialize context menu functionality
            this.InitializeContextMenu();

            // Add keyboard shortcuts for copy operations
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesCommand.Execute(this)), Key.C, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand.Execute(this)), Key.C, ModifierKeys.Control | ModifierKeys.Shift));

            // Subscribe to selection change events to update row count display
            this.SelectionChanged += OnSelectionChanged;
            this.SelectedCellsChanged += OnSelectedCellsChanged;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// When applying the template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Wire up editing events to handle transformed data editing
            this.BeginningEdit += OnBeginningEdit;
            this.RowEditEnding += OnRowEditEnding;
            this.CellEditEnding += OnCellEditEnding;

            // Get the FilterPanel template part and connect it to our FilterPanel instance
            if (GetTemplateChild("PART_FilterPanel") is FilterPanel templateFilterPanel && FilterPanel != null)
            {
                // Copy the current state from our FilterPanel to the template FilterPanel
                templateFilterPanel.FiltersEnabled = FilterPanel.FiltersEnabled;
                templateFilterPanel.UpdateActiveFilters(FilterPanel.ActiveFilters);

                // Wire up events from template FilterPanel to our FilterPanel events
                templateFilterPanel.FiltersEnabledChanged += (s, e) => OnFiltersEnabledChanged(s, e);
                templateFilterPanel.FilterRemoved += (s, e) => OnFilterRemoved(s, e);
                templateFilterPanel.ValueRemovedFromToken += (s, e) => OnValueRemovedFromToken(s, e);
                templateFilterPanel.OperatorToggled += (s, e) => OnOperatorToggled(s, e);
                templateFilterPanel.ClearAllFiltersRequested += (s, e) => OnClearAllFiltersRequested(s, e);

                // Replace our FilterPanel property with the template instance so updates go to the right place
                FilterPanel = templateFilterPanel;
            }

            // Set up select-all column headers when template is applied
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetupSelectAllColumnHeaders();
            }), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Sets up select-all checkboxes in column headers
        /// </summary>
        private void SetupSelectAllColumnHeaders()
        {
            try
            {
                // Find all column headers
                var headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return;

                var headers = FindVisualChildren<DataGridColumnHeader>(headersPresenter).ToList();

                foreach (var header in headers)
                {
                    var column = header.Column;
                    if (column == null)
                        continue;

                    // Check if this column has IsSelectAllColumn enabled and is boolean type
                    bool isSelectAllColumn = GridColumn.GetIsSelectAllColumn(column);
                    bool isBooleanColumn = GridColumn.IsColumnBooleanType(column, this);

                    // Find the select-all checkbox in the header
                    var checkbox = FindVisualChild<CheckBox>(header, "PART_SelectAllCheckBox");
                    if (checkbox == null)
                        continue;

                    // Find the row count TextBlock in the header
                    var rowCountTextBlock = FindVisualChild<TextBlock>(header, "PART_SelectAllRowCount");

                    // Show checkbox only if both conditions are met
                    if (isSelectAllColumn && isBooleanColumn)
                    {
                        checkbox.Visibility = Visibility.Visible;

                        // Wire up click event (remove old handler first to prevent duplicates)
                        checkbox.Click -= OnSelectAllCheckboxClicked;
                        checkbox.Click += OnSelectAllCheckboxClicked;

                        // Set initial state
                        checkbox.IsChecked = CalculateSelectAllCheckboxState(column);

                        // Show/hide row count based on scope
                        var scope = GridColumn.GetSelectAllScope(column);
                        if (rowCountTextBlock != null)
                        {
                            if (scope == SelectAllScope.SelectedRows)
                            {
                                rowCountTextBlock.Visibility = Visibility.Visible;
                                // Initialize the count
                                var items = GetItemsForSelectAllScope(scope);
                                rowCountTextBlock.Text = $"({items?.Count() ?? 0})";
                            }
                            else
                            {
                                rowCountTextBlock.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        checkbox.Visibility = Visibility.Collapsed;
                        checkbox.Click -= OnSelectAllCheckboxClicked;

                        if (rowCountTextBlock != null)
                            rowCountTextBlock.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up select-all column headers: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles click events on select-all checkboxes
        /// </summary>
        private void OnSelectAllCheckboxClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not CheckBox checkbox)
                    return;

                // Find the column header that contains this checkbox
                var header = FindVisualParent<DataGridColumnHeader>(checkbox);
                if (header?.Column == null)
                    return;

                // Toggle all values in the column
                ToggleSelectAllColumn(header.Column);

                // Update the checkbox state to reflect the new data state
                checkbox.IsChecked = CalculateSelectAllCheckboxState(header.Column);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling select-all checkbox click: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds a parent of a specific type in the visual tree
        /// </summary>
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            try
            {
                DependencyObject parentObject = VisualTreeHelper.GetParent(child);

                if (parentObject == null)
                    return null;

                if (parentObject is T parent)
                    return parent;

                return FindVisualParent<T>(parentObject);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Handles selection change events to update row count displays
        /// </summary>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Update row count displays for columns using SelectedRows scope
                UpdateSelectAllRowCountForAllColumns();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSelectionChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles selected cells change events to update row count displays
        /// </summary>
        private void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                // Update row count displays for columns using SelectedRows scope
                UpdateSelectAllRowCountForAllColumns();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSelectedCellsChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates row count displays and checkbox states for all columns using SelectedRows scope
        /// </summary>
        private void UpdateSelectAllRowCountForAllColumns()
        {
            try
            {
                // Find all column headers
                var headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return;

                var headers = FindVisualChildren<DataGridColumnHeader>(headersPresenter).ToList();

                foreach (var header in headers)
                {
                    var column = header.Column;
                    if (column == null)
                        continue;

                    // Check if this column has IsSelectAllColumn enabled
                    bool isSelectAllColumn = GridColumn.GetIsSelectAllColumn(column);
                    if (!isSelectAllColumn)
                        continue;

                    // Get the scope for this column
                    var scope = GridColumn.GetSelectAllScope(column);

                    // Update row count display if using SelectedRows scope
                    if (scope == SelectAllScope.SelectedRows)
                    {
                        // Find the row count TextBlock
                        var countTextBlock = FindVisualChild<TextBlock>(header, "PART_SelectAllRowCount");
                        if (countTextBlock != null && countTextBlock.Visibility == Visibility.Visible)
                        {
                            // Update the count
                            var items = GetItemsForSelectAllScope(scope);
                            countTextBlock.Text = $"({items?.Count() ?? 0})";
                        }
                    }

                    // Update checkbox state for SelectedRows scope (checkbox state should reflect selected items)
                    if (scope == SelectAllScope.SelectedRows)
                    {
                        // Find the select-all checkbox in the header
                        var checkbox = FindVisualChild<CheckBox>(header, "PART_SelectAllCheckBox");
                        if (checkbox != null && checkbox.Visibility == Visibility.Visible)
                        {
                            // Update the checkbox state based on the selected rows
                            checkbox.IsChecked = CalculateSelectAllCheckboxState(column);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating select-all row counts: {ex.Message}");
            }
        }

        #endregion

        #region Editing Event Handlers

        /// <summary>
        /// Handles the beginning of edit operations
        /// </summary>
        private void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                // Capture original value for change detection
                if (e.Row?.Item != null && e.Column != null)
                {
                    var bindingPath = GetColumnBindingPath(e.Column);
                    if (!string.IsNullOrEmpty(bindingPath))
                    {
                        var snapshotKey = CreateSnapshotKey(e.Row.Item, bindingPath);
                        if (!string.IsNullOrEmpty(snapshotKey))
                        {
                            var originalValue = ReflectionHelper.GetPropValue(e.Row.Item, bindingPath);
                            _cellValueSnapshots[snapshotKey] = originalValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnBeginningEdit: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles row edit ending
        /// </summary>
        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            try
            {
                // Standard row editing - no special handling needed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnRowEditEnding: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles cell edit ending
        /// </summary>
        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                // Skip processing if edit was cancelled
                if (e.EditAction == DataGridEditAction.Cancel)
                    return;

                // Get binding path and snapshot key
                var bindingPath = GetColumnBindingPath(e.Column);
                if (string.IsNullOrEmpty(bindingPath) || e.Row?.Item == null)
                    return;

                var snapshotKey = CreateSnapshotKey(e.Row.Item, bindingPath);
                if (string.IsNullOrEmpty(snapshotKey) || !_cellValueSnapshots.TryGetValue(snapshotKey, out var originalValue))
                    return;

                var rowIndex = e.Row.GetIndex();
                var columnIndex = e.Column.DisplayIndex;

                // Force the binding to update the source (this makes focus loss behave like Enter)
                ForceBindingUpdate(e.EditingElement);

                // Try to get the edited value from the editing element with type conversion
                object editedValue = null;
                try
                {
                    editedValue = GetEditedValueFromElement(e.EditingElement, originalValue);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting edited value: {ex.Message}");
                }

                // Use Dispatcher.BeginInvoke to process after binding updates complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        object finalValue;

                        if (editedValue != null)
                        {
                            finalValue = editedValue;
                        }
                        else
                        {
                            try
                            {
                                CommitEdit(DataGridEditingUnit.Cell, true);
                            }
                            catch
                            {
                            }
                            finalValue = ReflectionHelper.GetPropValue(e.Row.Item, bindingPath);
                        }

                        if (!EqualityComparer<object>.Default.Equals(originalValue, finalValue))
                        {
                            OnCellValueChangedInternal(e.Row.Item, e.Column, bindingPath,
                                originalValue, finalValue, rowIndex, columnIndex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in delayed cell edit processing: {ex.Message}");
                    }
                    finally
                    {
                        // Clean up snapshot after processing/error
                        _cellValueSnapshots.Remove(snapshotKey);
                    }
                }), DispatcherPriority.DataBind);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCellEditEnding: {ex.Message}");
            }
        }

        #endregion

        #region Methods

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        protected override void OnInitializingNewItem(InitializingNewItemEventArgs e)
        {
            base.OnInitializingNewItem(e);
        }

        protected override void OnAddingNewItem(AddingNewItemEventArgs e)
        {
            base.OnAddingNewItem(e);

            if (Items.Filter != null)
            {
                FilterItemsSource();
            }

            ItemsSourceChanged?.Invoke(this, EventArgs.Empty);
            UpdateLayout();

            // Update ActualHasItems property after item is added
            UpdateHasItemsProperty();
        }

        protected override void OnLoadingRow(DataGridRowEventArgs e)
        {
            base.OnLoadingRow(e);
            if (!initialUpdateLayoutCompleted)
            {
                ItemsSourceChanged?.Invoke(this, null);
                UpdateLayout();
                initialUpdateLayoutCompleted = true;
            }
        }

        private static void OnActualHasItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                // Force column headers to update
                grid.InvalidateVisual();

                // If we now have items and didn't before, we may need to adjust layout
                if ((bool)e.NewValue && !(bool)e.OldValue)
                {
                    // Ensure column headers update their layout
                    grid.UpdateLayout();
                }
            }
        }

        /// <summary>
        /// When items source changes, notify controls with safeguards against recursive calls
        /// </summary>
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            try
            {
                base.OnItemsSourceChanged(oldValue, newValue);

                // Clear cached data from the old data source to prevent memory leaks
                if (oldValue != null && newValue != oldValue)
                {
                    ClearAllCachedData();
                }

                // Clear cell value snapshots when data source changes
                _cellValueSnapshots.Clear();

                originalItemsSource = newValue;
                
                // Invalidate collection context cache when data source changes
                InvalidateCollectionContextCache();

                // Register for collection changed events if the source supports it
                UnregisterCollectionChangedEvent(oldValue);
                RegisterCollectionChangedEvent(newValue);

                if (newValue != null)
                {
                    // Update ActualHasItems property
                    UpdateHasItemsProperty();

                    // Notify controls that items source has changed
                    ItemsSourceChanged?.Invoke(this, EventArgs.Empty);

                    // Apply any existing filters - check for active column filters, not just Items.Filter
                    if (HasActiveColumnFilters() || Items.Filter != null)
                    {
                        FilterItemsSource();
                    }

                    UpdateLayout();

                    // Update select-all checkbox states after items source changes
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateAllSelectAllCheckboxStates();
                    }), DispatcherPriority.Background);
                }
                else
                {
                    // If items source is null, set ActualHasItems to false and clear cached data
                    ActualHasItems = false;
                    ClearAllCachedData();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnItemsSourceChanged: {ex.Message}");
            }
        }

        private void RegisterCollectionChangedEvent(IEnumerable collection)
        {
            if (collection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void UnregisterCollectionChangedEvent(IEnumerable collection)
        {
            if (collection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged -= OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Update ActualHasItems property when collection changes
            UpdateHasItemsProperty();

            // Handle incremental column value cache updates for better performance
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // Incrementally add new values to column caches
                UpdateColumnCachesForAddedItems(e.NewItems);
                InvalidateCollectionContextCache();
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                // Incrementally remove values from column caches
                UpdateColumnCachesForRemovedItems(e.OldItems);
                InvalidateCollectionContextCache();
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                // Handle replace as remove old + add new
                if (e.OldItems != null)
                    UpdateColumnCachesForRemovedItems(e.OldItems);
                if (e.NewItems != null)
                    UpdateColumnCachesForAddedItems(e.NewItems);
                InvalidateCollectionContextCache();
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Full reset - clear all cached data
                ClearAllCachedData();
            }

            CollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Updates column value caches incrementally when items are added
        /// </summary>
        private void UpdateColumnCachesForAddedItems(IList newItems)
        {
            if (newItems == null || newItems.Count == 0 || DataColumns.Count == 0)
                return;

            try
            {
                // For each column with a search template controller, update its cache
                foreach (var columnSearchBox in DataColumns)
                {
                    if (columnSearchBox?.SearchTemplateController == null ||
                        string.IsNullOrEmpty(columnSearchBox.BindingPath))
                        continue;

                    try
                    {
                        // Extract values from new items for this column
                        var newValues = new List<object>();
                        foreach (var item in newItems)
                        {
                            var value = ReflectionHelper.GetPropValue(item, columnSearchBox.BindingPath);
                            newValues.Add(value);
                        }

                        // Try incremental update
                        bool success = columnSearchBox.SearchTemplateController.TryAddColumnValues(newValues);

                        if (!success)
                        {
                            // Fallback: refresh this column's cache
                            Debug.WriteLine($"Incremental add failed for column {columnSearchBox.BindingPath}, refreshing cache");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating cache for column {columnSearchBox.BindingPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateColumnCachesForAddedItems: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates column value caches incrementally when items are removed
        /// </summary>
        private void UpdateColumnCachesForRemovedItems(IList oldItems)
        {
            if (oldItems == null || oldItems.Count == 0 || DataColumns.Count == 0)
                return;

            try
            {
                // For each column with a search template controller, update its cache
                foreach (var columnSearchBox in DataColumns)
                {
                    if (columnSearchBox?.SearchTemplateController == null ||
                        string.IsNullOrEmpty(columnSearchBox.BindingPath))
                        continue;

                    try
                    {
                        // Extract values from removed items for this column
                        var removedValues = new List<object>();
                        foreach (var item in oldItems)
                        {
                            var value = ReflectionHelper.GetPropValue(item, columnSearchBox.BindingPath);
                            removedValues.Add(value);
                        }

                        // Try incremental update
                        bool success = columnSearchBox.SearchTemplateController.TryRemoveColumnValues(removedValues);

                        if (!success)
                        {
                            // Fallback: refresh this column's cache
                            Debug.WriteLine($"Incremental remove failed for column {columnSearchBox.BindingPath}, refreshing cache");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating cache for column {columnSearchBox.BindingPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateColumnCachesForRemovedItems: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the ActualHasItems property based on the original items source
        /// </summary>
        private void UpdateHasItemsProperty()
        {
            bool hasAnyItems = false;

            // Check if the original items source has any items
            if (originalItemsSource != null)
            {
                // Different ways to check if collection has items
                if (originalItemsSource is ICollection collection)
                {
                    hasAnyItems = collection.Count > 0;
                }
                else
                {
                    // For other enumerable types, check if there's at least one item
                    var enumerator = originalItemsSource.GetEnumerator();
                    hasAnyItems = enumerator.MoveNext();

                    // Dispose the enumerator if it's disposable
                    if (enumerator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            // Update property if it changed
            if (ActualHasItems != hasAnyItems)
            {
                ActualHasItems = hasAnyItems;
            }
        }

        /// <summary>
        /// Checks if there are any active column filters that should be applied to new data
        /// </summary>
        /// <returns>True if there are active column filters</returns>
        private bool HasActiveColumnFilters()
        {
            return DataColumns?.Any(d => d.SearchTemplateController?.HasCustomExpression == true) == true ||
                   DataColumns?.Any(d => d.HasActiveFilter) == true;
        }

        /// <summary>
        /// Apply filters to the items source with performance optimization for large datasets
        /// </summary>
        /// <param name="delay">Optional delay before filtering</param>
        public async void FilterItemsSource(int delay = 0)
        {
            try
            {
                // Cancel any existing filtering operation
                _filterCancellationTokenSource?.Cancel();
                _filterCancellationTokenSource = new CancellationTokenSource();
                
                var cancellationToken = _filterCancellationTokenSource.Token;

                // Wait for delay if requested
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                // If cancelled, return
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                CommitEdit(DataGridEditingUnit.Row, true);

                // Check if filters are enabled before applying - respects FilterPanel checkbox
                if (FilterPanel?.FiltersEnabled == true)
                {
                    var activeFilters = DataColumns.Where(d => d.SearchTemplateController?.HasCustomExpression == true).ToList();
                    
                    if (activeFilters.Count > 0)
                    {
                        // Determine if async filtering is needed based on dataset size and filter complexity
                        var shouldUseAsyncFiltering = ShouldUseAsyncFiltering(activeFilters);
                        
                        if (shouldUseAsyncFiltering)
                        {
                            await ApplyFiltersAsync(activeFilters, cancellationToken);
                        }
                        else
                        {
                            // Use synchronous filtering for small datasets
                            Items.Filter = item => EvaluateUnifiedFilter(item, activeFilters);
                            SearchFilter = Items.Filter;
                        }
                    }
                    else
                    {
                        Items.Filter = null;
                        SearchFilter = null;
                    }
                }
                else
                {
                    // Filters are disabled - clear filter but preserve definitions
                    Items.Filter = null;
                    SearchFilter = null;
                }

                UpdateFilterPanel();

                // Update select-all checkbox states after filtering
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateAllSelectAllCheckboxStates();
                }), DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Filter operation was cancelled");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering items: {ex.Message}");
            }
        }

        /// <summary>
        /// Unified filter evaluation that handles both regular and collection-context filters with proper AND/OR logic
        /// Performance optimized with cached collection contexts
        /// </summary>
        /// <param name="item">The item to evaluate</param>
        /// <param name="activeFilters">List of all active column filters</param>
        /// <returns>True if the item passes all filters according to their logical operators</returns>
        private bool EvaluateUnifiedFilter(object item, List<ColumnSearchBox> activeFilters)
        {
            if (activeFilters.Count == 0)
                return true;

            try
            {
                // First filter is always included (no preceding operator)
                bool result = EvaluateFilterWithContext(item, activeFilters[0]);

                // Process remaining filters with their logical operators
                for (int i = 1; i < activeFilters.Count; i++)
                {
                    var filter = activeFilters[i];
                    bool filterResult = EvaluateFilterWithContext(item, filter);

                    // Apply the logical operator from this filter
                    // Get the operator from the first search group
                    string operatorName = "And"; // Default
                    if (filter.SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        operatorName = filter.SearchTemplateController.SearchGroups[0].OperatorName ?? "And";
                    }

                    if (operatorName == "Or")
                    {
                        result = result || filterResult;
                    }
                    else // AND is default
                    {
                        result = result && filterResult;
                    }

                    // Short-circuit optimization: if result is false and next operator is AND, we can stop
                    string nextOperator = "And";
                    if (i + 1 < activeFilters.Count && 
                        activeFilters[i + 1].SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        nextOperator = activeFilters[i + 1].SearchTemplateController.SearchGroups[0].OperatorName ?? "And";
                    }
                    if (!result && nextOperator != "Or")
                    {
                        break;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in unified filter evaluation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates a single filter against an item using cached collection contexts for optimal performance
        /// </summary>
        private bool EvaluateFilterWithContext(object item, ColumnSearchBox filter)
        {
            try
            {
                // Get the property value for this filter
                object propertyValue = ReflectionHelper.GetPropValue(item, filter.BindingPath);

                // Check if this filter requires collection context
                bool needsCollectionContext = DoesFilterRequireCollectionContext(filter);

                if (needsCollectionContext)
                {
                    // Use cached collection context for this column
                    var collectionContext = GetOrCreateCollectionContext(filter.BindingPath);
                    if (collectionContext != null)
                    {
                        return filter.SearchTemplateController.EvaluateWithCollectionContext(propertyValue, collectionContext);
                    }
                    else
                    {
                        // Fallback to standard evaluation if context creation failed
                        return filter.SearchTemplateController.FilterExpression(propertyValue);
                    }
                }
                else
                {
                    // Standard evaluation without collection context
                    return filter.SearchTemplateController.FilterExpression(propertyValue);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error evaluating filter for column {filter.BindingPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a filter requires collection context for evaluation
        /// </summary>
        private bool DoesFilterRequireCollectionContext(ColumnSearchBox filter)
        {
            if (filter?.SearchTemplateController?.SearchGroups == null)
                return false;

            // Check if any search template in any group requires collection context
            return filter.SearchTemplateController.SearchGroups
                .SelectMany(g => g.SearchTemplates)
                .Any(t => SearchEngine.RequiresCollectionContext(t.SearchType));
        }

        /// <summary>
        /// Gets or creates a materialized data source for collection context operations
        /// </summary>
        private List<object> GetMaterializedDataSource()
        {
            if (_materializedDataSource == null && originalItemsSource != null)
            {
                _materializedDataSource = originalItemsSource.Cast<object>().ToList();
            }
            return _materializedDataSource;
        }

        /// <summary>
        /// Gets or creates a cached collection context for the specified column
        /// </summary>
        private CollectionContext GetOrCreateCollectionContext(string bindingPath)
        {
            lock (_contextCacheLock)
            {
                if (!_collectionContextCache.TryGetValue(bindingPath, out var context))
                {
                    var materializedData = GetMaterializedDataSource();
                    if (materializedData != null && materializedData.Count > 0)
                    {
                        context = new CollectionContext(materializedData, bindingPath);
                        _collectionContextCache[bindingPath] = context;
                    }
                }
                return context;
            }
        }

        /// <summary>
        /// Clears the collection context cache when the data source changes
        /// </summary>
        private void InvalidateCollectionContextCache()
        {
            lock (_contextCacheLock)
            {
                // Dispose of existing collection contexts to release their cached references
                foreach (var context in _collectionContextCache.Values)
                {
                    if (context is IDisposable disposableContext)
                    {
                        try
                        {
                            disposableContext.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error disposing collection context: {ex.Message}");
                        }
                    }
                }
                
                _collectionContextCache.Clear();
                _materializedDataSource = null;
            }
        }

        /// <summary>
        /// Determines if asynchronous filtering should be used based on dataset size and filter complexity
        /// </summary>
        private bool ShouldUseAsyncFiltering(List<ColumnSearchBox> activeFilters)
        {
            try
            {
                var itemCount = OriginalItemsCount;
                
                // Use async for large datasets (>10k items)
                if (itemCount > 10000)
                    return true;
                
                // Use async for medium datasets with collection context filters
                if (itemCount > 5000 && activeFilters.Any(f => DoesFilterRequireCollectionContext(f)))
                    return true;
                    
                return false;
            }
            catch
            {
                // Default to synchronous filtering if we can't determine
                return false;
            }
        }

        /// <summary>
        /// Applies filters asynchronously with progress reporting and cancellation support
        /// </summary>
        private async Task ApplyFiltersAsync(
            List<ColumnSearchBox> activeFilters, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Pre-build collection contexts on background thread if needed
                await Task.Run(() =>
                {
                    // Pre-create collection contexts for filters that need them
                    foreach (var filter in activeFilters.Where(f => DoesFilterRequireCollectionContext(f)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        GetOrCreateCollectionContext(filter.BindingPath);
                    }
                }, cancellationToken);

                Items.Filter = item => EvaluateUnifiedFilter(item, activeFilters);
                SearchFilter = Items.Filter;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyFiltersAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearAllFilters()
        {
            foreach (var control in DataColumns)
            {
                control.ClearFilter();
            }

            Items.Filter = null;
            SearchFilter = null;

            ForceRestoreOriginalData();
        }
        
        /// <summary>
        /// Clears all cached data references to prevent memory leaks when data is cleared
        /// This method should be called when the data source is cleared or replaced
        /// </summary>
        public void ClearAllCachedData()
        {
            // Clear collection context cache and materialized data
            InvalidateCollectionContextCache();

            // Clear cell value snapshots
            _cellValueSnapshots.Clear();
            
            // Dispose of all column controllers to release their cached data
            foreach (var control in DataColumns)
            {
                if (control.SearchTemplateController is IDisposable disposableController)
                {
                    try
                    {
                        disposableController.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing SearchTemplateController: {ex.Message}");
                    }
                }
            }
            
            // Clear filters to release any remaining references
            Items.Filter = null;
            SearchFilter = null;
            
            // Trigger cache manager cleanup
            ColumnValueCacheManager.Instance.Cleanup(clearAll: false);
            
            // Force garbage collection to reclaim memory immediately
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Forces restoration of the original data source
        /// </summary>
        public void ForceRestoreOriginalData()
        {
            if (originalItemsSource != null)
            {
                // No longer needed - using unified filtering approach
                
                // Clear the filter to show all items from original source
                Items.Filter = null;
                
                Debug.WriteLine($"ForceRestoreOriginalData: ItemsSource count = {Items.Count} (event connectivity preserved)");
            }
        }

        /// <summary>
        /// Extracts the text content from a column header, handling both simple strings and template headers
        /// </summary>
        /// <param name="column">The DataGrid column</param>
        /// <returns>The extracted header text, or null if no text could be found</returns>
        internal static string ExtractColumnHeaderText(DataGridColumn column)
        {
            if (column == null)
                return null;

            var header = column.Header;
            if (header == null)
                return null;

            // If header is already a string, return it directly
            if (header is string headerString)
                return headerString;

            // If header is a FrameworkElement (template), extract text from it
            if (header is FrameworkElement element)
            {
                return ExtractTextFromVisualTree(element);
            }

            // Fallback: try ToString()
            return header.ToString();
        }

        /// <summary>
        /// Recursively extracts text content from a visual tree, prioritizing common text-bearing controls
        /// </summary>
        /// <param name="element">The root element to search</param>
        /// <returns>The extracted text, or null if no text could be found</returns>
        internal static string ExtractTextFromVisualTree(DependencyObject element)
        {
            if (element == null)
                return null;

            // Check common text-bearing controls first
            switch (element)
            {
                case TextBlock textBlock:
                    if (!string.IsNullOrWhiteSpace(textBlock.Text))
                        return textBlock.Text;
                    break;

                case Label label:
                    if (label.Content is string labelText && !string.IsNullOrWhiteSpace(labelText))
                        return labelText;
                    else if (label.Content is FrameworkElement labelContent)
                        return ExtractTextFromVisualTree(labelContent);
                    break;

                case TextBox textBox:
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                        return textBox.Text;
                    break;

                case ContentControl contentControl:
                    if (contentControl.Content is string contentText && !string.IsNullOrWhiteSpace(contentText))
                        return contentText;
                    else if (contentControl.Content is FrameworkElement contentElement)
                        return ExtractTextFromVisualTree(contentElement);
                    break;
            }

            // Recursively search children in the visual tree
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var text = ExtractTextFromVisualTree(child);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return null;
        }

        /// <summary>
        /// Gets the active column filters for the filter panel
        /// </summary>
        /// <returns>Collection of active filter information</returns>
        public IEnumerable<ColumnFilterInfo> GetActiveColumnFilters()
        {
            var activeFilters = new List<ColumnFilterInfo>();
            bool isFirstFilter = true;

            foreach (var column in DataColumns.Where(c => c.HasActiveFilter))
            {
                // Extract the actual operator from the SearchTemplateController
                string logicalOperator = string.Empty;
                if (!isFirstFilter)
                {
                    // Use the first SearchTemplateGroup's OperatorName as the operator for this column
                    if (column.SearchTemplateController?.SearchGroups?.Count > 0)
                    {
                        logicalOperator = column.SearchTemplateController.SearchGroups[0].OperatorName?.ToUpper() ?? "AND";
                    }
                    else
                    {
                        // Default to "AND" if no groups exist
                        logicalOperator = "AND";
                    }
                }

                var filterInfo = new ColumnFilterInfo
                {
                    ColumnName = GridColumn.GetEffectiveColumnDisplayName(column.CurrentColumn) ?? "Unknown",
                    BindingPath = column.BindingPath,
                    IsActive = true,
                    FilterData = column,
                    Operator = logicalOperator
                };

                // Determine filter type and display text
                // PRIORITY: Always check SearchTemplateController first (handles incremental Contains filters)
                if (column.SearchTemplateController?.HasCustomExpression == true)
                {
                    // Get structured components from SearchTemplateController
                    var components = column.SearchTemplateController.GetTokenizedFilterComponents();
                    filterInfo.SearchTypeText = components.SearchTypeText;
                    filterInfo.PrimaryValue = components.PrimaryValue;
                    filterInfo.SecondaryValue = components.SecondaryValue;
                    filterInfo.ValueOperatorText = components.ValueOperatorText;
                    filterInfo.IsDateInterval = components.IsDateInterval;
                    filterInfo.HasNoInputValues = components.HasNoInputValues;
                    
                    // Get all components for complex filters (including multiple Contains templates)
                    var allComponents = column.SearchTemplateController.GetAllTokenizedFilterComponents();
                    filterInfo.FilterComponents.Clear();
                    foreach (var component in allComponents)
                    {
                        filterInfo.FilterComponents.Add(component);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(column.SearchText) && column.HasTemporaryTemplate)
                {
                    // Set component properties for simple filters
                    filterInfo.SearchTypeText = "Contains";
                    filterInfo.PrimaryValue = column.SearchText;
                    filterInfo.HasNoInputValues = false;
                    filterInfo.IsDateInterval = false;
                    
                    // Add single component to collection
                    var simpleComponent = new FilterChipComponents
                    {
                        SearchTypeText = "Contains",
                        PrimaryValue = column.SearchText,
                        HasNoInputValues = false,
                        IsDateInterval = false
                    };
                    simpleComponent.ParsePrimaryValueAsMultipleValues();
                    filterInfo.FilterComponents.Add(simpleComponent);
                }

                activeFilters.Add(filterInfo);
                isFirstFilter = false;
            }

            return activeFilters;
        }

        /// <summary>
        /// Updates the filter panel with current filter state
        /// </summary>
        public void UpdateFilterPanel()
        {
            if (FilterPanel != null)
            {
                var activeFilters = GetActiveColumnFilters();
                FilterPanel.UpdateActiveFilters(activeFilters);
            }
        }

        #region FilterPanel Event Handlers

        /// <summary>
        /// Handles changes to the filters enabled state
        /// </summary>
        private void OnFiltersEnabledChanged(object sender, FilterEnabledChangedEventArgs e)
        {
            try
            {
                if (e.Enabled)
                {
                    FilterItemsSource();
                }
                else
                {
                    Items.Filter = null;
                    SearchFilter = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnFiltersEnabledChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to remove a specific filter
        /// </summary>
        private void OnFilterRemoved(object sender, RemoveFilterEventArgs e)
        {
            try
            {
                if (e.FilterInfo?.FilterData is ColumnSearchBox columnSearchBox)
                {
                    columnSearchBox.ClearFilter();
                    FilterItemsSource();
                    UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnFilterRemoved: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to remove a specific value from a filter token
        /// </summary>
        private void OnValueRemovedFromToken(object sender, ValueRemovedFromTokenEventArgs e)
        {
            try
            {
                if (e.RemovableToken?.RemovalContext != null && e.RemovableToken.SourceFilter?.FilterData is ColumnSearchBox columnSearchBox)
                {
                    var template = e.RemovableToken.RemovalContext.ParentTemplate;
                    var controller = columnSearchBox.SearchTemplateController;

                    if (controller != null && template != null)
                    {
                        // Check if this is the last template in the controller and if removing the value would make it invalid
                        var totalTemplates = controller.SearchGroups.SelectMany(g => g.SearchTemplates).Count();
                        var wouldBeInvalid = !template.WouldBeValidAfterValueRemoval(e.RemovableToken.RemovalContext);

                        if (totalTemplates <= 1 && wouldBeInvalid)
                        {
                            // If this is the last template and it would become invalid, clear the entire filter
                            columnSearchBox.ClearFilter();
                        }
                        else
                        {
                            // Otherwise, handle the value removal normally
                            controller.HandleValueRemoval(template, e.RemovableToken.RemovalContext);
                        }
                    }

                    // Reapply filters and update UI
                    FilterItemsSource();
                    UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnValueRemovedFromToken: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles operator toggle requests from the filter panel
        /// </summary>
        private void OnOperatorToggled(object sender, OperatorToggledEventArgs e)
        {
            try
            {
                if (e.OperatorToken?.SourceFilter?.FilterData is ColumnSearchBox columnSearchBox)
                {
                    var controller = columnSearchBox.SearchTemplateController;
                    if (controller == null)
                        return;

                    // Update the operator based on the level
                    if (e.Level == OperatorLevel.Group)
                    {
                        // Group-level operator: This represents the operator between different columns
                        // The column-level operator is always stored in SearchGroups[0].OperatorName
                        // (See GetActiveColumnFilters line 1208)
                        if (controller.SearchGroups.Count > 0)
                        {
                            controller.SearchGroups[0].OperatorName = e.NewOperator;
                        }
                    }
                    else if (e.Level == OperatorLevel.Template)
                    {
                        // Update the SearchTemplate operator
                        if (e.OperatorToken is TemplateLogicalConnectorToken templateToken)
                        {
                            var groupIndex = templateToken.GroupIndex;
                            var templateIndex = templateToken.TemplateIndex;

                            if (groupIndex >= 0 && groupIndex < controller.SearchGroups.Count)
                            {
                                var group = controller.SearchGroups[groupIndex];
                                if (templateIndex >= 0 && templateIndex < group.SearchTemplates.Count)
                                {
                                    group.SearchTemplates[templateIndex].OperatorName = e.NewOperator;
                                }
                            }
                        }
                    }

                    // Trigger filter update
                    controller.UpdateFilterExpression();

                    // Reapply filters and update UI
                    FilterItemsSource();
                    UpdateFilterPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnOperatorToggled: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles requests to clear all filters
        /// </summary>
        private void OnClearAllFiltersRequested(object sender, EventArgs e)
        {
            try
            {
                ClearAllFilters();
                UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnClearAllFiltersRequested: {ex.Message}");
            }
        }

        #endregion

        #region Cell Value Change Detection

        /// <summary>
        /// Gets the binding path for a DataGrid column
        /// </summary>
        private string GetColumnBindingPath(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding)
                {
                    return binding.Path.Path;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a snapshot key for tracking cell values during editing
        /// </summary>
        private string CreateSnapshotKey(object item, string bindingPath)
        {
            if (item == null || string.IsNullOrEmpty(bindingPath))
                return null;

            var itemIndex = Items.IndexOf(item);
            return $"{itemIndex}_{bindingPath}";
        }

        /// <summary>
        /// Extracts the edited value from the editing element and converts it to the correct type
        /// </summary>
        private object GetEditedValueFromElement(FrameworkElement editingElement, object originalValue)
        {
            if (editingElement == null)
                return null;

            object rawValue = null;

            // Handle TextBox (most common case)
            if (editingElement is TextBox textBox)
            {
                rawValue = textBox.Text;
            }
            // Handle CheckBox
            else if (editingElement is CheckBox checkBox)
            {
                rawValue = checkBox.IsChecked;
            }
            // Handle ComboBox
            else if (editingElement is ComboBox comboBox)
            {
                rawValue = comboBox.SelectedItem ?? comboBox.Text;
            }
            // Handle DatePicker
            else if (editingElement is DatePicker datePicker)
            {
                rawValue = datePicker.SelectedDate;
            }
            else
            {
                // For other custom controls, try to get the value from common value properties
                var properties = new[] { "Value", "SelectedItem", "Text", "Content" };
                foreach (var propName in properties)
                {
                    var prop = editingElement.GetType().GetProperty(propName);
                    if (prop != null && prop.CanRead)
                    {
                        try
                        {
                            rawValue = prop.GetValue(editingElement);
                            break;
                        }
                        catch
                        {
                            // Continue to next property
                        }
                    }
                }
            }

            // Convert the raw value to match the original value's type
            if (rawValue != null && originalValue != null)
            {
                try
                {
                    var targetType = originalValue.GetType();

                    // Handle nullable types
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        targetType = Nullable.GetUnderlyingType(targetType);
                    }

                    // Convert to target type
                    if (targetType == typeof(string))
                    {
                        return rawValue.ToString();
                    }
                    else if (rawValue is string stringValue)
                    {
                        // Convert string to target type
                        if (targetType == typeof(int))
                            return int.TryParse(stringValue, out int intVal) ? intVal : originalValue;
                        else if (targetType == typeof(double))
                            return double.TryParse(stringValue, out double doubleVal) ? doubleVal : originalValue;
                        else if (targetType == typeof(decimal))
                            return decimal.TryParse(stringValue, out decimal decimalVal) ? decimalVal : originalValue;
                        else if (targetType == typeof(DateTime))
                            return DateTime.TryParse(stringValue, out DateTime dateVal) ? dateVal : originalValue;
                        else if (targetType == typeof(bool))
                            return bool.TryParse(stringValue, out bool boolVal) ? boolVal : originalValue;
                        else
                        {
                            // Try using Convert.ChangeType for other types
                            return Convert.ChangeType(stringValue, targetType);
                        }
                    }
                    else
                    {
                        // Raw value is already the correct type or convertible
                        return Convert.ChangeType(rawValue, targetType);
                    }
                }
                catch
                {
                    // If conversion fails, return the raw value
                    return rawValue;
                }
            }

            return rawValue;
        }

        /// <summary>
        /// Forces the editing element to update its binding source
        /// </summary>
        private void ForceBindingUpdate(FrameworkElement editingElement)
        {
            if (editingElement == null) return;

            try
            {
                // For TextBox, update the Text binding
                if (editingElement is TextBox textBox)
                {
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                }
                // For CheckBox, update the IsChecked binding
                else if (editingElement is CheckBox checkBox)
                {
                    var binding = checkBox.GetBindingExpression(CheckBox.IsCheckedProperty);
                    binding?.UpdateSource();
                }
                // For ComboBox, update the SelectedItem/SelectedValue binding
                else if (editingElement is ComboBox comboBox)
                {
                    var binding = comboBox.GetBindingExpression(ComboBox.SelectedItemProperty) ??
                                 comboBox.GetBindingExpression(ComboBox.SelectedValueProperty);
                    binding?.UpdateSource();
                }
                // For DatePicker, update the SelectedDate binding
                else if (editingElement is DatePicker datePicker)
                {
                    var binding = datePicker.GetBindingExpression(DatePicker.SelectedDateProperty);
                    binding?.UpdateSource();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error forcing binding update: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal handler for cell value changes that updates caches and raises events.
        /// NOTE: When a column uses GridColumn.FilterMemberPath that differs from its Binding.Path,
        /// this method prioritizes finding the ColumnSearchBox by column reference to ensure
        /// cache updates work correctly even when the paths differ.
        /// </summary>
        private void OnCellValueChangedInternal(object item, DataGridColumn column, string bindingPath,
            object oldValue, object newValue, int rowIndex, int columnIndex)
        {
            try
            {
                // Find the corresponding column search box
                // Priority 1: Match by column reference (handles FilterMemberPath != Binding.Path scenarios)
                var columnSearchBox = DataColumns.FirstOrDefault(d => d.CurrentColumn == column);

                // Priority 2: Fallback to BindingPath matching (for backward compatibility)
                if (columnSearchBox == null)
                {
                    columnSearchBox = DataColumns.FirstOrDefault(d => d.BindingPath == bindingPath);
                }
                if (columnSearchBox?.SearchTemplateController != null)
                {
                    // Update column value caches
                    columnSearchBox.SearchTemplateController.RemoveColumnValue(oldValue);
                    columnSearchBox.SearchTemplateController.AddOrUpdateColumnValue(newValue);
                }

                // Invalidate collection context cache to refresh statistical calculations
                InvalidateCollectionContextCache();

                // Raise public event for external subscribers
                var eventArgs = new CellValueChangedEventArgs(item, column, bindingPath,
                    oldValue, newValue, rowIndex, columnIndex);
                CellValueChanged?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnCellValueChangedInternal: {ex.Message}");
            }
        }

        #endregion

        #region Select All Column Support

        /// <summary>
        /// Gets the binding path for a column, respecting FilterMemberPath, SortMemberPath, and Binding.Path priority
        /// </summary>
        /// <param name="column">The column to get the binding path for</param>
        /// <returns>The resolved binding path, or null if none can be determined</returns>
        internal string GetColumnBindingPathForSelectAll(DataGridColumn column)
        {
            if (column == null)
                return null;

            // Priority 1: FilterMemberPath
            string bindingPath = GridColumn.GetFilterMemberPath(column);

            // Priority 2: SortMemberPath
            if (string.IsNullOrEmpty(bindingPath))
                bindingPath = column.SortMemberPath;

            // Priority 3: Binding path
            if (string.IsNullOrEmpty(bindingPath) && column is DataGridBoundColumn boundColumn)
            {
                bindingPath = (boundColumn.Binding as Binding)?.Path?.Path;
            }

            return bindingPath;
        }

        /// <summary>
        /// Calculates the checkbox state for a select-all column based on the SelectAllScope setting.
        /// Returns: true (all non-null values are true), false (all non-null values are false),
        /// or null (mixed state or all null values)
        /// </summary>
        /// <param name="column">The column to calculate state for</param>
        /// <returns>The checkbox state: true, false, or null for indeterminate</returns>
        internal bool? CalculateSelectAllCheckboxState(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return null;

                string bindingPath = GetColumnBindingPathForSelectAll(column);
                if (string.IsNullOrEmpty(bindingPath))
                    return null;

                // Get the scope for this column to determine which items to evaluate
                SelectAllScope scope = GridColumn.GetSelectAllScope(column);

                // Get items based on scope
                var itemsToCheck = GetItemsForSelectAllScope(scope);
                if (itemsToCheck == null || !itemsToCheck.Any())
                    return null;

                // Calculate state using the scoped items
                return CalculateSelectAllCheckboxStateForItems(itemsToCheck, bindingPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating select-all checkbox state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Toggles all non-null boolean values in a column to the opposite state.
        /// Null values are preserved unchanged.
        /// Respects the SelectAllScope property to determine which items are affected.
        /// </summary>
        /// <param name="column">The column to toggle values in</param>
        internal void ToggleSelectAllColumn(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return;

                string bindingPath = GetColumnBindingPathForSelectAll(column);
                if (string.IsNullOrEmpty(bindingPath))
                    return;

                // Get the scope for this column
                SelectAllScope scope = GridColumn.GetSelectAllScope(column);

                // Get the items to operate on based on scope
                var itemsToToggle = GetItemsForSelectAllScope(scope);
                if (itemsToToggle == null || !itemsToToggle.Any())
                    return;

                // Calculate current state based on scoped items
                var currentState = CalculateSelectAllCheckboxStateForItems(itemsToToggle, bindingPath);

                // Determine new value:
                // - If all true (currentState == true), set all to false
                // - If all false (currentState == false), set all to true
                // - If mixed (currentState == null), set all to true (consistent behavior)
                bool newValue = currentState != true;

                // Save current selection state before making changes
                List<object> savedSelectedItems = null;
                List<DataGridCellInfo> savedSelectedCells = null;

                if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                {
                    // Save selected cells
                    savedSelectedCells = SelectedCells.ToList();
                }
                else
                {
                    // Save selected items
                    savedSelectedItems = SelectedItems.Cast<object>().ToList();
                }

                // Toggle all non-null values in the scoped items
                foreach (var item in itemsToToggle)
                {
                    var value = ReflectionHelper.GetPropValue(item, bindingPath);

                    // Only modify non-null boolean values
                    if (value != null && value is bool)
                    {
                        ReflectionHelper.SetPropValue(item, bindingPath, newValue);
                    }
                }

                // Refresh the view to show changes
                Items.Refresh();

                // Update filter caches if filtering is active
                if (HasActiveColumnFilters())
                {
                    InvalidateCollectionContextCache();
                }

                // Refilter the datagrid to apply the new values
                FilterItemsSource();
                UpdateFilterPanel();

                // Restore selection after all refresh operations complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                        {
                            // Restore cell selection
                            if (savedSelectedCells != null && savedSelectedCells.Count > 0)
                            {
                                SelectedCells.Clear();
                                foreach (var cellInfo in savedSelectedCells)
                                {
                                    // Only restore cells that still exist in the filtered view
                                    if (cellInfo.Item != null && Items.Contains(cellInfo.Item))
                                    {
                                        SelectedCells.Add(cellInfo);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Restore row selection
                            if (savedSelectedItems != null && savedSelectedItems.Count > 0)
                            {
                                SelectedItems.Clear();
                                foreach (var item in savedSelectedItems)
                                {
                                    // Only restore items that still exist in the filtered view
                                    if (Items.Contains(item))
                                    {
                                        SelectedItems.Add(item);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error restoring selection: {ex.Message}");
                    }
                }), DispatcherPriority.DataBind);

                // Sync ColumnSearchBox filter if UseCheckBoxInSearchBox is true and a filter is active
                SyncColumnSearchBoxFilterWithSelectAll(column, newValue);

                // Update row count display if using SelectedRows scope
                if (scope == SelectAllScope.SelectedRows)
                {
                    UpdateSelectAllRowCountDisplay(column, itemsToToggle.Count());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling select-all column: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the items to operate on based on the SelectAllScope
        /// </summary>
        /// <param name="scope">The scope defining which items to include</param>
        /// <returns>Collection of items to operate on</returns>
        private IEnumerable<object> GetItemsForSelectAllScope(SelectAllScope scope)
        {
            switch (scope)
            {
                case SelectAllScope.FilteredRows:
                    // Return currently visible/filtered items
                    return Items.Cast<object>();

                case SelectAllScope.SelectedRows:
                    // Return selected items (handles both row and cell selection)
                    if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                    {
                        // For cell-based selection, get unique row items from selected cells
                        var selectedItems = new HashSet<object>();
                        foreach (var cell in SelectedCells)
                        {
                            if (cell.Item != null)
                                selectedItems.Add(cell.Item);
                        }
                        return selectedItems;
                    }
                    else
                    {
                        // For row-based selection, return selected items
                        return SelectedItems.Cast<object>();
                    }

                case SelectAllScope.AllItems:
                    // Return all items from the original unfiltered source
                    if (originalItemsSource != null)
                        return originalItemsSource.Cast<object>();
                    return Enumerable.Empty<object>();

                default:
                    return Items.Cast<object>();
            }
        }

        /// <summary>
        /// Calculates the checkbox state for a specific set of items
        /// </summary>
        /// <param name="items">The items to evaluate</param>
        /// <param name="bindingPath">The property path to check</param>
        /// <returns>The checkbox state: true, false, or null for indeterminate</returns>
        private bool? CalculateSelectAllCheckboxStateForItems(IEnumerable<object> items, string bindingPath)
        {
            try
            {
                if (items == null || !items.Any())
                    return null;

                if (string.IsNullOrEmpty(bindingPath))
                    return null;

                int trueCount = 0;
                int falseCount = 0;
                int totalNonNull = 0;

                foreach (var item in items)
                {
                    var value = ReflectionHelper.GetPropValue(item, bindingPath);

                    if (value == null)
                        continue; // Skip null values

                    totalNonNull++;

                    if (value is bool boolValue)
                    {
                        if (boolValue)
                            trueCount++;
                        else
                            falseCount++;
                    }
                }

                // If no non-null values, return indeterminate
                if (totalNonNull == 0)
                    return null;

                // All non-null values are true
                if (trueCount == totalNonNull)
                    return true;

                // All non-null values are false
                if (falseCount == totalNonNull)
                    return false;

                // Mixed state
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating checkbox state for items: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronizes the ColumnSearchBox checkbox filter with the new select-all state
        /// </summary>
        /// <param name="column">The column to sync</param>
        /// <param name="newValue">The new boolean value that was applied</param>
        private void SyncColumnSearchBoxFilterWithSelectAll(DataGridColumn column, bool newValue)
        {
            try
            {
                // Check if UseCheckBoxInSearchBox is enabled for this column
                if (!GridColumn.GetUseCheckBoxInSearchBox(column))
                    return;

                // Find the ColumnSearchBox for this column
                var columnSearchBox = DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                if (columnSearchBox == null)
                    return;

                // Check if there's an active filter
                if (!columnSearchBox.HasActiveFilter)
                    return;

                // Update the FilterCheckboxState to match the new value
                // Setting this property will automatically update the filter
                if (newValue)
                    columnSearchBox.FilterCheckboxState = true;
                else
                    columnSearchBox.FilterCheckboxState = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing ColumnSearchBox filter: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the row count display next to the select-all checkbox
        /// </summary>
        /// <param name="column">The column to update</param>
        /// <param name="count">The count of affected rows</param>
        private void UpdateSelectAllRowCountDisplay(DataGridColumn column, int count)
        {
            try
            {
                var columnHeader = FindColumnHeader(column);
                if (columnHeader == null)
                    return;

                // Find the row count TextBlock in the header template
                var countTextBlock = FindVisualChild<TextBlock>(columnHeader, "PART_SelectAllRowCount");
                if (countTextBlock != null)
                {
                    countTextBlock.Text = $"({count})";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating select-all row count display: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the select-all checkbox state for a specific column header.
        /// This method is called to synchronize the checkbox visual state with the data.
        /// </summary>
        /// <param name="column">The column to update the checkbox for</param>
        internal void UpdateSelectAllCheckboxForColumn(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return;

                // Find the column header in the visual tree
                var columnHeader = FindColumnHeader(column);
                if (columnHeader == null)
                    return;

                // Find the select-all checkbox in the header template
                var checkbox = FindVisualChild<CheckBox>(columnHeader, "PART_SelectAllCheckBox");
                if (checkbox == null)
                    return;

                // Calculate and set the checkbox state
                checkbox.IsChecked = CalculateSelectAllCheckboxState(column);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating select-all checkbox: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the state of all select-all checkboxes across all columns
        /// </summary>
        private void UpdateAllSelectAllCheckboxStates()
        {
            try
            {
                // Find all column headers
                var headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return;

                var headers = FindVisualChildren<DataGridColumnHeader>(headersPresenter).ToList();

                foreach (var header in headers)
                {
                    var column = header.Column;
                    if (column == null)
                        continue;

                    // Check if this column has IsSelectAllColumn enabled
                    bool isSelectAllColumn = GridColumn.GetIsSelectAllColumn(column);
                    if (!isSelectAllColumn)
                        continue;

                    // Find the select-all checkbox in the header
                    var checkbox = FindVisualChild<CheckBox>(header, "PART_SelectAllCheckBox");
                    if (checkbox == null || checkbox.Visibility != Visibility.Visible)
                        continue;

                    // Update the checkbox state
                    checkbox.IsChecked = CalculateSelectAllCheckboxState(column);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating all select-all checkbox states: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds a DataGridColumnHeader for a specific column
        /// </summary>
        private DataGridColumnHeader FindColumnHeader(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return null;

                // Get the column headers presenter
                var headersPresenter = FindVisualChild<DataGridColumnHeadersPresenter>(this);
                if (headersPresenter == null)
                    return null;

                // Find all column headers
                var headers = FindVisualChildren<DataGridColumnHeader>(headersPresenter);

                // Find the header for this specific column
                return headers.FirstOrDefault(h => h.Column == column);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds a child of a specific type with a specific name in the visual tree
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : DependencyObject
        {
            try
            {
                if (parent == null)
                    return null;

                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                    {
                        if (string.IsNullOrEmpty(name))
                            return typedChild;

                        if (child is FrameworkElement element && element.Name == name)
                            return typedChild;
                    }

                    var result = FindVisualChild<T>(child, name);
                    if (result != null)
                        return result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds all children of a specific type in the visual tree
        /// </summary>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();

            try
            {
                if (parent == null)
                    return children;

                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                        children.Add(typedChild);

                    children.AddRange(FindVisualChildren<T>(child));
                }
            }
            catch
            {
                // Return what we found so far
            }

            return children;
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Event arguments for cell value change notifications
    /// </summary>
    public class CellValueChangedEventArgs : EventArgs
    {
        public object Item { get; }
        public DataGridColumn Column { get; }
        public string BindingPath { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public int RowIndex { get; }
        public int ColumnIndex { get; }

        public CellValueChangedEventArgs(object item, DataGridColumn column, string bindingPath,
            object oldValue, object newValue, int rowIndex, int columnIndex)
        {
            Item = item;
            Column = column;
            BindingPath = bindingPath;
            OldValue = oldValue;
            NewValue = newValue;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }
    }
}