using System;
using System.Globalization;
using WWSearchDataGrid.Modern.Core.Display;
using Xunit;

namespace WWSearchDataGrid.Modern.Core.Tests.Display
{
    /// <summary>
    /// Phase-3 lock-down tests for the TimeSpan engine. Covers format-string translation,
    /// Format/Parse round-trips for positive and negative intervals, sign-toggle keystrokes,
    /// and rejection of variable-precision fractional specifiers.
    /// </summary>
    public class TimeSpanMaskFormatterTests
    {
        private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

        // ---------- Standard-format resolution ----------

        [Fact]
        public void StandardFormat_c_ResolvesToNoDayNoFractionalMask()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            Assert.Equal("00:00:00", f.MaskPattern);
        }

        [Fact]
        public void StandardFormat_g_ResolvesToNoDayNoFractionalMask()
        {
            var f = new TimeSpanMaskFormatter("g", EnUs);
            Assert.Equal("00:00:00", f.MaskPattern);
        }

        [Fact]
        public void StandardFormat_G_ResolvesToDayMask()
        {
            var f = new TimeSpanMaskFormatter("G", EnUs);
            Assert.Equal("00.00:00:00", f.MaskPattern);
        }

        // ---------- Custom format translation ----------

        [Fact]
        public void Custom_HoursMinutesSeconds_TranslatesToSixDigits()
        {
            var f = new TimeSpanMaskFormatter(@"hh\:mm\:ss", EnUs);
            Assert.Equal("00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Custom_DaysHoursMinutesSeconds_TranslatesToEightDigits()
        {
            var f = new TimeSpanMaskFormatter(@"dd\.hh\:mm\:ss", EnUs);
            Assert.Equal("00.00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Custom_FractionalFff_EmitsThreeDigitSlots()
        {
            var f = new TimeSpanMaskFormatter(@"hh\:mm\:ss\.fff", EnUs);
            Assert.Equal("00:00:00.000", f.MaskPattern);
        }

        [Fact]
        public void Custom_SingleLetterSpecifiers_NormalizedToTwoDigits()
        {
            // h → hh, m → mm, s → ss
            var f = new TimeSpanMaskFormatter(@"h\:m\:s", EnUs);
            Assert.Equal("00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Custom_FourDigitDays_EmitsFourSlots()
        {
            var f = new TimeSpanMaskFormatter(@"dddd\.hh\:mm\:ss", EnUs);
            Assert.Equal("0000.00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Custom_QuotedLiteral_PreservedAsLiteral()
        {
            var f = new TimeSpanMaskFormatter(@"hh' hours 'mm' min'", EnUs);
            Assert.Equal("00 hours 00 min", f.MaskPattern);
        }

        // ---------- Format / Parse round-trips ----------

        [Fact]
        public void Format_PositiveTimeSpan_ProducesMaskedDisplay()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            Assert.Equal("12:34:56", f.Format(new TimeSpan(12, 34, 56)));
        }

        [Fact]
        public void Format_NegativeTimeSpan_PrependsMinus()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            Assert.Equal("-12:34:56", f.Format(new TimeSpan(12, 34, 56).Negate()));
        }

        [Fact]
        public void Format_TimeSpanWithDays_UsesGStandardCorrectly()
        {
            var f = new TimeSpanMaskFormatter("G", EnUs);
            var ts = new TimeSpan(5, 3, 0, 0); // 5 days, 3 hours
            Assert.Equal("05.03:00:00", f.Format(ts));
        }

        [Fact]
        public void Format_NullValue_ReturnsEmpty()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            Assert.Equal(string.Empty, f.Format(null));
        }

        [Fact]
        public void Format_FractionalSeconds()
        {
            var f = new TimeSpanMaskFormatter(@"hh\:mm\:ss\.fff", EnUs);
            var ts = new TimeSpan(0, 12, 34, 56, 789);
            Assert.Equal("12:34:56.789", f.Format(ts));
        }

        [Fact]
        public void Parse_PositiveDuration_RoundTripsToSignedInvariant()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            string parsed = f.Parse("12:34:56");
            Assert.Equal("12:34:56", parsed);
        }

        [Fact]
        public void Parse_NegativeDuration_RoundTripsWithMinus()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            string parsed = f.Parse("-12:34:56");
            Assert.StartsWith("-", parsed);
            Assert.Contains("12:34:56", parsed);
        }

        [Fact]
        public void RoundTrip_FormatThenParse_RecoversTimeSpan()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            var original = new TimeSpan(12, 34, 56);
            string display = f.Format(original);
            var recovered = TimeSpan.Parse(f.Parse(display), CultureInfo.InvariantCulture);
            Assert.Equal(original, recovered);
        }

        [Fact]
        public void RoundTrip_NegativeTimeSpan_RecoversSign()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            var original = new TimeSpan(1, 2, 3).Negate();
            string display = f.Format(original);
            var recovered = TimeSpan.Parse(f.Parse(display), CultureInfo.InvariantCulture);
            Assert.Equal(original, recovered);
        }

        [Fact]
        public void RoundTrip_DaysFormat_RecoversFullSpan()
        {
            var f = new TimeSpanMaskFormatter("G", EnUs);
            var original = new TimeSpan(5, 3, 30, 45);
            string display = f.Format(original);
            Assert.Equal("05.03:30:45", display);
            var recovered = TimeSpan.Parse(f.Parse(display), CultureInfo.InvariantCulture);
            Assert.Equal(original, recovered);
        }

        // ---------- Sign-toggle keystrokes ----------

        [Fact]
        public void InsertChar_MinusSign_TogglesNegativeAndPrependsDash()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            f.Format(new TimeSpan(12, 34, 56));
            var (after, _) = f.InsertChar('-', 0);
            Assert.Equal("-12:34:56", after);
        }

