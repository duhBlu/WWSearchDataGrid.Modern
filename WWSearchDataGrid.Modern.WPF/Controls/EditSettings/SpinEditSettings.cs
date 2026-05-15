using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
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

        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var grid = BuildSpinHostGrid();

            var textBlock = new FrameworkElementFactory(typeof(TextBlock));
            ApplyDisplayStyle(textBlock, EditSettingsThemeKeys.DisplayNumericTextBlock);
            ApplyTextAlignment(textBlock, column);

            var binding = new Binding(column.FieldName) { Mode = BindingMode.OneWay };
            if (column.DisplayValueConverter != null)
            {
                binding.Converter = column.DisplayValueConverter;
                binding.ConverterParameter = column.DisplayConverterParameter;
            }
            if (!string.IsNullOrEmpty(column.DisplayStringFormat))
                binding.StringFormat = column.DisplayStringFormat;

            textBlock.SetBinding(TextBlock.TextProperty, binding);
            textBlock.SetValue(Grid.ColumnProperty, 0);
            textBlock.SetValue(Grid.RowSpanProperty, 2);
            grid.AppendChild(textBlock);

            // Display-mode buttons enter edit mode on click — the user opted into seeing them via
            // EditorButtonShowMode, so clicking expresses edit intent. Increment doesn't fire
            // directly from display mode (the actual increment happens in the edit template's
            // arrow-key handler once the cell is editing).
            grid.AppendChild(BuildSpinButton(this, column, isUp: true, isDisplayMode: true));
            grid.AppendChild(BuildSpinButton(this, column, isUp: false, isDisplayMode: true));

            return new DataTemplate { VisualTree = grid };
        }

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
            => WithNullability(new[]
            {
                Core.SearchType.Equals, Core.SearchType.NotEquals,
                Core.SearchType.GreaterThan, Core.SearchType.LessThan,
                Core.SearchType.GreaterThanOrEqualTo, Core.SearchType.LessThanOrEqualTo,
            }, isNullable);

        public override UIElement CreateFilterDisplay(IColumnFilterHost host)
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
            var style = Application.Current?.TryFindResource(EditSettingsThemeKeys.DisplayNumericTextBlock) as Style;
            if (style != null) tb.Style = style;

            var binding = new Binding("SearchValue")
            {
                Source = host,
                Mode = BindingMode.OneWay,
            };
            var column = host?.GridColumn;
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

        public override UIElement CreateFilterEditor(IColumnFilterHost host)
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

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            var grid = BuildSpinHostGrid();

            var factory = new FrameworkElementFactory(typeof(TextBox));
            ApplyEditorStyle(factory, EditSettingsThemeKeys.EditNumericTextBox);
            ApplyTextAlignment(factory, column);
            factory.SetBinding(TextBox.TextProperty, CreateValueBinding(column));
            factory.SetValue(Grid.ColumnProperty, 0);
            factory.SetValue(Grid.RowSpanProperty, 2);

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
            var largeStep = LargeIncrement;

            // Increment requires Ctrl: Ctrl+Up/Down adds/subtracts Increment, Ctrl+Shift+Up/Down
            // uses LargeIncrement. Bare Up/Down falls through to AddTextBoxCaretAwareArrowExit
            // (registered below) which commits the cell and moves DataGrid focus to the adjacent
            // row — consistent with every other editor's arrow-key behavior. Without the Ctrl
            // gate, the spinner would steal row navigation from the user.
            factory.AddHandler(UIElement.PreviewKeyDownEvent,
                new KeyEventHandler((s, e) =>
                {
                    if (s is not TextBox tb) return;
                    if (e.Key != Key.Up && e.Key != Key.Down) return;
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

                    bool large = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                    var delta = large ? largeStep : step;

                    if (!double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var current))
                        current = 0;

                    var next = e.Key == Key.Up ? current + delta : current - delta;
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

            // Single-click in: focus lands on the TextBox so the user can type / arrow-key
            // immediately. Without this, edit mode starts but focus stays on the cell.
            AutoFocusOnLoad(factory);

            // Arrow keys with caret at a boundary (or in SelectAll state) exit the cell to
            // navigate the grid; otherwise they move the caret. Up/Down arrows on the
            // spinner editor are claimed for increment/decrement by the PreviewKeyDown
            // handler above, which marks them Handled before the arrow-exit helper sees
            // them — so increment behavior wins on Up/Down, exit behavior wins on Left/Right.
            AddTextBoxCaretAwareArrowExit(factory);

            grid.AppendChild(factory);
            grid.AppendChild(BuildSpinButton(this, column, isUp: true, isDisplayMode: false));
            grid.AppendChild(BuildSpinButton(this, column, isUp: false, isDisplayMode: false));

            return new DataTemplate { VisualTree = grid };
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
        /// Builds a single up or down spin button. Two display modes share the same visual:
        /// <list type="bullet">
        ///   <item><c>isDisplayMode=true</c>: the button lives in the read-only display template
        ///   and its <see cref="UIElement.Visibility"/> is bound through
        ///   <see cref="Converters.EditorButtonVisibilityConverter"/>. Click enters edit mode —
        ///   the user can then use the now-active edit template's button to increment.</item>
        ///   <item><c>isDisplayMode=false</c>: the button lives in the edit template; always
        ///   visible, click increments the bound value via the same numeric path the keyboard
        ///   Up/Down handler uses.</item>
        /// </list>
        /// </summary>
        private static FrameworkElementFactory BuildSpinButton(SpinEditSettings settings, GridColumn column, bool isUp, bool isDisplayMode)
        {
            var btn = new FrameworkElementFactory(typeof(RepeatButton));
            // Style FIRST. FrameworkElementFactory has a known quirk where StyleProperty must be
            // set before other SetValue / SetBinding calls; otherwise the Style fails to apply.
            ApplyKeyedStyle(btn, EditSettingsThemeKeys.SpinButton);
            btn.SetValue(Grid.ColumnProperty, 1);
            btn.SetValue(Grid.RowProperty, isUp ? 0 : 1);

            // Glyph passed via Content; the SpinButton style's template renders it as a Fluent
            // icon TextBlock. ChevronUp = U+E70E, ChevronDown = U+E70D.
            btn.SetValue(ContentControl.ContentProperty, isUp ? "" : "");

            // Capture spin parameters by value so each cell's button operates against the
            // settings instance configured at column-generation time.
            var step = settings.Increment;
            var min = settings.Minimum;
            var max = settings.Maximum;
            var fieldName = column.FieldName;
            double sign = isUp ? +1 : -1;

            if (isDisplayMode)
            {
                // Visibility tracks the EditorButtonShowMode + cell/row state. Edit-template buttons
                // always show — they're inside the edit template that's only mounted when editing.
                var visBinding = BuildEditorButtonVisibilityBinding(settings, column);
                if (visBinding != null)
                    btn.SetBinding(UIElement.VisibilityProperty, visBinding);

                btn.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, _) =>
                {
                    if (s is DependencyObject d) EnsureCellEditing(d);
                }));
            }
            else
            {
                btn.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, _) =>
                {
                    var cell = FindVisualAncestor<DataGridCell>(s as DependencyObject);
                    if (cell == null) return;
                    var tb = FindVisualDescendant<TextBox>(cell);
                    if (tb == null) return;

                    if (!double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var current))
                        current = 0;
                    var next = current + sign * step;
                    if (min.HasValue) next = Math.Max(min.Value, next);
                    if (max.HasValue) next = Math.Min(max.Value, next);

                    tb.Text = next.ToString(CultureInfo.CurrentCulture);
                    tb.SelectAll();
                    tb.Focus();
                }));
            }

            return btn;
        }

        /// <summary>Walks the visual tree downward to find the first descendant of type <typeparamref name="T"/>.</summary>
        private static T FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                var deeper = FindVisualDescendant<T>(child);
                if (deeper != null) return deeper;
            }
            return null;
        }

        // Re-declare visual-tree helper here to avoid bumping accessibility on the BaseEditSettings
        // private. Same logic as the base class's FindVisualAncestor.
        private static T FindVisualAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
