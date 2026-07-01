using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// The edit-form surface shown for the row open in full-row edit mode when the grid's
    /// <see cref="SearchDataGrid.EditFormShowMode"/> is not <see cref="EditFormShowMode.None"/>.
    /// Renders caption/editor pairs in a form layout — either an auto-generated layout built from
    /// the grid's columns, or the developer-supplied <see cref="CustomTemplate"/> — plus a docked
    /// Update / Cancel action bar that calls back into the owning <see cref="SearchDataGrid"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each per-field editor binds its <see cref="ContentControl.Content"/> to
    /// <see cref="EditingItem"/> (so the editor's <c>DataContext</c> is the row item and its
    /// two-way bindings write straight to the item under the grid's open
    /// <see cref="System.ComponentModel.IEditableObject"/> row transaction) and uses the column's
    /// effective edit-form template (display template when the column is read-only). This is the
    /// same reuse the <see cref="RowEditPresenter"/> strip relies on.
    /// </para>
    /// <para>
    /// Dirty tracking is generic: the presenter listens for the bubbling change events of the
    /// standard editor primitives (text, toggle, selector) anywhere in its subtree, so editors
    /// realized from column templates are covered without per-editor wiring. The first real change
    /// after the initial value seed sets <see cref="IsDirty"/>, which the grid reads for the
    /// focus-leave confirmation (<see cref="SearchDataGrid.EditFormPostConfirmationMode"/>).
    /// </para>
    /// </remarks>
    public class EditFormPresenter : Control
    {
        static EditFormPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(EditFormPresenter),
                new FrameworkPropertyMetadata(typeof(EditFormPresenter)));
        }

        /// <summary>Template part name of the content host that carries the form body.</summary>
        public const string PartFormHostName = "PART_FormHost";

        /// <summary>Template part name of the commit button.</summary>
        public const string PartUpdateButtonName = "PART_UpdateButton";

        /// <summary>Template part name of the cancel button.</summary>
        public const string PartCancelButtonName = "PART_CancelButton";

        private ContentControl _formHost;
        private ButtonBase _updateButton;
        private ButtonBase _cancelButton;

        // The per-field editor cells of the auto layout, tagged with their owning DataGridColumn so
        // focus can be handed off to the field the user was on. Empty for a custom template.
        private readonly List<ContentControl> _editorCells = new();

        // Suppresses dirty marking while the editors seed their initial bound values (the binding
        // that sets a TextBox.Text / Selector.SelectedItem raises a change during setup).
        private bool _suppressDirty;

        // Auto-layout sizing: a field block's base width (scaled by EditFormColumnSpan) and an
        // editor's base height (scaled by EditFormRowSpan).
        private const double FieldBaseWidth = 200;
        private const double EditorBaseHeight = 26;

        // Subtracted from the grid's width when capping the form to the visible data area — covers the
        // vertical scrollbar and the grid's border so the form never forces a horizontal scroll.
        private const double ViewportWidthAllowance = 24;

        // The grid we're tracking for size changes, so MaxWidth follows the viewport. Hooked/unhooked
        // as OwnerGrid changes and on unload (avoids leaking the presenter via the grid's event).
        private SearchDataGrid _sizeHookedGrid;

        public EditFormPresenter()
        {
            // Mark this subtree as a bordered editor host: every editor the form hosts inherits this
            // and renders its own border, while the same editor templates stay flat in a grid cell.
            EditorChrome.SetShowEditorBorder(this, true);

            Loaded += (_, _) => UpdateMaxWidth();
            Unloaded += (_, _) => HookOwnerGridSize(null);

            // Listen for the bubbling change events of the standard editor primitives so any editor
            // realized from a column template marks the form dirty without per-editor wiring.
            // handledEventsToo: a Selector's SelectionChanged is sometimes marked handled before it
            // reaches us.
            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnEditorChanged), true);
            AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnEditorChanged), true);
            AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnEditorChanged), true);
            AddHandler(ToggleButton.IndeterminateEvent, new RoutedEventHandler(OnEditorChanged), true);
            AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(OnEditorChanged), true);
        }

        #region Dependency Properties

        /// <summary>
        /// The row item currently being edited. Every per-field editor binds its content to this,
        /// so the editors re-target whenever it changes (no rebuild needed).
        /// </summary>
        public static readonly DependencyProperty EditingItemProperty =
            DependencyProperty.Register(nameof(EditingItem), typeof(object), typeof(EditFormPresenter),
                new PropertyMetadata(null, OnEditingItemChanged));

        public object EditingItem
        {
            get => GetValue(EditingItemProperty);
            set => SetValue(EditingItemProperty, value);
        }

        /// <summary>The grid that owns this form — supplies the columns and the commit/cancel calls.</summary>
        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(nameof(OwnerGrid), typeof(SearchDataGrid), typeof(EditFormPresenter),
                new PropertyMetadata(null, OnOwnerGridChanged));

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }

        /// <summary>Optional caption shown in the form's header bar (e.g. a row title).</summary>
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(object), typeof(EditFormPresenter),
                new PropertyMetadata(null));

        public object Caption
        {
            get => GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        /// <summary>Optional template for the header caption.</summary>
        public static readonly DependencyProperty CaptionTemplateProperty =
            DependencyProperty.Register(nameof(CaptionTemplate), typeof(DataTemplate), typeof(EditFormPresenter),
                new PropertyMetadata(null));

        public DataTemplate CaptionTemplate
        {
            get => (DataTemplate)GetValue(CaptionTemplateProperty);
            set => SetValue(CaptionTemplateProperty, value);
        }

        /// <summary>
        /// Developer-supplied full form layout (the grid's <see cref="SearchDataGrid.EditFormTemplate"/>).
        /// When set, it replaces the auto-generated layout; its <c>DataContext</c> is the editing
        /// item, and any <see cref="EditFormEditor"/> elements inside it resolve their field editors
        /// against the owning grid's columns.
        /// </summary>
        public static readonly DependencyProperty CustomTemplateProperty =
            DependencyProperty.Register(nameof(CustomTemplate), typeof(DataTemplate), typeof(EditFormPresenter),
                new PropertyMetadata(null, OnCustomTemplateChanged));

        public DataTemplate CustomTemplate
        {
            get => (DataTemplate)GetValue(CustomTemplateProperty);
            set => SetValue(CustomTemplateProperty, value);
        }

        #endregion

        /// <summary>True once the user has changed a value in the form since the last reset.</summary>
        public bool IsDirty { get; private set; }

        /// <summary>Clears the dirty flag — called by the grid when a fresh edit session begins.</summary>
        internal void ResetDirty() => IsDirty = false;

        private static void OnEditingItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EditFormPresenter p)
            {
                p.IsDirty = false;
                p.SeedSuppression();
            }
        }

        private static void OnOwnerGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not EditFormPresenter p)
                return;
            p.HookOwnerGridSize(e.NewValue as SearchDataGrid);
            p.UpdateMaxWidth();
            p.TryBuildForm();
        }

        private void HookOwnerGridSize(SearchDataGrid grid)
        {
            if (ReferenceEquals(_sizeHookedGrid, grid))
                return;
            if (_sizeHookedGrid != null)
                _sizeHookedGrid.SizeChanged -= OnOwnerGridSizeChanged;
            _sizeHookedGrid = grid;
            if (_sizeHookedGrid != null)
                _sizeHookedGrid.SizeChanged += OnOwnerGridSizeChanged;
        }

        private void OnOwnerGridSizeChanged(object sender, SizeChangedEventArgs e) => UpdateMaxWidth();

        /// <summary>
        /// Caps the form to the grid's visible data-area width (grid width minus the row-header gutter
        /// and a scrollbar/border allowance). Row details are measured with unconstrained width, so
        /// without this cap the wrapped layout would lay out on one line and push the row wider than
        /// the viewport. With it, the WrapPanel measures against a finite width and wraps.
        /// </summary>
        private void UpdateMaxWidth()
        {
            if (OwnerGrid == null)
            {
                ClearValue(MaxWidthProperty);
                return;
            }

            double available = OwnerGrid.ActualWidth - OwnerGrid.RowHeaderActualWidth - ViewportWidthAllowance;
            MaxWidth = available > 0 ? available : double.PositiveInfinity;
        }

        private static void OnCustomTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as EditFormPresenter)?.TryBuildForm();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_updateButton != null) _updateButton.Click -= OnUpdateClick;
            if (_cancelButton != null) _cancelButton.Click -= OnCancelClick;

            _formHost = GetTemplateChild(PartFormHostName) as ContentControl;
            _updateButton = GetTemplateChild(PartUpdateButtonName) as ButtonBase;
            _cancelButton = GetTemplateChild(PartCancelButtonName) as ButtonBase;

            if (_updateButton != null) _updateButton.Click += OnUpdateClick;
            if (_cancelButton != null) _cancelButton.Click += OnCancelClick;

            TryBuildForm();
        }

        private void OnUpdateClick(object sender, RoutedEventArgs e) => OwnerGrid?.CommitEditForm();

        private void OnCancelClick(object sender, RoutedEventArgs e) => OwnerGrid?.CancelEditForm();

        private void OnEditorChanged(object sender, RoutedEventArgs e)
        {
            if (!_suppressDirty)
                IsDirty = true;
        }

        /// <summary>
        /// Suppresses dirty marking until the editor value seed settles. The editors realize and
        /// bind their initial values during the layout pass that follows; we clear the flag after
        /// that pass so only genuine user changes mark the form dirty.
        /// </summary>
        private void SeedSuppression()
        {
            _suppressDirty = true;
            Dispatcher.BeginInvoke(new System.Action(() => _suppressDirty = false), DispatcherPriority.Background);
        }

        /// <summary>
        /// (Re)builds the form body into <c>PART_FormHost</c>. Hosts the custom template when set,
        /// otherwise the auto-generated layout. No-op until the host part and owner grid exist.
        /// </summary>
        private void TryBuildForm()
        {
            if (_formHost == null || OwnerGrid == null)
                return;

            _editorCells.Clear();
            SeedSuppression();

            if (CustomTemplate != null)
            {
                _formHost.ContentTemplate = CustomTemplate;
                _formHost.SetBinding(ContentControl.ContentProperty,
                    new Binding(nameof(EditingItem)) { Source = this });
                return;
            }

            _formHost.ContentTemplate = null;
            _formHost.Content = BuildAutoLayout();
        }

        /// <summary>
        /// Builds the auto-generated form: a <see cref="WrapPanel"/> of caption-over-editor field
        /// blocks over the grid's edit-form-visible columns in display order, so fields flow across
        /// the available width and wrap instead of stacking in a single column.
        /// <see cref="ColumnDataBase.EditFormColumnSpan"/> scales a field's width;
        /// <see cref="ColumnDataBase.EditFormRowSpan"/> scales a tall editor's height.
        /// </summary>
        private Panel BuildAutoLayout()
        {
            var panel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 6, 8, 6) };

            var columns = OwnerGrid.GridColumns
                .Where(c => c != null && c.ResolveEffectiveEditFormVisible())
                .OrderBy(c => c.InternalColumn?.DisplayIndex ?? int.MaxValue)
                .ToList();

            foreach (var descriptor in columns)
                panel.Children.Add(BuildFieldBlock(descriptor));

            return panel;
        }

        /// <summary>
        /// A single field in the wrapped layout: the caption above its editor. The block's width
        /// scales with <see cref="ColumnDataBase.EditFormColumnSpan"/> so wider fields claim more of
        /// the row. The editor renders its own border via the inherited
        /// <see cref="EditorChrome.ShowEditorBorderProperty"/> the presenter sets — read-only and
        /// checkbox editors carry no border trigger and stay flat.
        /// </summary>
        private FrameworkElement BuildFieldBlock(ColumnDataBase descriptor)
        {
            int span = descriptor.EditFormColumnSpan < 1 ? 1 : descriptor.EditFormColumnSpan;
            int rowSpan = descriptor.EditFormRowSpan < 1 ? 1 : descriptor.EditFormRowSpan;

            var block = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(6, 4, 6, 4),
                Width = FieldBaseWidth * span,
            };

            block.Children.Add(BuildCaption(descriptor.ResolveEffectiveEditFormCaption()));

            var editor = BuildEditorCell(descriptor);
            if (rowSpan > 1)
                editor.MinHeight = EditorBaseHeight * rowSpan;
            block.Children.Add(editor);

            return block;
        }

        private static TextBlock BuildCaption(string text) => new()
        {
            Text = string.IsNullOrEmpty(text) ? string.Empty : text,
            Margin = new Thickness(2, 0, 2, 3),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
        };

        /// <summary>
        /// Builds the editor cell for <paramref name="descriptor"/>: a <see cref="ContentControl"/>
        /// whose content binds to <see cref="EditingItem"/> and whose template is the column's
        /// effective edit-form template (display template when the column is read-only). Tagged with
        /// the owning <see cref="DataGridColumn"/> so focus handoff can find it.
        /// </summary>
        private ContentControl BuildEditorCell(ColumnDataBase descriptor)
        {
            var cell = new ContentControl
            {
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                MinHeight = EditorBaseHeight,
                Tag = descriptor.InternalColumn,
            };
            cell.SetBinding(ContentControl.ContentProperty, new Binding(nameof(EditingItem)) { Source = this });

            bool readOnly = descriptor.InternalColumn?.IsReadOnly ?? descriptor.IsReadOnly;
            DataTemplate template = readOnly
                ? descriptor.ResolveEffectiveCellDisplayTemplate()
                : descriptor.ResolveEffectiveEditFormCellTemplate();
            template ??= descriptor.ResolveEffectiveCellDisplayTemplate();

            if (template != null)
                cell.ContentTemplate = template;

            _editorCells.Add(cell);
            return cell;
        }

        /// <summary>
        /// Moves keyboard focus to the editor hosting <paramref name="column"/> — used to hand off
        /// from the cell the user clicked to its counterpart in the form. Falls back to the first
        /// focusable element in the form body (covers a custom template, which has no tagged cells).
        /// </summary>
        internal void FocusEditorForColumn(DataGridColumn column)
        {
            ContentControl target = null;
            if (column != null)
                target = _editorCells.FirstOrDefault(c => ReferenceEquals(c.Tag, column));
            target ??= _editorCells.FirstOrDefault();

            if (target != null)
                FocusFirstFocusable(target);
            else if (_formHost != null)
                FocusFirstFocusable(_formHost);
        }

        private static void FocusFirstFocusable(DependencyObject root)
        {
            var editor = FindFocusable(root);
            editor?.Focus();
        }

        private static IInputElement FindFocusable(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                // Known editor types first; then any tab-stoppable Control (covers custom editors
                // like NumericUpDown / the segmented date editor) — but not the bare focusable
                // containers WPF stamps between the cell and its real input element.
                if (child is UIElement el && el.Focusable && el.IsEnabled
                    && (el is TextBox || el is ComboBox || el is CheckBox || el is DatePicker
                        || (el is Control c && c.IsTabStop)))
                {
                    return el;
                }
                var nested = FindFocusable(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
