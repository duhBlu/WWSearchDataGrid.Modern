using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using System.Reflection;

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
        /// When true, focusing any cell whose column is editable immediately enters edit mode.
        /// Individual columns can override this via <see cref="GridColumn.EditOnFocus"/> (set to
        /// true or false to opt in or out per column). Defaults to false — preserving the WPF
        /// "click to select, click again to edit" convention.
        /// </summary>
        public static readonly DependencyProperty EditOnFocusProperty =
            DependencyProperty.Register("EditOnFocus", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false));

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

        /// <summary>
        /// Backing key for <see cref="VisualOrderedColumns"/>. Kept in sync with
        /// <see cref="DataGrid.Columns"/> but always sorted by <see cref="DataGridColumn.DisplayIndex"/>.
        /// Used by the vertical-gridline overlay so gridlines move with the visual column order
        /// when the user reorders columns.
        /// </summary>
        private static readonly DependencyPropertyKey VisualOrderedColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(VisualOrderedColumns),
                typeof(ObservableCollection<DataGridColumn>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty VisualOrderedColumnsProperty = VisualOrderedColumnsPropertyKey.DependencyProperty;

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
        /// Gets the columns ordered by <see cref="DataGridColumn.DisplayIndex"/>. Maintained in
        /// response to <c>Columns</c> changes and column reordering. The vertical gridline overlay
        /// binds to this so gridlines track the visual column order rather than the raw collection.
        /// </summary>
        public ObservableCollection<DataGridColumn> VisualOrderedColumns
        {
            get { return (ObservableCollection<DataGridColumn>)GetValue(VisualOrderedColumnsProperty); }
        }

        /// <summary>
        /// Grid-wide default for <see cref="GridColumn.EditOnFocus"/>. When a column doesn't set
        /// its own <c>EditOnFocus</c>, this value is used. See <see cref="EditOnFocusProperty"/>.
        /// </summary>
        public bool EditOnFocus
        {
            get => (bool)GetValue(EditOnFocusProperty);
            set => SetValue(EditOnFocusProperty, value);
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

            // Make the library's editor element styles (SdgEditTextBoxStyle / SdgEditComboBoxStyle /
            // SdgDisplayTextBlockStyle / etc.) findable from cell templates. Theme dictionaries
            // (Themes/Generic.xaml) feed the default-style system but don't participate in keyed
            // DynamicResource / FindResource lookups, so we explicitly merge the dictionary into
            // Application.Resources on first SearchDataGrid creation. Idempotent across instances.
            EnsureEditSettingsResourcesMerged();

            // Maintain a DisplayIndex-ordered mirror of Columns for the gridline overlay.
            SetValue(VisualOrderedColumnsPropertyKey, new ObservableCollection<DataGridColumn>());
            ((INotifyCollectionChanged)Columns).CollectionChanged += (_, __) => RebuildVisualOrderedColumns();
            ColumnDisplayIndexChanged += (_, __) => RebuildVisualOrderedColumns();

            // Initialize context menu functionality
            this.InitializeContextMenu();

            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesCommand.Execute(this)), Key.C, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand.Execute(this)), Key.C, ModifierKeys.Control | ModifierKeys.Shift));

            // Subscribe to selection change events to update row count display
            this.SelectionChanged += OnSelectionChanged;
            this.SelectedCellsChanged += OnSelectedCellsChanged;

            // Generate columns from GridColumns descriptors once the control is loaded
            Loaded += OnSearchDataGridLoaded;

            // Edit-on-focus support: when a DataGridCell in a column with GridColumn.EditOnFocus=true
            // gets focus and is editable, enter edit mode immediately. AddHandler with handledEventsToo
            // because GotFocus is often marked handled by upstream selection logic before reaching us.
            AddHandler(GotFocusEvent, new RoutedEventHandler(OnAnyDescendantGotFocus), handledEventsToo: true);

            // Push DataContext through to GridColumn descriptors so XAML bindings on them (and on
            // their EditSettings) resolve. Descriptors live outside the logical tree, so they don't
            // inherit DataContext on their own.
            DataContextChanged += (_, e) =>
            {
                var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
                if (descriptors == null) return;
                foreach (var descriptor in descriptors)
                    descriptor.DataContext = e.NewValue;
            };
        }

        #endregion

        #region GridColumns Support

        /// <summary>
        /// Tracks whether <see cref="EnsureEditSettingsResourcesMerged"/> has already merged the
        /// editor-styles dictionary into <see cref="Application.Resources"/> for this process.
        /// </summary>
        private static bool _editSettingsResourcesMerged;

        /// <summary>
        /// On first call, merges <c>Themes/EditSettings.xaml</c> into <see cref="Application"/>'s
        /// resources so the keyed editor styles (<c>SdgEditTextBoxStyle</c>, <c>SdgEditComboBoxStyle</c>,
        /// etc.) are findable via <see cref="FrameworkElement.FindResource"/> and the
        /// DynamicResource references that the editor templates set up. Theme dictionaries alone
        /// don't feed those keyed lookups — they only contribute to the default-style system —
        /// so an explicit merge is required for cross-cell resource resolution.
        /// </summary>
        private static void EnsureEditSettingsResourcesMerged()
        {
            if (_editSettingsResourcesMerged) return;
            var app = Application.Current;
            if (app == null) return; // design-time / no app — bail out, runtime will retry on next ctor

            try
            {
                var dict = new ResourceDictionary
                {
                    Source = new Uri(
                        "pack://application:,,,/WWSearchDataGrid.Modern.WPF;component/Themes/EditSettings.xaml",
                        UriKind.Absolute)
                };
                app.Resources.MergedDictionaries.Add(dict);
                _editSettingsResourcesMerged = true;
                Debug.WriteLine("SearchDataGrid: merged EditSettings.xaml into Application.Resources.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SearchDataGrid: failed to merge EditSettings.xaml — {ex.Message}");
            }
        }

        /// <summary>
        /// Handles GotFocus on any descendant. When the focused element is inside a
        /// <see cref="DataGridCell"/> whose column resolves to <c>EditOnFocus=true</c>, transitions
        /// the cell into edit mode. The resolution falls through column → grid → false, so a
        /// column without its own setting inherits the grid-wide <see cref="EditOnFocus"/>.
        /// Skips read-only cells and cells already editing.
        /// </summary>
        private void OnAnyDescendantGotFocus(object sender, RoutedEventArgs e)
        {
            var cell = e.OriginalSource as DataGridCell ?? FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || cell.IsEditing || cell.IsReadOnly) return;

            var descriptor = FindGridColumnDescriptor(cell.Column);
            // Resolve in priority order: explicit column setting → grid-wide default → false.
            bool shouldEdit = descriptor?.EditOnFocus ?? EditOnFocus;
            if (!shouldEdit) return;

            // Defer BeginEdit one dispatcher tick — calling it directly inside GotFocus can
            // race the focus pipeline and leave the editor un-focusable.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!cell.IsEditing && cell.IsKeyboardFocusWithin)
                    BeginEdit();
            }), DispatcherPriority.Input);
        }

        private static T FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Tracks whether columns have already been generated from <see cref="GridColumns"/>
        /// to prevent duplicate generation on repeated Loaded events.
        /// </summary>
        private bool _gridColumnsGenerated;

        /// <summary>
        /// Suppresses <see cref="OnGridColumnsCollectionChanged"/> while auto-generation populates
        /// the descriptor collection — the subsequent <see cref="GenerateColumnsFromDescriptors"/>
        /// pass will build the WPF columns in one shot.
        /// </summary>
        private bool _suppressGridColumnsChanged;

        /// <summary>
        /// Re-entry guard for the DataView → <see cref="ListCollectionView"/> wrapping path in
        /// <see cref="OnItemsSourceChanged"/>. Prevents infinite recursion when the wrapper assignment
        /// triggers another <see cref="OnItemsSourceChanged"/> notification.
        /// </summary>
        private bool _isRewrappingItemsSource;

        /// <summary>
        /// True when the supplied source's default view will not support predicate-style filtering
        /// (i.e. assigning <c>Items.Filter = ...</c> would throw <see cref="NotSupportedException"/>).
        /// Currently catches DataView / IBindingListView shapes and DataTable (via IListSource).
        /// </summary>
        private static bool RequiresPredicateWrap(IEnumerable source)
        {
            // Already a ListCollectionView (or compatible) — predicate filter works.
            if (source is ListCollectionView) return false;
            // DataView and other IBindingListView sources go through BindingListCollectionView,
            // which only supports string-based filters.
            if (source is System.ComponentModel.IBindingListView) return true;
            // DataTable resolves to its DefaultView (a DataView) via IListSource.
            if (source is IListSource) return true;
            return false;
        }

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
        /// Rebuilds <see cref="VisualOrderedColumns"/> from <see cref="DataGrid.Columns"/> using the
        /// current <see cref="DataGridColumn.DisplayIndex"/> values. Called when the column set
        /// changes or any column is reordered. Edits the existing collection in place so the bound
        /// gridline ItemsControl observes per-item moves rather than a full reset.
        /// </summary>
        private void RebuildVisualOrderedColumns()
        {
            var target = VisualOrderedColumns;
            if (target == null) return;

            var ordered = Columns
                .OrderBy(c => c.DisplayIndex >= 0 ? c.DisplayIndex : int.MaxValue)
                .ToList();

            // Diff in place: remove stale columns, then place each ordered column at its index.
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!ordered.Contains(target[i]))
                    target.RemoveAt(i);
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                var col = ordered[i];
                int existing = target.IndexOf(col);
                if (existing < 0)
                    target.Insert(i, col);
                else if (existing != i)
                    target.Move(existing, i);
            }
        }

        /// <summary>
        /// Handles the Loaded event to generate columns from <see cref="GridColumns"/> descriptors.
        /// If <see cref="DataGrid.AutoGenerateColumns"/> is true and no descriptors were declared,
        /// auto-generates them from the data source first.
        /// </summary>
        private void OnSearchDataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (!_gridColumnsGenerated)
            {
                EnsureAutoGeneratedDescriptors();
                GenerateColumnsFromDescriptors();
            }
        }

        /// <summary>
        /// We manage column generation ourselves through the <see cref="GridColumns"/> descriptors.
        /// Cancel the WPF DataGrid's built-in auto-generation pipeline so it does not produce
        /// duplicate columns when <see cref="DataGrid.AutoGenerateColumns"/> is true.
        /// </summary>
        protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Cancel = true;
            base.OnAutoGeneratingColumn(e);
        }

        /// <summary>
        /// Populates <see cref="GridColumns"/> from the data source's property descriptors when
        /// <see cref="DataGrid.AutoGenerateColumns"/> is true and the user did not declare any.
        /// Honors <see cref="BrowsableAttribute"/> and <see cref="DisplayAttribute"/>.
        /// </summary>
        private void EnsureAutoGeneratedDescriptors()
        {
            if (!AutoGenerateColumns) return;
            if (ItemsSource == null) return;

            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null || descriptors.Count > 0) return;

            var props = GetItemPropertyDescriptors();
            if (props == null || props.Count == 0) return;

            var orderedProps = props
                .Cast<PropertyDescriptor>()
                .Where(IsAutoGenerable)
                .Select(pd => new { Pd = pd, Order = GetDisplayOrder(pd) })
                .OrderBy(x => x.Order)
                .Select(x => x.Pd)
                .ToList();

            _suppressGridColumnsChanged = true;
            try
            {
                foreach (var pd in orderedProps)
                {
                    var gc = new GridColumn
                    {
                        FieldName = pd.Name,
                        Header = ResolveHeaderText(pd) ?? pd.Name,
                        IsAutoGenerated = true
                    };
                    // Set FieldType directly from the descriptor — no further reflection needed.
                    gc.SetAutoFieldType(Nullable.GetUnderlyingType(pd.PropertyType) ?? pd.PropertyType);
                    descriptors.Add(gc);
                }
            }
            finally
            {
                _suppressGridColumnsChanged = false;
            }
        }

        /// <summary>
        /// Resolves the property descriptors for the current <see cref="ItemsSource"/>. Tries
        /// <see cref="ITypedList"/> first (DataView/DataTable), then <see cref="IItemProperties"/>
        /// from the default view, then <see cref="TypeDescriptor"/> against a sample item, and
        /// finally reflection on the generic item type.
        /// </summary>
        private PropertyDescriptorCollection GetItemPropertyDescriptors()
        {
            var source = ItemsSource;
            if (source == null) return null;

            // 1. ITypedList — direct access. Works on empty DataViews.
            if (source is ITypedList typedList)
            {
                var pds = typedList.GetItemProperties(null);
                if (pds != null && pds.Count > 0)
                    return pds;
            }

            // 2. IItemProperties via the default CollectionView.
            var itemProps = GetItemPropertiesFromSource();
            if (itemProps?.ItemProperties != null && itemProps.ItemProperties.Count > 0)
            {
                var pds = itemProps.ItemProperties
                    .Select(ip => ip.Descriptor as PropertyDescriptor)
                    .Where(pd => pd != null)
                    .ToArray();
                if (pds.Length > 0)
                    return new PropertyDescriptorCollection(pds);
            }

            // 3. Sample item — TypeDescriptor handles ICustomTypeDescriptor and POCOs.
            Type itemType = GetItemTypeFromSource(out object sampleItem);
            if (sampleItem != null)
                return TypeDescriptor.GetProperties(sampleItem);

            // 4. Empty source with a known generic type.
            if (itemType != null)
                return TypeDescriptor.GetProperties(itemType);

            return null;
        }

        private static bool IsAutoGenerable(PropertyDescriptor pd)
        {
            if (!pd.IsBrowsable) return false;
            var display = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
            if (display?.GetAutoGenerateField() == false) return false;
            return true;
        }

        private static string ResolveHeaderText(PropertyDescriptor pd)
        {
            var display = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
            var name = display?.GetName();
            if (!string.IsNullOrEmpty(name)) return name;
            if (!string.Equals(pd.DisplayName, pd.Name, StringComparison.Ordinal))
                return pd.DisplayName;
            return null;
        }

        private static int GetDisplayOrder(PropertyDescriptor pd)
        {
            var display = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
            return display?.GetOrder() ?? int.MaxValue;
        }

        /// <summary>
        /// Handles additions and removals in the <see cref="GridColumns"/> collection at runtime.
        /// </summary>
        private void OnGridColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressGridColumnsChanged)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (GridColumn descriptor in e.NewItems)
                        {
                            descriptor.Owner = this;
                            descriptor.DataContext = DataContext;
                            ResolveFieldTypeForDescriptor(descriptor);
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
                            descriptor.DataContext = DataContext;
                            ResolveFieldTypeForDescriptor(descriptor);
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

            // Resolve FieldType from the data source before generating WPF columns so the right
            // column type (e.g. DataGridCheckBoxColumn for bool) is chosen.
            ResolveFieldTypesFromItemsSource();

            foreach (var descriptor in descriptors)
            {
                descriptor.Owner = this;
                descriptor.DataContext = DataContext;
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

        #region Field Type Resolution

        /// <summary>
        /// Walks every descriptor in <see cref="GridColumns"/> and assigns <see cref="GridColumn.FieldType"/>
        /// from the data source where it has not been set explicitly. No-op if there is no data source.
        /// </summary>
        private void ResolveFieldTypesFromItemsSource()
        {
            if (ItemsSource == null)
                return;

            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null || descriptors.Count == 0)
                return;

            // Resolve item type and a sample instance once per pass; reuse across all descriptors.
            Type itemType = GetItemTypeFromSource(out object sampleItem);
            IItemProperties itemProps = GetItemPropertiesFromSource();

            foreach (var descriptor in descriptors)
                ResolveFieldTypeForDescriptor(descriptor, itemType, itemProps, sampleItem);
        }

        /// <summary>
        /// Resolves <see cref="GridColumn.FieldType"/> for a single descriptor against the current
        /// <see cref="ItemsSource"/>. Skips descriptors with an explicit FieldType or empty FieldName.
        /// </summary>
        private void ResolveFieldTypeForDescriptor(GridColumn descriptor)
        {
            if (descriptor == null) return;
            if (ItemsSource == null) return;
            Type itemType = GetItemTypeFromSource(out object sampleItem);
            ResolveFieldTypeForDescriptor(descriptor, itemType, GetItemPropertiesFromSource(), sampleItem);
        }

        private void ResolveFieldTypeForDescriptor(GridColumn descriptor, Type itemType, IItemProperties itemProps, object sampleItem)
        {
            if (descriptor == null || descriptor.IsFieldTypeExplicit)
                return;
            if (string.IsNullOrEmpty(descriptor.FieldName))
                return;

            Type resolved = ResolveTypeForField(descriptor.FieldName, itemType, itemProps, sampleItem);
            if (resolved != null)
                descriptor.SetAutoFieldType(resolved);
        }

        private static Type ResolveTypeForField(string fieldName, Type itemType, IItemProperties itemProps, object sampleItem)
        {
            // 1. IItemProperties — works on empty collections, handles ITypedList/DataView descriptors.
            if (itemProps?.ItemProperties != null)
            {
                foreach (var prop in itemProps.ItemProperties)
                {
                    if (string.Equals(prop.Name, fieldName, StringComparison.Ordinal))
                        return prop.PropertyType;
                }
            }

            // 2. TypeDescriptor on a sample instance — picks up ITypedList (DataRowView columns)
            //    and ICustomTypeDescriptor uniformly. Last-resort coverage when no CollectionView
            //    wrapped the source.
            if (sampleItem != null)
            {
                PropertyDescriptor pd = TypeDescriptor.GetProperties(sampleItem)?[fieldName];
                if (pd != null)
                    return pd.PropertyType;
            }

            // 3. Reflection on item type — supports dotted property paths for nested CLR objects.
            if (itemType != null)
                return ResolvePropertyTypeByPath(itemType, fieldName);

            return null;
        }

        private static Type ResolvePropertyTypeByPath(Type rootType, string path)
        {
            Type currentType = rootType;
            foreach (var segment in path.Split('.'))
            {
                if (currentType == null) return null;
                var prop = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null) return null;
                currentType = prop.PropertyType;
            }
            return currentType;
        }

        private Type GetItemTypeFromSource() => GetItemTypeFromSource(out _);

        private Type GetItemTypeFromSource(out object sampleItem)
        {
            sampleItem = null;
            var source = ItemsSource;
            if (source == null) return null;

            // Try IEnumerable<T> first — works without enumerating.
            Type genericType = null;
            var enumerableInterface = source.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableInterface != null)
            {
                var arg = enumerableInterface.GetGenericArguments()[0];
                if (arg != typeof(object))
                    genericType = arg;
            }

            // Capture the first non-null item for TypeDescriptor-based resolution (DataRowView, etc.).
            // Falls back to that item's runtime type if the generic type wasn't useful.
            foreach (var item in source)
            {
                if (item != null)
                {
                    sampleItem = item;
                    return genericType ?? item.GetType();
                }
            }
            return genericType;
        }

        private IItemProperties GetItemPropertiesFromSource()
        {
            var source = ItemsSource;
            if (source == null) return null;
            var view = source as ICollectionView ?? CollectionViewSource.GetDefaultView(source);
            return view as IItemProperties;
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
            // The DataTrigger bound to ActualHasItems in the column-header template re-evaluates
            // automatically when this property changes. Calling UpdateLayout here is unnecessary
            // and triggers measure/arrange side effects that can re-enter our CollectionChanged
            // handlers, so we deliberately do not force layout here.
            if (d is SearchDataGrid grid)
                grid.InvalidateVisual();
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
            // BindingListCollectionView (the default view for DataView / IBindingListView) does not
            // support predicate-based Items.Filter — it throws NotSupportedException. Our filter
            // pipeline assigns predicates, so re-wrap the source in a ListCollectionView, which does.
            // Guard against re-entry: if we're already wrapping, the inner re-set will skip this.
            if (!_isRewrappingItemsSource && newValue != null && RequiresPredicateWrap(newValue))
            {
                _isRewrappingItemsSource = true;
                try
                {
                    var list = newValue as IList ?? (newValue is IListSource ils ? ils.GetList() : null);
                    if (list != null)
                    {
                        SetCurrentValue(ItemsSourceProperty, new ListCollectionView(list));
                        return;
                    }
                }
                finally
                {
                    _isRewrappingItemsSource = false;
                }
            }

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

                    // If AutoGenerateColumns is on and no descriptors were declared, populate
                    // GridColumns from the data source. Only meaningful before the first
                    // GenerateColumnsFromDescriptors pass — after that, the column set is established.
                    if (!_gridColumnsGenerated)
                        EnsureAutoGeneratedDescriptors();

                    // Auto-resolve FieldType on descriptors that don't have one set explicitly.
                    ResolveFieldTypesFromItemsSource();

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
        /// Updates the ActualHasItems property. Intentionally simple: this drives a UI cosmetic
        /// (hiding search boxes when no source is assigned), and any attempt to inspect the
        /// actual count has historically created CollectionChanged feedback loops with views
        /// like <see cref="ListCollectionView"/> over IBindingListView sources. Treating
        /// "source assigned" as "has items" is good enough for the cosmetic and is loop-free.
        /// </summary>
        private void UpdateHasItemsProperty()
        {
            bool hasAnyItems = originalItemsSource != null;
            if (ActualHasItems != hasAnyItems)
                ActualHasItems = hasAnyItems;
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