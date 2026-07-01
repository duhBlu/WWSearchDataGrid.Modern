using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWControls.Core;
using WWControls.Wpf.Display;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Per-column filter editor hosting the search-type selector, a data-type-matched editor,
    /// a clear button, and the rule-filter popup button. Mutates the column's
    /// <see cref="SearchTemplateController"/> directly and triggers
    /// <see cref="SearchDataGrid.FilterItemsSource(int)"/> on changes.
    /// </summary>
    public partial class ColumnFilterControl : Control, IColumnFilterHost
    {
        static ColumnFilterControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(typeof(ColumnFilterControl)));

            // Host must be focusable + a tab stop: click on the read-only display surface
            // lands focus on the host; NoInput operators have no inner editor to focus;
            // FilterRowNavigator needs an anchor to route Tab/arrows in DisplayIndex order
            // (FilterRowPanel children are in column-insertion order, not display order).
            FocusableProperty.OverrideMetadata(typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(true));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(true));
            FocusVisualStyleProperty.OverrideMetadata(typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata((Style)null));
        }

        /// <summary>The column context for the filter editor (upcast of <see cref="GridColumn"/>). Satisfies <see cref="IFilterEditorHost.EditorColumn"/>.</summary>
        public IEditorColumn EditorColumn => GridColumn;

        /// <summary>Routes a boundary arrow key to the filter row's DisplayIndex-order navigation. Satisfies <see cref="IFilterEditorHost.TryNavigateOnArrow"/>.</summary>
        public bool TryNavigateOnArrow(KeyEventArgs e) => FilterRowNavigator.TryNavigate(this, e);

        #region Template parts and fields

        public const string PartSearchTextBoxName = "PART_SearchTextBox";
        public const string PartFilterCheckBoxName = "PART_FilterCheckBox";
        public const string PartSearchTypeSelectorName = "PART_SearchTypeSelector";
        public const string PartEditorHostName = "PART_EditorHost";
        public const string PartClearFilterButtonName = "PART_ClearFilterButton";
        public const string PartRuleFilterEditorButtonName = "PART_RuleFilterEditorButton";

        private TextBox _searchTextBox;
        private CheckBox _filterCheckBox;
        private SearchTypeSelector _searchTypeSelector;
        private DependencyPropertyDescriptor _searchTypeSelectorDropDownDescriptor;
        private ContentPresenter _editorHost;
        private UIElement _filterEditor;
        private Popup _filterPopup;
        private ColumnFilterPopup _filterContent;
        private DispatcherTimer _changeTimer;
        private SearchTemplate _temporarySearchTemplate;
        private CheckboxCycleState _checkboxCycleState = CheckboxCycleState.Intermediate;
        private bool _isInitialState = true;
        private bool _suppressSearchTextSync;
        /// <summary>Data context for user-supplied FilterRow templates. Non-null only while a template is active.</summary>
        private EditGridCellData _filterCellData;

        /// <summary>Re-entrancy guard masking the focus dip during display/edit surface swap.</summary>
        private bool _isSwappingSurfaces;

        /// <summary>Currently-subscribed controller. Tracked separately so a recycled cell can detach from the prior column's controller before subscribing to the new one.</summary>
        private SearchTemplateController _subscribedController;

        /// <summary>
        /// Snapshot of the input that the most recent Enter/Tab in this cell committed as a
        /// temp template. When live filtering is off, a second Enter on unchanged input
        /// promotes that temp template to a permanent rule and resets the editor — this
        /// snapshot is how that "unchanged input" check is made.
        /// </summary>
        private string _lastCommittedSearchText;
        private object _lastCommittedSearchValue;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CurrentColumnProperty =
            DependencyProperty.Register(nameof(CurrentColumn), typeof(DataGridColumn), typeof(ColumnFilterControl),
                new PropertyMetadata(null, OnCurrentColumnChanged));

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register(nameof(SourceDataGrid), typeof(SearchDataGrid), typeof(ColumnFilterControl),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSearchTextChanged));

        public static readonly DependencyProperty SearchValueProperty =
            DependencyProperty.Register(nameof(SearchValue), typeof(object), typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSearchValueChanged));

        public static readonly DependencyProperty SelectedSearchTypeProperty =
            DependencyProperty.Register(nameof(SelectedSearchType), typeof(SearchType), typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(SearchType.Contains,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedSearchTypeChanged));

        public static readonly DependencyProperty HeaderDisplayStateProperty =
            DependencyProperty.Register(nameof(HeaderDisplayState), typeof(HeaderDisplayState), typeof(ColumnFilterControl),
                new PropertyMetadata(HeaderDisplayState.Editing, OnHeaderDisplayStateChanged));

        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.Register(nameof(HasActiveFilter), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        private static readonly DependencyPropertyKey HasEditorInputValuePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasEditorInputValue), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        /// <summary>
        /// True when the auto-filter row editor itself has clearable input. Narrower than
        /// <see cref="HasActiveFilter"/>, which also covers rule-filter commits the X button can't clear.
        /// </summary>
        public static readonly DependencyProperty HasEditorInputValueProperty = HasEditorInputValuePropertyKey.DependencyProperty;

        public static readonly DependencyProperty HasAdvancedFilterProperty =
            DependencyProperty.Register(nameof(HasAdvancedFilter), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsCheckboxColumnProperty =
            DependencyProperty.Register(nameof(IsCheckboxColumn), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty FilterCheckboxStateProperty =
            DependencyProperty.Register(nameof(FilterCheckboxState), typeof(bool?), typeof(ColumnFilterControl),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnFilterCheckboxStateChanged));

        public static readonly DependencyProperty IsComplexFilteringEnabledProperty =
            DependencyProperty.Register(nameof(IsComplexFilteringEnabled), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ColumnDataTypeProperty =
            DependencyProperty.Register(nameof(ColumnDataType), typeof(ColumnDataType), typeof(ColumnFilterControl),
                new PropertyMetadata(ColumnDataType.Unknown));

        public static readonly DependencyProperty IsFilterVisibleProperty =
            DependencyProperty.Register(nameof(IsFilterVisible), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsFilterEnabledProperty =
            DependencyProperty.Register(nameof(IsFilterEnabled), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(true, OnIsFilterEnabledChanged));

        private static readonly DependencyPropertyKey EffectiveShowCriteriaPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(EffectiveShowCriteria), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        /// <summary>
        /// Resolved "show the inline search-type selector" — column override beats grid setting.
        /// </summary>
        public static readonly DependencyProperty EffectiveShowCriteriaProperty = EffectiveShowCriteriaPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey HasFilterRowTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasFilterRowTemplate), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        /// <summary>
        /// True when a user-supplied FilterRow template is driving the editor host —
        /// suppresses the default boolean checkbox/editor switching.
        /// </summary>
        public static readonly DependencyProperty HasFilterRowTemplateProperty = HasFilterRowTemplatePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsFilterCellEditingPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsFilterCellEditing), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false));

        /// <summary>
        /// Display/edit state for the auto-filter cell. Driven by <see cref="UIElement.IsKeyboardFocusWithin"/>;
        /// true renders the full editor, false renders the read-only display surface.
        /// </summary>
        public static readonly DependencyProperty IsFilterCellEditingProperty = IsFilterCellEditingPropertyKey.DependencyProperty;

        /// <summary>
        /// Backs <see cref="GridColumn"/>. Must be a DP because the template binds against it
        /// with <c>RelativeSource TemplatedParent</c> and needs change notification.
        /// </summary>
        public static readonly DependencyProperty GridColumnProperty =
            DependencyProperty.Register(nameof(GridColumn), typeof(GridColumn), typeof(ColumnFilterControl),
                new PropertyMetadata(null, OnGridColumnChanged));

        /// <summary>
        /// Operator whitelist for the embedded <see cref="SearchTypeSelector"/>. Computed from
        /// the column's <see cref="BaseEditSettings.GetSupportedFilterSearchTypes"/> override —
        /// lets each editor shape scope its operator list independently of the data type.
        /// </summary>
        public static readonly DependencyProperty SupportedSearchTypesProperty =
            DependencyProperty.Register(nameof(SupportedSearchTypes), typeof(System.Collections.Generic.IEnumerable<SearchType>),
                typeof(ColumnFilterControl),
                new PropertyMetadata(null, OnSupportedSearchTypesChanged));

        private static readonly DependencyPropertyKey IsColumnNullablePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsColumnNullable), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(false, OnIsColumnNullableChanged));

        /// <summary>
        /// True when the column's observed data actually contains nulls — gates IsNull/IsNotNull
        /// in the selector. Distinct from CLR-type nullability: tracks real data, not shape.
        /// Resolved lazily once the column value cache loads.
        /// </summary>
        public static readonly DependencyProperty IsColumnNullableProperty = IsColumnNullablePropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ActiveSearchTypeRequiresInputPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ActiveSearchTypeRequiresInput), typeof(bool), typeof(ColumnFilterControl),
                new PropertyMetadata(true, OnActiveSearchTypeRequiresInputChanged));

        /// <summary>
        /// False for NoInput search types (IsNull, Today, AboveAverage, Unique, …). When false,
        /// the editor is disabled and the filter auto-applies on selection.
        /// </summary>
        public static readonly DependencyProperty ActiveSearchTypeRequiresInputProperty = ActiveSearchTypeRequiresInputPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ActiveSearchTypeDisplayNamePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(ActiveSearchTypeDisplayName), typeof(string), typeof(ColumnFilterControl),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Registry display name of the active <see cref="SelectedSearchType"/>. Shown in the
        /// editor area when the editor is disabled by a NoInput search type.
        /// </summary>
        public static readonly DependencyProperty ActiveSearchTypeDisplayNameProperty = ActiveSearchTypeDisplayNamePropertyKey.DependencyProperty;

        #endregion

        #region CLR properties

        public DataGridColumn CurrentColumn
        {
            get => (DataGridColumn)GetValue(CurrentColumnProperty);
            set => SetValue(CurrentColumnProperty, value);
        }

        public SearchDataGrid SourceDataGrid
        {
            get => (SearchDataGrid)GetValue(SourceDataGridProperty);
            set => SetValue(SourceDataGridProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        /// <summary>
        /// Typed value for non-text editors. Mutually exclusive with <see cref="SearchText"/>.
        /// </summary>
        public object SearchValue
        {
            get => GetValue(SearchValueProperty);
            set => SetValue(SearchValueProperty, value);
        }

        public SearchType SelectedSearchType
        {
            get => (SearchType)GetValue(SelectedSearchTypeProperty);
            set => SetValue(SelectedSearchTypeProperty, value);
        }

        public HeaderDisplayState HeaderDisplayState
        {
            get => (HeaderDisplayState)GetValue(HeaderDisplayStateProperty);
            set => SetValue(HeaderDisplayStateProperty, value);
        }

        public bool HasActiveFilter
        {
            get => (bool)GetValue(HasActiveFilterProperty);
            private set => SetValue(HasActiveFilterProperty, value);
        }

        /// <inheritdoc cref="HasEditorInputValueProperty"/>
        public bool HasEditorInputValue
        {
            get => (bool)GetValue(HasEditorInputValueProperty);
            private set => SetValue(HasEditorInputValuePropertyKey, value);
        }

        public bool HasAdvancedFilter
        {
            get => (bool)GetValue(HasAdvancedFilterProperty);
            set => SetValue(HasAdvancedFilterProperty, value);
        }

        public bool IsCheckboxColumn
        {
            get => (bool)GetValue(IsCheckboxColumnProperty);
            private set => SetValue(IsCheckboxColumnProperty, value);
        }

        public bool? FilterCheckboxState
        {
            get => (bool?)GetValue(FilterCheckboxStateProperty);
            set => SetValue(FilterCheckboxStateProperty, value);
        }

        public bool IsComplexFilteringEnabled
        {
            get => (bool)GetValue(IsComplexFilteringEnabledProperty);
            private set => SetValue(IsComplexFilteringEnabledProperty, value);
        }

        /// <summary>Column data type, exposed for the <see cref="SearchTypeSelector"/> binding.</summary>
        public ColumnDataType ColumnDataType
        {
            get => (ColumnDataType)GetValue(ColumnDataTypeProperty);
            private set => SetValue(ColumnDataTypeProperty, value);
        }

        /// <summary>Mirrors <see cref="Wpf.GridColumn.AllowFiltering"/>.</summary>
        public bool IsFilterVisible
        {
            get => (bool)GetValue(IsFilterVisibleProperty);
            set => SetValue(IsFilterVisibleProperty, value);
        }

        /// <inheritdoc cref="IColumnFilterHost.IsFilterEnabled"/>
        public bool IsFilterEnabled
        {
            get => (bool)GetValue(IsFilterEnabledProperty);
            set => SetValue(IsFilterEnabledProperty, value);
        }

        /// <inheritdoc cref="EffectiveShowCriteriaProperty"/>
        public bool EffectiveShowCriteria
        {
            get => (bool)GetValue(EffectiveShowCriteriaProperty);
            private set => SetValue(EffectiveShowCriteriaPropertyKey, value);
        }

        /// <inheritdoc cref="HasFilterRowTemplateProperty"/>
        public bool HasFilterRowTemplate
        {
            get => (bool)GetValue(HasFilterRowTemplateProperty);
            private set => SetValue(HasFilterRowTemplatePropertyKey, value);
        }

        /// <inheritdoc cref="IsFilterCellEditingProperty"/>
        public bool IsFilterCellEditing
        {
            get => (bool)GetValue(IsFilterCellEditingProperty);
            private set => SetValue(IsFilterCellEditingPropertyKey, value);
        }

        /// <inheritdoc cref="SupportedSearchTypesProperty"/>
        public System.Collections.Generic.IEnumerable<SearchType> SupportedSearchTypes
        {
            get => (System.Collections.Generic.IEnumerable<SearchType>)GetValue(SupportedSearchTypesProperty);
            set => SetValue(SupportedSearchTypesProperty, value);
        }

        /// <inheritdoc cref="IsColumnNullableProperty"/>
        public bool IsColumnNullable
        {
            get => (bool)GetValue(IsColumnNullableProperty);
            private set => SetValue(IsColumnNullablePropertyKey, value);
        }

        /// <inheritdoc cref="ActiveSearchTypeRequiresInputProperty"/>
        public bool ActiveSearchTypeRequiresInput
        {
            get => (bool)GetValue(ActiveSearchTypeRequiresInputProperty);
            private set => SetValue(ActiveSearchTypeRequiresInputPropertyKey, value);
        }

        /// <inheritdoc cref="ActiveSearchTypeDisplayNameProperty"/>
        public string ActiveSearchTypeDisplayName
        {
            get => (string)GetValue(ActiveSearchTypeDisplayNameProperty);
            private set => SetValue(ActiveSearchTypeDisplayNamePropertyKey, value);
        }

        public GridColumn GridColumn
        {
            get => (GridColumn)GetValue(GridColumnProperty);
            internal set => SetValue(GridColumnProperty, value);
        }

        public SearchTemplateController SearchTemplateController { get; private set; }

        public string BindingPath { get; private set; }

        /// <inheritdoc/>
        public bool HasTemporaryTemplate => _temporarySearchTemplate != null;

        /// <summary>
        /// Effective live-filtering state — the column's <see cref="ColumnDataBase.EnableLiveFiltering"/>
        /// override when set, otherwise the grid's <see cref="SearchDataGrid.EnableLiveFiltering"/>.
        /// Reads the descriptor's resolver directly (not the mirror DP) so it is never stale, and
        /// falls back to the grid / <c>true</c> when no descriptor is attached yet.
        /// </summary>
        public bool EffectiveIsLiveFilteringEnabled
            => GridColumn?.ResolveEffectiveEnableLiveFiltering()
               ?? SourceDataGrid?.EnableLiveFiltering
               ?? true;

        #endregion

        #region Commands

        private ICommand _clearSearchTextCommand;
        private ICommand _showRuleFilterEditorCommand;

        public ICommand ClearSearchTextCommand
            => _clearSearchTextCommand ??= new RelayCommand(_ => ClearSearchTextAndTemporaryFilter());

        public ICommand ShowRuleFilterEditorCommand => _showRuleFilterEditorCommand ??= new RelayCommand(_ =>
        {
            SourceDataGrid?.CommitEdit(DataGridEditingUnit.Cell, true);
            ShowFilterPopup();
        });

        #endregion

        public ColumnFilterControl()
        {
            Loaded += OnControlLoaded;
            Unloaded += OnControlUnloaded;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Defeat any ancestor-applied IsEnabled=false — filter row is for filtering only,
            // independent of column/grid IsReadOnly. Editor descendants are pinned by ForceFilterEditorEnabled.
            SetCurrentValue(IsEnabledProperty, true);

            // Template can be re-applied (e.g. when IsCheckboxColumn flips); detach stale handlers first.
            DetachSearchTextBox();
            DetachFilterCheckBox();
            DetachSearchTypeSelector();

            _searchTextBox = GetTemplateChild(PartSearchTextBoxName) as TextBox;
            if (_searchTextBox != null)
            {
                _searchTextBox.TextChanged += OnSearchTextBoxTextChanged;
                _searchTextBox.PreviewKeyDown += OnSearchTextBoxPreviewKeyDown;
            }

            _filterCheckBox = GetTemplateChild(PartFilterCheckBoxName) as CheckBox;
            if (_filterCheckBox != null)
            {
                _filterCheckBox.PreviewKeyDown += OnCheckboxPreviewKeyDown;
                _filterCheckBox.PreviewMouseDown += OnCheckboxPreviewMouseDown;
            }

            _searchTypeSelector = GetTemplateChild(PartSearchTypeSelectorName) as SearchTypeSelector;
            // Observe IsDropDownOpen and ItemChosen so the selector's popup acts as a focus
            // surface: opening promotes the cell to editing state; picking an item routes focus
            // into the materialized editor.
            if (_searchTypeSelector != null)
            {
                _searchTypeSelectorDropDownDescriptor = DependencyPropertyDescriptor.FromProperty(
                    SearchTypeSelector.IsDropDownOpenProperty, typeof(SearchTypeSelector));
                _searchTypeSelectorDropDownDescriptor?.AddValueChanged(_searchTypeSelector, OnSearchTypeSelectorDropDownChanged);
                _searchTypeSelector.ItemChosen += OnSearchTypeSelectorItemChosen;
            }

            _editorHost = GetTemplateChild(PartEditorHostName) as ContentPresenter;
            if (_editorHost != null)
            {
                _editorHost.PreviewMouseLeftButtonDown -= OnEditorHostPreviewMouseLeftButtonDown;
                _editorHost.PreviewMouseLeftButtonDown += OnEditorHostPreviewMouseLeftButtonDown;
            }
            RefreshEditor();
            RefreshFilterRowCellStyle();
        }

        private static void OnGridColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
                ctl.RefreshEditor();
        }

        /// <summary>
        /// Materializes the per-column editor inside <see cref="_editorHost"/>. Uses the column's
        /// FilterRow template if supplied, otherwise the descriptor's display/edit pair.
        /// </summary>
        private void RefreshEditor()
        {
            if (_editorHost == null) return;

            DataTemplate template = GridColumn?.FilterRowEditTemplate ?? GridColumn?.FilterRowDisplayTemplate;
            if (template != null)
            {
                // Template author binds {Binding Value} to drive the filter; un-bound templates
                // simply won't filter. Templates own their own display/edit split via IsFilterCellEditing.
                DetachFilterEditor();
                EnsureFilterCellData();
                _editorHost.Content = _filterCellData;
                _editorHost.ContentTemplate = template;
            }
            else
            {
                DetachFilterCellData();
                var settings = GridColumn?.EditSettings ?? new TextEditSettings();

                DetachFilterEditor();
                UIElement editor = IsFilterCellEditing
                    ? settings.CreateFilterEditor(this)
                    : settings.CreateFilterDisplay(this);
                _editorHost.ContentTemplate = null;
                _editorHost.Content = editor;
                _filterEditor = editor;
                if (_filterEditor != null)
                    _filterEditor.PreviewKeyDown += OnFilterEditorPreviewKeyDown;
            }

            ForceFilterEditorEnabled();

            HasFilterRowTemplate = template != null;

            // Selector is hosted outside PART_EditorHost, so it applies to both editor branches.
            RefreshSupportedSearchTypes();

            // Fresh-bind path: per-DP callbacks cover in-place flips, but the descriptor itself
            // only re-binds here.
            RefreshEffectiveShowCriteria();
            RefreshFilterRowCellStyle();
            RefreshFocusNav();
            UpdateEffectiveIsCellEnabled();
        }

        /// <summary>Re-runs <see cref="RefreshEditor"/> when the column's template DPs change.</summary>
        internal void RefreshTemplate() => RefreshEditor();

        /// <summary>
        /// Drives the display ↔ edit surface swap from focus transitions, mirroring
        /// <see cref="System.Windows.Controls.DataGridCell"/>'s display/edit promotion.
        /// </summary>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            UpdateFilterCellEditingFromInputs(allowEditorAutoFocus: true);
        }

        /// <summary>
        /// True while a popup owned by this cell (search-type dropdown or rule-filter editor)
        /// is open. <see cref="FilterRowNavigator"/> uses this to short-circuit cell navigation.
        /// </summary>
        internal bool HasOpenSubPopup
            => (_searchTypeSelector?.IsDropDownOpen ?? false)
            || (_filterPopup?.IsOpen ?? false);

        /// <summary>
        /// Routes Left/Right arrows to <see cref="FilterRowNavigator"/> only when focus is on
        /// the host itself — in-cell editors keep arrows for caret movement.
        /// </summary>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Handled) return;
            if (e.Key != Key.Left && e.Key != Key.Right) return;
            if (!ReferenceEquals(e.OriginalSource, this)) return;
            FilterRowNavigator.TryNavigate(this, e);
        }

        /// <summary>
        /// Tab / Shift+Tab handler. Runs at bubble so the editor's commit-on-Tab tunneling
        /// path fires first; pre-empts WPF's default Tab traversal, which walks columns in
        /// insertion order rather than display order.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;
            if (e.Key != Key.Tab) return;
            FilterRowNavigator.TryNavigate(this, e);
        }

        /// <summary>
        /// Drives <see cref="IsFilterCellEditing"/> from keyboard focus OR the search-type
        /// popup being open — the toggle glyph isn't focusable, so clicks on it don't move focus.
        /// </summary>
        /// <param name="allowEditorAutoFocus">
        /// False from the popup-open path — focusing the editor while the popup is open would
        /// dismiss the popup before the user picks.
        /// </param>
        private void UpdateFilterCellEditingFromInputs(bool allowEditorAutoFocus)
        {
            if (_isSwappingSurfaces) return;
            bool popupOpen = _searchTypeSelector?.IsDropDownOpen ?? false;
            bool wantsEditing = IsKeyboardFocusWithin || popupOpen;
            if (wantsEditing == IsFilterCellEditing) return;
            ApplyFilterCellEditingTransition(wantsEditing, autoFocusEditor: allowEditorAutoFocus && !popupOpen);
        }

        private void ApplyFilterCellEditingTransition(bool wantsEditing, bool autoFocusEditor = true)
        {
            _isSwappingSurfaces = true;
            try
            {
                IsFilterCellEditing = wantsEditing;
                RefreshEditor();

                if (wantsEditing && autoFocusEditor)
                {
                    // Checkbox columns collapse PART_EditorHost — the visible focus target is
                    // PART_FilterCheckBox; everything else focuses the materialized editor.
                    UIElement focusTarget = IsCheckboxColumn && _filterCheckBox != null
                        ? (UIElement)_filterCheckBox
                        : _filterEditor;
                    if (focusTarget != null)
                    {
                        // Defer so the editor finishes its Loaded pass — synchronous Focus
                        // can race the ContentPresenter's content swap.
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (!focusTarget.IsKeyboardFocusWithin)
                                Keyboard.Focus(focusTarget);
                        }), DispatcherPriority.Input);
                    }
                }
            }
            finally
            {
                // Clear the guard at Background priority so focus-loss events from the teardown
                // are absorbed before we'd react to them.
                Dispatcher.BeginInvoke(new Action(() => _isSwappingSurfaces = false),
                    DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Promotes the cell when the selector opens; defers the demote on close so an
        /// item-chosen focus transfer onto the editor wins before we sample focus state,
        /// avoiding a flicker.
        /// </summary>
        private void OnSearchTypeSelectorDropDownChanged(object sender, EventArgs e)
        {
            bool popupOpen = _searchTypeSelector?.IsDropDownOpen ?? false;
            if (popupOpen)
            {
                // Force the value cache to load before the ListBox materializes — otherwise
                // IsNull/IsNotNull would never appear on first open. Idempotent thereafter.
                SearchTemplateController?.EnsureNullStatusDetermined();
                RefreshIsColumnNullable();
                UpdateFilterCellEditingFromInputs(allowEditorAutoFocus: false);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateFilterCellEditingFromInputs(allowEditorAutoFocus: false);
                }), DispatcherPriority.Input);
            }
        }

        /// <summary>
        /// Closes the selector popup and routes focus into the materialized editor. Deferred
        /// to dodge a race against WPF's popup-teardown focus pipeline.
        /// </summary>
        private void OnSearchTypeSelectorItemChosen(object sender, EventArgs e)
        {
            if (_searchTypeSelector == null) return;
            _searchTypeSelector.IsDropDownOpen = false;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_filterEditor is UIElement editor && !editor.IsKeyboardFocusWithin)
                    Keyboard.Focus(editor);
            }), DispatcherPriority.Input);
        }

        /// <summary>
        /// Triggers the display → edit swap on a click against the (non-focusable by default)
        /// display surface — the keyboard path is covered by <see cref="OnIsKeyboardFocusWithinChanged"/>.
        /// </summary>
        private void OnEditorHostPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsFilterCellEditing) return;
            if (HasFilterRowTemplate) return;
            Focus();
        }

        private void DetachFilterEditor()
        {
            if (_filterEditor == null) return;
            _filterEditor.PreviewKeyDown -= OnFilterEditorPreviewKeyDown;
            _filterEditor = null;
        }

        /// <summary>
        /// Enables the editor host based on whether the active search type accepts input.
        /// Force-enables to override grid/column IsReadOnly, which is for data rows only.
        /// </summary>
        private void ForceFilterEditorEnabled()
        {
            if (_editorHost == null) return;
            bool wantEnabled = ActiveSearchTypeRequiresInput;
            _editorHost.IsEnabled = wantEnabled;
            if (_editorHost.Content is UIElement direct)
                direct.IsEnabled = wantEnabled;

            // Re-hook unconditionally — RefreshEditor runs multiple times per host (descriptor
            // swap, template flip); a stale handler would double-fire. Loaded covers the
            // user-template branch where the subtree only exists post-inflation.
            _editorHost.Loaded -= OnEditorHostLoadedForceEnable;
            _editorHost.Loaded += OnEditorHostLoadedForceEnable;
        }

        private void OnEditorHostLoadedForceEnable(object sender, RoutedEventArgs e)
        {
            if (sender is not ContentPresenter cp) return;
            ApplyDescendantsEnabled(cp, ActiveSearchTypeRequiresInput);
        }

        private static void ApplyDescendantsEnabled(DependencyObject root, bool enabled)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is UIElement uie)
                    uie.IsEnabled = enabled;
                ApplyDescendantsEnabled(child, enabled);
            }
        }

        /// <summary>
        /// Lazily constructs <see cref="_filterCellData"/> with <c>Value</c> two-way-bound to
        /// <see cref="SearchValue"/> so any template editor shape can drive the filter.
        /// </summary>
        private void EnsureFilterCellData()
        {
            if (_filterCellData != null)
            {
                _filterCellData.Column = GridColumn;
                _filterCellData.View = SourceDataGrid;
                return;
            }
            _filterCellData = new EditGridCellData
            {
                Column = GridColumn,
                View = SourceDataGrid,
            };
            BindingOperations.SetBinding(
                _filterCellData,
                EditableDataObject.ValueProperty,
                new Binding(nameof(SearchValue))
                {
                    Source = this,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                });
        }

        private void DetachFilterCellData()
        {
            if (_filterCellData == null) return;
            BindingOperations.ClearBinding(_filterCellData, EditableDataObject.ValueProperty);
            _filterCellData = null;
        }

        private void DetachSearchTextBox()
        {
            if (_searchTextBox == null) return;
            _searchTextBox.TextChanged -= OnSearchTextBoxTextChanged;
            _searchTextBox.PreviewKeyDown -= OnSearchTextBoxPreviewKeyDown;
            _searchTextBox = null;
        }

        private void DetachFilterCheckBox()
        {
            if (_filterCheckBox == null) return;
            _filterCheckBox.PreviewKeyDown -= OnCheckboxPreviewKeyDown;
            _filterCheckBox.PreviewMouseDown -= OnCheckboxPreviewMouseDown;
            _filterCheckBox = null;
        }

        private void DetachSearchTypeSelector()
        {
            if (_searchTypeSelector != null)
            {
                _searchTypeSelectorDropDownDescriptor?.RemoveValueChanged(_searchTypeSelector, OnSearchTypeSelectorDropDownChanged);
                _searchTypeSelector.ItemChosen -= OnSearchTypeSelectorItemChosen;
            }
            _searchTypeSelectorDropDownDescriptor = null;
            _searchTypeSelector = null;
        }

        #region Lifecycle

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            InitializeSearchTemplateController();
            UpdateIsComplexFilteringEnabled();
            RegisterWithSourceDataGrid();
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (_changeTimer != null)
            {
                _changeTimer.Stop();
                _changeTimer.Tick -= OnChangeTimerTick;
                _changeTimer = null;
            }

            _temporarySearchTemplate = null;

            DetachSearchTextBox();
            DetachFilterCheckBox();
            DetachFilterEditor();
            DetachFilterCellData();
            DetachFromController();

            if (SourceDataGrid != null)
            {
                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                SourceDataGrid.ItemsSourceChanged -= OnSourceDataGridItemsSourceChanged;
                SourceDataGrid.RegisterColumnFilterControl(CurrentColumn, null);
                SourceDataGrid.DataColumns?.Remove(this);
            }

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

        private void RegisterWithSourceDataGrid()
        {
            if (SourceDataGrid == null || CurrentColumn == null) return;
            SourceDataGrid.RegisterColumnFilterControl(CurrentColumn, this);

            // Join DataColumns so the filter pipeline sees this control. Replace stale entries
            // for the same column rather than accumulating duplicates on recycled instances.
            var columns = SourceDataGrid.DataColumns;
            if (columns == null) return;
            for (int i = columns.Count - 1; i >= 0; i--)
            {
                var existing = columns[i];
                if (existing != null && !ReferenceEquals(existing, this) && existing.CurrentColumn == CurrentColumn)
                    columns.RemoveAt(i);
            }
            if (!columns.Contains(this))
                columns.Add(this);
        }

        #endregion

        #region DP change callbacks

        private static void OnCurrentColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnFilterControl ctl) return;

            // Re-resolve descriptor — a presenter may retarget a control at a different column
            // when columns are added/removed/reset.
            if (e.OldValue is DataGridColumn oldColumn && ctl.SourceDataGrid != null)
                ctl.SourceDataGrid.RegisterColumnFilterControl(oldColumn, null);

            ctl.InitializeSearchTemplateController();
            ctl.UpdateIsComplexFilteringEnabled();
            ctl.RegisterWithSourceDataGrid();
        }

        private static void OnSourceDataGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnFilterControl ctl) return;

            if (e.OldValue is SearchDataGrid oldGrid)
            {
                oldGrid.CollectionChanged -= ctl.OnSourceDataGridCollectionChanged;
                oldGrid.ItemsSourceChanged -= ctl.OnSourceDataGridItemsSourceChanged;
                if (ctl.CurrentColumn != null)
                    oldGrid.RegisterColumnFilterControl(ctl.CurrentColumn, null);
            }

            if (ctl.SourceDataGrid != null)
                ctl.SourceDataGrid.ItemsSourceChanged += ctl.OnSourceDataGridItemsSourceChanged;

            ctl.InitializeSearchTemplateController();
            ctl.UpdateIsComplexFilteringEnabled();
            ctl.RegisterWithSourceDataGrid();
        }

        private static void OnFilterCheckboxStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
                ctl.OnCheckboxFilterChanged();
        }

        private static void OnSelectedSearchTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnFilterControl ctl) return;

            // Refresh unconditionally — gates editor enable state and the auto-apply branch.
            ctl.UpdateActiveSearchTypeRequiresInput();

            if (ctl._suppressSearchTextSync)
            {
                // Even suppressed, the criteria-excluded rule depends on the resolved selection.
                ctl.UpdateEffectiveIsCellEnabled();
                return;
            }

            bool newIsNoInput = !ctl.ActiveSearchTypeRequiresInput;
            if (newIsNoInput)
            {
                // Clear in-flight input so the disabled editor doesn't ghost stale text,
                // then create-or-retarget the temp template so the filter applies immediately.
                ctl.ClearEditorInputsForNoInputTransition();
                ctl.CreateOrRetargetNoInputTemporaryTemplate();
                ctl.SearchTemplateController?.UpdateFilterExpression();
                if (ctl.EffectiveIsLiveFilteringEnabled)
                    ctl.SourceDataGrid?.FilterItemsSource();
                ctl.SourceDataGrid?.UpdateFilterSummaryPanel();
                ctl.HasAdvancedFilter = ctl.SearchTemplateController?.HasCustomExpression ?? false;
                ctl.UpdateHasActiveFilterState();
            }
            else if (ctl._temporarySearchTemplate != null)
            {
                // If the temp template has no user value, drop it; otherwise retarget the
                // value onto the new operator.
                if (!ctl.HasAnyInputValue())
                {
                    ctl.ClearTemporaryTemplate();
                }
                else
                {
                    ctl._temporarySearchTemplate.SearchType = ctl.SelectedSearchType;
                    ctl.SearchTemplateController?.UpdateFilterExpression();
                    if (ctl.EffectiveIsLiveFilteringEnabled)
                        ctl.SourceDataGrid?.FilterItemsSource();
                    ctl.SourceDataGrid?.UpdateFilterSummaryPanel();
                    ctl.UpdateHasActiveFilterState();
                }
            }

            ctl.UpdateEffectiveIsCellEnabled();
        }

        /// <summary>
        /// Clears <see cref="SearchText"/>/<see cref="SearchValue"/> before a NoInput transition
        /// so the disabled editor doesn't show stale content.
        /// </summary>
        private void ClearEditorInputsForNoInputTransition()
        {
            _suppressSearchTextSync = true;
            try
            {
                SearchText = string.Empty;
                SearchValue = null;
            }
            finally { _suppressSearchTextSync = false; }
            if (_searchTextBox != null)
                _searchTextBox.Text = string.Empty;
        }

        /// <summary>
        /// Creates or retargets the temporary template for a NoInput search type. Skips the
        /// input-value plumbing — <c>HasCustomFilter</c> is true for NoInput types regardless.
        /// </summary>
        private void CreateOrRetargetNoInputTemporaryTemplate()
        {
            try
            {
                if (SearchTemplateController == null) return;

                if (SearchTemplateController.SearchGroups.Count == 0)
                    SearchTemplateController.AddSearchGroup();

                var firstGroup = SearchTemplateController.SearchGroups[0];
                RemoveDefaultEmptyTemplates(firstGroup);

                var searchType = SelectedSearchType;

                if (_temporarySearchTemplate != null)
                {
                    _temporarySearchTemplate.SearchType = searchType;
                    _temporarySearchTemplate.SelectedValue = null;
                }
                else
                {
                    _temporarySearchTemplate = new SearchTemplate(SearchTemplateController.ColumnDataType)
                    {
                        SearchType = searchType,
                        SelectedValue = null,
                        SearchTemplateController = SearchTemplateController,
                    };
                    SearchTemplateController.SubscribeToTemplateChanges(_temporarySearchTemplate);

                    var existingOfSameType = firstGroup.SearchTemplates
                        .Where(t => t.SearchType == searchType && t.HasCustomFilter)
                        .ToList();
                    if (existingOfSameType.Any())
                        _temporarySearchTemplate.OperatorName = "Or";

                    firstGroup.SearchTemplates.Add(_temporarySearchTemplate);
                }

                SearchTemplateController.UpdateOperatorVisibility();
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateOrRetargetNoInputTemporaryTemplate: {ex.Message}");
            }
        }

        private static void OnHeaderDisplayStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
            {
                VisualStateManager.GoToState(ctl, ctl.HeaderDisplayState.ToString(), true);
            }
        }

        /// <summary>
        /// Lazily constructs the keystroke debounce timer on the UI dispatcher at Background
        /// priority so filter rebuilds can't preempt user input.
        /// </summary>
        internal void EnsureChangeTimer()
        {
            if (_changeTimer != null) return;
            _changeTimer = new DispatcherTimer(DispatcherPriority.Background);
            _changeTimer.Tick += OnChangeTimerTick;
        }

        /// <summary>
        /// Debounced filter-apply tick. Skipped while the rule-filter popup is open — the
        /// popup owns the controller during edit.
        /// </summary>
        private void OnChangeTimerTick(object sender, EventArgs e)
        {
            _changeTimer?.Stop();
            if (_filterPopup?.IsOpen == true) return;
            if (HasAnyInputValue())
                UpdateSimpleFilter();
        }

        /// <summary>Routes <see cref="IsFilterEnabled"/> changes through <see cref="UpdateEffectiveIsCellEnabled"/>.</summary>
        private static void OnIsFilterEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
                ctl.UpdateEffectiveIsCellEnabled();
        }

        /// <summary>Routes <see cref="SupportedSearchTypes"/> changes through <see cref="UpdateEffectiveIsCellEnabled"/>.</summary>
        private static void OnSupportedSearchTypesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
                ctl.UpdateEffectiveIsCellEnabled();
        }

        /// <summary>
        /// Recomputes <see cref="SupportedSearchTypes"/> when nullability flips — the selector's
        /// own IsNullable DP is bypassed when the whitelist is non-null.
        /// </summary>
        private static void OnIsColumnNullableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
                ctl.RefreshSupportedSearchTypes();
        }

        /// <summary>Re-applies editor enable state when the active type flips between Input/NoInput.</summary>
        private static void OnActiveSearchTypeRequiresInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnFilterControl ctl)
                ctl.ForceFilterEditorEnabled();
        }

        #endregion

        #region Effective-value resolution

        /// <summary>Recomputes <see cref="EffectiveShowCriteria"/> — column override beats grid setting.</summary>
        internal void RefreshEffectiveShowCriteria()
        {
            bool? columnOverride = GridColumn?.ShowCriteriaInFilterRow;
            bool gridDefault = SourceDataGrid?.ShowCriteriaInFilterRow ?? false;
            EffectiveShowCriteria = columnOverride ?? gridDefault;
        }

        /// <summary>Resolves the cell Style — column override beats grid setting, falls back to the keyed theme style.</summary>
        internal void RefreshFilterRowCellStyle()
        {
            Style resolved = GridColumn?.FilterRowCellStyle ?? SourceDataGrid?.FilterRowCellStyle;
            if (resolved != null)
            {
                Style = resolved;
            }
            else
            {
                ClearValue(StyleProperty);
            }
        }

        /// <summary>
        /// Pushes the descriptor's <see cref="ColumnDataBase.ActualAllowFocus"/> and
        /// <see cref="ColumnDataBase.ActualTabStop"/> onto this filter cell. Called from
        /// <see cref="RefreshEditor"/> when the descriptor binds and from the descriptor's
        /// focus/nav DP change callback.
        /// </summary>
        internal void RefreshFocusNav()
        {
            bool allowFocus = GridColumn?.ActualAllowFocus ?? true;
            bool tabStop = GridColumn?.ActualTabStop ?? true;
            Focusable = allowFocus;
            KeyboardNavigation.SetIsTabStop(this, allowFocus && tabStop);
        }

        /// <summary>
        /// Disables the cell when the column's default criteria is excluded from
        /// <see cref="SupportedSearchTypes"/> and the user hasn't picked an allowed type.
        /// Also combines with <see cref="IsFilterEnabled"/>.
        /// </summary>
        internal void UpdateEffectiveIsCellEnabled()
        {
            bool allowAutoFilter = IsFilterEnabled;
            if (!allowAutoFilter)
            {
                IsEnabled = false;
                return;
            }

            var allowed = SupportedSearchTypes;
            if (allowed == null)
            {
                IsEnabled = true;
                return;
            }

            var defaultType = MapDefaultSearchTypeToSearchType(ResolveDefaultSearchType());
            bool defaultAllowed = allowed.Contains(defaultType);

            // A user-picked allowed type keeps the cell enabled even when the default is excluded.
            bool selectionIsAllowed = allowed.Contains(SelectedSearchType);

            IsEnabled = defaultAllowed || selectionIsAllowed;
        }

        /// <summary>True for search types tagged <see cref="FilterInputTemplate.NoInput"/> in the registry.</summary>
        internal static bool IsNoInputSearchType(SearchType type)
            => SearchTypeRegistry.GetMetadata(type)?.InputTemplate == FilterInputTemplate.NoInput;

        /// <summary>
        /// Re-pushes the operator whitelist using the current <see cref="IsColumnNullable"/>
        /// so IsNull/IsNotNull surface based on real data, not CLR-type shape.
        /// </summary>
        private void RefreshSupportedSearchTypes()
        {
            var supportSource = GridColumn?.EditSettings ?? new TextEditSettings();
            SupportedSearchTypes = supportSource.GetSupportedFilterSearchTypes(ColumnDataType, IsColumnNullable);
        }

        /// <summary>
        /// Recomputes <see cref="ActiveSearchTypeRequiresInput"/> and <see cref="ActiveSearchTypeDisplayName"/>
        /// from the registry metadata for <see cref="SelectedSearchType"/>.
        /// </summary>
        private void UpdateActiveSearchTypeRequiresInput()
        {
            var metadata = SearchTypeRegistry.GetMetadata(SelectedSearchType);
            ActiveSearchTypeRequiresInput = metadata?.InputTemplate != FilterInputTemplate.NoInput;
            ActiveSearchTypeDisplayName = metadata?.DisplayName ?? string.Empty;
        }

        /// <summary>
        /// Reads cached <see cref="SearchTemplateController.ContainsNullValues"/>. Does not
        /// force a cache load — callers needing an authoritative answer call
        /// <see cref="SearchTemplateController.EnsureNullStatusDetermined"/> first.
        /// </summary>
        private void RefreshIsColumnNullable()
        {
            IsColumnNullable = SearchTemplateController?.ContainsNullValues ?? false;
        }

        /// <summary>
        /// Resets <see cref="SelectedSearchType"/> to the column default after a NoInput filter
        /// commits or clears so the cell doesn't stay stuck on the NoInput operator.
        /// </summary>
        private void ResetSelectedSearchTypeToDefault()
        {
            _suppressSearchTextSync = true;
            try
            {
                SelectedSearchType = MapDefaultSearchTypeToSearchType(ResolveDefaultSearchType());
            }
            finally { _suppressSearchTextSync = false; }
            UpdateActiveSearchTypeRequiresInput();
            UpdateEffectiveIsCellEnabled();
        }

        #endregion

        #region Source-data-grid hooks

        private void OnSourceDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(BindingPath) || SearchTemplateController == null)
                return;

            try
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SearchTemplateController.RefreshColumnValues();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSourceDataGridCollectionChanged: {ex.Message}");
                SearchTemplateController?.RefreshColumnValues();
            }
        }

        private void OnSourceDataGridItemsSourceChanged(object sender, EventArgs e)
        {
            try
            {
                if (SearchTemplateController == null)
                {
                    InitializeSearchTemplateController();
                    return;
                }

                if (SourceDataGrid != null && !string.IsNullOrEmpty(BindingPath))
                {
                    SearchTemplateController.SetupColumnDataLazy(ResolveColumnDisplayName(), GetColumnValuesFromDataGrid, BindingPath);
                    if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                        SearchTemplateController.RefreshColumnValues();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSourceDataGridItemsSourceChanged: {ex.Message}");
            }
        }

        #endregion

        #region Descriptor resolution

        public string ResolveColumnDisplayName()
        {
            if (GridColumn == null) return null;
            return !string.IsNullOrEmpty(GridColumn.ColumnDisplayName)
                ? GridColumn.ColumnDisplayName
                : GridColumn.HeaderCaption;
        }

        internal string ResolveFilterMemberPath()
            => GridColumn?.FilterMemberPath ?? GridColumn?.FieldName;

        internal DefaultSearchType ResolveDefaultSearchType()
            => GridColumn?.DefaultSearchType ?? DefaultSearchType.StartsWith;

        internal bool ResolveUseCheckBoxInSearchBox()
            => GridColumn?.UseCheckBoxInSearchBox ?? false;

        internal string ResolveDisplayMask()
            => GridColumn?.DisplayMask;

        internal bool ResolveAllowFilterPopup()
        {
            if (GridColumn != null)
            {
                var localValue = GridColumn.ReadLocalValue(GridColumn.AllowFilterPopupProperty);
                if (localValue != DependencyProperty.UnsetValue)
                    return (bool)localValue;
            }
            return SourceDataGrid?.AllowFilterPopup ?? true;
        }

        #endregion

        #region Initialization

        private void InitializeSearchTemplateController()
        {
            try
            {
                if (SourceDataGrid == null || CurrentColumn == null)
                    return;

                GridColumn = SourceDataGrid.FindGridColumnDescriptor(CurrentColumn);

                if (GridColumn != null && !GridColumn.AllowFiltering)
                {
                    Visibility = Visibility.Collapsed;
                    return;
                }

                // AllowAutoFilter greys the cell while preserving its space — distinct from
                // AllowFiltering, which collapses it.
                IsFilterEnabled = GridColumn?.AllowAutoFilter ?? true;

                // Shared bootstrap: creates the controller and populates ColumnName, values
                // provider, ColumnDataType, DisplayValueProvider, DisplayMaskPattern, and
                // RoundDateTime in one place. The FilterString DP uses the same helper so the
                // two paths can't drift.
                if (GridColumn != null)
                {
                    SearchTemplateController = SourceDataGrid.EnsureControllerBootstrapped(GridColumn);
                }
                else if (SearchTemplateController == null)
                {
                    SearchTemplateController = new SearchTemplateController();
                }

                SearchTemplateController.ColumnName = ResolveColumnDisplayName();

                string resolvedPath = ResolveFilterMemberPath();
                if (string.IsNullOrEmpty(resolvedPath))
                    resolvedPath = CurrentColumn.SortMemberPath;
                if (string.IsNullOrEmpty(resolvedPath) && CurrentColumn is DataGridBoundColumn boundColumn)
                    resolvedPath = (boundColumn.Binding as Binding)?.Path?.Path;
                BindingPath = resolvedPath;

                DetermineCheckboxColumnTypeFromColumnDefinition();

                SourceDataGrid.CollectionChanged -= OnSourceDataGridCollectionChanged;
                SourceDataGrid.CollectionChanged += OnSourceDataGridCollectionChanged;
                SourceDataGrid.ItemsSourceChanged -= OnSourceDataGridItemsSourceChanged;
                SourceDataGrid.ItemsSourceChanged += OnSourceDataGridItemsSourceChanged;

                // The descriptor path already configured the controller via the shared helper;
                // the standalone (no-descriptor) path still needs its own setup.
                if (GridColumn == null)
                {
                    SearchTemplateController.SetupColumnDataLazy(ResolveColumnDisplayName(), GetColumnValuesFromDataGrid, BindingPath);
                    SearchTemplateController.DisplayValueProvider = DisplayValueProviderFactory.Create(GridColumn);
                    SearchTemplateController.DisplayMaskPattern = ResolveDisplayMask();
                }

                if (SourceDataGrid.Items != null && SourceDataGrid.Items.Count > 0)
                {
                    var sampleSize = Math.Min(10, SourceDataGrid.Items.Count);
                    if (sampleSize > 0)
                    {
                        var sampleValues = new System.Collections.Generic.HashSet<object>();
                        foreach (var item in SourceDataGrid.Items.Cast<object>().Take(sampleSize))
                        {
                            var value = ReflectionHelper.GetPropValue(item, BindingPath);
                            sampleValues.Add(value);
                            if (sampleValues.Count >= 5) break;
                        }
                        if (sampleValues.Any())
                            SearchTemplateController.ColumnDataType = ReflectionHelper.DetermineColumnDataType(sampleValues);
                    }
                }

                ColumnDataType = SearchTemplateController.ColumnDataType;

                // Date-only columns need their time component dropped at comparison so a
                // DatePicker selection matches values stored at midnight.
                if (GridColumn != null && ColumnDataType == ColumnDataType.DateTime)
                    SearchTemplateController.RoundDateTime = GridColumn.ResolveEffectiveRoundDateTime();

                // Seed SelectedSearchType from the column's default mode.
                _suppressSearchTextSync = true;
                try
                {
                    SelectedSearchType = MapDefaultSearchTypeToSearchType(ResolveDefaultSearchType());
                }
                finally
                {
                    _suppressSearchTextSync = false;
                }

                HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                UpdateHasActiveFilterState();
                AttachToController(SearchTemplateController);
                RefreshIsColumnNullable();
                UpdateActiveSearchTypeRequiresInput();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeSearchTemplateController: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribes to <paramref name="controller"/>, detaching from the prior one. Tracking
        /// via <see cref="_subscribedController"/> prevents double-subscribe or leak on recycle.
        /// </summary>
        private void AttachToController(SearchTemplateController controller)
        {
            if (ReferenceEquals(_subscribedController, controller)) return;
            DetachFromController();
            if (controller == null) return;
            controller.PropertyChanged += OnControllerPropertyChanged;
            controller.ColumnValuesChanged += OnControllerColumnValuesChanged;
            _subscribedController = controller;
        }

        private void DetachFromController()
        {
            if (_subscribedController == null) return;
            _subscribedController.PropertyChanged -= OnControllerPropertyChanged;
            _subscribedController.ColumnValuesChanged -= OnControllerColumnValuesChanged;
            _subscribedController = null;
        }

        private void OnControllerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchTemplateController.ContainsNullValues))
                RefreshIsColumnNullable();
        }

        private void OnControllerColumnValuesChanged(object sender, EventArgs e)
        {
            // Cache rebuilt — re-read ContainsNullValues so a flip propagates to the DP.
            RefreshIsColumnNullable();
        }

        private System.Collections.Generic.IEnumerable<object> GetColumnValuesFromDataGrid()
        {
            var dataSource = SourceDataGrid?.OriginalItemsSource ?? SourceDataGrid?.Items;
            if (dataSource == null || string.IsNullOrEmpty(BindingPath))
                return Enumerable.Empty<object>();

            var values = new System.Collections.Generic.List<object>();
            foreach (var item in dataSource)
                values.Add(ReflectionHelper.GetPropValue(item, BindingPath));
            return values;
        }

        public void UpdateIsComplexFilteringEnabled()
            => IsComplexFilteringEnabled = ResolveAllowFilterPopup();

        /// <summary>
        /// Recomputes <see cref="HasActiveFilter"/> (any filter applied) and the narrower
        /// <see cref="HasEditorInputValue"/> (clear-button-actionable input only).
        /// </summary>
        public void UpdateHasActiveFilterState()
        {
            bool hasFilter = false;

            if (SearchTemplateController != null)
            {
                if (IsCheckboxColumn)
                {
                    if (FilterCheckboxState.HasValue)
                    {
                        hasFilter = true;
                    }
                    else if (SearchTemplateController.HasCustomExpression)
                    {
                        var firstGroup = SearchTemplateController.SearchGroups.FirstOrDefault();
                        var firstTemplate = firstGroup?.SearchTemplates.FirstOrDefault();
                        if (firstTemplate?.SearchType == SearchType.IsNull)
                            hasFilter = true;
                    }
                }

                if (!hasFilter)
                {
                    hasFilter = SearchTemplateController.HasCustomExpression;
                    if (!hasFilter && _temporarySearchTemplate != null)
                        hasFilter = true;
                }
            }

            HasActiveFilter = hasFilter;

            // HasEditorInputValue: scoped to what the X button can actually clear. Permanent
            // rule filters light up HasActiveFilter but not this — the X would no-op.
            bool hasEditorInput;
            if (IsCheckboxColumn)
            {
                // _isInitialState distinguishes the untouched-Intermediate state from the
                // IsNull-Intermediate state on nullable columns (both have null FilterCheckboxState).
                hasEditorInput = !_isInitialState;
            }
            else
            {
                hasEditorInput = HasAnyInputValue() || _temporarySearchTemplate != null;
            }

            HasEditorInputValue = hasEditorInput;

            PushAutoFilterStateToDescriptor();
        }

        /// <summary>
        /// Mirrors the auto-filter cell's current value, operator, and aggregate state onto the
        /// persistent column descriptor's read-only DPs (<see cref="ColumnDataBase.AutoFilterValue"/>,
        /// <see cref="ColumnDataBase.AutoFilterCondition"/>, <see cref="ColumnDataBase.AutoFilterHeaderState"/>)
        /// so consumers can bind to filter-row state without holding a reference to this ephemeral,
        /// virtualization-recycled control. Called from <see cref="UpdateHasActiveFilterState"/>,
        /// which runs on every meaningful filter change (init, type, search-type swap, clear).
        /// </summary>
        private void PushAutoFilterStateToDescriptor()
        {
            var descriptor = GridColumn;
            if (descriptor == null)
                return;

            descriptor.SetAutoFilterCondition(SelectedSearchType);

            object currentValue = IsCheckboxColumn
                ? FilterCheckboxState
                : SearchValue ?? (string.IsNullOrEmpty(SearchText) ? null : SearchText);
            descriptor.SetAutoFilterValue(currentValue);

            descriptor.SetAutoFilterHeaderState(ResolveAutoFilterHeaderState());
        }

        /// <summary>
        /// Collapses the cell's filter flags into the aggregate
        /// <see cref="AutoFilterHeaderState"/>, with precedence Hidden &gt; Disabled &gt; Active
        /// &gt; PendingInput &gt; Empty.
        /// </summary>
        private AutoFilterHeaderState ResolveAutoFilterHeaderState()
        {
            if (!IsFilterVisible) return AutoFilterHeaderState.Hidden;
            if (!IsFilterEnabled) return AutoFilterHeaderState.Disabled;
            if (HasActiveFilter) return AutoFilterHeaderState.Active;
            if (HasEditorInputValue) return AutoFilterHeaderState.PendingInput;
            return AutoFilterHeaderState.Empty;
        }

        public void ClearFilter()
        {
            try
            {
                _changeTimer?.Stop();
                ClearLastCommittedSnapshot();

                bool wasNoInput = !ActiveSearchTypeRequiresInput;

                _suppressSearchTextSync = true;
                try { SearchText = string.Empty; }
                finally { _suppressSearchTextSync = false; }

                if (_searchTextBox != null)
                    _searchTextBox.Text = string.Empty;

                if (IsCheckboxColumn)
                    ResetCheckboxToInitialState();

                ClearFilterInternal();

                if (wasNoInput && !IsCheckboxColumn)
                    ResetSelectedSearchTypeToDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearFilter: {ex.Message}");
            }
        }

        private void ClearFilterInternal()
        {
            try
            {
                if (SearchTemplateController == null) return;

                _temporarySearchTemplate = null;
                SearchTemplateController.ClearAndReset();
                HasAdvancedFilter = false;
                SourceDataGrid?.FilterItemsSource();
                UpdateHasActiveFilterState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearFilterInternal: {ex.Message}");
            }
        }

        #endregion
    }
}
