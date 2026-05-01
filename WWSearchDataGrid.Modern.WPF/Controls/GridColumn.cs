using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
        /// Gets or sets whether the column is frozen (fixed).
        /// </summary>
        public static readonly DependencyProperty FixedProperty =
            DependencyProperty.Register(
                nameof(Fixed),
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false));

        public bool Fixed
        {
            get => (bool)GetValue(FixedProperty);
            set => SetValue(FixedProperty, value);
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
        /// <item><c>bool</c> / <c>bool?</c> → <see cref="UseCheckBoxInSearchBox"/> = <c>true</c></item>
        /// <item><c>DateTime</c> / <c>DateTime?</c> → <see cref="DefaultSearchMode"/> = <see cref="DefaultSearchMode.Equals"/></item>
        /// <item>Enum types → <see cref="DefaultSearchMode"/> = <see cref="DefaultSearchMode.Equals"/></item>
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
            }
            else if (underlying == typeof(DateTime))
            {
                if (!IsDefaultSearchModeExplicit)
                    SetAutoDefaultSearchMode(DefaultSearchMode.Equals);
            }
            else if (underlying.IsEnum)
            {
                if (!IsDefaultSearchModeExplicit)
                    SetAutoDefaultSearchMode(DefaultSearchMode.Equals);
                // TODO: populate the search dropdown from Enum.GetValues(underlying).
                // The current dropdown is data-driven via SetupColumnDataLazy; injecting a
                // static enum source needs a separate code path on SearchTemplateController.
            }
            // string defaults to Contains (already the registered default).
            // decimal/double: no auto-format — DisplayStringFormat stays user-controlled.
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
        /// Gets or sets the default search mode for simple textbox searches.
        /// </summary>
        public static readonly DependencyProperty DefaultSearchModeProperty =
            DependencyProperty.Register(
                nameof(DefaultSearchMode),
                typeof(DefaultSearchMode),
                typeof(GridColumn),
                new PropertyMetadata(DefaultSearchMode.Contains, OnDefaultSearchModePropertyChanged));

        public DefaultSearchMode DefaultSearchMode
        {
            get => (DefaultSearchMode)GetValue(DefaultSearchModeProperty);
            set => SetValue(DefaultSearchModeProperty, value);
        }

        private bool _isAutoDefaultSearchMode;

        /// <summary>
        /// Gets whether <see cref="DefaultSearchMode"/> was set explicitly. Auto-configuration
        /// from <see cref="FieldType"/> skips columns where this is true.
        /// </summary>
        internal bool IsDefaultSearchModeExplicit => IsExplicitlySet(DefaultSearchModeProperty, _isAutoDefaultSearchMode);

        /// <summary>
        /// Sets <see cref="DefaultSearchMode"/> from auto-configuration.
        /// </summary>
        internal void SetAutoDefaultSearchMode(DefaultSearchMode mode)
        {
            SetValue(DefaultSearchModeProperty, mode);
            _isAutoDefaultSearchMode = true;
        }

        private static void OnDefaultSearchModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridColumn gc)
                gc._isAutoDefaultSearchMode = false;
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
                Debug.WriteLine("GridColumn.CreateDataGridColumn: FieldName is required.");
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

            if (isBoolField && !wantsCustomDisplay)
            {
                column = new DataGridCheckBoxColumn
                {
                    Binding = new Binding(FieldName)
                };
            }
            else
            {
                column = new DataGridTextColumn
                {
                    Binding = CreateBinding()
                };
            }

            // Apply layout properties
            column.Header = Header ?? FieldName;
            column.Width = Width;
            column.MinWidth = MinWidth;
            column.MaxWidth = MaxWidth;
            column.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;
            column.IsReadOnly = ReadOnly;
            column.CanUserSort = AllowSorting;
            column.CanUserResize = AllowResizing;
            column.CanUserReorder = AllowMoving;
            column.SortMemberPath = SortMemberPath ?? FieldName;

            InternalColumn = column;
            return column;
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
            InternalColumn.IsReadOnly = ReadOnly;
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

        #endregion
    }
}
