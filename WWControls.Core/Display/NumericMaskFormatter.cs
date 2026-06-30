using System;
using System.Globalization;
using System.Text;

namespace WWControls.Core.Display
{
    /// <summary>
    /// Numeric mask engine. Accepts standard .NET numeric format strings (C, N, F, P with
    /// optional precision) and renders culture-aware formatted text live as the user types.
    /// No region model — keystroke handling tracks an internal digit buffer that the engine
    /// re-formats on every change.
    /// </summary>
    /// <remarks>
    /// Scope: <c>C</c>, <c>C&lt;n&gt;</c>, <c>N</c>, <c>N&lt;n&gt;</c>, <c>F</c>, <c>F&lt;n&gt;</c>,
    /// <c>P</c>, <c>P&lt;n&gt;</c>. Custom format strings (<c>#,##0.00</c>) and exponential / general
    /// formats are out of scope for this engine.
    /// <para>
    /// Internal state represents the <em>displayed</em> number portion (so for a percent format,
    /// state holds "50.00", not the underlying 0.5 ratio). <see cref="Parse"/> and
    /// <see cref="UnmaskedValue"/> still return the underlying value (divided by 100 for percent)
    /// so binding round-trips behave correctly.
    /// </para>
    /// <para>
    /// Caret position is tracked by counting "raw" characters (digits + decimal separator) in the
    /// rendered display — group separators, currency symbols, percent symbols, and parentheses are
    /// chrome and don't count. This gives stable caret behavior across re-formats.
    /// </para>
    /// </remarks>
    public class NumericMaskFormatter : IMaskFormatter
    {
        private readonly string _format;
        private readonly CultureInfo _culture;
        private readonly NumericFormatSpec _spec;

        // Edit state — represents the displayed number portion (no chrome, no group separators).
        private string _intDigits = "";
        private string _fracDigits = "";
        private bool _hasDecimal = false;
        private bool _isNegative = false;
        private bool _isEmpty = true;

        public NumericMaskFormatter(string format, CultureInfo culture = null)
        {
            _culture = culture ?? CultureInfo.CurrentCulture;
            _format = string.IsNullOrEmpty(format) ? "G" : format;
            _spec = NumericFormatSpec.Parse(_format, _culture);
        }

        public bool IsMaskComplete => !_isEmpty;

        public string UnmaskedValue
        {
            get
            {
                if (_isEmpty) return string.Empty;
                decimal displayValue = ComputeDisplayedValue();
                if (_isNegative) displayValue = -displayValue;
                decimal underlying = _spec.IsPercent ? displayValue / 100m : displayValue;
                return underlying.ToString(CultureInfo.InvariantCulture);
            }
        }

        public int DisplayLength => BuildDisplayText().Length;

        public string Format(object rawValue)
        {
            if (rawValue == null)
            {
                Reset();
                return string.Empty;
            }

            // String inputs are treated as already-displayed form (the user-visible representation
            // — currency symbol / percent / group separators acceptable). Skip the underlying-value
            // scale step. This is what MaskInputBehavior.OnGotFocus passes when the binding's
            // converter has already produced "$1,250.00" / "15.00%" / etc.
            if (rawValue is string strVal)
                return FormatStringInput(strVal);

            decimal underlying;
            if (rawValue is decimal dec) underlying = dec;
            else if (rawValue is double dbl) underlying = (decimal)dbl;
            else if (rawValue is float flt) underlying = (decimal)flt;
            else if (rawValue is IConvertible)
            {
                try { underlying = Convert.ToDecimal(rawValue, _culture); }
                catch
                {
                    Reset();
                    return rawValue.ToString();
                }
            }
            else
            {
                Reset();
                return rawValue.ToString();
            }

            decimal displayValue = _spec.IsPercent ? underlying * 100m : underlying;
            SetValue(displayValue);
            return BuildDisplayText();
        }

        /// <summary>
        /// Format path for string inputs. Strips the culture's percent symbol (NumberStyles.Any
        /// doesn't cover AllowPercent) and parses the remainder as a culture- or invariant-form
        /// decimal. Treats the parsed value as the displayed-number portion — no x100 scaling
        /// for percent — since string input is already what the user sees.
        /// </summary>
        private string FormatStringInput(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                Reset();
                return string.Empty;
            }

            var nfi = _culture.NumberFormat;
            string stripped = _spec.IsPercent ? s.Replace(nfi.PercentSymbol, "") : s;

