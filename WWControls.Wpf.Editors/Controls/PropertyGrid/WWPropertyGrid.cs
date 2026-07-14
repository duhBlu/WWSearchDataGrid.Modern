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

namespace WWControls.Wpf.Editors
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
        private bool _isRefreshing;

        static WWPropertyGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWPropertyGrid),
                new FrameworkPropertyMetadata(typeof(WWPropertyGrid)));
        }

        public WWPropertyGrid()
        {
            _editorDefinitions = new List<WWEditorDefinition>();

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
        /// Custom editor templates matched by property name. Populate in XAML, one
        /// <see cref="WWEditorDefinition"/> per editor.
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

            var filter = FilterText;
            if (string.IsNullOrWhiteSpace(filter))
            {
                _propertyItemsView.Filter = null;
            }
            else
            {
                _propertyItemsView.Filter = obj =>
                {
                    if (obj is WWPropertyItem item)
                    {
                        return item.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                            || item.PropertyName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                            || item.Category.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    return false;
                };
            }
        }

        #endregion

        #region SelectedObject Changed

        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WWPropertyGrid)d).RebuildPropertyItems(e.OldValue, e.NewValue);
        }

        private void RebuildPropertyItems(object oldSource, object newSource)
        {
            // Unsubscribe from old source
            if (oldSource is INotifyPropertyChanged oldNpc)
            {
                oldNpc.PropertyChanged -= Source_GlobalPropertyChanged;
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

            // Runtime metadata overrides, if the source provides them
            var metadataProvider = newSource as IPropertyMetadataProvider;

            var props = newSource.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var overrides = metadataProvider?.GetPropertyMetadata(prop.Name);

                // Browsable: override wins, then static attribute, then default true
                var browsableAttr = prop.GetCustomAttribute<BrowsableAttribute>();
                bool browsable = overrides?.Browsable
                    ?? (browsableAttr != null ? browsableAttr.Browsable : true);

                if (!browsable)
                    continue;

                var item = new WWPropertyItem(newSource, prop, overrides);

                // Match a custom editor definition
                var editorDef = _editorDefinitions.FirstOrDefault(d => d.Matches(prop.Name));
                if (editorDef != null)
                {
                    item.EditorTemplate = editorDef.EditingTemplate;
                }

                item.ValueWritten = RefreshAllValues;
                _items.Add(item);
            }

            // Create grouped + sorted view
            var view = CollectionViewSource.GetDefaultView(_items);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(WWPropertyItem.Category)));
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(WWPropertyItem.Category), ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(nameof(WWPropertyItem.PropertyOrder), ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(nameof(WWPropertyItem.DisplayName), ListSortDirection.Ascending));

            PropertyItems = view;
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
    /// Picks the editor <see cref="DataTemplate"/> for each property row: a custom template from
    /// <see cref="WWPropertyGrid.EditorDefinitions"/> when one matches, otherwise the read-only
    /// placeholder.
    /// </summary>
    public class WWPropertyGridEditorSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var pi = item as WWPropertyItem;
            if (pi == null)
                return null;

            // A custom editor supplied via EditorDefinitions always wins.
            if (pi.EditorTemplate != null)
                return pi.EditorTemplate;

            var element = container as FrameworkElement;

            // ── TODO: built-in typed editors ─────────────────────────────────────────────
            // WWPropertyGrid ships with NO built-in per-type editors yet. Consumers supply
            // their own per property via EditorDefinitions. Every property without a custom
            // template falls back to "WWPropertyGrid_PlaceholderEditor" — a read-only display
            // of the value.
            //
            // To add built-in editors later, dispatch on pi.PropertyType here and return keyed
            // templates from the theme dictionary (WWControls.Wpf.Themes.Default), e.g.:
            //     var underlying = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
            //     if (underlying == typeof(bool))   return element?.TryFindResource("WWPropertyGrid_BoolEditor")   as DataTemplate;
            //     if (underlying.IsEnum)            return element?.TryFindResource("WWPropertyGrid_EnumEditor")   as DataTemplate;
            //     if (underlying == typeof(string)) return element?.TryFindResource("WWPropertyGrid_StringEditor") as DataTemplate;
            //     // numeric / DateTime / etc.
            // and author those templates over the WWControls editor controls (WWTextBox,
            // WWCheckBox, WWComboBox, WWNumericUpDown, WWDatePicker).
            // ─────────────────────────────────────────────────────────────────────────────
            return element?.TryFindResource("WWPropertyGrid_PlaceholderEditor") as DataTemplate;
        }
    }
}
