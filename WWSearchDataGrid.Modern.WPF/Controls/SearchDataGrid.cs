using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using WWSearchDataGrid.Modern.Core;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.WPF
{

    /// <summary>
    /// Modern implementation of the SearchDataGrid
    /// </summary>
    public class SearchDataGrid : DataGrid
    {
        #region Fields

        private TokenSource tokenSource = new TokenSource();
        private ObservableCollection<SearchControl> dataColumns = new ObservableCollection<SearchControl>();
        private System.Collections.IEnumerable originalItemsSource;
        private bool initialUpdateLayoutCompleted;
        private SearchTemplateController globalFilterController;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SearchFilterProperty =
            DependencyProperty.Register("SearchFilter", typeof(Predicate<object>), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHasItemsProperty =
            DependencyProperty.Register("ActualHasItems", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnActualHasItemsChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data columns collection
        /// </summary>
        public ObservableCollection<SearchControl> DataColumns
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
        /// Gets the global filter controller
        /// </summary>
        public SearchTemplateController GlobalFilterController
        {
            get
            {
                if (globalFilterController == null)
                {
                    globalFilterController = new SearchTemplateController(typeof(SearchTemplate));

                    // Initialize with first column if available
                    var firstColumn = DataColumns.FirstOrDefault();
                    if (firstColumn != null)
                    {
                        globalFilterController.ColumnName = "Global Filter";
                    }
                }
                return globalFilterController;
            }
        }

        /// <summary>
        /// Gets the dictionary of column property info
        /// </summary>
        internal Dictionary<string, System.Reflection.PropertyInfo> ColumnPropertyInfo { get; } = new Dictionary<string, System.Reflection.PropertyInfo>();
        
        /// <summary>
        /// Gets the original unfiltered items source
        /// </summary>
        public System.Collections.IEnumerable OriginalItemsSource => originalItemsSource;

        #endregion

        #region Commands

        public ICommand OpenGlobalFilterCommand => new RelayCommand(_ => ShowGlobalFilterWindow());

        #endregion Commands

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
        /// Event raised when items source is filtered
        /// </summary>
        public event EventHandler ItemsSourceFiltered;

        #endregion

        #region Constructor

        public SearchDataGrid() : base()
        {
            // Add binding for DataGrid.Items attached property changes
            DependencyPropertyDescriptor
                .FromProperty(ItemsControl.ItemsSourceProperty, typeof(SearchDataGrid))
                .AddValueChanged(this, (s, e) => UpdateHasItemsProperty());
        }

        #endregion

        #region Methods

        private static void OnAdvancedFilterModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                // Update the visibility of the advanced filter button in column headers
                grid.UpdateColumnHeaderFilterVisibility();
            }
        }

        /// <summary>
        /// Initialize the global filter controller with column data
        /// </summary>
        private void InitializeGlobalFilterController()
        {
            var controller = GlobalFilterController;

            // Load column data for each column
            foreach (var column in DataColumns)
            {
                if (!string.IsNullOrEmpty(column.BindingPath))
                {
                    var columnValues = new HashSet<object>();
                    foreach (var item in Items)
                    {
                        var value = ReflectionHelper.GetPropValue(item, column.BindingPath);
                        columnValues.Add(value);
                    }

                    controller.LoadColumnData(
                        column.CurrentColumn.Header,
                        columnValues,
                        null,
                        column.BindingPath);
                }
            }
        }

        /// <summary>
        /// Updates the visibility of advanced filter buttons in column headers
        /// </summary>
        private void UpdateColumnHeaderFilterVisibility()
        {
            // This method will be called when the AdvancedFilterMode changes
            // In a real implementation, you might want to update the XAML bindings instead
        }

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
        /// When items source changes, notify controls
        /// </summary>
        protected override void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            // Store the original items source
            originalItemsSource = newValue;

            // Register for collection changed events if the source supports it
            UnregisterCollectionChangedEvent(oldValue);
            RegisterCollectionChangedEvent(newValue);

            if (newValue != null)
            {
                // Update ActualHasItems property
                UpdateHasItemsProperty();

                // Notify controls that items source has changed
                ItemsSourceChanged?.Invoke(this, EventArgs.Empty);

                // Apply any existing filters
                if (Items.Filter != null)
                {
                    FilterItemsSource();
                }
                UpdateLayout();
            }
            else
            {
                // If items source is null, set ActualHasItems to false
                ActualHasItems = false;
            }
        }

        private void RegisterCollectionChangedEvent(System.Collections.IEnumerable collection)
        {
            if (collection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        private void UnregisterCollectionChangedEvent(System.Collections.IEnumerable collection)
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
            CollectionChanged?.Invoke(this, e);
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
                if (originalItemsSource is System.Collections.ICollection collection)
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
        /// Apply filters to the items source
        /// </summary>
        /// <param name="delay">Optional delay before filtering</param>
        public async void FilterItemsSource(int delay = 0)
        {
            try
            {
                // Create token source for cancellation
                var cts = tokenSource.GetNewCancellationTokenSource();

                // Wait for delay if requested
                if (delay > 0)
                {
                    await System.Threading.Tasks.Task.Delay(delay);
                }

                // If cancelled, return
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                // Commit any edits
                CommitEdit(DataGridEditingUnit.Row, true);

                // Per-column mode - each column has its own filter
                var activeFilters = DataColumns.Where(d => d.SearchTemplateController?.HasCustomExpression == true);
                Items.Filter = item => activeFilters.All(f => EvaluateFilter(item, f));

                // Update search filter property
                SearchFilter = Items.Filter;

                // Notify that items have been filtered
                ItemsSourceFiltered?.Invoke(this, EventArgs.Empty);

                // Remove token source
                tokenSource.RemoveCancellationTokenSource(cts);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering items: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the global filter window
        /// </summary>
        private void ShowGlobalFilterWindow()
        {
            // Initialize the global filter controller if needed
            InitializeGlobalFilterController();

            var window = new Window
            {
                Title = "Advanced Filter (Global)",
                Width = 800,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var filterControl = new AdvancedFilterControl
            {
                SearchTemplateController = GlobalFilterController,
                DataContext = this
            };

            window.Content = filterControl;
            window.Closed += (s, e) =>
            {
                // Apply filters after the window is closed
                if (GlobalFilterController.HasCustomExpression)
                {
                    FilterItemsSource();
                }
            };

            window.ShowDialog();
        }

        /// <summary>
        /// Evaluate a filter against an item for per-column filtering
        /// </summary>
        private bool EvaluateFilter(object item, SearchControl filter)
        {
            try
            {
                // Check if this is grouped filtering
                if (filter.GroupedFilterCombinations != null && !string.IsNullOrEmpty(filter.GroupByColumnPath))
                {
                    // Grouped filtering: check both the group column and the current column
                    var groupValue = ReflectionHelper.GetPropValue(item, filter.GroupByColumnPath);
                    var currentValue = ReflectionHelper.GetPropValue(item, filter.BindingPath);
                    
                    // Check if this item matches any of the selected group-child combinations
                    return filter.GroupedFilterCombinations.Any(c => 
                        Equals(c.GroupKey, groupValue) && Equals(c.ChildValue, currentValue));
                }
                else
                {
                    // Standard filtering: get the property value and evaluate
                    object propertyValue = ReflectionHelper.GetPropValue(item, filter.BindingPath);
                    return filter.SearchTemplateController.FilterExpression(propertyValue);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        public void ClearAllFilters()
        {
            // Clear per-column filters
            foreach (var control in DataColumns)
            {
                control.ClearFilter();
            }

            // Clear global filter if applicable
            if (globalFilterController != null)
            {
                globalFilterController.SearchGroups.Clear();
                globalFilterController.AddSearchGroup();
                globalFilterController.HasCustomExpression = false;
            }

            // Clear the filter
            Items.Filter = null;
            SearchFilter = null;

            // Notify that items have been filtered
            ItemsSourceFiltered?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Save filters to file
        /// </summary>
        public void SaveFilters()
        {
            // Show save dialog
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".json",
                Filter = "Filter files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Create filter data to save
                    object filterData;

                     // Save global filter
                    filterData = new
                    {
                        FilterMode = "Global",
                        SearchGroups = GlobalFilterController.SearchGroups
                    };

                    // Serialize to JSON
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(filterData,
                        Newtonsoft.Json.Formatting.Indented);

                    // Save to file
                    System.IO.File.WriteAllText(dialog.FileName, json);

                    MessageBox.Show("Filters saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving filters: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Load filters from file
        /// </summary>
        public void LoadFilters()
        {
            // Show open dialog
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "Filter files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Read the JSON file
                    string json = System.IO.File.ReadAllText(dialog.FileName);

                    // Try to determine if this is a global or per-column filter
                    bool isGlobal = json.Contains("\"FilterMode\":\"Global\"");


                    // Clear existing filters
                    ClearAllFilters();


                    // Deserialize from JSON
                    var filterData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(json);

                    // Apply filters to matching columns
                    foreach (var filter in filterData)
                    {
                        // Find matching column
                        var column = DataColumns.FirstOrDefault(c =>
                            c.CurrentColumn.Header.ToString() == (string)filter.ColumnName &&
                            c.BindingPath == (string)filter.BindingPath);

                        if (column != null)
                        {
                            // Apply the filter
                            // (Note: This needs more detailed implementation to restore search groups)
                            column.SearchTemplateController.HasCustomExpression = true;
                        }
                    }

                    // Apply the filters
                    FilterItemsSource();

                    MessageBox.Show("Filters loaded successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading filters: {ex.Message}");
                }
            }
        }

        #endregion
    }
}