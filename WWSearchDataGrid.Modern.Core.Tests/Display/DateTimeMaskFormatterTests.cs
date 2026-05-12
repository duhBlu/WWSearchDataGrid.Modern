using System;
using System.Globalization;
using WWSearchDataGrid.Modern.Core.Display;
using Xunit;

namespace WWSearchDataGrid.Modern.Core.Tests.Display
{
    /// <summary>
    /// Phase-2 lock-down tests for the datetime engine. Covers format-string → mask translation,
    /// Format/Parse round-trips, cross-culture behavior, and rejection of text-form specifiers.
    /// </summary>
    public class DateTimeMaskFormatterTests
    {
        private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
        private static readonly CultureInfo DeDe = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        // ---------- Format-string → mask translation ----------

        [Fact]
        public void Translation_MMddyyyy_ProducesEightDigitDateMask()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            Assert.Equal("00/00/0000", f.MaskPattern);
        }

        [Fact]
        public void Translation_HHmmss_ProducesSixDigitTimeMask()
        {
            var f = new DateTimeMaskFormatter("HH:mm:ss", EnUs);
            Assert.Equal("00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Translation_FullDateTime_ProducesCombinedMask()
        {
            var f = new DateTimeMaskFormatter("yyyy-MM-dd HH:mm:ss", EnUs);
            Assert.Equal("0000-00-00 00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Translation_SingleLetterFormsNormalized()
        {
            // M → MM, d → dd, y → yy, H → HH, m → mm, s → ss
            var f = new DateTimeMaskFormatter("M/d/y H:m:s", EnUs);
            Assert.Equal("00/00/00 00:00:00", f.MaskPattern);
        }

        [Fact]
        public void Translation_TripleYearNormalizedToFour()
        {
            // yyy → yyyy (the .NET pattern resolver also normalizes this way).
            var f = new DateTimeMaskFormatter("MM/dd/yyy", EnUs);
            Assert.Equal("00/00/0000", f.MaskPattern);
        }

        [Fact]
        public void Translation_QuotedLiteralPreserved()
        {
            var f = new DateTimeMaskFormatter("yyyy'年'MM'月'dd'日'", EnUs);
            // Year/month/day digits with the kanji literals interspersed.
            Assert.Equal("0000年00月00日", f.MaskPattern);
        }

        [Fact]
        public void Translation_FractionalSecondsEmitsDigitSlots()
        {
            var f = new DateTimeMaskFormatter("HH:mm:ss.fff", EnUs);
            Assert.Equal("00:00:00.000", f.MaskPattern);
        }

        // ---------- Standard format codes resolve via culture ----------

        [Fact]
        public void StandardFormat_d_ResolvesToCultureShortDatePattern_EnUs()
        {
            var f = new DateTimeMaskFormatter("d", EnUs);
            // ShortDatePattern in en-US is "M/d/yyyy" which normalizes to "MM/dd/yyyy".
            Assert.Equal("00/00/0000", f.MaskPattern);
        }

        [Fact]
        public void StandardFormat_d_ResolvesToCultureShortDatePattern_DeDe()
        {
            var f = new DateTimeMaskFormatter("d", DeDe);
            // de-DE ShortDatePattern is "dd.MM.yyyy".
            Assert.Equal("00.00.0000", f.MaskPattern);
        }

        [Fact]
        public void StandardFormat_s_ProducesSortableMask()
        {
            var f = new DateTimeMaskFormatter("s", EnUs);
            // Sortable: yyyy-MM-ddTHH:mm:ss
            Assert.Equal("0000-00-00T00:00:00", f.MaskPattern);
        }

        [Fact]
        public void StandardFormat_u_ProducesUniversalSortableMask()
        {
            var f = new DateTimeMaskFormatter("u", EnUs);
            Assert.Equal("0000-00-00 00:00:00", f.MaskPattern);
        }

        // ---------- Format ↔ Parse round-trips ----------

        [Fact]
        public void Format_DateTime_ProducesMaskedDisplay_EnUs()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            Assert.Equal("03/30/2026", f.Format(new DateTime(2026, 3, 30)));
        }

        [Fact]
        public void Format_DateTime_ProducesMaskedDisplay_DeDe()
        {
            var f = new DateTimeMaskFormatter("dd.MM.yyyy", DeDe);
            Assert.Equal("30.03.2026", f.Format(new DateTime(2026, 3, 30)));
        }

        [Fact]
        public void Format_DateTimeOffset_UsesLocalDateTime()
        {
            var f = new DateTimeMaskFormatter("yyyy-MM-dd", EnUs);
            var dto = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.FromHours(-5));
            Assert.Equal("2026-03-30", f.Format(dto));
        }

        [Fact]
        public void Format_NullValue_ReturnsEmpty()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            Assert.Equal(string.Empty, f.Format(null));
        }

        [Fact]
        public void Parse_FormattedString_ReturnsIsoDateTime()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            string parsed = f.Parse("03/30/2026");
            Assert.StartsWith("2026-03-30", parsed);
        }

