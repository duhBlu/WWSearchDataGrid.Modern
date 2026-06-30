using System;
using System.Globalization;
using System.Text;

namespace WWControls.Core.Display
{
    /// <summary>
    /// TimeSpan mask engine. Accepts the standard format codes <c>c</c> / <c>g</c> / <c>G</c>
    /// or a custom TimeSpan format string (<c>dd\.hh\:mm\:ss</c>, <c>hh\:mm\:ss</c>,
    /// <c>hh\:mm\:ss\.fff</c>) and translates the digit specifiers into a
    /// <see cref="SimpleMaskFormatter"/>-compatible <c>0</c>-grammar mask.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TimeSpan custom format strings require <c>\</c>-escaping for separator literals
    /// (<c>:</c>, <c>.</c>, <c>/</c>) — that's a TimeSpan-format quirk, preserved here. The
    /// translator also accepts quoted literals (<c>'text'</c>).
    /// </para>
    /// <para>
    /// Standard codes resolve to fixed canonical patterns suitable for masked input:
    /// <list type="bullet">
    ///   <item><c>c</c> → <c>hh\:mm\:ss</c> (no days, no fractional — most common duration entry)</item>
    ///   <item><c>g</c> → <c>hh\:mm\:ss</c> (same as <c>c</c> for masking purposes)</item>
    ///   <item><c>G</c> → <c>dd\.hh\:mm\:ss</c> (always shows days)</item>
    /// </list>
    /// Variable-precision fractional specifiers (<c>F</c> / <c>FF</c> / etc.) are rejected — they
    /// don't decompose into fixed digit slots. Use <c>fff</c> for a fixed three-digit fractional.
    /// </para>
    /// <para>
    /// Sign handling: negative TimeSpans render with a leading <c>-</c>. The user can toggle the
    /// sign by typing <c>-</c> (set negative) or <c>+</c> (clear negative). The mask itself only
    /// covers the digit slots; caret positions are translated across the optional sign prefix.
    /// </para>
    /// </remarks>
    public class TimeSpanMaskFormatter : IMaskFormatter
    {
        private readonly string _format;
        private readonly CultureInfo _culture;
        private readonly string _maskPattern;
        private readonly SimpleMaskFormatter _inner;
        private bool _isNegative;

        public TimeSpanMaskFormatter(string format, CultureInfo culture = null)
        {
            _culture = culture ?? CultureInfo.CurrentCulture;
            _format = ResolveStandardFormat(format);
            _maskPattern = TranslateToMaskPattern(_format);
            _inner = new SimpleMaskFormatter(_maskPattern);
        }

        /// <summary>The (possibly normalized) format string used for TimeSpan ↔ display conversion.</summary>
        public string ResolvedFormat => _format;

        /// <summary>The translated SimpleMaskFormatter mask pattern.</summary>
        public string MaskPattern => _maskPattern;

        public bool IsMaskComplete => _inner.IsMaskComplete;
        public string UnmaskedValue => (_isNegative ? "-" : string.Empty) + _inner.UnmaskedValue;
        public int DisplayLength => (_isNegative ? 1 : 0) + _inner.DisplayLength;

        public string Format(object rawValue)
        {
            _isNegative = false;

            if (rawValue == null)
                return _inner.Format(string.Empty);

            TimeSpan ts;
            if (rawValue is TimeSpan t) ts = t;
            else if (rawValue is string s)
            {
                if (string.IsNullOrEmpty(s)) return _inner.Format(string.Empty);
                if (!TimeSpan.TryParseExact(s, _format, _culture, out ts)
                    && !TimeSpan.TryParse(s, _culture, out ts))
                {
                    // Pass the string through inner — caller may be editing partial input.
                    return _inner.Format(s);
                }
            }
            else if (rawValue is IFormattable formattable)
            {
                string formatted = formattable.ToString(_format, _culture);
                if (formatted.StartsWith("-", StringComparison.Ordinal))
                {
                    _isNegative = true;
                    formatted = formatted.Substring(1);
                }
                return Decorate(_inner.Format(formatted));
            }
            else
            {
                return _inner.Format(rawValue.ToString());
            }

            _isNegative = ts < TimeSpan.Zero;
            TimeSpan abs = _isNegative ? ts.Negate() : ts;
            return Decorate(_inner.Format(abs.ToString(_format, _culture)));
        }

