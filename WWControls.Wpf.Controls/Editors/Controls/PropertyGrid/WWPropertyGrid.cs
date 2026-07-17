using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WWControls.Core.DataAnnotations;
using WWControls.Wpf.Controls.Editors.Settings;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Reflection-driven property grid. Reads the standard metadata attributes
    /// (<c>[Browsable]</c>, <c>[Category]</c>, <c>[DisplayName]</c>, <c>[Description]</c>,
    /// <c>[PropertyOrder]</c>, <c>[ReadOnly]</c>) off <see cref="SelectedObject"/> and renders its
    /// properties grouped by category, each row hosting an editor.
    /// </summary>
    /// <remarks>
    /// Editors are supplied per property through <see cref="EditorDefinitions"/> — a custom
    /// <see cref="DataTemplate"/> matched by property name. The control ships with no built-in
    /// per-type editors yet; a property without a matching definition falls back to a read-only
    /// placeholder (see <see cref="WWPropertyGridEditorSelector"/>).
    /// </remarks>
    public class WWPropertyGrid : Control, INotifyPropertyChanged
    {
        private readonly ObservableCollection<WWPropertyItem> _items = new ObservableCollection<WWPropertyItem>();
        private ICollectionView _propertyItemsView;
        private readonly List<WWEditorDefinition> _editorDefinitions;
        private readonly List<WWPropertyDefinition> _propertyDefinitions;
        private IObservablePropertyMetadataProvider _observableProvider;
        private bool _isRefreshing;

        static WWPropertyGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(typeof(WWPropertyGrid)));
        }

        public WWPropertyGrid()
        {
            _editorDefinitions = new List<WWEditorDefinition>();
            _propertyDefinitions = new List<WWPropertyDefinition>();

            // Descriptors live outside the logical tree, so they don't inherit DataContext on their
            // own — push the grid's DataContext down to each so bindings on their EditSettings (e.g.
            // a bound ComboBox ItemsSource) resolve against the consumer's view model.
            DataContextChanged += (_, e) =>
            {
                foreach (var def in _propertyDefinitions)
                    def.DataContext = e.NewValue;
            };

            CommandBindings.Add(new CommandBinding(
                WWPropertyGridCommands.SelectItemCommand,
                (s, e) =>
                {
                    if (e.Parameter is WWPropertyItem item)
                        SelectedPropertyItem = item;
                }));

            AddHandler(Keyboard.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(OnDescendantGotKeyboardFocus), true);
        }

        #region Dependency Properties

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(null, OnSelectedObjectChanged));

        public static readonly DependencyProperty NameColumnWidthProperty =
            DependencyProperty.Register(nameof(NameColumnWidth), typeof(GridLength), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(new GridLength(150), OnNameColumnWidthChanged));

        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register(nameof(ShowTitle), typeof(bool), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ShowSearchBoxProperty =
            DependencyProperty.Register(nameof(ShowSearchBox), typeof(bool), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty SelectedPropertyItemProperty =
            DependencyProperty.Register(nameof(SelectedPropertyItem), typeof(WWPropertyItem), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(null, OnSelectedPropertyItemChanged));

        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register(nameof(FilterText), typeof(string), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(string.Empty, OnFilterTextChanged));

        public static readonly DependencyProperty ShowValidationErrorsProperty =
            DependencyProperty.Register(nameof(ShowValidationErrors), typeof(bool), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(true, OnValidationContextChanged));

        public static readonly DependencyProperty AllowCommitOnValidationErrorProperty =
            DependencyProperty.Register(nameof(AllowCommitOnValidationError), typeof(bool), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(false, OnValidationContextChanged));

        public static readonly DependencyProperty HeaderShowModeProperty =
            DependencyProperty.Register(nameof(HeaderShowMode), typeof(PropertyHeaderShowMode), typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(PropertyHeaderShowMode.Left, OnLayoutContextChanged));

        #endregion

        #region CLR Properties

        /// <summary>The object whose properties the grid reflects and displays.</summary>
        public object SelectedObject
        {
            get => GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        /// <summary>Width of the name column, shared across all rows and driven by the splitter thumb.</summary>
        public GridLength NameColumnWidth
        {
            get => (GridLength)GetValue(NameColumnWidthProperty);
            set => SetValue(NameColumnWidthProperty, value);
        }

        /// <summary>Whether the title bar (showing the selected object's type name) is shown.</summary>
        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        /// <summary>Whether the search box that filters property rows is shown.</summary>
        public bool ShowSearchBox
        {
            get => (bool)GetValue(ShowSearchBoxProperty);
            set => SetValue(ShowSearchBoxProperty, value);
        }

        /// <summary>
        /// The currently selected property item. Drives the description panel and the selected-row
        /// highlight.
        /// </summary>
        public WWPropertyItem SelectedPropertyItem
        {
            get => (WWPropertyItem)GetValue(SelectedPropertyItemProperty);
            set => SetValue(SelectedPropertyItemProperty, value);
        }

        /// <summary>Search text filtering the rows by display name, property name, or category.</summary>
        public string FilterText
        {
            get => (string)GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        /// <summary>
        /// Grid-wide toggle for the per-row validation badges (default true). A
        /// <see cref="WWPropertyDefinition.ShowValidationErrors"/> override wins per property.
        /// </summary>
        public bool ShowValidationErrors
        {
            get => (bool)GetValue(ShowValidationErrorsProperty);
            set => SetValue(ShowValidationErrorsProperty, value);
        }

        /// <summary>
        /// Whether a value that fails its data-annotation attributes may still commit to the model
        /// (default false). When false, the failing edit is held and surfaced as an in-progress badge;
        /// when true, the value commits and the badge shows it advisory-style.
        /// </summary>
        public bool AllowCommitOnValidationError
        {
            get => (bool)GetValue(AllowCommitOnValidationErrorProperty);
            set => SetValue(AllowCommitOnValidationErrorProperty, value);
        }

        /// <summary>
        /// Grid-wide default for where each row places its header relative to the editor (default
        /// <see cref="PropertyHeaderShowMode.Left"/>). A <see cref="WWPropertyDefinition.HeaderShowMode"/>
        /// override wins per property.
        /// </summary>
        public PropertyHeaderShowMode HeaderShowMode
        {
            get => (PropertyHeaderShowMode)GetValue(HeaderShowModeProperty);
            set => SetValue(HeaderShowModeProperty, value);
        }

        /// <summary>
        /// Per-property editor declarations matched by property name — the primary way to override
        /// the auto-resolved editor. Populate in XAML, one <see cref="WWPropertyDefinition"/> per
        /// property (or group of properties), supplying <see cref="WWPropertyDefinition.EditSettings"/>
        /// and/or a custom <see cref="WWPropertyDefinition.EditTemplate"/>.
        /// </summary>
        public List<WWPropertyDefinition> PropertyDefinitions => _propertyDefinitions;

        /// <summary>
        /// Legacy custom editor templates matched by property name. Superseded by
        /// <see cref="PropertyDefinitions"/> (which also carries edit settings); still honored so
        /// existing consumers keep working. Populate in XAML, one <see cref="WWEditorDefinition"/>
        /// per editor.
        /// </summary>
        public List<WWEditorDefinition> EditorDefinitions => _editorDefinitions;

        /// <summary>The grouped/sorted view of property items the template binds to.</summary>
        public ICollectionView PropertyItems
        {
            get => _propertyItemsView;
            private set
            {
                _propertyItemsView = value;
                OnPropertyChanged(nameof(PropertyItems));
            }
        }

        #endregion

        #region Column Splitter

        private Thumb _columnSplitter;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_columnSplitter != null)
                _columnSplitter.DragDelta -= ColumnSplitter_DragDelta;

            _columnSplitter = GetTemplateChild("PART_ColumnSplitter") as Thumb;

            if (_columnSplitter != null)
            {
                _columnSplitter.DragDelta += ColumnSplitter_DragDelta;
                PositionColumnSplitter();
            }
        }

        private void ColumnSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var currentWidth = NameColumnWidth.Value;
            var newWidth = Math.Max(50, currentWidth + e.HorizontalChange);
            NameColumnWidth = new GridLength(newWidth);
            PositionColumnSplitter();
        }

        private void PositionColumnSplitter()
        {
            if (_columnSplitter != null)
            {
                // offset by the margin on the item template root (2px left)
                var offset = NameColumnWidth.Value + 2;
                _columnSplitter.Margin = new Thickness(offset - 3, 0, 0, 0);
            }
        }

        private static void OnNameColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WWPropertyGrid)d).PositionColumnSplitter();
        }

        #endregion

        #region Selection

        private static void OnSelectedPropertyItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is WWPropertyItem oldItem)
                oldItem.IsSelected = false;
            if (e.NewValue is WWPropertyItem newItem)
                newItem.IsSelected = true;
        }

        private void OnDescendantGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var element = e.NewFocus as DependencyObject;
            while (element != null && element != this)
            {
                if (element is FrameworkElement fe && fe.DataContext is WWPropertyItem item)
                {
                    if (SelectedPropertyItem != item)
                        SelectedPropertyItem = item;
                    return;
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }

        #endregion

        #region Filtering

        private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WWPropertyGrid)d).ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_propertyItemsView == null)
                return;

            // The filter always honors item.IsVisible (so a hidden property drops out live via the
            // view's live filtering), then narrows by the search text when present.
            var filter = FilterText;
            _propertyItemsView.Filter = obj =>
            {
                if (obj is not WWPropertyItem item)
                    return false;

                if (!item.IsVisible)
                    return false;

                if (string.IsNullOrWhiteSpace(filter))
                    return true;

                return item.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                    || item.PropertyName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                    || item.Category.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
            };
        }

        #endregion

        #region SelectedObject Changed

        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (WWPropertyGrid)d;

            // Skip the build while the control is still initializing from XAML. A bound
            // SelectedObject resolves during start-tag processing — before the
            // PropertyDefinitions child content is parsed — so building now would match every
            // property against an empty definition list and fall back to the CLR-type editor.
            // OnInitialized runs the first build once the definitions are present; after that,
            // a SelectedObject change rebuilds here immediately.
            if (!grid.IsInitialized)
                return;

            grid.RebuildPropertyItems(e.OldValue, e.NewValue);
        }

        /// <summary>
        /// Runs the first property build once XAML initialization completes. By this point the
        /// <see cref="PropertyDefinitions"/> child content is fully parsed, so each row resolves
        /// against its matching definition (its <c>EditSettings</c> / <c>EditTemplate</c>) instead
        /// of the CLR-type default. A <see cref="SelectedObject"/> whose binding resolves later
        /// (after its source DataContext is available) rebuilds through
        /// <see cref="OnSelectedObjectChanged"/>, which is unblocked once initialized.
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (SelectedObject != null)
                RebuildPropertyItems(null, SelectedObject);
        }

        private void RebuildPropertyItems(object oldSource, object newSource)
        {
            // Unsubscribe from old source
            if (oldSource is INotifyPropertyChanged oldNpc)
            {
                oldNpc.PropertyChanged -= Source_GlobalPropertyChanged;
            }
            if (_observableProvider != null)
            {
                _observableProvider.PropertyMetadataChanged -= OnProviderMetadataChanged;
                _observableProvider = null;
            }

            // Dispose old items
            foreach (var item in _items)
                item.Dispose();
            _items.Clear();

            SelectedPropertyItem = null;
            FilterText = string.Empty;

            if (newSource == null)
            {
                PropertyItems = null;
                return;
            }

            // Subscribe to new source for global refresh
            if (newSource is INotifyPropertyChanged newNpc)
            {
                newNpc.PropertyChanged += Source_GlobalPropertyChanged;
            }

            // Mechanism B: a source that signals metadata changes lets rows update live without a
            // rebuild. Plain IPropertyMetadataProvider sources keep the read-once snapshot behavior.
            _observableProvider = newSource as IObservablePropertyMetadataProvider;
            if (_observableProvider != null)
            {
                _observableProvider.PropertyMetadataChanged += OnProviderMetadataChanged;
            }

            // Definitions may have been added after the grid's DataContext was first set (XAML parse
            // order), so seed each with the current DataContext before reading their EditSettings.
            PropagateDataContextToDefinitions();

            // Runtime metadata overrides, if the source provides them
            var metadataProvider = newSource as IPropertyMetadataProvider;

            var props = newSource.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                // Build an item for every property — visibility is now a runtime filter (item.IsVisible)
                // rather than a build-time skip, so a hidden property can be shown live (a definition
                // binding or the provider flipping its visibility) without reassigning SelectedObject.
                var overrides = metadataProvider?.GetPropertyMetadata(prop.Name);
                var definition = _propertyDefinitions.FirstOrDefault(d => d.Matches(prop.Name));

                var item = new WWPropertyItem(newSource, prop, overrides, definition);
                ResolveEditor(item, prop, definition);
                item.SetValidationContext(ShowValidationErrors, AllowCommitOnValidationError);
                item.SetLayoutContext(HeaderShowMode);

                item.ValueWritten = RefreshAllValues;
                _items.Add(item);
            }

            // Create grouped + sorted view with live shaping so metadata that flips at runtime
            // (IsVisible → filter, Category / PropertyOrder / DisplayName → sort, Category → group)
            // reshapes the view off the items' PropertyChanged without a manual refresh.
            var view = CollectionViewSource.GetDefaultView(_items);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(WWPropertyItem.Category)));
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(WWPropertyItem.Category), ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(nameof(WWPropertyItem.PropertyOrder), ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(nameof(WWPropertyItem.DisplayName), ListSortDirection.Ascending));
            EnableLiveShaping(view);

            PropertyItems = view;
            ApplyFilter();
        }

        /// <summary>
        /// Turns on <see cref="ICollectionViewLiveShaping"/> for the property view so runtime metadata
        /// changes reshape it automatically: live filtering keyed on <see cref="WWPropertyItem.IsVisible"/>,
        /// live sorting on the sort keys, and live grouping on <see cref="WWPropertyItem.Category"/>.
        /// </summary>
        private static void EnableLiveShaping(ICollectionView view)
        {
            if (view is not ICollectionViewLiveShaping live)
                return;

            if (live.CanChangeLiveFiltering)
            {
                live.LiveFilteringProperties.Clear();
                live.LiveFilteringProperties.Add(nameof(WWPropertyItem.IsVisible));
                live.IsLiveFiltering = true;
            }
            if (live.CanChangeLiveSorting)
            {
                live.LiveSortingProperties.Clear();
                live.LiveSortingProperties.Add(nameof(WWPropertyItem.Category));
                live.LiveSortingProperties.Add(nameof(WWPropertyItem.PropertyOrder));
                live.LiveSortingProperties.Add(nameof(WWPropertyItem.DisplayName));
                live.IsLiveSorting = true;
            }
            if (live.CanChangeLiveGrouping)
            {
                live.LiveGroupingProperties.Clear();
                live.LiveGroupingProperties.Add(nameof(WWPropertyItem.Category));
                live.IsLiveGrouping = true;
            }
        }

        /// <summary>
        /// Re-pulls metadata for the affected property (or all) when an
        /// <see cref="IObservablePropertyMetadataProvider"/> source signals a change, then hands the
        /// fresh override to the row so it recomputes its effective metadata (mechanism B).
        /// </summary>
        private void OnProviderMetadataChanged(object sender, PropertyMetadataChangedEventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => OnProviderMetadataChanged(sender, e)));
                return;
            }

            var provider = _observableProvider;
            if (provider == null)
                return;

            foreach (var item in _items)
            {
                if (string.IsNullOrEmpty(e?.PropertyName) || item.PropertyName == e.PropertyName)
                    item.SetMetadataOverride(provider.GetPropertyMetadata(item.PropertyName));
            }
        }

        /// <summary>
        /// Resolves the editor for one property row and attaches it to <paramref name="item"/>,
        /// in precedence order: a custom edit template (matched <see cref="WWPropertyDefinition.EditTemplate"/>,
        /// else a legacy <see cref="WWEditorDefinition"/>) → the definition's
        /// <see cref="WWPropertyDefinition.EditSettings"/> → a <c>[PropertyGridEditor]</c> /
        /// <c>[DefaultEditor]</c> attribute → the CLR type default. When nothing resolves,
        /// <see cref="WWPropertyItem.EditSettings"/> stays null and the row falls back to the
        /// read-only placeholder.
        /// </summary>
        private void ResolveEditor(WWPropertyItem item, PropertyInfo prop, WWPropertyDefinition definition)
        {
            var legacyDefinition = _editorDefinitions.FirstOrDefault(d => d.Matches(prop.Name));

            // A fully custom template wins and short-circuits the settings resolution below.
            item.EditorTemplate = definition?.EditTemplate ?? legacyDefinition?.EditingTemplate;
            if (item.EditorTemplate != null)
                return;

            // Explicit EditSettings on the definition, then the editor attribute, then the CLR type.
            var settings = definition?.EditSettings
                ?? EditorSettingsFactory.CreateSettings(ResolveEditorKind(prop))
                ?? EditorSettingsFactory.CreateSettingsForType(item.PropertyType, item.EnumValues);

            // A combo over an enum self-populates from the enum values when none was wired (covers
            // an explicit [PropertyGridEditor(ComboBox)] on an enum property).
            if (settings is ComboBoxSettings combo && combo.ItemsSource == null && item.EnumValues != null)
                combo.ItemsSource = item.EnumValues;

            item.EditSettings = settings;
        }

        /// <summary>
        /// Resolves the property-grid editor kind from the property's editor attributes: a
        /// <c>[PropertyGridEditor]</c> wins, then a <c>[DefaultEditor]</c>, otherwise
        /// <see cref="EditorKind.Default"/> (the caller then picks by CLR type).
        /// </summary>
        private static EditorKind ResolveEditorKind(PropertyInfo prop)
        {
            var editorAttrs = prop.GetCustomAttributes<EditorAttributeBase>(inherit: true).ToList();
            var propertyGrid = editorAttrs.FirstOrDefault(a => a.Context == EditorContext.PropertyGrid);
            if (propertyGrid != null)
                return propertyGrid.Editor;
            var def = editorAttrs.FirstOrDefault(a => a.Context == EditorContext.Default);
            return def?.Editor ?? EditorKind.Default;
        }

        /// <summary>Pushes the grid's current <c>DataContext</c> onto every property definition so
        /// bindings on their <see cref="WWPropertyDefinition.EditSettings"/> resolve.</summary>
        private void PropagateDataContextToDefinitions()
        {
            foreach (var def in _propertyDefinitions)
                def.DataContext = DataContext;
        }

        private static void OnValidationContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WWPropertyGrid)d).PropagateValidationContext();
        }

        /// <summary>Pushes the grid-level validation context onto every row so a runtime toggle of
        /// <see cref="ShowValidationErrors"/> / <see cref="AllowCommitOnValidationError"/> takes effect live.</summary>
        private void PropagateValidationContext()
        {
            foreach (var item in _items)
                item.SetValidationContext(ShowValidationErrors, AllowCommitOnValidationError);
        }

        private static void OnLayoutContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WWPropertyGrid)d).PropagateLayoutContext();
        }

        /// <summary>Pushes the grid-level header-layout default onto every row so a runtime change of
        /// <see cref="HeaderShowMode"/> re-lays out the rows that inherit it.</summary>
        private void PropagateLayoutContext()
        {
            foreach (var item in _items)
                item.SetLayoutContext(HeaderShowMode);
        }

        private void Source_GlobalPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(RefreshAllValues));
                return;
            }
            RefreshAllValues();
        }

        /// <summary>
        /// Re-reads every property value from the source. Guarded against re-entrancy so cascading
        /// changes don't loop.
        /// </summary>
        private void RefreshAllValues()
        {
            if (_isRefreshing)
                return;
            _isRefreshing = true;
            try
            {
                foreach (var item in _items)
                    item.RefreshValue();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Picks the editor <see cref="DataTemplate"/> for each property row, using the editor the grid
    /// resolved onto the <see cref="WWPropertyItem"/>: a custom <see cref="WWPropertyItem.EditorTemplate"/>
    /// wins, else the settings-built editor from <see cref="WWPropertyItem.EditSettings"/>, else the
    /// read-only placeholder (only reached for a property whose CLR type has no built-in editor).
    /// </summary>
    public class WWPropertyGridEditorSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var pi = item as WWPropertyItem;
            if (pi == null)
                return null;

            // A fully custom editor template always wins.
            if (pi.EditorTemplate != null)
                return pi.EditorTemplate;

            // Settings-built editor (from a definition, an editor attribute, or the CLR type) —
            // the row hosts it inline. The value binds straight to the model via the item's
            // IEditorColumn.CreateFieldBinding, so the editor edits the source directly.
            if (pi.EditSettings != null)
                return pi.EditSettings.ResolveEditTemplate(pi);

            // Final fallback: a property whose type has no natural editor (a complex object, Guid, …)
            // shows the read-only placeholder.
            var element = container as FrameworkElement;
            return element?.TryFindResource("WWPropertyGrid_PlaceholderEditor") as DataTemplate;
        }
    }
}
