using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Numeric editor. Edit mode is a TextBox with Up/Down arrow-key increments, optional
    /// Min/Max bounds, and digit-only input filtering. Display mode is a TextBlock with the
    /// column's <see cref="GridColumn.DisplayStringFormat"/> applied (e.g. "C2", "N0").
    /// </summary>
    public class NumericEditSettings : BaseEditSettings
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double?), typeof(NumericEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double?), typeof(NumericEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(nameof(Increment), typeof(double), typeof(NumericEditSettings), new PropertyMetadata(1.0));

        public double? Minimum
        {
            get => (double?)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double? Maximum
        {
            get => (double?)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Increment
        {
            get => (double)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            // Right-aligned numeric variant extends SdgDisplayTextBlockStyle.
            ApplyDisplayStyle(factory, "SdgDisplayNumericTextBlockStyle");

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
            ApplyEditorStyle(factory, "SdgEditNumericTextBoxStyle");
            factory.SetBinding(TextBox.TextProperty, CreateValueBinding(column));

            // Filter input: digits, decimal point, sign. Lets binding-time conversion handle the
            // numeric parse (which respects the actual property type — int rejects "1.5", etc.).
            factory.AddHandler(UIElement.PreviewTextInputEvent,
                new TextCompositionEventHandler((_, e) =>
                {
                    foreach (var ch in e.Text)
                    {
                        if (!char.IsDigit(ch) && ch != '.' && ch != ',' && ch != '-' && ch != '+')
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }));

            // Capture in closures so each cell's TextBox sees the same min/max/increment values.
            var min = Minimum;
            var max = Maximum;
            var step = Increment;

            factory.AddHandler(UIElement.PreviewKeyDownEvent,
                new KeyEventHandler((s, e) =>
                {
                    if (s is not TextBox tb) return;
                    if (e.Key != Key.Up && e.Key != Key.Down) return;

                    if (!double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var current))
                        current = 0;

                    var next = e.Key == Key.Up ? current + step : current - step;
                    if (min.HasValue) next = Math.Max(min.Value, next);
                    if (max.HasValue) next = Math.Min(max.Value, next);

                    tb.Text = next.ToString(CultureInfo.CurrentCulture);
                    tb.SelectAll();
                    e.Handled = true;
                }));

            factory.AddHandler(UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((s, _) =>
                {
                    if (s is TextBox tb)
                        tb.SelectAll();
                }));

            return new DataTemplate { VisualTree = factory };
        }
    }
}
