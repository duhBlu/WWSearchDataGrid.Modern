using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WWSearchDataGrid.Modern.WPF.Converters;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Masked text editor. Display mode applies the configured <see cref="Mask"/> via
    /// <see cref="MaskFormatConverter"/> (e.g. "(000) 000-0000" → "(555) 123-4567"). Edit mode is
    /// a plain TextBox — full enforcement of the mask during keystrokes is not implemented in
    /// this version; the source value is stored unmasked.
    /// </summary>
    /// <remarks>
    /// If the column already declares <see cref="GridColumn.DisplayMask"/>, that value is used
    /// as a fallback when the editor's <see cref="Mask"/> is not set, so consumers can configure
    /// the mask in either place.
    /// </remarks>
    public class MaskedEditSettings : BaseEditSettings
    {
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(MaskedEditSettings), new PropertyMetadata(null));

        /// <summary>
        /// Mask pattern (e.g. "(000) 000-0000" for phone numbers, "00/00/0000" for dates).
        /// Falls back to <see cref="GridColumn.DisplayMask"/> when null.
        /// </summary>
        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, "SdgDisplayTextBlockStyle");

            var binding = new Binding(column.FieldName) { Mode = BindingMode.OneWay };
            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : column.DisplayMask;
            if (!string.IsNullOrEmpty(effectiveMask))
            {
                binding.Converter = new MaskFormatConverter();
                binding.ConverterParameter = effectiveMask;
            }

            factory.SetBinding(TextBlock.TextProperty, binding);
            return new DataTemplate { VisualTree = factory };
        }

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            // Plain TextBox for edit. Real keystroke-masked input is a separate piece of work
            // (would need a custom MaskedTextBox control); for now the source value is stored
            // unformatted and the mask is reapplied on commit via the display template.
            var factory = new FrameworkElementFactory(typeof(TextBox));
            ApplyEditorStyle(factory, "SdgEditTextBoxStyle");
            factory.SetBinding(TextBox.TextProperty, CreateValueBinding(column));

            factory.AddHandler(UIElement.GotKeyboardFocusEvent,
                new System.Windows.Input.KeyboardFocusChangedEventHandler((s, _) =>
                {
                    if (s is TextBox tb)
                        tb.SelectAll();
                }));

            return new DataTemplate { VisualTree = factory };
        }
    }
}
