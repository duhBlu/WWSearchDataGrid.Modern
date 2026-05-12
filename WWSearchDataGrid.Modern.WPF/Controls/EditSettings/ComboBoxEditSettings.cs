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

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Dropdown editor. Edit mode is a ComboBox with the configured ItemsSource, DisplayMemberPath,
    /// and SelectedValuePath. Display mode shows the same ComboBox with hit-testing disabled and
    /// no border — so the resolved display text appears like a TextBlock but the lookup remains
    /// driven by the ItemsSource (no extra converter needed).
    /// </summary>
    public class ComboBoxEditSettings : BaseEditSettings
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ComboBoxEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(ComboBoxEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(ComboBoxEditSettings), new PropertyMetadata(null));

        /// <summary>
        /// When <c>true</c>, the dropdown popup opens automatically as soon as the cell enters
        /// edit mode. Defaults to <c>false</c> so keyboard navigation (Tab / arrow keys) into a
        /// cell does not steal focus into the dropdown items and break onward grid navigation —
        /// the user opens the popup explicitly via click, F4, Alt+Down, or Space (non-editable
        /// only). Set to <c>true</c> for columns where browsing the list is the dominant action.
        /// </summary>
        public static readonly DependencyProperty OpenDropDownOnEditProperty =
            DependencyProperty.Register(nameof(OpenDropDownOnEdit), typeof(bool), typeof(ComboBoxEditSettings),
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
        public override DataTemplate CreateDisplayTemplate(GridColumn column)
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
            ApplyDisplayStyle(factory, EditSettingsThemeKeys.DisplayTextBlock);
            ApplyTextAlignment(factory, column);
            factory.SetValue(Grid.ColumnProperty, 0);

            var binding = new Binding(column.FieldName)
            {
                Mode = BindingMode.OneWay,
                Converter = new ComboBoxValueLookupConverter(ItemsSource, DisplayMemberPath, SelectedValuePath)
            };
            factory.SetBinding(TextBlock.TextProperty, binding);
            grid.AppendChild(factory);

            // Chevron — non-functional in display, click enters edit mode (the auto-open dropdown
            // behavior in CreateEditTemplate then opens the popup). The exact same style is applied
            // to the chevron sub-element inside the EditComboBox template, so overriding
            // EditComboBoxDropDownButton retheme this indicator and the editor's caret together.
            // IsHitTestVisible=false lets the click reach the underlying cell, which the DataGrid
            // promotes to edit mode.
            var chevron = new FrameworkElementFactory(typeof(ToggleButton));
            ApplyKeyedStyle(chevron, EditSettingsThemeKeys.EditComboBoxDropDownButton);
            chevron.SetValue(UIElement.IsHitTestVisibleProperty, false);
            chevron.SetValue(Grid.ColumnProperty, 1);

            var visBinding = BuildEditorButtonVisibilityBinding(this, column);
            if (visBinding != null)
                chevron.SetBinding(UIElement.VisibilityProperty, visBinding);

            grid.AppendChild(chevron);
            return new DataTemplate { VisualTree = grid };
        }

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            // Style FIRST. FrameworkElementFactory has a known quirk where StyleProperty must be
            // set before other SetValue / SetBinding calls; otherwise the Style fails to apply.
            ApplyEditorStyle(factory, EditSettingsThemeKeys.EditComboBox);
            ApplyTextAlignment(factory, column);

            factory.SetValue(ItemsControl.ItemsSourceProperty, ItemsSource);

            // Provide an explicit ItemTemplate instead of leaning on DisplayMemberPath. WPF's
            // built-in DisplayMemberPath → SelectionBoxItem rendering relies on the
            // ContentPresenter being auto-generated by the ItemContainerGenerator so it can walk
            // up to the ComboBox and pick up the path. Our EditComboBox ControlTemplate hand-
            // places the selection-box ContentPresenter, so that walk-up never fires and the
            // SelectionBoxItem renders via ToString() on the raw item (a positional record
            // prints as "PriorityOption { Id = 4, Name = Urgent }" instead of "Urgent"). Setting
            // ItemTemplate fixes both halves: the dropdown items render via the template, and
            // WPF copies ItemTemplate → SelectionBoxItemTemplate so the closed-state display
            // uses it too. ItemTemplate and DisplayMemberPath are mutually exclusive on
            // ItemsControl, so this branch replaces the DisplayMemberPath assignment entirely.
            if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                var itemTemplate = new DataTemplate();
                var textFactory = new FrameworkElementFactory(typeof(TextBlock));
                textFactory.SetBinding(TextBlock.TextProperty, new Binding(DisplayMemberPath));
                itemTemplate.VisualTree = textFactory;
                factory.SetValue(ItemsControl.ItemTemplateProperty, itemTemplate);
            }

            if (!string.IsNullOrEmpty(SelectedValuePath))
            {
                factory.SetValue(Selector.SelectedValuePathProperty, SelectedValuePath);
                factory.SetBinding(Selector.SelectedValueProperty, CreateValueBinding(column));
            }
            else
            {
                factory.SetBinding(Selector.SelectedItemProperty, CreateValueBinding(column));
            }

            // Pull keyboard focus onto the ComboBox itself when the edit template materializes —
            // otherwise tabbing into the cell lands focus on the DataGridCell wrapper and the
            // user has to press Tab a second time to reach the ComboBox before F4 / Alt+Down /
            // Space can toggle the popup.
            AutoFocusOnLoad(factory);

            // Auto-open the dropdown on edit-mode entry. Two paths into this:
            //   • OpenDropDownOnEdit=true (explicit opt-in): always open, regardless of how edit
            //     was entered. Use for columns where browsing the list is the dominant action.
            //   • Mouse-driven edit (default): mouse-down on a cell is an intentional "I want to
            //     change this value" gesture, so saving the user a second click to reach the
            //     list is the right UX. Consume the same SearchDataGrid mouse-edit stash that
            //     TextEditSettings uses for caret-positioning — it's only set when the BeginEdit
            //     was triggered by a mouse gesture (stock click-to-edit or our MouseUp /
            //     MouseUpFocused handlers). Tab / arrow-key / F2 / programmatic edits leave it
            //     null and fall through to "focus the ComboBox but leave the popup closed,"
            //     preserving uninterrupted keyboard grid-navigation.
            // Loaded fires once per template instantiation, which is once per edit-mode entry.
            // The Dispatcher hop defers the open until the focus / layout pipeline has settled;
            // opening synchronously inside Loaded can race focus routing and leave the popup
            // un-focusable.
            bool openOnEdit = OpenDropDownOnEdit;
            factory.AddHandler(FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, _) =>
                {
                    if (s is not ComboBox cb) return;

                    bool shouldOpen = openOnEdit;
                    if (!shouldOpen)
                    {
                        var cell = FindAncestor<DataGridCell>(cb);
                        var grid = FindAncestor<SearchDataGrid>(cell);
                        if (grid != null && cell != null
                            && grid.TryConsumeMouseEditPoint(cell, out Point _))
                        {
                            shouldOpen = true;
                        }
                    }

                    if (!shouldOpen) return;

                    cb.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        cb.IsDropDownOpen = true;
                    }), DispatcherPriority.Input);
                }));

            // Keyboard interaction inside the ComboBox edit cell:
            //   • Tab / Shift+Tab with dropdown open → close popup + refocus ComboBox so
            //     DataGrid's native Tab handler can commit + move to next cell. Don't mark
            //     Handled; DataGrid still needs to see the Tab.
            //   • Arrow keys with dropdown CLOSED → exit cell to navigate adjacent cells
            //     (commits the ComboBox edit and re-raises the arrow on the parent grid).
            //     With dropdown OPEN, arrows pass through to default ComboBox handling and
            //     navigate the dropdown items.
            //   • Space on a non-editable ComboBox → toggle the dropdown. F4 and Alt+Down
            //     are already handled natively by WPF and work for both editable and
            //     non-editable variants; Space is added here as a discoverable shortcut
            //     for the non-editable case only — on an editable ComboBox Space must
            //     pass through as text entry.
            // PreviewKeyDown tunnels root→item, so this fires before any ComboBoxItem can
            // consume the key.
            factory.AddHandler(UIElement.PreviewKeyDownEvent,
                new KeyEventHandler((s, e) =>
                {
                    if (s is not ComboBox cb) return;

                    if (e.Key == Key.Tab && cb.IsDropDownOpen)
                    {
                        cb.IsDropDownOpen = false;
                        cb.Focus();
                        // Don't mark Handled — DataGrid still needs to see the Tab.
                        return;
                    }

                    if (e.Key == Key.Space && !cb.IsEditable)
                    {
                        cb.IsDropDownOpen = !cb.IsDropDownOpen;
                        e.Handled = true;
                        return;
                    }

                    if (!cb.IsDropDownOpen
                        && (e.Key == Key.Left || e.Key == Key.Right
                            || e.Key == Key.Up || e.Key == Key.Down))
                    {
                        e.Handled = true;
                        ExitCellViaArrow(cb, e);
                    }
                }));

            return new DataTemplate { VisualTree = factory };
        }

        private static T FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }

    /// <summary>
    /// One-way converter used by <see cref="ComboBoxEditSettings"/>'s display template to map
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
