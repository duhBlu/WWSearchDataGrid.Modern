using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWControls.Wpf.Primitives;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Dropdown editor. A real <see cref="ComboBox"/> whose single self-contained template owns
    /// every part — chrome frame, selection box, editable text (<c>PART_EditableTextBox</c>),
    /// chevron, and popup (<c>PART_Popup</c>) — so selection, keyboard, and typeahead behavior come
    /// from the platform control and the entire look lives in one place (the ModernWpf approach:
    /// retemplate the control, never wrap it).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not a <see cref="WWEditorBase"/> derivative — it inherits its editing surface from
    /// <see cref="ComboBox"/> instead — but it participates in the same chrome contract:
    /// <see cref="ShowBorder"/>, the <see cref="WWEditorBase.FlattenEditorsProperty"/> host flag,
    /// and <c>ControlHelper.CornerRadius</c> behave identically to the other editors.
    /// </para>
    /// <para>
    /// On top of the platform control it layers opt-in search-combo behaviors (modeled on the
    /// CabinetDesigner WWSearchComboBox): <see cref="IncrementalFiltering"/> narrows the dropdown
    /// as you type (editable or not) with match highlighting, <see cref="AutoComplete"/> fills the
    /// editable text with the closest item and selects the appended tail, <see cref="ShowNone"/>
    /// pins a clear-selection row, <see cref="ShowSizeGrip"/> makes the popup user-resizable, and
    /// <see cref="SelectionMode"/> switches items to checkbox multi-select or radio visuals.
    /// The typed filter is tracked in <see cref="FilterText"/>, deliberately separate from the
    /// selected item's display text; Escape clears the filter without touching the selection.
    /// </para>
    /// <para>
    /// Incremental filtering works through <c>Items.Filter</c> — the default collection view of
    /// the ItemsSource. If two controls share the same collection instance, give each its own
    /// view (e.g. a <c>CollectionViewSource</c>) so their filters don't fight. Smart-mode ranking
    /// additionally requires the view to be a <see cref="ListCollectionView"/> (any IList-backed
    /// source); other views filter without reordering.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = "PART_EditableTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_PopupBorder", Type = typeof(Border))]
    [TemplatePart(Name = "PART_ScrollViewer", Type = typeof(ScrollViewer))]
    [TemplatePart(Name = "PART_NoneItem", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_SelectAllItem", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_ResizeGrip", Type = typeof(Thumb))]
    public class WWComboBox : ComboBox
    {
        static WWComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWComboBox),
                new FrameworkPropertyMetadata(typeof(WWComboBox)));

            // DisplayMemberPath feeds GetItemDisplayText (selection box text, filter matching,
            // container highlight text), so recompute those when it changes. OverrideMetadata
            // chains onto ItemsControl's own callback rather than replacing it.
            DisplayMemberPathProperty.OverrideMetadata(typeof(WWComboBox),
                new FrameworkPropertyMetadata(string.Empty, OnDisplayTextSourceChanged));
        }

        private TextBox _editableTextBox;
        private Border _popupBorder;
        private ScrollViewer _popupScrollViewer;
        private Thumb _resizeGrip;
        private ButtonBase _noneItem;
        private ButtonBase _selectAllItem;

        private Predicate<object> _filterPredicate;
        private bool _matchSortApplied;
        private bool _internalTextChange;
        private bool _suppressAutoComplete;
        private bool _restoringSelection;
        private bool _suppressFocusOpen;
        private object _selectionSnapshot;

        public WWComboBox()
        {
            Loaded += OnEditorLoaded;
        }

        // Same in-cell self-flatten contract as WWEditorBase (see its OnBaseEditLoaded): bordered
        // standalone, flat + square inside a DataGridCell or a FlattenEditors host.
        private void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            if (WWEditorBase.GetFlattenEditors(this) || VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(this) != null)
            {
                ShowBorder = false;
                ControlHelper.SetCornerRadius(this, default);
            }
            UpdateSelectionBoxDisplayText();
        }

        #region Chrome

        /// <summary>
        /// Whether the chrome border draws — same semantics as <see cref="WWEditorBase.ShowBorder"/>:
        /// <c>true</c> standalone / on forms, cleared inside grid cells and flattening hosts.
        /// </summary>
        public static readonly DependencyProperty ShowBorderProperty =
            DependencyProperty.Register(nameof(ShowBorder), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(true));

        public bool ShowBorder
        {
            get => (bool)GetValue(ShowBorderProperty);
            set => SetValue(ShowBorderProperty, value);
        }

        #endregion

        #region Popup behavior properties

        /// <summary>
        /// Shows a footer bar in the popup with a bottom-right grip the user drags to resize the
        /// dropdown. The chosen size persists across opens; dragging taller than
        /// <see cref="ComboBox.MaxDropDownHeight"/> raises it.
        /// </summary>
        public static readonly DependencyProperty ShowSizeGripProperty =
            DependencyProperty.Register(nameof(ShowSizeGrip), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false));

        public bool ShowSizeGrip
        {
            get => (bool)GetValue(ShowSizeGripProperty);
            set => SetValue(ShowSizeGripProperty, value);
        }

        /// <summary>
        /// Opens the popup as soon as keyboard focus enters the control (from outside it) — the
        /// browse-first pattern for combos where picking from the list is the dominant action.
        /// </summary>
        public static readonly DependencyProperty ShowPopupWhenFocusedProperty =
            DependencyProperty.Register(nameof(ShowPopupWhenFocused), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false));

        public bool ShowPopupWhenFocused
        {
            get => (bool)GetValue(ShowPopupWhenFocusedProperty);
            set => SetValue(ShowPopupWhenFocusedProperty, value);
        }

        /// <summary>
        /// Pins a "none" row above the items that clears the selection (SelectedItem/SelectedValue
        /// to null, checked items emptied in checkbox mode) and closes the popup. The row's text
        /// is <see cref="NoneItemText"/>. Nothing is injected into the ItemsSource.
        /// </summary>
        public static readonly DependencyProperty ShowNoneProperty =
            DependencyProperty.Register(nameof(ShowNone), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false));

        public bool ShowNone
        {
            get => (bool)GetValue(ShowNoneProperty);
            set => SetValue(ShowNoneProperty, value);
        }

        /// <summary>Display text of the <see cref="ShowNone"/> row.</summary>
        public static readonly DependencyProperty NoneItemTextProperty =
            DependencyProperty.Register(nameof(NoneItemText), typeof(string), typeof(WWComboBox),
                new PropertyMetadata("(None)"));

        public string NoneItemText
        {
            get => (string)GetValue(NoneItemTextProperty);
            set => SetValue(NoneItemTextProperty, value);
        }

        /// <summary>
        /// Checkbox mode only: pins a "select all" row above the items whose tri-state glyph
        /// tracks <see cref="AreAllItemsChecked"/>. Clicking checks every currently VISIBLE item
        /// (so with a filter active it selects just the matches) or, when all are already
        /// checked, unchecks them. The popup stays open, like the row toggles themselves.
        /// </summary>
        public static readonly DependencyProperty ShowSelectAllProperty =
            DependencyProperty.Register(nameof(ShowSelectAll), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false));

        public bool ShowSelectAll
        {
            get => (bool)GetValue(ShowSelectAllProperty);
            set => SetValue(ShowSelectAllProperty, value);
        }

        /// <summary>Display text of the <see cref="ShowSelectAll"/> row.</summary>
        public static readonly DependencyProperty SelectAllTextProperty =
            DependencyProperty.Register(nameof(SelectAllText), typeof(string), typeof(WWComboBox),
                new PropertyMetadata("(Select All)"));

        public string SelectAllText
        {
            get => (string)GetValue(SelectAllTextProperty);
            set => SetValue(SelectAllTextProperty, value);
        }

        /// <summary>
        /// Tri-state of the select-all row against the currently visible items: <c>true</c> all
        /// checked, <c>false</c> none checked (or not in checkbox mode), <c>null</c> some checked.
        /// </summary>
        private static readonly DependencyPropertyKey AreAllItemsCheckedPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AreAllItemsChecked), typeof(bool?), typeof(WWComboBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty AreAllItemsCheckedProperty = AreAllItemsCheckedPropertyKey.DependencyProperty;

        public bool? AreAllItemsChecked => (bool?)GetValue(AreAllItemsCheckedProperty);

        #endregion

        #region Filtering / auto-complete properties

        /// <summary>
        /// Narrows the dropdown to items matching the typed text (per
        /// <see cref="IncrementalFilteringMode"/>; or, with <see cref="FilterDropdownItems"/> off,
        /// sorts matches to the top instead of hiding), highlighting the match in each row. Works in
        /// both editable mode (the text box drives the filter) and non-editable mode (typed
        /// characters accumulate in a search strip at the top of the popup; Backspace edits,
        /// Escape clears without touching the selection). Disables native text search.
        /// </summary>
        public static readonly DependencyProperty IncrementalFilteringProperty =
            DependencyProperty.Register(nameof(IncrementalFiltering), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false, OnTypingBehaviorChanged));

        public bool IncrementalFiltering
        {
            get => (bool)GetValue(IncrementalFilteringProperty);
            set => SetValue(IncrementalFilteringProperty, value);
        }

        /// <summary>
        /// Whether incremental filtering actually hides non-matching items (default). When
        /// <c>false</c>, typing hides nothing — the dropdown reorders instead, matching items
        /// first (ranked per <see cref="IncrementalFilteringMode"/>, original order preserved
        /// among ties and non-matches) with the match still highlighted. Reordering requires a
        /// <c>ListCollectionView</c> (any IList-backed ItemsSource); other views keep their order
        /// and only highlight.
        /// </summary>
        public static readonly DependencyProperty FilterDropdownItemsProperty =
            DependencyProperty.Register(nameof(FilterDropdownItems), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(true, OnIncrementalFilteringModeChanged));

        public bool FilterDropdownItems
        {
            get => (bool)GetValue(FilterDropdownItemsProperty);
            set => SetValue(FilterDropdownItemsProperty, value);
        }

        /// <summary>How typed text matches item display text when <see cref="IncrementalFiltering"/> is active.</summary>
        public static readonly DependencyProperty IncrementalFilteringModeProperty =
            DependencyProperty.Register(nameof(IncrementalFilteringMode), typeof(IncrementalFilteringMode), typeof(WWComboBox),
                new PropertyMetadata(IncrementalFilteringMode.Smart, OnIncrementalFilteringModeChanged));

        public IncrementalFilteringMode IncrementalFilteringMode
        {
            get => (IncrementalFilteringMode)GetValue(IncrementalFilteringModeProperty);
            set => SetValue(IncrementalFilteringModeProperty, value);
        }

        /// <summary>
        /// Editable mode only: as the user types, fills the text box with the closest matching
        /// item (first prefix match, best-ranked first under Smart filtering) and selects the
        /// auto-appended tail so continued typing replaces it. Suppressed while deleting.
        /// Replaces (and disables) native text search.
        /// </summary>
        public static readonly DependencyProperty AutoCompleteProperty =
            DependencyProperty.Register(nameof(AutoComplete), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false, OnTypingBehaviorChanged));

        public bool AutoComplete
        {
            get => (bool)GetValue(AutoCompleteProperty);
            set => SetValue(AutoCompleteProperty, value);
        }

        /// <summary>
        /// The live typed search text — deliberately separate from the selected item's display
        /// text. Item templates bind highlight elements to this (the built-in row rendering does).
        /// </summary>
        private static readonly DependencyPropertyKey FilterTextPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(FilterText), typeof(string), typeof(WWComboBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FilterTextProperty = FilterTextPropertyKey.DependencyProperty;

        public string FilterText => (string)GetValue(FilterTextProperty);

        /// <summary>True while <see cref="FilterText"/> is non-empty — template trigger convenience.</summary>
        private static readonly DependencyPropertyKey HasFilterTextPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasFilterText), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasFilterTextProperty = HasFilterTextPropertyKey.DependencyProperty;

        public bool HasFilterText => (bool)GetValue(HasFilterTextProperty);

        #endregion

        #region Selection properties

        /// <summary>
        /// Single (default), Checkbox (multi-select — items toggle membership in
        /// <see cref="SelectedItems"/> without closing the popup), or Radio (single-select with a
        /// radio glyph reflecting the selection).
        /// </summary>
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(nameof(SelectionMode), typeof(ComboBoxSelectionMode), typeof(WWComboBox),
                new PropertyMetadata(ComboBoxSelectionMode.Single, OnSelectionModeChanged));

        public ComboBoxSelectionMode SelectionMode
        {
            get => (ComboBoxSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// Checkbox mode's checked set. Bind an <see cref="ObservableCollection{T}"/> (or any
        /// IList; INotifyCollectionChanged keeps external mutations reflected); left null, the
        /// control creates its own on first toggle.
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(WWComboBox),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        /// <summary>Separator between display texts in the selection box when multiple items are checked.</summary>
        public static readonly DependencyProperty MultiSelectSeparatorProperty =
            DependencyProperty.Register(nameof(MultiSelectSeparator), typeof(string), typeof(WWComboBox),
                new PropertyMetadata(", ", OnDisplayTextSourceChanged));

        public string MultiSelectSeparator
        {
            get => (string)GetValue(MultiSelectSeparatorProperty);
            set => SetValue(MultiSelectSeparatorProperty, value);
        }

        /// <summary>
        /// When true, the closed control renders the selected item through the ItemTemplate /
        /// template selector (the same visual as its popup row). Default false renders plain
        /// display text. Checkbox mode always renders the joined text.
        /// </summary>
        public static readonly DependencyProperty ApplyItemTemplateToSelectedItemProperty =
            DependencyProperty.Register(nameof(ApplyItemTemplateToSelectedItem), typeof(bool), typeof(WWComboBox),
                new PropertyMetadata(false));

        public bool ApplyItemTemplateToSelectedItem
        {
            get => (bool)GetValue(ApplyItemTemplateToSelectedItemProperty);
            set => SetValue(ApplyItemTemplateToSelectedItemProperty, value);
        }

        /// <summary>
        /// Plain-text rendering of the current selection: the selected item's display text, or in
        /// checkbox mode every checked item's display text joined by <see cref="MultiSelectSeparator"/>.
        /// The template's selection box shows this unless <see cref="ApplyItemTemplateToSelectedItem"/>.
        /// </summary>
        private static readonly DependencyPropertyKey SelectionBoxDisplayTextPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SelectionBoxDisplayText), typeof(string), typeof(WWComboBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SelectionBoxDisplayTextProperty = SelectionBoxDisplayTextPropertyKey.DependencyProperty;

        public string SelectionBoxDisplayText => (string)GetValue(SelectionBoxDisplayTextProperty);

        #endregion

        #region Property changed handlers

        private static void OnTypingBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var combo = (WWComboBox)d;
            // Custom filtering / completion replaces native text search wholesale — both react to
            // the same keystrokes and fight over selection. SetCurrentValue so a consumer's local
            // assignment still wins.
            combo.SetCurrentValue(IsTextSearchEnabledProperty, !(combo.IncrementalFiltering || combo.AutoComplete));
            if (e.Property == IncrementalFilteringProperty && !(bool)e.NewValue)
                combo.ClearFilter();
            combo.RefreshContainerState();
        }

        private static void OnIncrementalFilteringModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var combo = (WWComboBox)d;
            if (combo.HasFilterText)
                combo.ApplyFilter(combo.FilterText);
            combo.RefreshContainerState();
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var combo = (WWComboBox)d;
            combo.RefreshContainerState();
            combo.UpdateSelectionBoxDisplayText();
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var combo = (WWComboBox)d;
            if (e.OldValue is INotifyCollectionChanged oldIncc)
                oldIncc.CollectionChanged -= combo.OnSelectedItemsCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newIncc)
                newIncc.CollectionChanged += combo.OnSelectedItemsCollectionChanged;
            combo.RefreshContainerState();
            combo.UpdateSelectionBoxDisplayText();
        }

        private static void OnDisplayTextSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var combo = (WWComboBox)d;
            combo.RefreshContainerState();
            combo.UpdateSelectionBoxDisplayText();
        }

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshContainerState();
            UpdateSelectionBoxDisplayText();
        }

        #endregion

        #region Template wiring

        public override void OnApplyTemplate()
        {
            if (_editableTextBox != null)
                _editableTextBox.TextChanged -= OnEditableTextChanged;
            if (_resizeGrip != null)
                _resizeGrip.DragDelta -= OnResizeGripDragDelta;
            if (_noneItem != null)
                _noneItem.Click -= OnNoneItemClick;
            if (_selectAllItem != null)
                _selectAllItem.Click -= OnSelectAllItemClick;

            base.OnApplyTemplate();

            _editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
            _popupBorder = GetTemplateChild("PART_PopupBorder") as Border;
            _popupScrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            _resizeGrip = GetTemplateChild("PART_ResizeGrip") as Thumb;
            _noneItem = GetTemplateChild("PART_NoneItem") as ButtonBase;
            _selectAllItem = GetTemplateChild("PART_SelectAllItem") as ButtonBase;

            if (_editableTextBox != null)
                _editableTextBox.TextChanged += OnEditableTextChanged;
            if (_resizeGrip != null)
                _resizeGrip.DragDelta += OnResizeGripDragDelta;
            if (_noneItem != null)
                _noneItem.Click += OnNoneItemClick;
            if (_selectAllItem != null)
                _selectAllItem.Click += OnSelectAllItemClick;
        }

        #endregion

        #region Container generation

        protected override DependencyObject GetContainerForItemOverride() => new WWComboBoxItem();

        protected override bool IsItemItsOwnContainerOverride(object item) => item is ComboBoxItem;

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is WWComboBoxItem container)
                ApplyContainerState(container, item);
        }

        private void ApplyContainerState(WWComboBoxItem container, object item)
        {
            container.SetGlyphMode(SelectionMode);
            container.SetIsChecked(SelectionMode == ComboBoxSelectionMode.Checkbox && SelectedItemsContains(item));
            container.SetDisplayText(GetItemDisplayText(item));
            container.SetSearchText(FilterText);
            container.SetHighlightMode(MapHighlightMode(IncrementalFilteringMode));
            container.SetUseHighlightDisplay(IncrementalFiltering && ItemTemplate == null && ItemTemplateSelector == null);
        }

        // Push current glyph / check / highlight state onto every realized container. Recycled or
        // newly realized containers pick the state up in PrepareContainerForItemOverride.
        private void RefreshContainerState()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is WWComboBoxItem container)
                    ApplyContainerState(container, Items[i]);
            }
        }

        private static HighlightMatchMode MapHighlightMode(IncrementalFilteringMode mode)
        {
            switch (mode)
            {
                case IncrementalFilteringMode.StartsWith: return HighlightMatchMode.StartsWith;
                case IncrementalFilteringMode.EndsWith: return HighlightMatchMode.EndsWith;
                default: return HighlightMatchMode.Contains;
            }
        }

        #endregion

        #region Display text

        /// <summary>
        /// Resolves an item's display text: DisplayMemberPath when set, else TextSearch.TextPath
        /// (the WPF convention for naming the text property when an ItemTemplate is in play —
        /// DisplayMemberPath and ItemTemplate are mutually exclusive), else ToString.
        /// </summary>
        internal string GetItemDisplayText(object item)
        {
            if (item == null)
                return string.Empty;
            if (item is ComboBoxItem cbi)
                return GetItemDisplayText(cbi.Content is string s ? s : cbi.Content);
            var path = DisplayMemberPath;
            if (string.IsNullOrEmpty(path))
                path = TextSearch.GetTextPath(this);
            if (!string.IsNullOrEmpty(path))
            {
                // TypeDescriptor so POCOs, anonymous types, DataRowView, and
                // ICustomTypeDescriptor all resolve uniformly (same approach as ComboBoxSettings).
                var pd = TypeDescriptor.GetProperties(item)[path];
                if (pd != null)
                    return pd.GetValue(item)?.ToString() ?? string.Empty;
            }
            return item.ToString() ?? string.Empty;
        }

        private void UpdateSelectionBoxDisplayText()
        {
            string text;
            if (SelectionMode == ComboBoxSelectionMode.Checkbox)
            {
                var selected = SelectedItems;
                if (selected == null || selected.Count == 0)
                {
                    text = string.Empty;
                }
                else
                {
                    var parts = new List<string>(selected.Count);
                    foreach (var item in selected)
                        parts.Add(GetItemDisplayText(item));
                    text = string.Join(MultiSelectSeparator ?? ", ", parts);
                }
            }
            else
            {
                text = GetItemDisplayText(SelectedItem);
            }
            SetValue(SelectionBoxDisplayTextPropertyKey, text);
            UpdateSelectAllState();
        }

        // The select-all tri-state compares checks against the VISIBLE items, so it also moves
        // when the item set changes (filtering, source mutations) with checks untouched.
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateSelectAllState();
        }

        #endregion

        #region Incremental filtering

        private void ApplyFilter(string filterText)
        {
            filterText = filterText ?? string.Empty;
            SetValue(FilterTextPropertyKey, filterText);
            SetValue(HasFilterTextPropertyKey, filterText.Length > 0);
            RefreshContainerState();

            bool active = filterText.Length > 0;
            if (Items.CanFilter)
                Items.Filter = active && FilterDropdownItems
                    ? (_filterPredicate ?? (_filterPredicate = FilterPredicate))
                    : null;

            // Reordering: Smart mode always leads with the best matches; with FilterDropdownItems
            // off nothing hides, so every mode sorts matches to the top instead. Only
            // ListCollectionView exposes CustomSort; other views keep source order.
            var listView = ItemsSource != null
                ? CollectionViewSource.GetDefaultView(ItemsSource) as ListCollectionView
                : null;
            if (listView != null)
            {
                bool wantSort = active
                    && (!FilterDropdownItems || IncrementalFilteringMode == IncrementalFilteringMode.Smart);
                if (wantSort)
                {
                    listView.CustomSort = new MatchRankComparer(this, filterText, IncrementalFilteringMode, BuildOriginalIndexMap());
                    _matchSortApplied = true;
                }
                else if (_matchSortApplied)
                {
                    listView.CustomSort = null;
                    _matchSortApplied = false;
                }
            }

            // The viewport otherwise stays anchored on the selected item — which, once matches
            // sort (or filter) to the top, can leave the best matches scrolled out of sight.
            if (active && IsDropDownOpen)
                ScrollPopupToTop();
        }

        // Deferred past the view refresh (and past the platform's own scroll-selected-into-view
        // on open) so the top of the re-ranked list is what ends up visible.
        private void ScrollPopupToTop()
        {
            if (_popupScrollViewer == null)
                return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (HasFilterText && IsDropDownOpen)
                    _popupScrollViewer.ScrollToTop();
            }), DispatcherPriority.Loaded);
        }

        // Source positions for stable ordering — ties and non-matching items keep their original
        // relative order instead of jumping to an alphabetical shuffle.
        private Dictionary<object, int> BuildOriginalIndexMap()
        {
            var map = new Dictionary<object, int>();
            var source = ItemsSource;
            if (source == null)
                return map;
            int i = 0;
            foreach (var item in source)
            {
                if (item != null && !map.ContainsKey(item))
                    map[item] = i;
                i++;
            }
            return map;
        }

        /// <summary>
        /// Clears the typed filter and restores the pre-filter selection if filtering knocked it
        /// out (hiding the selected item from the view clears the platform selection). An explicit
        /// clear (the none row) nulls the snapshot first so nothing comes back.
        /// </summary>
        private void ClearFilter()
        {
            if (HasFilterText || _matchSortApplied)
                ApplyFilter(string.Empty);

            if (SelectedItem == null && _selectionSnapshot != null && SelectionMode != ComboBoxSelectionMode.Checkbox)
            {
                _restoringSelection = true;
                try { SetCurrentValue(SelectedItemProperty, _selectionSnapshot); }
                finally { _restoringSelection = false; }
            }
        }

        private bool FilterPredicate(object item)
        {
            var filter = FilterText;
            if (string.IsNullOrEmpty(filter))
                return true;
            var display = GetItemDisplayText(item);
            switch (IncrementalFilteringMode)
            {
                case IncrementalFilteringMode.StartsWith:
                    return display.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
                case IncrementalFilteringMode.EndsWith:
                    return display.EndsWith(filter, StringComparison.OrdinalIgnoreCase);
                default: // Contains, Smart
                    return display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>
        /// Orders matches ahead of non-matches per the filtering mode (Smart additionally ranks
        /// exact, then prefix, then substring by how early it hits), with original source order
        /// as the final tiebreak so the list never looks arbitrarily shuffled.
        /// </summary>
        private sealed class MatchRankComparer : IComparer
        {
            private readonly WWComboBox _owner;
            private readonly string _filter;
            private readonly IncrementalFilteringMode _mode;
            private readonly Dictionary<object, int> _originalIndex;

            public MatchRankComparer(WWComboBox owner, string filter, IncrementalFilteringMode mode, Dictionary<object, int> originalIndex)
            {
                _owner = owner;
                _filter = filter;
                _mode = mode;
                _originalIndex = originalIndex;
            }

            public int Compare(object x, object y)
            {
                string dx = _owner.GetItemDisplayText(x);
                string dy = _owner.GetItemDisplayText(y);
                int rank = Rank(dx).CompareTo(Rank(dy));
                if (rank != 0) return rank;
                if (_mode == IncrementalFilteringMode.Contains || _mode == IncrementalFilteringMode.Smart)
                {
                    int index = MatchIndex(dx).CompareTo(MatchIndex(dy));
                    if (index != 0) return index;
                }
                return OriginalIndex(x).CompareTo(OriginalIndex(y));
            }

            private int Rank(string display)
            {
                switch (_mode)
                {
                    case IncrementalFilteringMode.StartsWith:
                        return display.StartsWith(_filter, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                    case IncrementalFilteringMode.EndsWith:
                        return display.EndsWith(_filter, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                    case IncrementalFilteringMode.Contains:
                        return display.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1;
                    default: // Smart
                        if (string.Equals(display, _filter, StringComparison.OrdinalIgnoreCase)) return 0;
                        if (display.StartsWith(_filter, StringComparison.OrdinalIgnoreCase)) return 1;
                        if (display.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
                        return 3;
                }
            }

            private int MatchIndex(string display)
            {
                int i = display.IndexOf(_filter, StringComparison.OrdinalIgnoreCase);
                return i < 0 ? int.MaxValue : i;
            }

            private int OriginalIndex(object item)
                => item != null && _originalIndex.TryGetValue(item, out int i) ? i : int.MaxValue;
        }

        #endregion

        #region Editable typing (filter + auto-complete)

        private void OnEditableTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_internalTextChange || !IsEditable || !(IncrementalFiltering || AutoComplete))
                return;

            string typed = _editableTextBox.Text ?? string.Empty;
            bool deleting = _suppressAutoComplete;
            _suppressAutoComplete = false;

            // Arrow-navigating the open dropdown updates the text to the newly selected item's
            // display text (platform behavior). That's a selection echo, not typing — refiltering
            // on it would collapse the list to just that item.
            if (SelectedItem != null && typed == GetItemDisplayText(SelectedItem))
                return;

            if (IncrementalFiltering)
            {
                if (typed.Length > 0 && !IsDropDownOpen && IsKeyboardFocusWithin)
                    SetCurrentValue(IsDropDownOpenProperty, true);
                ApplyFilter(typed);

                // Filtering the selected item out of view clears the platform selection, and the
                // platform echoes that null selection into the text box — put the typing back.
                if (_editableTextBox.Text != typed)
                {
                    _internalTextChange = true;
                    try
                    {
                        _editableTextBox.Text = typed;
                        _editableTextBox.CaretIndex = typed.Length;
                    }
                    finally
                    {
                        _internalTextChange = false;
                    }
                }
            }
            else
            {
                // Auto-complete alone still exposes the typed text for highlight bindings.
                SetValue(FilterTextPropertyKey, typed);
                SetValue(HasFilterTextPropertyKey, typed.Length > 0);
            }

            if (AutoComplete && !deleting && typed.Length > 0)
                TryAutoComplete(typed);
        }

        // Classic combo completion: keep what the user typed, append the closest item's remainder
        // as selected text so the next keystroke replaces it. First prefix match in view order —
        // under Smart filtering that's the best-ranked item.
        private void TryAutoComplete(string typed)
        {
            string completion = null;
            foreach (var item in Items)
            {
                var display = GetItemDisplayText(item);
                if (display.Length > typed.Length
                    && display.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
                {
                    completion = display;
                    break;
                }
            }
            if (completion == null)
                return;

            _internalTextChange = true;
            try
            {
                _editableTextBox.Text = typed + completion.Substring(typed.Length);
                _editableTextBox.Select(typed.Length, _editableTextBox.Text.Length - typed.Length);
            }
            finally
            {
                _internalTextChange = false;
            }
        }

        /// <summary>
        /// Commits the typed text to a selection: exact display match wins; otherwise (Enter only)
        /// the first visible item. No match leaves the selection alone — the close/blur path then
        /// restores the display text.
        /// </summary>
        private void CommitTypedText(bool allowBestMatch)
        {
            string typed = _editableTextBox?.Text ?? string.Empty;
            if (typed.Length == 0)
                return;

            object match = null;
            foreach (var item in Items)
            {
                if (string.Equals(GetItemDisplayText(item), typed, StringComparison.OrdinalIgnoreCase))
                {
                    match = item;
                    break;
                }
            }
            if (match == null && allowBestMatch && HasFilterText && Items.Count > 0)
                match = Items[0];

            if (match != null && !ReferenceEquals(match, SelectedItem))
                SetCurrentValue(SelectedItemProperty, match);
        }

        // After a commit/close, make the text box agree with the selection again (the typed
        // filter may not have matched anything).
        private void SyncEditableText()
        {
            if (!IsEditable || _editableTextBox == null)
                return;
            string display = GetItemDisplayText(SelectedItem);
            if (_editableTextBox.Text == display)
                return;
            _internalTextChange = true;
            try
            {
                _editableTextBox.Text = display;
                _editableTextBox.CaretIndex = display.Length;
            }
            finally
            {
                _internalTextChange = false;
            }
        }

        #endregion

        #region Non-editable incremental search

        // Non-editable search keeps keyboard focus wherever the platform put it (the control or a
        // focused item) — printable keystrokes bubble here, accumulate into FilterText, and render
        // in the popup's read-only search strip. No focusable popup text box, so none of the
        // ComboBox capture/focus fragility.
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (IncrementalFiltering && !IsEditable && !string.IsNullOrEmpty(e.Text) && !char.IsControl(e.Text[0]))
            {
                if (!IsDropDownOpen)
                    SetCurrentValue(IsDropDownOpenProperty, true);
                ApplyFilter(FilterText + e.Text);
                e.Handled = true;
                return;
            }
            base.OnTextInput(e);
        }

        #endregion

        #region Keyboard

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // Tab never navigates the popup: close it and let keyboard navigation proceed to the
            // next/previous control from the combo itself (deliberately NOT handled). Refocusing
            // matters when a popup item held focus — tab must leave the control, not walk rows.
            if (e.Key == Key.Tab && IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                if (!IsEditable)
                    Focus();
            }

            // Checkbox mode: Space toggles the focused row, Enter closes keeping the checks.
            // Both intercepted ahead of the platform's commit-selection handling.
            if (SelectionMode == ComboBoxSelectionMode.Checkbox && IsDropDownOpen)
            {
                if (e.Key == Key.Space && Keyboard.FocusedElement is WWComboBoxItem container)
                {
                    ToggleItemCheck(ItemContainerGenerator.ItemFromContainer(container));
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Enter)
                {
                    SetCurrentValue(IsDropDownOpenProperty, false);
                    e.Handled = true;
                    return;
                }
            }

            if (!IsEditable && IncrementalFiltering && HasFilterText)
            {
                if (e.Key == Key.Back)
                {
                    ApplyFilter(FilterText.Substring(0, FilterText.Length - 1));
                    e.Handled = true;
                    return;
                }
                // Escape clears the search but keeps the selection (and the popup); a second
                // Escape falls through to the platform close.
                if (e.Key == Key.Escape)
                {
                    ClearFilter();
                    e.Handled = true;
                    return;
                }
            }

            if (IsEditable && (IncrementalFiltering || AutoComplete))
            {
                if (e.Key == Key.Back || e.Key == Key.Delete)
                {
                    // Deleting must not re-complete — otherwise Backspace fights the fill-in and
                    // the text never shrinks.
                    _suppressAutoComplete = true;
                }
                else if (e.Key == Key.Escape && HasFilterText)
                {
                    ClearFilter();
                    SyncEditableText();
                    SetCurrentValue(IsDropDownOpenProperty, false);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Enter)
                {
                    CommitTypedText(allowBestMatch: true);
                    if (IsDropDownOpen)
                        SetCurrentValue(IsDropDownOpenProperty, false);
                    else
                    {
                        ClearFilter();
                        SyncEditableText();
                    }
                    e.Handled = true;
                    return;
                }
            }

            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region Selection plumbing

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            // Snapshot every real selection so a filter that momentarily hides the selected item
            // (clearing platform selection) can be undone in ClearFilter. Deliberately not
            // updated on the null transitions filtering causes.
            if (!_restoringSelection && SelectedItem != null)
                _selectionSnapshot = SelectedItem;
            UpdateSelectionBoxDisplayText();
        }

        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            // Opening mid-search (editable typing auto-opens after the first character lands):
            // show the best matches, not the platform's scroll-to-selected.
            if (HasFilterText)
                ScrollPopupToTop();

            // The platform hands keyboard focus to the selected item as the popup opens — which
            // puts the popup's items into the Tab path. Park focus back on the control instead:
            // Tab keeps moving through the form (see the Tab handling in OnPreviewKeyDown), and
            // the first arrow key press is what hands focus to the items.
            if (!IsEditable)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (IsDropDownOpen && !IsEditable && IsKeyboardFocusWithin && !IsKeyboardFocused)
                        Focus();
                }), DispatcherPriority.Input);
            }
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);

            // Closing re-focuses the control; without this gate ShowPopupWhenFocused would
            // immediately reopen the popup that just closed. Cleared at Background priority so
            // it outlives the Input-priority deferred open the refocus queues.
            _suppressFocusOpen = true;
            Dispatcher.BeginInvoke(new Action(() => _suppressFocusOpen = false), DispatcherPriority.Background);

            if (IsEditable && (IncrementalFiltering || AutoComplete))
                CommitTypedText(allowBestMatch: false);
            ClearFilter();
            SyncEditableText();
            UpdateSelectionBoxDisplayText();
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            if (!(bool)e.NewValue)
            {
                // Focus left entirely: commit an exact typed match, then drop the filter state.
                if (IsEditable && (IncrementalFiltering || AutoComplete))
                    CommitTypedText(allowBestMatch: false);
                ClearFilter();
                SyncEditableText();
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (!ShowPopupWhenFocused || _suppressFocusOpen || IsDropDownOpen)
                return;

            // Only opening on focus ENTERING the control. Closing the popup hands focus from the
            // focused popup item back to the control — that internal shuffle (and text box ↔
            // chevron moves) must not reopen what the user just closed via the toggle or a
            // click-away.
            if (IsFocusWithinSelf(e.OldFocus))
                return;

            // Deferred past the mouse-press toggle so a click that both focuses and toggles
            // doesn't open-then-close within one gesture.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsKeyboardFocusWithin && !IsDropDownOpen && !_suppressFocusOpen)
                    SetCurrentValue(IsDropDownOpenProperty, true);
            }), DispatcherPriority.Input);
        }

        // Whether the element belongs to this combo, including popup content — the popup's visual
        // tree roots at its own PopupRoot, so the walk falls back to logical parents (Popup is the
        // popup content's logical parent, and the Popup lives in this control's template).
        private bool IsFocusWithinSelf(IInputElement element)
        {
            var node = element as DependencyObject;
            while (node != null)
            {
                if (ReferenceEquals(node, this))
                    return true;
                if (node is ComboBoxItem item
                    && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(item), this))
                    return true;
                DependencyObject next = node is Visual || node is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(node)
                    : null;
                node = next ?? LogicalTreeHelper.GetParent(node);
            }
            return false;
        }

        #endregion

        #region Checkbox multi-select

        internal void ToggleItemCheck(object item)
        {
            if (item == null)
                return;

            var list = EnsureSelectedItems();
            if (list.Contains(item))
                list.Remove(item);
            else
                list.Add(item);

            NotifyIfNotObservable(list);
        }

        private IList EnsureSelectedItems()
        {
            var list = SelectedItems;
            if (list == null)
            {
                list = new ObservableCollection<object>();
                SetCurrentValue(SelectedItemsProperty, list); // callback hooks CollectionChanged
            }
            return list;
        }

        // Plain (non-INCC) lists don't notify — refresh directly.
        private void NotifyIfNotObservable(IList list)
        {
            if (!(list is INotifyCollectionChanged))
            {
                RefreshContainerState();
                UpdateSelectionBoxDisplayText();
            }
        }

        private bool SelectedItemsContains(object item)
        {
            var list = SelectedItems;
            return list != null && list.Contains(item);
        }

        // Check every currently visible item (the filtered view when a search is active), or
        // uncheck them all when they're all already checked. Items hidden by the filter keep
        // their checked state either way.
        private void OnSelectAllItemClick(object sender, RoutedEventArgs e)
        {
            if (SelectionMode != ComboBoxSelectionMode.Checkbox || Items.Count == 0)
                return;

            var list = EnsureSelectedItems();
            if (list.IsReadOnly)
                return;

            bool allChecked = AreAllItemsChecked == true;
            foreach (var item in Items)
            {
                if (allChecked)
                    list.Remove(item);
                else if (!list.Contains(item))
                    list.Add(item);
            }

            NotifyIfNotObservable(list);
        }

        private void UpdateSelectAllState()
        {
            bool? state = false;
            if (SelectionMode == ComboBoxSelectionMode.Checkbox && Items.Count > 0)
            {
                var list = SelectedItems;
                int checkedCount = 0;
                if (list != null && list.Count > 0)
                {
                    foreach (var item in Items)
                    {
                        if (list.Contains(item))
                            checkedCount++;
                    }
                }
                state = checkedCount == 0 ? false
                    : checkedCount == Items.Count ? (bool?)true
                    : null;
            }
            SetValue(AreAllItemsCheckedPropertyKey, state);
        }

        #endregion

        #region None row / size grip

        private void OnNoneItemClick(object sender, RoutedEventArgs e)
        {
            _selectionSnapshot = null;
            if (SelectionMode == ComboBoxSelectionMode.Checkbox && SelectedItems != null && !SelectedItems.IsReadOnly)
            {
                SelectedItems.Clear();
                if (!(SelectedItems is INotifyCollectionChanged))
                {
                    RefreshContainerState();
                    UpdateSelectionBoxDisplayText();
                }
            }
            SetCurrentValue(SelectedIndexProperty, -1);
            SetCurrentValue(IsDropDownOpenProperty, false);
            if (IsEditable && _editableTextBox != null)
            {
                _internalTextChange = true;
                try { _editableTextBox.Text = string.Empty; }
                finally { _internalTextChange = false; }
            }
            UpdateSelectionBoxDisplayText();
        }

        private void OnResizeGripDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_popupBorder == null)
                return;

            double width = double.IsNaN(_popupBorder.Width) ? _popupBorder.ActualWidth : _popupBorder.Width;
            double height = double.IsNaN(_popupBorder.Height) ? _popupBorder.ActualHeight : _popupBorder.Height;

            _popupBorder.Width = Math.Max(Math.Max(ActualWidth, 80), width + e.HorizontalChange);
            double newHeight = Math.Max(60, height + e.VerticalChange);
            _popupBorder.Height = newHeight;

            // The popup border's MaxHeight tracks MaxDropDownHeight; growing past it means the
            // user wants a taller list than the configured cap.
            if (newHeight > MaxDropDownHeight)
                SetCurrentValue(MaxDropDownHeightProperty, newHeight);
        }

        #endregion
    }
}
