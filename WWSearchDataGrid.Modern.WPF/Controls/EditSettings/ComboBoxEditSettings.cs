using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

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
        /// Display mode is a plain TextBlock — no ComboBox chrome. The lookup converter resolves
        /// the bound value to its display text using the configured ItemsSource / DisplayMemberPath
        /// / SelectedValuePath, so a column storing an Id still shows the friendly name.
        /// </summary>
        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            ApplyDisplayStyle(factory, "SdgDisplayTextBlockStyle");

            var binding = new Binding(column.FieldName)
            {
                Mode = BindingMode.OneWay,
                Converter = new ComboBoxValueLookupConverter(ItemsSource, DisplayMemberPath, SelectedValuePath)
            };
            factory.SetBinding(TextBlock.TextProperty, binding);

            return new DataTemplate { VisualTree = factory };
        }

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            // Style FIRST. FrameworkElementFactory has a known quirk where StyleProperty must be
            // set before other SetValue / SetBinding calls; otherwise the Style fails to apply.
            ApplyEditorStyle(factory, "SdgEditComboBoxStyle");

            factory.SetValue(ItemsControl.ItemsSourceProperty, ItemsSource);

            if (!string.IsNullOrEmpty(DisplayMemberPath))
                factory.SetValue(ItemsControl.DisplayMemberPathProperty, DisplayMemberPath);

            if (!string.IsNullOrEmpty(SelectedValuePath))
            {
                factory.SetValue(Selector.SelectedValuePathProperty, SelectedValuePath);
                factory.SetBinding(Selector.SelectedValueProperty, CreateValueBinding(column));
            }
            else
            {
                factory.SetBinding(Selector.SelectedItemProperty, CreateValueBinding(column));
            }

            return new DataTemplate { VisualTree = factory };
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
