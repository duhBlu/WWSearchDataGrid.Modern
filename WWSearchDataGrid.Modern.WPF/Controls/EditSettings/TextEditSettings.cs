using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core.Display;
using WWSearchDataGrid.Modern.WPF.Behaviors;
using WWSearchDataGrid.Modern.WPF.Converters;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Plain text editor. TextBlock in display, TextBox in edit. Honors the column's
    /// <c>DisplayValueConverter</c> / <c>DisplayStringFormat</c> / <c>DisplayMask</c> for the
    /// read-only view, plus an optional input mask via <see cref="Mask"/> /
    /// <see cref="MaskType"/> for keystroke-by-keystroke validation in edit mode.
    /// </summary>
    /// <remarks>
    /// The mask API mirrors the conceptual model used by DevExpress and similar editor
    /// frameworks: a <see cref="Mask"/> pattern, a <see cref="MaskType"/> to interpret it,
    /// and <see cref="UseMaskAsDisplayFormat"/> to choose whether the mask formats the
    /// display TextBlock as well as the editing TextBox. See <see cref="Core.Display.MaskType"/>
    /// for the implemented engines (<c>Simple</c>, <c>Numeric</c>, <c>DateTime</c> /
    /// <c>DateOnly</c> / <c>TimeOnly</c>, <c>TimeSpan</c>). Types without an engine yet throw
    /// <see cref="NotSupportedException"/> at template-build time.
    /// <para>
    /// When <see cref="Mask"/> isn't set explicitly, the templates fall through to the
    /// column's <c>DisplayMask</c>; failing that, a <c>DisplayStringFormat</c> that names a
    /// supported Numeric format (<c>C</c>/<c>N</c>/<c>F</c>/<c>P</c>, optional precision) is
    /// adopted as the mask with <see cref="Core.Display.MaskType.Numeric"/>. So a column with
    /// just <c>DisplayStringFormat="C2"</c> and no explicit <see cref="Mask"/> still gets
    /// keystroke-validated currency input.
    /// </para>
    /// </remarks>
    public class TextEditSettings : BaseEditSettings
    {
        /// <summary>
        /// Optional mask pattern. When set, the editor TextBox is wired through
        /// <see cref="MaskInputBehavior"/> for keystroke validation, region-aware Tab
        /// navigation, mask-aware paste, and finalize-on-blur. The pattern grammar is
        /// determined by <see cref="MaskType"/>.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(TextEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// How the <see cref="Mask"/> pattern is interpreted. Default
        /// <see cref="Core.Display.MaskType.Simple"/>. See <see cref="Core.Display.MaskType"/>
        /// for the implemented engines; types without an engine throw
        /// <see cref="NotSupportedException"/> at template-build time.
        /// </summary>
        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(TextEditSettings),
                new PropertyMetadata(Core.Display.MaskType.Simple));

        /// <summary>
        /// When <c>true</c>, the display TextBlock formats the bound value through the same
        /// mask pattern used in edit mode — display and edit text become identical (the
        /// DevExpress equivalent of the property of the same name). When <c>false</c>
        /// (default), the display path uses the column's <c>DisplayValueConverter</c> /
        /// <c>DisplayStringFormat</c>, and the mask only kicks in during editing.
        /// </summary>
        public static readonly DependencyProperty UseMaskAsDisplayFormatProperty =
            DependencyProperty.Register(nameof(UseMaskAsDisplayFormat), typeof(bool), typeof(TextEditSettings),
                new PropertyMetadata(false));

        /// <summary>Mask pattern. See <see cref="MaskType"/> for grammar.</summary>
        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        /// <summary>How the <see cref="Mask"/> pattern is interpreted.</summary>
        public MaskType MaskType
        {
            get => (MaskType)GetValue(MaskTypeProperty);
            set => SetValue(MaskTypeProperty, value);
        }

        /// <summary>Whether the mask is also used to format the display (non-editing) text.</summary>
        public bool UseMaskAsDisplayFormat
        {
            get => (bool)GetValue(UseMaskAsDisplayFormatProperty);
            set => SetValue(UseMaskAsDisplayFormatProperty, value);
        }

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
        {
            // Numeric mask → numeric operators. The mask configuration is the authoritative
            // signal: ColumnDataType detection runs from sampled values at filter-row init
            // time and can lag the items-source binding or default to String, so we can't
            // rely on it. Mask is set at column-generation time and is reliable.
            if (MaskType == Core.Display.MaskType.Numeric)
            {
                return WithNullability(new[]
                {
                    Core.SearchType.Equals, Core.SearchType.NotEquals,
                    Core.SearchType.GreaterThan, Core.SearchType.LessThan,
                    Core.SearchType.GreaterThanOrEqualTo, Core.SearchType.LessThanOrEqualTo,
                    Core.SearchType.Between, Core.SearchType.NotBetween,
                }, isNullable);
            }

            return WithNullability(new[]
            {
                Core.SearchType.Contains, Core.SearchType.DoesNotContain,
                Core.SearchType.StartsWith, Core.SearchType.EndsWith,
                Core.SearchType.IsLike, Core.SearchType.IsNotLike,
                Core.SearchType.Equals, Core.SearchType.NotEquals,
            }, isNullable);
        }

        public override DataTemplate CreateDisplayTemplate(ColumnDataBase column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, EditSettingsThemeKeys.DisplayTextBlock);
            ApplyTextAlignment(factory, column);

            var binding = new Binding(column.FieldName) { Mode = BindingMode.OneWay };

            var (effectiveMask, effectiveMaskType) = ResolveEffectiveMask(column);

            if (UseMaskAsDisplayFormat && !string.IsNullOrEmpty(effectiveMask))
            {
                // Mask-as-display: route through MaskFormatConverter so the display string is
                // identical to what the user sees while editing. Suppresses StringFormat and
                // DisplayValueConverter — the mask wins.
                MaskFormatterFactory.EnsureSupported(effectiveMaskType);
                binding.Converter = new MaskFormatConverter(effectiveMaskType);
                binding.ConverterParameter = effectiveMask;
            }
            else
            {
                // Mirror the existing display-value resolution rules so a column with a Converter
                // or StringFormat shows the formatted value when read-only, just like
                // DataGridTextColumn does.
                if (column.DisplayValueConverter != null)
                {
                    binding.Converter = column.DisplayValueConverter;
                    binding.ConverterParameter = column.DisplayConverterParameter;
                }
                if (!string.IsNullOrEmpty(column.DisplayStringFormat))
                    binding.StringFormat = column.DisplayStringFormat;
            }

            factory.SetBinding(TextBlock.TextProperty, binding);
            // Binding.ValidatesOnNotifyDataErrors defaults on, so a display element bound to a
            // property of an INotifyDataErrorInfo row draws WPF's red error adorner whenever that
            // property reports an error. The library surfaces errors through the cell's
            // ValidationErrorIcon badge, so strip the adorner from the read-only display element.
            SuppressValidationErrorAdorner(factory);
            return new DataTemplate { VisualTree = factory };
        }

        public override DataTemplate CreateEditTemplate(ColumnDataBase column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            ApplyEditorStyle(factory, EditSettingsThemeKeys.EditTextBox);
            ApplyTextAlignment(factory, column);

            // Wire MaskInputBehavior when a mask resolves. Resolution order: explicit Mask >
            // column.DisplayMask > column.DisplayStringFormat (auto-adopted with Numeric type
            // when the format is C/N/F/P). The third hop lets a single DisplayStringFormat
            // declaration cover both the read-only display (via Binding.StringFormat) and the
            // edit-time mask, without the consumer having to also configure Mask.
            var (effectiveMask, effectiveMaskType) = ResolveEffectiveMask(column);
            var editBinding = CreateValueBinding(column);
            if (!string.IsNullOrEmpty(effectiveMask))
            {
                MaskFormatterFactory.EnsureSupported(effectiveMaskType);
                // Numeric masks render chrome (currency symbol, group separators, percent sign)
                // that the default string→decimal binding parser can't handle. Route the edit
                // binding through MaskFormatConverter so ConvertBack strips chrome and returns
                // the underlying value before WPF converts to the target property type.
                if (effectiveMaskType == Core.Display.MaskType.Numeric)
                {
                    editBinding.Converter = new MaskFormatConverter(effectiveMaskType);
                    editBinding.ConverterParameter = effectiveMask;
                }
                // Set MaskType BEFORE Mask so the MaskInputBehavior.OnMaskChanged callback
                // reads the right type when constructing the formatter through the factory.
                // Reversing this order means OnMaskChanged sees the default Simple type and
                // builds a SimpleMaskFormatter against (e.g.) "C" or "P2" — the literal collapses
                // the textbox display to the bare format-string character on focus enter.
                factory.SetValue(MaskInputBehavior.MaskTypeProperty, effectiveMaskType);
                factory.SetValue(MaskInputBehavior.MaskProperty, effectiveMask);
            }
            factory.SetBinding(TextBox.TextProperty, editBinding);
            SuppressValidationErrorAdorner(factory);

            // Select all when the editor receives focus — standard cell-editing UX for keyboard
            // entry (Tab / Enter / F2 / programmatic). When edit mode was triggered by a mouse
            // click, the SearchDataGrid stashes the click point and we instead place the caret
            // at the clicked text index — matching the behavior the user would get clicking into
            // a regular TextBox. Skipped when MaskInputBehavior is active because its own
            // GotFocus handler selects the first editable region; both branches would clobber it.
            factory.AddHandler(UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((s, e) =>
                {
                    if (s is not TextBox tb) return;
                    if (!string.IsNullOrEmpty(MaskInputBehavior.GetMask(tb))) return;

                    if (TryApplyMouseClickCaret(tb)) return;
                    tb.SelectAll();
                }));

            // Single-click in: focus lands on the TextBox so the user can type immediately
            // (and the GotKeyboardFocus handler above selects existing text). Without this,
            // edit mode starts but focus stays on the cell, requiring a second click.
            AutoFocusOnLoad(factory);

            // Arrow keys with caret at a boundary (or in SelectAll state) exit the cell to
            // navigate the grid; otherwise they move the caret normally. Ctrl/Shift+Arrow
            // defer to TextBox default (jump / extend selection).
            AddTextBoxCaretAwareArrowExit(factory);

            return new DataTemplate { VisualTree = factory };
        }

        /// <summary>
        /// Mouse-driven edit entry: pull the click point the SearchDataGrid stashed for this
        /// cell, project it into <paramref name="tb"/>'s coordinate space, and set
        /// <see cref="TextBox.CaretIndex"/> to the character at that point. Returns false (and
        /// makes no changes) when there's no pending point — the caller falls back to select-all.
        /// </summary>
        private static bool TryApplyMouseClickCaret(TextBox tb)
        {
            var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(tb);
            if (cell == null) return false;
            var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(cell);
            if (grid == null) return false;
            if (!grid.TryConsumeMouseEditPoint(cell, out var cellPoint)) return false;

            try
            {
                var pointInTb = cell.TranslatePoint(cellPoint, tb);
                int idx = tb.GetCharacterIndexFromPoint(pointInTb, snapToText: true);
                if (idx < 0) return false;

                // Snap-to-end: GetCharacterIndexFromPoint(snapToText:true) returns the index of
                // the closest character — for a click past the trailing edge of the last
                // character it returns Text.Length - 1, leaving the caret one slot short of the
                // end. Compare the click X to the post-text caret rect; if the click is at or
                // past it, set the caret after the last character instead.
                int textLen = tb.Text?.Length ?? 0;
                if (textLen > 0)
                {
                    var endRect = tb.GetRectFromCharacterIndex(textLen, false);
                    if (!endRect.IsEmpty && pointInTb.X >= endRect.X)
                        idx = textLen;
                }

                tb.CaretIndex = idx;
                tb.SelectionLength = 0;
                return true;
            }
            catch
            {
                // TranslatePoint can throw if the elements aren't in a common visual tree
                // (e.g., the cell was unloaded between BeginEdit and focus). Fall through to
                // select-all rather than leaving the caret in an undefined state.
                return false;
            }
        }

        /// <summary>
        /// Resolves the effective mask pattern + engine type for a column. Order:
        /// <list type="number">
        /// <item>Explicit <see cref="Mask"/> — uses the instance's <see cref="MaskType"/>.</item>
        /// <item><see cref="GridColumn.DisplayMask"/> — uses the instance's <see cref="MaskType"/>
        /// (defaults to <see cref="Core.Display.MaskType.Simple"/>).</item>
        /// <item><see cref="GridColumn.DisplayStringFormat"/> when it names a supported numeric
        /// format (<c>C</c>/<c>N</c>/<c>F</c>/<c>P</c>, optional precision) — adopted as the
        /// mask with <see cref="Core.Display.MaskType.Numeric"/>.</item>
        /// </list>
        /// Returns <c>(null, MaskType)</c> when nothing applies — the caller leaves
        /// <see cref="MaskInputBehavior"/> unwired and the TextBox behaves like a plain editor.
        /// </summary>
        private (string mask, MaskType maskType) ResolveEffectiveMask(ColumnDataBase column)
        {
            if (!string.IsNullOrEmpty(Mask))
                return (Mask, MaskType);
            if (column != null && !string.IsNullOrEmpty(column.DisplayMask))
                return (column.DisplayMask, MaskType);
            if (column != null && IsNumericMaskFormat(column.DisplayStringFormat))
                return (column.DisplayStringFormat, Core.Display.MaskType.Numeric);
            return (null, MaskType);
        }

        /// <summary>
        /// True when <paramref name="format"/> is a standard .NET numeric format the
        /// <c>NumericMaskFormatter</c> can interpret — currency (<c>C</c>), number (<c>N</c>),
        /// fixed-point (<c>F</c>), or percent (<c>P</c>), with optional precision. Other format
        /// strings (custom <c>#,##0.00</c>, hex <c>X</c>, exponential <c>E</c>, date patterns)
        /// don't round-trip through the engine and stay on the StringFormat-only display path.
        /// </summary>
        private static bool IsNumericMaskFormat(string format)
        {
            if (string.IsNullOrEmpty(format)) return false;
            char first = char.ToUpperInvariant(format[0]);
            return first == 'C' || first == 'N' || first == 'F' || first == 'P';
        }
    }
}
