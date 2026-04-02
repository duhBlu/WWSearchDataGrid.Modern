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
    public partial class SearchDataGrid : DataGrid
    {
        #region Fields

        private readonly ObservableCollection<ColumnSearchBox> dataColumns = new ObservableCollection<ColumnSearchBox>();
        private IEnumerable originalItemsSource;
        private bool initialUpdateLayoutCompleted;

        // Collection context caching for performance optimization
        private readonly Dictionary<string, CollectionContext> _collectionContextCache = new Dictionary<string, CollectionContext>();
        private List<object> _materializedDataSource;
        private readonly object _contextCacheLock = new object();

        // Asynchronous filtering support
        private CancellationTokenSource _filterCancellationTokenSource;

        // Cell value change detection support
        private readonly Dictionary<string, object> _cellValueSnapshots = new Dictionary<string, object>();

        // Auto-sizing support
        private readonly Dictionary<DataGridColumn, DataGridLength> _originalColumnWidths = new Dictionary<DataGridColumn, DataGridLength>();
        private ScrollViewer _scrollViewer;

        // Column Chooser support
        private ColumnChooser _columnChooser;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SearchFilterProperty =
            DependencyProperty.Register("SearchFilter", typeof(Predicate<object>), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHasItemsProperty =
            DependencyProperty.Register("ActualHasItems", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnActualHasItemsChanged));

        /// <summary>
        /// Dependency property for EnableRuleFiltering
        /// </summary>
        public static readonly DependencyProperty EnableRuleFilteringProperty =
            DependencyProperty.Register("EnableRuleFiltering", typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEnableRuleFilteringChanged));

        /// <summary>
        /// Dependency property for AutoSizeColumns
        /// </summary>
        public static readonly DependencyProperty AutoSizeColumnsProperty =
            DependencyProperty.Register("AutoSizeColumns", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnAutoSizeColumnsChanged));

        /// <summary>
        /// Dependency property for IsColumnChooserVisible
        /// </summary>
        public static readonly DependencyProperty IsColumnChooserVisibleProperty =
            DependencyProperty.Register("IsColumnChooserVisible", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnIsColumnChooserVisibleChanged));

        /// <summary>
        /// Dependency property for IsColumnChooserEnabled
        /// </summary>
        public static readonly DependencyProperty IsColumnChooserEnabledProperty =
            DependencyProperty.Register("IsColumnChooserEnabled", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnIsColumnChooserEnabledChanged));

        /// <summary>
        /// Dependency property for IsColumnChooserConfinedToGrid
        /// </summary>
        public static readonly DependencyProperty IsColumnChooserConfinedToGridProperty =
            DependencyProperty.Register("IsColumnChooserConfinedToGrid", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnIsColumnChooserConfinedToGridChanged));

        /// <summary>
        /// Dependency property for LastFocusedColumn. Persists the most recently focused
        /// column so it remains available when focus leaves the grid.
        /// </summary>
        public static readonly DependencyProperty LastFocusedColumnProperty =
            DependencyProperty.Register("LastFocusedColumn", typeof(DataGridColumn), typeof(SearchDataGrid),
                new PropertyMetadata(null));

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
        /// Gets or sets whether rule filtering is enabled at the grid level.
        /// When false, all columns use ColumnSearchBox filtering only, and only single text filter per column.
        /// When true, per-column EnableRuleFiltering settings are respected.
        /// </summary>
        public bool EnableRuleFiltering
        {
            get { return (bool)GetValue(EnableRuleFilteringProperty); }
            set { SetValue(EnableRuleFilteringProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether columns should automatically size to fit their content.  
        /// When set to <c>true</c>, column widths are dynamically adjusted to fit the visible cell content,  
        /// ignoring any static width definitions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Column widths are recalculated during scrolling to accommodate newly visible content,  
        /// ensuring that data remains fully visible without manual resizing.
        /// </para>
        /// <para>
        /// This behavior still respects each column's <see cref="DataGridColumn.MinWidth"/>  
        /// and <see cref="DataGridColumn.MaxWidth"/> constraints.
        /// </para>
        /// </remarks>

        public bool AutoSizeColumns
        {
            get { return (bool)GetValue(AutoSizeColumnsProperty); }
            set { SetValue(AutoSizeColumnsProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the Column Chooser window is visible.
        /// When set to true, displays a non-modal window allowing users to show/hide columns.
        /// This property is overridden by IsColumnChooserEnabled - if IsColumnChooserEnabled is false,
        /// the column chooser cannot be shown.
        /// </summary>
        public bool IsColumnChooserVisible
        {
            get { return (bool)GetValue(IsColumnChooserVisibleProperty); }
            set { SetValue(IsColumnChooserVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the Column Chooser feature is enabled.
        /// When set to false, hides Column Chooser menu items from context menus and prevents
        /// the column chooser from being shown. Default is true.
        /// </summary>
        public bool IsColumnChooserEnabled
        {
            get { return (bool)GetValue(IsColumnChooserEnabledProperty); }
            set { SetValue(IsColumnChooserEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the Column Chooser window is confined to the grid's viewport bounds.
        /// When set to true, the Column Chooser window cannot be dragged outside the grid's visible area.
        /// When set to false, the window can be moved freely. Default is false.
        /// </summary>
        public bool IsColumnChooserConfinedToGrid
        {
            get { return (bool)GetValue(IsColumnChooserConfinedToGridProperty); }
            set { SetValue(IsColumnChooserConfinedToGridProperty, value); }
        }

        /// <summary>
        /// Gets the last column that had focus. Persists when focus leaves the grid,
        /// so external panels can continue displaying column properties.
        /// </summary>
        public DataGridColumn LastFocusedColumn
        {
            get => (DataGridColumn)GetValue(LastFocusedColumnProperty);
            private set => SetValue(LastFocusedColumnProperty, value);
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

        // Track template FilterPanel reference for event cleanup on re-template
        private FilterPanel _templateFilterPanel;

        public SearchDataGrid() : base()
        {
            // Note: Do not use DependencyPropertyDescriptor.AddValueChanged here - it creates a
            // strong reference that prevents GC. OnItemsSourceChanged override handles this already.

            // Initialize context menu functionality
            this.InitializeContextMenu();

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

            // Unsubscribe editing events before re-subscribing to prevent duplicate handlers on re-template
            this.BeginningEdit -= OnBeginningEdit;
            this.RowEditEnding -= OnRowEditEnding;
            this.CellEditEnding -= OnCellEditEnding;

            this.BeginningEdit += OnBeginningEdit;
            this.RowEditEnding += OnRowEditEnding;
            this.CellEditEnding += OnCellEditEnding;

            if (FilterPanel == null)
            {
                FilterPanel = new FilterPanel();
            }

            // Unsubscribe from previous template FilterPanel if re-templating
            if (_templateFilterPanel != null)
            {
                _templateFilterPanel.FiltersEnabledChanged -= OnFiltersEnabledChanged;
                _templateFilterPanel.FilterRemoved -= OnFilterRemoved;
                _templateFilterPanel.ValueRemovedFromToken -= OnValueRemovedFromToken;
                _templateFilterPanel.OperatorToggled -= OnOperatorToggled;
                _templateFilterPanel.ClearAllFiltersRequested -= OnClearAllFiltersRequested;
            }

            // Get the FilterPanel template part and connect it to our FilterPanel instance
            if (GetTemplateChild("PART_FilterPanel") is FilterPanel templateFilterPanel && templateFilterPanel != null)
            {
                // Copy the current state from our FilterPanel to the template FilterPanel
                templateFilterPanel.FiltersEnabled = FilterPanel.FiltersEnabled;
                templateFilterPanel.UpdateActiveFilters(FilterPanel.ActiveFilters);

                // Wire up events from template FilterPanel using named methods (not lambdas) for cleanup
                templateFilterPanel.FiltersEnabledChanged += OnFiltersEnabledChanged;
                templateFilterPanel.FilterRemoved += OnFilterRemoved;
                templateFilterPanel.ValueRemovedFromToken += OnValueRemovedFromToken;
                templateFilterPanel.OperatorToggled += OnOperatorToggled;
                templateFilterPanel.ClearAllFiltersRequested += OnClearAllFiltersRequested;

                // Track reference for cleanup and replace FilterPanel property
                _templateFilterPanel = templateFilterPanel;
                FilterPanel = templateFilterPanel;
            }

            // Set up select-all column headers when template is applied
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetupSelectAllColumnHeaders();
            }), DispatcherPriority.Loaded);
        }

        #endregion

        #region Methods

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        protected override void OnCurrentCellChanged(EventArgs e)
        {
            base.OnCurrentCellChanged(e);

            // Persist the column so it survives focus leaving the grid
            if (CurrentCell.Column != null)
                LastFocusedColumn = CurrentCell.Column;
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


        private static void OnIsColumnChooserVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                bool isVisible = (bool)e.NewValue;

                if (isVisible)
                {
                    grid.ShowColumnChooser();
                }
                else
                {
                    grid.HideColumnChooser();
                }
            }
        }

        private static void OnIsColumnChooserEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                bool isEnabled = (bool)e.NewValue;

                // If disabled, hide the column chooser if it's currently visible
                if (!isEnabled && grid.IsColumnChooserVisible)
                {
                    grid.IsColumnChooserVisible = false;
                }
            }
        }

        private static void OnIsColumnChooserConfinedToGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                bool isConfined = (bool)e.NewValue;

                // Update the existing column chooser if it exists
                if (grid._columnChooser != null)
                {
                    grid._columnChooser.IsConfinedToGrid = isConfined;
                }
            }
        }

        private static void OnEnableRuleFilteringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                // Notify all column search boxes to update their visual state
                // This ensures the filter UI reflects the new EnableRuleFiltering setting
                grid.RefreshColumnFilterStates();
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