using System;
using System.Globalization;
using System.Text;

namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// DateTime mask engine. Accepts standard .NET datetime format strings (<c>d</c>, <c>t</c>,
    /// <c>g</c>, <c>s</c>, <c>u</c>, etc.) or custom patterns (<c>MM/dd/yyyy</c>,
    /// <c>HH:mm:ss</c>, <c>yyyy-MM-dd HH:mm:ss</c>). Translates the pattern into a
    /// <see cref="SimpleMaskFormatter"/>-compatible <c>0</c>-grammar mask and delegates
    /// per-keystroke work to it; <see cref="Format"/>/<see cref="Parse"/> are overridden
    /// to handle <see cref="DateTime"/> conversions.
    /// </summary>
    /// <remarks>
    /// Single-letter date specifiers in the input pattern (<c>M</c>, <c>d</c>, <c>H</c>,
    /// <c>h</c>, <c>m</c>, <c>s</c>, <c>y</c>) are normalized to fixed-width forms (<c>MM</c>,
    /// <c>dd</c>, etc., year normalizing to <c>yyyy</c> when a 4-digit context is reasonable)
    /// so the mask has predictable slot counts. The DateTime is formatted using the normalized
    /// pattern, so display and mask always agree on width.
    /// <para>
    /// Text-form specifiers — <c>MMM</c>/<c>MMMM</c> (month name), <c>ddd</c>/<c>dddd</c>
    /// (day name), <c>tt</c> (AM/PM), <c>K</c>/<c>z</c>/<c>zz</c>/<c>zzz</c> (timezone) — don't
    /// decompose into digit slots and throw <see cref="NotSupportedException"/> at construction.
    /// Use a numeric-only pattern (24-hour <c>HH</c>, no AM/PM) when masked input is required.
    /// </para>
    /// <para>
    /// Standard format codes that resolve to text-heavy patterns (<c>D</c>, <c>F</c>, <c>R</c>,
    /// <c>U</c>, <c>f</c>, <c>M</c>, <c>m</c>, <c>Y</c>, <c>y</c>) likewise fail at construction
    /// after the underlying culture pattern resolves to text — this is intentional, since masking
    /// "Sunday, January 1, 2026" character-by-character would be hostile to the user.
    /// </para>
    /// <para>
    /// Callers that need the normalized pattern string for text-form-aware consumers (segmented
    /// editors that render <c>tt</c> / <c>MMM</c> / <c>ddd</c> as typed sections rather than digit
    /// slots) should use the public static <see cref="ResolvePattern"/> helper, which runs
    /// <c>ResolveStandardFormat</c> + <c>NormalizePattern</c> without invoking the strict mask
    /// translator. The instance constructor remains strict so the <see cref="IMaskFormatter"/>
    /// contract (digit-slot grammar) is preserved.
    /// </para>
    /// </remarks>
    public class DateTimeMaskFormatter : IMaskFormatter
    {
        private readonly string _format;
        private readonly CultureInfo _culture;
        private readonly string _maskPattern;
        private readonly SimpleMaskFormatter _inner;

        public DateTimeMaskFormatter(string format, CultureInfo culture = null)
        {
            _culture = culture ?? CultureInfo.CurrentCulture;
            string resolved = ResolveStandardFormat(format, _culture);
            _format = NormalizePattern(resolved);
            _maskPattern = TranslateToMaskPattern(_format);
            _inner = new SimpleMaskFormatter(_maskPattern);
        }

        /// <summary>
        /// Resolves a user-supplied datetime format string to its normalized form
        /// (<c>ResolveStandardFormat</c> + <c>NormalizePattern</c>) without invoking the strict
        /// mask translator. Returns a pattern with predictable digit-run widths
        /// (<c>M</c>→<c>MM</c>, <c>d</c>→<c>dd</c>, <c>yyy</c>→<c>yyyy</c>, etc.) and text-form
        /// specifiers preserved (<c>MMM</c>, <c>MMMM</c>, <c>ddd</c>, <c>dddd</c>, <c>tt</c>,
        /// <c>K</c>, <c>z</c>/<c>zz</c>/<c>zzz</c>, <c>g</c>/<c>gg</c>). Use this when a consumer
        /// (e.g. a segmented datetime editor) needs to walk the pattern and emit per-token UI for
        /// text-form sections — the instance constructor would throw on those tokens before the
        /// resolved pattern was reachable.
        /// </summary>
        public static string ResolvePattern(string format, CultureInfo culture = null)
        {
            var c = culture ?? CultureInfo.CurrentCulture;
            string resolved = ResolveStandardFormat(format, c);
            return NormalizePattern(resolved);
        }

        /// <summary>The normalized format string the engine uses for DateTime ↔ display conversion.</summary>
        public string ResolvedFormat => _format;

        /// <summary>The translated SimpleMaskFormatter mask string. Useful for diagnostics.</summary>
        public string MaskPattern => _maskPattern;

        public bool IsMaskComplete => _inner.IsMaskComplete;
        public string UnmaskedValue => _inner.UnmaskedValue;
        public int DisplayLength => _inner.DisplayLength;

        public string Format(object rawValue) => FormatCore(rawValue);

        public string Parse(string displayText) => ParseCore(displayText);

        public string FormatEndAligned(string value) => _inner.FormatEndAligned(value);
        public string StripLiterals(string text) => _inner.StripLiterals(text);
        public string BuildDisplayText() => _inner.BuildDisplayText();

        public (string displayText, int newCaret) InsertChar(char c, int caretPosition)
            => _inner.InsertChar(c, caretPosition);

        public (string displayText, int newCaret) DeleteChar(int caretPosition, bool forward)
            => _inner.DeleteChar(caretPosition, forward);

        public void ClearSelection(int selectionStart, int selectionLength)
            => _inner.ClearSelection(selectionStart, selectionLength);

        public (string displayText, int newCaret) Paste(string text, int caretPosition, int selectionLength)
            => _inner.Paste(text, caretPosition, selectionLength);

        public string Finalize() => _inner.Finalize();

        public (int regionIndex, int localOffset) GetRegionAtCaret(int caretPosition)
            => _inner.GetRegionAtCaret(caretPosition);

        public (int start, int length) GetEditableRegionBounds(int regionIndex)
            => _inner.GetEditableRegionBounds(regionIndex);

        public int GetNextEditableRegionStart(int fromRegionIndex)
            => _inner.GetNextEditableRegionStart(fromRegionIndex);

        public int GetPrevEditableRegionStart(int fromRegionIndex)
            => _inner.GetPrevEditableRegionStart(fromRegionIndex);

        public int GetFirstEditableRegionIndex() => _inner.GetFirstEditableRegionIndex();

        // ---- DateTime-specific Format / Parse ----

        private string FormatCore(object rawValue)
        {
            if (rawValue == null)
                return _inner.Format(string.Empty);

            DateTime dt;
            if (rawValue is DateTime d) dt = d;
            else if (rawValue is DateTimeOffset dto) dt = dto.LocalDateTime;
            else if (rawValue is IFormattable formattable)
            {
                // Covers DateOnly / TimeOnly (.NET 6+) without taking a hard reference on those types.
                string formatted = formattable.ToString(_format, _culture);
                return _inner.Format(formatted);
            }
            else if (rawValue is string str)
            {
                if (string.IsNullOrEmpty(str)) return _inner.Format(string.Empty);
                if (DateTime.TryParse(str, _culture, DateTimeStyles.None, out dt))
                {
                    return _inner.Format(dt.ToString(_format, _culture));
                }
                // Pass the string through the inner formatter as-is — user may be editing.
                return _inner.Format(str);
            }
            else
            {
                return _inner.Format(rawValue.ToString());
            }

            return _inner.Format(dt.ToString(_format, _culture));
        }

        private string ParseCore(string displayText)
        {
            if (string.IsNullOrEmpty(displayText)) return string.Empty;

            DateTime dt;
            if (DateTime.TryParseExact(displayText, _format, _culture, DateTimeStyles.None, out dt)
                || DateTime.TryParse(displayText, _culture, DateTimeStyles.None, out dt))
            {
                return dt.ToString("o", CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }

        // ---- format-string handling ----

        private static string ResolveStandardFormat(string format, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("Empty datetime format string is not supported.");
            if (format.Length > 1) return format;

            var dtfi = culture.DateTimeFormat;
            switch (format[0])
            {
                case 'd': return dtfi.ShortDatePattern;
                case 't': return dtfi.ShortTimePattern;
                case 'T': return dtfi.LongTimePattern;
                case 'g': return dtfi.ShortDatePattern + " " + dtfi.ShortTimePattern;
                case 'G': return dtfi.ShortDatePattern + " " + dtfi.LongTimePattern;
                case 's': return "yyyy-MM-ddTHH:mm:ss";
                case 'u': return "yyyy-MM-dd HH:mm:ss";

                // Text-heavy standard codes — let the translator throw on the embedded MMM/dddd/etc.
                case 'D': return dtfi.LongDatePattern;
                case 'F': return dtfi.FullDateTimePattern;
                case 'f': return dtfi.LongDatePattern + " " + dtfi.ShortTimePattern;
                case 'M':
                case 'm': return dtfi.MonthDayPattern;
                case 'Y':
                case 'y': return dtfi.YearMonthPattern;
                case 'R':
                case 'r': return dtfi.RFC1123Pattern;
                case 'U': return dtfi.FullDateTimePattern;

                // Round-trip — has 'K' (timezone offset), translator will reject.
                case 'O':
                case 'o': return "yyyy-MM-ddTHH:mm:ss.fffffffK";

                default:
                    return format;
            }
        }

        /// <summary>
        /// Upgrades single-letter digit specifiers to a fixed width so the mask has predictable
        /// slot counts. <c>M</c> → <c>MM</c>, <c>d</c> → <c>dd</c>, <c>H</c>/<c>h</c> → <c>HH</c>/<c>hh</c>,
        /// <c>m</c>/<c>s</c> → <c>mm</c>/<c>ss</c>, <c>y</c> → <c>yy</c>, <c>yyy</c> → <c>yyyy</c>.
        /// Quoted literals (<c>'text'</c>) and escaped chars (<c>\X</c>) are preserved as-is.
        /// </summary>
        private static string NormalizePattern(string pattern)
        {
            var sb = new StringBuilder(pattern.Length + 4);
            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];

                if (c == '\'')
                {
                    int end = pattern.IndexOf('\'', i + 1);
                    if (end < 0)
                        throw new NotSupportedException("Unterminated quoted literal in datetime format.");
                    sb.Append(pattern, i, end - i + 1);
                    i = end + 1;
                    continue;
                }

                if (c == '\\' && i + 1 < pattern.Length)
                {
                    sb.Append(c).Append(pattern[i + 1]);
                    i += 2;
                    continue;
                }

                if ("MdHhms".IndexOf(c) >= 0)
                {
                    int run = CountRun(pattern, i, c);
                    // For M and d, runs >= 3 are text form (month/day name) — keep as-is so the
                    // translator can reject with a clear error.
                    if ((c == 'M' || c == 'd') && run >= 3)
                        sb.Append(c, run);
                    else if (run == 1)
                        sb.Append(c, 2);
                    else
                        sb.Append(c, run);
                    i += run;
                    continue;
                }

                if (c == 'y')
                {
                    int run = CountRun(pattern, i, c);
                    // y or yy → 2-digit year. yyy or yyyy → 4-digit year. yyyyy+ kept as-is.
                    if (run == 1) sb.Append("yy");
                    else if (run == 3) sb.Append("yyyy");
                    else sb.Append('y', run);
                    i += run;
                    continue;
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Walks the (already-normalized) datetime pattern and emits a
        /// <see cref="SimpleMaskFormatter"/> mask string — <c>0</c> for each digit slot, literal
        /// chars passed through with mask-grammar escapes where needed.
        /// </summary>
        private static string TranslateToMaskPattern(string pattern)
        {
            var sb = new StringBuilder(pattern.Length * 2);
            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];

                if (c == '\'')
                {
                    int end = pattern.IndexOf('\'', i + 1);
                    if (end < 0)
                        throw new NotSupportedException("Unterminated quoted literal in datetime format.");
                    for (int k = i + 1; k < end; k++)
                        AppendLiteral(sb, pattern[k]);
                    i = end + 1;
                    continue;
                }

                if (c == '\\' && i + 1 < pattern.Length)
                {
                    AppendLiteral(sb, pattern[i + 1]);
                    i += 2;
                    continue;
                }

                switch (c)
                {
                    case 'M':
                    case 'd':
                    {
                        int run = CountRun(pattern, i, c);
                        if (run >= 3)
                            throw new NotSupportedException(
                                $"Datetime format contains text-form specifier '{new string(c, run)}' " +
                                $"(month/day name). Masked input requires numeric-only patterns — use " +
                                $"'{new string(c, 2)}' instead.");
                        // Run is 2 after normalization — emit 2 digit slots.
                        sb.Append('0', run);
                        i += run;
                        continue;
                    }
                    case 'y':
                    {
                        int run = CountRun(pattern, i, 'y');
                        sb.Append('0', run);
                        i += run;
                        continue;
                    }
                    case 'H':
                    case 'h':
                    case 'm':
                    case 's':
                    case 'f':
                    case 'F':
                    {
                        int run = CountRun(pattern, i, c);
                        sb.Append('0', run);
                        i += run;
                        continue;
                    }
                    case 't':
                    {
                        int run = CountRun(pattern, i, 't');
                        throw new NotSupportedException(
                            "AM/PM designator ('" + new string('t', run) + "') is not supported in masked datetime input. " +
                            "Use a 24-hour pattern with 'HH' instead.");
                    }
                    case 'K':
                    case 'z':
                    {
                        int run = CountRun(pattern, i, c);
                        throw new NotSupportedException(
                            "Timezone specifier ('" + new string(c, run) + "') is not supported in masked datetime input. " +
                            "Use DateTimeOffset with a fixed-offset pattern when timezone is needed.");
                    }
                    case 'g':
                    {
                        // Era specifier ('g'/'gg') — text form, reject.
                        int run = CountRun(pattern, i, 'g');
                        throw new NotSupportedException(
                            "Era specifier ('" + new string('g', run) + "') is not supported in masked datetime input.");
                    }
                    default:
                        AppendLiteral(sb, c);
                        i++;
                        continue;
                }
            }
            return sb.ToString();
        }

        private static void AppendLiteral(StringBuilder sb, char c)
        {
            // Escape chars that the SimpleMaskFormatter grammar would otherwise interpret.
            if ("09#L?Aa+*\\".IndexOf(c) >= 0)
                sb.Append('\\').Append(c);
            else
                sb.Append(c);
        }

        private static int CountRun(string s, int start, char c)
        {
            int n = 0;
            while (start + n < s.Length && s[start + n] == c) n++;
            return n;
        }
    }
}
