using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WWControls.Core.Display;
using WWControls.Wpf.Converters;

namespace WWControls.Wpf.Editors.Settings
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

        public override DataTemplate CreateDisplayTemplate(IEditorColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, EditorThemeKeys.DisplayTextBlock);
            ApplyTextAlignment(factory, column);

            var binding = column.CreateFieldBinding();
            binding.Mode = BindingMode.OneWay;

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

        public override DataTemplate CreateEditTemplate(IEditorColumn column)
        {
            var factory = new FrameworkElementFactory(typeof(WWTextEdit));

            // Chrome (border, background, padding, focus accent) is owned once by WWBaseEdit's
            // style. ShowBorder tracks the inherited host-context flag, so the editor is bordered
            // in the edit form and flat in a grid cell / filter row — the control stays
            // grid-agnostic and the adapter wires the signal.
            factory.SetBinding(WWBaseEdit.ShowBorderProperty, new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                Path = new PropertyPath(EditorChrome.ShowEditorBorderProperty),
                Mode = BindingMode.OneWay,
            });

            factory.SetValue(WWTextEdit.TextAlignmentProperty, column.TextAlignment);

            // Mask resolution order: explicit Mask > column.DisplayMask > column.DisplayStringFormat
            // (auto-adopted with Numeric type when the format is C/N/F/P). The third hop lets a
            // single DisplayStringFormat declaration cover both the read-only display (via
            // Binding.StringFormat) and the edit-time mask. WWTextEdit owns the mask wiring on its
            // inner TextBox; the adapter just hands it the resolved pattern/type.
            var (effectiveMask, effectiveMaskType) = ResolveEffectiveMask(column);
            var editBinding = CreateValueBinding(column);
            if (!string.IsNullOrEmpty(effectiveMask))
            {
                MaskFormatterFactory.EnsureSupported(effectiveMaskType);
                // Numeric masks render chrome (currency symbol, group separators, percent sign)
                // the default string→decimal parser can't handle. Route the value binding through
                // MaskFormatConverter so ConvertBack strips chrome and returns the underlying value
                // before WPF converts to the target property type.
                if (effectiveMaskType == Core.Display.MaskType.Numeric)
                {
                    editBinding.Converter = new MaskFormatConverter(effectiveMaskType);
                    editBinding.ConverterParameter = effectiveMask;
                }
                factory.SetValue(WWTextEdit.MaskTypeProperty, effectiveMaskType);
                factory.SetValue(WWTextEdit.MaskProperty, effectiveMask);
            }
            factory.SetBinding(WWBaseEdit.ValueProperty, editBinding);
            SuppressValidationErrorAdorner(factory);

            // Grid-cell interaction — focus-on-edit, mouse-click caret, and arrow-key cell exit —
            // is layered on by the grid-side host (EditorHostBehavior), not by the control.
            factory.SetValue(EditorHostBehavior.HostInCellProperty, true);

            return new DataTemplate { VisualTree = factory };
        }

        /// <summary>
        /// Filter-row editor: the same <see cref="WWTextEdit"/> the cell uses, bound to the host's
        /// <see cref="IColumnFilterHost.SearchText"/> and updating on every keystroke so the filter
        /// pipeline's debounce fires. Flat — the inherited border flag resolves false in the filter
        /// row — and without cell-host wiring, since the filter row drives its own navigation.
        /// </summary>
        public override System.Windows.UIElement CreateFilterEditor(IFilterEditorHost host)
        {
            var editor = new WWTextEdit();
            BindingOperations.SetBinding(editor, WWBaseEdit.ShowBorderProperty, new Binding
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                Path = new PropertyPath(EditorChrome.ShowEditorBorderProperty),
                Mode = BindingMode.OneWay,
            });
            BindingOperations.SetBinding(editor, WWBaseEdit.ValueProperty, new Binding(nameof(IFilterEditorHost.SearchText))
            {
                Source = host,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
            return editor;
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
        private (string mask, MaskType maskType) ResolveEffectiveMask(IEditorColumn column)
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
