using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Abstract layout/visibility/header tier of the column descriptor hierarchy. Holds the
    /// generic column primitives — size, position, pinning, header content, and chooser
    /// participation — independent of any data-binding or filtering surface. Derived tiers add
    /// data identity (<see cref="ColumnDataBase"/>) and grid-specific features (<see cref="GridColumn"/>).
    /// </summary>
    public abstract class ColumnLayoutBase : ColumnDescriptorElement
    {
        protected ColumnLayoutBase()
        {
            // Materialize the initial HeaderCaption value so bindings see the resolved
            // string immediately instead of waiting for the first Header/FieldName write.
            RefreshHeaderCaption();
            RefreshActualHeaderStyling();
        }

        #region Header

        /// <summary>
        /// Gets or sets the column header content.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnHeaderChanged));

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        private static readonly DependencyPropertyKey HeaderCaptionPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HeaderCaption),
                typeof(string),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Read-only dependency property exposing <see cref="HeaderCaption"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty HeaderCaptionProperty = HeaderCaptionPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved display text for the header. <see cref="ColumnLayoutBase"/> stringifies
        /// <see cref="Header"/>; <see cref="ColumnDataBase"/> falls back to <c>FieldName</c> when
        /// the header is null. Updated automatically via <see cref="RefreshHeaderCaption"/>
        /// when the inputs change.
        /// </summary>
        public string HeaderCaption => (string)GetValue(HeaderCaptionProperty);

        /// <summary>
        /// Computes the effective <see cref="HeaderCaption"/> for this tier and pushes it onto
        /// the read-only DP. Derived tiers override to introduce additional fallbacks (e.g.
        /// <see cref="ColumnDataBase"/> uses <c>FieldName</c> when <see cref="Header"/> is null);
        /// they should call <see cref="SetHeaderCaption"/> with the resolved value.
        /// </summary>
        protected virtual void RefreshHeaderCaption()
        {
            SetHeaderCaption(ResolveHeaderCaption());
        }

        /// <summary>
        /// Default header-caption resolver — just stringifies <see cref="Header"/>. Exposed so
        /// derived tiers can call into the base resolution and apply their own fallback on top.
        /// </summary>
        protected string ResolveHeaderCaption()
        {
            if (Header is string s && !string.IsNullOrEmpty(s)) return s;
            return Header?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Sets the read-only <see cref="HeaderCaption"/>. Used by <see cref="RefreshHeaderCaption"/>
        /// implementations.
        /// </summary>
        protected void SetHeaderCaption(string value)
            => SetValue(HeaderCaptionPropertyKey, value ?? string.Empty);

        #endregion

        #region Header Templating

        /// <summary>
        /// Gets or sets the <see cref="Style"/> applied to the generated
        /// <see cref="DataGridColumnHeader"/>. The resolved value (after any synthesized
        /// alignment setter) is exposed via <see cref="ActualHeaderStyle"/>.
        /// </summary>
        public static readonly DependencyProperty HeaderStyleProperty =
            DependencyProperty.Register(
                nameof(HeaderStyle),
                typeof(Style),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnHeaderStylingChanged));

        public Style HeaderStyle
        {
            get => (Style)GetValue(HeaderStyleProperty);
            set => SetValue(HeaderStyleProperty, value);
        }

        private static readonly DependencyPropertyKey ActualHeaderStylePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualHeaderStyle),
                typeof(Style),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null));

        /// <summary>
        /// Read-only dependency property exposing <see cref="ActualHeaderStyle"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ActualHeaderStyleProperty = ActualHeaderStylePropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved header style. Equals <see cref="HeaderStyle"/> unless
        /// <see cref="HorizontalHeaderContentAlignment"/> is explicitly set, in which case it's
        /// a derived <see cref="Style"/> (based on <see cref="HeaderStyle"/>) that adds the
        /// alignment setter. The grid writes this onto the generated
        /// <see cref="DataGridColumn.HeaderStyle"/>.
        /// </summary>
        public Style ActualHeaderStyle => (Style)GetValue(ActualHeaderStyleProperty);

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> rendered as the header content. When set,
        /// the template's <c>DataContext</c> is the column descriptor's <see cref="Header"/>
        /// value (matching WPF's <see cref="DataGridColumn.HeaderTemplate"/> semantics).
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(
                nameof(HeaderTemplate),
                typeof(DataTemplate),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnHeaderStylingChanged));

        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="DataTemplateSelector"/> used to choose the header template
        /// per-column. <see cref="ActualHeaderTemplateSelector"/> exposes the resolved value
        /// (currently mirrors <see cref="HeaderTemplateSelector"/>; reserved for future
        /// grid-level default selection).
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register(
                nameof(HeaderTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnHeaderStylingChanged));

        public DataTemplateSelector HeaderTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty);
            set => SetValue(HeaderTemplateSelectorProperty, value);
        }

        private static readonly DependencyPropertyKey ActualHeaderTemplateSelectorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualHeaderTemplateSelector),
                typeof(DataTemplateSelector),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHeaderTemplateSelectorProperty = ActualHeaderTemplateSelectorPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved header template selector. Mirrors <see cref="HeaderTemplateSelector"/>
        /// today; reserved for future grid-level default resolution.
        /// </summary>
        public DataTemplateSelector ActualHeaderTemplateSelector => (DataTemplateSelector)GetValue(ActualHeaderTemplateSelectorProperty);

        /// <summary>
        /// Gets or sets the horizontal alignment of the header content within the header cell.
        /// When set explicitly, the grid synthesizes a <see cref="DataGridColumnHeader"/> style
        /// (derived from <see cref="HeaderStyle"/>) that includes the matching
        /// <see cref="Control.HorizontalContentAlignmentProperty"/> setter and assigns it to
        /// <see cref="ActualHeaderStyle"/>.
        /// </summary>
        public static readonly DependencyProperty HorizontalHeaderContentAlignmentProperty =
            DependencyProperty.Register(
                nameof(HorizontalHeaderContentAlignment),
                typeof(HorizontalAlignment),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(HorizontalAlignment.Left, OnHeaderStylingChanged));

        public HorizontalAlignment HorizontalHeaderContentAlignment
        {
            get => (HorizontalAlignment)GetValue(HorizontalHeaderContentAlignmentProperty);
            set => SetValue(HorizontalHeaderContentAlignmentProperty, value);
        }

        private static readonly DependencyPropertyKey ActualHeaderWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualHeaderWidth),
                typeof(double),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ActualHeaderWidthProperty = ActualHeaderWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved rendered width of the column header. Mirrors <see cref="ActualWidth"/> in
        /// WPF (the header occupies the full column width); kept as a separate DP for spec
        /// compatibility and to leave room for header-strip scenarios where header sizing may
        /// diverge from cell sizing.
        /// </summary>
        public double ActualHeaderWidth => (double)GetValue(ActualHeaderWidthProperty);

        internal void SetActualHeaderWidth(double value)
            => SetValue(ActualHeaderWidthPropertyKey, value);

        /// <summary>
        /// Gets or sets the tooltip content for the column header. Routed through
        /// <see cref="ActualHeaderStyle"/> as a setter for <see cref="FrameworkElement.ToolTipProperty"/>.
        /// When <see cref="HeaderToolTipTemplate"/> is also set, this value becomes the templated
        /// <see cref="ToolTip"/>'s <c>Content</c>.
        /// </summary>
        public static readonly DependencyProperty HeaderToolTipProperty =
            DependencyProperty.Register(
                nameof(HeaderToolTip),
                typeof(object),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnHeaderStylingChanged));

        public object HeaderToolTip
        {
            get => GetValue(HeaderToolTipProperty);
            set => SetValue(HeaderToolTipProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> applied to the header tooltip. The
        /// templated <see cref="ToolTip"/>'s <c>Content</c> is <see cref="HeaderToolTip"/> when
        /// non-null, otherwise the descriptor itself — so templates can bind to <c>GridColumn</c>
        /// properties (<c>HeaderCaption</c>, <c>FieldName</c>, etc.) without consumers having to
        /// route them through <see cref="HeaderToolTip"/>.
        /// </summary>
        public static readonly DependencyProperty HeaderToolTipTemplateProperty =
            DependencyProperty.Register(
                nameof(HeaderToolTipTemplate),
                typeof(DataTemplate),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnHeaderStylingChanged));

        public DataTemplate HeaderToolTipTemplate
        {
            get => (DataTemplate)GetValue(HeaderToolTipTemplateProperty);
            set => SetValue(HeaderToolTipTemplateProperty, value);
        }

        private static readonly DependencyPropertyKey ActualHeaderToolTipTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualHeaderToolTipTemplate),
                typeof(DataTemplate),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActualHeaderToolTipTemplateProperty = ActualHeaderToolTipTemplatePropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved header tooltip template. Mirrors <see cref="HeaderToolTipTemplate"/> today;
        /// reserved for future grid-level default resolution.
        /// </summary>
        public DataTemplate ActualHeaderToolTipTemplate => (DataTemplate)GetValue(ActualHeaderToolTipTemplateProperty);

        private static void OnHeaderStylingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnLayoutBase bc) return;
            bc.RefreshActualHeaderStyling();
            bc.SyncToInternalColumn();
        }

        /// <summary>
        /// Recomputes <see cref="ActualHeaderStyle"/> and <see cref="ActualHeaderTemplateSelector"/>
        /// from the current source DPs. Synthesizes a derived header style when
        /// <see cref="HorizontalHeaderContentAlignment"/> is explicitly set.
        /// </summary>
        private void RefreshActualHeaderStyling()
        {
            SetValue(ActualHeaderStylePropertyKey, ResolveActualHeaderStyle());
            SetValue(ActualHeaderTemplateSelectorPropertyKey, HeaderTemplateSelector);
            SetValue(ActualHeaderToolTipTemplatePropertyKey, HeaderToolTipTemplate);
        }

        private Style ResolveActualHeaderStyle()
        {
            bool alignmentExplicit =
                DependencyPropertyHelper.GetValueSource(this, HorizontalHeaderContentAlignmentProperty)
                    .BaseValueSource != BaseValueSource.Default;
            bool hasToolTip = HeaderToolTip != null || HeaderToolTipTemplate != null;

            if (!alignmentExplicit && !hasToolTip)
                return HeaderStyle;

            // Synthesize a style based on the user's HeaderStyle that layers in the alignment
            // setter and/or tooltip. Same pattern as the cell-side stretching wrap.
            var style = new Style(typeof(DataGridColumnHeader), HeaderStyle);

            if (alignmentExplicit)
                style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalHeaderContentAlignment));

            if (hasToolTip)
                style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, BuildHeaderToolTipValue()));

            return style;
        }

        /// <summary>
        /// Resolves the value pushed to <see cref="FrameworkElement.ToolTipProperty"/> on the
        /// header style. Bare <see cref="HeaderToolTip"/> rides as the value directly (WPF's
        /// tooltip framework wraps it in a default <see cref="ToolTip"/>); when
        /// <see cref="HeaderToolTipTemplate"/> is set, returns a templated <see cref="ToolTip"/>
        /// whose <c>Content</c> is the user-supplied <see cref="HeaderToolTip"/> if any,
        /// otherwise the descriptor itself — so the template binds to <c>GridColumn</c>
        /// properties (<c>HeaderCaption</c>, <c>FieldName</c>, etc.) by default.
        /// </summary>
        private object BuildHeaderToolTipValue()
        {
            if (HeaderToolTipTemplate == null)
                return HeaderToolTip;

            return new ToolTip
            {
                Content = HeaderToolTip ?? (object)this,
                ContentTemplate = HeaderToolTipTemplate,
            };
        }

        #endregion

        #region Size

        /// <summary>
        /// Gets or sets the column width.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(DataGridLength),
                typeof(ColumnLayoutBase),
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
                typeof(ColumnLayoutBase),
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
                typeof(ColumnLayoutBase),
                new PropertyMetadata(double.PositiveInfinity, OnLayoutPropertyChanged));

        public double MaxWidth
        {
            get => (double)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        private static readonly DependencyPropertyKey ActualWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualWidth),
                typeof(double),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(0.0));

        /// <summary>
        /// Read-only dependency property exposing <see cref="ActualWidth"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ActualWidthProperty = ActualWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved rendered width of the column. Pushed by <see cref="SearchDataGrid"/> from the
        /// generated <see cref="DataGridColumn.ActualWidth"/>; <c>0</c> until the column has
        /// participated in a measure pass.
        /// </summary>
        public double ActualWidth => (double)GetValue(ActualWidthProperty);

        internal void SetActualWidth(double value)
            => SetValue(ActualWidthPropertyKey, value);

        private static readonly DependencyPropertyKey ActualDataWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualDataWidth),
                typeof(double),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(0.0));

        /// <summary>
        /// Read-only dependency property exposing <see cref="ActualDataWidth"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ActualDataWidthProperty = ActualDataWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// Width of the column's widest measured data content, including cell chrome (padding /
        /// borders). Pushed by <see cref="SearchDataGrid"/> best-fit runs; <c>0</c> until the
        /// column has been best-fit at least once. Not live-tracked on data changes.
        /// </summary>
        public double ActualDataWidth => (double)GetValue(ActualDataWidthProperty);

        internal void SetActualDataWidth(double value)
            => SetValue(ActualDataWidthPropertyKey, value);

        /// <summary>
        /// Column-level override for the grid's <see cref="SearchDataGrid.AllowBestFit"/>.
        /// <c>null</c> (default) inherits the grid value; <c>true</c>/<c>false</c> overrides it
        /// for this column only. The resolved value is exposed by
        /// <see cref="ActualAllowBestFit"/> and gates the best-fit UI surfaces (context-menu
        /// items, gripper double-click) — not the explicit
        /// <see cref="SearchDataGrid.BestFitColumn(GridColumn)"/> API.
        /// </summary>
        public static readonly DependencyProperty AllowBestFitProperty =
            DependencyProperty.Register(
                nameof(AllowBestFit),
                typeof(bool?),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnAllowBestFitChanged));

        public bool? AllowBestFit
        {
            get => (bool?)GetValue(AllowBestFitProperty);
            set => SetValue(AllowBestFitProperty, value);
        }

        private static readonly DependencyPropertyKey ActualAllowBestFitPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualAllowBestFit),
                typeof(bool),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ActualAllowBestFitProperty = ActualAllowBestFitPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved best-fit availability for this column: the explicit
        /// <see cref="AllowBestFit"/> when set, otherwise the grid-level
        /// <see cref="SearchDataGrid.AllowBestFit"/> (defaulting to <c>true</c> when no grid is
        /// attached).
        /// </summary>
        public bool ActualAllowBestFit => (bool)GetValue(ActualAllowBestFitProperty);

        /// <summary>
        /// Resolves the effective best-fit availability — column override first, then the grid,
        /// then <c>true</c>.
        /// </summary>
        internal bool ResolveEffectiveAllowBestFit()
            => AllowBestFit ?? View?.AllowBestFit ?? true;

        /// <summary>
        /// Recomputes <see cref="ActualAllowBestFit"/>. Called when the column override changes,
        /// when the column attaches to a grid (<see cref="View"/> change), and when the grid's
        /// <see cref="SearchDataGrid.AllowBestFit"/> changes (the grid walks columns).
        /// </summary>
        internal void RefreshActualAllowBestFit()
            => SetValue(ActualAllowBestFitPropertyKey, ResolveEffectiveAllowBestFit());

        private static void OnAllowBestFitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnLayoutBase col)
                col.RefreshActualAllowBestFit();
        }

        /// <summary>
        /// How many rows this column's best-fit measures.
        /// <see cref="WPF.BestFitMode.Default"/> (the default) inherits the grid's
        /// <see cref="SearchDataGrid.BestFitMode"/>.
        /// </summary>
        public static readonly DependencyProperty BestFitModeProperty =
            DependencyProperty.Register(
                nameof(BestFitMode),
                typeof(BestFitMode),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(BestFitMode.Default));

        public BestFitMode BestFitMode
        {
            get => (BestFitMode)GetValue(BestFitModeProperty);
            set => SetValue(BestFitModeProperty, value);
        }

        /// <summary>
        /// Which parts of the column participate in best-fit: header, data rows, or both
        /// (default).
        /// </summary>
        public static readonly DependencyProperty BestFitAreaProperty =
            DependencyProperty.Register(
                nameof(BestFitArea),
                typeof(BestFitArea),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(BestFitArea.All));

        public BestFitArea BestFitArea
        {
            get => (BestFitArea)GetValue(BestFitAreaProperty);
            set => SetValue(BestFitAreaProperty, value);
        }

        /// <summary>
        /// Cap on the rows scanned by an <see cref="WPF.BestFitMode.AllRows"/> best-fit pass.
        /// Negative (default) = unlimited.
        /// </summary>
        public static readonly DependencyProperty BestFitMaxRowCountProperty =
            DependencyProperty.Register(
                nameof(BestFitMaxRowCount),
                typeof(int),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(-1));

        public int BestFitMaxRowCount
        {
            get => (int)GetValue(BestFitMaxRowCountProperty);
            set => SetValue(BestFitMaxRowCountProperty, value);
        }

        #endregion

        #region Visibility & Position

        /// <summary>
        /// Gets or sets whether the column is visible.
        /// </summary>
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register(
                nameof(Visible),
                typeof(bool),
                typeof(ColumnLayoutBase),
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
                typeof(ColumnLayoutBase),
                new PropertyMetadata(-1));

        public int VisibleIndex
        {
            get => (int)GetValue(VisibleIndexProperty);
            set => SetValue(VisibleIndexProperty, value);
        }

        private static readonly DependencyPropertyKey ActualVisibleIndexPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualVisibleIndex),
                typeof(int),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(-1));

        /// <summary>
        /// Read-only dependency property exposing <see cref="ActualVisibleIndex"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ActualVisibleIndexProperty = ActualVisibleIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved position among visible columns, pushed by <see cref="SearchDataGrid"/> after
        /// column generation/reordering. <c>-1</c> when the column is not visible or has not
        /// been ordered yet.
        /// </summary>
        public int ActualVisibleIndex => (int)GetValue(ActualVisibleIndexProperty);

        internal void SetActualVisibleIndex(int value)
            => SetValue(ActualVisibleIndexPropertyKey, value);

        private static readonly DependencyPropertyKey ActualCollectionIndexPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualCollectionIndex),
                typeof(int),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(-1));

        /// <summary>
        /// Read-only dependency property exposing <see cref="ActualCollectionIndex"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ActualCollectionIndexProperty = ActualCollectionIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// Resolved zero-based index of the column in the owning grid's column collection,
        /// regardless of visibility. <c>-1</c> until the column has been attached to a grid.
        /// </summary>
        public int ActualCollectionIndex => (int)GetValue(ActualCollectionIndexProperty);

        internal void SetActualCollectionIndex(int value)
            => SetValue(ActualCollectionIndexPropertyKey, value);

        private static readonly DependencyPropertyKey ColumnPositionPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnPosition),
                typeof(ColumnPositionKind),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(ColumnPositionKind.None, OnColumnPositionChanged));

        /// <summary>
        /// Read-only dependency property exposing <see cref="ColumnPosition"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ColumnPositionProperty = ColumnPositionPropertyKey.DependencyProperty;

        /// <summary>
        /// Position of this column within the visible-column run — First / Middle / Last /
        /// Single — for templates that want to light up edge styling without per-column
        /// boilerplate. Updated by <see cref="SearchDataGrid"/> whenever the visible-column
        /// order changes.
        /// </summary>
        public ColumnPositionKind ColumnPosition => (ColumnPositionKind)GetValue(ColumnPositionProperty);

        internal void SetColumnPosition(ColumnPositionKind value)
            => SetValue(ColumnPositionPropertyKey, value);

        private static readonly DependencyPropertyKey IsFirstPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFirst),
                typeof(bool),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsFirstProperty = IsFirstPropertyKey.DependencyProperty;

        /// <summary>
        /// True when the column is at the leading edge of the visible-column run
        /// (<see cref="ColumnPosition"/> is <see cref="ColumnPositionKind.First"/> or
        /// <see cref="ColumnPositionKind.Single"/>). Derived automatically from
        /// <see cref="ColumnPosition"/>.
        /// </summary>
        public bool IsFirst => (bool)GetValue(IsFirstProperty);

        private static readonly DependencyPropertyKey IsLastPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsLast),
                typeof(bool),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsLastProperty = IsLastPropertyKey.DependencyProperty;

        /// <summary>
        /// True when the column is at the trailing edge of the visible-column run
        /// (<see cref="ColumnPosition"/> is <see cref="ColumnPositionKind.Last"/> or
        /// <see cref="ColumnPositionKind.Single"/>). Derived automatically from
        /// <see cref="ColumnPosition"/>.
        /// </summary>
        public bool IsLast => (bool)GetValue(IsLastProperty);

        private static void OnColumnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnLayoutBase bc) return;
            var pos = (ColumnPositionKind)e.NewValue;
            bool first = pos == ColumnPositionKind.First || pos == ColumnPositionKind.Single;
            bool last = pos == ColumnPositionKind.Last || pos == ColumnPositionKind.Single;
            bc.SetValue(IsFirstPropertyKey, first);
            bc.SetValue(IsLastPropertyKey, last);
        }

        /// <summary>
        /// Gets or sets the column's pinned position. <see cref="FixedColumnPosition.Left"/>
        /// pins the column to the left edge of the grid via
        /// <see cref="System.Windows.Controls.DataGrid.FrozenColumnCount"/>;
        /// <see cref="FixedColumnPosition.Right"/> pins the column to the right edge of the
        /// viewport — ordered after every unpinned column and anchored while they scroll
        /// beneath it; <see cref="FixedColumnPosition.None"/>
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
                typeof(ColumnLayoutBase),
                new PropertyMetadata(FixedColumnPosition.None, OnFixedChanged));

        public FixedColumnPosition Fixed
        {
            get => (FixedColumnPosition)GetValue(FixedProperty);
            set => SetValue(FixedProperty, value);
        }

        #endregion

        #region User Interaction

        /// <summary>
        /// Gets or sets whether the user can drag-reorder the column.
        /// </summary>
        public static readonly DependencyProperty AllowMovingProperty =
            DependencyProperty.Register(
                nameof(AllowMoving),
                typeof(bool),
                typeof(ColumnLayoutBase),
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
                typeof(ColumnLayoutBase),
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
                typeof(ColumnLayoutBase),
                new PropertyMetadata(true));

        public bool ShowInColumnChooser
        {
            get => (bool)GetValue(ShowInColumnChooserProperty);
            set => SetValue(ShowInColumnChooserProperty, value);
        }

        #endregion

        #region Auto-generation flag

        private static readonly DependencyPropertyKey IsAutoGeneratedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsAutoGenerated),
                typeof(bool),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(false));

        /// <summary>
        /// Read-only dependency property exposing <see cref="IsAutoGenerated"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty IsAutoGeneratedProperty = IsAutoGeneratedPropertyKey.DependencyProperty;

        /// <summary>
        /// True when the column was auto-generated by the grid from the data source, false when
        /// declared explicitly in XAML / code. Bindable so styles can differentiate auto-generated
        /// columns visually.
        /// </summary>
        public bool IsAutoGenerated => (bool)GetValue(IsAutoGeneratedProperty);

        internal void SetIsAutoGenerated(bool value)
            => SetValue(IsAutoGeneratedPropertyKey, value);

        #endregion

        #region Grid Back-Pointer

        private static readonly DependencyPropertyKey ViewPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(View),
                typeof(SearchDataGrid),
                typeof(ColumnLayoutBase),
                new PropertyMetadata(null, OnViewPropertyChanged));

        private static void OnViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var col = (ColumnLayoutBase)d;
            // Base-tier resolved mirrors that fall back to grid-level defaults — refreshed here
            // rather than inside OnViewChanged so derived overrides can't skip them.
            col.RefreshActualAllowBestFit();
            col.OnViewChanged();
        }

        /// <summary>
        /// Called when <see cref="View"/> is set or cleared. Derived tiers override to refresh
        /// state that depends on the grid back-pointer (e.g. <see cref="ColumnDataBase"/> re-resolves
        /// <c>ActualCellStyle</c> when its grid-default fallback may have changed).
        /// </summary>
        protected virtual void OnViewChanged() { }

        /// <summary>
        /// Read-only dependency property exposing <see cref="View"/> for bindings.
        /// </summary>
        public static readonly DependencyProperty ViewProperty = ViewPropertyKey.DependencyProperty;

        /// <summary>
        /// Back-pointer to the parent <see cref="SearchDataGrid"/> that owns this column
        /// descriptor. Set by the grid when the descriptor enters/leaves <c>GridColumns</c>;
        /// <c>null</c> while detached.
        /// </summary>
        public SearchDataGrid View => (SearchDataGrid)GetValue(ViewProperty);

        internal void SetView(SearchDataGrid value)
            => SetValue(ViewPropertyKey, value);

        /// <summary>
        /// Gets the WPF <see cref="DataGridColumn"/> that was generated from this descriptor.
        /// Set internally after the column factory runs. (Still a CLR property — the rendered
        /// column is grid plumbing, not consumer-facing binding state.)
        /// </summary>
        public DataGridColumn InternalColumn { get; internal set; }

        #endregion

        #region Internal Column Sync

        /// <summary>
        /// Pushes the descriptor's current property values onto the generated WPF
        /// <see cref="DataGridColumn"/>. No-op when the column hasn't been generated yet.
        /// </summary>
        internal void SyncToInternalColumn()
        {
            if (InternalColumn == null) return;

            InternalColumn.Header = EffectiveHeader;
            InternalColumn.Width = Width;
            InternalColumn.MinWidth = MinWidth;
            InternalColumn.MaxWidth = MaxWidth;
            InternalColumn.Visibility = Visible ? Visibility.Visible : Visibility.Collapsed;
            InternalColumn.CanUserResize = AllowResizing;
            InternalColumn.CanUserReorder = AllowMoving;

            // Setting a null value would override the grid-level ColumnHeaderStyle /
            // ColumnHeaderTemplate fallback (WPF's transfer-property coercion only fires when
            // the column-level value source is Default, not Local-null). ClearValue restores
            // the unset state so the grid's defaults flow through.
            SetOrClear(InternalColumn, DataGridColumn.HeaderStyleProperty, ActualHeaderStyle);
            SetOrClear(InternalColumn, DataGridColumn.HeaderTemplateProperty, HeaderTemplate);
            SetOrClear(InternalColumn, DataGridColumn.HeaderTemplateSelectorProperty, ActualHeaderTemplateSelector);

            SyncDerivedToInternalColumn(InternalColumn);
        }

        /// <summary>
        /// Writes <paramref name="value"/> to <paramref name="property"/> on <paramref name="target"/>
        /// when non-null; clears it (back to the default value source) otherwise. Used for properties
        /// that participate in WPF's column → grid transfer-property coercion (e.g.
        /// <see cref="DataGridColumn.HeaderStyle"/> → <see cref="DataGrid.ColumnHeaderStyle"/>) — a
        /// literal-null write would block the grid-level fallback.
        /// </summary>
        protected static void SetOrClear(DependencyObject target, DependencyProperty property, object value)
        {
            if (value != null)
                target.SetValue(property, value);
            else
                target.ClearValue(property);
        }

        /// <summary>
        /// Resolved header value pushed onto the internal <see cref="DataGridColumn"/>. Derived
        /// tiers override to add fallbacks (e.g. <see cref="ColumnDataBase"/> falls back to
        /// <c>FieldName</c> when <see cref="Header"/> is null).
        /// </summary>
        protected virtual object EffectiveHeader => Header;

        /// <summary>
        /// Hook for derived tiers to push their own properties onto the generated
        /// <see cref="DataGridColumn"/>. Called at the end of <see cref="SyncToInternalColumn"/>.
        /// </summary>
        protected virtual void SyncDerivedToInternalColumn(DataGridColumn col) { }

        #endregion

        #region Property Changed Callbacks

        /// <summary>
        /// Called when any layout-related property changes (Width / MinWidth / MaxWidth /
        /// AllowMoving / AllowResizing). Syncs to the internal DataGridColumn.
        /// </summary>
        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnLayoutBase bc)
                bc.SyncToInternalColumn();
        }

        /// <summary>
        /// Called when <see cref="Header"/> changes. Syncs the internal column and refreshes the
        /// derived <see cref="HeaderCaption"/> projection so bindings to it see the new value.
        /// </summary>
        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnLayoutBase bc) return;
            bc.SyncToInternalColumn();
            bc.RefreshHeaderCaption();
        }

        /// <summary>
        /// Called when <see cref="Visible"/> changes. Only syncs visibility to the internal column
        /// without resetting other properties like Width, which can corrupt DataGrid scroll metrics
        /// during bulk visibility changes (e.g., column chooser Select All).
        /// </summary>
        private static void OnVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnLayoutBase bc && bc.InternalColumn != null)
                bc.InternalColumn.Visibility = bc.Visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnFixedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnLayoutBase bc)
                bc.View?.ApplyFixedColumnLayout();
        }

        #endregion
    }
}
