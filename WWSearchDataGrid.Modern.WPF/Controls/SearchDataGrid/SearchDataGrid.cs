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
using System.Windows.Automation.Peers;
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
        /// Dependency property for EnableLiveScrolling. When true, the grid content updates
        /// in real-time while dragging the scrollbar thumb instead of waiting for release.
        /// Defaults to true. Disable for very large datasets (100k+) if scrolling feels choppy.
        /// </summary>
        public static readonly DependencyProperty EnableLiveScrollingProperty =
            DependencyProperty.Register("EnableLiveScrolling", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(true, OnEnableLiveScrollingChanged));

        /// <summary>
        /// Dependency property for LastFocusedColumn. Persists the most recently focused
        /// column so it remains available when focus leaves the grid.
        /// </summary>
        public static readonly DependencyProperty LastFocusedColumnProperty =
            DependencyProperty.Register("LastFocusedColumn", typeof(DataGridColumn), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty LastFocusedGridColumnProperty =
            DependencyProperty.Register("LastFocusedGridColumn", typeof(GridColumn), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>
        /// Backing key for the <see cref="GridColumns"/> dependency property.
        /// The collection is read-only from external code; internal logic populates it
        /// via the CLR property or XAML collection syntax.
        /// </summary>
        private static readonly DependencyPropertyKey GridColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(GridColumns),
                typeof(FreezableCollection<GridColumn>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="GridColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GridColumnsProperty = GridColumnsPropertyKey.DependencyProperty;

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
        /// Gets or sets whether the grid content updates in real-time while dragging the
        /// scrollbar thumb. When false, scrolling is deferred until thumb release.
        /// Defaults to true. Disable for very large datasets (100k+ rows) if scrolling stutters.
        /// </summary>
        public bool EnableLiveScrolling
        {
            get => (bool)GetValue(EnableLiveScrollingProperty);
            set => SetValue(EnableLiveScrollingProperty, value);
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
        /// Gets the <see cref="GridColumn"/> descriptor for the last focused column.
        /// Updates automatically when the focused cell changes.
        /// </summary>
        public GridColumn LastFocusedGridColumn
        {
            get => (GridColumn)GetValue(LastFocusedGridColumnProperty);
            private set => SetValue(LastFocusedGridColumnProperty, value);
        }

        /// <summary>
        /// Gets the collection of <see cref="GridColumn"/> descriptors.
        /// When this collection is populated, the grid auto-generates internal
        /// <see cref="DataGridColumn"/> instances from each descriptor.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;sdg:SearchDataGrid.GridColumns&gt;
        ///     &lt;sdg:GridColumn FieldName="OrderNumber" Header="Order #" Width="80"
        ///                     DefaultSearchMode="StartsWith" EnableRuleFiltering="False" /&gt;
        /// &lt;/sdg:SearchDataGrid.GridColumns&gt;
        /// </code>
        /// </example>
        public FreezableCollection<GridColumn> GridColumns
        {
            get => (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
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
            // Initialize the GridColumns collection so XAML can populate it immediately
            var gridColumns = new FreezableCollection<GridColumn>();
            SetValue(GridColumnsPropertyKey, gridColumns);
            SubscribeToGridColumnsChanged(gridColumns);

            // Initialize context menu functionality
            this.InitializeContextMenu();

            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesCommand.Execute(this)), Key.C, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand.Execute(this)), Key.C, ModifierKeys.Control | ModifierKeys.Shift));

            // Subscribe to selection change events to update row count display
            this.SelectionChanged += OnSelectionChanged;
            this.SelectedCellsChanged += OnSelectedCellsChanged;

            // Generate columns from GridColumns descriptors once the control is loaded
            Loaded += OnSearchDataGridLoaded;
        }

        #endregion

        #region GridColumns Support

        /// <summary>
        /// Tracks whether columns have already been generated from <see cref="GridColumns"/>
        /// to prevent duplicate generation on repeated Loaded events.
        /// </summary>
        private bool _gridColumnsGenerated;

        /// <summary>
        /// Finds the <see cref="GridColumn"/> descriptor that generated the given
        /// <see cref="DataGridColumn"/>, or null if not found.
        /// </summary>
        internal GridColumn FindGridColumnDescriptor(DataGridColumn column)
        {
            if (column == null)
                return null;

            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null || descriptors.Count == 0)
                return null;

            foreach (var descriptor in descriptors)
            {
                if (descriptor.InternalColumn == column)
                    return descriptor;
            }
            return null;
        }

        /// <summary>
        /// Subscribes to collection-changed notifications on the <see cref="GridColumns"/> collection
        /// so that runtime additions/removals are reflected in the grid.
        /// </summary>
        private void SubscribeToGridColumnsChanged(FreezableCollection<GridColumn> collection)
        {
            ((INotifyCollectionChanged)collection).CollectionChanged += OnGridColumnsCollectionChanged;
        }

        /// <summary>
        /// Handles the Loaded event to generate columns from <see cref="GridColumns"/> descriptors.
        /// </summary>
        private void OnSearchDataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (!_gridColumnsGenerated)
            {
                GenerateColumnsFromDescriptors();
            }
        }

        /// <summary>
        /// Handles additions and removals in the <see cref="GridColumns"/> collection at runtime.
        /// </summary>
        private void OnGridColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (GridColumn descriptor in e.NewItems)
                        {
                            descriptor.Owner = this;
                            var column = descriptor.CreateDataGridColumn();
                            if (column != null)
                            {
                                // Insert at the correct position if possible
                                int insertIndex = e.NewStartingIndex >= 0 && e.NewStartingIndex < Columns.Count
                                    ? e.NewStartingIndex
                                    : Columns.Count;
                                Columns.Insert(insertIndex, column);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (GridColumn descriptor in e.OldItems)
                        {
                            if (descriptor.InternalColumn != null)
                            {
                                Columns.Remove(descriptor.InternalColumn);
                                descriptor.InternalColumn = null;
                            }
                            descriptor.Owner = null;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // Remove old
                    if (e.OldItems != null)
                    {
                        foreach (GridColumn descriptor in e.OldItems)
                        {
                            if (descriptor.InternalColumn != null)
                            {
                                Columns.Remove(descriptor.InternalColumn);
                                descriptor.InternalColumn = null;
                            }
                            descriptor.Owner = null;
                        }
                    }
                    // Add new
                    if (e.NewItems != null)
                    {
                        foreach (GridColumn descriptor in e.NewItems)
                        {
                            descriptor.Owner = this;
                            var column = descriptor.CreateDataGridColumn();
                            if (column != null)
                                Columns.Add(column);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // Remove all previously generated columns
                    RemoveGeneratedColumns();
                    _gridColumnsGenerated = false;
                    // Re-generate if collection still has items
                    GenerateColumnsFromDescriptors();
                    break;
            }
        }

        /// <summary>
        /// Generates internal <see cref="DataGridColumn"/> instances from all <see cref="GridColumn"/>
        /// descriptors in the <see cref="GridColumns"/> collection and adds them to <see cref="DataGrid.Columns"/>.
        /// </summary>
        private void GenerateColumnsFromDescriptors()
        {
            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null || descriptors.Count == 0)
                return;

            // If GridColumns is populated, we manage Columns. Warn if user also added manual columns.
            if (Columns.Count > 0)
            {
                Debug.WriteLine(
                    "SearchDataGrid: GridColumns is populated but Columns already contains items. " +
                    "GridColumns will manage the Columns collection — manual columns may be overwritten.");
                Columns.Clear();
            }

            foreach (var descriptor in descriptors)
            {
                descriptor.Owner = this;
                var column = descriptor.CreateDataGridColumn();
                if (column != null)
                    Columns.Add(column);
            }

            _gridColumnsGenerated = true;
        }

        /// <summary>
        /// Removes all columns that were generated from <see cref="GridColumn"/> descriptors.
        /// </summary>
        private void RemoveGeneratedColumns()
        {
            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null)
                return;

            foreach (var descriptor in descriptors)
            {
                if (descriptor.InternalColumn != null)
                {
                    Columns.Remove(descriptor.InternalColumn);
                    descriptor.InternalColumn = null;
                }
                descriptor.Owner = null;
            }
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

            // Initialize scroll velocity tracking and enhancement infrastructure
            InitializeScrollInfrastructure();
        }

        #endregion

        #region Methods

        protected override AutomationPeer OnCreateAutomationPeer()
            => new FrameworkElementAutomationPeer(this);

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        protected override void OnCurrentCellChanged(EventArgs e)
        {
            base.OnCurrentCellChanged(e);

            // Persist the column so it survives focus leaving the grid
            if (CurrentCell.Column != null)
            {
                LastFocusedColumn = CurrentCell.Column;
                LastFocusedGridColumn = FindGridColumnDescriptor(CurrentCell.Column);
            }
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

            // Hide the placeholder row used to preserve horizontal scroll extent
            if (IsPlaceholderItem(e.Row.Item))
            {
                ConfigurePlaceholderRow(e.Row);
            }
            else
            {
                HandleRowAnimationOnLoadingRow(e.Row);
            }

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
                grid.RefreshColumnFilterStates();
            }
        }

        private static void OnEnableLiveScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                // IsDeferredScrollingEnabled is the inverse of EnableLiveScrolling
                grid.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, !(bool)e.NewValue);
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

                // Clear cell value snapshots and placeholder state when data source changes
                _cellValueSnapshots.Clear();
                ClearPlaceholderState();

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

        #region Column Chooser

        /// <summary>
        /// Shows the Column Chooser window
        /// </summary>
        private void ShowColumnChooser()
        {
            try
            {
                // Don't show if the feature is disabled
                if (!IsColumnChooserEnabled)
                {
                    IsColumnChooserVisible = false;
                    return;
                }

                // Create the ColumnChooser instance if it doesn't exist
                if (_columnChooser == null)
                {
                    _columnChooser = new ColumnChooser
                    {
                        SourceDataGrid = this,
                        IsConfinedToGrid = IsColumnChooserConfinedToGrid
                    };

                    // When the column chooser window closes, update the property
                    _columnChooser.Unloaded += (s, e) =>
                    {
                        IsColumnChooserVisible = false;
                    };
                }

                // Show the non-modal window
                _columnChooser.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing column chooser: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the Column Chooser window
        /// </summary>
        private void HideColumnChooser()
        {
            try
            {
                _columnChooser?.Close();
                _columnChooser = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding column chooser: {ex.Message}");
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