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
    /// display TextBlock as well as the editing TextBox. Currently only
    /// <see cref="Core.Display.MaskType.Simple"/> is implemented; other types throw
    /// <see cref="NotSupportedException"/>.
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
        /// <see cref="Core.Display.MaskType.Simple"/>. Other types throw
        /// <see cref="NotSupportedException"/> at template-build time until their respective
        /// engines are implemented.
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

        public override DataTemplate CreateDisplayTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, EditSettingsThemeKeys.DisplayTextBlock);
            ApplyTextAlignment(factory, column);

            var binding = new Binding(column.FieldName) { Mode = BindingMode.OneWay };

            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : column.DisplayMask;

            if (UseMaskAsDisplayFormat && !string.IsNullOrEmpty(effectiveMask))
            {
                // Mask-as-display: route through MaskFormatConverter so the display string is
                // identical to what the user sees while editing. Suppresses StringFormat and
                // DisplayValueConverter — the mask wins.
                MaskFormatterFactory.EnsureSupported(MaskType);
                binding.Converter = new MaskFormatConverter(MaskType);
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
            return new DataTemplate { VisualTree = factory };
        }

        public override DataTemplate CreateEditTemplate(GridColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            ApplyEditorStyle(factory, EditSettingsThemeKeys.EditTextBox);
            ApplyTextAlignment(factory, column);

            // Wire MaskInputBehavior when a Mask is configured. Falls back to the column-level
            // DisplayMask so a single declaration on the column can cover both display formatting
            // (via Mask + UseMaskAsDisplayFormat or the legacy DisplayMask path) and edit-time
            // keystroke validation.
            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : column.DisplayMask;
            var editBinding = CreateValueBinding(column);
            if (!string.IsNullOrEmpty(effectiveMask))
            {
                MaskFormatterFactory.EnsureSupported(MaskType);
                // Numeric masks render chrome (currency symbol, group separators, percent sign)
                // that the default string→decimal binding parser can't handle. Route the edit
                // binding through MaskFormatConverter so ConvertBack strips chrome and returns
                // the underlying value before WPF converts to the target property type.
                if (MaskType == Core.Display.MaskType.Numeric)
                {
                    editBinding.Converter = new MaskFormatConverter(MaskType);
                    editBinding.ConverterParameter = effectiveMask;
                }
                // Set MaskType BEFORE Mask so the MaskInputBehavior.OnMaskChanged callback
                // reads the right type when constructing the formatter through the factory.
                // Reversing this order means OnMaskChanged sees the default Simple type and
                // builds a SimpleMaskFormatter against (e.g.) "C" or "P2" — the literal collapses
                // the textbox display to the bare format-string character on focus enter.
                factory.SetValue(MaskInputBehavior.MaskTypeProperty, MaskType);
                factory.SetValue(MaskInputBehavior.MaskProperty, effectiveMask);
            }
            factory.SetBinding(TextBox.TextProperty, editBinding);

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
            var cell = FindVisualAncestor<System.Windows.Controls.DataGridCell>(tb);
            if (cell == null) return false;
            var grid = FindVisualAncestor<SearchDataGrid>(cell);
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

        private static T FindVisualAncestor<T>(System.Windows.DependencyObject start) where T : System.Windows.DependencyObject
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
}
