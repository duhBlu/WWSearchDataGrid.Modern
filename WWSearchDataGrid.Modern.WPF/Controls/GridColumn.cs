using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A column descriptor that defines how a column should be created and configured in a <see cref="SearchDataGrid"/>.
    /// Instead of manually creating <see cref="DataGridColumn"/> instances and setting attached properties,
    /// declare <see cref="GridColumn"/> descriptors inside <c>SearchDataGrid.GridColumns</c> and the grid
    /// will generate the internal WPF columns automatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GridColumn"/> inherits from <see cref="FrameworkContentElement"/>, which provides
    /// DependencyProperty support, DataContext inheritance, and XAML binding capabilities without
    /// participating in the visual tree.
    /// </para>
    /// <para>
    /// The <see cref="FieldName"/> property is the primary key: it drives <c>Binding</c>,
    /// <c>SortMemberPath</c>, and <c>FilterMemberPath</c> unless explicitly overridden.
    /// </para>
    /// </remarks>
    public class GridColumn : FrameworkContentElement
    {
        public GridColumn()
        {
            // Forward DataContext changes to EditSettings so its bindings stay in sync. EditSettings
            // is not in the logical/visual tree and doesn't inherit DataContext on its own.
            DataContextChanged += (_, e) =>
            {
                if (EditSettings != null)
                    EditSettings.DataContext = e.NewValue;
            };
        }

        #region Layout Properties

        /// <summary>
        /// Gets or sets the property name on the data source that this column is bound to.
        /// This is the primary key — it auto-generates Binding, SortMemberPath, and FilterMemberPath.
        /// </summary>
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register(
                nameof(FieldName),
                typeof(string),
                typeof(GridColumn),
                new PropertyMetadata(null));

        public string FieldName
        {
            get => (string)GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the column header content. Falls back to <see cref="FieldName"/> if null.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(GridColumn),
                new PropertyMetadata(null, OnLayoutPropertyChanged));

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets the resolved display text of the header. Returns <see cref="Header"/> as string,
        /// or falls back to <see cref="FieldName"/>.
        /// </summary>
        public string HeaderCaption
        {
            get
            {
                if (Header is string s && !string.IsNullOrEmpty(s))
                    return s;
                return Header?.ToString() ?? FieldName ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the column width.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(DataGridLength),
                typeof(GridColumn),
                new PropertyMetadata(DataGridLength.Auto, OnLayoutPropertyChanged));

        public DataGridLength Width
        {
            get => (DataGridLength)GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum column width.
        /// </summary>
        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register(
                nameof(MinWidth),
                typeof(double),
                typeof(GridColumn),
                new PropertyMetadata(20.0, OnLayoutPropertyChanged));

        public double MinWidth
        {
            get => (double)GetValue(MinWidthProperty);
            set => SetValue(MinWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum column width.
        /// </summary>
        public static readonly DependencyProperty MaxWidthProperty =
            DependencyProperty.Register(
                nameof(MaxWidth),
                typeof(double),
                typeof(GridColumn),
                new PropertyMetadata(double.PositiveInfinity, OnLayoutPropertyChanged));

        public double MaxWidth
        {
            get => (double)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the column is visible.
        /// </summary>
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register(
                nameof(Visible),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnVisiblePropertyChanged));

        public bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the position among visible columns. -1 means auto (append order).
        /// </summary>
        public static readonly DependencyProperty VisibleIndexProperty =
            DependencyProperty.Register(
                nameof(VisibleIndex),
                typeof(int),
                typeof(GridColumn),
                new PropertyMetadata(-1));

        public int VisibleIndex
        {
            get => (int)GetValue(VisibleIndexProperty);
            set => SetValue(VisibleIndexProperty, value);
        }

        /// <summary>
        /// Gets or sets the column's pinned position. <see cref="FixedColumnPosition.Left"/>
        /// pins the column to the left edge of the grid via
        /// <see cref="System.Windows.Controls.DataGrid.FrozenColumnCount"/>;
        /// <see cref="FixedColumnPosition.Right"/> orders the column after every
        /// unpinned column so it stays anchored at the right end; <see cref="FixedColumnPosition.None"/>
        /// (the default) leaves the column in the normal flow.
        /// </summary>
        /// <remarks>
        /// Layout reordering is performed by <see cref="SearchDataGrid"/> whenever this
        /// property changes — left-fixed columns are moved to the start of
        /// <see cref="System.Windows.Controls.DataGrid.Columns"/>, right-fixed columns to the
        /// end, and unfixed columns keep their relative order between the two groups.
        /// </remarks>
        public static readonly DependencyProperty FixedProperty =
            DependencyProperty.Register(
                nameof(Fixed),
                typeof(FixedColumnPosition),
                typeof(GridColumn),
                new PropertyMetadata(FixedColumnPosition.None, OnFixedChanged));

        public FixedColumnPosition Fixed
        {
            get => (FixedColumnPosition)GetValue(FixedProperty);
            set => SetValue(FixedProperty, value);
        }

        private static void OnFixedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn gc)
                gc.Owner?.ApplyFixedColumnLayout();
        }

        /// <summary>
        /// Gets or sets whether the user can drag-reorder the column.
        /// </summary>
        public static readonly DependencyProperty AllowMovingProperty =
            DependencyProperty.Register(
                nameof(AllowMoving),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool AllowMoving
        {
            get => (bool)GetValue(AllowMovingProperty);
            set => SetValue(AllowMovingProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the user can resize the column.
        /// </summary>
        public static readonly DependencyProperty AllowResizingProperty =
            DependencyProperty.Register(
                nameof(AllowResizing),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool AllowResizing
        {
            get => (bool)GetValue(AllowResizingProperty);
            set => SetValue(AllowResizingProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the column appears in the column chooser.
        /// </summary>
        public static readonly DependencyProperty ShowInColumnChooserProperty =
            DependencyProperty.Register(
                nameof(ShowInColumnChooser),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true));

        public bool ShowInColumnChooser
        {
            get => (bool)GetValue(ShowInColumnChooserProperty);
            set => SetValue(ShowInColumnChooserProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the column is read-only (prevents editing).
        /// </summary>
        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false, OnLayoutPropertyChanged));

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        #endregion

        #region Data/Display Properties

        /// <summary>
        /// Gets or sets the property path used for filtering. Overrides <see cref="FieldName"/>.
        /// </summary>
        public static readonly DependencyProperty FilterMemberPathProperty =
            DependencyProperty.Register(
                nameof(FilterMemberPath),
                typeof(string),
                typeof(GridColumn),
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
                typeof(GridColumn),
                new PropertyMetadata(null));

        public string SortMemberPath
        {
            get => (string)GetValue(SortMemberPathProperty);
            set => SetValue(SortMemberPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the display name shown in Column Chooser, Filter Panel, and other UI components.
        /// Overrides <see cref="Header"/> for display purposes.
        /// </summary>
        public static readonly DependencyProperty ColumnDisplayNameProperty =
            DependencyProperty.Register(
                nameof(ColumnDisplayName),
                typeof(string),
                typeof(GridColumn),
                new PropertyMetadata(null, OnColumnDisplayNameChanged));

        public string ColumnDisplayName
        {
            get => (string)GetValue(ColumnDisplayNameProperty);
            set => SetValue(ColumnDisplayNameProperty, value);
        }

        /// <summary>
        /// Gets or sets a .NET format string for display values (e.g., "C2", "MM/dd/yyyy", "N0").
        /// </summary>
        public static readonly DependencyProperty DisplayStringFormatProperty =
            DependencyProperty.Register(
                nameof(DisplayStringFormat),
                typeof(string),
                typeof(GridColumn),
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
                typeof(GridColumn),
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
                typeof(GridColumn),
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
                typeof(GridColumn),
                new PropertyMetadata(null, OnDisplayPropertyChanged));

        public string DisplayMask
        {
            get => (string)GetValue(DisplayMaskProperty);
            set => SetValue(DisplayMaskProperty, value);
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
                typeof(GridColumn),
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
            if (d is GridColumn gc)
                gc._isAutoTextAlignment = false;
        }

        /// <summary>
        /// Gets or sets the data type of the field. Auto-detected from the data source if not set explicitly.
        /// </summary>
        public static readonly DependencyProperty FieldTypeProperty =
            DependencyProperty.Register(
                nameof(FieldType),
                typeof(Type),
                typeof(GridColumn),
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
        /// it after <see cref="SetValue"/> returns so a subsequent <see cref="IsFieldTypeExplicit"/>
        /// check correctly reports the value as auto.
        /// </summary>
        internal void SetAutoFieldType(Type type)
        {
            SetValue(FieldTypeProperty, type);
            _isAutoFieldType = true;
        }

        private static void OnFieldTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc) return;

            // Any write clears the auto flag. SetAutoFieldType restores it after SetValue returns.
            gc._isAutoFieldType = false;

            // Apply type-based defaults whenever FieldType changes — covers both auto-resolution
            // from the data source and explicit XAML/code values. Properties already set by the
            // user are preserved (each Set... helper checks Is...Explicit).
            gc.ApplyTypeBasedDefaults();
        }

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
                // AutoFilterRow cell because the registered StartsWith default isn't in the
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

        #endregion

        #region Filtering/Search Properties

        /// <summary>
        /// Gets or sets whether complex filtering UI is enabled for this column.
        /// </summary>
        public static readonly DependencyProperty EnableRuleFilteringProperty =
            DependencyProperty.Register(
                nameof(EnableRuleFiltering),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnFilterPropertyChanged));

        public bool EnableRuleFiltering
        {
            get => (bool)GetValue(EnableRuleFilteringProperty);
            set => SetValue(EnableRuleFilteringProperty, value);
        }

        /// <summary>
        /// Gets or sets the default search type for this column's auto-filter row quick search.
        /// </summary>
        /// <remarks>
        /// String columns default to <see cref="WPF.DefaultSearchType.StartsWith"/> (set in
        /// <see cref="ApplyTypeBasedDefaults"/>). Other CLR types use the registered default
        /// (<see cref="WPF.DefaultSearchType.StartsWith"/>) unless overridden by
        /// <see cref="ApplyTypeBasedDefaults"/> (e.g. <c>DateTime</c> / enums → <c>Equals</c>).
        /// </remarks>
        public static readonly DependencyProperty DefaultSearchTypeProperty =
            DependencyProperty.Register(
                nameof(DefaultSearchType),
                typeof(DefaultSearchType),
                typeof(GridColumn),
                new PropertyMetadata(WWSearchDataGrid.Modern.WPF.DefaultSearchType.StartsWith, OnDefaultSearchTypePropertyChanged));

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
            if (d is not GridColumn gc) return;
            gc._isAutoDefaultSearchType = false;

            // Spec rule (§1.8 of the plan): when the column's default search criteria is excluded
            // from the matching host's SupportedSearchTypes whitelist, the cell disables. The
            // resolved DefaultSearchType is one of the inputs to that check — re-evaluate on the
            // host whenever this value changes.
            if (gc.Owner == null) return;
            var host = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn) as ColumnFilterControl;
            host?.UpdateEffectiveIsCellEnabled();
        }

        /// <summary>
        /// Gets or sets whether to force checkbox filtering mode in the search box.
        /// </summary>
        public static readonly DependencyProperty UseCheckBoxInSearchBoxProperty =
            DependencyProperty.Register(
                nameof(UseCheckBoxInSearchBox),
                typeof(bool),
                typeof(GridColumn),
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
            if (d is not GridColumn gc) return;

            // Any write clears the auto flag. SetAutoUseCheckBoxInSearchBox restores it after SetValue.
            gc._isAutoUseCheckBoxInSearchBox = false;

            if (gc.Owner == null) return;
            var searchBox = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn);
            searchBox?.DetermineCheckboxColumnTypeFromColumnDefinition();
        }

        /// <summary>
        /// Gets or sets a custom search template type for this column.
        /// </summary>
        public static readonly DependencyProperty CustomSearchTemplateProperty =
            DependencyProperty.Register(
                nameof(CustomSearchTemplate),
                typeof(Type),
                typeof(GridColumn),
                new PropertyMetadata(null));

        public Type CustomSearchTemplate
        {
            get => (Type)GetValue(CustomSearchTemplateProperty);
            set => SetValue(CustomSearchTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets whether filtering is allowed on this column.
        /// When false, the search box is completely hidden.
        /// </summary>
        public static readonly DependencyProperty AllowFilteringProperty =
            DependencyProperty.Register(
                nameof(AllowFiltering),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnFilterPropertyChanged));

        public bool AllowFiltering
        {
            get => (bool)GetValue(AllowFilteringProperty);
            set => SetValue(AllowFilteringProperty, value);
        }

        /// <summary>
        /// Gets or sets whether sorting is allowed on this column.
        /// When false, clicking the header does not sort.
        /// </summary>
        public static readonly DependencyProperty AllowSortingProperty =
            DependencyProperty.Register(
                nameof(AllowSorting),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(true, OnLayoutPropertyChanged));

        public bool AllowSorting
        {
            get => (bool)GetValue(AllowSortingProperty);
            set => SetValue(AllowSortingProperty, value);
        }

        /// <summary>
        /// Gets or sets a column-level override for the grid's
        /// <see cref="SearchDataGrid.ShowCriteriaInAutoFilterRow"/>. <c>null</c> (the default)
        /// inherits the grid value; <c>true</c> / <c>false</c> overrides it for this column.
        /// </summary>
        public static readonly DependencyProperty ShowCriteriaInAutoFilterRowProperty =
            DependencyProperty.Register(
                nameof(ShowCriteriaInAutoFilterRow),
                typeof(bool?),
                typeof(GridColumn),
                new PropertyMetadata(null, OnShowCriteriaInAutoFilterRowChanged));

        public bool? ShowCriteriaInAutoFilterRow
        {
            get => (bool?)GetValue(ShowCriteriaInAutoFilterRowProperty);
            set => SetValue(ShowCriteriaInAutoFilterRowProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the auto-filter row cell for this column is enabled.
        /// <c>false</c> disables (greys) the cell while preserving its space — distinct
        /// from <see cref="AllowFiltering"/>, which hides the cell entirely. The spec
        /// names this property <c>AllowAutoFilter</c>; both remain on the column with
        /// complementary semantics.
        /// </summary>
        public static readonly DependencyProperty AllowAutoFilterProperty =
            DependencyProperty.Register(
                nameof(AllowAutoFilter),
                typeof(bool),
                typeof(GridColumn),
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
                typeof(GridColumn),
                new PropertyMetadata(null, OnRoundDateTimeChanged));

        public bool? RoundDateTime
        {
            get => (bool?)GetValue(RoundDateTimeProperty);
            set => SetValue(RoundDateTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets a column-level override for the grid's
        /// <see cref="SearchDataGrid.AutoFilterRowCellStyle"/>. <c>null</c> (default)
        /// inherits the grid setting; any non-null <see cref="Style"/> wins over the grid
        /// value for this column. When both are null, the keyed theme style
        /// (<see cref="SdgThemeKeys.FilterRow.ColumnFilterControl"/>) is used.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowCellStyleProperty =
            DependencyProperty.Register(
                nameof(AutoFilterRowCellStyle),
                typeof(Style),
                typeof(GridColumn),
                new PropertyMetadata(null, OnAutoFilterRowCellStyleChanged));

        public Style AutoFilterRowCellStyle
        {
            get => (Style)GetValue(AutoFilterRowCellStyleProperty);
            set => SetValue(AutoFilterRowCellStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used as the display surface for the
        /// auto-filter row cell on this column. When set, it replaces the default editor
        /// produced by <see cref="BaseEditSettings.CreateFilterEditor"/>. The template's
        /// <see cref="FrameworkElement.DataContext"/> is an <see cref="EditGridCellData"/>
        /// instance with <c>Value</c> two-way-bound to the column's filter value; templates
        /// that don't bind <c>{Binding Value}</c> simply won't drive the filter — matching
        /// DevExpress's behavior. Used as the fallback when
        /// <see cref="AutoFilterRowEditTemplate"/> is also <c>null</c>; the filter row is
        /// always-edit, so the spec's display/edit distinction collapses here.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowDisplayTemplateProperty =
            DependencyProperty.Register(
                nameof(AutoFilterRowDisplayTemplate),
                typeof(DataTemplate),
                typeof(GridColumn),
                new PropertyMetadata(null, OnAutoFilterRowTemplateChanged));

        public DataTemplate AutoFilterRowDisplayTemplate
        {
            get => (DataTemplate)GetValue(AutoFilterRowDisplayTemplateProperty);
            set => SetValue(AutoFilterRowDisplayTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used as the edit surface for the
        /// auto-filter row cell on this column. Takes precedence over
        /// <see cref="AutoFilterRowDisplayTemplate"/> — when both are set, the edit template
        /// wins; when only the display template is set, it serves both display and edit roles
        /// (the filter row is always-edit). Same <see cref="EditGridCellData"/> context shape
        /// as <see cref="AutoFilterRowDisplayTemplate"/>.
        /// </summary>
        public static readonly DependencyProperty AutoFilterRowEditTemplateProperty =
            DependencyProperty.Register(
                nameof(AutoFilterRowEditTemplate),
                typeof(DataTemplate),
                typeof(GridColumn),
                new PropertyMetadata(null, OnAutoFilterRowTemplateChanged));

        public DataTemplate AutoFilterRowEditTemplate
        {
            get => (DataTemplate)GetValue(AutoFilterRowEditTemplateProperty);
            set => SetValue(AutoFilterRowEditTemplateProperty, value);
        }

        #endregion

        #region Editor Properties

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
                typeof(GridColumn),
                new PropertyMetadata(null, OnEditSettingsChanged));

        public BaseEditSettings EditSettings
        {
            get => (BaseEditSettings)GetValue(EditSettingsProperty);
            set => SetValue(EditSettingsProperty, value);
        }

        private static void OnEditSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn col) return;

            // EditSettings is a FrameworkContentElement but isn't part of the visual / logical tree,
            // so it doesn't automatically inherit DataContext. Push the column's current DataContext
            // down so XAML bindings on EditSettings (e.g. ComboBoxEditSettings.ItemsSource) resolve
            // against the same source as bindings elsewhere on the grid.
            if (e.NewValue is BaseEditSettings settings)
                settings.DataContext = col.DataContext;

            // Rebuild the generated cell templates against the new editor configuration so a
            // runtime swap (e.g. CheckBox → ComboBox for a bool column) actually re-renders the
            // existing cells. Only valid when the column has already been generated and is a
            // DataGridTemplateColumn (the EditSettings code path always produces one).
            if (col.InternalColumn is DataGridTemplateColumn templateColumn && e.NewValue is BaseEditSettings newSettings)
            {
                templateColumn.CellTemplate = newSettings.ResolveDisplayTemplate(col);
                templateColumn.CellEditingTemplate = newSettings.ResolveEditTemplate(col);
            }

            // Editor-shape preference may differ from the CLR-type default applied earlier.
            // ApplyTypeBasedDefaults also calls this, but that runs on FieldType change — when
            // EditSettings is set or swapped after FieldType has settled, we still need to clamp.
            col.ApplyEditSettingsPreferredDefaults();
        }

        #endregion

        #region Select-All Properties

        /// <summary>
        /// Gets or sets whether this column shows a select-all checkbox in the header.
        /// Only works with boolean-typed columns.
        /// </summary>
        public static readonly DependencyProperty IsSelectAllColumnProperty =
            DependencyProperty.Register(
                nameof(IsSelectAllColumn),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false, OnSelectAllPropertyChanged));

        public bool IsSelectAllColumn
        {
            get => (bool)GetValue(IsSelectAllColumnProperty);
            set => SetValue(IsSelectAllColumnProperty, value);
        }

        /// <summary>
        /// Gets or sets the scope of items affected by the select-all checkbox.
        /// </summary>
        public static readonly DependencyProperty SelectAllScopeProperty =
            DependencyProperty.Register(
                nameof(SelectAllScope),
                typeof(SelectAllScope),
                typeof(GridColumn),
                new PropertyMetadata(SelectAllScope.FilteredRows, OnSelectAllPropertyChanged));

        public SelectAllScope SelectAllScope
        {
            get => (SelectAllScope)GetValue(SelectAllScopeProperty);
            set => SetValue(SelectAllScopeProperty, value);
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the resolved rendered width of the column.
        /// Set internally by <see cref="SearchDataGrid"/> after column generation.
        /// </summary>
        public double ActualWidth { get; internal set; }

        /// <summary>
        /// Gets the resolved position among visible columns.
        /// Set internally by <see cref="SearchDataGrid"/> after column generation.
        /// </summary>
        public int ActualVisibleIndex { get; internal set; } = -1;

        /// <summary>
        /// Gets whether this column was auto-generated by the grid (not declared in XAML).
        /// </summary>
        public bool IsAutoGenerated { get; internal set; }

        /// <summary>
        /// Gets the WPF <see cref="DataGridColumn"/> that was generated from this descriptor.
        /// Set internally after <see cref="CreateDataGridColumn"/> is called.
        /// </summary>
        public DataGridColumn InternalColumn { get; internal set; }

        /// <summary>
        /// Gets the parent <see cref="SearchDataGrid"/> that owns this column descriptor.
        /// </summary>
        public SearchDataGrid Owner { get; internal set; }

        /// <summary>
        /// The persistent <see cref="WWSearchDataGrid.Modern.Core.SearchTemplateController"/> for this column.
        /// Stored on the descriptor (not the <see cref="ColumnSearchBox"/>) so that filter state survives
        /// horizontal column virtualization — when the header scrolls out and back, the new
        /// <see cref="ColumnSearchBox"/> instance reconnects to the same controller instead of starting empty.
        /// </summary>
        internal WWSearchDataGrid.Modern.Core.SearchTemplateController SearchTemplateController { get; set; }

        #endregion

        #region Column Generation

        /// <summary>
        /// Creates the internal WPF <see cref="DataGridColumn"/> from this descriptor's properties.
        /// The generated column is stored in <see cref="InternalColumn"/> and returned.
        /// </summary>
        /// <returns>The generated <see cref="DataGridColumn"/>.</returns>
        internal DataGridColumn CreateDataGridColumn()
        {
            if (string.IsNullOrEmpty(FieldName))
            {
                //Debug.WriteLine("GridColumn.CreateDataGridColumn: FieldName is required.");
                return null;
            }

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
            // importantly ColumnFilterControl, which reads GridColumn.EditSettings to pick the
            // filter-row editor shape and the SearchTypeSelector operator whitelist — see the
            // same instance that's driving the cell templates. Without this, auto-created
            // settings remain invisible to the filter row and it defaults to a string-only
            // TextEditSettings whitelist. The assignment fires OnEditSettingsChanged, but its
            // template-rebuild branch is guarded by a non-null InternalColumn (still null at
            // this point), so the only side effect is DataContext propagation — desirable.
            if (EditSettings == null && effectiveEditSettings != null)
                EditSettings = effectiveEditSettings;

            // EditSettings, when present, drives template generation — produces a DataGridTemplateColumn
            // with display + edit templates from the editor configuration. Takes precedence over the
            // default text/checkbox selection so consumers can opt into a richer editor.
            if (effectiveEditSettings != null)
            {
                column = new DataGridTemplateColumn
                {
                    // Resolve* prefers a user-supplied EditTemplate / DisplayTemplate over the
                    // editor's code-built default, so consumers can take over the layout entirely.
                    CellTemplate = effectiveEditSettings.ResolveDisplayTemplate(this),
                    CellEditingTemplate = effectiveEditSettings.ResolveEditTemplate(this),
                    ClipboardContentBinding = CreateBinding(),
                    // Override the default cell style alignments (Center) so the editor template
                    // stretches to fill the cell. Without this, controls like ComboBox/DatePicker
                    // shrink to their content size and float in the middle of the cell.
                    CellStyle = BuildStretchingCellStyle(Owner?.CellStyle)
                };
            }
            else if (isBoolField && !wantsCustomDisplay)
            {
                var checkBoxColumn = new DataGridCheckBoxColumn
                {
                    Binding = new Binding(FieldName)
                };
                ApplyAlignmentToCheckBoxColumn(checkBoxColumn);
                column = checkBoxColumn;
            }
            else
            {
                var textColumn = new DataGridTextColumn
                {
                    Binding = CreateBinding()
                };
                ApplyAlignmentToTextColumn(textColumn);
                column = textColumn;
            }

            // Apply layout properties
            column.Header = Header ?? FieldName;
            column.Width = Width;
            column.MinWidth = MinWidth;
            column.MaxWidth = MaxWidth;
            column.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;
            column.IsReadOnly = GetEffectiveReadOnly();
            column.CanUserSort = AllowSorting;
            column.CanUserResize = AllowResizing;
            column.CanUserReorder = AllowMoving;
            column.SortMemberPath = SortMemberPath ?? FieldName;

            InternalColumn = column;
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
        ///   is set when <see cref="GridColumn.DisplayMask"/> is present so the mask formats the
        ///   display text; otherwise <see cref="TextEditSettings.CreateDisplayTemplate"/> applies
        ///   the column's <see cref="GridColumn.DisplayValueConverter"/> or
        ///   <see cref="GridColumn.DisplayStringFormat"/> to the styled
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

        private static bool IsNumericType(Type t)
        {
            if (t == null) return false;
            var inner = Nullable.GetUnderlyingType(t) ?? t;
            return inner == typeof(int) || inner == typeof(long) || inner == typeof(short)
                || inner == typeof(byte) || inner == typeof(sbyte) || inner == typeof(ushort)
                || inner == typeof(uint) || inner == typeof(ulong)
                || inner == typeof(double) || inner == typeof(float) || inner == typeof(decimal);
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
            IEnumerable source = Owner?.ItemsSource;
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

        /// <summary>
        /// Builds a <see cref="DataGridCell"/> style that stretches its content to fill the cell,
        /// based on the grid's currently-configured cell style. Used for template columns
        /// generated from <see cref="EditSettings"/> so editors like ComboBox / DatePicker / TextBox
        /// span the full cell rather than shrinking to content size.
        /// </summary>
        /// <param name="basedOn">
        /// The cell style currently in use on the parent grid. The result inherits everything
        /// from this style (background, padding, selection brushes) and just overrides the
        /// content alignments.
        /// </param>
        private static Style BuildStretchingCellStyle(Style basedOn)
        {
            var style = new Style(typeof(DataGridCell), basedOn);
            style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Stretch));
            return style;
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
        /// Resolves the effective read-only state. Returns true when the descriptor itself is
        /// marked <see cref="ReadOnly"/>, or when the source data exposes <see cref="FieldName"/>
        /// as a read-only column (e.g. a computed <see cref="System.Data.DataColumn"/> with
        /// <c>ReadOnly = true</c>). Setting <see cref="DataGridColumn.IsReadOnly"/> from this value
        /// keeps the WPF DataGrid from beginning an edit on a target the binding cannot write back
        /// to, which would otherwise raise a TwoWay binding error against the source property.
        /// </summary>
        internal bool GetEffectiveReadOnly()
        {
            if (ReadOnly) return true;
            return Owner?.IsSourceFieldReadOnly(FieldName) ?? false;
        }

        /// <summary>
        /// Creates the <see cref="Binding"/> for a text column, applying <see cref="DisplayStringFormat"/>
        /// and <see cref="DisplayValueConverter"/> if set.
        /// </summary>
        private Binding CreateBinding()
        {
            var binding = new Binding(FieldName);

            if (!string.IsNullOrEmpty(DisplayStringFormat))
                binding.StringFormat = DisplayStringFormat;

            if (DisplayValueConverter != null)
            {
                binding.Converter = DisplayValueConverter;
                binding.ConverterParameter = DisplayConverterParameter;
            }

            return binding;
        }

        /// <summary>
        /// Updates the internal column's properties to reflect changes to this descriptor.
        /// </summary>
        internal void SyncToInternalColumn()
        {
            if (InternalColumn == null)
                return;

            InternalColumn.Header = Header ?? FieldName;
            InternalColumn.Width = Width;
            InternalColumn.MinWidth = MinWidth;
            InternalColumn.MaxWidth = MaxWidth;
            InternalColumn.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;
            InternalColumn.IsReadOnly = GetEffectiveReadOnly();
            InternalColumn.CanUserSort = AllowSorting;
            InternalColumn.CanUserResize = AllowResizing;
            InternalColumn.CanUserReorder = AllowMoving;
            InternalColumn.SortMemberPath = SortMemberPath ?? FieldName;
        }

        #endregion

        #region Property Changed Callbacks

        /// <summary>
        /// Called when any layout-related property changes. Syncs to the internal DataGridColumn.
        /// </summary>
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn gc)
                gc.SyncToInternalColumn();
        }

        /// <summary>
        /// Called when the Visible property changes. Only syncs visibility to the internal column
        /// without resetting other properties like Width, which can corrupt DataGrid scroll metrics
        /// during bulk visibility changes (e.g., column chooser Select All).
        /// </summary>
        private static void OnVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn gc && gc.InternalColumn != null)
            {
                gc.InternalColumn.Visibility = gc.Visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Called when a filter/search property changes. Notifies the associated ColumnSearchBox.
        /// </summary>
        private static void OnFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null)
                return;

            var searchBox = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn);
            if (searchBox == null)
                return;

            if (e.Property == EnableRuleFilteringProperty)
            {
                searchBox.UpdateIsComplexFilteringEnabled();
            }
            else if (e.Property == AllowFilteringProperty)
            {
                bool allow = (bool)e.NewValue;
                searchBox.IsFilterVisible = allow;
                searchBox.Visibility = allow ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Called when a display formatting property changes. Recreates the display value provider.
        /// </summary>
        private static void OnDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null)
                return;

            // Sync layout (DisplayStringFormat affects the Binding.StringFormat)
            gc.SyncToInternalColumn();

            var searchBox = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn);
            if (searchBox?.SearchTemplateController != null)
            {
                searchBox.SearchTemplateController.DisplayValueProvider =
                    Display.DisplayValueProviderFactory.Create(gc);
                searchBox.SearchTemplateController.DisplayMaskPattern = gc.DisplayMask;
            }
        }

        /// <summary>
        /// Called when ColumnDisplayName changes. Updates filter panel and column chooser.
        /// </summary>
        private static void OnColumnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null)
                return;

            gc.SyncToInternalColumn();
            gc.Owner.UpdateFilterPanel();

            var searchBox = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn);
            if (searchBox?.SearchTemplateController != null)
            {
                searchBox.SearchTemplateController.ColumnName = searchBox.ResolveColumnDisplayName();
            }
        }

        /// <summary>
        /// Called when select-all properties change. Refreshes select-all header checkboxes.
        /// </summary>
        private static void OnSelectAllPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null)
                return;

            gc.Owner.Dispatcher.BeginInvoke(new Action(() =>
            {
                gc.Owner.SetupSelectAllColumnHeaders();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Called when the column-level <see cref="ShowCriteriaInAutoFilterRow"/> override
        /// changes. Re-resolves the effective value on the matching
        /// <see cref="ColumnFilterControl"/> so the inline selector visibility updates.
        /// </summary>
        private static void OnShowCriteriaInAutoFilterRowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null) return;
            var host = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn) as ColumnFilterControl;
            host?.RefreshEffectiveShowCriteria();
        }

        /// <summary>
        /// Called when <see cref="AllowAutoFilter"/> changes. Pushes the new value into the
        /// matching <see cref="IColumnFilterHost.IsFilterEnabled"/> so the cell greys / re-enables.
        /// </summary>
        private static void OnAllowAutoFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null) return;
            var host = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn);
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
            if (d is not GridColumn gc) return;
            if (gc.SearchTemplateController != null)
                gc.SearchTemplateController.RoundDateTime = gc.ResolveEffectiveRoundDateTime();
        }

        /// <summary>
        /// Called when <see cref="AutoFilterRowCellStyle"/> changes. Re-resolves the style on the
        /// matching <see cref="ColumnFilterControl"/> (column override > grid setting > theme key).
        /// </summary>
        private static void OnAutoFilterRowCellStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null) return;
            var host = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn) as ColumnFilterControl;
            host?.RefreshAutoFilterRowCellStyle();
        }

        /// <summary>
        /// Called when <see cref="AutoFilterRowDisplayTemplate"/> or
        /// <see cref="AutoFilterRowEditTemplate"/> changes. Triggers the matching
        /// <see cref="ColumnFilterControl"/> to rebuild its editor host via
        /// <see cref="ColumnFilterControl.RefreshTemplate"/>, which swaps between the
        /// template-driven and EditSettings-driven editor surfaces.
        /// </summary>
        private static void OnAutoFilterRowTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GridColumn gc || gc.Owner == null) return;
            var host = gc.Owner.DataColumns.FirstOrDefault(c => c.CurrentColumn == gc.InternalColumn) as ColumnFilterControl;
            host?.RefreshTemplate();
        }

        #endregion
    }
}
