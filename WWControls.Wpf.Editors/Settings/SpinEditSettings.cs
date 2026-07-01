using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf.Editors.Settings
{
    /// <summary>
    /// Spinner-style numeric editor. Edit mode is a TextBox with Up/Down arrow-key increments,
    /// optional Min/Max bounds, and digit-only input filtering. Display mode is a TextBlock
    /// with the column's <see cref="GridColumn.DisplayStringFormat"/> applied (e.g. "C2", "N0").
    /// For format-string-driven numeric rendering without spinner UX, use
    /// <see cref="TextEditSettings"/> with <see cref="Core.Display.MaskType.Numeric"/> instead.
    /// </summary>
    public class SpinEditSettings : BaseEditSettings
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double?), typeof(SpinEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double?), typeof(SpinEditSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(nameof(Increment), typeof(double), typeof(SpinEditSettings), new PropertyMetadata(1.0));

        /// <summary>
        /// Step applied by the Ctrl+Shift+Up / Ctrl+Shift+Down keyboard gesture. Default
        /// <c>10</c> — useful when <see cref="Increment"/> is small and the user wants a quick
        /// jump (e.g. Increment=1 / LargeIncrement=10, or Increment=0.25 / LargeIncrement=1).
        /// </summary>
        public static readonly DependencyProperty LargeIncrementProperty =
            DependencyProperty.Register(nameof(LargeIncrement), typeof(double), typeof(SpinEditSettings), new PropertyMetadata(10.0));

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

        public double LargeIncrement
        {
            get => (double)GetValue(LargeIncrementProperty);
            set => SetValue(LargeIncrementProperty, value);
        }

        public override DataTemplate CreateDisplayTemplate(IEditorColumn column)
        {
            var grid = BuildSpinHostGrid();

            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            ApplyDisplayStyle(textBlock, EditorThemeKeys.DisplayNumericTextBlock);
            ApplyTextAlignment(textBlock, column);

            var binding = column.CreateFieldBinding();
            binding.Mode = BindingMode.OneWay;
            if (column.DisplayValueConverter != null)
            {
                binding.Converter = column.DisplayValueConverter;
                binding.ConverterParameter = column.DisplayConverterParameter;
            }
            if (!string.IsNullOrEmpty(column.DisplayStringFormat))
                binding.StringFormat = column.DisplayStringFormat;

            textBlock.SetBinding(TextBlock.TextProperty, binding);
            // Display element validates against the INotifyDataErrorInfo row by default; the badge
            // is the library's error surface, so strip WPF's red adorner. See TextEditSettings.
            SuppressValidationErrorAdorner(textBlock);
            textBlock.SetValue(Grid.ColumnProperty, 0);
            textBlock.SetValue(Grid.RowSpanProperty, 2);
            grid.AppendChild(textBlock);

            // Display-mode buttons enter edit mode on click — the user opted into seeing them via
            // EditorButtonShowMode, so clicking expresses edit intent. Increment doesn't fire
            // directly from display mode (WWSpinEdit owns the actual increment buttons once the
            // cell is editing).
            grid.AppendChild(BuildSpinDisplayButton(this, column, isUp: true));
            grid.AppendChild(BuildSpinDisplayButton(this, column, isUp: false));

            return new DataTemplate { VisualTree = grid };
        }

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
            => WithNullability(new[]
            {
                Core.SearchType.Equals, Core.SearchType.NotEquals,
                Core.SearchType.GreaterThan, Core.SearchType.LessThan,
                Core.SearchType.GreaterThanOrEqualTo, Core.SearchType.LessThanOrEqualTo,
            }, isNullable);

        public override UIElement CreateFilterDisplay(IFilterEditorHost host)
        {
            // Read-only display: TextBlock with the column's DisplayStringFormat (e.g. "C2", "N0"),
            // mirroring the cell display template. No spinner buttons in display mode — they
            // appear when the cell enters edit.
            var tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0),
            };
            var style = Application.Current?.TryFindResource(EditorThemeKeys.DisplayNumericTextBlock) as Style;
            if (style != null) tb.Style = style;

            var binding = new Binding("SearchValue")
            {
                Source = host,
                Mode = BindingMode.OneWay,
            };
            var column = host?.EditorColumn;
            if (column?.DisplayValueConverter != null)
            {
                binding.Converter = column.DisplayValueConverter;
                binding.ConverterParameter = column.DisplayConverterParameter;
            }
            if (!string.IsNullOrEmpty(column?.DisplayStringFormat))
                binding.StringFormat = column.DisplayStringFormat;

            BindingOperations.SetBinding(tb, TextBlock.TextProperty, binding);
            return tb;
        }

        public override UIElement CreateFilterEditor(IFilterEditorHost host)
        {
            // NumericUpDown matches the cell editor's spinner UX. Value is object-typed on
            // NumericUpDown; the filter pipeline's engine coerces to the column's runtime
            // type when applying the filter.
            var spin = new NumericUpDown
            {
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            if (Minimum.HasValue) spin.Minimum = (int)Minimum.Value;
            if (Maximum.HasValue) spin.Maximum = (int)Maximum.Value;
            spin.Increment = (int)Math.Max(1, Increment);

            BindingOperations.SetBinding(spin, NumericUpDown.ValueProperty, new Binding("SearchValue")
            {
                Source = host,
                Mode = BindingMode.TwoWay,
            });
            return spin;
        }

        public override DataTemplate CreateEditTemplate(IEditorColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(WWSpinEdit));

            // The editor owns its own border and draws it by default, flattening itself when it
            // detects a grid cell — so the cell edit template needs no border wiring.
            if (Minimum.HasValue) factory.SetValue(WWSpinEdit.MinimumProperty, Minimum);
            if (Maximum.HasValue) factory.SetValue(WWSpinEdit.MaximumProperty, Maximum);
            factory.SetValue(WWSpinEdit.IncrementProperty, Increment);
            factory.SetValue(WWSpinEdit.LargeIncrementProperty, LargeIncrement);

            factory.SetBinding(WWBaseEdit.ValueProperty, CreateValueBinding(column));
            SuppressValidationErrorAdorner(factory);

            // Grid-cell interaction — focus-on-edit, mouse-click caret, Left/Right caret-boundary
            // exit and bare Up/Down row exit — is layered on by the grid-side host. The control
            // keeps Ctrl+Up/Down (and Ctrl+Shift for the large step) for increment, and the host
            // ignores modified arrows, so the two don't collide.
            factory.SetValue(EditorHostBehavior.HostInCellProperty, true);

            return new DataTemplate { VisualTree = factory };
        }

        /// <summary>
        /// Two-column / two-row Grid host: column 0 stretches and row-spans both rows for the
        /// TextBox / TextBlock; column 1 is auto-sized with one row per button (up on row 0,
        /// down on row 1).
        /// </summary>
        private static FrameworkElementFactory BuildSpinHostGrid()
        {
            var grid = new FrameworkElementFactory(typeof(Grid));

            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);

            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            grid.AppendChild(col1);

            var row0 = new FrameworkElementFactory(typeof(RowDefinition));
            row0.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(row0);

            var row1 = new FrameworkElementFactory(typeof(RowDefinition));
            row1.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(row1);

            return grid;
        }

        /// <summary>
        /// Builds a single up or down spin button for the read-only display template. Its
        /// <see cref="UIElement.Visibility"/> is bound through
        /// <see cref="EditorButtonVisibilityConverter"/> (so it shows per
        /// <c>EditorButtonShowMode</c> + cell/row state); clicking enters edit mode, where the
        /// hosted <see cref="WWSpinEdit"/>'s own up/down buttons drive the actual increment.
        /// </summary>
        private static FrameworkElementFactory BuildSpinDisplayButton(SpinEditSettings settings, IEditorColumn column, bool isUp)
        {
            var btn = new FrameworkElementFactory(typeof(RepeatButton));
            // Style FIRST. FrameworkElementFactory has a known quirk where StyleProperty must be
            // set before other SetValue / SetBinding calls; otherwise the Style fails to apply.
            ApplyKeyedStyle(btn, EditorThemeKeys.SpinButton);
            btn.SetValue(Grid.ColumnProperty, 1);
            btn.SetValue(Grid.RowProperty, isUp ? 0 : 1);

            // Glyph passed via Content; the SpinButton style's template renders it as a Fluent
            // icon TextBlock. ChevronUp = U+E70E, ChevronDown = U+E70D.
            btn.SetValue(ContentControl.ContentProperty, isUp ? "" : "");

            // Visibility tracks the EditorButtonShowMode + cell/row state.
            var visBinding = BuildEditorButtonVisibilityBinding(settings, column);
            if (visBinding != null)
                btn.SetBinding(UIElement.VisibilityProperty, visBinding);

            btn.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, _) =>
            {
                if (s is DependencyObject d) EnsureCellEditing(d);
            }));

            return btn;
        }

    }
}
