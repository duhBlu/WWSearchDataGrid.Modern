using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WWControls.Core.Display;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Abstract data/filter/sort/editor tier of the column descriptor hierarchy. Layers data
    /// identity, display formatting, filter and sort behavior, and the editor surface onto the
    /// pure layout tier defined by <see cref="ColumnLayoutBase"/>. The concrete
    /// <see cref="GridColumn"/> derives from this tier.
    /// </summary>
    /// <remarks>
    /// The <see cref="FieldName"/> property is the primary key: it drives <c>Binding</c>,
    /// <c>SortMemberPath</c>, and <c>FilterMemberPath</c> unless explicitly overridden.
    /// </remarks>
    public abstract class ColumnDataBase : ColumnLayoutBase
    {
        protected ColumnDataBase()
        {
            // Initialize the custom-popup tabs collection so the XAML implicit-collection syntax
            // (`<sdg:GridColumn.CustomColumnFilterTabs><sdg:ColumnFilterTab .../></...>`) can add
            // entries. WPF's XAML reader calls GetValue directly to find the target list and
            // fails when it's null — the CLR getter's lazy init never runs during parse.
            SetValue(CustomColumnFilterTabsProperty, new FreezableCollection<ColumnFilterTab>());

            // Forward DataContext changes to EditSettings so its bindings stay in sync. EditSettings
            // is not in the logical/visual tree and doesn't inherit DataContext on its own.
            DataContextChanged += (_, e) =>
            {
                if (EditSettings != null)
                    EditSettings.DataContext = e.NewValue;
            };
        }

        #region Data Identity

        /// <summary>
        /// Gets or sets the property name on the data source that this column is bound to.
        /// This is the primary key — it auto-generates Binding, SortMemberPath, and FilterMemberPath.
        /// </summary>
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register(
                nameof(FieldName),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnFieldNameChanged));

        public string FieldName
        {
            get => (string)GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        private static void OnFieldNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnDataBase col)
                col.RefreshHeaderCaption();
        }

        // Deliberately a plain CLR property, NOT a DependencyProperty. WPF's Binding markup
        // extension assigns the Binding *object* as a literal only when the target is a CLR
        // property of type BindingBase; against a DependencyProperty it would instead establish a
        // BindingExpression (binding this property to the column's DataContext), so XAML like
        // Binding="{Binding Id}" would never reach the setter as a Binding. This mirrors stock
        // WPF's DataGridBoundColumn.Binding, which is a CLR property for exactly this reason.
        private BindingBase _binding;

        /// <summary>
        /// Gets or sets an explicit binding for the cell's value. When set, it overrides the
        /// binding auto-generated from <see cref="FieldName"/> for the displayed and edited cell
        /// value only — <see cref="FieldName"/> stays the column's identity key and continues to
        /// drive sorting, filtering, validation, read-only resolution, and the header fallback.
        /// When no <see cref="FieldName"/> is set, the binding's path becomes the identity (so a
        /// binding-only <c>Binding="{Binding Id}"</c> column still sorts/filters); a nested path,
        /// different source, or converter is honored for the cell value.
        /// <para>
        /// A single <see cref="System.Windows.Data.Binding"/> has the column's
        /// <see cref="DisplayStringFormat"/> / <see cref="DisplayValueConverter"/> layered onto any
        /// slot it leaves empty. A <see cref="MultiBinding"/> / <see cref="PriorityBinding"/> is
        /// taken verbatim for the read-only / bound-column display and the clipboard value;
        /// template-column editors fall back to <see cref="FieldName"/> for the editable binding
        /// (a multi-binding has no single editable path). Read at column-generation time, mirroring
        /// <see cref="FieldName"/>.
        /// </para>
        /// </summary>
        public BindingBase Binding
        {
            get => _binding;
            set
            {
                _binding = value;

                // Binding-only columns (an explicit Binding with no FieldName, e.g.
                // <GridColumn Binding="{Binding Id}"/> or a runtime-added column over an
                // ExpandoObject source) adopt the binding's path as their identity so sorting,
                // filtering, validation, and the header fallback all work. An explicitly-set
                // FieldName always wins and is left untouched.
                if (string.IsNullOrEmpty(FieldName)
                    && value is Binding b
                    && !string.IsNullOrEmpty(b.Path?.Path))
                {
                    FieldName = b.Path.Path;
                }
            }
        }

        /// <summary>
        /// Gets or sets the property path used for filtering. Overrides <see cref="FieldName"/>.
        /// </summary>
        public static readonly DependencyProperty FilterMemberPathProperty =
            DependencyProperty.Register(
                nameof(FilterMemberPath),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public string FilterMemberPath
        {
            get => (string)GetValue(FilterMemberPathProperty);
            set => SetValue(FilterMemberPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path used for sorting. Overrides <see cref="FieldName"/>.
        /// </summary>
        public static readonly DependencyProperty SortMemberPathProperty =
            DependencyProperty.Register(
                nameof(SortMemberPath),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public string SortMemberPath
        {
            get => (string)GetValue(SortMemberPathProperty);
            set => SetValue(SortMemberPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the display name shown in Column Chooser, Filter Panel, and other UI components.
        /// Overrides <see cref="ColumnLayoutBase.Header"/> for display purposes.
        /// </summary>
        public static readonly DependencyProperty ColumnDisplayNameProperty =
            DependencyProperty.Register(
                nameof(ColumnDisplayName),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnColumnDisplayNameChanged));

        public string ColumnDisplayName
        {
            get => (string)GetValue(ColumnDisplayNameProperty);
            set => SetValue(ColumnDisplayNameProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the column is read-only. Defaults to <c>false</c> — columns are
        /// editable by default, matching the WPF <see cref="DataGridColumn.IsReadOnly"/>
        /// convention. The effective read-only state ORs three sources: this column-local DP,
        /// the host grid's <see cref="DataGrid.IsReadOnly"/>, and any data-source-level
        /// read-only flag (e.g. a <see cref="System.Data.DataColumn.ReadOnly"/> column on a
        /// <c>DataTable</c>). For per-row gating, use <see cref="IsEnabledBinding"/> to disable
        /// specific rows.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false, OnSyncRequiredPropertyChanged));

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets a per-row binding that controls each cell's <c>IsEnabled</c> state.
        /// Evaluated against the row item; <c>false</c> greys out the cell. Applied via a
        /// <see cref="UIElement.IsEnabledProperty"/> setter on the cell style — works on both
        /// template columns and text/checkbox columns.
        /// </summary>
        public static readonly DependencyProperty IsEnabledBindingProperty =
            DependencyProperty.Register(
                nameof(IsEnabledBinding),
                typeof(BindingBase),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public BindingBase IsEnabledBinding
        {
            get => (BindingBase)GetValue(IsEnabledBindingProperty);
            set => SetValue(IsEnabledBindingProperty, value);
        }

        /// <summary>
        /// Gets or sets the data type of the field. Auto-detected from the data source if not set explicitly.
        /// </summary>
        public static readonly DependencyProperty FieldTypeProperty =
            DependencyProperty.Register(
                nameof(FieldType),
                typeof(Type),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnFieldTypePropertyChanged));

        public Type FieldType
        {
            get => (Type)GetValue(FieldTypeProperty);
            set => SetValue(FieldTypeProperty, value);
        }

        // True while the current FieldType value came from SetAutoFieldType. Cleared by the
        // PropertyChangedCallback whenever a foreign write changes the value.
        private bool _isAutoFieldType;

        /// <summary>
        /// Gets whether <see cref="FieldType"/> was set explicitly (by XAML or user code) rather
        /// than resolved from the data source. Uses <see cref="DependencyPropertyHelper.GetValueSource"/>
        /// so explicit assignments equal to the registered default are still detected.
        /// </summary>
        internal bool IsFieldTypeExplicit => IsExplicitlySet(FieldTypeProperty, _isAutoFieldType);

        /// <summary>
        /// Sets <see cref="FieldType"/> from auto-resolution.
        /// The PropertyChangedCallback clears the auto flag at the start of every write — we restore
        /// it after <see cref="SetValue(DependencyProperty, object)"/> returns so a subsequent
        /// <see cref="IsFieldTypeExplicit"/> check correctly reports the value as auto.
        /// </summary>
        internal void SetAutoFieldType(Type type)
        {
            SetValue(FieldTypeProperty, type);
            _isAutoFieldType = true;
        }

        private static void OnFieldTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;

            // Any write clears the auto flag. SetAutoFieldType restores it after SetValue returns.
            col._isAutoFieldType = false;

            // Apply type-based defaults whenever FieldType changes — covers both auto-resolution
            // from the data source and explicit XAML/code values. Properties already set by the
            // user are preserved (each Set... helper checks Is...Explicit).
            col.ApplyTypeBasedDefaults();
        }

        #endregion

        #region Display

        /// <summary>
        /// Gets or sets a .NET format string for display values (e.g., "C2", "MM/dd/yyyy", "N0").
        /// </summary>
        public static readonly DependencyProperty DisplayStringFormatProperty =
            DependencyProperty.Register(
                nameof(DisplayStringFormat),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnDisplayPropertyChanged));

        public string DisplayStringFormat
        {
            get => (string)GetValue(DisplayStringFormatProperty);
            set => SetValue(DisplayStringFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets a custom <see cref="IValueConverter"/> for transforming raw values to display values.
        /// </summary>
        public static readonly DependencyProperty DisplayValueConverterProperty =
            DependencyProperty.Register(
                nameof(DisplayValueConverter),
                typeof(IValueConverter),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnDisplayPropertyChanged));

        public IValueConverter DisplayValueConverter
        {
            get => (IValueConverter)GetValue(DisplayValueConverterProperty);
            set => SetValue(DisplayValueConverterProperty, value);
        }

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="DisplayValueConverter"/>.
        /// </summary>
        public static readonly DependencyProperty DisplayConverterParameterProperty =
            DependencyProperty.Register(
                nameof(DisplayConverterParameter),
                typeof(object),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public object DisplayConverterParameter
        {
            get => GetValue(DisplayConverterParameterProperty);
            set => SetValue(DisplayConverterParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets a mask pattern for formatting raw values into display values.
        /// </summary>
        public static readonly DependencyProperty DisplayMaskProperty =
            DependencyProperty.Register(
                nameof(DisplayMask),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnDisplayPropertyChanged));

        public string DisplayMask
        {
            get => (string)GetValue(DisplayMaskProperty);
            set => SetValue(DisplayMaskProperty, value);
        }

        /// <summary>
        /// Gets or sets whether clipboard copy operations emit this column's formatted
        /// display text (<c>true</c>, default) or its raw editing value (<c>false</c>).
        /// The copy commands always run the cell through the column's display pipeline
        /// (<see cref="DisplayMask"/> / <see cref="DisplayValueConverter"/> /
        /// <see cref="DisplayStringFormat"/> / ComboBox lookup); setting this <c>false</c>
        /// bypasses that and copies the underlying value's <c>ToString()</c> — useful when
        /// the display layer masks an id or code the consumer actually wants to paste.
        /// </summary>
        public static readonly DependencyProperty CopyValueAsDisplayTextProperty =
            DependencyProperty.Register(
                nameof(CopyValueAsDisplayText),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public bool CopyValueAsDisplayText
        {
            get => (bool)GetValue(CopyValueAsDisplayTextProperty);
            set => SetValue(CopyValueAsDisplayTextProperty, value);
        }

        /// <summary>
        /// Gets or sets how cell content aligns horizontally within its editor element. Routes to
        /// the inner control's text-alignment property — <see cref="TextBlock.TextAlignmentProperty"/>
        /// / <see cref="TextBox.TextAlignmentProperty"/> for text editors,
        /// <see cref="Control.HorizontalContentAlignmentProperty"/> for ComboBox, and the
        /// <see cref="FrameworkElement.HorizontalAlignmentProperty"/> of the CheckBox itself for
        /// boolean cells. Editors continue to fill their cell — this only controls where the
        /// content sits inside the editor.
        /// <para>
        /// Auto-derived from <see cref="FieldType"/> when not set explicitly: <c>string</c> →
        /// <see cref="System.Windows.TextAlignment.Left"/>, numeric types →
        /// <see cref="System.Windows.TextAlignment.Right"/>, <c>bool</c> →
        /// <see cref="System.Windows.TextAlignment.Center"/>.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(ColumnDataBase),
                new PropertyMetadata(TextAlignment.Left, OnTextAlignmentPropertyChanged));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        private bool _isAutoTextAlignment;

        /// <summary>
        /// Gets whether <see cref="TextAlignment"/> was set explicitly. Type-based auto-defaults
        /// skip columns where this is true.
        /// </summary>
        internal bool IsTextAlignmentExplicit => IsExplicitlySet(TextAlignmentProperty, _isAutoTextAlignment);

        /// <summary>
        /// Sets <see cref="TextAlignment"/> from auto-configuration. The PropertyChangedCallback
        /// clears the auto flag at the start of every write — this restores it after
        /// <see cref="DependencyObject.SetValue(DependencyProperty, object)"/> returns.
        /// </summary>
        internal void SetAutoTextAlignment(TextAlignment value)
        {
            SetValue(TextAlignmentProperty, value);
            _isAutoTextAlignment = true;
        }

        private static void OnTextAlignmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnDataBase col)
                col._isAutoTextAlignment = false;
        }

        #endregion

        #region Filtering / Search

        /// <summary>
        /// Gets or sets whether the column-filter popup (the drop-down driven by
        /// <see cref="FilterPopupMode"/> / <see cref="CustomColumnFilterTabs"/>)
        /// is exposed for this column. When the local value is set, it overrides the
        /// grid-level <see cref="SearchDataGrid.AllowFilterPopup"/>; when unset, the column
        /// inherits the grid value. <see cref="ActualAllowFilterPopup"/> exposes the
        /// resolved value.
        /// </summary>
        public static readonly DependencyProperty AllowFilterPopupProperty =
            DependencyProperty.Register(
                nameof(AllowFilterPopup),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnFilterPropertyChanged));

        public bool AllowFilterPopup
        {
            get => (bool)GetValue(AllowFilterPopupProperty);
            set => SetValue(AllowFilterPopupProperty, value);
        }

        #region Auto-Filter Row State

        /// <summary>
        /// Column-level override for the grid's <see cref="SearchDataGrid.EnableLiveFiltering"/>.
        /// <c>null</c> (default) inherits the grid value; <c>true</c>/<c>false</c> overrides it for
        /// this column only — <c>true</c> applies filter-row edits as they happen, <c>false</c>
        /// defers them until commit (Enter / Tab / focus loss). The resolved value is exposed by
        /// <see cref="ActualEnableLiveFiltering"/> and consumed by
        /// <c>ColumnFilterControl.EffectiveIsLiveFilteringEnabled</c>. Named to match the grid DP
        /// rather than the DevExpress <c>ImmediateUpdateAutoFilter</c> spelling, by team convention.
        /// </summary>
        public static readonly DependencyProperty EnableLiveFilteringProperty =
            DependencyProperty.Register(
                nameof(EnableLiveFiltering),
                typeof(bool?),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnEnableLiveFilteringChanged));

        public bool? EnableLiveFiltering
        {
            get => (bool?)GetValue(EnableLiveFilteringProperty);
            set => SetValue(EnableLiveFilteringProperty, value);
        }

        private static readonly DependencyPropertyKey ActualEnableLiveFilteringPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualEnableLiveFiltering),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualEnableLiveFilteringProperty = ActualEnableLiveFilteringPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved live-filtering state for this column: the explicit <see cref="EnableLiveFiltering"/>
        /// when set, otherwise the grid-level <see cref="SearchDataGrid.EnableLiveFiltering"/>
        /// (defaulting to <c>true</c> when no grid is attached).
        /// </summary>
        public bool ActualEnableLiveFiltering => (bool)GetValue(ActualEnableLiveFilteringProperty);

        /// <summary>
        /// Resolves the effective live-filtering value — column override first, then the grid,
        /// then <c>true</c>. The single source of truth read by both
        /// <see cref="RefreshActualEnableLiveFiltering"/> and <c>ColumnFilterControl</c> (which
        /// calls this directly to avoid any staleness in the mirror DP).
        /// </summary>
        internal bool ResolveEffectiveEnableLiveFiltering()
            => EnableLiveFiltering ?? View?.EnableLiveFiltering ?? true;

        /// <summary>
        /// Recomputes <see cref="ActualEnableLiveFiltering"/>. Called when the column override
        /// changes, when the column attaches to a grid (<see cref="OnViewChanged"/>), and when the
        /// grid's <see cref="SearchDataGrid.EnableLiveFiltering"/> changes (the grid walks columns).
        /// </summary>
        internal void RefreshActualEnableLiveFiltering()
            => SetValue(ActualEnableLiveFilteringPropertyKey, ResolveEffectiveEnableLiveFiltering());

        private static void OnEnableLiveFilteringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnDataBase col)
                col.RefreshActualEnableLiveFiltering();
        }

        private static readonly DependencyPropertyKey AutoFilterValuePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(AutoFilterValue),
                typeof(object),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AutoFilterValueProperty = AutoFilterValuePropertyKey.DependencyProperty;

        /// <summary>
        /// The value currently held by this column's auto-filter (filter row) cell — the typed
        /// editor value, the text-box string, or the checkbox tri-state. Read-only; pushed by the
        /// live <c>ColumnFilterControl</c> whenever the cell's filter state changes. <c>null</c>
        /// when the cell is empty.
        /// </summary>
        public object AutoFilterValue => GetValue(AutoFilterValueProperty);

        internal void SetAutoFilterValue(object value)
            => SetValue(AutoFilterValuePropertyKey, value);

        private static readonly DependencyPropertyKey AutoFilterConditionPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(AutoFilterCondition),
                typeof(Core.SearchType),
                typeof(ColumnDataBase),
                new PropertyMetadata(Core.SearchType.Equals));

        public static readonly DependencyProperty AutoFilterConditionProperty = AutoFilterConditionPropertyKey.DependencyProperty;

        /// <summary>
        /// The comparison operator (<see cref="Core.SearchType"/>) the auto-filter row currently
        /// uses for this column — e.g. <c>Contains</c>, <c>StartsWith</c>, <c>Equals</c>. Read-only;
        /// pushed by the live <c>ColumnFilterControl</c>. Seeded from <see cref="DefaultSearchType"/>.
        /// </summary>
        public Core.SearchType AutoFilterCondition => (Core.SearchType)GetValue(AutoFilterConditionProperty);

        internal void SetAutoFilterCondition(Core.SearchType value)
            => SetValue(AutoFilterConditionPropertyKey, value);

        private static readonly DependencyPropertyKey AutoFilterHeaderStatePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(AutoFilterHeaderState),
                typeof(AutoFilterHeaderState),
                typeof(ColumnDataBase),
                new PropertyMetadata(AutoFilterHeaderState.Empty));

        public static readonly DependencyProperty AutoFilterHeaderStateProperty = AutoFilterHeaderStatePropertyKey.DependencyProperty;

        /// <summary>
        /// Aggregate semantic state of this column's auto-filter cell
        /// (<see cref="Wpf.AutoFilterHeaderState"/>: Empty / PendingInput / Active / Disabled /
        /// Hidden). Read-only; pushed by the live <c>ColumnFilterControl</c>.
        /// </summary>
        public AutoFilterHeaderState AutoFilterHeaderState => (AutoFilterHeaderState)GetValue(AutoFilterHeaderStateProperty);

        internal void SetAutoFilterHeaderState(AutoFilterHeaderState value)
            => SetValue(AutoFilterHeaderStatePropertyKey, value);

        #endregion

        /// <summary>
        /// Gets or sets the default search type for this column's auto-filter row quick search.
        /// </summary>
        /// <remarks>
        /// String columns default to <see cref="Wpf.DefaultSearchType.StartsWith"/> (set in
        /// <see cref="ApplyTypeBasedDefaults"/>). Other CLR types use the registered default
        /// (<see cref="Wpf.DefaultSearchType.StartsWith"/>) unless overridden by
        /// <see cref="ApplyTypeBasedDefaults"/> (e.g. <c>DateTime</c> / enums → <c>Equals</c>).
        /// </remarks>
        public static readonly DependencyProperty DefaultSearchTypeProperty =
            DependencyProperty.Register(
                nameof(DefaultSearchType),
                typeof(DefaultSearchType),
                typeof(ColumnDataBase),
                new PropertyMetadata(WWControls.Wpf.DefaultSearchType.StartsWith, OnDefaultSearchTypePropertyChanged));

        public DefaultSearchType DefaultSearchType
        {
            get => (DefaultSearchType)GetValue(DefaultSearchTypeProperty);
            set => SetValue(DefaultSearchTypeProperty, value);
        }

        private bool _isAutoDefaultSearchType;

        /// <summary>
        /// Gets whether <see cref="DefaultSearchType"/> was set explicitly. Auto-configuration
        /// from <see cref="FieldType"/> skips columns where this is true.
        /// </summary>
        internal bool IsDefaultSearchTypeExplicit => IsExplicitlySet(DefaultSearchTypeProperty, _isAutoDefaultSearchType);

        /// <summary>
        /// Sets <see cref="DefaultSearchType"/> from auto-configuration.
        /// </summary>
        internal void SetAutoDefaultSearchType(DefaultSearchType type)
        {
            SetValue(DefaultSearchTypeProperty, type);
            _isAutoDefaultSearchType = true;
        }

        private static void OnDefaultSearchTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col._isAutoDefaultSearchType = false;

            // Spec rule: when the column's default search criteria is excluded from the matching
            // host's SupportedSearchTypes whitelist, the cell disables. The resolved
            // DefaultSearchType is one of the inputs to that check — re-evaluate on the host
            // whenever this value changes.
            if (col.View == null) return;
            var host = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn) as ColumnFilterControl;
            host?.UpdateEffectiveIsCellEnabled();
        }

        /// <summary>
        /// Gets or sets whether to force checkbox filtering mode in the search box.
        /// </summary>
        public static readonly DependencyProperty UseCheckBoxInSearchBoxProperty =
            DependencyProperty.Register(
                nameof(UseCheckBoxInSearchBox),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false, OnUseCheckBoxInSearchBoxPropertyChanged));

        public bool UseCheckBoxInSearchBox
        {
            get => (bool)GetValue(UseCheckBoxInSearchBoxProperty);
            set => SetValue(UseCheckBoxInSearchBoxProperty, value);
        }

        private bool _isAutoUseCheckBoxInSearchBox;

        /// <summary>
        /// Gets whether <see cref="UseCheckBoxInSearchBox"/> was set explicitly. Auto-configuration
        /// from <see cref="FieldType"/> skips columns where this is true.
        /// </summary>
        internal bool IsUseCheckBoxInSearchBoxExplicit => IsExplicitlySet(UseCheckBoxInSearchBoxProperty, _isAutoUseCheckBoxInSearchBox);

        /// <summary>
        /// Sets <see cref="UseCheckBoxInSearchBox"/> from auto-configuration.
        /// </summary>
        internal void SetAutoUseCheckBoxInSearchBox(bool value)
        {
            SetValue(UseCheckBoxInSearchBoxProperty, value);
            _isAutoUseCheckBoxInSearchBox = true;
        }

        private static void OnUseCheckBoxInSearchBoxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col._isAutoUseCheckBoxInSearchBox = false;

            if (col.View == null) return;
            var searchBox = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn);
            searchBox?.DetermineCheckboxColumnTypeFromColumnDefinition();
        }

        /// <summary>
        /// Gets or sets a custom search template type for this column.
        /// </summary>
        public static readonly DependencyProperty CustomSearchTemplateProperty =
            DependencyProperty.Register(
                nameof(CustomSearchTemplate),
                typeof(Type),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public Type CustomSearchTemplate
        {
            get => (Type)GetValue(CustomSearchTemplateProperty);
            set => SetValue(CustomSearchTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets whether filtering is allowed on this column. When <c>false</c> the
        /// search box / auto-filter cell is hidden entirely (distinct from
        /// <see cref="AllowAutoFilter"/>, which only greys the cell). <see cref="ActualAllowFiltering"/>
        /// exposes the resolved value.
        /// </summary>
        public static readonly DependencyProperty AllowFilteringProperty =
            DependencyProperty.Register(
                nameof(AllowFiltering),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnFilterPropertyChanged));

        public bool AllowFiltering
        {
            get => (bool)GetValue(AllowFilteringProperty);
            set => SetValue(AllowFilteringProperty, value);
        }

        private static readonly DependencyPropertyKey ActualAllowFilteringPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowFiltering),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualAllowFilteringProperty = ActualAllowFilteringPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="AllowFiltering"/>. Mirrors the column-level value today; reserved
        /// for future grid-level default resolution.
        /// </summary>
        public bool ActualAllowFiltering => (bool)GetValue(ActualAllowFilteringProperty);

        private static readonly DependencyPropertyKey ActualAllowFilterPopupPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowFilterPopup),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualAllowFilterPopupProperty = ActualAllowFilterPopupPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="AllowFilterPopup"/>. Mirrors the column-level value today;
        /// real resolution (column-local override falling back to <see cref="SearchDataGrid.AllowFilterPopup"/>)
        /// happens inside <c>ColumnFilterControl.ResolveAllowFilterPopup</c> at runtime.
        /// </summary>
        public bool ActualAllowFilterPopup => (bool)GetValue(ActualAllowFilterPopupProperty);

        /// <summary>
        /// Gets or sets whether the column-header dropdown filter popup is exposed. Reserved
        /// for Phase 2.1's drop-down filter UI — no current implementation consults this DP,
        /// but the surface exists so consumer XAML can opt out without breaking when the popup
        /// ships. Default <c>true</c>.
        /// </summary>
        public static readonly DependencyProperty AllowColumnFilteringProperty =
            DependencyProperty.Register(
                nameof(AllowColumnFiltering),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnFilterPropertyChanged));

        public bool AllowColumnFiltering
        {
            get => (bool)GetValue(AllowColumnFilteringProperty);
            set => SetValue(AllowColumnFilteringProperty, value);
        }

        private static readonly DependencyPropertyKey ActualAllowColumnFilteringPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowColumnFiltering),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualAllowColumnFilteringProperty = ActualAllowColumnFilteringPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="AllowColumnFiltering"/>. Mirrors the column-level value today;
        /// reserved for future grid-level default resolution.
        /// </summary>
        public bool ActualAllowColumnFiltering => (bool)GetValue(ActualAllowColumnFilteringProperty);

        #region Allowed Filter Categories

        // Each Allowed*Filters DP gates a category of search operators. Default true — the
        // category is exposed by the filter UI when the underlying EditSettings also lists it
        // in GetSupportedFilterSearchTypes. Currently advisory: the auto-filter row selector
        // and the rule editor don't yet intersect these flags with the EditSettings whitelist.
        // Wiring lands when a consumer first asks for it.

        public static readonly DependencyProperty AllowedAggregateFiltersProperty =
            DependencyProperty.Register(nameof(AllowedAggregateFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Aggregate operators (Sum, Average, Min, Max, Count) for use in summary filters. Default <c>true</c>.</summary>
        public bool AllowedAggregateFilters
        {
            get => (bool)GetValue(AllowedAggregateFiltersProperty);
            set => SetValue(AllowedAggregateFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedAnyOfFiltersProperty =
            DependencyProperty.Register(nameof(AllowedAnyOfFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>"Is any of" / "is none of" set-membership operators. Default <c>true</c>.</summary>
        public bool AllowedAnyOfFilters
        {
            get => (bool)GetValue(AllowedAnyOfFiltersProperty);
            set => SetValue(AllowedAnyOfFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedBetweenFiltersProperty =
            DependencyProperty.Register(nameof(AllowedBetweenFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Range operators (Between / NotBetween). Default <c>true</c>.</summary>
        public bool AllowedBetweenFilters
        {
            get => (bool)GetValue(AllowedBetweenFiltersProperty);
            set => SetValue(AllowedBetweenFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedBinaryFiltersProperty =
            DependencyProperty.Register(nameof(AllowedBinaryFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Binary comparison operators (Equals, NotEquals, GreaterThan, LessThan, …). Default <c>true</c>.</summary>
        public bool AllowedBinaryFilters
        {
            get => (bool)GetValue(AllowedBinaryFiltersProperty);
            set => SetValue(AllowedBinaryFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedDataAnalysisFiltersProperty =
            DependencyProperty.Register(nameof(AllowedDataAnalysisFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Data-analysis operators (TopN / BottomN / AboveAverage / …). Default <c>true</c>.</summary>
        public bool AllowedDataAnalysisFilters
        {
            get => (bool)GetValue(AllowedDataAnalysisFiltersProperty);
            set => SetValue(AllowedDataAnalysisFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedDateTimeFiltersProperty =
            DependencyProperty.Register(nameof(AllowedDateTimeFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Date-relative operators (Today, Yesterday, ThisWeek, NextMonth, …). Default <c>true</c>.</summary>
        public bool AllowedDateTimeFilters
        {
            get => (bool)GetValue(AllowedDateTimeFiltersProperty);
            set => SetValue(AllowedDateTimeFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedFormatConditionFiltersProperty =
            DependencyProperty.Register(nameof(AllowedFormatConditionFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Conditional-formatting comparison operators. Default <c>true</c>.</summary>
        public bool AllowedFormatConditionFilters
        {
            get => (bool)GetValue(AllowedFormatConditionFiltersProperty);
            set => SetValue(AllowedFormatConditionFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedTimeOnlyFiltersProperty =
            DependencyProperty.Register(nameof(AllowedTimeOnlyFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Time-of-day comparison operators (BeforeNoon, BetweenHours, …). Default <c>true</c>.</summary>
        public bool AllowedTimeOnlyFilters
        {
            get => (bool)GetValue(AllowedTimeOnlyFiltersProperty);
            set => SetValue(AllowedTimeOnlyFiltersProperty, value);
        }

        public static readonly DependencyProperty AllowedUnaryFiltersProperty =
            DependencyProperty.Register(nameof(AllowedUnaryFilters), typeof(bool), typeof(ColumnDataBase), new PropertyMetadata(true));
        /// <summary>Unary operators (IsNull / IsNotNull / IsTrue / IsFalse / IsBlank / IsNotBlank). Default <c>true</c>.</summary>
        public bool AllowedUnaryFilters
        {
            get => (bool)GetValue(AllowedUnaryFiltersProperty);
            set => SetValue(AllowedUnaryFiltersProperty, value);
        }

        #endregion

        /// <summary>
        /// Gets or sets whether sorting is allowed on this column.
        /// When false, clicking the header does not sort.
        /// </summary>
        public static readonly DependencyProperty AllowSortingProperty =
            DependencyProperty.Register(
                nameof(AllowSorting),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnAllowSortingChanged));

        public bool AllowSorting
        {
            get => (bool)GetValue(AllowSortingProperty);
            set => SetValue(AllowSortingProperty, value);
        }

        private static void OnAllowSortingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.SyncToInternalColumn();
            col.RefreshActualAllowSorting();
        }

        private void RefreshActualAllowSorting()
        {
            // Effective sortability requires both the column-wide gate AND at least one allowed
            // direction. AllowedSortOrders=None makes the column unsortable even if AllowSorting
            // is true — the header click would otherwise fire and we'd cancel it anyway.
            SetActualAllowSorting(AllowSorting && AllowedSortOrders != AllowedSortOrders.None);
        }

        #region Sorting State

        private static readonly DependencyPropertyKey SortOrderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SortOrder),
                typeof(ColumnSortOrder),
                typeof(ColumnDataBase),
                new PropertyMetadata(ColumnSortOrder.None, OnSortOrderChanged));

        /// <summary>
        /// Read-only dependency property exposing <see cref="SortOrder"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty SortOrderProperty = SortOrderPropertyKey.DependencyProperty;

        /// <summary>
        /// Current sort direction applied to this column, pushed by <see cref="SearchDataGrid"/>
        /// when the underlying <see cref="DataGridColumn.SortDirection"/> changes (header click,
        /// programmatic sort, or clear-sort command). <see cref="ColumnSortOrder.None"/> when
        /// the column is not participating in the current sort.
        /// </summary>
        public ColumnSortOrder SortOrder => (ColumnSortOrder)GetValue(SortOrderProperty);

        internal void SetSortOrder(ColumnSortOrder value)
            => SetValue(SortOrderPropertyKey, value);

        private static void OnSortOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.SetValue(IsSortedPropertyKey, (ColumnSortOrder)e.NewValue != ColumnSortOrder.None);
        }

        private static readonly DependencyPropertyKey SortIndexPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SortIndex),
                typeof(int),
                typeof(ColumnDataBase),
                new PropertyMetadata(-1));

        /// <summary>
        /// Read-only dependency property exposing <see cref="SortIndex"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty SortIndexProperty = SortIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// Zero-based position of this column within the current multi-column sort. <c>0</c> is
        /// the primary sort column, <c>1</c> secondary, etc. <c>-1</c> when not sorted.
        /// </summary>
        public int SortIndex => (int)GetValue(SortIndexProperty);

        internal void SetSortIndex(int value)
            => SetValue(SortIndexPropertyKey, value);

        private static readonly DependencyPropertyKey IsSortedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsSorted),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsSortedProperty = IsSortedPropertyKey.DependencyProperty;

        /// <summary>
        /// True when <see cref="SortOrder"/> is not <see cref="ColumnSortOrder.None"/>. Derived
        /// automatically; bindable for styles that want to highlight the sorted column.
        /// </summary>
        public bool IsSorted => (bool)GetValue(IsSortedProperty);

        private static readonly DependencyPropertyKey ActualAllowSortingPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowSorting),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualAllowSortingProperty = ActualAllowSortingPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved sortability — currently mirrors <see cref="AllowSorting"/>. Reserved as a
        /// read-only projection so future logic that combines the column setting with grid-level
        /// configuration (e.g. <c>SearchDataGrid.AllowSorting</c>) can light up without breaking
        /// existing bindings.
        /// </summary>
        public bool ActualAllowSorting => (bool)GetValue(ActualAllowSortingProperty);

        internal void SetActualAllowSorting(bool value)
            => SetValue(ActualAllowSortingPropertyKey, value);

        /// <summary>
        /// Gets or sets the sort direction applied the first time this column enters the sort
        /// (e.g. on the first header click). The grid honors this value once <c>1.2</c>'s
        /// click-driven sort wiring is in place; until then it is informational.
        /// </summary>
        public static readonly DependencyProperty DefaultSortOrderProperty =
            DependencyProperty.Register(
                nameof(DefaultSortOrder),
                typeof(ColumnSortOrder),
                typeof(ColumnDataBase),
                new PropertyMetadata(ColumnSortOrder.Ascending));

        public ColumnSortOrder DefaultSortOrder
        {
            get => (ColumnSortOrder)GetValue(DefaultSortOrderProperty);
            set => SetValue(DefaultSortOrderProperty, value);
        }

        /// <summary>
        /// Gets or sets the bitmask of allowed sort directions for this column. Header-click
        /// cycling skips directions not in the mask. Default is
        /// <see cref="Wpf.AllowedSortOrders.All"/>.
        /// </summary>
        public static readonly DependencyProperty AllowedSortOrdersProperty =
            DependencyProperty.Register(
                nameof(AllowedSortOrders),
                typeof(AllowedSortOrders),
                typeof(ColumnDataBase),
                new PropertyMetadata(Wpf.AllowedSortOrders.All, OnAllowedSortOrdersChanged));

        public AllowedSortOrders AllowedSortOrders
        {
            get => (AllowedSortOrders)GetValue(AllowedSortOrdersProperty);
            set => SetValue(AllowedSortOrdersProperty, value);
        }

        private static void OnAllowedSortOrdersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.RefreshActualAllowSorting();
            col.SyncToInternalColumn();
        }

        /// <summary>
        /// Gets or sets how the column's values are compared when sorting. Only
        /// <see cref="ColumnSortMode.Value"/> is wired today; the other modes are reserved.
        /// </summary>
        public static readonly DependencyProperty SortModeProperty =
            DependencyProperty.Register(
                nameof(SortMode),
                typeof(ColumnSortMode),
                typeof(ColumnDataBase),
                new PropertyMetadata(ColumnSortMode.Value));

        public ColumnSortMode SortMode
        {
            get => (ColumnSortMode)GetValue(SortModeProperty);
            set => SetValue(SortModeProperty, value);
        }

        #endregion

        /// <summary>
        /// Gets or sets a column-level override for the grid's
        /// <see cref="SearchDataGrid.ShowCriteriaInFilterRow"/>. <c>null</c> (the default)
        /// inherits the grid value; <c>true</c> / <c>false</c> overrides it for this column.
        /// </summary>
        public static readonly DependencyProperty ShowCriteriaInFilterRowProperty =
            DependencyProperty.Register(
                nameof(ShowCriteriaInFilterRow),
                typeof(bool?),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnShowCriteriaInFilterRowChanged));

        public bool? ShowCriteriaInFilterRow
        {
            get => (bool?)GetValue(ShowCriteriaInFilterRowProperty);
            set => SetValue(ShowCriteriaInFilterRowProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the filter row cell for this column is enabled.
        /// <c>false</c> disables (greys) the cell while preserving its space — distinct
        /// from <see cref="AllowFiltering"/>, which hides the cell entirely.
        /// </summary>
        public static readonly DependencyProperty AllowAutoFilterProperty =
            DependencyProperty.Register(
                nameof(AllowAutoFilter),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnAllowAutoFilterChanged));

        public bool AllowAutoFilter
        {
            get => (bool)GetValue(AllowAutoFilterProperty);
            set => SetValue(AllowAutoFilterProperty, value);
        }

        /// <summary>
        /// Column-level override for date-only filter comparison on DateTime columns.
        /// <c>null</c> (default) auto-detects: the column samples its bound values, and the
        /// resolved value is <c>true</c> when no value carries a non-zero time-of-day (the
        /// auto editor is a date-only DatePicker, so filtering by date is the only sensible
        /// comparison) and <c>false</c> when at least one value has a time component (the
        /// auto editor exposes a time segment, so the user's typed time is honored). Set to
        /// <c>true</c> / <c>false</c> to override the auto-detection.
        /// Surfaced as <see cref="SearchCondition.RoundDateTime"/> on every condition the
        /// column's filter row produces.
        /// </summary>
        public static readonly DependencyProperty RoundDateTimeProperty =
            DependencyProperty.Register(
                nameof(RoundDateTime),
                typeof(bool?),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnRoundDateTimeChanged));

        public bool? RoundDateTime
        {
            get => (bool?)GetValue(RoundDateTimeProperty);
            set => SetValue(RoundDateTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets the .NET date/time format string used to render this column's values
        /// when its dates are rounded to date-only (e.g. <c>"MM/dd/yyyy"</c>, <c>"d"</c>).
        /// Acts as a lower-priority, date-specific fallback for <see cref="DisplayStringFormat"/>:
        /// the display pipeline consults it after <see cref="DisplayStringFormat"/> at every
        /// display-consumption point (cell binding, filter chips, copy commands). Because a
        /// date-only format carries no time tokens, setting it also drives
        /// <see cref="ResolveEffectiveRoundDateTime"/> to <c>true</c> — so a column rendered
        /// date-only filters on dates only, without a separate <see cref="RoundDateTime"/> set.
        /// Display-only: it never alters the editor's input mask.
        /// </summary>
        public static readonly DependencyProperty RoundDateDisplayFormatProperty =
            DependencyProperty.Register(
                nameof(RoundDateDisplayFormat),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnDisplayPropertyChanged));

        public string RoundDateDisplayFormat
        {
            get => (string)GetValue(RoundDateDisplayFormatProperty);
            set => SetValue(RoundDateDisplayFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets a column-level override for the grid's
        /// <see cref="SearchDataGrid.FilterRowCellStyle"/>. <c>null</c> (default)
        /// inherits the grid setting; any non-null <see cref="Style"/> wins over the grid
        /// value for this column. When both are null, the keyed theme style
        /// (<see cref="ThemeKeys.FilterRow.ColumnFilterControl"/>) is used.
        /// </summary>
        public static readonly DependencyProperty FilterRowCellStyleProperty =
            DependencyProperty.Register(
                nameof(FilterRowCellStyle),
                typeof(Style),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnFilterRowCellStyleChanged));

        public Style FilterRowCellStyle
        {
            get => (Style)GetValue(FilterRowCellStyleProperty);
            set => SetValue(FilterRowCellStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used as the display surface for the
        /// filter row cell on this column. When set, it replaces the default editor
        /// produced by <see cref="BaseEditSettings.CreateFilterEditor"/>. The template's
        /// <see cref="FrameworkElement.DataContext"/> is an <see cref="EditGridCellData"/>
        /// instance with <c>Value</c> two-way-bound to the column's filter value; templates
        /// that don't bind <c>{Binding Value}</c> simply won't drive the filter — matching
        /// DevExpress's behavior. Used as the fallback when
        /// <see cref="FilterRowEditTemplate"/> is also <c>null</c>; the filter row is
        /// always-edit, so the spec's display/edit distinction collapses here.
        /// </summary>
        public static readonly DependencyProperty FilterRowDisplayTemplateProperty =
            DependencyProperty.Register(
                nameof(FilterRowDisplayTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnFilterRowTemplateChanged));

        public DataTemplate FilterRowDisplayTemplate
        {
            get => (DataTemplate)GetValue(FilterRowDisplayTemplateProperty);
            set => SetValue(FilterRowDisplayTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used as the edit surface for the
        /// filter row cell on this column. Takes precedence over
        /// <see cref="FilterRowDisplayTemplate"/> — when both are set, the edit template
        /// wins; when only the display template is set, it serves both display and edit roles
        /// (the filter row is always-edit). Same <see cref="EditGridCellData"/> context shape
        /// as <see cref="FilterRowDisplayTemplate"/>.
        /// </summary>
        public static readonly DependencyProperty FilterRowEditTemplateProperty =
            DependencyProperty.Register(
                nameof(FilterRowEditTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnFilterRowTemplateChanged));

        public DataTemplate FilterRowEditTemplate
        {
            get => (DataTemplate)GetValue(FilterRowEditTemplateProperty);
            set => SetValue(FilterRowEditTemplateProperty, value);
        }

        #region Column Filter Popup 

        // Drop-down filter popup configuration. The popup itself — typically opened by clicking
        // a chevron glyph on the column header and offering a checkbox-list of distinct values,
        // a search box, and predefined filter shortcuts — is not yet implemented. These DPs
        // exist so consumer XAML can configure the popup ahead of time; once the popup ships,
        // it consults each of these without breaking existing markup.

        /// <summary>
        /// How the popup compares row values for filter matching: against the raw underlying
        /// <c>Value</c> or against the rendered <c>DisplayText</c> (default).
        /// </summary>
        public static readonly DependencyProperty ColumnFilterModeProperty =
            DependencyProperty.Register(
                nameof(ColumnFilterMode),
                typeof(ColumnFilterMode),
                typeof(ColumnDataBase),
                new PropertyMetadata(ColumnFilterMode.DisplayText));

        public ColumnFilterMode ColumnFilterMode
        {
            get => (ColumnFilterMode)GetValue(ColumnFilterModeProperty);
            set => SetValue(ColumnFilterModeProperty, value);
        }

        /// <summary>
        /// Layout strategy for the popup. <see cref="Wpf.FilterPopupMode.Default"/> renders the
        /// stock tabbed UI (Filter Rules + Filter Values with checkbox list or DateTime tree by
        /// column type). When <see cref="CustomColumnFilterTabs"/> is non-empty the editor
        /// substitutes those tabs implicitly — there is no separate <c>Custom</c> enum value to
        /// set. The DP is preserved as the future expansion point for additional layout modes.
        /// </summary>
        public static readonly DependencyProperty FilterPopupModeProperty =
            DependencyProperty.Register(
                nameof(FilterPopupMode),
                typeof(FilterPopupMode),
                typeof(ColumnDataBase),
                new PropertyMetadata(FilterPopupMode.Default));

        public FilterPopupMode FilterPopupMode
        {
            get => (FilterPopupMode)GetValue(FilterPopupModeProperty);
            set => SetValue(FilterPopupModeProperty, value);
        }

        /// <summary>
        /// Cap on the number of distinct values the popup loads. Large columns degrade UI
        /// responsiveness past a few thousand entries; the default of <c>1000</c> balances
        /// coverage against perf. Setting <c>0</c> means unbounded (use only on small datasets).
        /// </summary>
        public static readonly DependencyProperty ColumnFilterPopupMaxRecordsCountProperty =
            DependencyProperty.Register(
                nameof(ColumnFilterPopupMaxRecordsCount),
                typeof(int),
                typeof(ColumnDataBase),
                new PropertyMetadata(1000));

        public int ColumnFilterPopupMaxRecordsCount
        {
            get => (int)GetValue(ColumnFilterPopupMaxRecordsCountProperty);
            set => SetValue(ColumnFilterPopupMaxRecordsCountProperty, value);
        }

        /// <summary>
        /// Consumer-supplied tab list for the column filter popup. When non-empty AND
        /// <see cref="FilterPopupMode"/> is <see cref="Wpf.FilterPopupMode.Custom"/>, the popup
        /// replaces its default Rules + Values UI with a <see cref="TabControl"/> built from
        /// these entries — symmetric with how the default popup itself is tabbed. Single-tab
        /// consumers add one <see cref="ColumnFilterTab"/>; the popup still renders a tab strip
        /// so users get consistent chrome.
        /// </summary>
        public static readonly DependencyProperty CustomColumnFilterTabsProperty =
            DependencyProperty.Register(
                nameof(CustomColumnFilterTabs),
                typeof(FreezableCollection<ColumnFilterTab>),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public FreezableCollection<ColumnFilterTab> CustomColumnFilterTabs
        {
            get => (FreezableCollection<ColumnFilterTab>)GetValue(CustomColumnFilterTabsProperty);
            set => SetValue(CustomColumnFilterTabsProperty, value);
        }

        /// <summary>
        /// Comma-separated field-name list that the popup groups its checkbox entries under
        /// — e.g. <c>"Region,Country"</c> nests countries under regions. Order-sensitive.
        /// </summary>
        public static readonly DependencyProperty FilterPopupGroupFieldsProperty =
            DependencyProperty.Register(
                nameof(FilterPopupGroupFields),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public string FilterPopupGroupFields
        {
            get => (string)GetValue(FilterPopupGroupFieldsProperty);
            set => SetValue(FilterPopupGroupFieldsProperty, value);
        }

        /// <summary>
        /// When <c>true</c>, each click in the popup applies the filter immediately. When
        /// <c>false</c> (default), the popup batches selections and waits for an explicit
        /// Apply button.
        /// </summary>
        public static readonly DependencyProperty ImmediateUpdateColumnFilterProperty =
            DependencyProperty.Register(
                nameof(ImmediateUpdateColumnFilter),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public bool ImmediateUpdateColumnFilter
        {
            get => (bool)GetValue(ImmediateUpdateColumnFilterProperty);
            set => SetValue(ImmediateUpdateColumnFilterProperty, value);
        }

        /// <summary>
        /// When <c>true</c>, the popup shows every distinct value in the underlying source.
        /// When <c>false</c> (default), the popup limits itself to values currently passing
        /// the other columns' filters — Excel's behavior, useful for progressive narrowing.
        /// </summary>
        public static readonly DependencyProperty ShowAllTableValuesInFilterPopupProperty =
            DependencyProperty.Register(
                nameof(ShowAllTableValuesInFilterPopup),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public bool ShowAllTableValuesInFilterPopup
        {
            get => (bool)GetValue(ShowAllTableValuesInFilterPopupProperty);
            set => SetValue(ShowAllTableValuesInFilterPopupProperty, value);
        }

        /// <summary>
        /// Same as <see cref="ShowAllTableValuesInFilterPopup"/>, but specifically for the
        /// checked-list popup mode. Lets consumers configure progressive narrowing differently
        /// for the checkbox-list style vs the list style.
        /// </summary>
        public static readonly DependencyProperty ShowAllTableValuesInCheckedFilterPopupProperty =
            DependencyProperty.Register(
                nameof(ShowAllTableValuesInCheckedFilterPopup),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public bool ShowAllTableValuesInCheckedFilterPopup
        {
            get => (bool)GetValue(ShowAllTableValuesInCheckedFilterPopupProperty);
            set => SetValue(ShowAllTableValuesInCheckedFilterPopupProperty, value);
        }

        /// <summary>
        /// On <c>DateTime</c> columns, controls whether the popup includes a "(empty)" entry
        /// matching rows where the date is null / unset. Default <c>true</c> — most consumers
        /// want explicit filtering for missing dates.
        /// </summary>
        public static readonly DependencyProperty ShowEmptyDateFilterProperty =
            DependencyProperty.Register(
                nameof(ShowEmptyDateFilter),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public bool ShowEmptyDateFilter
        {
            get => (bool)GetValue(ShowEmptyDateFilterProperty);
            set => SetValue(ShowEmptyDateFilterProperty, value);
        }

        /// <summary>
        /// Consumer-supplied named filter shortcuts shown at the top of the popup (e.g. "Last
        /// 7 days", "This month"). The concrete element type is intentionally
        /// <see cref="System.Collections.IList"/> so the eventual <c>ColumnFilterDefinition</c>
        /// shape can land without a DP rename. <c>null</c> means no predefined filters.
        /// </summary>
        public static readonly DependencyProperty PredefinedFiltersProperty =
            DependencyProperty.Register(
                nameof(PredefinedFilters),
                typeof(System.Collections.IList),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public System.Collections.IList PredefinedFilters
        {
            get => (System.Collections.IList)GetValue(PredefinedFiltersProperty);
            set => SetValue(PredefinedFiltersProperty, value);
        }

        private static readonly DependencyPropertyKey IsFilteredPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFiltered),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsFilteredProperty = IsFilteredPropertyKey.DependencyProperty;

        /// <summary>
        /// True when the column has an active rule filter. Driven from
        /// <see cref="WWControls.Core.SearchTemplateController.HasCustomExpression"/>
        /// on the descriptor's controller — the setter on <c>SearchTemplateController</c>
        /// subscribes to controller property changes and pushes this DP. Read-only externally
        /// so consumer styles can bind for "filtered column" affordances (filter icon visibility,
        /// header highlight, etc.).
        /// </summary>
        public bool IsFiltered => (bool)GetValue(IsFilteredProperty);

        internal void SetIsFiltered(bool value)
            => SetValue(IsFilteredPropertyKey, value);

        #endregion

        #endregion

        #region Editor

        /// <summary>
        /// Gets or sets the editor configuration for this column. When set, the grid generates a
        /// <see cref="DataGridTemplateColumn"/> using <see cref="BaseEditSettings.CreateDisplayTemplate"/>
        /// and <see cref="BaseEditSettings.CreateEditTemplate"/> instead of the default text/checkbox
        /// column. Leave null to use the default column type chosen from <see cref="FieldType"/>.
        /// </summary>
        public static readonly DependencyProperty EditSettingsProperty =
            DependencyProperty.Register(
                nameof(EditSettings),
                typeof(BaseEditSettings),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnEditSettingsChanged));

        public BaseEditSettings EditSettings
        {
            get => (BaseEditSettings)GetValue(EditSettingsProperty);
            set => SetValue(EditSettingsProperty, value);
        }

        private static void OnEditSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;

            // EditSettings is a FrameworkContentElement but isn't part of the visual / logical tree,
            // so it doesn't automatically inherit DataContext. Push the column's current DataContext
            // down so XAML bindings on EditSettings (e.g. ComboBoxEditSettings.ItemsSource) resolve
            // against the same source as bindings elsewhere on the grid.
            if (e.NewValue is BaseEditSettings settings)
                settings.DataContext = col.DataContext;

            // Surface the resolved value for bindings. Mirror-only today; reserved for future
            // grid-level default resolution.
            col.SetValue(ActualEditSettingsPropertyKey, e.NewValue);

            // SyncToInternalColumn re-resolves the cell templates against the new EditSettings
            // through ResolveEffectiveCellDisplay/EditTemplate. CellDisplayTemplate /
            // CellEditTemplate (when set) still win over the new EditSettings via that resolver.
            col.SyncToInternalColumn();

            // Editor-shape preference may differ from the CLR-type default applied earlier.
            // ApplyTypeBasedDefaults also calls this, but that runs on FieldType change — when
            // EditSettings is set or swapped after FieldType has settled, we still need to clamp.
            col.ApplyEditSettingsPreferredDefaults();
        }

        private static readonly DependencyPropertyKey ActualEditSettingsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualEditSettings),
                typeof(BaseEditSettings),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualEditSettingsProperty = ActualEditSettingsPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="EditSettings"/>. Mirrors the column-level value today; reserved
        /// for future grid-level default resolution (e.g. a <c>SearchDataGrid.DefaultEditSettings</c>
        /// per CLR type).
        /// </summary>
        public BaseEditSettings ActualEditSettings => (BaseEditSettings)GetValue(ActualEditSettingsProperty);

        /// <summary>
        /// Gets or sets whether cell edits commit on every change (typically every keystroke)
        /// rather than on focus loss / commit. Default <c>false</c>. Wiring this to the
        /// generated edit template's <see cref="System.Windows.Data.UpdateSourceTrigger"/> lives
        /// inside the relevant <see cref="BaseEditSettings"/> subclasses — currently the DP is
        /// exposed for consumer configuration but not yet honored by any editor.
        /// </summary>
        public static readonly DependencyProperty EnableImmediatePostingProperty =
            DependencyProperty.Register(
                nameof(EnableImmediatePosting),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public bool EnableImmediatePosting
        {
            get => (bool)GetValue(EnableImmediatePostingProperty);
            set => SetValue(EnableImmediatePostingProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this column configures itself from the bound property's data
        /// annotations. When <c>true</c>, the grid reads the property's
        /// <see cref="System.ComponentModel.DataAnnotations"/> metadata at column-generation time
        /// — <c>Display</c> (header / order), <c>DataType</c> / <c>DisplayFormat</c> (display
        /// format), the library's mask attributes (<c>SimpleMask</c> / <c>NumericMask</c> /
        /// <c>DateTimeMask</c>), and editor attributes (<c>DefaultEditor</c> / <c>GridEditor</c>)
        /// — and applies them to this descriptor on top of the CLR-type defaults. Annotation
        /// values never overwrite a property the consumer set explicitly. Default <c>false</c>.
        /// Auto-generated columns inherit <see cref="SearchDataGrid.EnableSmartColumnsGeneration"/>.
        /// </summary>
        /// <remarks>
        /// Read once when the column is generated. Toggling it after the grid has built the column
        /// has no retroactive effect; set it in XAML / before the grid loads.
        /// </remarks>
        public static readonly DependencyProperty IsSmartProperty =
            DependencyProperty.Register(
                nameof(IsSmart),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public bool IsSmart
        {
            get => (bool)GetValue(IsSmartProperty);
            set => SetValue(IsSmartProperty, value);
        }

        #endregion

        #region Edit Form

        /// <summary>
        /// Gets or sets whether this column appears in the grid's edit form (see
        /// <see cref="SearchDataGrid.EditFormShowMode"/>). <c>null</c> (the default) inherits the
        /// column's <see cref="ColumnLayoutBase.Visible"/> state; <c>true</c> / <c>false</c>
        /// forces the field into or out of the form independently of grid visibility. Resolved by
        /// <see cref="ResolveEffectiveEditFormVisible"/>.
        /// </summary>
        public static readonly DependencyProperty EditFormVisibleProperty =
            DependencyProperty.Register(
                nameof(EditFormVisible),
                typeof(bool?),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public bool? EditFormVisible
        {
            get => (bool?)GetValue(EditFormVisibleProperty);
            set => SetValue(EditFormVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the caption shown beside this field's editor in the edit form. When unset,
        /// the form falls back to the column's <see cref="ColumnLayoutBase.HeaderCaption"/>.
        /// Resolved by <see cref="ResolveEffectiveEditFormCaption"/>.
        /// </summary>
        public static readonly DependencyProperty EditFormCaptionProperty =
            DependencyProperty.Register(
                nameof(EditFormCaption),
                typeof(string),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public string EditFormCaption
        {
            get => (string)GetValue(EditFormCaptionProperty);
            set => SetValue(EditFormCaptionProperty, value);
        }

        /// <summary>
        /// Gets or sets how many layout columns this field's editor spans in the auto-generated
        /// edit form. Default <c>1</c>. Honored only by the auto layout — a custom
        /// <see cref="SearchDataGrid.EditFormTemplate"/> places editors itself.
        /// </summary>
        public static readonly DependencyProperty EditFormColumnSpanProperty =
            DependencyProperty.Register(
                nameof(EditFormColumnSpan),
                typeof(int),
                typeof(ColumnDataBase),
                new PropertyMetadata(1));

        public int EditFormColumnSpan
        {
            get => (int)GetValue(EditFormColumnSpanProperty);
            set => SetValue(EditFormColumnSpanProperty, value);
        }

        /// <summary>
        /// Gets or sets how many layout rows this field's editor spans in the auto-generated edit
        /// form. Default <c>1</c>. Honored only by the auto layout.
        /// </summary>
        public static readonly DependencyProperty EditFormRowSpanProperty =
            DependencyProperty.Register(
                nameof(EditFormRowSpan),
                typeof(int),
                typeof(ColumnDataBase),
                new PropertyMetadata(1));

        public int EditFormRowSpan
        {
            get => (int)GetValue(EditFormRowSpanProperty);
            set => SetValue(EditFormRowSpanProperty, value);
        }

        /// <summary>
        /// Optional per-field editor template used in the edit form, overriding the column's
        /// normal cell edit template for the form only. When unset, the form reuses the column's
        /// effective cell edit template (see <see cref="ResolveEffectiveEditFormCellTemplate"/>),
        /// so the in-grid editor and the form editor stay in sync by default.
        /// </summary>
        public static readonly DependencyProperty EditFormCellTemplateProperty =
            DependencyProperty.Register(
                nameof(EditFormCellTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public DataTemplate EditFormCellTemplate
        {
            get => (DataTemplate)GetValue(EditFormCellTemplateProperty);
            set => SetValue(EditFormCellTemplateProperty, value);
        }

        /// <summary>
        /// Resolves whether this column appears in the edit form: the explicit
        /// <see cref="EditFormVisible"/> when set, otherwise the column's
        /// <see cref="ColumnLayoutBase.Visible"/> state.
        /// </summary>
        internal bool ResolveEffectiveEditFormVisible() => EditFormVisible ?? Visible;

        /// <summary>
        /// Resolves the edit-form caption: <see cref="EditFormCaption"/> when set, otherwise the
        /// column's <see cref="ColumnLayoutBase.HeaderCaption"/>.
        /// </summary>
        internal string ResolveEffectiveEditFormCaption()
            => !string.IsNullOrEmpty(EditFormCaption) ? EditFormCaption : HeaderCaption;

        /// <summary>
        /// Resolves the edit-form editor template for this field: <see cref="EditFormCellTemplate"/>
        /// when set, otherwise the column's effective cell edit template
        /// (<see cref="ResolveEffectiveCellEditTemplate"/>). Read-only columns fall back to the
        /// display template at the presenter level.
        /// </summary>
        internal DataTemplate ResolveEffectiveEditFormCellTemplate()
            => EditFormCellTemplate ?? ResolveEffectiveCellEditTemplate();

        #endregion

        #region Validation

        /// <summary>
        /// Gets or sets a column-level override for whether data-annotation validation errors are
        /// surfaced while editing this column. <c>null</c> (the default) inherits the grid's
        /// <see cref="SearchDataGrid.ShowValidationAttributeErrors"/>; <c>true</c> / <c>false</c>
        /// overrides it for this column. <see cref="ActualShowValidationAttributeErrors"/> exposes
        /// the resolved value.
        /// </summary>
        public static readonly DependencyProperty ShowValidationAttributeErrorsProperty =
            DependencyProperty.Register(
                nameof(ShowValidationAttributeErrors),
                typeof(bool?),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnShowValidationAttributeErrorsChanged));

        public bool? ShowValidationAttributeErrors
        {
            get => (bool?)GetValue(ShowValidationAttributeErrorsProperty);
            set => SetValue(ShowValidationAttributeErrorsProperty, value);
        }

        private static void OnShowValidationAttributeErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnDataBase col)
                col.RefreshActualShowValidationAttributeErrors();
        }

        private static readonly DependencyPropertyKey ActualShowValidationAttributeErrorsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualShowValidationAttributeErrors),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualShowValidationAttributeErrorsProperty = ActualShowValidationAttributeErrorsPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved value of <see cref="ShowValidationAttributeErrors"/>: the column override when
        /// set, otherwise the grid's <see cref="SearchDataGrid.ShowValidationAttributeErrors"/>,
        /// otherwise <c>true</c>. The cell editor's data-annotation validation rule reads this to
        /// decide whether to report errors.
        /// </summary>
        public bool ActualShowValidationAttributeErrors => (bool)GetValue(ActualShowValidationAttributeErrorsProperty);

        /// <summary>
        /// Re-resolves <see cref="ActualShowValidationAttributeErrors"/> from the column override
        /// and the owning grid. Called when the override changes, when the grid-level default
        /// changes, and when the column attaches to a grid.
        /// </summary>
        internal void RefreshActualShowValidationAttributeErrors()
        {
            bool resolved = ShowValidationAttributeErrors ?? View?.ShowValidationAttributeErrors ?? true;
            SetValue(ActualShowValidationAttributeErrorsPropertyKey, resolved);
        }

        /// <summary>
        /// True when the bound property carries at least one
        /// <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/>. Set by
        /// <see cref="SmartColumnConfigurator"/> during smart configuration. Gates the display-mode
        /// error badge (<see cref="ValidationErrorIcon"/>) so only columns that can actually report
        /// an error pay for the overlay.
        /// </summary>
        internal bool HasValidationAttributes { get; set; }

        /// <summary>
        /// True when the bound row type implements <see cref="System.ComponentModel.INotifyDataErrorInfo"/>.
        /// Set by <see cref="SmartColumnConfigurator"/> during smart configuration. Unlike
        /// <see cref="HasValidationAttributes"/> (a static, per-property fact), a self-reporting model
        /// can raise an error on any property at runtime, so this gates the badge overlay on for every
        /// such column rather than only those with attributes.
        /// </summary>
        internal bool RowImplementsDataErrorInfo { get; set; }

        /// <summary>
        /// True when this column should carry the validation badge overlay — either the property has
        /// data-annotation attributes or the row self-reports errors via
        /// <see cref="System.ComponentModel.INotifyDataErrorInfo"/>.
        /// </summary>
        private bool SupportsValidation => HasValidationAttributes || RowImplementsDataErrorInfo;

        #endregion

        #region Cell Templating & Styling

        /// <summary>
        /// Gets or sets the column-level <see cref="Style"/> applied to the generated
        /// <see cref="DataGridCell"/>. <see cref="ActualCellStyle"/> exposes the resolved value
        /// (column override falls back to <see cref="SearchDataGrid.CellStyle"/>).
        /// </summary>
        public static readonly DependencyProperty CellStyleProperty =
            DependencyProperty.Register(
                nameof(CellStyle),
                typeof(Style),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public Style CellStyle
        {
            get => (Style)GetValue(CellStyleProperty);
            set => SetValue(CellStyleProperty, value);
        }

        private static readonly DependencyPropertyKey ActualCellStylePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualCellStyle),
                typeof(Style),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualCellStyleProperty = ActualCellStylePropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved cell style — <see cref="CellStyle"/> when set, otherwise
        /// <see cref="SearchDataGrid.CellStyle"/> from the owning grid. The grid writes this
        /// onto the generated <see cref="DataGridColumn.CellStyle"/>; for template columns the
        /// value is wrapped by <see cref="ResolveEffectiveCellStyle"/> so editors (ComboBox/DatePicker/
        /// TextBox) stretch to fill the cell.
        /// </summary>
        public Style ActualCellStyle => (Style)GetValue(ActualCellStyleProperty);

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used as a fallback cell template. When
        /// <see cref="CellDisplayTemplate"/> is set it wins; otherwise this is the display
        /// template. Setting <i>any</i> cell template DP forces the grid to generate a
        /// <see cref="DataGridTemplateColumn"/> even when no <see cref="EditSettings"/> is
        /// configured.
        /// </summary>
        public static readonly DependencyProperty CellTemplateProperty =
            DependencyProperty.Register(
                nameof(CellTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplate CellTemplate
        {
            get => (DataTemplate)GetValue(CellTemplateProperty);
            set => SetValue(CellTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the fallback <see cref="DataTemplateSelector"/> for cell display.
        /// <see cref="CellDisplayTemplateSelector"/> wins when set.
        /// </summary>
        public static readonly DependencyProperty CellTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(CellTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplateSelector CellTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(CellTemplateSelectorProperty);
            set => SetValue(CellTemplateSelectorProperty, value);
        }

        private static readonly DependencyPropertyKey ActualCellTemplateSelectorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualCellTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualCellTemplateSelectorProperty = ActualCellTemplateSelectorPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved fallback cell template selector. Mirrors <see cref="CellTemplateSelector"/>
        /// today; reserved for future grid-level default resolution.
        /// </summary>
        public DataTemplateSelector ActualCellTemplateSelector => (DataTemplateSelector)GetValue(ActualCellTemplateSelectorProperty);

        /// <summary>
        /// Gets or sets the explicit display-mode <see cref="DataTemplate"/>. Wins over
        /// <see cref="CellTemplate"/> when both are set. Pairs with <see cref="CellEditTemplate"/>
        /// to split display and edit appearances.
        /// </summary>
        public static readonly DependencyProperty CellDisplayTemplateProperty =
            DependencyProperty.Register(
                nameof(CellDisplayTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplate CellDisplayTemplate
        {
            get => (DataTemplate)GetValue(CellDisplayTemplateProperty);
            set => SetValue(CellDisplayTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the explicit display-mode <see cref="DataTemplateSelector"/>. Wins over
        /// <see cref="CellTemplateSelector"/> when both are set.
        /// </summary>
        public static readonly DependencyProperty CellDisplayTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(CellDisplayTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplateSelector CellDisplayTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(CellDisplayTemplateSelectorProperty);
            set => SetValue(CellDisplayTemplateSelectorProperty, value);
        }

        private static readonly DependencyPropertyKey ActualCellDisplayTemplateSelectorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualCellDisplayTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualCellDisplayTemplateSelectorProperty = ActualCellDisplayTemplateSelectorPropertyKey.DependencyProperty;

        public DataTemplateSelector ActualCellDisplayTemplateSelector => (DataTemplateSelector)GetValue(ActualCellDisplayTemplateSelectorProperty);

        /// <summary>
        /// Gets or sets the explicit edit-mode <see cref="DataTemplate"/>. Wins over
        /// <see cref="EditSettings"/>'s generated edit template when set.
        /// </summary>
        public static readonly DependencyProperty CellEditTemplateProperty =
            DependencyProperty.Register(
                nameof(CellEditTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplate CellEditTemplate
        {
            get => (DataTemplate)GetValue(CellEditTemplateProperty);
            set => SetValue(CellEditTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the explicit edit-mode <see cref="DataTemplateSelector"/>.
        /// </summary>
        public static readonly DependencyProperty CellEditTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(CellEditTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplateSelector CellEditTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(CellEditTemplateSelectorProperty);
            set => SetValue(CellEditTemplateSelectorProperty, value);
        }

        private static readonly DependencyPropertyKey ActualCellEditTemplateSelectorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualCellEditTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualCellEditTemplateSelectorProperty = ActualCellEditTemplateSelectorPropertyKey.DependencyProperty;

        public DataTemplateSelector ActualCellEditTemplateSelector => (DataTemplateSelector)GetValue(ActualCellEditTemplateSelectorProperty);

        private static void OnCellTemplatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.RefreshActualCellTemplating();
            col.SyncToInternalColumn();
        }

        /// <summary>
        /// Recomputes the <c>Actual*</c> cell-templating DPs from the source DPs and the
        /// currently-attached <see cref="ColumnLayoutBase.View"/>. Called on every change to the
        /// source DPs and whenever the grid attaches.
        /// </summary>
        private void RefreshActualCellTemplating()
        {
            SetValue(ActualCellStylePropertyKey, CellStyle ?? View?.CellStyle);
            SetValue(ActualCellTemplateSelectorPropertyKey, CellTemplateSelector);
            SetValue(ActualCellDisplayTemplateSelectorPropertyKey, CellDisplayTemplateSelector);
            SetValue(ActualCellEditTemplateSelectorPropertyKey, CellEditTemplateSelector);
            SetValue(ActualCellToolTipTemplatePropertyKey, CellToolTipTemplate);
        }

        /// <inheritdoc/>
        protected override void OnViewChanged()
        {
            // ActualCellStyle's grid-default fallback depends on View — refresh on attach.
            RefreshActualCellTemplating();
            // ActualShowValidationAttributeErrors falls back to the grid's value — refresh on attach.
            RefreshActualShowValidationAttributeErrors();
            // ActualShowCheckBoxInHeader depends on InternalColumn / SearchTemplateController,
            // both of which can flip after View is wired. Re-resolve so the header chrome
            // shows/hides correctly on first paint.
            RefreshActualShowCheckBoxInHeader();
            // Push the initial IsChecked snapshot once the grid is reachable.
            View?.RefreshSelectAllHeader(this);
            // Focus/nav projections — seed on attach so the first cell-style build picks them up.
            RefreshActualFocusNav();
            // ActualEnableLiveFiltering falls back to the grid's value — resolve on attach.
            RefreshActualEnableLiveFiltering();
        }

        /// <summary>
        /// Resolves the effective display-mode cell template. Precedence:
        /// <see cref="CellDisplayTemplate"/> &gt; <see cref="CellTemplate"/> &gt;
        /// <see cref="EditSettings"/>'s resolved display template (which honors
        /// <c>EditSettings.DisplayTemplate</c> over the editor's code-built default).
        /// </summary>
        internal DataTemplate ResolveEffectiveCellDisplayTemplate()
            => CellDisplayTemplate ?? CellTemplate ?? EditSettings?.ResolveDisplayTemplate(this);

        /// <summary>
        /// Resolved display-mode selector — <see cref="ActualCellDisplayTemplateSelector"/>
        /// when set, otherwise <see cref="ActualCellTemplateSelector"/>.
        /// </summary>
        internal DataTemplateSelector ResolveEffectiveCellDisplayTemplateSelector()
            => ActualCellDisplayTemplateSelector ?? ActualCellTemplateSelector;

        /// <summary>
        /// Resolves the effective edit-mode cell template. Precedence:
        /// <see cref="CellEditTemplate"/> &gt; <see cref="EditSettings"/>'s resolved edit
        /// template.
        /// </summary>
        internal DataTemplate ResolveEffectiveCellEditTemplate()
            => CellEditTemplate ?? EditSettings?.ResolveEditTemplate(this);

        /// <summary>
        /// True when any user-supplied cell template / selector is set. Used by
        /// <see cref="CreateDataGridColumn"/> to force a <see cref="DataGridTemplateColumn"/>
        /// even when no <see cref="EditSettings"/> is present, and by the best-fit engine to
        /// detect columns whose content can't be text-measured.
        /// </summary>
        internal bool HasUserCellTemplate =>
            CellDisplayTemplate != null
            || CellTemplate != null
            || CellEditTemplate != null
            || CellTemplateSelector != null
            || CellDisplayTemplateSelector != null
            || CellEditTemplateSelector != null;

        /// <summary>
        /// Gets or sets the binding evaluated per-row to produce the cell's tooltip content.
        /// When <see cref="CellToolTipTemplate"/> is also set, the binding feeds the templated
        /// <see cref="ToolTip"/>'s <c>Content</c>; without a template, the binding's value is
        /// the tooltip directly.
        /// </summary>
        public static readonly DependencyProperty CellToolTipBindingProperty =
            DependencyProperty.Register(
                nameof(CellToolTipBinding),
                typeof(BindingBase),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public BindingBase CellToolTipBinding
        {
            get => (BindingBase)GetValue(CellToolTipBindingProperty);
            set => SetValue(CellToolTipBindingProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> applied to the cell tooltip. The
        /// template's <c>DataContext</c> is the resolved <see cref="CellToolTipBinding"/> value
        /// if a binding is set; otherwise the inherited cell <c>DataContext</c> (typically the
        /// row item), so templates can bind directly to row properties.
        /// </summary>
        public static readonly DependencyProperty CellToolTipTemplateProperty =
            DependencyProperty.Register(
                nameof(CellToolTipTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null, OnCellTemplatingChanged));

        public DataTemplate CellToolTipTemplate
        {
            get => (DataTemplate)GetValue(CellToolTipTemplateProperty);
            set => SetValue(CellToolTipTemplateProperty, value);
        }

        private static readonly DependencyPropertyKey ActualCellToolTipTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualCellToolTipTemplate),
                typeof(DataTemplate),
                typeof(ColumnDataBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualCellToolTipTemplateProperty = ActualCellToolTipTemplatePropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved cell tooltip template. Mirrors <see cref="CellToolTipTemplate"/> today;
        /// reserved for future grid-level default resolution.
        /// </summary>
        public DataTemplate ActualCellToolTipTemplate => (DataTemplate)GetValue(ActualCellToolTipTemplateProperty);

        private bool HasCellToolTip => CellToolTipBinding != null || CellToolTipTemplate != null;

        /// <summary>
        /// Builds the cell style applied to the generated <see cref="DataGridColumn.CellStyle"/>.
        /// Conditionally wraps <paramref name="basedOn"/> with stretching content alignment
        /// (template columns only — editors like ComboBox/DatePicker shrink to content size
        /// without it) and a <see cref="FrameworkElement.ToolTipProperty"/> setter when
        /// <see cref="CellToolTipBinding"/> or <see cref="CellToolTipTemplate"/> is set.
        /// Returns <paramref name="basedOn"/> unchanged (which may be <c>null</c>) when neither
        /// wrap is required.
        /// </summary>
        private Style ResolveEffectiveCellStyle(Style basedOn, bool stretching)
        {
            bool needsTooltip = HasCellToolTip;
            bool needsIsEnabled = IsEnabledBinding != null;
            // AllowFocus/TabStop differ from defaults => need cell-style setters.
            bool needsFocusable = !ActualAllowFocus;
            bool needsTabStop = !ActualTabStop;
            if (!stretching && !needsTooltip && !needsIsEnabled && !needsFocusable && !needsTabStop)
                return basedOn;

            var style = new Style(typeof(DataGridCell), basedOn);

            if (stretching)
            {
                style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
                style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
            }

            if (needsTooltip)
                style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, BuildCellToolTipValue()));

            if (needsIsEnabled)
                style.Setters.Add(new Setter(UIElement.IsEnabledProperty, IsEnabledBinding));

            if (needsFocusable)
                style.Setters.Add(new Setter(UIElement.FocusableProperty, false));

            if (needsTabStop)
                style.Setters.Add(new Setter(KeyboardNavigation.IsTabStopProperty, false));

            return style;
        }

        /// <summary>
        /// Resolves the value pushed to <see cref="FrameworkElement.ToolTipProperty"/> on the
        /// cell style. Three cases:
        /// <list type="bullet">
        /// <item>Binding alone → setter value is the binding itself (WPF evaluates it per-cell
        /// against the row item).</item>
        /// <item>Template alone → setter value is a <see cref="ToolTip"/> with the template;
        /// <c>Content</c> binds to <c>{Binding}</c> so each cell's row item flows into the
        /// template.</item>
        /// <item>Binding + template → templated <see cref="ToolTip"/>; <c>Content</c> uses the
        /// user binding.</item>
        /// </list>
        /// </summary>
        private object BuildCellToolTipValue()
        {
            if (CellToolTipTemplate == null)
                return CellToolTipBinding;

            var tooltip = new ToolTip { ContentTemplate = CellToolTipTemplate };
            BindingOperations.SetBinding(
                tooltip,
                ContentControl.ContentProperty,
                CellToolTipBinding ?? new Binding());
            return tooltip;
        }

        #endregion

        #region Select All

        /// <summary>
        /// Gets or sets whether this column shows a tri-state checkbox in its header. When
        /// <c>true</c> and the column is boolean-typed, the checkbox surfaces in the header
        /// chrome — toggling it pushes the new value into every row in <see cref="SelectAllScope"/>.
        /// The effective visibility is <see cref="ActualShowCheckBoxInHeader"/>, which ANDs
        /// this flag with the column's boolean-type detection.
        /// </summary>
        public static readonly DependencyProperty ShowCheckBoxInHeaderProperty =
            DependencyProperty.Register(
                nameof(ShowCheckBoxInHeader),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false, OnShowCheckBoxInHeaderChanged));

        public bool ShowCheckBoxInHeader
        {
            get => (bool)GetValue(ShowCheckBoxInHeaderProperty);
            set => SetValue(ShowCheckBoxInHeaderProperty, value);
        }

        private static readonly DependencyPropertyKey ActualShowCheckBoxInHeaderPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualShowCheckBoxInHeader),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ActualShowCheckBoxInHeaderProperty = ActualShowCheckBoxInHeaderPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="ShowCheckBoxInHeader"/>. <c>true</c> only when
        /// <see cref="ShowCheckBoxInHeader"/> is set AND the column is detected as boolean
        /// (<see cref="FieldType"/> is <c>bool</c>/<c>bool?</c>, the generated column is a
        /// <see cref="DataGridCheckBoxColumn"/>, <see cref="UseCheckBoxInSearchBox"/> is set,
        /// or the runtime <see cref="SearchTemplateController"/> reports boolean data). Bound
        /// by the header template to gate checkbox visibility.
        /// </summary>
        public bool ActualShowCheckBoxInHeader => (bool)GetValue(ActualShowCheckBoxInHeaderProperty);

        internal void SetActualShowCheckBoxInHeader(bool value)
            => SetValue(ActualShowCheckBoxInHeaderPropertyKey, value);

        /// <summary>
        /// Gets or sets the tri-state value of the header checkbox. Two-way bindable: writes
        /// from the header checkbox (or from consumer code) push the new value into every row
        /// in <see cref="SelectAllScope"/>; the grid pushes the resolved state back via
        /// <see cref="SetIsCheckedFromGrid"/> after each mutation, after a filter change, and
        /// after a selection change so the UI reflects mixed/uniform state without further
        /// wiring.
        /// <para>
        /// <c>true</c> = all non-null values in scope are <c>true</c>; <c>false</c> = all are
        /// <c>false</c>; <c>null</c> = mixed or all-null (indeterminate). Writes of <c>null</c>
        /// are treated as no-ops on the data — the column header simply displays the
        /// indeterminate visual.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool?),
                typeof(ColumnDataBase),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsCheckedChanged));

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        // Guards the IsChecked DP callback while the grid pushes the resolved state back to
        // the descriptor — avoids re-triggering ApplyHeaderCheckedValue from a refresh write.
        private bool _suppressIsCheckedApply;

        /// <summary>
        /// Pushes a refreshed <see cref="IsChecked"/> value from the grid without re-entering
        /// the apply-to-rows path. Used by <see cref="SearchDataGrid"/> after any operation
        /// that may change the resolved state.
        /// </summary>
        internal void SetIsCheckedFromGrid(bool? value)
        {
            if (_suppressIsCheckedApply) return;
            _suppressIsCheckedApply = true;
            try { SetValue(IsCheckedProperty, value); }
            finally { _suppressIsCheckedApply = false; }
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            if (col._suppressIsCheckedApply) return;
            if (col.View == null) return;

            // Null write = consumer can't push "indeterminate" onto rows. Mirror the WPF
            // CheckBox behavior: indeterminate is a display-only state.
            if (e.NewValue is not bool target) return;
            col.View.ApplyHeaderCheckedValue(col, target);
        }

        /// <summary>
        /// Gets or sets the scope of items affected by header-checkbox writes. Has no spec
        /// equivalent in the 120-property reference surface — kept as-is because it carries
        /// real consumer-facing behavior (filtered vs. selected vs. unfiltered universe) that
        /// the spec <see cref="IsChecked"/> alone can't express.
        /// </summary>
        public static readonly DependencyProperty SelectAllScopeProperty =
            DependencyProperty.Register(
                nameof(SelectAllScope),
                typeof(SelectAllScope),
                typeof(ColumnDataBase),
                new PropertyMetadata(SelectAllScope.FilteredRows, OnSelectAllScopeChanged));

        public SelectAllScope SelectAllScope
        {
            get => (SelectAllScope)GetValue(SelectAllScopeProperty);
            set => SetValue(SelectAllScopeProperty, value);
        }

        /// <summary>
        /// Recomputes <see cref="ActualShowCheckBoxInHeader"/> from
        /// <see cref="ShowCheckBoxInHeader"/> and the column's boolean-type detection. Called
        /// from the source DP callbacks and on grid attach.
        /// </summary>
        internal void RefreshActualShowCheckBoxInHeader()
        {
            SetActualShowCheckBoxInHeader(ShowCheckBoxInHeader && IsBooleanColumn());
        }

        /// <summary>
        /// Mirrors <c>SearchDataGrid.IsColumnBooleanType</c>: a column counts as boolean when
        /// it generates a <see cref="DataGridCheckBoxColumn"/>, its <see cref="FieldType"/> is
        /// <c>bool</c>/<c>bool?</c>, <see cref="UseCheckBoxInSearchBox"/> is forced on, or the
        /// runtime <see cref="WWControls.Core.SearchTemplateController"/> has
        /// detected boolean data on the bound source.
        /// </summary>
        private bool IsBooleanColumn()
        {
            if (InternalColumn is DataGridCheckBoxColumn) return true;
            if (FieldType == typeof(bool) || FieldType == typeof(bool?)) return true;
            if (UseCheckBoxInSearchBox) return true;
            if (SearchTemplateController != null
                && SearchTemplateController.ColumnDataType == Core.ColumnDataType.Boolean)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Focus & Navigation 

        /// <summary>
        /// Gets or sets whether cells in this column can receive keyboard focus. When
        /// <c>false</c>, Tab/Shift+Tab AND arrow keys skip the column, mouse clicks on cells
        /// do not focus them, and edit-mode entry (F2 / double-click / type-to-edit) is
        /// suppressed. Distinct from <see cref="IsReadOnly"/>, which only blocks the write —
        /// a read-only cell can still be focused (for selection, copy, sort participation).
        /// <para>
        /// Drives the cell-style <see cref="UIElement.FocusableProperty"/> setter: when
        /// <c>false</c>, the cell is non-focusable and native WPF skips it for all input.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty AllowFocusProperty =
            DependencyProperty.Register(
                nameof(AllowFocus),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnFocusNavPropertyChanged));

        public bool AllowFocus
        {
            get => (bool)GetValue(AllowFocusProperty);
            set => SetValue(AllowFocusProperty, value);
        }

        private static readonly DependencyPropertyKey ActualAllowFocusPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowFocus),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualAllowFocusProperty = ActualAllowFocusPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="AllowFocus"/>. Mirrors the column-level value today; reserved
        /// for future grid-level default resolution (matching the <c>AllowFilterPopup</c>
        /// pattern).
        /// </summary>
        public bool ActualAllowFocus => (bool)GetValue(ActualAllowFocusProperty);

        /// <summary>
        /// Gets or sets whether Tab / Shift+Tab traversal stops on cells in this column. When
        /// <c>false</c>, Tab skips the column but arrow keys still reach it (the "skip in Tab
        /// order but cell is still selectable" case). Combine with
        /// <see cref="AllowFocus"/>=<c>false</c> to make a column entirely keyboard-inert.
        /// <para>
        /// Drives the cell-style <see cref="KeyboardNavigation.IsTabStopProperty"/> setter.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty TabStopProperty =
            DependencyProperty.Register(
                nameof(TabStop),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true, OnFocusNavPropertyChanged));

        public bool TabStop
        {
            get => (bool)GetValue(TabStopProperty);
            set => SetValue(TabStopProperty, value);
        }

        private static readonly DependencyPropertyKey ActualTabStopPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualTabStop),
                typeof(bool),
                typeof(ColumnDataBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualTabStopProperty = ActualTabStopPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved <see cref="TabStop"/>. Mirrors the column-level value today.
        /// </summary>
        public bool ActualTabStop => (bool)GetValue(ActualTabStopProperty);

        /// <summary>
        /// Gets or sets a custom Tab-traversal order for this column, independent of its
        /// visual <see cref="ColumnLayoutBase.DisplayIndex"/>. Default <c>-1</c> means "use
        /// the natural display order." When at least one column in the grid sets a
        /// non-default value, Tab visits columns with explicit indices first (ascending),
        /// then everything else in display order. Same column resolution is used by the data
        /// row Tab handler and by <c>FilterRowNavigator</c>, so a single configuration
        /// governs both rows.
        /// </summary>
        /// <remarks>
        /// Mirrors the WPF <see cref="KeyboardNavigation.TabIndexProperty"/> convention
        /// (lower wins, default = "no opinion"), but lives on the column descriptor instead
        /// of the cell — WPF's built-in DataGrid Tab handler walks cells by
        /// <c>DisplayIndex</c> and ignores cell-level <c>TabIndex</c>, so we route Tab
        /// through <c>SearchDataGrid.TryHandleNavigationIndexTab</c> when this DP is set.
        /// </remarks>
        public static readonly DependencyProperty NavigationIndexProperty =
            DependencyProperty.Register(
                nameof(NavigationIndex),
                typeof(int),
                typeof(ColumnDataBase),
                new PropertyMetadata(-1));

        public int NavigationIndex
        {
            get => (int)GetValue(NavigationIndexProperty);
            set => SetValue(NavigationIndexProperty, value);
        }

        /// <summary>
        /// Recomputes the focus / navigation <c>Actual*</c> projections from their source
        /// DPs. Called from <see cref="OnFocusNavPropertyChanged"/> and on grid attach.
        /// </summary>
        internal void RefreshActualFocusNav()
        {
            SetValue(ActualAllowFocusPropertyKey, AllowFocus);
            SetValue(ActualTabStopPropertyKey, TabStop);
        }

        private static void OnFocusNavPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.RefreshActualFocusNav();
            // Cell style carries Focusable / IsTabStop setters; re-resolve so the change
            // takes effect on the generated cells without a full template rebuild.
            col.SyncToInternalColumn();
            // Filter cell consults the descriptor directly via ColumnFilterControl —
            // ask the matching host to re-evaluate.
            if (col.View != null)
            {
                var host = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn) as ColumnFilterControl;
                host?.RefreshFocusNav();
            }
        }

        #endregion

        #region Internal State

        private WWControls.Core.SearchTemplateController _searchTemplateController;

        /// <summary>
        /// The persistent <see cref="WWControls.Core.SearchTemplateController"/> for this column.
        /// Stored on the descriptor (not the <see cref="ColumnSearchBox"/>) so that filter state survives
        /// horizontal column virtualization — when the header scrolls out and back, the new
        /// <see cref="ColumnSearchBox"/> instance reconnects to the same controller instead of starting empty.
        /// The setter (re)subscribes to <see cref="WWControls.Core.SearchTemplateController.PropertyChanged"/>
        /// so the descriptor's <see cref="IsFiltered"/> DP stays in sync with the controller's
        /// <c>HasCustomExpression</c> independent of whether a runtime <see cref="ColumnFilterControl"/>
        /// is materialized.
        /// </summary>
        internal WWControls.Core.SearchTemplateController SearchTemplateController
        {
            get => _searchTemplateController;
            set
            {
                if (ReferenceEquals(_searchTemplateController, value)) return;

                if (_searchTemplateController != null)
                    _searchTemplateController.PropertyChanged -= OnSearchTemplateControllerPropertyChanged;

                _searchTemplateController = value;

                if (_searchTemplateController != null)
                    _searchTemplateController.PropertyChanged += OnSearchTemplateControllerPropertyChanged;

                SetIsFiltered(_searchTemplateController?.HasCustomExpression == true);
            }
        }

        private void OnSearchTemplateControllerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WWControls.Core.SearchTemplateController.HasCustomExpression)
                || string.IsNullOrEmpty(e.PropertyName))
            {
                SetIsFiltered(_searchTemplateController?.HasCustomExpression == true);
            }
        }

        #endregion

        #region Header Resolution

        /// <inheritdoc/>
        protected override void RefreshHeaderCaption()
        {
            string baseCaption = ResolveHeaderCaption();
            if (!string.IsNullOrEmpty(baseCaption))
            {
                SetHeaderCaption(baseCaption);
                return;
            }
            SetHeaderCaption(FieldName ?? string.Empty);
        }

        /// <inheritdoc/>
        protected override object EffectiveHeader => Header ?? FieldName;

        /// <inheritdoc/>
        protected override void SyncDerivedToInternalColumn(DataGridColumn col)
        {
            col.IsReadOnly = GetEffectiveReadOnly();
            col.CanUserSort = ActualAllowSorting;
            col.SortMemberPath = SortMemberPath ?? FieldName;

            // Template columns wrap CellStyle with the stretching style + optional tooltip setter
            // so editor controls fill the cell. Text / checkbox columns get the tooltip setter
            // only when needed. Null-valued template / selector properties are cleared rather
            // than assigned literal null so WPF's column → grid transfer-property coercion
            // (e.g. DataGridColumn.CellStyle → DataGrid.CellStyle) still falls back.
            if (col is DataGridTemplateColumn tplCol)
            {
                SetOrClear(tplCol, DataGridTemplateColumn.CellStyleProperty,
                    ResolveEffectiveCellStyle(ActualCellStyle ?? View?.CellStyle, stretching: true));
                ApplyDisplayTemplate(tplCol);
                ApplyEditTemplate(tplCol);
            }
            else
            {
                SetOrClear(col, DataGridColumn.CellStyleProperty,
                    ResolveEffectiveCellStyle(ActualCellStyle, stretching: false));
            }
        }

        /// <summary>
        /// Assigns the display-mode cell template / selector to <paramref name="tplCol"/>, wrapping
        /// the template with a <see cref="ValidationErrorIcon"/> overlay when this column
        /// <see cref="SupportsValidation"/> (data-annotation attributes or a self-reporting row) and
        /// error display is enabled. Shared by
        /// <see cref="CreateDataGridColumn"/> and <see cref="SyncDerivedToInternalColumn"/> so the
        /// badge is applied on both initial generation and runtime template refresh.
        /// </summary>
        private void ApplyDisplayTemplate(DataGridTemplateColumn tplCol)
        {
            var displayTemplate = ResolveEffectiveCellDisplayTemplate();
            var displaySelector = ResolveEffectiveCellDisplayTemplateSelector();

            // Only wrap when the property can actually report an error, errors are on, there's a
            // concrete template (a selector path is left untouched), and a field to validate.
            if (displayTemplate != null && displaySelector == null
                && SupportsValidation && ActualShowValidationAttributeErrors
                && !string.IsNullOrEmpty(FieldName))
            {
                displayTemplate = BuildValidatingCellTemplate(displayTemplate, this);
            }

            SetOrClear(tplCol, DataGridTemplateColumn.CellTemplateProperty, displayTemplate);
            SetOrClear(tplCol, DataGridTemplateColumn.CellTemplateSelectorProperty, displaySelector);
        }

        /// <summary>
        /// Assigns the edit-mode cell template / selector to <paramref name="tplCol"/>, wrapping
        /// the template with the same <see cref="ValidationErrorIcon"/> overlay
        /// <see cref="ApplyDisplayTemplate"/> applies. Without this the badge would blink out the
        /// moment the cell entered edit mode (WPF swaps the display template for the edit template),
        /// so the error indicator has to live in both. Shared by <see cref="CreateDataGridColumn"/>
        /// and <see cref="SyncDerivedToInternalColumn"/> so the badge is applied on both initial
        /// generation and runtime template refresh.
        /// </summary>
        private void ApplyEditTemplate(DataGridTemplateColumn tplCol)
        {
            var editTemplate = ResolveEffectiveCellEditTemplate();
            var editSelector = ActualCellEditTemplateSelector;

            // Wrap on the same terms as the display template: concrete template (a selector path
            // is left untouched), the column supports validation, errors are on, and there's a
            // field to validate.
            if (editTemplate != null && editSelector == null
                && SupportsValidation && ActualShowValidationAttributeErrors
                && !string.IsNullOrEmpty(FieldName))
            {
                editTemplate = BuildValidatingCellTemplate(editTemplate, this);
            }

            SetOrClear(tplCol, DataGridTemplateColumn.CellEditingTemplateProperty, editTemplate);
            SetOrClear(tplCol, DataGridTemplateColumn.CellEditingTemplateSelectorProperty, editSelector);
        }

        /// <summary>
        /// Builds a display template that renders <paramref name="inner"/> through a
        /// <see cref="ValidationCellPresenter"/> so a <see cref="ValidationErrorIcon"/> badge sits
        /// beside the cell content (badge appears whenever the value fails its data-annotation
        /// attributes). The presenter's layout is themable via
        /// <see cref="ThemeKeys.ValidationCellPresenter"/>; this method only feeds it the per-column
        /// content, field name, and resolved <see cref="ActualShowValidationAttributeErrors"/>.
        /// </summary>
        private static DataTemplate BuildValidatingCellTemplate(DataTemplate inner, ColumnDataBase column)
        {
            var presenter = new FrameworkElementFactory(typeof(ValidationCellPresenter));
            presenter.SetValue(ValidationCellPresenter.PropertyNameProperty, column.FieldName);
            presenter.SetBinding(ValidationCellPresenter.IsValidationEnabledProperty,
                new Binding(nameof(ActualShowValidationAttributeErrors)) { Source = column });
            // Content = the row item (cell DataContext); ContentTemplate = the column's real
            // display template. The presenter's themed template overlays the badge.
            presenter.SetBinding(ContentControl.ContentProperty, new Binding());
            presenter.SetValue(ContentControl.ContentTemplateProperty, inner);
            return new DataTemplate { VisualTree = presenter };
        }

        #endregion

        #region Type-Based Auto-Configuration

        /// <summary>
        /// Returns true if the property was assigned explicitly (XAML, code, binding, style — anything
        /// other than the registered default) AND the assignment was not from our auto-config helpers.
        /// </summary>
        private bool IsExplicitlySet(DependencyProperty dp, bool isAutoFlag)
        {
            if (isAutoFlag) return false;
            var source = DependencyPropertyHelper.GetValueSource(this, dp);
            return source.BaseValueSource != BaseValueSource.Default;
        }

        /// <summary>
        /// Applies sensible defaults to filter/search properties based on <see cref="FieldType"/>.
        /// Skips properties the user has already set explicitly.
        /// </summary>
        /// <remarks>
        /// Mapping:
        /// <list type="bullet">
        /// <item><c>bool</c> / <c>bool?</c> → <see cref="UseCheckBoxInSearchBox"/> = <c>true</c>, <see cref="TextAlignment"/> = <see cref="System.Windows.TextAlignment.Center"/></item>
        /// <item><c>DateTime</c> / <c>DateTime?</c> → <see cref="DefaultSearchType"/> = <see cref="DefaultSearchType.Equals"/></item>
        /// <item>Enum types → <see cref="DefaultSearchType"/> = <see cref="DefaultSearchType.Equals"/></item>
        /// <item><c>string</c> → <see cref="DefaultSearchType"/> = <see cref="DefaultSearchType.StartsWith"/> (spec-aligned)</item>
        /// <item>Numeric types → <see cref="TextAlignment"/> = <see cref="System.Windows.TextAlignment.Right"/>, <see cref="DefaultSearchType"/> = <see cref="DefaultSearchType.Equals"/></item>
        /// </list>
        /// </remarks>
        internal void ApplyTypeBasedDefaults()
        {
            var type = FieldType;
            if (type == null) return;

            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool))
            {
                if (!IsUseCheckBoxInSearchBoxExplicit)
                    SetAutoUseCheckBoxInSearchBox(true);
                if (!IsTextAlignmentExplicit)
                    SetAutoTextAlignment(TextAlignment.Center);
                // CheckBoxEditSettings.GetSupportedFilterSearchTypes whitelists only Equals
                // (+ IsNull/IsNotNull when nullable). Without an Equals default,
                // ColumnFilterControl.UpdateEffectiveIsCellEnabled sees the registered
                // StartsWith default fall outside the whitelist and disables the entire
                // filter cell — which then propagates IsEnabled=false through the visual
                // tree to PART_FilterCheckBox, making the cycle checkbox look greyed-out
                // and uninteractive even though the grid / column is editable.
                if (!IsDefaultSearchTypeExplicit)
                    SetAutoDefaultSearchType(DefaultSearchType.Equals);
            }
            else if (underlying == typeof(DateTime))
            {
                if (!IsDefaultSearchTypeExplicit)
                    SetAutoDefaultSearchType(DefaultSearchType.Equals);
            }
            else if (underlying.IsEnum)
            {
                if (!IsDefaultSearchTypeExplicit)
                    SetAutoDefaultSearchType(DefaultSearchType.Equals);
                // TODO: populate the search dropdown from Enum.GetValues(underlying).
                // The current dropdown is data-driven via SetupColumnDataLazy; injecting a
                // static enum source needs a separate code path on SearchTemplateController.
            }
            else if (underlying == typeof(string))
            {
                // Spec-aligned: string columns default to StartsWith (prefix match), which
                // matches user expectations for free-text searches in a tabular UI. Redundant
                // with the registered default for now, but the explicit branch protects
                // against future default changes.
                if (!IsDefaultSearchTypeExplicit)
                    SetAutoDefaultSearchType(DefaultSearchType.StartsWith);
            }
            else if (IsNumericType(underlying))
            {
                // Spreadsheet convention: numbers right-align so decimal points line up across
                // a column.
                if (!IsTextAlignmentExplicit)
                    SetAutoTextAlignment(TextAlignment.Right);
                // Numeric columns can't meaningfully StartsWith / Contains, and the matching
                // EditSettings (auto-created TextEditSettings with MaskType=Numeric, or an
                // explicit SpinEditSettings) exposes only numeric operators. Without an Equals
                // default, ColumnFilterControl.UpdateEffectiveIsCellEnabled greys the
                // FilterRow cell because the registered StartsWith default isn't in the
                // numeric whitelist.
                if (!IsDefaultSearchTypeExplicit)
                    SetAutoDefaultSearchType(DefaultSearchType.Equals);
            }
            // decimal/double: no auto-format — DisplayStringFormat stays user-controlled.

            // Editor-shape override wins over CLR-type default. Runs after the type branches so
            // a ComboBoxEditSettings on a string-typed column ends up at Equals (whitelist-
            // compatible) instead of the StartsWith the string branch would otherwise pick.
            ApplyEditSettingsPreferredDefaults();
        }

        /// <summary>
        /// Applies <see cref="BaseEditSettings.GetPreferredDefaultSearchType"/> as the column's
        /// auto-default when the editor shape constrains the allowed operator set tighter than
        /// the CLR-type default. Skipped when the user has explicitly set
        /// <see cref="DefaultSearchType"/>. Called from both <see cref="ApplyTypeBasedDefaults"/>
        /// (covers FieldType-resolves-after-EditSettings) and <see cref="OnEditSettingsChanged"/>
        /// (covers EditSettings-set-after-FieldType).
        /// </summary>
        private void ApplyEditSettingsPreferredDefaults()
        {
            if (IsDefaultSearchTypeExplicit) return;
            var preferred = EditSettings?.GetPreferredDefaultSearchType();
            if (preferred.HasValue && preferred.Value != DefaultSearchType)
                SetAutoDefaultSearchType(preferred.Value);
        }

        private static bool IsNumericType(Type t)
        {
            if (t == null) return false;
            var inner = Nullable.GetUnderlyingType(t) ?? t;
            return inner == typeof(int) || inner == typeof(long) || inner == typeof(short)
                || inner == typeof(byte) || inner == typeof(sbyte) || inner == typeof(ushort)
                || inner == typeof(uint) || inner == typeof(ulong)
                || inner == typeof(double) || inner == typeof(float) || inner == typeof(decimal);
        }

        /// <summary>
        /// True when <paramref name="format"/> is a standard .NET numeric format the
        /// <c>NumericMaskFormatter</c> can interpret — currency (<c>C</c>), number (<c>N</c>),
        /// fixed-point (<c>F</c>), or percent (<c>P</c>), with optional precision. Other format
        /// strings (custom <c>#,##0.00</c>, hex <c>X</c>, exponential <c>E</c>, date patterns)
        /// fall through to the plain text-column path.
        /// </summary>
        private static bool IsNumericFormatString(string format)
        {
            if (string.IsNullOrEmpty(format)) return false;
            char first = char.ToUpperInvariant(format[0]);
            return first == 'C' || first == 'N' || first == 'F' || first == 'P';
        }

        #endregion

        #region DateTime Resolution

        /// <summary>
        /// Resolves the effective <see cref="RoundDateTime"/> for this column. Resolution order:
        /// <list type="number">
        ///   <item>Explicit <see cref="RoundDateTimeProperty"/> wins.</item>
        ///   <item>Effective display format strips time-of-day (no <c>H</c>/<c>h</c>/<c>m</c>/
        ///   <c>s</c>/<c>f</c>/<c>F</c>/<c>t</c>/<c>K</c>/<c>z</c> tokens) → <c>true</c>. A
        ///   column that visually shows just a date must filter on dates only; otherwise the
        ///   filter editor's midnight commit can't match rows whose backing timestamps carry
        ///   times the user can't see.</item>
        ///   <item>Fall back to data sampling — when no sampled value carries a non-zero time,
        ///   the editor is a date-only DatePicker and the filter rounds to <c>.Date</c>;
        ///   otherwise the editor exposes a time segment and the filter keeps the full instant.</item>
        /// </list>
        /// Called from <c>ColumnFilterControl</c> when it pushes the value into the column's
        /// <c>SearchTemplateController</c>.
        /// </summary>
        internal bool ResolveEffectiveRoundDateTime()
        {
            if (RoundDateTime.HasValue)
                return RoundDateTime.Value;

            string effectiveDisplay = ResolveEffectiveDisplayFormat();
            if (!string.IsNullOrEmpty(effectiveDisplay) && !DateTimeFormatHasTimeTokens(effectiveDisplay))
                return true;

            return !AnyBoundValueHasTimeOfDay(maxSamples: 200);
        }

        /// <summary>
        /// Returns the format string the cell uses to render its value, mirroring
        /// <see cref="Display.DisplayValueProviderFactory"/>'s priority chain so this
        /// resolver and the chip-display pipeline agree. Returns <c>null</c> when the column
        /// doesn't carry an opinion (display falls back to the editor's default short date).
        /// </summary>
        private string ResolveEffectiveDisplayFormat()
        {
            if (EditSettings is DateEditSettings dateSettings
                && dateSettings.UseMaskAsDisplayFormat
                && !string.IsNullOrEmpty(dateSettings.Mask))
                return dateSettings.Mask;
            if (EditSettings is TextEditSettings textSettings
                && textSettings.UseMaskAsDisplayFormat
                && !string.IsNullOrEmpty(textSettings.Mask))
                return textSettings.Mask;
            if (!string.IsNullOrEmpty(DisplayMask)) return DisplayMask;
            if (!string.IsNullOrEmpty(DisplayStringFormat)) return DisplayStringFormat;
            if (!string.IsNullOrEmpty(RoundDateDisplayFormat)) return RoundDateDisplayFormat;
            return null;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="format"/> contains tokens that render
        /// time-of-day content — hour (<c>H</c>/<c>h</c>), minute (<c>m</c>), second (<c>s</c>),
        /// fractional seconds (<c>f</c>/<c>F</c>), AM/PM (<c>t</c>), or timezone offset
        /// (<c>K</c>/<c>z</c>). Recognizes standard single-char codes ('t'/'T'/'f'/'F'/'g'/
        /// 'G'/'s'/'u'/'U'/'r'/'R'/'o'/'O') as time-bearing; everything else is treated as
        /// date-only. Quoted literals (<c>'text'</c>) and backslash-escaped chars are skipped.
        /// </summary>
        private static bool DateTimeFormatHasTimeTokens(string format)
        {
            if (string.IsNullOrEmpty(format)) return false;

            // Standard single-letter format codes resolve through culture patterns; classify
            // them up-front rather than running each one through DateTimeFormatInfo just to
            // re-scan the result.
            if (format.Length == 1)
            {
                switch (format[0])
                {
                    case 't':
                    case 'T':
                    case 'f':
                    case 'F':
                    case 'g':
                    case 'G':
                    case 's':
                    case 'u':
                    case 'U':
                    case 'r':
                    case 'R':
                    case 'o':
                    case 'O':
                        return true;
                    default:
                        return false;
                }
            }

            int i = 0;
            while (i < format.Length)
            {
                char c = format[i];

                if (c == '\\' && i + 1 < format.Length) { i += 2; continue; }
                if (c == '\'' || c == '"')
                {
                    int end = format.IndexOf(c, i + 1);
                    if (end < 0) return false;
                    i = end + 1;
                    continue;
                }

                // 'M' (uppercase) is month, NOT time — exclude. 'm' (lowercase) is minute.
                // 'F' (uppercase) as a custom specifier is fractional seconds (time-bearing).
                if (c == 'H' || c == 'h' || c == 'm' || c == 's' || c == 'f' || c == 'F'
                    || c == 't' || c == 'K' || c == 'z')
                    return true;

                i++;
            }
            return false;
        }

        /// <summary>
        /// Walks the parent grid's <c>ItemsSource</c> (up to <paramref name="maxSamples"/> items)
        /// looking for any DateTime value at <see cref="FieldName"/> with a non-zero time
        /// component. Bounded sampling keeps the cost stable on large collections; if items
        /// haven't been bound yet at column-generation time, returns <c>false</c> and the caller
        /// falls back to the date-only default.
        /// </summary>
        internal bool AnyBoundValueHasTimeOfDay(int maxSamples)
        {
            IEnumerable source = View?.ItemsSource;
            if (source == null || string.IsNullOrEmpty(FieldName)) return false;

            PropertyInfo prop = null;
            int sampled = 0;
            foreach (var item in source)
            {
                if (sampled++ >= maxSamples) break;
                if (item == null) continue;
                if (prop == null)
                {
                    prop = item.GetType().GetProperty(FieldName,
                        BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null) return false;
                }
                var value = prop.GetValue(item);
                DateTime? dt = value switch
                {
                    DateTime d => d,
                    _ => null,
                };
                if (dt.HasValue && dt.Value.TimeOfDay != TimeSpan.Zero)
                    return true;
            }
            return false;
        }

        #endregion

        #region Column Generation

        /// <summary>
        /// Creates the internal WPF <see cref="DataGridColumn"/> from this descriptor's properties.
        /// The generated column is stored in <see cref="ColumnLayoutBase.InternalColumn"/> and returned.
        /// </summary>
        /// <returns>The generated <see cref="DataGridColumn"/>.</returns>
        internal DataGridColumn CreateDataGridColumn()
        {
            if (string.IsNullOrEmpty(FieldName))
                return null;

            DataGridColumn column;

            // A user-supplied display setting (converter / format string / mask) signals "render
            // as text using my custom display." Honor that even for bool — DataGridCheckBoxColumn
            // ignores Binding.Converter/StringFormat, so we'd silently drop the user's intent.
            bool wantsCustomDisplay =
                DisplayValueConverter != null
                || !string.IsNullOrEmpty(DisplayStringFormat)
                || !string.IsNullOrEmpty(DisplayMask);

            bool isBoolField = FieldType == typeof(bool) || FieldType == typeof(bool?);

            // Auto-fill an EditSettings for fields that would otherwise fall through to plain
            // DataGridTextColumn / DataGridCheckBoxColumn so consumers get a styled, keyboard-aware
            // editor regardless of CLR type. Columns with a user-supplied display hint (converter /
            // string format / mask) also auto-fill — TextEditSettings.CreateDisplayTemplate applies
            // the converter / mask / format string through the same styled DisplayTextBlock the
            // numeric and date paths use, so all formatted columns get matching padding and
            // vertical centering.
            BaseEditSettings effectiveEditSettings = EditSettings ?? AutoCreateEditSettings(wantsCustomDisplay);

            // Surface auto-created settings on the descriptor so downstream readers — most
            // importantly ColumnFilterControl, which reads EditSettings to pick the
            // filter-row editor shape and the SearchTypeSelector operator whitelist — see the
            // same instance that's driving the cell templates. Without this, auto-created
            // settings remain invisible to the filter row and it defaults to a string-only
            // TextEditSettings whitelist. The assignment fires OnEditSettingsChanged, but its
            // template-rebuild branch is guarded by a non-null InternalColumn (still null at
            // this point), so the only side effect is DataContext propagation — desirable.
            if (EditSettings == null && effectiveEditSettings != null)
                EditSettings = effectiveEditSettings;

            // EditSettings OR any user-supplied cell template forces the DataGridTemplateColumn
            // path. User cell templates take precedence over the EditSettings-generated default
            // via Resolve* helpers; ClipboardContentBinding stays wired so copy/paste still uses
            // the raw value regardless of the template's visual layout.
            bool useTemplateColumn = effectiveEditSettings != null || HasUserCellTemplate;
            if (useTemplateColumn)
            {
                // Construct empty; the SetOrClear block below assigns / clears the nullable
                // templates and selectors. Initializing them in the object initializer with
                // null returns would block the column → grid transfer-property coercion the
                // same way the SyncToInternalColumn path used to.
                var tplCol = new ValidationSuppressingTemplateColumn
                {
                    ClipboardContentBinding = ResolveCellBinding(),
                };
                ApplyDisplayTemplate(tplCol);
                ApplyEditTemplate(tplCol);
                // Stretching wrap is required so editor controls (ComboBox/DatePicker/TextBox)
                // fill the cell instead of shrinking to content size. Tooltip setter is layered
                // in by ResolveEffectiveCellStyle when CellToolTipBinding/Template is set.
                SetOrClear(tplCol, DataGridTemplateColumn.CellStyleProperty,
                    ResolveEffectiveCellStyle(ActualCellStyle ?? View?.CellStyle, stretching: true));
                column = tplCol;
            }
            else if (isBoolField && !wantsCustomDisplay)
            {
                var checkBoxColumn = new DataGridCheckBoxColumn
                {
                    Binding = ResolveCellBinding()
                };
                ApplyAlignmentToCheckBoxColumn(checkBoxColumn);
                SetOrClear(checkBoxColumn, DataGridColumn.CellStyleProperty,
                    ResolveEffectiveCellStyle(ActualCellStyle, stretching: false));
                column = checkBoxColumn;
            }
            else
            {
                var textColumn = new DataGridTextColumn
                {
                    Binding = ResolveCellBinding()
                };
                ApplyAlignmentToTextColumn(textColumn);
                SetOrClear(textColumn, DataGridColumn.CellStyleProperty,
                    ResolveEffectiveCellStyle(ActualCellStyle, stretching: false));
                column = textColumn;
            }

            // Apply layout properties
            column.Header = Header ?? FieldName;
            column.Width = Width;
            column.MinWidth = MinWidth;
            column.MaxWidth = MaxWidth;
            column.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;
            column.IsReadOnly = GetEffectiveReadOnly();
            column.CanUserSort = ActualAllowSorting;
            column.CanUserResize = AllowResizing;
            column.CanUserReorder = AllowMoving;
            column.SortMemberPath = SortMemberPath ?? FieldName;
            // Header style / template / selector: use SetOrClear so a null descriptor value
            // leaves the column-level DP in its default state, letting WPF's transfer-property
            // coercion fall back to the grid's ColumnHeaderStyle / ColumnHeaderTemplate.
            SetOrClear(column, DataGridColumn.HeaderStyleProperty, ActualHeaderStyle);
            SetOrClear(column, DataGridColumn.HeaderTemplateProperty, HeaderTemplate);
            SetOrClear(column, DataGridColumn.HeaderTemplateSelectorProperty, ActualHeaderTemplateSelector);

            InternalColumn = column;
            // Pin a back-pointer on the WPF column so the header template can bind directly
            // to descriptor-side DPs (e.g. IsFiltered for the "filtered column" highlight)
            // without any visual-tree-walk subscription wiring on the header side.
            SearchDataGridColumnHeader.SetDescriptor(column, this);
            return column;
        }

        /// <summary>
        /// Picks a sensible default <see cref="BaseEditSettings"/> by CLR type so columns without
        /// an explicit <see cref="EditSettings"/> still get a styled editor:
        /// <list type="bullet">
        ///   <item>DateTime / DateTime? → <see cref="DateEditSettings"/> (masked date editor).
        ///   Data with time-of-day uses <c>"MM/dd/yyyy HH:mm:ss"</c>; date-only uses the default
        ///   short-date mask. Always applied — display formatting is integrated into the editor.</item>
        ///   <item>Numeric (int, long, decimal, double, etc.) with a numeric
        ///   <see cref="DisplayStringFormat"/> (<c>C</c>/<c>N</c>/<c>F</c>/<c>P</c> with optional
        ///   precision) → <see cref="TextEditSettings"/> with <see cref="TextEditSettings.Mask"/>
        ///   set to the format string and <see cref="TextEditSettings.MaskType"/> =
        ///   <see cref="MaskType.Numeric"/>. The read-only TextBlock keeps the user's format via
        ///   <c>Binding.StringFormat</c>; the edit TextBox routes through <c>NumericMaskFormatter</c>
        ///   for keystroke validation and chrome stripping; the filter row picks up the numeric
        ///   operator whitelist via <see cref="TextEditSettings.GetSupportedFilterSearchTypes"/>.</item>
        ///   <item>bool / bool? → <see cref="CheckBoxEditSettings"/>.</item>
        ///   <item>string → <see cref="TextEditSettings"/> (no mask).</item>
        ///   <item>Numeric (int, long, decimal, double, etc.) without a custom display →
        ///   <see cref="TextEditSettings"/> with <see cref="TextEditSettings.MaskType"/> =
        ///   <see cref="MaskType.Numeric"/> and no <see cref="TextEditSettings.Mask"/>. Behaves
        ///   like a plain TextBox in edit mode while still routing the filter row through the
        ///   numeric operator whitelist (Equals, GreaterThan, Between, …). Consumers who want
        ///   up/down spinner UX assign <see cref="SpinEditSettings"/> explicitly.</item>
        ///   <item>Any non-DateTime, non-numeric-format field with <paramref name="wantsCustomDisplay"/> →
        ///   plain <see cref="TextEditSettings"/>. <see cref="TextEditSettings.UseMaskAsDisplayFormat"/>
        ///   is set when <see cref="DisplayMask"/> is present so the mask formats the
        ///   display text; otherwise <see cref="TextEditSettings.CreateDisplayTemplate"/> applies
        ///   the column's <see cref="DisplayValueConverter"/> or
        ///   <see cref="DisplayStringFormat"/> to the styled
        ///   <c>DisplayTextBlock</c>.</item>
        /// </list>
        /// </summary>
        private BaseEditSettings AutoCreateEditSettings(bool wantsCustomDisplay)
        {
            if (FieldType == typeof(DateTime) || FieldType == typeof(DateTime?))
                return BuildAutoDateTimeEditSettings();

            // Numeric column with a numeric DisplayStringFormat: skip the spinner editor (the
            // user opted into custom display, so spinner chrome is unwanted) and instead wire a
            // mask-aware TextEditSettings. The Numeric mask engine accepts the same C/N/F/P
            // grammar the format string uses, so DisplayStringFormat doubles as the edit-time
            // mask without extra configuration on the consumer's side.
            if (wantsCustomDisplay
                && IsNumericType(FieldType)
                && IsNumericFormatString(DisplayStringFormat))
            {
                return new TextEditSettings
                {
                    Mask = DisplayStringFormat,
                    MaskType = MaskType.Numeric,
                };
            }

            // Custom-display column on a non-DateTime, non-numeric-format field: route through
            // a plain TextEditSettings so the styled DisplayTextBlock template renders the value
            // with the same padding and vertical centering the other formatted columns use.
            // TextEditSettings.CreateDisplayTemplate already honors DisplayValueConverter and
            // DisplayStringFormat; UseMaskAsDisplayFormat=true engages the MaskFormatConverter
            // path when the column carries a DisplayMask.
            if (wantsCustomDisplay)
            {
                return new TextEditSettings
                {
                    UseMaskAsDisplayFormat = !string.IsNullOrEmpty(DisplayMask),
                };
            }

            if (FieldType == typeof(bool) || FieldType == typeof(bool?))
                return new CheckBoxEditSettings();

            if (FieldType == typeof(string))
                return new TextEditSettings();

            if (IsNumericType(FieldType))
                return BuildAutoNumericEditSettings();

            return null;
        }

        // Numeric default. MaskType=Numeric (without an explicit Mask) keeps the filter row's
        // operator whitelist on the numeric set (Equals, GreaterThan, Between, …) per
        // TextEditSettings.GetSupportedFilterSearchTypes, while the empty Mask leaves
        // MaskInputBehavior unwired — the edit TextBox behaves like a plain text editor.
        // Spinner UX is opt-in via explicit SpinEditSettings.
        private static BaseEditSettings BuildAutoNumericEditSettings()
            => new TextEditSettings { MaskType = MaskType.Numeric };

        private BaseEditSettings BuildAutoDateTimeEditSettings()
        {
            // Date-only DisplayStringFormat: adopt it as the editor Mask with
            // UseMaskAsDisplayFormat=true so the filter editor has no time segments, the cell
            // continues to render the same date-only text via MaskFormatConverter, and
            // ResolveEffectiveRoundDateTime forces .Date comparison. Time-bearing
            // DisplayStringFormat is intentionally left to the path below: routing it through
            // MaskFormatConverter would normalize single 'h'/'m' tokens to fixed-width
            // 'hh'/'mm' and silently change the displayed hour from "8:30 AM" to "08:30 AM".
            if (!string.IsNullOrEmpty(DisplayStringFormat) && !DateTimeFormatHasTimeTokens(DisplayStringFormat))
            {
                return new DateEditSettings
                {
                    Mask = DisplayStringFormat,
                    MaskType = MaskType.DateTime,
                    UseMaskAsDisplayFormat = true,
                };
            }

            // Past this point DisplayStringFormat is empty or carries time tokens. The editor
            // mask must agree with whether the filter comparison rounds time-of-day — when it
            // rounds, exposing time segments lets the user commit a value the filter silently
            // discards. ResolveEffectiveRoundDateTime is the single source of truth: explicit
            // RoundDateTime DP, otherwise the data-sampling fallback.
            if (!ResolveEffectiveRoundDateTime())
            {
                return new DateEditSettings
                {
                    Mask = "MM/dd/yyyy HH:mm:ss",
                    MaskType = MaskType.DateTime,
                };
            }
            return new DateEditSettings();
        }

        /// <summary>
        /// Routes <see cref="TextAlignment"/> into a <see cref="DataGridTextColumn"/>'s
        /// <c>ElementStyle</c> (read-only TextBlock) and <c>EditingElementStyle</c> (edit TextBox).
        /// Synthesizes new styles based on whatever the column already has so user-supplied
        /// styles are preserved.
        /// </summary>
        private void ApplyAlignmentToTextColumn(DataGridTextColumn column)
        {
            column.ElementStyle = BuildStyleWithSetter(typeof(TextBlock), column.ElementStyle,
                TextBlock.TextAlignmentProperty, TextAlignment);
            column.EditingElementStyle = BuildStyleWithSetter(typeof(TextBox), column.EditingElementStyle,
                TextBox.TextAlignmentProperty, TextAlignment);
        }

        /// <summary>
        /// Routes <see cref="TextAlignment"/> into a <see cref="DataGridCheckBoxColumn"/>'s
        /// element styles by setting the CheckBox's <see cref="FrameworkElement.HorizontalAlignment"/>
        /// — TextAlignment doesn't apply to a checkbox glyph, so the box itself shifts left /
        /// center / right within the cell instead.
        /// </summary>
        private void ApplyAlignmentToCheckBoxColumn(DataGridCheckBoxColumn column)
        {
            HorizontalAlignment hAlign = TextAlignment switch
            {
                TextAlignment.Center => HorizontalAlignment.Center,
                TextAlignment.Right => HorizontalAlignment.Right,
                TextAlignment.Justify => HorizontalAlignment.Stretch,
                _ => HorizontalAlignment.Left,
            };
            column.ElementStyle = BuildStyleWithSetter(typeof(CheckBox), column.ElementStyle,
                FrameworkElement.HorizontalAlignmentProperty, hAlign);
            column.EditingElementStyle = BuildStyleWithSetter(typeof(CheckBox), column.EditingElementStyle,
                FrameworkElement.HorizontalAlignmentProperty, hAlign);
        }

        private static Style BuildStyleWithSetter(Type targetType, Style basedOn, DependencyProperty property, object value)
        {
            var style = new Style(targetType, basedOn);
            style.Setters.Add(new Setter(property, value));
            return style;
        }

        /// <summary>
        /// Resolves the effective read-only state. Returns <c>true</c> when any of the three
        /// inputs marks the column read-only:
        /// <list type="bullet">
        /// <item>column-level <see cref="IsReadOnly"/> — the explicit per-column setting,</item>
        /// <item>grid-level <see cref="DataGrid.IsReadOnly"/> on the host <see cref="View"/> —
        ///   the grid-wide default,</item>
        /// <item>source data marks <see cref="FieldName"/> as read-only (e.g. a computed
        ///   <see cref="System.Data.DataColumn"/> with <c>ReadOnly = true</c>).</item>
        /// </list>
        /// For per-row gating use <see cref="IsEnabledBinding"/> on the column.
        /// </summary>
        internal bool GetEffectiveReadOnly()
        {
            if (IsReadOnly) return true;
            if (View?.IsReadOnly == true) return true;
            return View?.IsSourceFieldReadOnly(FieldName) ?? false;
        }

        /// <summary>
        /// Creates the value <see cref="Binding"/> for a text column, applying
        /// <see cref="DisplayStringFormat"/> and <see cref="DisplayValueConverter"/> onto any slot
        /// the resolved field binding leaves empty. The path/source come from
        /// <see cref="CreateFieldBinding"/> (the explicit <see cref="Binding"/> override when set,
        /// otherwise <see cref="FieldName"/>).
        /// </summary>
        private Binding CreateBinding()
        {
            var binding = CreateFieldBinding();

            if (binding.Converter == null && DisplayValueConverter != null)
            {
                binding.Converter = DisplayValueConverter;
                binding.ConverterParameter = DisplayConverterParameter;
            }

            if (string.IsNullOrEmpty(binding.StringFormat) && !string.IsNullOrEmpty(DisplayStringFormat))
                binding.StringFormat = DisplayStringFormat;
            else if (string.IsNullOrEmpty(binding.StringFormat) && !string.IsNullOrEmpty(RoundDateDisplayFormat))
                binding.StringFormat = RoundDateDisplayFormat;

            return binding;
        }

        /// <summary>
        /// Builds a fresh single <see cref="Binding"/> targeting the cell value. When
        /// <see cref="Binding"/> is set to a single <see cref="Binding"/>, its path, source, and
        /// converter are honored; otherwise — no override, or a <see cref="MultiBinding"/> /
        /// <see cref="PriorityBinding"/> that has no single editable path — the binding targets
        /// <see cref="FieldName"/>. Callers layer mode, update trigger, validation, and display
        /// formatting on top. Shared by the bound-column paths and every EditSettings editor /
        /// display template so they bind to the same effective value.
        /// </summary>
        internal Binding CreateFieldBinding()
        {
            if (Binding is Binding custom)
            {
                var b = new Binding
                {
                    Converter = custom.Converter,
                    ConverterParameter = custom.ConverterParameter,
                    ConverterCulture = custom.ConverterCulture,
                };
                if (custom.Path != null)
                    b.Path = custom.Path;
                if (custom.Source != null)
                    b.Source = custom.Source;
                else if (custom.RelativeSource != null)
                    b.RelativeSource = custom.RelativeSource;
                else if (!string.IsNullOrEmpty(custom.ElementName))
                    b.ElementName = custom.ElementName;
                return b;
            }

            return new Binding(FieldName);
        }

        /// <summary>
        /// Resolves the binding used for a bound column's cell value and clipboard content. An
        /// explicit <see cref="MultiBinding"/> / <see cref="PriorityBinding"/> is returned verbatim
        /// (no display-formatting layering — the consumer owns a multi-binding's output); otherwise
        /// the result is <see cref="CreateBinding"/>, i.e. the field/override path with the column's
        /// display formatting layered in.
        /// </summary>
        internal BindingBase ResolveCellBinding()
        {
            if (Binding != null && Binding is not System.Windows.Data.Binding)
                return Binding;
            return CreateBinding();
        }

        /// <summary>
        /// Resolves the top-level property-path string the cell value reads from: the explicit
        /// <see cref="Binding"/> override's path when it is a single <see cref="Binding"/>,
        /// otherwise <see cref="FieldName"/>. Used by reflection-based value access (e.g. the spin
        /// editor's increment buttons) that needs the path rather than a <see cref="Binding"/>.
        /// </summary>
        internal string ResolveValuePath() => (Binding as Binding)?.Path?.Path ?? FieldName;

        #endregion

        #region Property Changed Callbacks

        /// <summary>
        /// Triggers a SyncToInternalColumn pass — used for ColumnDataBase-tier properties whose
        /// values land on the generated <see cref="DataGridColumn"/> (e.g. <see cref="IsReadOnly"/>
        /// → <c>DataGridColumn.IsReadOnly</c>, <see cref="AllowSorting"/> → <c>CanUserSort</c>).
        /// </summary>
        private static void OnSyncRequiredPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnDataBase col)
                col.SyncToInternalColumn();
        }

        /// <summary>
        /// Called when a filter/search property changes. Notifies the associated ColumnSearchBox.
        /// </summary>
        private static void OnFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null)
                return;

            var searchBox = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn);
            if (searchBox == null)
                return;

            if (e.Property == AllowFilterPopupProperty)
            {
                col.SetValue(ActualAllowFilterPopupPropertyKey, (bool)e.NewValue);
                searchBox.UpdateIsComplexFilteringEnabled();
            }
            else if (e.Property == AllowFilteringProperty)
            {
                bool allow = (bool)e.NewValue;
                col.SetValue(ActualAllowFilteringPropertyKey, allow);
                searchBox.IsFilterVisible = allow;
                searchBox.Visibility = allow ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.Property == AllowColumnFilteringProperty)
            {
                col.SetValue(ActualAllowColumnFilteringPropertyKey, (bool)e.NewValue);
            }
        }

        /// <summary>
        /// Called when a display formatting property changes. Recreates the display value provider.
        /// </summary>
        private static void OnDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null)
                return;

            // Sync layout (DisplayStringFormat affects the Binding.StringFormat)
            col.SyncToInternalColumn();

            var searchBox = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn);
            if (searchBox?.SearchTemplateController != null)
            {
                searchBox.SearchTemplateController.DisplayValueProvider =
                    Display.DisplayValueProviderFactory.Create(col);
                searchBox.SearchTemplateController.DisplayMaskPattern = col.DisplayMask;
            }
        }

        /// <summary>
        /// Called when ColumnDisplayName changes. Updates filter panel and column chooser.
        /// </summary>
        private static void OnColumnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null)
                return;

            col.SyncToInternalColumn();
            col.View.UpdateFilterSummaryPanel();

            var searchBox = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn);
            if (searchBox?.SearchTemplateController != null)
            {
                searchBox.SearchTemplateController.ColumnName = searchBox.ResolveColumnDisplayName();
            }
        }

        /// <summary>
        /// Called when <see cref="ShowCheckBoxInHeader"/> changes. Refreshes the resolved
        /// <see cref="ActualShowCheckBoxInHeader"/> and asks the grid to re-sync the header
        /// row-count display (for <see cref="SelectAllScope.SelectedRows"/>) and the descriptor's
        /// <see cref="IsChecked"/> snapshot.
        /// </summary>
        private static void OnShowCheckBoxInHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.RefreshActualShowCheckBoxInHeader();
            col.RequestSelectAllRefresh();
        }

        /// <summary>
        /// Called when <see cref="SelectAllScope"/> changes. Asks the grid to re-evaluate the
        /// descriptor's <see cref="IsChecked"/> snapshot against the new scope.
        /// </summary>
        private static void OnSelectAllScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            col.RequestSelectAllRefresh();
        }

        private void RequestSelectAllRefresh()
        {
            if (View == null) return;
            View.Dispatcher.BeginInvoke(new Action(() =>
            {
                View.RefreshSelectAllHeader(this);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Called when the column-level <see cref="ShowCriteriaInFilterRow"/> override
        /// changes. Re-resolves the effective value on the matching
        /// <see cref="ColumnFilterControl"/> so the inline selector visibility updates.
        /// </summary>
        private static void OnShowCriteriaInFilterRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null) return;
            var host = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn) as ColumnFilterControl;
            host?.RefreshEffectiveShowCriteria();
        }

        /// <summary>
        /// Called when <see cref="AllowAutoFilter"/> changes. Pushes the new value into the
        /// matching <see cref="IColumnFilterHost.IsFilterEnabled"/> so the cell greys / re-enables.
        /// </summary>
        private static void OnAllowAutoFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null) return;
            var host = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn);
            if (host == null) return;
            host.IsFilterEnabled = (bool)e.NewValue;
        }

        /// <summary>
        /// Called when <see cref="RoundDateTime"/> changes. Re-resolves the effective value on the
        /// matching <see cref="ColumnFilterControl"/>'s <see cref="SearchTemplateController"/> so
        /// in-flight filters pick up the new comparison mode without waiting for the cell to
        /// re-initialize.
        /// </summary>
        private static void OnRoundDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col) return;
            if (col.SearchTemplateController != null)
                col.SearchTemplateController.RoundDateTime = col.ResolveEffectiveRoundDateTime();
        }

        /// <summary>
        /// Called when <see cref="FilterRowCellStyle"/> changes. Re-resolves the style on the
        /// matching <see cref="ColumnFilterControl"/> (column override > grid setting > theme key).
        /// </summary>
        private static void OnFilterRowCellStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null) return;
            var host = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn) as ColumnFilterControl;
            host?.RefreshFilterRowCellStyle();
        }

        /// <summary>
        /// Called when <see cref="FilterRowDisplayTemplate"/> or
        /// <see cref="FilterRowEditTemplate"/> changes. Triggers the matching
        /// <see cref="ColumnFilterControl"/> to rebuild its editor host via
        /// <see cref="ColumnFilterControl.RefreshTemplate"/>, which swaps between the
        /// template-driven and EditSettings-driven editor surfaces.
        /// </summary>
        private static void OnFilterRowTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnDataBase col || col.View == null) return;
            var host = col.View.DataColumns.FirstOrDefault(c => c.CurrentColumn == col.InternalColumn) as ColumnFilterControl;
            host?.RefreshTemplate();
        }

        #endregion
    }
}
