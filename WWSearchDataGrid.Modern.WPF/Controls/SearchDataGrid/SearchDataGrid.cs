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
using WWSearchDataGrid.Modern.WPF.Behaviors;
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

        private readonly ObservableCollection<IColumnFilterHost> dataColumns = new ObservableCollection<IColumnFilterHost>();
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
        /// Live thumb-drag scrolling. Defaults true. Disable for very large datasets (100k+) if scrolling feels choppy.
        /// </summary>
        public static readonly DependencyProperty EnableLiveScrollingProperty =
            DependencyProperty.Register("EnableLiveScrolling", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(true, OnEnableLiveScrollingChanged));

        /// <summary>Persists the most recently focused column, surviving focus leaving the grid.</summary>
        public static readonly DependencyProperty LastFocusedColumnProperty =
            DependencyProperty.Register("LastFocusedColumn", typeof(DataGridColumn), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public static readonly DependencyProperty LastFocusedGridColumnProperty =
            DependencyProperty.Register("LastFocusedGridColumn", typeof(GridColumn), typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>
        /// Grid-wide default for when a cell click triggers edit. Default is
        /// <see cref="WPF.EditorShowMode.MouseDownFocused"/> (focus on first click, edit on
        /// second). Per-editor override via <see cref="BaseEditSettings.EditorShowMode"/>.
        /// </summary>
        public static readonly DependencyProperty EditorShowModeProperty =
            DependencyProperty.Register(nameof(EditorShowMode), typeof(EditorShowMode), typeof(SearchDataGrid),
                new PropertyMetadata(EditorShowMode.MouseDownFocused));

        /// <summary>
        /// Grid-wide default for <see cref="BaseEditSettings.EditorButtonShowMode"/> — controls
        /// when editor decoration buttons (combo toggle, spinner, calendar dropdown) appear.
        /// </summary>
        public static readonly DependencyProperty EditorButtonShowModeProperty =
            DependencyProperty.Register(nameof(EditorButtonShowMode), typeof(EditorButtonShowMode), typeof(SearchDataGrid),
                new PropertyMetadata(EditorButtonShowMode.ShowOnlyInEditor));

        /// <summary>Read-only key for <see cref="GridColumns"/>.</summary>
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
        /// Gates the auto-filter UI. Combined with <see cref="AutoFilterRowPositionProperty"/>
        /// to choose between a pinned row and in-header editors. Defaults to <c>true</c>.
        /// </summary>
        public static readonly DependencyProperty ShowAutoFilterRowProperty =
            DependencyProperty.Register(nameof(ShowAutoFilterRow), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Dependency property for <see cref="AutoFilterRowPosition"/>. Defaults to
        /// <see cref="WPF.AutoFilterRowPosition.Cell"/>.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowPositionProperty =
            DependencyProperty.Register(nameof(AutoFilterRowPosition), typeof(AutoFilterRowPosition), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(AutoFilterRowPosition.Cell));

        /// <summary>
        /// Grid-wide default for showing the inline <see cref="SearchTypeSelector"/> in each
        /// filter cell. <c>false</c> hides it; column override via
        /// <see cref="GridColumn.ShowCriteriaInAutoFilterRow"/> wins when set.
        /// </summary>
        public static readonly DependencyProperty ShowCriteriaInAutoFilterRowProperty =
            DependencyProperty.Register(nameof(ShowCriteriaInAutoFilterRow), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnShowCriteriaInAutoFilterRowChanged));

        /// <summary>
        /// Grid-wide default Style for <see cref="ColumnFilterControl"/>. Column override
        /// via <see cref="GridColumn.AutoFilterRowCellStyle"/> wins; both null falls back to
        /// the keyed theme style under <see cref="SdgThemeKeys.FilterRow.ColumnFilterControl"/>.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowCellStyleProperty =
            DependencyProperty.Register(nameof(AutoFilterRowCellStyle), typeof(Style), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null, OnAutoFilterRowCellStyleChanged));

        /// <summary>
        /// Debounce window (ms) for keystroke-driven filter updates. <c>0</c> applies every
        /// keystroke immediately. Only meaningful when live filtering is enabled — without it,
        /// filters fire on Enter/Tab/lost-focus regardless.
        /// </summary>
        public static readonly DependencyProperty FilterRowDelayProperty =
            DependencyProperty.Register(nameof(FilterRowDelay), typeof(int), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Grid-wide live-filtering toggle. <c>true</c> (default): filter changes — popup edits
        /// and auto-filter-row typing — apply to the grid as they happen. <c>false</c>: changes
        /// are deferred until the user commits via Enter / Tab / focus loss / popup close.
        /// </summary>
        public static readonly DependencyProperty EnableLiveFilteringProperty =
            DependencyProperty.Register(nameof(EnableLiveFiltering), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Grid-wide policy for the per-cell clear (X) button visibility. Defaults to
        /// <see cref="WPF.AutoFilterRowClearButtonMode.Always"/>.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowClearButtonModeProperty =
            DependencyProperty.Register(nameof(AutoFilterRowClearButtonMode), typeof(AutoFilterRowClearButtonMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(AutoFilterRowClearButtonMode.Always, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Gates the runtime "Pin Left / Pin Right / Unpin" items in the column-header
        /// context menu. When <c>true</c>, end users can change a column's
        /// <see cref="GridColumn.Fixed"/> position interactively; when <c>false</c>
        /// (the default), pinning can only be set declaratively in XAML.
        /// </summary>
        public static readonly DependencyProperty AllowFixedColumnMenuProperty =
            DependencyProperty.Register(nameof(AllowFixedColumnMenu), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(false));

        /// <summary>Re-resolves effective ShowCriteria on each filter cell when the grid DP changes.</summary>
        private static void OnShowCriteriaInAutoFilterRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            foreach (var host in grid.DataColumns)
            {
                if (host is ColumnFilterControl ctl)
                    ctl.RefreshEffectiveShowCriteria();
            }
        }

        /// <summary>Re-resolves the cell Style on each filter cell when the grid DP changes.</summary>
        private static void OnAutoFilterRowCellStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            foreach (var host in grid.DataColumns)
            {
                if (host is ColumnFilterControl ctl)
                    ctl.RefreshAutoFilterRowCellStyle();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data columns collection
        /// </summary>
        public ObservableCollection<IColumnFilterHost> DataColumns
        {
            get { return dataColumns; }
        }

        /// <summary>Grid-wide click-to-edit policy. See <see cref="EditorShowModeProperty"/>.</summary>
        public EditorShowMode EditorShowMode
        {
            get => (EditorShowMode)GetValue(EditorShowModeProperty);
            set => SetValue(EditorShowModeProperty, value);
        }

        /// <summary>Grid-wide editor-button visibility default. See <see cref="EditorButtonShowModeProperty"/>.</summary>
        public EditorButtonShowMode EditorButtonShowMode
        {
            get => (EditorButtonShowMode)GetValue(EditorButtonShowModeProperty);
            set => SetValue(EditorButtonShowModeProperty, value);
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
        /// Grid-wide rule-filtering toggle. False forces single-text-filter mode; true defers
        /// to per-column EnableRuleFiltering.
        /// </summary>
        public bool EnableRuleFiltering
        {
            get { return (bool)GetValue(EnableRuleFilteringProperty); }
            set { SetValue(EnableRuleFilteringProperty, value); }
        }

        /// <summary>
        /// Shows/hides the non-modal Column Chooser window. Forced false when
        /// <see cref="IsColumnChooserEnabled"/> is false.
        /// </summary>
        public bool IsColumnChooserVisible
        {
            get { return (bool)GetValue(IsColumnChooserVisibleProperty); }
            set { SetValue(IsColumnChooserVisibleProperty, value); }
        }

        /// <summary>
        /// Enables the Column Chooser feature. False hides menu items and blocks
        /// <see cref="IsColumnChooserVisible"/>.
        /// </summary>
        public bool IsColumnChooserEnabled
        {
            get { return (bool)GetValue(IsColumnChooserEnabledProperty); }
            set { SetValue(IsColumnChooserEnabledProperty, value); }
        }

        /// <summary>
        /// Confines the Column Chooser window to the grid's viewport bounds when true.
        /// </summary>
        public bool IsColumnChooserConfinedToGrid
        {
            get { return (bool)GetValue(IsColumnChooserConfinedToGridProperty); }
            set { SetValue(IsColumnChooserConfinedToGridProperty, value); }
        }

        /// <summary>
        /// Live thumb-drag scrolling. Defaults true. Disable for very large datasets (100k+ rows)
        /// if scrolling stutters.
        /// </summary>
        public bool EnableLiveScrolling
        {
            get => (bool)GetValue(EnableLiveScrollingProperty);
            set => SetValue(EnableLiveScrollingProperty, value);
        }

        /// <summary>Last focused column. Persists when focus leaves the grid.</summary>
        public DataGridColumn LastFocusedColumn
        {
            get => (DataGridColumn)GetValue(LastFocusedColumnProperty);
            private set => SetValue(LastFocusedColumnProperty, value);
        }

        /// <summary>Descriptor for <see cref="LastFocusedColumn"/>.</summary>
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
        ///                     DefaultSearchType="StartsWith" EnableRuleFiltering="False" /&gt;
        /// &lt;/sdg:SearchDataGrid.GridColumns&gt;
        /// </code>
        /// </example>
        public FreezableCollection<GridColumn> GridColumns
        {
            get => (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
        }

        /// <summary>Shows the auto-filter UI (placement via <see cref="AutoFilterRowPosition"/>).</summary>
        public bool ShowAutoFilterRow
        {
            get => (bool)GetValue(ShowAutoFilterRowProperty);
            set => SetValue(ShowAutoFilterRowProperty, value);
        }

        /// <summary>
        /// Placement of the auto-filter UI: Cell (default, pinned row) or Header (in-header
        /// expand-on-click).
        /// </summary>
        public AutoFilterRowPosition AutoFilterRowPosition
        {
            get => (AutoFilterRowPosition)GetValue(AutoFilterRowPositionProperty);
            set => SetValue(AutoFilterRowPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether each column's filter cell renders the inline search-type
        /// selector. See <see cref="ShowCriteriaInAutoFilterRowProperty"/>.
        /// </summary>
        public bool ShowCriteriaInAutoFilterRow
        {
            get => (bool)GetValue(ShowCriteriaInAutoFilterRowProperty);
            set => SetValue(ShowCriteriaInAutoFilterRowProperty, value);
        }

        /// <summary>
        /// Gets or sets the grid-wide default <see cref="Style"/> for the per-column
        /// <see cref="ColumnFilterControl"/>. See <see cref="AutoFilterRowCellStyleProperty"/>.
        /// </summary>
        public Style AutoFilterRowCellStyle
        {
            get => (Style)GetValue(AutoFilterRowCellStyleProperty);
            set => SetValue(AutoFilterRowCellStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the debounce window in milliseconds for keystroke-driven filter updates.
        /// See <see cref="FilterRowDelayProperty"/>.
        /// </summary>
        public int FilterRowDelay
        {
            get => (int)GetValue(FilterRowDelayProperty);
            set => SetValue(FilterRowDelayProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility policy for the per-cell clear (X) button in the
        /// auto-filter row. See <see cref="AutoFilterRowClearButtonModeProperty"/>.
        /// </summary>
        public AutoFilterRowClearButtonMode AutoFilterRowClearButtonMode
        {
            get => (AutoFilterRowClearButtonMode)GetValue(AutoFilterRowClearButtonModeProperty);
            set => SetValue(AutoFilterRowClearButtonModeProperty, value);
        }

        /// <summary>
        /// Gates the runtime pin/unpin items in the column-header context menu.
        /// See <see cref="AllowFixedColumnMenuProperty"/>.
        /// </summary>
        public bool AllowFixedColumnMenu
        {
            get => (bool)GetValue(AllowFixedColumnMenuProperty);
            set => SetValue(AllowFixedColumnMenuProperty, value);
        }

        /// <inheritdoc cref="EnableLiveFilteringProperty"/>
        public bool EnableLiveFiltering
        {
            get => (bool)GetValue(EnableLiveFilteringProperty);
            set => SetValue(EnableLiveFilteringProperty, value);
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

            // Edit-on-focus is flag-driven: OnAnyDescendantGotFocus only enters edit when
            // _carryEditStateOnNextFocus is set. Set by click-to-edit and Tab-from-editing;
            // arrow keys and bare programmatic focus changes leave the flag clear.
            // handledEventsToo because upstream selection often marks GotFocus.
            AddHandler(GotFocusEvent, new RoutedEventHandler(OnAnyDescendantGotFocus), handledEventsToo: true);
            AddHandler(PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown_SetEditCarry), handledEventsToo: true);
            AddHandler(PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnPreviewMouseLeftButtonUp_BeginEdit), handledEventsToo: true);
            AddHandler(PreviewKeyDownEvent,
                new KeyEventHandler(OnGridPreviewKeyDown), handledEventsToo: true);
            AddHandler(PreviewTextInputEvent,
                new TextCompositionEventHandler(OnGridPreviewTextInput), handledEventsToo: true);

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
        /// One-shot: next focus into a DataGridCell auto-enters edit. Set by click-to-edit
        /// or Tab-from-editing; cleared on consume and at the start of every key pre-handler.
        /// </summary>
        private bool _carryEditStateOnNextFocus;

        /// <summary>
        /// Cell with a deferred BeginEdit queued by <see cref="OnAnyDescendantGotFocus"/>.
        /// Lets the arrow handler re-arm the carry flag when auto-repeat outruns the dispatcher,
        /// so held-arrow nav keeps editing every cell along the way.
        /// </summary>
        private DataGridCell _pendingEditCell;

        /// <summary>
        /// Arms the one-shot carry flag. Called by <see cref="BaseEditSettings.ExitCellViaArrow"/>
        /// after committing so arrow nav from an editing cell stays in edit mode at the destination.
        /// </summary>
        internal void SetCarryEditStateOnNextFocus() => _carryEditStateOnNextFocus = true;

        /// <summary>
        /// True when the cell or owning grid is read-only. The cell-level check alone is
        /// unreliable for DataGridTemplateColumn — the grid→cell coercion doesn't propagate.
        /// </summary>
        private bool IsCellOrGridReadOnly(DataGridCell cell)
            => cell == null || cell.IsReadOnly || IsReadOnly;

        /// <summary>
        /// Was the clicked cell focused before this mouse-down? Read by *Focused modes to
        /// distinguish "click unfocused" (focus only) from "click already focused" (edit).
        /// </summary>
        private bool _wasCellFocusedAtMouseDown;

        /// <summary>
        /// Click point (cell-local) for the most recent mouse BeginEdit. TextBox editors
        /// consume this on first GotKeyboardFocus to land the caret at the click index;
        /// keyboard entry leaves it null and falls back to select-all.
        /// </summary>
        private Point? _pendingMouseEditCellPoint;
        private DataGridCell _pendingMouseEditCell;

        internal void StashMouseEditPoint(DataGridCell cell, Point cellPoint)
        {
            _pendingMouseEditCell = cell;
            _pendingMouseEditCellPoint = cellPoint;
        }

        /// <summary>
        /// Returns and clears the stashed click point for <paramref name="forCell"/>.
        /// Consume-on-read so a stale point can't bleed into a later keyboard-driven edit.
        /// </summary>
        internal bool TryConsumeMouseEditPoint(DataGridCell forCell, out Point cellPoint)
        {
            if (forCell != null
                && ReferenceEquals(_pendingMouseEditCell, forCell)
                && _pendingMouseEditCellPoint.HasValue)
            {
                cellPoint = _pendingMouseEditCellPoint.Value;
                _pendingMouseEditCellPoint = null;
                _pendingMouseEditCell = null;
                return true;
            }
            cellPoint = default;
            return false;
        }

        /// <summary>
        /// Decides whether mouse-down triggers edit now (MouseDown/MouseDownFocused) or
        /// defers to mouse-up. *Focused variants require pre-existing focus.
        /// </summary>
        private void OnPreviewMouseLeftButtonDown_SetEditCarry(object sender, MouseButtonEventArgs e)
        {
            var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || IsCellOrGridReadOnly(cell))
            {
                _wasCellFocusedAtMouseDown = false;
                return;
            }

            // Capture pre-click focus state for *Focused modes consumed below and on mouse-up.
            _wasCellFocusedAtMouseDown = cell.IsKeyboardFocusWithin;

            var mode = ResolveEditorShowMode(cell);
            bool shouldEditNow = mode == EditorShowMode.MouseDown
                || (mode == EditorShowMode.MouseDownFocused && _wasCellFocusedAtMouseDown);
            if (!shouldEditNow) return;

            // Stash here (not in OnBeginningEdit) — stock DataGridCell calls BeginEdit() without
            // args, so OnBeginningEdit's mouse branch never fires for stock-driven edits.
            StashMouseEditPoint(cell, e.GetPosition(cell));

            // Already-focused: no GotFocus will fire, so BeginEdit directly (deferred a tick
            // for the focus pipeline). Newly-focusing: let OnAnyDescendantGotFocus handle it.
            if (_wasCellFocusedAtMouseDown && !cell.IsEditing)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!cell.IsEditing && cell.IsKeyboardFocusWithin)
                        BeginEdit();
                }), DispatcherPriority.Input);
            }
            else
            {
                _carryEditStateOnNextFocus = true;
            }
        }

        /// <summary>
        /// Drives BeginEdit for the MouseUp / MouseUpFocused modes. The *Focused variant
        /// gates on the pre-click focus state captured at mouse-down.
        /// </summary>
        private void OnPreviewMouseLeftButtonUp_BeginEdit(object sender, MouseButtonEventArgs e)
        {
            var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || IsCellOrGridReadOnly(cell) || cell.IsEditing) return;

            var mode = ResolveEditorShowMode(cell);
            bool shouldEdit = mode == EditorShowMode.MouseUp
                || (mode == EditorShowMode.MouseUpFocused && _wasCellFocusedAtMouseDown);
            if (!shouldEdit) return;

            // Stash for the editor's caret placement — our deferred BeginEdit() below fires
            // programmatically with EditingEventArgs == null, so OnBeginningEdit can't capture it.
            StashMouseEditPoint(cell, e.GetPosition(cell));

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!cell.IsEditing && cell.IsKeyboardFocusWithin)
                    BeginEdit();
            }), DispatcherPriority.Input);
        }

        /// <summary>
        /// Cell keyboard handler. Enter toggles edit (handled). Tab/Shift+Tab sets the carry
        /// flag when source is editing but lets the native handler run. Arrow keys mostly
        /// defer to the editor (TextBox caret-aware exit) or to native nav.
        /// </summary>
        private void OnGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reset per-keystroke — a click on an already-editing cell would otherwise leak
            // the flag into the next key-driven focus change.
            _carryEditStateOnNextFocus = false;

            var focused = Keyboard.FocusedElement as DependencyObject;
            var cell = FindAncestor<DataGridCell>(focused);
            if (cell == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                    {
                        // CheckBox cells: Enter toggles the value (toggle-edit has no meaningful
                        // visual since display and edit are both interactive CheckBoxes).
                        var checkBox = FindCheckBoxInCell(cell);
                        if (checkBox != null && !IsCellOrGridReadOnly(cell))
                        {
                            checkBox.IsChecked = !(checkBox.IsChecked ?? false);
                            e.Handled = true;
                            break;
                        }

                        if (cell.IsEditing)
                        {
                            // Row-level commit so DataGridRow.IsEditing flips false and the
                            // row-header pencil indicator clears — cell-level commit would leave
                            // it stuck until row navigation. Tab still uses cell-level so mid-row
                            // tabbing keeps the pencil up. cell.Focus() pins focus on the cell.
                            CommitEdit(DataGridEditingUnit.Row, true);
                            cell.Focus();
                        }
                        else if (!IsCellOrGridReadOnly(cell))
                        {
                            // Bypass EditOnFocus — Enter is an explicit edit gesture.
                            BeginEdit();
                        }
                        e.Handled = true;
                        break;
                    }

                case Key.Space:
                    {
                        // Space toggles CheckBox cells programmatically — works in both display
                        // mode (display CheckBox is Focusable=False) and edit mode.
                        var checkBox = FindCheckBoxInCell(cell);
                        if (checkBox != null && !IsCellOrGridReadOnly(cell))
                        {
                            checkBox.IsChecked = !(checkBox.IsChecked ?? false);
                            e.Handled = true;
                        }
                        break;
                    }

                case Key.Tab:
                {
                    if (TryWrapTabWithinRow(cell, e))
                    {
                        // Within-row wrap — carry flag already set by the helper.
                        e.Handled = true;
                        break;
                    }

                    if (cell.IsEditing)
                        _carryEditStateOnNextFocus = true;
                    // Don't mark handled — DataGrid's native Tab handler commits + moves focus.
                    break;
                }

                // Up on the first data row hands off to the filter row (symmetric with the
                // filter row's Down handoff). Only fires when focus is on the cell shell so
                // editors with real Up uses (multiline caret, ComboBox, DatePicker) get it first.
                case Key.Up:
                {
                    if (!ShowAutoFilterRow) break;
                    if (!ReferenceEquals(focused, cell)) break;
                    var rowContainer = FindAncestor<DataGridRow>(cell);
                    if (rowContainer == null) break;
                    if (ItemContainerGenerator.IndexFromContainer(rowContainer) != 0) break;
                    if (TryFocusFilterCellForColumn(cell.Column))
                        e.Handled = true;
                    break;
                }
                case Key.Down:
                    break;

                // Left/Right wrap at row edges — Right at last cell → first cell of next row,
                // Left at first cell → last cell of previous row (Tab wraps within same row).
                // Fully-settled editing cells route through ExitCellViaArrow → same helper;
                // the two transitional cases below cover press-and-hold focus-on-cell states.
                case Key.Left:
                case Key.Right:
                {
                    // Press-and-hold transitional states where focus is on the cell, not the
                    // editor — native KeyDown would move focus without the carry flag, dropping
                    // edit mid-hold. Re-arm explicitly:
                    //   • _pendingEditCell == cell: queued BeginEdit hasn't drained yet.
                    //   • cell.IsEditing && focused == cell: editor's deferred focus hasn't run.
                    bool pendingEditOnCell = !cell.IsEditing && ReferenceEquals(_pendingEditCell, cell);
                    bool inFlightEdit = cell.IsEditing && ReferenceEquals(focused, cell);

                    if (pendingEditOnCell)
                        _carryEditStateOnNextFocus = true;
                    else if (inFlightEdit)
                    {
                        CommitEdit();
                        _carryEditStateOnNextFocus = true;
                    }

                    if (!cell.IsEditing || inFlightEdit)
                    {
                        if (TryWrapArrowAtRowEdge(cell, e.Key == Key.Right))
                            e.Handled = true;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Locates a CheckBox in the cell's visual tree. Non-null = CheckBox-style cell for
        /// keyboard-toggle UX.
        /// </summary>
        private static CheckBox FindCheckBoxInCell(DataGridCell cell)
        {
            return FindVisualDescendant<CheckBox>(cell);
        }

        /// <summary>
        /// Within-row Tab wrap: Tab at last cell → first cell of same row, Shift+Tab at first
        /// → last. Sets the carry flag so the destination auto-edits like a normal Tab move.
        /// Returns false (no wrap) when the cell isn't at a row edge.
        /// </summary>
        private bool TryWrapTabWithinRow(DataGridCell cell, KeyEventArgs e)
        {
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            var targetCell = GetWrapTargetAtRowEdge(cell, forward: !isShift, crossRow: false);
            if (targetCell == null) return false;

            // Commit before navigating so the bound source updates (matching native Tab).
            if (cell.IsEditing)
            {
                CommitEdit();
                _carryEditStateOnNextFocus = true;
            }
            else if (IsCellAutoEditEligible(targetCell))
            {
                // Source wasn't editing, but destination opts into auto-edit — carry forward
                // so wrap matches "tab through editable cells".
                _carryEditStateOnNextFocus = true;
            }

            targetCell.Focus();
            return true;
        }

        /// <summary>
        /// Cross-row arrow wrap at row edges: Right at last cell → first cell of next row,
        /// Left at first cell → last cell of previous row. Returns false at grid edges or
        /// when not at a row edge (caller falls back to native arrow handling).
        /// </summary>
        internal bool TryWrapArrowAtRowEdge(DataGridCell cell, bool forward)
        {
            var targetCell = GetWrapTargetAtRowEdge(cell, forward, crossRow: true);
            if (targetCell == null) return false;

            // Carry only when source was editing — arrow-from-display stays focus-only at
            // the destination, even if it opts into auto-edit (click-to-edit is click-only).
            if (cell.IsEditing)
                _carryEditStateOnNextFocus = true;

            targetCell.Focus();
            return true;
        }

        /// <summary>
        /// Shared wrap-target resolution. <paramref name="crossRow"/>=false for Tab (within
        /// same row), true for arrow (next/previous row). Virtualized adjacent rows are
        /// realized via ScrollIntoView before the cell lookup.
        /// </summary>
        private DataGridCell GetWrapTargetAtRowEdge(DataGridCell cell, bool forward, bool crossRow)
        {
            var visibleCols = Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();
            if (visibleCols.Count == 0) return null;

            int currentIdx = visibleCols.IndexOf(cell.Column);
            if (currentIdx < 0) return null;

            bool wrapForward = forward && currentIdx == visibleCols.Count - 1;
            bool wrapBackward = !forward && currentIdx == 0;
            if (!wrapForward && !wrapBackward) return null;

            DataGridColumn targetColumn;
            object targetRowItem;

            if (crossRow)
            {
                // Use Items (filtered/sorted view), not ItemsSource, so wrap follows what the
                // user sees.
                int rowIdx = Items.IndexOf(cell.DataContext);
                if (rowIdx < 0) return null;

                if (wrapForward)
                {
                    if (rowIdx >= Items.Count - 1) return null; // last row — no wrap target
                    targetRowItem = Items[rowIdx + 1];
                    targetColumn = visibleCols[0];
                }
                else
                {
                    if (rowIdx <= 0) return null; // first row — no wrap target
                    targetRowItem = Items[rowIdx - 1];
                    targetColumn = visibleCols[visibleCols.Count - 1];
                }
            }
            else
            {
                targetRowItem = cell.DataContext;
                targetColumn = wrapForward ? visibleCols[0] : visibleCols[visibleCols.Count - 1];
            }

            var row = ItemContainerGenerator.ContainerFromItem(targetRowItem) as DataGridRow;
            if (row == null && crossRow)
            {
                // Force synchronous realization so Focus lands before native nav resumes.
                ScrollIntoView(targetRowItem);
                UpdateLayout();
                row = ItemContainerGenerator.ContainerFromItem(targetRowItem) as DataGridRow;
            }
            if (row == null) return null;

            return GetCellAt(row, targetColumn);
        }

        /// <summary>Returns the cell at <paramref name="column"/> inside <paramref name="row"/>, or null if not realized.</summary>
        private static DataGridCell GetCellAt(DataGridRow row, DataGridColumn column)
        {
            if (row == null || column == null) return null;
            var presenter = FindVisualDescendant<DataGridCellsPresenter>(row);
            if (presenter == null)
            {
                row.ApplyTemplate();
                presenter = FindVisualDescendant<DataGridCellsPresenter>(row);
            }
            if (presenter == null) return null;
            return presenter.ItemContainerGenerator.ContainerFromIndex(column.DisplayIndex) as DataGridCell;
        }

        /// <summary>
        /// Type-to-edit: a printable character on a focused, non-editing, editable cell enters
        /// edit and routes the keystroke into the editor's TextBox (Excel-style type-to-replace).
        /// Skipped for CheckBox cells (Space is the gesture) and ComboBox cells (non-text editor).
        /// Hooks PreviewTextInput rather than PreviewKeyDown to handle IME / dead-keys / modifiers.
        /// </summary>
        private void OnGridPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            // Skip control characters — Esc/Tab/etc. don't normally route through TextInput,
            // but be safe in case any IME/composition path raises them.
            if (char.IsControl(e.Text[0])) return;

            var focused = Keyboard.FocusedElement as DependencyObject;
            var cell = FindAncestor<DataGridCell>(focused);
            if (cell == null || cell.IsEditing || IsCellOrGridReadOnly(cell)) return;
            if (!IsCellAutoEditEligible(cell)) return;

            // CheckBox cells: Space toggle is the keyboard UX; printable-char-to-edit doesn't apply.
            if (FindCheckBoxInCell(cell) != null) return;

            // ComboBox cells: editor is non-editable; type-to-edit can't route into a text field.
            // Let the user press Enter/click to enter edit mode and use arrow/dropdown to pick.
            var descriptor = FindGridColumnDescriptor(cell.Column);
            if (descriptor?.EditSettings is ComboBoxEditSettings) return;

            _carryEditStateOnNextFocus = true;
            BeginEdit();

            // Inject at Background priority — runs after AutoFocusOnLoad (Input) and the
            // editor's GotKeyboardFocus→SelectAll, so the set replaces the selected content.
            string typedText = e.Text;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var tb = FindVisualDescendant<TextBox>(cell);
                if (tb == null) return;

                // Masked TextBoxes need PreviewTextInput routing — a bare Text= would wipe the
                // value. Collapse the initial select-all to length 1 so MaskInputBehavior
                // overwrites only the first character.
                if (!string.IsNullOrEmpty(MaskInputBehavior.GetMask(tb)))
                {
                    if (tb.SelectionLength > 1)
                        tb.SelectionLength = 1;

                    var args = new TextCompositionEventArgs(
                        InputManager.Current.PrimaryKeyboardDevice,
                        new TextComposition(InputManager.Current, tb, typedText))
                    {
                        RoutedEvent = TextCompositionManager.PreviewTextInputEvent,
                    };
                    tb.RaiseEvent(args);
                    return;
                }

                tb.Text = typedText;
                tb.CaretIndex = typedText.Length;
                tb.SelectionLength = 0;
            }), DispatcherPriority.Background);

            e.Handled = true;
        }

        private static T FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                var deeper = FindVisualDescendant<T>(child);
                if (deeper != null) return deeper;
            }
            return null;
        }

        /// <summary>
        /// Resolves effective EditorShowMode — column override (non-Default) wins, otherwise
        /// the grid value. Grid-level Default means no auto-edit (Enter/F2 only).
        /// </summary>
        private EditorShowMode ResolveEditorShowMode(DataGridCell cell)
        {
            var descriptor = FindGridColumnDescriptor(cell?.Column);
            var fromSettings = descriptor?.EditSettings?.EditorShowMode ?? EditorShowMode.Default;
            return fromSettings != EditorShowMode.Default ? fromSettings : EditorShowMode;
        }

        /// <summary>
        /// True for any active click-to-edit mode. Default and None leave cells focus-only.
        /// </summary>
        private bool IsCellAutoEditEligible(DataGridCell cell)
        {
            var mode = ResolveEditorShowMode(cell);
            return mode != EditorShowMode.Default && mode != EditorShowMode.None;
        }

        /// <summary>
        /// Auto-edits on focus when the carry flag is set (click or Tab-from-editing).
        /// Bare programmatic focus changes leave the flag clear → focus-only at destination.
        /// </summary>
        private void OnAnyDescendantGotFocus(object sender, RoutedEventArgs e)
        {
            var cell = e.OriginalSource as DataGridCell ?? FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || cell.IsEditing || IsCellOrGridReadOnly(cell)) return;

            if (!_carryEditStateOnNextFocus) return;
            _carryEditStateOnNextFocus = false;

            if (!IsCellAutoEditEligible(cell)) return;

            // Track for the arrow-key handler in case auto-repeat outruns the queued BeginEdit.
            _pendingEditCell = cell;

            // Defer one tick — direct BeginEdit inside GotFocus races the focus pipeline.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!cell.IsEditing && cell.IsKeyboardFocusWithin)
                    BeginEdit();
                // A faster auto-repeat may have queued a newer cell — don't clear theirs.
                if (ReferenceEquals(_pendingEditCell, cell))
                    _pendingEditCell = null;
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

        /// <summary>Prevents duplicate column generation on repeated Loaded events.</summary>
        private bool _gridColumnsGenerated;

        /// <summary>
        /// Suppresses <see cref="OnGridColumnsCollectionChanged"/> during auto-generation —
        /// <see cref="GenerateColumnsFromDescriptors"/> will build the WPF columns in one pass.
        /// </summary>
        private bool _suppressGridColumnsChanged;

        /// <summary>Re-entry guard for the DataView → ListCollectionView wrap in OnItemsSourceChanged.</summary>
        private bool _isRewrappingItemsSource;

        /// <summary>
        /// True when the source's default view doesn't support predicate filtering (DataView,
        /// IBindingListView, DataTable via IListSource).
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
        /// Generates columns from <see cref="GridColumns"/> on Loaded, auto-creating descriptors
        /// from the data source first when AutoGenerateColumns is on and none were declared.
        /// </summary>
        private void OnSearchDataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (!_gridColumnsGenerated)
            {
                EnsureAutoGeneratedDescriptors();
                GenerateColumnsFromDescriptors();
            }

            if (!string.IsNullOrEmpty(FilterString))
                ApplyFilterString();
        }

        /// <summary>
        /// We manage columns via <see cref="GridColumns"/> descriptors — cancel WPF's auto-gen
        /// pipeline so it doesn't produce duplicates.
        /// </summary>
        protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Cancel = true;
            base.OnAutoGeneratingColumn(e);
        }

        /// <summary>
        /// Populates <see cref="GridColumns"/> from the data source's property descriptors when
        /// AutoGenerateColumns is on and no descriptors were declared. Honors Browsable/Display.
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
        /// Property descriptors for ItemsSource — tries ITypedList → IItemProperties →
        /// TypeDescriptor (sample item) → reflection (generic type), in that order.
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
                        ApplyFixedColumnLayout();
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
                        ApplyFixedColumnLayout();
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
                    ApplyFixedColumnLayout();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // Remove all previously generated columns
                    RemoveGeneratedColumns();
                    _gridColumnsGenerated = false;
                    // Re-generate if collection still has items
                    GenerateColumnsFromDescriptors();
                    break;
            }

            // Runtime descriptor changes can introduce or remove a column referenced by
            // FilterString — re-apply so seeded filters track the collection.
            if (!string.IsNullOrEmpty(FilterString))
                ApplyFilterString();
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

            ApplyFixedColumnLayout();
        }

        /// <summary>
        /// Sorts <see cref="DataGrid.Columns"/> into three groups based on each descriptor's
        /// <see cref="GridColumn.Fixed"/> value: left-pinned columns first, then unpinned
        /// (preserving their existing relative order), then right-pinned columns. Updates
        /// <see cref="DataGrid.FrozenColumnCount"/> to the number of left-pinned columns so
        /// WPF's native left-frozen layout takes effect, and resets it to 0 when no
        /// columns are pinned left.
        /// </summary>
        /// <remarks>
        /// Called whenever <see cref="GridColumn.Fixed"/> changes on any descriptor and once
        /// after the initial column generation. The reorder is performed by adjusting
        /// <see cref="DataGridColumn.DisplayIndex"/> rather than re-inserting columns, so
        /// callers don't observe collection-changed churn or lose their <c>SelectedCells</c>.
        /// </remarks>
        internal void ApplyFixedColumnLayout()
        {
            if (_applyingFixedColumnLayout) return;
            if (Columns.Count == 0) return;

            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);

            // Snapshot each column's descriptor and current DisplayIndex so we can sort by
            // (group, current display position) — preserving the user's existing visual
            // order within each pinning group instead of forcing descriptor-collection order.
            var entries = new List<(DataGridColumn Column, FixedColumnPosition Position, int CurrentDisplayIndex)>(Columns.Count);
            foreach (var column in Columns)
            {
                var descriptor = descriptors != null
                    ? descriptors.FirstOrDefault(d => d.InternalColumn == column)
                    : null;
                var position = descriptor?.Fixed ?? FixedColumnPosition.None;
                entries.Add((column, position, column.DisplayIndex));
            }

            int GroupOrder(FixedColumnPosition p) => p switch
            {
                FixedColumnPosition.Left => 0,
                FixedColumnPosition.None => 1,
                FixedColumnPosition.Right => 2,
                _ => 1,
            };

            var ordered = entries
                .OrderBy(e => GroupOrder(e.Position))
                .ThenBy(e => e.CurrentDisplayIndex)
                .Select(e => e.Column)
                .ToList();

            _applyingFixedColumnLayout = true;
            try
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (ordered[i].DisplayIndex != i)
                        ordered[i].DisplayIndex = i;
                }

                int leftCount = entries.Count(e => e.Position == FixedColumnPosition.Left);
                if (FrozenColumnCount != leftCount)
                    FrozenColumnCount = leftCount;
            }
            finally
            {
                _applyingFixedColumnLayout = false;
            }

            // Keep an open ColumnChooser in sync with the new pinning state — its
            // visual order (Left → None → Right) and per-row pin glyphs are derived
            // from each descriptor's Fixed value, so a context-menu-driven Pin
            // Left/Right/Unpin would otherwise leave the chooser stale until close.
            // The chooser guards against re-entry from its own reorder calls.
            _columnChooser?.OnGridFixedColumnLayoutChanged();
        }

        private bool _applyingFixedColumnLayout;

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
        /// Resolves <see cref="GridColumn.FieldType"/> from the data source for descriptors
        /// without an explicit one.
        /// </summary>
        /// <returns>True if any FieldType was newly resolved — caller may regenerate columns.</returns>
        private bool ResolveFieldTypesFromItemsSource()
        {
            if (ItemsSource == null)
                return false;

            var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
            if (descriptors == null || descriptors.Count == 0)
                return false;

            // Resolve item type and a sample instance once per pass; reuse across all descriptors.
            Type itemType = GetItemTypeFromSource(out object sampleItem);
            IItemProperties itemProps = GetItemPropertiesFromSource();

            bool anyResolved = false;
            foreach (var descriptor in descriptors)
            {
                if (ResolveFieldTypeForDescriptor(descriptor, itemType, itemProps, sampleItem))
                    anyResolved = true;
            }
            return anyResolved;
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

        /// <summary>
        /// Resolves FieldType for one descriptor. Returns true when a new value was written.
        /// </summary>
        private bool ResolveFieldTypeForDescriptor(GridColumn descriptor, Type itemType, IItemProperties itemProps, object sampleItem)
        {
            if (descriptor == null || descriptor.IsFieldTypeExplicit)
                return false;
            if (string.IsNullOrEmpty(descriptor.FieldName))
                return false;

            Type resolved = ResolveTypeForField(descriptor.FieldName, itemType, itemProps, sampleItem);
            if (resolved == null) return false;
            if (descriptor.FieldType == resolved) return false;

            descriptor.SetAutoFieldType(resolved);
            return true;
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

        /// <summary>
        /// True when the source is DataTable-backed and the named column is ReadOnly (typical
        /// for computed expression columns). False otherwise.
        /// </summary>
        internal bool IsSourceFieldReadOnly(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return false;
            var table = GetSourceDataTable();
            if (table == null) return false;
            if (!table.Columns.Contains(fieldName)) return false;
            return table.Columns[fieldName].ReadOnly;
        }

        /// <summary>Unwraps the items source through ICollectionView/IListSource to find the underlying DataTable.</summary>
        private System.Data.DataTable GetSourceDataTable()
        {
            object source = ItemsSource;
            for (int hop = 0; hop < 8 && source != null; hop++)
            {
                switch (source)
                {
                    case System.Data.DataTable dt:
                        return dt;
                    case System.Data.DataView dv:
                        return dv.Table;
                    case ICollectionView cv:
                        source = cv.SourceCollection;
                        continue;
                    case System.ComponentModel.IListSource ls:
                        source = ls.GetList();
                        continue;
                    default:
                        return null;
                }
            }
            return null;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// When applying the template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Re-subscribe editing events (template can re-apply at runtime).
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

            if (_templateFilterPanel != null)
            {
                _templateFilterPanel.FiltersEnabledChanged -= OnFiltersEnabledChanged;
                _templateFilterPanel.FilterRemoved -= OnFilterRemoved;
                _templateFilterPanel.ValueRemovedFromToken -= OnValueRemovedFromToken;
                _templateFilterPanel.OperatorToggled -= OnOperatorToggled;
                _templateFilterPanel.ClearAllFiltersRequested -= OnClearAllFiltersRequested;
                _templateFilterPanel.OpenFilterEditorRequested -= OnOpenFilterEditorRequested;
            }

            if (GetTemplateChild("PART_FilterPanel") is FilterPanel templateFilterPanel && templateFilterPanel != null)
            {
                templateFilterPanel.FiltersEnabled = FilterPanel.FiltersEnabled;
                templateFilterPanel.UpdateActiveFilters(FilterPanel.ActiveFilters);

                templateFilterPanel.FiltersEnabledChanged += OnFiltersEnabledChanged;
                templateFilterPanel.FilterRemoved += OnFilterRemoved;
                templateFilterPanel.ValueRemovedFromToken += OnValueRemovedFromToken;
                templateFilterPanel.OperatorToggled += OnOperatorToggled;
                templateFilterPanel.ClearAllFiltersRequested += OnClearAllFiltersRequested;
                templateFilterPanel.OpenFilterEditorRequested += OnOpenFilterEditorRequested;

                _templateFilterPanel = templateFilterPanel;
                FilterPanel = templateFilterPanel;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetupSelectAllColumnHeaders();
            }), DispatcherPriority.Loaded);

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
            // No UpdateLayout — the DataTrigger re-evaluates automatically, and forcing layout
            // here re-enters our CollectionChanged handlers.
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
            // BindingListCollectionView throws on predicate Items.Filter; wrap in
            // ListCollectionView so our filter pipeline works.
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

                    // Auto-generate descriptors before the first column pass.
                    if (!_gridColumnsGenerated)
                        EnsureAutoGeneratedDescriptors();

                    bool fieldTypesChanged = ResolveFieldTypesFromItemsSource();

                    // Late-binding case: grid loaded with null ItemsSource, so columns either
                    // weren't built yet or were built without type info. IsLoaded gate prevents
                    // generation before the implicit style applies CellStyle/RowStyle —
                    // CreateDataGridColumn copies Owner.CellStyle and would otherwise capture null.
                    if (IsLoaded)
                    {
                        var descriptors = (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
                        bool hasDescriptors = descriptors != null && descriptors.Count > 0;
                        if (!_gridColumnsGenerated && hasDescriptors)
                        {
                            GenerateColumnsFromDescriptors();
                        }
                        else if (_gridColumnsGenerated && fieldTypesChanged)
                        {
                            RemoveGeneratedColumns();
                            _gridColumnsGenerated = false;
                            GenerateColumnsFromDescriptors();
                        }
                    }

                    // Notify controls that items source has changed
                    ItemsSourceChanged?.Invoke(this, EventArgs.Empty);

                    // Apply FilterString now that ItemsSource is set and FieldTypes are resolved.
                    // ApplyFilterString itself calls FilterItemsSource on success.
                    bool filterStringApplied = false;
                    if (!string.IsNullOrEmpty(FilterString))
                    {
                        ApplyFilterString();
                        filterStringApplied = true;
                    }

                    // Apply any existing filters - check for active column filters, not just Items.Filter
                    if (!filterStringApplied && (HasActiveColumnFilters() || Items.Filter != null))
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
        /// "Source assigned" = "has items". Intentionally doesn't inspect the actual count —
        /// past attempts created CollectionChanged feedback loops with ListCollectionView over
        /// IBindingListView sources.
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