using System.Globalization;
using WWSearchDataGrid.Modern.Core.Display;
using Xunit;

namespace WWSearchDataGrid.Modern.Core.Tests.Display
{
    /// <summary>
    /// Phase-1 lock-down tests for the numeric engine. Covers the format strings called out in
    /// INPUT-MASKING-IMPLEMENTATION-PLAN.md (C2, N0, N2, F2, P0, P2) plus cross-culture round-trips
    /// for en-US / de-DE / fr-FR.
    /// </summary>
    public class NumericMaskFormatterTests
    {
        private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
        private static readonly CultureInfo DeDe = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        // ---------- Currency (C / C2 / C0) ----------

        [Fact]
        public void Currency_C2_FormatsWithGroupSeparators_EnUs()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal("$1,234.50", f.Format(1234.5m));
        }

        [Fact]
        public void Currency_C_DefaultsToCultureDecimalDigits_EnUs()
        {
            var f = new NumericMaskFormatter("C", EnUs);
            // EnUs.CurrencyDecimalDigits = 2.
            Assert.Equal("$1,234.50", f.Format(1234.5m));
        }

        [Fact]
        public void Currency_C0_NoFractional()
        {
            var f = new NumericMaskFormatter("C0", EnUs);
            Assert.Equal("$1,235", f.Format(1234.5m));
        }

        [Fact]
        public void Currency_C2_Negative_FollowsCultureNegativePattern()
        {
            // Modern .NET en-US CurrencyNegativePattern = 1 → "-$n". (Older runtimes used pattern 0
            // "($n)"; the assertion follows whatever the active runtime reports rather than
            // hard-coding parens, so the test stays portable across .NET versions.)
            var f = new NumericMaskFormatter("C2", EnUs);
            string formatted = f.Format(-1234.5m);
            // Must contain the negative sign and the currency symbol; the exact arrangement
            // varies by runtime globalization data.
            Assert.Contains("-", formatted);
            Assert.Contains("$", formatted);
            Assert.Contains("1,234.50", formatted);
        }

