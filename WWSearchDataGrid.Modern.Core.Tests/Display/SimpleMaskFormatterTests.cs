using WWSearchDataGrid.Modern.Core.Display;
using Xunit;

namespace WWSearchDataGrid.Modern.Core.Tests.Display
{
    /// <summary>
    /// Locks down current <see cref="SimpleMaskFormatter"/> behavior for every example pattern
    /// listed in INPUT-MASKING-IMPLEMENTATION-PLAN.md. The Phase-0 safety net for the polymorphic
    /// formatter refactor — every later phase must keep these green.
    /// </summary>
    public class SimpleMaskFormatterTests
    {
        // ---------- Phone: (000) 000-0000 ----------

        [Fact]
        public void Phone_Format_FromRawDigits()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            Assert.Equal("(555) 123-4567", f.Format("5551234567"));
        }

        [Fact]
        public void Phone_Parse_StripsLiterals()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            Assert.Equal("5551234567", f.Parse("(555) 123-4567"));
        }

        [Fact]
        public void Phone_RoundTrip_FormatThenParse()
        {
            var f1 = new SimpleMaskFormatter("(000) 000-0000");
            var formatted = f1.Format("5551234567");
            var f2 = new SimpleMaskFormatter("(000) 000-0000");
            Assert.Equal("5551234567", f2.Parse(formatted));
        }

        [Fact]
        public void Phone_InsertChar_DigitsAdvancePastLiterals()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format(""); // initialize regions
            // Type "5" at start — should land in first slot, advance through "("
            var (text, caret) = f.InsertChar('5', 0);
            Assert.Contains("5", text);
        }

        [Fact]
        public void Phone_DeleteChar_ClearsSlot()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format("5551234567");
            var (text, _) = f.DeleteChar(2, forward: true); // delete the '5' at index 1 inside (5
            Assert.NotEqual("(555) 123-4567", text);
        }

        [Fact]
        public void Phone_Paste_FillsFirstEditableRegion()
        {
            // Paste with bare digits and no literal delimiters fills only the first editable
            // region (per current SplitTextByLiterals behavior — see SimpleMaskFormatter.Paste).
            // Smarter raw-distribution lives on the Format path, not the Paste path.
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format("");
            var (text, _) = f.Paste("555", 1, 0);
            Assert.Equal("(555) ___-____", text);
        }

        [Fact]
        public void Phone_Finalize_FillsRequiredSlotsWithDefaults()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format("");
            var finalized = f.Finalize();
            Assert.Equal(14, finalized.Length); // structural length preserved
        }

        [Fact]
        public void Phone_IsMaskComplete_Reflects_FullValue()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format("5551234567");
            Assert.True(f.IsMaskComplete);
        }

        [Fact]
        public void Phone_IsMaskComplete_FalseForPartial()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format("555");
            Assert.False(f.IsMaskComplete);
        }

        // ---------- Date: 00/00/0000 ----------

        [Fact]
        public void Date_Format_FromRawDigits()
        {
            var f = new SimpleMaskFormatter("00/00/0000");
            Assert.Equal("03/30/2026", f.Format("03302026"));
        }

        [Fact]
        public void Date_Parse_StripsSlashes()
        {
            var f = new SimpleMaskFormatter("00/00/0000");
            Assert.Equal("03302026", f.Parse("03/30/2026"));
        }

        [Fact]
        public void Date_RoundTrip()
        {
            var f1 = new SimpleMaskFormatter("00/00/0000");
            var s = f1.Format("12252025");
            var f2 = new SimpleMaskFormatter("00/00/0000");
            Assert.Equal("12252025", f2.Parse(s));
        }

        // ---------- Decimal with quantifier: 0+\.00 ----------

        [Fact]
        public void Decimal_Format_VariableIntegerPart()
        {
            var f = new SimpleMaskFormatter(@"0+\.00");
            Assert.Equal("1246.00", f.Format("124600"));
        }

        [Fact]
        public void Decimal_Format_PreservesDecimalPart()
        {
            var f = new SimpleMaskFormatter(@"0+\.00");
            Assert.Equal("1.50", f.Format("150"));
        }

        [Fact]
        public void Decimal_Parse_StripsDecimalPoint()
        {
            var f = new SimpleMaskFormatter(@"0+\.00");
            Assert.Equal("124600", f.Parse("1246.00"));
        }

        // ---------- License plate: LLL-000 ----------

        [Fact]
        public void License_Format_AcceptsLettersAndDigits()
        {
            var f = new SimpleMaskFormatter("LLL-000");
            Assert.Equal("ABC-123", f.Format("ABC123"));
        }

        [Fact]
        public void License_Parse_StripsHyphen()
        {
            var f = new SimpleMaskFormatter("LLL-000");
            Assert.Equal("ABC123", f.Parse("ABC-123"));
        }

        [Fact]
        public void License_InsertChar_RejectsDigitInLetterSlot()
        {
            var f = new SimpleMaskFormatter("LLL-000");
            f.Format("");
            var before = f.BuildDisplayText();
            var (text, _) = f.InsertChar('1', 0); // '1' is not a letter → reject
            Assert.Equal(before, text);
        }

        // ---------- ZIP+4: 00000-9999 ----------

        [Fact]
        public void ZipPlus4_Format_FromRawDigits()
        {
            var f = new SimpleMaskFormatter("00000-9999");
            Assert.Equal("12345-6789", f.Format("123456789"));
        }

        [Fact]
        public void ZipPlus4_Parse()
        {
            var f = new SimpleMaskFormatter("00000-9999");
            Assert.Equal("123456789", f.Parse("12345-6789"));
        }

        // ---------- Card: 0000-0000-0000-0000 ----------

        [Fact]
        public void Card_Format_FromRawDigits()
        {
            var f = new SimpleMaskFormatter("0000-0000-0000-0000");
            Assert.Equal("4111-1111-1111-1111", f.Format("4111111111111111"));
        }

        [Fact]
        public void Card_Parse_StripsHyphens()
        {
            var f = new SimpleMaskFormatter("0000-0000-0000-0000");
            Assert.Equal("4111111111111111", f.Parse("4111-1111-1111-1111"));
        }

        [Fact]
        public void Card_RoundTrip()
        {
            var f1 = new SimpleMaskFormatter("0000-0000-0000-0000");
            var s = f1.Format("4111111111111111");
            var f2 = new SimpleMaskFormatter("0000-0000-0000-0000");
            Assert.Equal("4111111111111111", f2.Parse(s));
        }

        [Fact]
        public void Card_Paste_FullValueWithHyphens()
        {
            // Card mask starts with an editable region (no leading literal), so the empty
            // leading part doesn't appear and pasting the full formatted value works cleanly.
            var f = new SimpleMaskFormatter("0000-0000-0000-0000");
            f.Format("");
            var (text, _) = f.Paste("4111-1111-1111-1111", 0, 0);
            Assert.Equal("4111-1111-1111-1111", text);
        }

        // ---------- StripLiterals (cross-pattern) ----------

        [Fact]
        public void StripLiterals_RemovesPhonePunctuation()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            Assert.Equal("5551234567", f.StripLiterals("(555) 123-4567"));
        }

        [Fact]
        public void StripLiterals_RemovesPromptChar()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            Assert.Equal("555", f.StripLiterals("(555) ___-____"));
        }

        // ---------- ClearSelection ----------

        [Fact]
        public void ClearSelection_ClearsSpannedSlots()
        {
            var f = new SimpleMaskFormatter("(000) 000-0000");
            f.Format("5551234567");
            f.ClearSelection(1, 3); // clear "555" inside (___)
            var rebuilt = f.BuildDisplayText();
            Assert.DoesNotContain("555", rebuilt.Substring(0, 5));
        }

        // ---------- IMaskFormatter polymorphism sanity ----------

        [Fact]
        public void Factory_ReturnsIMaskFormatterForSimple()
        {
            IMaskFormatter f = MaskFormatterFactory.Create(MaskType.Simple, "(000) 000-0000");
            Assert.IsType<SimpleMaskFormatter>(f);
            Assert.Equal("(555) 123-4567", f.Format("5551234567"));
        }

        [Theory]
        [InlineData(MaskType.DateTimeOffset)]
        [InlineData(MaskType.RegEx)]
        [InlineData(MaskType.SimpleRegEx)]
        public void Factory_UnimplementedTypesThrow(MaskType type)
        {
            Assert.Throws<System.NotSupportedException>(
                () => MaskFormatterFactory.Create(type, "anything"));
        }

        [Fact]
        public void Factory_EnsureSupported_PassesForSimple()
        {
            MaskFormatterFactory.EnsureSupported(MaskType.Simple); // no throw
        }

        [Fact]
        public void Factory_EnsureSupported_ThrowsForUnimplemented()
        {
            // DateTimeOffset / RegEx engines ship in Phases 4-5.
            Assert.Throws<System.NotSupportedException>(
                () => MaskFormatterFactory.EnsureSupported(MaskType.DateTimeOffset));
        }
    }
}
