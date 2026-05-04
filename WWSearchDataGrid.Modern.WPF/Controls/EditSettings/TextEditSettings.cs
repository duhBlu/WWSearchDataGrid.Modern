using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Plain text editor. Renders the value as a TextBlock in display mode and a TextBox in edit
    /// mode. Honors the column's DisplayValueConverter / DisplayStringFormat / DisplayMask in the
    /// display template. The default editor for string columns.
    /// </summary>
    public class TextEditSettings : BaseEditSettings
    {
        /// <summary>
        /// Optional input mask to apply during edit (forwarded to the column's existing mask
        /// machinery if present). For now this hooks the same path as <see cref="GridColumn.DisplayMask"/>.
        /// </summary>
        public static readonly DependencyProperty InputMaskProperty =
            DependencyProperty.Register(nameof(InputMask), typeof(string), typeof(TextEditSettings),
                new PropertyMetadata(null));

        public string InputMask
        {
            get => (string)GetValue(InputMaskProperty);
            set => SetValue(InputMaskProperty, value);
        }

        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, "SdgDisplayTextBlockStyle");

            // Mirror the existing display-value resolution rules so a column with a Converter or
            // StringFormat shows the formatted value when read-only, just like DataGridTextColumn does.
            var binding = new Binding(column.FieldName) { Mode = BindingMode.OneWay };
            if (column.DisplayValueConverter != null)
            {
                binding.Converter = column.DisplayValueConverter;
                binding.ConverterParameter = column.DisplayConverterParameter;
            }
            if (!string.IsNullOrEmpty(column.DisplayStringFormat))
                binding.StringFormat = column.DisplayStringFormat;

            factory.SetBinding(TextBlock.TextProperty, binding);
            return new DataTemplate { VisualTree = factory };
        }

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            ApplyEditorStyle(factory, "SdgEditTextBoxStyle");
            factory.SetBinding(TextBox.TextProperty, CreateValueBinding(column));

            // Select all when the editor receives focus — standard cell-editing UX.
            factory.AddHandler(UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((s, e) =>
                {
                    if (s is TextBox tb)
                        tb.SelectAll();
                }));

            return new DataTemplate { VisualTree = factory };
        }
    }
}