        [Fact]
        public void RoundTrip_FormatThenParse_RecoversDate()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            var original = new DateTime(2026, 3, 30);
            string display = f.Format(original);
            string parsed = f.Parse(display);
            var recovered = DateTime.Parse(parsed, CultureInfo.InvariantCulture);
            Assert.Equal(original.Date, recovered.Date);
        }

        [Fact]
        public void RoundTrip_TimeFormat_RecoversTime()
        {
            var f = new DateTimeMaskFormatter("HH:mm:ss", EnUs);
            var original = new DateTime(2026, 1, 1, 14, 30, 45);
            string display = f.Format(original);
            Assert.Equal("14:30:45", display);
            string parsed = f.Parse(display);
            var recovered = DateTime.Parse(parsed, CultureInfo.InvariantCulture);
            Assert.Equal(original.TimeOfDay, recovered.TimeOfDay);
        }

        [Fact]
        public void RoundTrip_FullDateTime_RecoversBothPieces()
        {
            var f = new DateTimeMaskFormatter("yyyy-MM-dd HH:mm:ss", EnUs);
            var original = new DateTime(2026, 3, 30, 14, 30, 45);
            string display = f.Format(original);
            Assert.Equal("2026-03-30 14:30:45", display);
            string parsed = f.Parse(display);
            var recovered = DateTime.Parse(parsed, CultureInfo.InvariantCulture);
            Assert.Equal(original, recovered);
        }

        [Fact]
        public void RoundTrip_FrFr_DateOnly()
        {
            // fr-FR ShortDatePattern: dd/MM/yyyy
            var f = new DateTimeMaskFormatter("d", FrFr);
            var original = new DateTime(2026, 3, 30);
            string display = f.Format(original);
            Assert.Equal("30/03/2026", display);
            var recovered = DateTime.Parse(f.Parse(display), CultureInfo.InvariantCulture);
            Assert.Equal(original.Date, recovered.Date);
        }

        // ---------- Rejection of text-form specifiers ----------

        [Fact]
        public void Construction_RejectsMonthName()
        {
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("MMM dd, yyyy", EnUs));
        }

        [Fact]
        public void Construction_RejectsLongMonthName()
        {
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("MMMM dd, yyyy", EnUs));
        }

        [Fact]
        public void Construction_RejectsDayName()
        {
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("ddd MM/dd/yyyy", EnUs));
        }

        [Fact]
        public void Construction_RejectsAmPmDesignator()
        {
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("h:mm tt", EnUs));
        }

        [Fact]
        public void Construction_RejectsTimezoneSpecifier()
        {
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("yyyy-MM-ddTHH:mm:ssK", EnUs));
        }

        [Fact]
        public void Construction_StandardCode_O_RejectsOnEmbeddedTimezone()
        {
            // 'o' resolves to a pattern containing K — translator rejects K.
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("o", EnUs));
        }

        [Fact]
        public void Construction_StandardCode_D_RejectsOnEmbeddedDayName()
        {
            // 'D' resolves to LongDatePattern (text-heavy, contains dddd/MMMM).
            Assert.Throws<NotSupportedException>(
                () => new DateTimeMaskFormatter("D", EnUs));
        }

        // ---------- Delegated keystroke handling ----------

        [Fact]
        public void InsertChar_AcceptsDigit_MasksAdvanceLikeSimple()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            f.Format(string.Empty); // initialize regions
            var (text, _) = f.InsertChar('0', 0);
            Assert.Contains("0", text);
        }

        [Fact]
        public void DeleteChar_DelegatesToInner()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            f.Format(new DateTime(2026, 3, 30));
            // Backspace at caret 2 — after the second '3' in "03". Should clear that slot.
            var (text, _) = f.DeleteChar(2, forward: false);
            Assert.NotEqual("03/30/2026", text);
        }

        [Fact]
        public void IsMaskComplete_TrueWhenAllSlotsFilled()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            f.Format(new DateTime(2026, 3, 30));
            Assert.True(f.IsMaskComplete);
        }

        [Fact]
        public void IsMaskComplete_FalseAfterPartialKeystrokes()
        {
            // Type only a few digits — leaves required slots empty so IsMaskComplete stays false.
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            f.Format(string.Empty);
            f.InsertChar('0', 0);
            f.InsertChar('3', 1);
            Assert.False(f.IsMaskComplete);
        }

        // ---------- No region model is region-friendly (Tab cycles months/days/years) ----------

        [Fact]
        public void HasEditableRegions_TabbableAcrossDateParts()
        {
            var f = new DateTimeMaskFormatter("MM/dd/yyyy", EnUs);
            Assert.NotEqual(-1, f.GetFirstEditableRegionIndex());
            // Three editable regions (MM, dd, yyyy) plus two literal "/" — Tab navigates.
            int next = f.GetNextEditableRegionStart(f.GetFirstEditableRegionIndex());
            Assert.True(next > 0);
        }

        // ---------- Factory dispatch ----------

        [Fact]
        public void Factory_DispatchesDateTimeToDateTimeMaskFormatter()
        {
            var f = MaskFormatterFactory.Create(MaskType.DateTime, "MM/dd/yyyy", culture: EnUs);
            Assert.IsType<DateTimeMaskFormatter>(f);
            Assert.Equal("03/30/2026", f.Format(new DateTime(2026, 3, 30)));
        }

        [Fact]
        public void Factory_DispatchesDateOnlyToSameEngine()
        {
            var f = MaskFormatterFactory.Create(MaskType.DateOnly, "yyyy-MM-dd", culture: EnUs);
            Assert.IsType<DateTimeMaskFormatter>(f);
            Assert.Equal("2026-03-30", f.Format(new DateTime(2026, 3, 30)));
        }

        [Fact]
        public void Factory_DispatchesTimeOnlyToSameEngine()
        {
            var f = MaskFormatterFactory.Create(MaskType.TimeOnly, "HH:mm:ss", culture: EnUs);
            Assert.IsType<DateTimeMaskFormatter>(f);
            Assert.Equal("14:30:45", f.Format(new DateTime(2026, 1, 1, 14, 30, 45)));
        }

        [Fact]
        public void Factory_EnsureSupported_AcceptsDateTimeFamily()
        {
            MaskFormatterFactory.EnsureSupported(MaskType.DateTime);
            MaskFormatterFactory.EnsureSupported(MaskType.DateOnly);
            MaskFormatterFactory.EnsureSupported(MaskType.TimeOnly);
        }

        // ---------- ResolvePattern (public static, non-throwing on text-form specifiers) ----------
        //
        // ResolvePattern runs ResolveStandardFormat + NormalizePattern WITHOUT invoking the
        // strict TranslateToMaskPattern. Consumers like the segmented datetime editor need to walk
        // the normalized pattern and emit per-token UI for text-form sections (MMM, ddd, tt, ...).
        // The instance constructor stays strict (asserted by the *_ThrowsNotSupported tests above).

        [Fact]
        public void ResolvePattern_DigitOnlyPattern_NormalizesSingleLetters()
        {
            // Same normalization as the constructor — M→MM, d→dd, y→yy, etc.
            Assert.Equal("MM/dd/yy", DateTimeMaskFormatter.ResolvePattern("M/d/y", EnUs));
        }

        [Fact]
        public void ResolvePattern_GeneralLongTime_ResolvesToCulturePattern()
        {
            // 'G' is culture-dependent: en-US short date + long time, includes 'tt'.
            string resolved = DateTimeMaskFormatter.ResolvePattern("G", EnUs);
            Assert.Contains("MM", resolved);
            Assert.Contains("yyyy", resolved);
            Assert.Contains("tt", resolved);
        }

        [Fact]
        public void ResolvePattern_GeneralShortTime_IncludesAmPm()
        {
            // 'g' = short date + short time (with 'tt' on en-US).
            string resolved = DateTimeMaskFormatter.ResolvePattern("g", EnUs);
            Assert.Contains("tt", resolved);
        }

        [Fact]
        public void ResolvePattern_RoundTripFormat_PreservesTimezoneSpecifier()
        {
            // 'O' / 'o' — round-trip pattern with 'K' (timezone). NormalizePattern leaves K alone.
            string resolved = DateTimeMaskFormatter.ResolvePattern("O", EnUs);
            Assert.Equal("yyyy-MM-ddTHH:mm:ss.fffffffK", resolved);
        }

        [Fact]
        public void ResolvePattern_CustomMonthAndAmPm_PreservedVerbatim()
        {
            // Text-form MMM and tt round through unchanged — translator would have thrown.
            Assert.Equal(
                "MMM dd, yyyy hh:mm tt",
                DateTimeMaskFormatter.ResolvePattern("MMM d, yyyy h:m tt", EnUs));
        }

        [Fact]
        public void ResolvePattern_CustomDayAndMonthName_PreservedVerbatim()
        {
            Assert.Equal(
                "dddd, MMMM dd, yyyy",
                DateTimeMaskFormatter.ResolvePattern("dddd, MMMM d, yyyy", EnUs));
        }

        [Fact]
        public void ResolvePattern_FixedOffsetCustomPattern_KeepsAllZs()
        {
            Assert.Equal(
                "yyyy-MM-ddTHH:mm:sszzz",
                DateTimeMaskFormatter.ResolvePattern("yyyy-MM-ddTHH:mm:sszzz", EnUs));
        }

        [Fact]
        public void ResolvePattern_TextFormSpecifiers_DoNotThrow()
        {
            // Each of these throws when passed to the constructor — verify ResolvePattern is non-strict.
            DateTimeMaskFormatter.ResolvePattern("G", EnUs);
            DateTimeMaskFormatter.ResolvePattern("g", EnUs);
            DateTimeMaskFormatter.ResolvePattern("O", EnUs);
            DateTimeMaskFormatter.ResolvePattern("MMM d, yyyy h:mm tt", EnUs);
            DateTimeMaskFormatter.ResolvePattern("dddd, MMMM d, yyyy", EnUs);
            DateTimeMaskFormatter.ResolvePattern("yyyy-MM-ddTHH:mm:sszzz", EnUs);
            DateTimeMaskFormatter.ResolvePattern("'Era:' gg yyyy", EnUs);
        }

        [Fact]
        public void ResolvePattern_NullCulture_UsesCurrentCulture()
        {
            // Should not throw with culture omitted.
            string resolved = DateTimeMaskFormatter.ResolvePattern("MM/dd/yyyy");
            Assert.Equal("MM/dd/yyyy", resolved);
        }

        [Fact]
        public void ResolvePattern_QuotedLiteral_PreservedWithEscapes()
        {
            // Quoted literals pass through unchanged (NormalizePattern preserves them as-is).
            string resolved = DateTimeMaskFormatter.ResolvePattern("yyyy 'at' HH:mm", EnUs);
            Assert.Equal("yyyy 'at' HH:mm", resolved);
        }
    }
}