        [Fact]
        public void Currency_C2_Zero()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal("$0.00", f.Format(0m));
        }

        [Fact]
        public void Currency_C2_RoundTrip_Parse_ReturnsUnderlyingDecimal()
        {
            // decimal.ToString(InvariantCulture) preserves the scale chosen at parse time,
            // so "$1,234.50" round-trips to "1234.50" rather than "1234.5". The bound source
            // converts back to the target type (decimal/double) where the trailing zero is
            // semantically the same value.
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal("1234.50", f.Parse("$1,234.50"));
        }

        // ---------- Number (N / N0 / N2) ----------

        [Fact]
        public void Number_N0_FormatsIntegerWithGroupSeparators()
        {
            var f = new NumericMaskFormatter("N0", EnUs);
            Assert.Equal("1,234,568", f.Format(1234567.5m));
        }

        [Fact]
        public void Number_N2_FormatsTwoDecimal()
        {
            var f = new NumericMaskFormatter("N2", EnUs);
            Assert.Equal("1,234.50", f.Format(1234.5m));
        }

        [Fact]
        public void Number_N2_RoundTrip()
        {
            var f = new NumericMaskFormatter("N2", EnUs);
            Assert.Equal("1234.50", f.Parse("1,234.50"));
        }

        // ---------- Fixed-point (F / F2) ----------

        [Fact]
        public void Fixed_F2_NoGroupSeparators()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            Assert.Equal("1234.50", f.Format(1234.5m));
        }

        [Fact]
        public void Fixed_F2_Negative()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            Assert.Equal("-1234.50", f.Format(-1234.5m));
        }

        // ---------- Percent (P / P0 / P2) ----------

        [Fact]
        public void Percent_P2_FormatsAsDisplayedPercent()
        {
            // Underlying 0.5 → "50.00%" (en-US PercentPositivePattern=1 in modern .NET, no space).
            var f = new NumericMaskFormatter("P2", EnUs);
            Assert.Equal("50.00%", f.Format(0.5m));
        }

        [Fact]
        public void Percent_P0_NoFractional()
        {
            var f = new NumericMaskFormatter("P0", EnUs);
            Assert.Equal("50%", f.Format(0.5m));
        }

        [Fact]
        public void Percent_P2_Parse_ReturnsRatioNotPercent()
        {
            var f = new NumericMaskFormatter("P2", EnUs);
            // Display "50.00%" → underlying 0.50 (preserves scale via decimal.ToString).
            Assert.Equal("0.50", f.Parse("50.00%"));
        }

        [Fact]
        public void Percent_P2_UnmaskedValueIsRatio()
        {
            var f = new NumericMaskFormatter("P2", EnUs);
            f.Format(0.5m);
            // 0.5 multiplied by 100 → state holds 50.00 → divided by 100 → 0.50.
            Assert.Equal("0.50", f.UnmaskedValue);
        }

        // ---------- Keystroke sequences (InsertChar / DeleteChar) ----------

        [Fact]
        public void InsertChar_BuildUpInteger_C2()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            // Type "1234" digit-by-digit. After each digit, format pads to .00 and adds group seps.
            var (t1, _) = f.InsertChar('1', 0);
            Assert.Equal("$1.00", t1);

            var (t2, _) = f.InsertChar('2', t1.Length);
            Assert.Equal("$12.00", t2);

            var (t3, _) = f.InsertChar('3', t2.Length);
            Assert.Equal("$123.00", t3);

            var (t4, _) = f.InsertChar('4', t3.Length);
            Assert.Equal("$1,234.00", t4);
        }

        [Fact]
        public void InsertChar_DecimalThenFractional_C2()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            f.InsertChar('1', 0);
            f.InsertChar('2', 2);
            // Currently "$12.00". Now press "." — format already shows ".", caret should land
            // after the existing decimal separator and any further digits replace fractional.
            var (afterDot, _) = f.InsertChar('.', 3);
            Assert.Equal("$12.00", afterDot); // already had decimal capacity
            var (after5, _) = f.InsertChar('5', 4);
            Assert.Equal("$12.50", after5);
        }

        [Fact]
        public void InsertChar_RejectsExtraDigitBeyondPrecision()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            f.Format(1.99m);
            string before = f.BuildDisplayText();
            // Caret at end. Try to type another digit into fractional — capped at 2 frac.
            var (after, _) = f.InsertChar('9', before.Length);
            Assert.Equal(before, after);
        }

        [Fact]
        public void InsertChar_SignToggle_FlipsNegative()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            f.Format(100m);
            var (after, _) = f.InsertChar('-', 0);
            Assert.Equal("-100.00", after);
        }

        [Fact]
        public void InsertChar_PlusSign_ClearsNegative()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            f.Format(-100m);
            var (after, _) = f.InsertChar('+', 0);
            Assert.Equal("100.00", after);
        }

        [Fact]
        public void DeleteChar_Backspace_RemovesLastIntegerDigit()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            f.Format(1234m);
            // Display "$1,234.00", caret at 6 (after "4"). Backspace removes "4".
            var (after, _) = f.DeleteChar(6, forward: false);
            Assert.Equal("$123.00", after);
        }

        [Fact]
        public void DeleteChar_AllDigits_ReturnsEmpty()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            f.Format(5m);
            // Initial "5.00" (4 chars). Backspace at end deletes "0" then "0" then "." then "5".
            string current = f.BuildDisplayText();
            for (int i = 0; i < 10 && current.Length > 0; i++)
            {
                var (next, _) = f.DeleteChar(current.Length, forward: false);
                current = next;
            }
            Assert.Equal(string.Empty, current);
        }

        // ---------- Paste ----------

        [Fact]
        public void Paste_FormattedCurrencyValue_ReplacesState()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            var (after, _) = f.Paste("$2,500.75", 0, 0);
            Assert.Equal("$2,500.75", after);
        }

        [Fact]
        public void Paste_RawDigitsFallback_BuildsValue()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            // Plain "1234" parses as 1234 → display "1234.00".
            var (after, _) = f.Paste("1234", 0, 0);
            Assert.Equal("1234.00", after);
        }

        // ---------- StripLiterals ----------

        [Fact]
        public void StripLiterals_DropsCurrencySymbolAndGroupSeparators()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal("1234.56", f.StripLiterals("$1,234.56"));
        }

        [Fact]
        public void StripLiterals_DropsPercentSign()
        {
            var f = new NumericMaskFormatter("P2", EnUs);
            Assert.Equal("50.00", f.StripLiterals("50.00%"));
        }

        // ---------- Cross-culture ----------

        [Fact]
        public void Number_N2_DeDe_UsesCommaForDecimalAndDotForGroup()
        {
            var f = new NumericMaskFormatter("N2", DeDe);
            Assert.Equal("1.234,50", f.Format(1234.5m));
        }

        [Fact]
        public void Number_N2_DeDe_RoundTrip()
        {
            var f = new NumericMaskFormatter("N2", DeDe);
            Assert.Equal("1234.50", f.Parse("1.234,50"));
        }

        [Fact]
        public void Currency_C2_DeDe_TrailingEuroSymbol()
        {
            var f = new NumericMaskFormatter("C2", DeDe);
            string formatted = f.Format(1234.5m);
            // de-DE puts "€" trailing with non-breaking space — round-trip should still parse.
            Assert.Equal("1234.50", f.Parse(formatted));
        }

        [Fact]
        public void Number_N2_FrFr_RoundTrip()
        {
            var f = new NumericMaskFormatter("N2", FrFr);
            string formatted = f.Format(1234.5m);
            Assert.Equal("1234.50", f.Parse(formatted));
        }

        [Fact]
        public void Currency_C2_FrFr_NegativeRoundTrip()
        {
            var f = new NumericMaskFormatter("C2", FrFr);
            string formatted = f.Format(-1234.5m);
            Assert.Equal("-1234.50", f.Parse(formatted));
        }

        // ---------- Edge cases ----------

        [Fact]
        public void Format_Null_ReturnsEmpty()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal(string.Empty, f.Format(null));
            Assert.False(f.IsMaskComplete);
        }

        [Fact]
        public void Format_EmptyString_ReturnsEmpty()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal(string.Empty, f.Format(""));
        }

        [Fact]
        public void Format_MaxDecimal_DoesNotOverflow()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            string result = f.Format(decimal.MaxValue);
            Assert.NotNull(result);
            Assert.NotEqual(string.Empty, result);
        }

        [Fact]
        public void Format_MinDecimal_DoesNotOverflow()
        {
            var f = new NumericMaskFormatter("F2", EnUs);
            string result = f.Format(decimal.MinValue);
            Assert.NotNull(result);
            Assert.NotEqual(string.Empty, result);
        }

        [Fact]
        public void NoRegions_GetFirstEditableRegionIndex_ReturnsMinusOne()
        {
            var f = new NumericMaskFormatter("C2", EnUs);
            Assert.Equal(-1, f.GetFirstEditableRegionIndex());
            Assert.Equal(-1, f.GetNextEditableRegionStart(0));
            Assert.Equal(-1, f.GetPrevEditableRegionStart(0));
        }

        [Fact]
        public void Factory_ReturnsNumericMaskFormatterForNumericType()
        {
            IMaskFormatter f = MaskFormatterFactory.Create(MaskType.Numeric, "C2", culture: EnUs);
            Assert.IsType<NumericMaskFormatter>(f);
            Assert.Equal("$1,234.50", f.Format(1234.5m));
        }

        [Fact]
        public void Factory_EnsureSupported_AcceptsNumeric()
        {
            MaskFormatterFactory.EnsureSupported(MaskType.Numeric); // no throw
        }
    }
}
