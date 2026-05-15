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
        /// Grid-wide default for when a click on a cell triggers edit mode. Individual editors
        /// override via <see cref="BaseEditSettings.EditorShowMode"/>. The grid default is
        /// <see cref="WPF.EditorShowMode.MouseDownFocused"/> — first click focuses the cell,
        /// second click on the focused cell enters edit. Set to
        /// <see cref="WPF.EditorShowMode.None"/> to require explicit Enter/F2 to edit.
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
        /// Dependency property for <see cref="ShowAutoFilterRow"/>. Defaults to <c>true</c>.
        /// Gates the auto-filter UI as a whole. Combined with
        /// <see cref="AutoFilterRowPositionProperty"/> to decide whether filter editors live
        /// in a pinned row below the headers or inside the column headers themselves.
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
        /// Grid-wide default for <see cref="ShowCriteriaInAutoFilterRow"/>. When <c>true</c>,
        /// every column's filter cell renders the leading <see cref="SearchTypeSelector"/>
        /// glyph so the user can pick the active operator (Contains / StartsWith / Equals / ...)
        /// inline. When <c>false</c> (the default), the selector is hidden and the column's
        /// default search type is used implicitly. Individual columns can override via
        /// <see cref="GridColumn.ShowCriteriaInAutoFilterRow"/> — column override wins when set.
        /// </summary>
        /// <remarks>
        /// <see cref="FrameworkPropertyMetadataOptions.Inherits"/> is set for cross-tree
        /// composition scenarios, but the per-cell effective value is resolved explicitly by
        /// <see cref="ColumnFilterControl"/> against this grid's CLR property, so runtime
        /// changes always reach the hosts via <see cref="OnShowCriteriaInAutoFilterRowChanged"/>.
        /// </remarks>
        public static readonly DependencyProperty ShowCriteriaInAutoFilterRowProperty =
            DependencyProperty.Register(nameof(ShowCriteriaInAutoFilterRow), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnShowCriteriaInAutoFilterRowChanged));

        /// <summary>
        /// Grid-wide default <see cref="Style"/> applied to every <see cref="ColumnFilterControl"/>
        /// hosted inside the auto-filter row. A column can override via
        /// <see cref="GridColumn.AutoFilterRowCellStyle"/> — column override wins when set.
        /// When both are <c>null</c> the control resolves the keyed theme style under
        /// <see cref="SdgThemeKeys.ColumnFilterControl"/>.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowCellStyleProperty =
            DependencyProperty.Register(nameof(AutoFilterRowCellStyle), typeof(Style), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null, OnAutoFilterRowCellStyleChanged));

        /// <summary>
        /// Debounce window (in milliseconds) before keystroke-driven filter updates fire.
        /// Defaults to <c>0</c> — every keystroke applies the filter immediately. A positive
        /// value buffers rapid typing so the filter expression is rebuilt only after the user
        /// pauses for the specified interval. Only takes effect when live filtering is enabled
        /// for the column (see <see cref="GridColumn.ImmediateUpdateAutoFilter"/>); when live
        /// filtering is off, the delay is irrelevant — filters fire on Enter / Tab / lost-focus
        /// regardless. <see cref="FrameworkPropertyMetadataOptions.Inherits"/> lets per-cell
        /// <see cref="ColumnFilterControl"/> read the grid value without explicit wiring.
        /// </summary>
        public static readonly DependencyProperty FilterRowDelayProperty =
            DependencyProperty.Register(nameof(FilterRowDelay), typeof(int), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Grid-wide policy for when the per-cell clear (X) button appears in the auto-filter
        /// row. Defaults to <see cref="WPF.AutoFilterRowClearButtonMode.Always"/> — the button
        /// is shown whenever a cell has an active filter, regardless of which editor surface
        /// is rendering. <see cref="FrameworkPropertyMetadataOptions.Inherits"/> propagates
        /// the value into the per-cell control without manual wiring.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowClearButtonModeProperty =
            DependencyProperty.Register(nameof(AutoFilterRowClearButtonMode), typeof(AutoFilterRowClearButtonMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(AutoFilterRowClearButtonMode.Always, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Walks <see cref="DataColumns"/> and asks each <see cref="ColumnFilterControl"/> to
        /// re-resolve its effective ShowCriteria value when the grid-level DP changes.
        /// </summary>
        private static void OnShowCriteriaInAutoFilterRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            foreach (var host in grid.DataColumns)
            {
                if (host is ColumnFilterControl ctl)
                    ctl.RefreshEffectiveShowCriteria();
            }
        }

        /// <summary>
        /// Walks <see cref="DataColumns"/> and re-resolves the cell style on each
        /// <see cref="ColumnFilterControl"/> when the grid-level style DP changes.
        /// </summary>
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
        ///                     DefaultSearchType="StartsWith" EnableRuleFiltering="False" /&gt;
        /// &lt;/sdg:SearchDataGrid.GridColumns&gt;
        /// </code>
        /// </example>
        public FreezableCollection<GridColumn> GridColumns
        {
            get => (FreezableCollection<GridColumn>)GetValue(GridColumnsProperty);
        }

        /// <summary>
        /// Gets or sets whether the auto-filter UI is shown. When <c>true</c>, filter editors
        /// are surfaced for each filterable column (placement controlled by
        /// <see cref="AutoFilterRowPosition"/>). When <c>false</c>, no per-column filter UI is
        /// rendered. Defaults to <c>true</c>.
        /// </summary>
        public bool ShowAutoFilterRow
        {
            get => (bool)GetValue(ShowAutoFilterRowProperty);
            set => SetValue(ShowAutoFilterRowProperty, value);
        }

        /// <summary>
        /// Gets or sets where the auto-filter UI is placed. <see cref="WPF.AutoFilterRowPosition.Cell"/>
        /// (default) renders a dedicated pinned row beneath the column headers;
        /// <see cref="WPF.AutoFilterRowPosition.Header"/> embeds filter editors inside each
        /// column header (expand-on-click).
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
        /// Row-count threshold (in <see cref="OriginalItemsCount"/> terms) that flips the default
        /// filter-application mode from "live as the user types / clicks" to "deferred until the
        /// editor closes or the user explicitly commits". The same threshold is consulted by
        /// <see cref="ColumnSearchBox"/> (for the in-header text box debounce) and by
        /// <see cref="ColumnFilterEditor"/> (for the popup's apply-on-change behavior). Override
        /// per-column / per-editor via their explicit live-mode properties when needed.
        /// </summary>
        public const int LiveFilteringRowCountThreshold = 100_000;

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

            // Edit-on-focus support: flag-driven instead of always-on. The OnAnyDescendantGotFocus
            // handler only triggers BeginEdit when _carryEditStateOnNextFocus is set. The flag is
            // raised by:
            //   • a left-click on a cell (PreviewMouseLeftButtonDown handler below) — click-to-edit
            //   • a Tab/Shift+Tab keypress on a cell that is currently editing (OnGridPreviewKeyDown) —
            //     so Tab carries edit state forward
            // Tab from a non-editing cell, Arrow keys, and programmatic focus changes all leave the
            // flag clear, so the destination cell ends up focused but not editing.
            // handledEventsToo on GotFocus because upstream selection logic frequently marks it.
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
        /// When true, the next focus change into a <see cref="DataGridCell"/> should auto-enter
        /// edit mode. Raised by left-clicks on cells (click-to-edit) and by Tab/Shift+Tab from a
        /// cell that is already editing (Tab carries edit state forward). Cleared as soon as
        /// <see cref="OnAnyDescendantGotFocus"/> consumes it, AND at the start of every keyboard
        /// pre-handler so a stale flag from a click-on-already-editing-cell doesn't bleed into
        /// the next key navigation.
        /// </summary>
        private bool _carryEditStateOnNextFocus;

        /// <summary>
        /// The cell that just had a deferred <c>BeginEdit</c> queued by
        /// <see cref="OnAnyDescendantGotFocus"/>. Used by the arrow-key handler in
        /// <see cref="OnGridPreviewKeyDown"/> to detect the press-and-hold case where auto-repeat
        /// arrives faster than the dispatcher can drain the queued <c>BeginEdit</c> — in that
        /// window the cell has focus but isn't editing yet, and a naive arrow-nav would move
        /// focus on without preserving edit state. Re-arming the carry flag in that case keeps
        /// the held-arrow navigation editing every cell along the way.
        /// </summary>
        private DataGridCell _pendingEditCell;

        /// <summary>
        /// Sets the internal "next focused cell should auto-edit" flag — consumed exactly
        /// once by the next eligible <see cref="DataGridCell"/> in <c>OnAnyDescendantGotFocus</c>.
        /// Called by <see cref="BaseEditSettings.ExitCellViaArrow"/> after committing the
        /// source cell's edit so the destination cell stays in edit mode after arrow nav,
        /// matching the user's expectation that arrow-key navigation while editing carries
        /// the edit context across cells.
        /// </summary>
        internal void SetCarryEditStateOnNextFocus() => _carryEditStateOnNextFocus = true;

        /// <summary>
        /// True when the cell or its owning grid is read-only. Necessary because
        /// DataGridCell.IsReadOnly is a coerced transfer DP that should reflect
        /// DataGrid.IsReadOnly per WPF's coercion rules — but in practice the grid→cell
        /// propagation doesn't fire reliably for cells in DataGridTemplateColumn, leaving
        /// cell.IsReadOnly == false even when the grid is fully read-only. Every gating
        /// check in this file goes through here so grid-level read-only actually blocks
        /// the gestures (mouse-toggle, Space/Enter, click-to-edit, etc.).
        /// </summary>
        private bool IsCellOrGridReadOnly(DataGridCell cell)
            => cell == null || cell.IsReadOnly || IsReadOnly;

        /// <summary>
        /// Captures whether the cell under the cursor was already focused before the current
        /// mouse-down arrived. The <c>*Focused</c> editor-show modes consult this on
        /// mouse-up / focus-change to distinguish "clicking an unfocused cell" (gives focus only)
        /// from "clicking an already-focused cell" (enters edit).
        /// </summary>
        private bool _wasCellFocusedAtMouseDown;

        /// <summary>
        /// The click point (in <see cref="DataGridCell"/>-local coordinates) for the most recent
        /// mouse-driven BeginEdit. Captured when a mouse-up triggers our BeginEdit (MouseUp /
        /// MouseUpFocused) and when <see cref="OnBeginningEdit"/> allows the stock mouse-down
        /// edit through (MouseDown / MouseDownFocused). TextBox-based editors consume this on
        /// first GotKeyboardFocus to land the caret at the click index instead of selecting all
        /// text — keyboard entry (Tab / Enter / F2 / programmatic) leaves it null and falls back
        /// to select-all.
        /// </summary>
        private Point? _pendingMouseEditCellPoint;
        private DataGridCell _pendingMouseEditCell;

        internal void StashMouseEditPoint(DataGridCell cell, Point cellPoint)
        {
            _pendingMouseEditCell = cell;
            _pendingMouseEditCellPoint = cellPoint;
        }

        /// <summary>
        /// Returns and clears the pending mouse-edit click point if one was stashed for
        /// <paramref name="forCell"/>. Editors call this once during their first GotKeyboardFocus;
        /// the consume-on-read pattern ensures a stale point from a cancelled / superseded
        /// gesture can't bleed into a later keyboard-driven edit on the same or a different cell.
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
        /// Click-to-edit: on mouse-down, decide whether the cell should enter edit mode now
        /// (<see cref="EditorShowMode.MouseDown"/> /
        /// <see cref="EditorShowMode.MouseDownFocused"/>) or wait for mouse-up. For
        /// <c>*Focused</c> modes, the gesture only triggers edit when the cell already had
        /// focus before the click arrived.
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

            // Stash the click point for the editor that's about to materialize. WPF's stock
            // DataGridCell click-to-edit invokes BeginEdit() (parameterless), so the OnBeginningEdit
            // mouse-args branch in SearchDataGridEditing.cs never gets called for stock-fired
            // edits — without this stash the TextBox would always select-all on focus. Captured
            // here, after the shouldEditNow gate, so it covers both the already-focused branch
            // (deferred BeginEdit below) and the newly-focusing branch (OnAnyDescendantGotFocus).
            StashMouseEditPoint(cell, e.GetPosition(cell));

            // Newly-focusing case: arm the carry flag so OnAnyDescendantGotFocus enters edit
            // mode after the click moves focus. Already-focused case: no GotFocus will fire,
            // so begin edit directly (deferred a tick to let the focus pipeline settle).
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
        /// Mouse-up handler for the <see cref="EditorShowMode.MouseUp"/> /
        /// <see cref="EditorShowMode.MouseUpFocused"/> modes. Called after the mouse-down has
        /// moved focus to the cell (so by mouse-up the cell is focused regardless of whether
        /// it was beforehand). The <c>*Focused</c> variant gates on the captured pre-click
        /// focus state.
        /// </summary>
        private void OnPreviewMouseLeftButtonUp_BeginEdit(object sender, MouseButtonEventArgs e)
        {
            var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || IsCellOrGridReadOnly(cell) || cell.IsEditing) return;

            var mode = ResolveEditorShowMode(cell);
            bool shouldEdit = mode == EditorShowMode.MouseUp
                || (mode == EditorShowMode.MouseUpFocused && _wasCellFocusedAtMouseDown);
            if (!shouldEdit) return;

            // Stash the click point so TextBox editors place the caret where the user pointed.
            // OnBeginningEdit fires programmatically from our deferred BeginEdit() below with
            // EditingEventArgs == null, so it can't capture the point on its own.
            StashMouseEditPoint(cell, e.GetPosition(cell));

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!cell.IsEditing && cell.IsKeyboardFocusWithin)
                    BeginEdit();
            }), DispatcherPriority.Input);
        }

        /// <summary>
        /// Keyboard state machine for the cell:
        ///   • <see cref="Key.Enter"/> toggles edit state on the focused cell — commits if editing
        ///     (focus stays on the cell), otherwise begins edit. Always handled.
        ///   • <see cref="Key.Tab"/> / Shift+Tab: if the source cell is editing, set the carry flag
        ///     so the destination cell auto-edits. Not handled — DataGrid's native Tab navigation
        ///     still runs to commit the source and move focus.
        ///   • Arrow keys: leave the flag clear so destination is focus-only. Not handled — TextBox
        ///     editors override arrow keys themselves with caret-aware exit logic; non-text editors
        ///     defer to DataGrid's native arrow navigation.
        /// </summary>
        private void OnGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reset the flag at the start of every key event. A click on an already-editing cell
            // sets the flag but doesn't trigger a focus change (focus is already there), so the
            // flag would otherwise leak into the next key-driven focus change.
            _carryEditStateOnNextFocus = false;

            var focused = Keyboard.FocusedElement as DependencyObject;
            var cell = FindAncestor<DataGridCell>(focused);
            if (cell == null) return;

            switch (e.Key)
            {
                case Key.Enter:
                    {
                        // CheckBox-style cell: Enter toggles the value directly (toggle-edit-state
                        // has no meaningful visual since display and edit are both interactive
                        // CheckBoxes). Falls through to the standard toggle-edit semantics for
                        // non-CheckBox cells.
                        var checkBox = FindCheckBoxInCell(cell);
                        if (checkBox != null && !IsCellOrGridReadOnly(cell))
                        {
                            checkBox.IsChecked = !(checkBox.IsChecked ?? false);
                            e.Handled = true;
                            break;
                        }

                        if (cell.IsEditing)
                        {
                            // Commit at the Row level (not parameterless, which commits the cell
                            // only and leaves DataGridRow.IsEditing=true until row navigation).
                            // The row-header pencil indicator binds to DataGridRow.IsEditing, so a
                            // cell-only commit would leave the pencil stuck until the user moved
                            // off the row — visually contradicting the user's "Enter = done
                            // editing" gesture. Row-level commit flips IsEditing → false and the
                            // indicator falls back to the focused-row chevron immediately. Within-
                            // row Tab still uses cell-only commit so mid-row tabbing keeps the
                            // pencil up. cell.Focus() ensures focus lands on the cell itself
                            // rather than on whatever WPF picks after the editor goes away.
                            CommitEdit(DataGridEditingUnit.Row, true);
                            cell.Focus();
                        }
                        else if (!IsCellOrGridReadOnly(cell))
                        {
                            // Bypass the EditOnFocus gate — Enter is an explicit edit-intent gesture
                            // and should work even on columns that opt out of click/Tab auto-edit.
                            BeginEdit();
                        }
                        e.Handled = true;
                        break;
                    }

                case Key.Space:
                    {
                        // Standard CheckBox-cell keyboard UX: Space toggles the value. Works in
                        // both display mode (cell focused, display CheckBox has Focusable=False so
                        // wouldn't otherwise receive Space) and edit mode (preempts the inner
                        // CheckBox's own Space handling — same toggle, just programmatic).
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
                        // Wrap intercepted — focus is on the opposite-end cell of the same row,
                        // carry flag set so the destination auto-edits the same way Tab would
                        // anywhere else.
                        e.Handled = true;
                        break;
                    }

                    if (cell.IsEditing)
                        _carryEditStateOnNextFocus = true;
                    // Don't mark handled — DataGrid's native Tab handler commits + moves focus.
                    break;
                }

                // Up / Down: no flag manipulation. Native DataGrid arrow handler navigates
                // between rows. (In edit mode, the editor's own PreviewKeyDown handles these.)
                // Up additionally hands off from the first data row to the filter row when
                // ShowAutoFilterRow is on — symmetric with the filter row's Down handoff
                // implemented in FilterRowNavigator. The handoff only fires when focus is
                // on the cell shell itself; an editor descendant gets to interpret Up first
                // (multiline TextBox caret-up, ComboBox selection, DatePicker calendar) so
                // we don't hijack arrow keys from editors that have a real use for them.
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

                // Left / Right: wrap at the row edge — Right on the rightmost cell jumps to
                // the FIRST cell of the NEXT row, Left on the leftmost cell jumps to the LAST
                // cell of the PREVIOUS row (Tab still wraps within the same row). For fully-
                // settled editing cells where the editor has focus, this case is a no-op —
                // the editor's PreviewKeyDown routes through BaseEditSettings.ExitCellViaArrow,
                // which calls the same wrap helper. The two transitional cases below cover the
                // press-and-hold windows where focus is on the cell rather than the editor.
                case Key.Left:
                case Key.Right:
                {
                    // Two transitional press-and-hold states put focus on the cell rather
                    // than its inner editor — in both, the editor's own PreviewKeyDown won't
                    // fire (it isn't focused) and DataGrid's native KeyDown handler would
                    // move focus without the carry flag, dropping edit mode mid-hold:
                    //
                    //   • !cell.IsEditing && _pendingEditCell == cell:
                    //       BeginEdit was queued for this cell by the previous auto-repeat's
                    //       OnAnyDescendantGotFocus but hasn't drained yet.
                    //   • cell.IsEditing && focused == cell:
                    //       BeginEdit drained, but the editor's deferred focus action
                    //       (DispatcherPriority.Input) hasn't run, so focus is still on the
                    //       cell. Commit explicitly so the destination's BeginEdit can take
                    //       over on the next OnAnyDescendantGotFocus.
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
        /// Find a <see cref="CheckBox"/> in the cell's visual tree (display or edit content).
        /// A non-null result identifies the cell as CheckBox-style for keyboard-toggle UX —
        /// works for both <see cref="CheckBoxEditSettings"/>-driven cells and any column that
        /// happens to host a CheckBox in its template.
        /// </summary>
        private static CheckBox FindCheckBoxInCell(DataGridCell cell)
        {
            return FindVisualDescendant<CheckBox>(cell);
        }

        /// <summary>
        /// Implements horizontal Tab wrap within a row: Tab at the last visible cell loops
        /// back to the first visible cell of the same row, Shift+Tab at the first visible cell
        /// loops to the last. The carry-edit flag is set so the destination behaves the same
        /// way DataGrid's native Tab does for non-wrap moves — auto-editing if the column is
        /// EditOnFocus-eligible. Returns <c>true</c> when a wrap was performed (caller marks
        /// the event handled); <c>false</c> when the cell isn't at a row edge so the native
        /// Tab handler continues into the next cell normally.
        /// </summary>
        private bool TryWrapTabWithinRow(DataGridCell cell, KeyEventArgs e)
        {
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            var targetCell = GetWrapTargetAtRowEdge(cell, forward: !isShift, crossRow: false);
            if (targetCell == null) return false;

            // Commit the source cell's edit before navigating so its bound source updates,
            // matching the behavior the native Tab handler produces for non-wrap moves.
            if (cell.IsEditing)
            {
                CommitEdit();
                _carryEditStateOnNextFocus = true;
            }
            else if (IsCellAutoEditEligible(targetCell))
            {
                // Even when the source wasn't editing, signal carry so the destination cell
                // enters edit mode if its column opts in — matches the user's "tab through
                // editable cells" expectation across the wrap boundary.
                _carryEditStateOnNextFocus = true;
            }

            targetCell.Focus();
            return true;
        }

        /// <summary>
        /// Horizontal arrow-key wrap at row edges. Used by both the non-editing path
        /// (<see cref="OnGridPreviewKeyDown"/>'s Left/Right cases) and the editing path
        /// (<see cref="BaseEditSettings.ExitCellViaArrow"/>) so Right at the rightmost
        /// visible cell loops to the FIRST visible cell of the NEXT row, and Left at the
        /// leftmost loops to the LAST visible cell of the PREVIOUS row (vs. Tab, which
        /// wraps within the same row). At the grid's outer edge — Right on the last row's
        /// last cell, Left on the first row's first cell — there's no adjacent row to wrap
        /// to and this returns <c>false</c> so the caller falls back to native handling
        /// (which simply doesn't move). Carry-edit flag is set when the source was editing
        /// so the destination respects the column's EditOnFocus setting. Returns
        /// <c>false</c> when the cell isn't at an edge or no adjacent row exists (caller
        /// continues with the normal arrow-handling path).
        /// </summary>
        internal bool TryWrapArrowAtRowEdge(DataGridCell cell, bool forward)
        {
            var targetCell = GetWrapTargetAtRowEdge(cell, forward, crossRow: true);
            if (targetCell == null) return false;

            // Carry edit state ONLY when the source cell was already editing — the user is
            // navigating between editing cells and expects the wrap-around to preserve edit
            // mode. When the source isn't editing, plain arrow nav must stay focus-only,
            // even if the destination column is auto-edit-eligible (clicking-to-edit only
            // activates on click gestures, not on arrow-key wrap).
            if (cell.IsEditing)
                _carryEditStateOnNextFocus = true;

            targetCell.Focus();
            return true;
        }

        /// <summary>
        /// Returns the wrap-destination cell when <paramref name="cell"/> is at the row's
        /// edge in the requested direction, or <c>null</c> when there's no wrap to do
        /// (cell isn't at an edge, no visible columns, row not realized yet, no adjacent
        /// row when crossing rows, etc.). <paramref name="crossRow"/> selects between
        /// within-row wrap (Tab — Right edge → first cell of <em>same</em> row) and
        /// cross-row wrap (Arrow — Right edge → first cell of <em>next</em> row).
        /// For cross-row, virtualized adjacent rows are realized via
        /// <see cref="DataGrid.ScrollIntoView(object)"/> before the cell lookup.
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
                // Pull the row item from Items (the filtered/sorted view, what's actually
                // displayed) so wrap navigates the visible sequence rather than the raw
                // ItemsSource order.
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
                // Virtualized: force realization so we can focus the cell synchronously.
                // ScrollIntoView + UpdateLayout brings the row container into existence in
                // the same dispatcher frame, matching the caller's synchronous wrap contract
                // (Focus must land before the caller returns to native nav).
                ScrollIntoView(targetRowItem);
                UpdateLayout();
                row = ItemContainerGenerator.ContainerFromItem(targetRowItem) as DataGridRow;
            }
            if (row == null) return null;

            return GetCellAt(row, targetColumn);
        }

        /// <summary>
        /// Returns the <see cref="DataGridCell"/> at the given column inside the given row's
        /// cell presenter, or <c>null</c> if the row hasn't realized its template yet.
        /// </summary>
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
        /// edit mode and routes the typed character into the new editor's TextBox content. Mirrors
        /// the Excel/Google Sheets UX where typing replaces the cell's value.
        /// </summary>
        /// <remarks>
        /// Hooks <see cref="UIElement.PreviewTextInputEvent"/> rather than <see cref="UIElement.PreviewKeyDownEvent"/>
        /// so we get properly mapped Unicode text (handles dead-keys, IME, modifier-aware keymaps)
        /// instead of raw Key codes. After <c>BeginEdit</c>, defer the character injection at
        /// <see cref="DispatcherPriority.Background"/> so it runs <em>after</em> the editor's own
        /// <c>AutoFocusOnLoad</c> (queued at <see cref="DispatcherPriority.Input"/>) has moved focus
        /// and the existing <c>GotKeyboardFocus → SelectAll</c> behavior has run — setting Text then
        /// replaces the selected content with the typed character (Excel-like type-to-replace).
        /// <para>
        /// Skipped for CheckBox cells (Space toggle is in <see cref="OnGridPreviewKeyDown"/>) and
        /// ComboBox cells (the editor isn't text-input — type-to-edit would lose the keystroke
        /// and surprise the user). Other column types accept the injection; if it doesn't parse
        /// as a valid value, the edit reverts on commit, same as Excel.
        /// </para>
        /// </remarks>
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

            // After the dispatcher has run AutoFocusOnLoad (Input priority) and the editor's
            // GotKeyboardFocus→SelectAll, inject the typed character. Background runs after
            // Input drains, so ordering is deterministic without relying on tick counts.
            string typedText = e.Text;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var tb = FindVisualDescendant<TextBox>(cell);
                if (tb == null) return;

                // Masked TextBoxes can't take a bare Text assignment — that wipes the existing
                // value (e.g. account number "1234-5678-9012-3456" → "7") instead of overwriting
                // just the first character. Route the typed char through PreviewTextInput so
                // MaskInputBehavior sees it. OnGotFocus selected the entire first editable
                // region; collapse that to length 1 so ClearSelection + InsertChar overwrites
                // only the first character (yielding "7234-5678-9012-3456") rather than blanking
                // the whole region.
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
        /// Resolves the effective <see cref="WPF.EditorShowMode"/> for a cell. The column's
        /// <see cref="BaseEditSettings.EditorShowMode"/> wins when set to anything other than
        /// <see cref="WPF.EditorShowMode.Default"/>; otherwise the grid-level
        /// <see cref="EditorShowMode"/> applies. A grid-level <c>Default</c> means
        /// "no auto-edit" — cells focus on click but don't enter edit until the user explicitly
        /// invokes Enter / F2.
        /// </summary>
        private EditorShowMode ResolveEditorShowMode(DataGridCell cell)
        {
            var descriptor = FindGridColumnDescriptor(cell?.Column);
            var fromSettings = descriptor?.EditSettings?.EditorShowMode ?? EditorShowMode.Default;
            return fromSettings != EditorShowMode.Default ? fromSettings : EditorShowMode;
        }

        /// <summary>
        /// Whether a cell is eligible for Tab/arrow carry-edit. Any of the active modes
        /// (MouseDown, MouseDownFocused, MouseUp, MouseUpFocused) counts — the user opted into
        /// click-to-edit, so Tab navigation should carry the edit context the same way.
        /// <see cref="WPF.EditorShowMode.Default"/> and <see cref="WPF.EditorShowMode.None"/>
        /// both leave cells focus-only.
        /// </summary>
        private bool IsCellAutoEditEligible(DataGridCell cell)
        {
            var mode = ResolveEditorShowMode(cell);
            return mode != EditorShowMode.Default && mode != EditorShowMode.None;
        }

        /// <summary>
        /// Auto-edit-on-focus, but only when the carry flag has been set by a recent click or
        /// Tab-from-editing. Bare programmatic focus changes (Tab from non-editing, Arrow keys,
        /// initial focus into the grid) leave the flag clear, so the destination cell stays
        /// focus-only and the user can use Enter to opt into edit mode explicitly.
        /// </summary>
        private void OnAnyDescendantGotFocus(object sender, RoutedEventArgs e)
        {
            var cell = e.OriginalSource as DataGridCell ?? FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null || cell.IsEditing || IsCellOrGridReadOnly(cell)) return;

            if (!_carryEditStateOnNextFocus) return;
            _carryEditStateOnNextFocus = false;

            if (!IsCellAutoEditEligible(cell)) return;

            // Track this cell as the pending-edit target so the arrow-key handler can carry
            // edit state forward if auto-repeat fires before the deferred BeginEdit runs.
            _pendingEditCell = cell;

            // Defer BeginEdit one dispatcher tick — calling it directly inside GotFocus can
            // race the focus pipeline and leave the editor un-focusable.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!cell.IsEditing && cell.IsKeyboardFocusWithin)
                    BeginEdit();
                // Only clear when this action's cell is still the latest pending target —
                // a faster auto-repeat may have moved on and queued another cell, and that
                // newer pending should keep its tracking until its own action runs.
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
        /// <returns>
        /// <c>true</c> if any descriptor's <see cref="GridColumn.FieldType"/> was newly resolved
        /// during this pass — the caller can use this to decide whether to regenerate the WPF
        /// columns so type-driven defaults (auto <c>EditSettings</c>, column type) take effect.
        /// </returns>
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
        /// Returns <c>true</c> when a new <see cref="GridColumn.FieldType"/> value was written —
        /// either because the descriptor had no FieldType yet, or because the data source resolves
        /// it to a different type than the auto value already on the descriptor.
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
        /// Returns true when the items source is a <see cref="System.Data.DataTable"/>-backed
        /// collection and the named column is flagged <see cref="System.Data.DataColumn.ReadOnly"/>
        /// (typical for computed expression columns). Returns false for non-DataTable sources, an
        /// unknown <paramref name="fieldName"/>, or when no source is bound yet.
        /// </summary>
        internal bool IsSourceFieldReadOnly(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return false;
            var table = GetSourceDataTable();
            if (table == null) return false;
            if (!table.Columns.Contains(fieldName)) return false;
            return table.Columns[fieldName].ReadOnly;
        }

        /// <summary>
        /// Walks the items source — including <see cref="ICollectionView"/> wrappers — and returns
        /// the underlying <see cref="System.Data.DataTable"/>, or null if the source is not
        /// DataTable-backed.
        /// </summary>
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
                    // Tracks whether anything actually changed so we know to regenerate columns
                    // built earlier without type information.
                    bool fieldTypesChanged = ResolveFieldTypesFromItemsSource();

                    // Cover the late-binding case: the grid loaded with a null ItemsSource, so
                    // either no columns were generated yet (auto-gen path: descriptors arrived just
                    // now via EnsureAutoGeneratedDescriptors) or columns were generated with null
                    // FieldType (manual path: descriptors from XAML, but type resolution had no
                    // data source to run against). Either way, build/rebuild columns now that we
                    // have a real source — otherwise the grid renders empty (auto-gen) or with
                    // plain text columns missing type-driven default EditSettings (manual).
                    //
                    // Gated on IsLoaded so we don't generate columns before the implicit style
                    // has applied the grid's CellStyle/RowStyle. CreateDataGridColumn copies
                    // Owner.CellStyle into per-column styles (BuildStretchingCellStyle) — running
                    // pre-template would capture null and drop the styled setters. The Loaded
                    // handler still covers the initial build.
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