            decimal parsed;
            if (decimal.TryParse(stripped, NumberStyles.Any, _culture, out parsed)
                || decimal.TryParse(stripped, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                SetValue(parsed);
                return BuildDisplayText();
            }

            Reset();
            return s;
        }

        public string Parse(string displayText)
        {
            if (string.IsNullOrEmpty(displayText))
            {
                Reset();
                return string.Empty;
            }

            // NumberStyles.Any covers currency, parens, signs, thousands, decimal — but NOT percent.
            // Strip the percent symbol manually so the leading number can be parsed.
            var nfi = _culture.NumberFormat;
            string stripped = displayText.Replace(nfi.PercentSymbol, "");

            if (decimal.TryParse(stripped, NumberStyles.Any, _culture, out var displayValue))
            {
                SetValue(displayValue);
                decimal underlying = _spec.IsPercent ? displayValue / 100m : displayValue;
                if (_isNegative) underlying = underlying < 0 ? underlying : -underlying;
                return underlying.ToString(CultureInfo.InvariantCulture);
            }

            Reset();
            return string.Empty;
        }

        public string StripLiterals(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (IsRawChar(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        public string FormatEndAligned(string value) => Format(value);

        public string BuildDisplayText()
        {
            if (_isEmpty) return string.Empty;

            var nfi = _culture.NumberFormat;
            string decSep = GetDecimalSeparator(nfi);

            string intStr = _intDigits.Length == 0 ? "0" : _intDigits;
            if (_spec.UseGroupSeparators)
                intStr = ApplyGroupSeparators(intStr, nfi);

            int requiredFrac = _spec.FractionalDigits;
            string fracStr;
            bool showDecimal;
            if (requiredFrac > 0)
            {
                fracStr = _fracDigits.Length >= requiredFrac
                    ? _fracDigits.Substring(0, requiredFrac)
                    : _fracDigits.PadRight(requiredFrac, '0');
                showDecimal = true;
            }
            else if (_hasDecimal)
            {
                fracStr = _fracDigits;
                showDecimal = true;
            }
            else
            {
                fracStr = string.Empty;
                showDecimal = false;
            }

            string number = showDecimal ? intStr + decSep + fracStr : intStr;
            return ApplyChrome(number, _isNegative, nfi);
        }

        public (string displayText, int newCaret) InsertChar(char c, int caretPosition)
        {
            string display = BuildDisplayText();
            int rawCaret = RawIndexFromDisplay(display, caretPosition);

            // Sign toggle — meaningful only when at least one digit (or pending decimal) exists.
            if (c == '-' || c == '+')
            {
                if (_isEmpty)
                    return (display, caretPosition);
                _isNegative = c == '-' ? !_isNegative : false;
                string nd = BuildDisplayText();
                int newCaret = DisplayPosForRawCount(nd, rawCaret);
                return (nd, newCaret);
            }

            var nfi = _culture.NumberFormat;
            string decSep = GetDecimalSeparator(nfi);

            // Decimal separator
            if (decSep.Length == 1 && c == decSep[0])
            {
                if (_hasDecimal) return (display, caretPosition);
                if (_spec.FractionalDigits == 0 && _spec.Base != 'G')
                    return (display, caretPosition);
                _hasDecimal = true;
                _isEmpty = false;
                string nd = BuildDisplayText();
                int newCaret = DisplayPosForRawCount(nd, _intDigits.Length + 1);
                return (nd, newCaret);
            }

            if (!char.IsDigit(c))
                return (display, caretPosition);

            _isEmpty = false;
            int n = _intDigits.Length;

            if (!_hasDecimal)
            {
                if (rawCaret > n) rawCaret = n;
                _intDigits = _intDigits.Insert(rawCaret, c.ToString());
                CompactLeadingZeros();
                string nd = BuildDisplayText();
                int newCaret = DisplayPosForRawCount(nd, Math.Min(rawCaret + 1, _intDigits.Length));
                return (nd, newCaret);
            }

            // Has decimal: raw layout = [int digits][dec sep][frac digits]
            if (rawCaret <= n)
            {
                _intDigits = _intDigits.Insert(Math.Min(rawCaret, _intDigits.Length), c.ToString());
                CompactLeadingZeros();
                string nd = BuildDisplayText();
                int newCaret = DisplayPosForRawCount(nd, Math.Min(rawCaret + 1, _intDigits.Length));
                return (nd, newCaret);
            }

            int fracOffset = rawCaret - n - 1;
            if (fracOffset < 0) fracOffset = 0;
            if (fracOffset > _fracDigits.Length) fracOffset = _fracDigits.Length;

            if (_spec.FractionalDigits > 0 && _fracDigits.Length >= _spec.FractionalDigits)
                return (display, caretPosition);

            _fracDigits = _fracDigits.Insert(fracOffset, c.ToString());
            string ndFrac = BuildDisplayText();
            int rawTotal = _intDigits.Length + 1 + fracOffset + 1;
            return (ndFrac, DisplayPosForRawCount(ndFrac, rawTotal));
        }

        public (string displayText, int newCaret) DeleteChar(int caretPosition, bool forward)
        {
            string display = BuildDisplayText();
            int rawCaret = RawIndexFromDisplay(display, caretPosition);
            int targetRaw = forward ? rawCaret : rawCaret - 1;
            if (targetRaw < 0) return (display, caretPosition);

            int totalRaw = TotalRawCount();
            if (totalRaw == 0) return (display, caretPosition);

            // For fixed-precision formats the display can include padding "0"s that aren't in
            // stored state (e.g. "5.00" with _fracDigits="0"). Clamp targetRaw to stored range so
            // backspace at the visual end deletes the last stored char, not a padding ghost.
            if (targetRaw >= totalRaw) targetRaw = totalRaw - 1;

            DeleteRawAt(targetRaw);

            if (_intDigits.Length == 0 && _fracDigits.Length == 0 && !_hasDecimal)
            {
                Reset();
                return (string.Empty, 0);
            }

            string nd = BuildDisplayText();
            int newCaret = DisplayPosForRawCount(nd, targetRaw);
            return (nd, newCaret);
        }

        public void ClearSelection(int selectionStart, int selectionLength)
        {
            if (selectionLength <= 0) return;
            string display = BuildDisplayText();
            int rawStart = RawIndexFromDisplay(display, selectionStart);
            int rawEnd = RawIndexFromDisplay(display, selectionStart + selectionLength);

            for (int i = rawEnd - 1; i >= rawStart; i--)
                DeleteRawAt(i);

            if (_intDigits.Length == 0 && _fracDigits.Length == 0 && !_hasDecimal)
                Reset();
        }

        public (string displayText, int newCaret) Paste(string text, int caretPosition, int selectionLength)
        {
            if (string.IsNullOrEmpty(text))
                return (BuildDisplayText(), caretPosition);

            if (selectionLength > 0)
                ClearSelection(caretPosition, selectionLength);

            // Try whole-value parse first: pasting a fully-formatted number replaces state.
            var nfi = _culture.NumberFormat;
            string stripped = text.Replace(nfi.PercentSymbol, "");
            if (decimal.TryParse(stripped, NumberStyles.Any, _culture, out var v))
            {
                SetValue(v);
                string nd = BuildDisplayText();
                return (nd, nd.Length);
            }

            // Fallback: insert each raw char one at a time at the caret.
            string display = BuildDisplayText();
            int caret = caretPosition;
            foreach (char c in text)
            {
                if (!IsRawChar(c) && c != '+' && c != '-') continue;
                var result = InsertChar(c, caret);
                display = result.displayText;
                caret = result.newCaret;
            }
            return (display, caret);
        }

        public string Finalize() => BuildDisplayText();

        // Region model is not applicable — return sentinels so consumers fall back to default
        // navigation (no Tab cycling, no region selection on focus enter).
        public (int regionIndex, int localOffset) GetRegionAtCaret(int caretPosition) => (-1, 0);
        public (int start, int length) GetEditableRegionBounds(int regionIndex) => (0, 0);
        public int GetNextEditableRegionStart(int fromRegionIndex) => -1;
        public int GetPrevEditableRegionStart(int fromRegionIndex) => -1;
        public int GetFirstEditableRegionIndex() => -1;

        // ---- internals ----

        private void Reset()
        {
            _intDigits = string.Empty;
            _fracDigits = string.Empty;
            _hasDecimal = false;
            _isNegative = false;
            _isEmpty = true;
        }

        /// <summary>
        /// Replaces edit state from a decimal in <em>display-value</em> form (already multiplied
        /// by 100 if percent). Caller is responsible for the percent conversion.
        /// </summary>
        private void SetValue(decimal displayValue)
        {
            _isNegative = displayValue < 0m;
            if (_isNegative) displayValue = -displayValue;
            _isEmpty = false;

            int precision = _spec.FractionalDigits > 28 ? 28 : _spec.FractionalDigits;
            string s = displayValue.ToString("F" + precision, CultureInfo.InvariantCulture);
            int dot = s.IndexOf('.');
            if (dot < 0)
            {
                _intDigits = s;
                _hasDecimal = false;
                _fracDigits = string.Empty;
            }
            else
            {
                _intDigits = s.Substring(0, dot);
                _fracDigits = s.Substring(dot + 1);
                _hasDecimal = _fracDigits.Length > 0 || _spec.FractionalDigits > 0;
                if (!_hasDecimal) _fracDigits = string.Empty;
            }

            CompactLeadingZeros();
        }

        private void CompactLeadingZeros()
        {
            // Strip leading zeros (but keep at least one digit if no fractional component present).
            _intDigits = _intDigits.TrimStart('0');
            if (_intDigits.Length == 0)
                _intDigits = string.Empty; // implicit 0, BuildDisplayText handles
        }

        private decimal ComputeDisplayedValue()
        {
            string raw = (_intDigits.Length == 0 ? "0" : _intDigits)
                + (_hasDecimal && _fracDigits.Length > 0 ? "." + _fracDigits : "");
            decimal v;
            decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
            return v;
        }

        private int TotalRawCount()
            => _intDigits.Length + (_hasDecimal ? 1 : 0) + _fracDigits.Length;

        private void DeleteRawAt(int rawIndex)
        {
            int n = _intDigits.Length;
            if (rawIndex < n)
            {
                _intDigits = _intDigits.Remove(rawIndex, 1);
            }
            else if (rawIndex == n && _hasDecimal)
            {
                // Deleting the decimal separator collapses fractional into nothing — discarding
                // typed fractional digits is the intuitive behavior since the format may not
                // have a place to put them.
                _hasDecimal = false;
                _fracDigits = string.Empty;
            }
            else
            {
                int fracOffset = rawIndex - n - 1;
                if (fracOffset >= 0 && fracOffset < _fracDigits.Length)
                    _fracDigits = _fracDigits.Remove(fracOffset, 1);
            }
        }

        private string GetDecimalSeparator(NumberFormatInfo nfi)
        {
            if (_spec.IsCurrency) return nfi.CurrencyDecimalSeparator;
            if (_spec.IsPercent) return nfi.PercentDecimalSeparator;
            return nfi.NumberDecimalSeparator;
        }

        private string ApplyGroupSeparators(string digits, NumberFormatInfo nfi)
        {
            string sep;
            int[] sizes;
            if (_spec.IsCurrency)
            {
                sep = nfi.CurrencyGroupSeparator;
                sizes = nfi.CurrencyGroupSizes;
            }
            else if (_spec.IsPercent)
            {
                sep = nfi.PercentGroupSeparator;
                sizes = nfi.PercentGroupSizes;
            }
            else
            {
                sep = nfi.NumberGroupSeparator;
                sizes = nfi.NumberGroupSizes;
            }

            if (sizes == null || sizes.Length == 0 || sizes[0] <= 0 || string.IsNullOrEmpty(sep))
                return digits;

            int groupSize = sizes[0];
            int len = digits.Length;
            if (len <= groupSize) return digits;

            int firstGroupLen = len % groupSize;
            if (firstGroupLen == 0) firstGroupLen = groupSize;

            var sb = new StringBuilder(len + (len / groupSize) * sep.Length);
            sb.Append(digits, 0, firstGroupLen);
            for (int i = firstGroupLen; i < len; i += groupSize)
            {
                sb.Append(sep);
                sb.Append(digits, i, groupSize);
            }
            return sb.ToString();
        }

        private string ApplyChrome(string number, bool isNegative, NumberFormatInfo nfi)
        {
            if (_spec.IsCurrency)
                return ApplyCurrencyChrome(number, isNegative, nfi);
            if (_spec.IsPercent)
                return ApplyPercentChrome(number, isNegative, nfi);

            if (!isNegative) return number;
            switch (nfi.NumberNegativePattern)
            {
                case 0: return "(" + number + ")";
                case 1: return nfi.NegativeSign + number;
                case 2: return nfi.NegativeSign + " " + number;
                case 3: return number + nfi.NegativeSign;
                case 4: return number + " " + nfi.NegativeSign;
                default: return nfi.NegativeSign + number;
            }
        }

        private static string ApplyCurrencyChrome(string number, bool isNegative, NumberFormatInfo nfi)
        {
            string sym = nfi.CurrencySymbol;
            string neg = nfi.NegativeSign;
            if (!isNegative)
            {
                switch (nfi.CurrencyPositivePattern)
                {
                    case 0: return sym + number;
                    case 1: return number + sym;
                    case 2: return sym + " " + number;
                    case 3: return number + " " + sym;
                    default: return sym + number;
                }
            }
            switch (nfi.CurrencyNegativePattern)
            {
                case 0:  return "(" + sym + number + ")";
                case 1:  return neg + sym + number;
                case 2:  return sym + neg + number;
                case 3:  return sym + number + neg;
                case 4:  return "(" + number + sym + ")";
                case 5:  return neg + number + sym;
                case 6:  return number + neg + sym;
                case 7:  return number + sym + neg;
                case 8:  return neg + number + " " + sym;
                case 9:  return neg + sym + " " + number;
                case 10: return number + " " + sym + neg;
                case 11: return sym + " " + number + neg;
                case 12: return sym + " " + neg + number;
                case 13: return number + neg + " " + sym;
                case 14: return "(" + sym + " " + number + ")";
                case 15: return "(" + number + " " + sym + ")";
                default: return neg + sym + number;
            }
        }

        private static string ApplyPercentChrome(string number, bool isNegative, NumberFormatInfo nfi)
        {
            string sym = nfi.PercentSymbol;
            string neg = nfi.NegativeSign;
            if (!isNegative)
            {
                switch (nfi.PercentPositivePattern)
                {
                    case 0: return number + " " + sym;
                    case 1: return number + sym;
                    case 2: return sym + number;
                    case 3: return sym + " " + number;
                    default: return number + " " + sym;
                }
            }
            switch (nfi.PercentNegativePattern)
            {
                case 0:  return neg + number + " " + sym;
                case 1:  return neg + number + sym;
                case 2:  return neg + sym + number;
                case 3:  return sym + neg + number;
                case 4:  return sym + number + neg;
                case 5:  return number + neg + sym;
                case 6:  return number + sym + neg;
                case 7:  return neg + sym + " " + number;
                case 8:  return number + " " + sym + neg;
                case 9:  return neg + sym + " " + number;
                case 10: return sym + " " + number + neg;
                case 11: return sym + " " + neg + number;
                default: return neg + number + " " + sym;
            }
        }

        private bool IsRawChar(char c)
        {
            if (char.IsDigit(c)) return true;
            string decSep = GetDecimalSeparator(_culture.NumberFormat);
            if (decSep.Length == 1 && c == decSep[0]) return true;
            return false;
        }

        private int RawIndexFromDisplay(string display, int displayPos)
        {
            int raw = 0;
            int limit = displayPos < display.Length ? displayPos : display.Length;
            for (int i = 0; i < limit; i++)
            {
                if (IsRawChar(display[i])) raw++;
            }
            return raw;
        }

        private int DisplayPosForRawCount(string display, int rawTarget)
        {
            int raw = 0;
            for (int i = 0; i < display.Length; i++)
            {
                if (raw == rawTarget) return i;
                if (IsRawChar(display[i])) raw++;
            }
            return display.Length;
        }
    }

    /// <summary>
    /// Parsed shape of a standard .NET numeric format string supported by
    /// <see cref="NumericMaskFormatter"/>.
    /// </summary>
    internal sealed class NumericFormatSpec
    {
        public char Base { get; set; }
        public int FractionalDigits { get; set; }
        public bool UseGroupSeparators { get; set; }
        public bool IsCurrency => Base == 'C';
        public bool IsPercent => Base == 'P';

        public static NumericFormatSpec Parse(string format, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("Empty numeric format string is not supported.");

            char b = char.ToUpperInvariant(format[0]);
            if ("CNFPG".IndexOf(b) < 0)
                throw new NotSupportedException(
                    "Numeric format '" + format + "' is not supported. Use C / N / F / P (with optional precision).");

            int digits;
            var nfi = culture.NumberFormat;
            if (format.Length > 1)
            {
                if (!int.TryParse(format.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out digits))
                    throw new NotSupportedException(
                        "Numeric format '" + format + "' is not supported. Precision must be a non-negative integer.");
                if (digits < 0)
                    throw new NotSupportedException("Numeric format precision must be non-negative.");
            }
            else
            {
                switch (b)
                {
                    case 'C': digits = nfi.CurrencyDecimalDigits; break;
                    case 'N': digits = nfi.NumberDecimalDigits; break;
                    case 'F': digits = nfi.NumberDecimalDigits; break;
                    case 'P': digits = nfi.PercentDecimalDigits; break;
                    default:  digits = nfi.NumberDecimalDigits; break;
                }
            }

            return new NumericFormatSpec
            {
                Base = b,
                FractionalDigits = digits,
                UseGroupSeparators = b == 'C' || b == 'N' || b == 'P'
            };
        }
    }
}