        [Fact]
        public void InsertChar_PlusSign_ClearsNegative()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            f.Format(new TimeSpan(12, 34, 56).Negate());
            Assert.Equal("-12:34:56", f.BuildDisplayText());
            var (after, _) = f.InsertChar('+', 0);
            Assert.Equal("12:34:56", after);
        }

        [Fact]
        public void Backspace_OverLeadingMinus_ClearsSign()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            f.Format(new TimeSpan(12, 34, 56).Negate());
            // Display "-12:34:56", caret at 1 (just after the "-"). Backspace clears the sign
            // rather than chewing into the digit slots.
            var (after, caret) = f.DeleteChar(1, forward: false);
            Assert.Equal("12:34:56", after);
            Assert.Equal(0, caret);
        }

        // ---------- Rejection of unsupported specifiers ----------

        [Fact]
        public void Construction_RejectsVariableFractionalF()
        {
            Assert.Throws<NotSupportedException>(
                () => new TimeSpanMaskFormatter(@"hh\:mm\:ss\.FF", EnUs));
        }

        [Fact]
        public void Construction_RejectsEmptyFormat()
        {
            Assert.Throws<NotSupportedException>(
                () => new TimeSpanMaskFormatter("", EnUs));
        }

        [Fact]
        public void Construction_RejectsUnterminatedQuote()
        {
            Assert.Throws<NotSupportedException>(
                () => new TimeSpanMaskFormatter("hh'unclosed", EnUs));
        }

        // ---------- Factory dispatch ----------

        [Fact]
        public void Factory_DispatchesTimeSpanToTimeSpanMaskFormatter()
        {
            var f = MaskFormatterFactory.Create(MaskType.TimeSpan, "c", culture: EnUs);
            Assert.IsType<TimeSpanMaskFormatter>(f);
            Assert.Equal("12:34:56", f.Format(new TimeSpan(12, 34, 56)));
        }

        [Fact]
        public void Factory_EnsureSupported_AcceptsTimeSpan()
        {
            MaskFormatterFactory.EnsureSupported(MaskType.TimeSpan); // no throw
        }

        // ---------- Delegated properties ----------

        [Fact]
        public void IsMaskComplete_TrueWhenFullySpecified()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            f.Format(new TimeSpan(12, 34, 56));
            Assert.True(f.IsMaskComplete);
        }

        [Fact]
        public void DisplayLength_AccountsForLeadingMinus()
        {
            var f = new TimeSpanMaskFormatter("c", EnUs);
            f.Format(new TimeSpan(12, 34, 56));
            int positiveLength = f.DisplayLength;
            f.Format(new TimeSpan(12, 34, 56).Negate());
            Assert.Equal(positiveLength + 1, f.DisplayLength);
        }
    }
}