        public string Parse(string displayText)
        {
            if (string.IsNullOrEmpty(displayText)) return string.Empty;

            bool negative = displayText.StartsWith("-", StringComparison.Ordinal);
            string body = negative ? displayText.Substring(1) : displayText;

            if (TimeSpan.TryParseExact(body, _format, _culture, out var ts)
                || TimeSpan.TryParse(body, _culture, out ts))
            {
                if (negative) ts = ts.Negate();
                return ts.ToString("c", CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }

        public string StripLiterals(string text) => _inner.StripLiterals(text);
        public string FormatEndAligned(string value) => _inner.FormatEndAligned(value);
        public string BuildDisplayText() => Decorate(_inner.BuildDisplayText());

        public (string displayText, int newCaret) InsertChar(char c, int caretPosition)
        {
            // Sign toggle — meaningful even on empty mask; user can pre-set negative before typing.
            if (c == '-')
            {
                bool wasNegative = _isNegative;
                _isNegative = true;
                string nd = BuildDisplayText();
                int newCaret = wasNegative ? caretPosition : caretPosition + 1;
                return (nd, Math.Min(newCaret, nd.Length));
            }
            if (c == '+')
            {
                bool wasNegative = _isNegative;
                _isNegative = false;
                string nd = BuildDisplayText();
                int newCaret = wasNegative ? Math.Max(0, caretPosition - 1) : caretPosition;
                return (nd, newCaret);
            }

            int innerCaret = ToInner(caretPosition);
            var (innerText, innerNewCaret) = _inner.InsertChar(c, innerCaret);
            return (Decorate(innerText), ToDisplay(innerNewCaret));
        }

        public (string displayText, int newCaret) DeleteChar(int caretPosition, bool forward)
        {
            // Backspace at position 1 with leading "-" should clear the sign and not delete a digit.
            if (_isNegative && !forward && caretPosition == 1)
            {
                _isNegative = false;
                return (BuildDisplayText(), 0);
            }
            // Forward delete at position 0 with leading "-" likewise clears the sign.
            if (_isNegative && forward && caretPosition == 0)
            {
                _isNegative = false;
                return (BuildDisplayText(), 0);
            }

            int innerCaret = ToInner(caretPosition);
            var (innerText, innerNewCaret) = _inner.DeleteChar(innerCaret, forward);
            return (Decorate(innerText), ToDisplay(innerNewCaret));
        }

        public void ClearSelection(int selectionStart, int selectionLength)
        {
            int innerStart = ToInner(selectionStart);
            int innerEnd = ToInner(selectionStart + selectionLength);
            _inner.ClearSelection(innerStart, Math.Max(0, innerEnd - innerStart));
        }

        public (string displayText, int newCaret) Paste(string text, int caretPosition, int selectionLength)
        {
            if (string.IsNullOrEmpty(text))
                return (BuildDisplayText(), caretPosition);

            // If pasted text starts with a sign, consume it as a sign toggle then paste the rest.
            string body = text;
            if (body.StartsWith("-", StringComparison.Ordinal))
            {
                _isNegative = true;
                body = body.Substring(1);
            }
            else if (body.StartsWith("+", StringComparison.Ordinal))
            {
                _isNegative = false;
                body = body.Substring(1);
            }

            int innerCaret = ToInner(caretPosition);
            var (innerText, innerNewCaret) = _inner.Paste(body, innerCaret, selectionLength);
            return (Decorate(innerText), ToDisplay(innerNewCaret));
        }

        public string Finalize() => Decorate(_inner.Finalize());

        public (int regionIndex, int localOffset) GetRegionAtCaret(int caretPosition)
            => _inner.GetRegionAtCaret(ToInner(caretPosition));

        public (int start, int length) GetEditableRegionBounds(int regionIndex)
        {
            var (start, length) = _inner.GetEditableRegionBounds(regionIndex);
            return (ToDisplay(start), length);
        }

        public int GetNextEditableRegionStart(int fromRegionIndex)
        {
            int s = _inner.GetNextEditableRegionStart(fromRegionIndex);
            return s < 0 ? s : ToDisplay(s);
        }

        public int GetPrevEditableRegionStart(int fromRegionIndex)
        {
            int s = _inner.GetPrevEditableRegionStart(fromRegionIndex);
            return s < 0 ? s : ToDisplay(s);
        }

        public int GetFirstEditableRegionIndex() => _inner.GetFirstEditableRegionIndex();

        // ---- internals ----

        private string Decorate(string innerText)
            => _isNegative ? "-" + innerText : innerText;

        private int ToInner(int displayCaret)
            => _isNegative ? Math.Max(0, displayCaret - 1) : displayCaret;

        private int ToDisplay(int innerCaret)
            => _isNegative ? innerCaret + 1 : innerCaret;

        private static string ResolveStandardFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("Empty TimeSpan format string is not supported.");
            if (format.Length > 1) return format;

            switch (format[0])
            {
                case 'c': return @"hh\:mm\:ss";
                case 'g': return @"hh\:mm\:ss";
                case 'G': return @"dd\.hh\:mm\:ss";
                default:  return format;
            }
        }

        /// <summary>
        /// Walks a TimeSpan custom format string and emits a SimpleMaskFormatter mask. TimeSpan
        /// formats use <c>\</c>-escapes for separator literals (<c>\:</c>, <c>\.</c>, <c>\/</c>);
        /// quoted literals (<c>'text'</c>) work too. Single-letter digit specifiers
        /// (<c>d</c>/<c>h</c>/<c>m</c>/<c>s</c>) are normalized to two-letter forms so the mask
        /// has predictable slot counts.
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
                        throw new NotSupportedException("Unterminated quoted literal in TimeSpan format.");
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
                    case 'd':
                    case 'h':
                    case 'm':
                    case 's':
                    {
                        int run = CountRun(pattern, i, c);
                        // Normalize single-letter form to two-letter for predictable slot count.
                        int slots = run == 1 ? 2 : run;
                        sb.Append('0', slots);
                        i += run;
                        continue;
                    }
                    case 'f':
                    {
                        int run = CountRun(pattern, i, 'f');
                        // Fixed fractional — emit one digit slot per 'f'.
                        sb.Append('0', run);
                        i += run;
                        continue;
                    }
                    case 'F':
                    {
                        int run = CountRun(pattern, i, 'F');
                        throw new NotSupportedException(
                            "Variable-precision fractional specifier ('" + new string('F', run) +
                            "') is not supported in masked TimeSpan input. Use lowercase 'f' " +
                            "(fixed precision, e.g. 'fff') for a predictable digit-slot mask.");
                    }
                    default:
                        // In TimeSpan custom format, separator literals are normally backslash-
                        // escaped — but tolerate a stray ':'/'.' for forgiving authoring.
                        AppendLiteral(sb, c);
                        i++;
                        continue;
                }
            }
            return sb.ToString();
        }

        private static void AppendLiteral(StringBuilder sb, char c)
        {
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
