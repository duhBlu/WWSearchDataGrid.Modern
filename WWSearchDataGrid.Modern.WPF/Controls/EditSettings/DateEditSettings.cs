using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Date editor. Edit mode uses a <see cref="DatePicker"/>; display mode is a TextBlock
    /// formatted with the column's <see cref="GridColumn.DisplayStringFormat"/> (or a default
    /// short-date format if none was specified).
    /// </summary>
    public class DateEditSettings : BaseEditSettings
    {
        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(DateEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(DateEditSettings), new PropertyMetadata(null));

        /// <summary>Optional lower bound applied to the DatePicker.</summary>
        public DateTime? MinDate
        {
            get => (DateTime?)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        /// <summary>Optional upper bound applied to the DatePicker.</summary>
        public DateTime? MaxDate
        {
            get => (DateTime?)GetValue(MaxDateProperty);
            set => SetValue(MaxDateProperty, value);
        }

        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, "SdgDisplayTextBlockStyle");

            var binding = new Binding(column.FieldName) { Mode = BindingMode.OneWay };
            // Default to short date if the consumer didn't supply one — matches DatePicker's display.
            binding.StringFormat = string.IsNullOrEmpty(column.DisplayStringFormat) ? "d" : column.DisplayStringFormat;

            factory.SetBinding(TextBlock.TextProperty, binding);
            return new DataTemplate { VisualTree = factory };
        }

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(DatePicker));
            ApplyEditorStyle(factory, "SdgEditDatePickerStyle");
            factory.SetBinding(DatePicker.SelectedDateProperty, CreateValueBinding(column));

            if (MinDate.HasValue)
                factory.SetValue(DatePicker.DisplayDateStartProperty, MinDate.Value);
            if (MaxDate.HasValue)
                factory.SetValue(DatePicker.DisplayDateEndProperty, MaxDate.Value);

            return new DataTemplate { VisualTree = factory };
        }
    }
}
