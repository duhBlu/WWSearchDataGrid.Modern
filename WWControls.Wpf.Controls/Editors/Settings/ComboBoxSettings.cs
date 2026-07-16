using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWControls.Wpf.Controls.Editors.Settings
{
    /// <summary>
    /// Dropdown editor. Edit mode is a ComboBox with the configured ItemsSource, DisplayMemberPath,
    /// and SelectedValuePath. Display mode shows the same ComboBox with hit-testing disabled and
    /// no border — so the resolved display text appears like a TextBlock but the lookup remains
    /// driven by the ItemsSource (no extra converter needed).
    /// </summary>
    public class ComboBoxSettings : BaseEditorSettings
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ComboBoxSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(ComboBoxSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(ComboBoxSettings), new PropertyMetadata(null));

        /// <summary>
        /// When <c>true</c>, the dropdown popup opens automatically as soon as the cell enters
        /// edit mode. Defaults to <c>false</c> so keyboard navigation (Tab / arrow keys) into a
        /// cell does not steal focus into the dropdown items and break onward grid navigation —
        /// the user opens the popup explicitly via click, F4, Alt+Down, or Space (non-editable
        /// only). Set to <c>true</c> for columns where browsing the list is the dominant action.
        /// </summary>
        public static readonly DependencyProperty OpenDropDownOnEditProperty =
            DependencyProperty.Register(nameof(OpenDropDownOnEdit), typeof(bool), typeof(ComboBoxSettings),
                new PropertyMetadata(false));

        /// <summary>The list of items shown in the dropdown.</summary>
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>The property on each item used as its display text.</summary>
        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        /// <summary>
        /// The property on each item used as its bound value. When set, the column's field stores
        /// this value (e.g. an Id) instead of the full item — matching foreign-key usage.
        /// </summary>
        public string SelectedValuePath
        {
            get => (string)GetValue(SelectedValuePathProperty);
            set => SetValue(SelectedValuePathProperty, value);
        }

        /// <summary>
        /// Whether the dropdown popup auto-opens when the cell enters edit mode. Default <c>false</c>.
        /// </summary>
        public bool OpenDropDownOnEdit
        {
            get => (bool)GetValue(OpenDropDownOnEditProperty);
            set => SetValue(OpenDropDownOnEditProperty, value);
        }

        /// <summary>
        /// Display mode is a plain TextBlock — no ComboBox chrome. The lookup converter resolves
        /// the bound value to its display text using the configured ItemsSource / DisplayMemberPath
        /// / SelectedValuePath, so a column storing an Id still shows the friendly name.
        /// </summary>
        public override DataTemplate CreateDisplayTemplate(IEditorColumn column)
        {
            // Two-column host: TextBlock fills column 0, optional chevron drop-down indicator
            // sits in column 1 with visibility controlled by EditorButtonShowMode.
            var grid = new FrameworkElementFactory(typeof(Grid));
            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            grid.AppendChild(col1);

            var factory = new FrameworkElementFactory(typeof(TextBlock));
            ApplyDisplayStyle(factory, EditorThemeKeys.DisplayTextBlock);
            ApplyTextAlignment(factory, column);
            factory.SetValue(Grid.ColumnProperty, 0);

            var binding = column.CreateFieldBinding();
            binding.Mode = BindingMode.OneWay;
            binding.Converter = new ComboBoxValueLookupConverter(ItemsSource, DisplayMemberPath, SelectedValuePath);
            factory.SetBinding(TextBlock.TextProperty, binding);
            // Display element validates against the INotifyDataErrorInfo row by default; the badge
            // is the library's error surface, so strip WPF's red adorner. See TextBoxSettings.
            SuppressValidationErrorAdorner(factory);
            grid.AppendChild(factory);

            // Chevron — non-functional in display, click enters edit mode (the auto-open dropdown
            // behavior in CreateEditTemplate then opens the popup). The exact same style is applied
            // to the chevron sub-element inside the EditComboBox template, so overriding
            // EditComboBoxDropDownButton retheme this indicator and the editor's caret together.
            // IsHitTestVisible=false lets the click reach the underlying cell, which the DataGrid
            // promotes to edit mode.
            var chevron = new FrameworkElementFactory(typeof(ToggleButton));
            ApplyKeyedStyle(chevron, EditorThemeKeys.EditComboBoxDropDownButton);
            chevron.SetValue(UIElement.IsHitTestVisibleProperty, false);
            chevron.SetValue(Grid.ColumnProperty, 1);

            var visBinding = BuildEditorButtonVisibilityBinding(this, column);
            if (visBinding != null)
                chevron.SetBinding(UIElement.VisibilityProperty, visBinding);

            grid.AppendChild(chevron);
            return new DataTemplate { VisualTree = grid };
        }

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
            // ComboBox selects a discrete value from a known set — Contains / StartsWith etc.
            // don't make sense. Equals / NotEquals are the only operators that map cleanly to
            // a chosen value. IsAnyOf / IsNoneOf would map cleanly to a multi-select ComboBox
            // but that's a future feature; not added here.
            => WithNullability(new[] { Core.SearchType.Equals, Core.SearchType.NotEquals }, isNullable);

        // Pair with the whitelist above: string-typed fields would otherwise inherit
        // DefaultSearchType.StartsWith from GridColumn.ApplyTypeBasedDefaults, which is not in
        // the ComboBox whitelist and would disable the FilterRow cell.
        public override DefaultSearchType? GetPreferredDefaultSearchType() => DefaultSearchType.Equals;

        public override UIElement CreateFilterDisplay(IFilterEditorHost host)
        {
            // Read-only display: TextBlock that resolves SearchValue through the same
            // ItemsSource / DisplayMemberPath / SelectedValuePath lookup the cell display
            // template uses. SelectedValuePath mode (foreign-key) finds the matching item and
            // shows its DisplayMemberPath property; item-bound mode reads the property off the
            // item directly. Empty SearchValue → empty text.
            var tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0),
            };
            var style = Application.Current?.TryFindResource(EditorThemeKeys.DisplayTextBlock) as Style;
            if (style != null) tb.Style = style;

            BindingOperations.SetBinding(tb, TextBlock.TextProperty, new Binding("SearchValue")
            {
                Source = host,
                Mode = BindingMode.OneWay,
                Converter = new ComboBoxValueLookupConverter(ItemsSource, DisplayMemberPath, SelectedValuePath),
            });
            return tb;
        }

        public override UIElement CreateFilterEditor(IFilterEditorHost host)
        {
            // ComboBox with the same ItemsSource / DisplayMemberPath / SelectedValuePath as
            // the cell editor — the dropdown options for filtering match what the cell editor
            // offers for the column. Selection writes through to ColumnFilterControl.SearchValue.
            var cb = new ComboBox();

            // Style FIRST — mirror the cell-editor's FrameworkElementFactory ordering
            // (StyleProperty before other writes). The EditComboBox template's chevron
            // sub-element references EditComboBoxDropDownButton via StaticResource — moving
            // the style application ahead of ItemsSource / ItemTemplate keeps the chevron
            // template materialization deterministic across hosts.
            var style = Application.Current?.TryFindResource(EditorThemeKeys.EditComboBox) as Style;
            if (style != null) cb.Style = style;

            cb.VerticalContentAlignment = VerticalAlignment.Center;
            cb.HorizontalContentAlignment = HorizontalAlignment.Left;
            cb.ItemsSource = ItemsSource;

            if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                // Same ItemTemplate trick CreateEditTemplate uses: the custom ControlTemplate
                // hand-places its selection-box ContentPresenter, so DisplayMemberPath alone
                // doesn't render selection text correctly. Setting ItemTemplate gives both the
                // dropdown items and the closed-state display the right rendering.
                var itemTemplate = new DataTemplate();
                var textFactory = new FrameworkElementFactory(typeof(TextBlock));
                textFactory.SetBinding(TextBlock.TextProperty, new Binding(DisplayMemberPath));
                itemTemplate.VisualTree = textFactory;
                cb.ItemTemplate = itemTemplate;
            }

            if (!string.IsNullOrEmpty(SelectedValuePath))
            {
                cb.SelectedValuePath = SelectedValuePath;
                BindingOperations.SetBinding(cb, Selector.SelectedValueProperty, new Binding("SearchValue")
                {
                    Source = host,
                    Mode = BindingMode.TwoWay,
                });
            }
            else
            {
                BindingOperations.SetBinding(cb, Selector.SelectedItemProperty, new Binding("SearchValue")
                {
                    Source = host,
                    Mode = BindingMode.TwoWay,
                });
            }
            return cb;
        }

        public override DataTemplate CreateEditTemplate(IEditorColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(WWComboBox));

            // The editor owns its own border and draws it by default, flattening itself when it
            // detects a grid cell. WWComboBox IS a ComboBox (single self-contained template), so
            // selection / popup / DisplayMemberPath are all native.
            factory.SetValue(WWComboBox.ItemsSourceProperty, ItemsSource);
            if (!string.IsNullOrEmpty(DisplayMemberPath))
                factory.SetValue(WWComboBox.DisplayMemberPathProperty, DisplayMemberPath);

            if (!string.IsNullOrEmpty(SelectedValuePath))
            {
                factory.SetValue(WWComboBox.SelectedValuePathProperty, SelectedValuePath);
                factory.SetBinding(WWComboBox.SelectedValueProperty, CreateValueBinding(column));
            }
            else
            {
                factory.SetBinding(WWComboBox.SelectedItemProperty, CreateValueBinding(column));
            }
            SuppressValidationErrorAdorner(factory);

            bool hosted = column?.Host != null;

            if (hosted)
            {
                // Grid cell only: focus the editor when the edit template materializes (it forwards
                // focus to the inner combo) so F4 / Alt+Down / Space can toggle the popup without a
                // second Tab, and auto-open the dropdown on edit-mode entry — either
                // OpenDropDownOnEdit (explicit opt-in) or a mouse-driven edit (consuming the same
                // SearchDataGrid mouse-edit stash TextBoxSettings uses for the caret). A hostless row
                // (property grid) wants neither: every combo would grab focus / pop open on load.
                AutoFocusOnLoad(factory);

                bool openOnEdit = OpenDropDownOnEdit;
                factory.AddHandler(FrameworkElement.LoadedEvent,
                    new RoutedEventHandler((s, _) =>
                    {
                        if (s is not WWComboBox editor) return;

                        bool shouldOpen = openOnEdit;
                        if (!shouldOpen)
                        {
                            var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(editor);
                            var grid = VisualTreeHelperMethods.FindVisualAncestor<IEditingGridHost>(cell);
                            if (grid != null && cell != null
                                && grid.TryConsumeMouseEditPoint(cell, out Point _))
                            {
                                shouldOpen = true;
                            }
                        }

                        if (!shouldOpen) return;

                        editor.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            editor.IsDropDownOpen = true;
                        }), DispatcherPriority.Input);
                    }));
            }

            // Keyboard interaction (the editor exposes IsDropDownOpen / IsEditable; the adapter — the
            // bridge — drives the grid coupling):
            //   • Tab with dropdown open → close + refocus the combo so DataGrid's Tab handler commits
            //     and moves on (not marked Handled).
            //   • Space on a non-editable combo → toggle the dropdown.
            //   • Arrows with dropdown CLOSED → exit the cell (grid only). Hostless, arrows are left to
            //     the ComboBox so Up/Down change the selection; OPEN → both fall through to item nav.
            factory.AddHandler(UIElement.PreviewKeyDownEvent,
                new KeyEventHandler((s, e) =>
                {
                    if (s is not WWComboBox editor) return;

                    if (e.Key == Key.Tab && editor.IsDropDownOpen)
                    {
                        editor.IsDropDownOpen = false;
                        editor.Focus();
                        return;
                    }

                    if (e.Key == Key.Space && !editor.IsEditable)
                    {
                        editor.IsDropDownOpen = !editor.IsDropDownOpen;
                        e.Handled = true;
                        return;
                    }

                    if (hosted && !editor.IsDropDownOpen
                        && (e.Key == Key.Left || e.Key == Key.Right
                            || e.Key == Key.Up || e.Key == Key.Down))
                    {
                        e.Handled = true;
                        ExitCellViaArrow(editor, e);
                    }
                }));

            return new DataTemplate { VisualTree = factory };
        }

    }

    /// <summary>
    /// One-way converter used by <see cref="ComboBoxSettings"/>'s display template to map
    /// the bound value to a display string. Three scenarios:
    /// <list type="bullet">
    ///   <item><c>SelectedValuePath</c> set: bound value is an Id; finds the matching item in
    ///   <c>ItemsSource</c> and returns its <c>DisplayMemberPath</c> property.</item>
    ///   <item><c>DisplayMemberPath</c> set without <c>SelectedValuePath</c>: bound value is the
    ///   item itself; reads the <c>DisplayMemberPath</c> property off it directly.</item>
    ///   <item>Neither set: returns <c>value.ToString()</c>.</item>
    /// </list>
    /// </summary>
    internal sealed class ComboBoxValueLookupConverter : IValueConverter
    {
        private readonly IEnumerable _itemsSource;
        private readonly string _displayMemberPath;
        private readonly string _selectedValuePath;

        public ComboBoxValueLookupConverter(IEnumerable itemsSource, string displayMemberPath, string selectedValuePath)
        {
            _itemsSource = itemsSource;
            _displayMemberPath = displayMemberPath;
            _selectedValuePath = selectedValuePath;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            // Foreign-key path: find the item whose SelectedValuePath property equals `value`,
            // then read its DisplayMemberPath property.
            if (!string.IsNullOrEmpty(_selectedValuePath) && _itemsSource != null)
            {
                foreach (var item in _itemsSource)
                {
                    if (item == null) continue;
                    var idValue = ReadProperty(item, _selectedValuePath);
                    if (Equals(idValue, value))
                        return string.IsNullOrEmpty(_displayMemberPath) ? item.ToString() : ReadProperty(item, _displayMemberPath)?.ToString() ?? string.Empty;
                }
                return string.Empty;
            }

            // Item-bound path: value IS the item; navigate to DisplayMemberPath.
            if (!string.IsNullOrEmpty(_displayMemberPath))
                return ReadProperty(value, _displayMemberPath)?.ToString() ?? string.Empty;

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        // TypeDescriptor lookup — same approach the rest of the codebase uses, so it works
        // uniformly on POCOs, anonymous types, DataRowView, ICustomTypeDescriptor.
        private static object ReadProperty(object item, string path)
        {
            if (item == null || string.IsNullOrEmpty(path)) return null;
            var pd = TypeDescriptor.GetProperties(item)[path];
            return pd?.GetValue(item);
        }
    }
}
