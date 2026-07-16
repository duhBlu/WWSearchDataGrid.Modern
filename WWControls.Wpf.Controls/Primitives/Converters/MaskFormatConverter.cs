using System;
using System.Globalization;
using System.Windows.Data;
using WWControls.Core.Display;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Value converter that formats raw values through a mask pattern. The mask string is
    /// passed via <c>ConverterParameter</c>; the engine type is set on the instance via
    /// <see cref="MaskType"/> (default <see cref="Core.Display.MaskType.Simple"/>).
    ///
    /// Usage in XAML (Simple mask via shared resource):
    ///   Binding="{Binding PhoneNumber, Converter={StaticResource MaskFormatConverter}, ConverterParameter='(000) 000-0000'}"
    ///
    /// For Numeric (or future) types, construct per-binding with the right MaskType — typically
    /// done by edit settings rather than declared as a static resource.
    /// </summary>
    public class MaskFormatConverter : IValueConverter
    {
        /// <summary>
        /// Which engine the mask string targets. Defaults to
        /// <see cref="Core.Display.MaskType.Simple"/>. Set per-instance when constructing the
        /// converter for numeric / date / etc. bindings.
        /// </summary>
        public MaskType MaskType { get; set; } = MaskType.Simple;

        public MaskFormatConverter() { }

        public MaskFormatConverter(MaskType maskType)
        {
            MaskType = maskType;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter is not string mask || string.IsNullOrEmpty(mask))
                return value;

            // Genuinely-blank string fields → empty display, no formatting needed.
            if (value is string s && string.IsNullOrEmpty(s))
                return string.Empty;

            try
            {
                // DateTime family: text-form specifiers (MMM, MMMM, ddd, dddd, tt, K, zzz, gg)
                // are valid on the display side but rejected by the strict slot-mask engine.
                // Resolve the pattern (singletons normalized to fixed-width, text-form tokens
                // preserved) and render directly via the value's own ToString — no digit-grammar
                // intermediate. Matches the segmented editor, which uses the same ResolvePattern.
                if (MaskType == MaskType.DateTime || MaskType == MaskType.DateOnly || MaskType == MaskType.TimeOnly)
                {
                    string pattern = DateTimeMaskFormatter.ResolvePattern(mask, culture);
                    switch (value)
                    {
                        case DateTime dt: return dt.ToString(pattern, culture);
                        case DateTimeOffset dto: return dto.ToString(pattern, culture);
                        case IFormattable f: return f.ToString(pattern, culture);
                        case string vs when DateTime.TryParse(vs, culture, DateTimeStyles.None, out var dt2):
                            return dt2.ToString(pattern, culture);
                        default: return value.ToString();
                    }
                }

                var formatter = MaskFormatterFactory.Create(MaskType, mask, culture: culture);
                var formatted = formatter.Format(value);
                // A blank-tab-off can land the prompt skeleton ("____-____-____-____") in
                // the source when DataGrid.CommitEdit pushes the editor's text before
                // MaskInputBehavior.OnLostFocus rewrites it to "" — and a row-level revert
                // (Esc on a sibling cell) can then snap that skeleton back in. After
                // formatting, UnmaskedValue tells us whether there's any actual data; if
                // not, render empty rather than the literal mask. The skeleton is an
                // editing affordance, not a display value.
                if (string.IsNullOrEmpty(formatter.UnmaskedValue))
                    return string.Empty;
                return formatted;
            }
            catch
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter is not string mask || string.IsNullOrEmpty(mask))
                return value;

            try
            {
                var formatter = MaskFormatterFactory.Create(MaskType, mask, culture: culture);
                return formatter.Parse(value.ToString());
            }
            catch
            {
                return value;
            }
        }
    }
}
