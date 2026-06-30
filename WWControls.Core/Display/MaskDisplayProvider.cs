using System;
using System.Globalization;

namespace WWControls.Core.Display
{
    /// <summary>
    /// Display value provider that uses a mask pattern to format raw values. Supports every
    /// engine implemented by <see cref="MaskFormatterFactory"/>:
    /// <list type="bullet">
    ///   <item><see cref="Display.MaskType.Simple"/> — slot grammar (<c>0</c>/<c>9</c>/<c>L</c>/…)
    ///   wrapping <see cref="SimpleMaskFormatter"/>. Comparison is unmasked-raw (see
    ///   <see cref="UseRawComparison"/>) and <see cref="StripLiterals"/> /
    ///   <see cref="FormatEndAligned"/> delegate to the inner formatter.</item>
    ///   <item><see cref="Display.MaskType.Numeric"/> / <see cref="Display.MaskType.TimeSpan"/> —
    ///   factory-built engines, raw-character comparison.</item>
    ///   <item><see cref="Display.MaskType.DateTime"/> / <see cref="Display.MaskType.DateOnly"/> /
    ///   <see cref="Display.MaskType.TimeOnly"/> — text-form patterns (<c>dddd</c>, <c>MMMM</c>,
    ///   <c>tt</c>, etc.) that the strict slot translator rejects. Format directly via
    ///   <see cref="DateTimeMaskFormatter.ResolvePattern"/> + <c>ToString</c>, mirroring
    ///   <c>MaskFormatConverter</c>'s display path. Comparison is display-string-based
    ///   (<see cref="UseRawComparison"/> is <c>false</c>) so chip text and filter evaluation
    ///   match what the user reads in the cell.</item>
    /// </list>
    /// </summary>
    public class MaskDisplayProvider : IDisplayValueProvider
    {
        private readonly string _mask;
        private readonly MaskType _maskType;
        private readonly char _promptChar;
        private readonly IMaskFormatter _slotFormatter;

        /// <summary>
        /// Convenience constructor: Simple-mask provider. Kept for back-compat with callers
        /// that don't specify a mask type.
        /// </summary>
        /// <param name="mask">The mask pattern (e.g., "(000) 000-0000").</param>
        /// <param name="promptChar">Character for empty required slots (default: '_').</param>
        public MaskDisplayProvider(string mask, char promptChar = '_')
            : this(mask, MaskType.Simple, promptChar)
        {
        }

        /// <summary>
        /// Creates a <see cref="MaskDisplayProvider"/> for the supplied mask + engine.
        /// </summary>
        /// <param name="mask">The mask pattern. Grammar varies by <paramref name="maskType"/>.</param>
        /// <param name="maskType">Which engine the mask string targets.</param>
        /// <param name="promptChar">Character for empty required slots (Simple grammar).</param>
        public MaskDisplayProvider(string mask, MaskType maskType, char promptChar = '_')
        {
            _mask = mask ?? throw new ArgumentNullException(nameof(mask));
            _maskType = maskType;
            _promptChar = promptChar;

            // Slot-grammar engines are constructed eagerly. DateTime-family masks may carry
            // text-form specifiers that the strict slot translator rejects at construction
            // (DateTimeMaskFormatter rejects MMMM / dddd / tt / K / z); for those we format
            // through DateTimeMaskFormatter.ResolvePattern + DateTime.ToString in FormatValue,
            // matching the MaskFormatConverter display path.
            if (maskType == MaskType.Simple)
                _slotFormatter = new SimpleMaskFormatter(mask, promptChar);
            else if (maskType == MaskType.Numeric || maskType == MaskType.TimeSpan)
                _slotFormatter = MaskFormatterFactory.Create(maskType, mask);
        }

        /// <summary>The mask pattern this provider uses.</summary>
        public string Mask => _mask;

        /// <summary>Which engine the mask targets.</summary>
        public MaskType MaskType => _maskType;

        /// <summary>
        /// Formats a raw value through the mask pattern. DateTime-family masks resolve the
        /// pattern (so text-form specifiers like <c>MMMM</c> / <c>dddd</c> work) and format
        /// directly via the value's own <c>ToString(pattern, culture)</c>; slot-grammar
        /// engines delegate to <see cref="IMaskFormatter.Format"/>.
        /// </summary>
        public string FormatValue(object rawValue)
        {
            if (rawValue == null) return string.Empty;

            if (IsDateTimeFamily(_maskType))
                return FormatDateTime(rawValue);

            if (_slotFormatter != null)
                return _slotFormatter.Format(rawValue);

            // Defensive fallback for unsupported mask types.
            return rawValue.ToString();
        }

        /// <summary>
        /// Parses a masked display string back to its raw value. For DateTime-family masks,
        /// uses culture-aware <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>;
        /// slot-grammar engines delegate to <see cref="IMaskFormatter.Parse"/>.
        /// </summary>
        public object ParseValue(string displayText)
        {
            if (string.IsNullOrWhiteSpace(displayText)) return null;

            if (IsDateTimeFamily(_maskType))
            {
                if (DateTime.TryParse(displayText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
                    return dt;
                return null;
            }

            if (_slotFormatter != null)
                return _slotFormatter.Parse(displayText);

            return displayText;
        }

        public bool CanParse => true;

        /// <summary>
        /// Slot-grammar engines (Simple, Numeric, TimeSpan) compare against the raw character
        /// sequence — structural characters in the mask aren't part of the searchable content,
        /// so the filter pipeline strips them and compares the unmasked value. DateTime-family
        /// engines instead compare against the formatted display string so chip text and filter
        /// evaluation match what the user reads in the cell (e.g., typing "Friday, June 06, 2025"
        /// matches a row formatted with the <c>dddd, MMMM d, yyyy</c> mask).
        /// </summary>
        public bool UseRawComparison => !IsDateTimeFamily(_maskType);

        /// <summary>
        /// Strips mask literal characters from user input, leaving only data characters.
        /// Slot-grammar only: DateTime-family masks return the input unchanged (literals are
        /// part of the user's reading order — "June" is not noise to strip).
        /// </summary>
        public string StripLiterals(string text)
        {
            if (text == null) return string.Empty;
            return _slotFormatter != null ? _slotFormatter.StripLiterals(text) : text;
        }

        /// <summary>
        /// Formats a value aligned to the end of the mask (EndsWith chip support).
        /// Slot-grammar only; DateTime-family masks return the input unchanged.
        /// </summary>
        public string FormatEndAligned(string value)
        {
            if (value == null) return string.Empty;
            return _slotFormatter != null ? _slotFormatter.FormatEndAligned(value) : value;
        }

        private static bool IsDateTimeFamily(MaskType type)
            => type == MaskType.DateTime || type == MaskType.DateOnly || type == MaskType.TimeOnly;

        private string FormatDateTime(object rawValue)
        {
            var culture = CultureInfo.CurrentCulture;
            string pattern;
            try
            {
                pattern = DateTimeMaskFormatter.ResolvePattern(_mask, culture);
            }
            catch
            {
                // Unresolvable pattern — fall back to the value's default ToString so the
                // filter pipeline doesn't crash on a misconfigured column.
                return rawValue.ToString();
            }

            switch (rawValue)
            {
                case DateTime dt: return dt.ToString(pattern, culture);
                case DateTimeOffset dto: return dto.ToString(pattern, culture);
                case IFormattable f: return f.ToString(pattern, culture);
                case string s when DateTime.TryParse(s, culture, DateTimeStyles.None, out var dt2):
                    return dt2.ToString(pattern, culture);
                default: return rawValue.ToString();
            }
        }
    }
}
