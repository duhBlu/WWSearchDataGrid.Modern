using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Standalone mask parsing and formatting engine.
    /// Converts raw values to display strings and vice versa using a mask pattern.
    ///
    /// Mask syntax:
    ///   0  Required digit (0-9)
    ///   9  Optional digit or space
    ///   #  Optional digit, space, +, or -
    ///   L  Required letter (a-z, A-Z)
    ///   ?  Optional letter
    ///   A  Required alphanumeric
    ///   a  Optional alphanumeric
    ///   +  Quantifier: one or more (follows a mask char, e.g., 0+ for variable-length digits)
    ///   *  Quantifier: zero or more (follows a mask char, e.g., 0* for optional digits)
    ///   \  Escape next character as literal (e.g., \. for a literal period)
    ///   Any other character is a non-editable literal
    ///
    /// Examples:
    ///   "0+\.00"           Decimal with 2 places: 1246 -> "1246.00", 1.5 -> "1.50"
    ///   "(000) 000-0000"   Phone: 5551234567 -> "(555) 123-4567"
    ///   "00/00/0000"       Date slots: 03302026 -> "03/30/2026"
    ///
    /// Extracted from WWFormattedTextBox (WPF-CabinetDesigner) with all UI dependencies removed.
    /// </summary>
    public class MaskFormatter
    {
        #region Inner Types

        internal enum RegionType
        {
            Literal,
            Fixed,
            Free
        }

        internal class MaskSlot
        {
            public char MaskChar { get; set; }
            public char? Value { get; set; }

            public bool IsRequired
            {
                get
                {
                    switch (MaskChar)
                    {
                        case '0': case 'L': case 'A': return true;
                        default: return false;
                    }
                }
            }

            public bool Accepts(char c) => AcceptsForMaskChar(MaskChar, c);

            public static bool AcceptsForMaskChar(char maskChar, char c)
            {
                switch (maskChar)
                {
                    case '0': return char.IsDigit(c);
                    case '9': return char.IsDigit(c) || c == ' ';
                    case '#': return char.IsDigit(c) || c == ' ' || c == '+' || c == '-';
                    case 'L': return char.IsLetter(c);
                    case '?': return char.IsLetter(c) || c == ' ';
                    case 'A': return char.IsLetterOrDigit(c);
                    case 'a': return char.IsLetterOrDigit(c) || c == ' ';
                    default: return false;
                }
            }

            public static char DefaultForMaskChar(char maskChar)
            {
                switch (maskChar)
                {
                    case '0': case '9': case '#': return '0';
                    case 'L': case '?': return 'a';
                    case 'A': case 'a': return '0';
                    default: return '0';
                }
            }
        }

        internal class MaskRegion
        {
            public RegionType Type { get; set; }
            public string LiteralText { get; set; }
            public List<MaskSlot> Slots { get; set; }
            public char FreeMaskChar { get; set; }
            public bool FreeIsRequired { get; set; }
            public string FreeContent { get; set; } = "";
            public int StartIndex { get; set; }

            public bool IsEditable => Type != RegionType.Literal;

            public int DisplayLength
            {
                get
                {
                    switch (Type)
                    {
                        case RegionType.Literal: return LiteralText?.Length ?? 0;
                        case RegionType.Fixed: return Slots?.Count ?? 0;
                        case RegionType.Free:
                            if (FreeContent.Length > 0) return FreeContent.Length;
                            return FreeIsRequired ? 1 : 0;
                        default: return 0;
                    }
                }
            }

            public bool AcceptsFreeChar(char c) => MaskSlot.AcceptsForMaskChar(FreeMaskChar, c);
        }

        #endregion

        #region Fields

        private readonly string _mask;
        private List<MaskRegion> _regions;
        private char _promptChar;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new MaskFormatter with the specified mask pattern.
        /// </summary>
        /// <param name="mask">The mask pattern string</param>
        /// <param name="promptChar">Character shown for empty required slots (default: '_')</param>
        public MaskFormatter(string mask, char promptChar = '_')
        {
            _mask = mask ?? throw new ArgumentNullException(nameof(mask));
            _promptChar = promptChar;
            ParseMask();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The raw unmasked value (only actual data characters, no literals or prompts).
        /// </summary>
        public string UnmaskedValue { get; private set; } = "";

        /// <summary>
        /// Whether all required mask slots have been filled.
        /// </summary>
        public bool IsMaskComplete { get; private set; }

        /// <summary>
        /// The prompt character used for empty required slots.
        /// </summary>
        public char PromptChar
        {
            get => _promptChar;
            set => _promptChar = value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Formats a raw value through the mask, producing a display string.
        /// </summary>
        /// <param name="rawValue">The raw value to format (converted to string via ToString())</param>
        /// <returns>The formatted display string with literals and padding applied</returns>
        public string Format(object rawValue)
        {
            if (rawValue == null)
                return string.Empty;

            string text = rawValue.ToString();
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Reset regions and apply the text
            ResetRegions();
            ApplyTextToRegions(text);
            return BuildDisplayText();
        }

        /// <summary>
        /// Parses a display string back to its unmasked raw value (strips literals and prompts).
        /// </summary>
        /// <param name="displayText">The formatted display text</param>
        /// <returns>The unmasked raw string value</returns>
        public string Parse(string displayText)
        {
            if (string.IsNullOrEmpty(displayText))
                return string.Empty;

            ResetRegions();
            ApplyTextToRegions(displayText);
            UpdateMaskState();
            return UnmaskedValue;
        }

        /// <summary>
        /// Strips mask literal characters from user-typed text, returning only data characters.
        /// Handles cases where users type "(573)" by stripping "(", ")", " ", "-" etc.
        /// </summary>
        public string StripLiterals(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Collect all literal strings from the mask
            var literalChars = new HashSet<char>();
            foreach (var r in _regions)
            {
                if (r.Type == RegionType.Literal && !string.IsNullOrEmpty(r.LiteralText))
                {
                    foreach (char c in r.LiteralText)
                        literalChars.Add(c);
                }
            }

            // Also strip prompt chars
            literalChars.Add(_promptChar);

            var result = new StringBuilder();
            foreach (char c in text)
            {
                if (!literalChars.Contains(c))
                    result.Append(c);
            }
            return result.ToString();
        }

        /// <summary>
        /// Formats a value aligned to the END of the mask regions.
        /// Used for EndsWith chip display: "1234" → "(___) ___-1234" for mask "(000) 000-0000".
        /// </summary>
        public string FormatEndAligned(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            ResetRegions();

            // Collect total slot capacity
            int totalSlots = 0;
            var editableRegions = _regions.FindAll(r => r.IsEditable);
            foreach (var r in editableRegions)
            {
                if (r.Type == RegionType.Fixed)
                    totalSlots += r.Slots.Count;
                else if (r.Type == RegionType.Free)
                    totalSlots += value.Length; // free regions take whatever they need
            }

            // Pad the value with leading placeholder to push it to the end
            string padded = value.PadLeft(totalSlots, '\0');

            // Distribute across editable regions
            int charIndex = 0;
            foreach (var region in editableRegions)
            {
                if (region.Type == RegionType.Fixed)
                {
                    for (int j = 0; j < region.Slots.Count; j++)
                    {
                        if (charIndex < padded.Length && padded[charIndex] != '\0'
                            && region.Slots[j].Accepts(padded[charIndex]))
                        {
                            region.Slots[j].Value = padded[charIndex];
                        }
                        else
                        {
                            region.Slots[j].Value = null;
                        }
                        charIndex++;
                    }
                }
                else if (region.Type == RegionType.Free)
                {
                    // For free regions in end-aligned mode, take remaining chars
                    var content = new StringBuilder();
                    while (charIndex < padded.Length)
                    {
                        if (padded[charIndex] != '\0' && region.AcceptsFreeChar(padded[charIndex]))
                            content.Append(padded[charIndex]);
                        charIndex++;
                    }
                    region.FreeContent = content.ToString();
                }
            }

            return BuildDisplayText();
        }

        /// <summary>
        /// Gets the total display length of the current mask state.
        /// </summary>
        public int DisplayLength
        {
            get
            {
                RecalcRegionPositions();
                int len = 0;
                foreach (var r in _regions) len += r.DisplayLength;
                return len;
            }
        }

        /// <summary>
        /// Determines which mask region contains the given caret position.
        /// </summary>
        /// <returns>(regionIndex, localOffset) where localOffset is relative to the region start</returns>
        public (int regionIndex, int localOffset) GetRegionAtCaret(int caretPosition)
        {
            RecalcRegionPositions();
            for (int i = 0; i < _regions.Count; i++)
            {
                var r = _regions[i];
                int end = r.StartIndex + r.DisplayLength;

                if (caretPosition >= r.StartIndex && caretPosition < end)
                    return (i, caretPosition - r.StartIndex);

                if (i == _regions.Count - 1 && caretPosition == end)
                    return (i, caretPosition - r.StartIndex);

                // Zero-width optional free region
                if (r.Type == RegionType.Free && r.DisplayLength == 0 && caretPosition == r.StartIndex)
                    return (i, 0);
            }
            // Fallback: past end
            int last = _regions.Count - 1;
            return (last, _regions[last].DisplayLength);
        }

        /// <summary>
        /// Gets the start position and display length of an editable region for selection.
        /// </summary>
        public (int start, int length) GetEditableRegionBounds(int regionIndex)
        {
            if (regionIndex < 0 || regionIndex >= _regions.Count)
                return (0, 0);
            RecalcRegionPositions();
            var r = _regions[regionIndex];
            return (r.StartIndex, r.DisplayLength);
        }

        /// <summary>
        /// Gets the start position of the next editable region after fromRegionIndex.
        /// Returns -1 if none found.
        /// </summary>
        public int GetNextEditableRegionStart(int fromRegionIndex)
        {
            RecalcRegionPositions();
            for (int i = fromRegionIndex + 1; i < _regions.Count; i++)
            {
                if (_regions[i].IsEditable)
                    return _regions[i].StartIndex;
            }
            return -1;
        }

        /// <summary>
        /// Gets the start position of the previous editable region before fromRegionIndex.
        /// Returns -1 if none found.
        /// </summary>
        public int GetPrevEditableRegionStart(int fromRegionIndex)
        {
            RecalcRegionPositions();
            for (int i = fromRegionIndex - 1; i >= 0; i--)
            {
                if (_regions[i].IsEditable)
                    return _regions[i].StartIndex;
            }
            return -1;
        }

        /// <summary>
        /// Gets the index of the first editable region. Returns -1 if none.
        /// </summary>
        public int GetFirstEditableRegionIndex()
        {
            for (int i = 0; i < _regions.Count; i++)
                if (_regions[i].IsEditable) return i;
            return -1;
        }

        /// <summary>
        /// Inserts a character at the given caret position, respecting mask rules.
        /// Returns the new display text and where the caret should be placed.
        /// </summary>
        public (string displayText, int newCaret) InsertChar(char c, int caretPosition)
        {
            RecalcRegionPositions();
            var (regionIdx, localOffset) = GetRegionAtCaret(caretPosition);
            var region = _regions[regionIdx];

            // Literal region: skip to next editable or match the literal char
            if (region.Type == RegionType.Literal)
            {
                // Check if char matches literal (user typed the literal char)
                if (region.LiteralText.Length > 0 && c == region.LiteralText[0])
                {
                    int next = GetNextEditableRegionStart(regionIdx);
                    string text = BuildDisplayText();
                    return (text, next >= 0 ? next : caretPosition);
                }

                // Check if previous free region can accept this char
                for (int pi = regionIdx - 1; pi >= 0; pi--)
                {
                    if (_regions[pi].Type == RegionType.Free && _regions[pi].AcceptsFreeChar(c))
                    {
                        _regions[pi].FreeContent += c;
                        string text = BuildDisplayText();
                        RecalcRegionPositions();
                        return (text, _regions[pi].StartIndex + _regions[pi].FreeContent.Length);
                    }
                    if (_regions[pi].IsEditable) break;
                }

                // Skip to next editable region and try there
                int nextEdit = GetNextEditableRegionStart(regionIdx);
                if (nextEdit < 0)
                    return (BuildDisplayText(), caretPosition);

                regionIdx = -1;
                for (int i = 0; i < _regions.Count; i++)
                    if (_regions[i].StartIndex == nextEdit) { regionIdx = i; break; }
                if (regionIdx < 0)
                    return (BuildDisplayText(), caretPosition);

                region = _regions[regionIdx];
                localOffset = 0;
            }

            // Free region
            if (region.Type == RegionType.Free)
            {
                if (!region.AcceptsFreeChar(c))
                {
                    // Check if char matches next literal
                    if (regionIdx + 1 < _regions.Count && _regions[regionIdx + 1].Type == RegionType.Literal
                        && _regions[regionIdx + 1].LiteralText.Length > 0
                        && c == _regions[regionIdx + 1].LiteralText[0])
                    {
                        int next = GetNextEditableRegionStart(regionIdx + 1);
                        string txt = BuildDisplayText();
                        return (txt, next >= 0 ? next : caretPosition);
                    }
                    return (BuildDisplayText(), caretPosition); // reject
                }

                if (region.FreeContent.Length == 0)
                    region.FreeContent = c.ToString();
                else
                    region.FreeContent = region.FreeContent.Insert(localOffset, c.ToString());

                string display = BuildDisplayText();
                RecalcRegionPositions();
                return (display, region.StartIndex + localOffset + 1);
            }

            // Fixed region
            if (region.Type == RegionType.Fixed)
            {
                if (localOffset >= region.Slots.Count)
                    return (BuildDisplayText(), caretPosition);

                if (!region.Slots[localOffset].Accepts(c))
                    return (BuildDisplayText(), caretPosition); // reject

                region.Slots[localOffset].Value = c;
                string display = BuildDisplayText();
                RecalcRegionPositions();

                int newCaret = region.StartIndex + localOffset + 1;
                // If last slot in region, advance to next editable region
                if (localOffset + 1 >= region.Slots.Count)
                {
                    int next = GetNextEditableRegionStart(regionIdx);
                    if (next >= 0) newCaret = next;
                }
                return (display, newCaret);
            }

            return (BuildDisplayText(), caretPosition);
        }

        /// <summary>
        /// Deletes a character at the given caret position.
        /// </summary>
        /// <param name="caretPosition">Current caret position</param>
        /// <param name="forward">True for Delete key (forward), false for Backspace (backward)</param>
        /// <returns>New display text and caret position</returns>
        public (string displayText, int newCaret) DeleteChar(int caretPosition, bool forward)
        {
            int targetPos = forward ? caretPosition : caretPosition - 1;
            if (targetPos < 0) return (BuildDisplayText(), caretPosition);

            RecalcRegionPositions();
            var (regionIdx, localOffset) = GetRegionAtCaret(targetPos);
            var region = _regions[regionIdx];

            if (region.Type == RegionType.Literal)
            {
                // Can't delete literals - just move caret
                int newCaret = forward ? region.StartIndex + region.DisplayLength : region.StartIndex;
                return (BuildDisplayText(), newCaret);
            }

            if (region.Type == RegionType.Free)
            {
                if (region.FreeContent.Length == 0 || localOffset >= region.FreeContent.Length)
                    return (BuildDisplayText(), forward ? caretPosition : targetPos);

                region.FreeContent = region.FreeContent.Remove(localOffset, 1);
                string display = BuildDisplayText();
                RecalcRegionPositions();
                return (display, region.StartIndex + localOffset);
            }

            if (region.Type == RegionType.Fixed)
            {
                if (localOffset >= region.Slots.Count)
                    return (BuildDisplayText(), caretPosition);

                region.Slots[localOffset].Value = null;
                string display = BuildDisplayText();
                RecalcRegionPositions();
                return (display, region.StartIndex + localOffset);
            }

            return (BuildDisplayText(), caretPosition);
        }

        /// <summary>
        /// Clears all mask content within the given selection range.
        /// </summary>
        public void ClearSelection(int selectionStart, int selectionLength)
        {
            if (selectionLength <= 0) return;

            RecalcRegionPositions();
            int selEnd = selectionStart + selectionLength;

            foreach (var r in _regions)
            {
                if (!r.IsEditable) continue;

                int rEnd = r.StartIndex + r.DisplayLength;
                if (r.StartIndex >= selEnd || rEnd <= selectionStart) continue; // no overlap

                int from = Math.Max(0, selectionStart - r.StartIndex);
                int to = Math.Min(r.DisplayLength, selEnd - r.StartIndex);

                if (r.Type == RegionType.Free && r.FreeContent.Length > 0)
                {
                    int delFrom = Math.Min(from, r.FreeContent.Length);
                    int delTo = Math.Min(to, r.FreeContent.Length);
                    if (delTo > delFrom)
                        r.FreeContent = r.FreeContent.Remove(delFrom, delTo - delFrom);
                }
                else if (r.Type == RegionType.Fixed)
                {
                    for (int s = from; s < to && s < r.Slots.Count; s++)
                        r.Slots[s].Value = null;
                }
            }
        }

        /// <summary>
        /// Handles pasting text at the given caret position.
        /// </summary>
        public (string displayText, int newCaret) Paste(string text, int caretPosition, int selectionLength)
        {
            if (string.IsNullOrEmpty(text))
                return (BuildDisplayText(), caretPosition);

            if (selectionLength > 0)
                ClearSelection(caretPosition, selectionLength);

            // Strip prompt chars from pasted text
            text = text.Replace(_promptChar.ToString(), "");

            RecalcRegionPositions();
            var (startRegionIdx, localOffset) = GetRegionAtCaret(caretPosition);

            var parts = SplitTextByLiterals(text);
            int partIdx = 0;
            int lastCaret = caretPosition;

            for (int i = startRegionIdx; i < _regions.Count && partIdx < parts.Count; i++)
            {
                var region = _regions[i];
                if (!region.IsEditable) continue;

                string part = parts[partIdx++];

                if (region.Type == RegionType.Free)
                {
                    var cleaned = new StringBuilder();
                    foreach (char ch in part)
                        if (region.AcceptsFreeChar(ch)) cleaned.Append(ch);

                    if (i == startRegionIdx && localOffset > 0 && region.FreeContent.Length > 0)
                    {
                        region.FreeContent = region.FreeContent.Insert(localOffset, cleaned.ToString());
                        RecalcRegionPositions();
                        lastCaret = region.StartIndex + localOffset + cleaned.Length;
                    }
                    else
                    {
                        region.FreeContent = cleaned.ToString();
                        RecalcRegionPositions();
                        lastCaret = region.StartIndex + cleaned.Length;
                    }
                }
                else if (region.Type == RegionType.Fixed)
                {
                    int startSlot = (i == startRegionIdx) ? localOffset : 0;
                    int ci = 0;
                    for (int s = startSlot; s < region.Slots.Count && ci < part.Length; ci++)
                    {
                        if (region.Slots[s].Accepts(part[ci]))
                        {
                            region.Slots[s].Value = part[ci];
                            RecalcRegionPositions();
                            lastCaret = region.StartIndex + s + 1;
                            s++;
                        }
                    }
                }
            }

            string display = BuildDisplayText();
            return (display, Math.Min(lastCaret, display.Length));
        }

        /// <summary>
        /// Finalizes the mask by filling empty required slots with defaults.
        /// Call on focus loss.
        /// </summary>
        public string Finalize()
        {
            foreach (var r in _regions)
            {
                if (r.Type == RegionType.Free)
                {
                    if (r.FreeIsRequired && r.FreeContent.Length == 0)
                        r.FreeContent = MaskSlot.DefaultForMaskChar(r.FreeMaskChar).ToString();
                }
                else if (r.Type == RegionType.Fixed)
                {
                    foreach (var slot in r.Slots)
                    {
                        if (!slot.Value.HasValue)
                            slot.Value = slot.IsRequired ? MaskSlot.DefaultForMaskChar(slot.MaskChar) : ' ';
                    }
                }
            }
            return BuildDisplayText();
        }

        #endregion

        #region Mask Parsing

        private void ParseMask()
        {
            _regions = new List<MaskRegion>();

            if (string.IsNullOrEmpty(_mask))
                return;

            var pendingSlots = new List<MaskSlot>();

            for (int i = 0; i < _mask.Length; i++)
            {
                char c = _mask[i];

                if (c == '\\' && i + 1 < _mask.Length)
                {
                    FlushFixedSlots(pendingSlots);
                    _regions.Add(new MaskRegion { Type = RegionType.Literal, LiteralText = _mask[++i].ToString() });
                }
                else if ("09#L?Aa".IndexOf(c) >= 0)
                {
                    if (i + 1 < _mask.Length && (_mask[i + 1] == '+' || _mask[i + 1] == '*'))
                    {
                        FlushFixedSlots(pendingSlots);
                        char quantifier = _mask[++i];
                        _regions.Add(new MaskRegion
                        {
                            Type = RegionType.Free,
                            FreeMaskChar = c,
                            FreeIsRequired = (quantifier == '+')
                        });
                    }
                    else
                    {
                        pendingSlots.Add(new MaskSlot { MaskChar = c });
                    }
                }
                else
                {
                    FlushFixedSlots(pendingSlots);
                    var lit = new StringBuilder();
                    lit.Append(c);
                    while (i + 1 < _mask.Length)
                    {
                        char next = _mask[i + 1];
                        if (next == '\\' || "09#L?Aa".IndexOf(next) >= 0) break;
                        if (next == '+' || next == '*') break;
                        lit.Append(next);
                        i++;
                    }
                    _regions.Add(new MaskRegion { Type = RegionType.Literal, LiteralText = lit.ToString() });
                }
            }
            FlushFixedSlots(pendingSlots);
        }

        private void FlushFixedSlots(List<MaskSlot> slots)
        {
            if (slots.Count > 0)
            {
                _regions.Add(new MaskRegion { Type = RegionType.Fixed, Slots = new List<MaskSlot>(slots) });
                slots.Clear();
            }
        }

        #endregion

        #region Display Text Building

        public string BuildDisplayText()
        {
            if (_regions == null || _regions.Count == 0)
                return string.Empty;

            RecalcRegionPositions();

            var sb = new StringBuilder();
            foreach (var r in _regions)
            {
                switch (r.Type)
                {
                    case RegionType.Literal:
                        sb.Append(r.LiteralText);
                        break;
                    case RegionType.Fixed:
                        foreach (var slot in r.Slots)
                            sb.Append(slot.Value ?? _promptChar);
                        break;
                    case RegionType.Free:
                        if (r.FreeContent.Length > 0)
                            sb.Append(r.FreeContent);
                        else if (r.FreeIsRequired)
                            sb.Append(_promptChar);
                        break;
                }
            }

            UpdateMaskState();
            return sb.ToString();
        }

        private void RecalcRegionPositions()
        {
            int pos = 0;
            foreach (var r in _regions)
            {
                r.StartIndex = pos;
                pos += r.DisplayLength;
            }
        }

        #endregion

        #region Text to Regions

        private void ApplyTextToRegions(string text)
        {
            var editableRegions = _regions.Where(r => r.IsEditable).ToList();
            bool hasActualContent = !string.IsNullOrEmpty(text) && text != _promptChar.ToString();

            // Check if the incoming text contains the expected literal delimiters
            bool hasAllLiterals = true;
            if (hasActualContent)
            {
                var literals = _regions.Where(r => r.Type == RegionType.Literal).Select(r => r.LiteralText).ToList();
                string check = text;
                foreach (var lit in literals)
                {
                    int idx = check.IndexOf(lit, StringComparison.Ordinal);
                    if (idx >= 0)
                        check = check.Substring(idx + lit.Length);
                    else
                    { hasAllLiterals = false; break; }
                }
            }

            if (hasActualContent && !hasAllLiterals)
            {
                // Raw value without literal delimiters (e.g., "2005551234" for mask "(000) 000-0000").
                // Distribute valid characters sequentially across all editable regions.
                ApplyRawTextToRegions(text, editableRegions);
            }
            else
            {
                // Text contains literals (e.g., "(200) 555-1234") or is already formatted.
                // Split by literal delimiters and assign each part to the corresponding region.
                var parts = SplitTextByLiterals(text);
                ApplyPartsToRegions(parts, editableRegions);
            }
        }

        /// <summary>
        /// Distributes raw characters (no literal delimiters) sequentially across editable regions.
        /// Example: "2005551234" with mask "(000) 000-0000" -> regions get "200", "555", "1234".
        /// </summary>
        private void ApplyRawTextToRegions(string text, List<MaskRegion> editableRegions)
        {
            // Strip prompt chars that aren't valid data
            var chars = new StringBuilder();
            foreach (char ch in text)
            {
                if (ch != _promptChar || editableRegions.Any(r =>
                    (r.Type == RegionType.Free && r.AcceptsFreeChar(_promptChar)) ||
                    (r.Type == RegionType.Fixed && r.Slots.Count > 0 && r.Slots[0].Accepts(_promptChar))))
                {
                    chars.Append(ch);
                }
            }

            int charIndex = 0;
            string rawChars = chars.ToString();

            foreach (var region in editableRegions)
            {
                if (charIndex >= rawChars.Length) break;

                if (region.Type == RegionType.Free)
                {
                    // Free regions consume all remaining valid characters (up to the next region's needs)
                    // For a raw distribution, take characters until we've filled what's needed for remaining fixed regions
                    int charsNeededForRemainingFixed = editableRegions
                        .SkipWhile(r => r != region).Skip(1)
                        .Where(r => r.Type == RegionType.Fixed)
                        .Sum(r => r.Slots.Count);

                    int available = rawChars.Length - charIndex;
                    int toTake = Math.Max(1, available - charsNeededForRemainingFixed);

                    var content = new StringBuilder();
                    for (int i = 0; i < toTake && charIndex < rawChars.Length; i++)
                    {
                        if (region.AcceptsFreeChar(rawChars[charIndex]))
                            content.Append(rawChars[charIndex]);
                        charIndex++;
                    }
                    region.FreeContent = content.ToString();
                }
                else if (region.Type == RegionType.Fixed)
                {
                    for (int j = 0; j < region.Slots.Count; j++)
                    {
                        if (charIndex < rawChars.Length && region.Slots[j].Accepts(rawChars[charIndex]))
                        {
                            region.Slots[j].Value = rawChars[charIndex];
                            charIndex++;
                        }
                        else
                        {
                            region.Slots[j].Value = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Assigns pre-split parts (split by literal delimiters) to editable regions.
        /// Parts are aligned structurally: each part maps to the editable region that
        /// follows the corresponding literal in the mask, not by simple index.
        /// </summary>
        private void ApplyPartsToRegions(List<string> parts, List<MaskRegion> editableRegions)
        {
            // Build a structural mapping: for each editable region, determine which part index
            // it should receive based on how many literals precede it in the region list.
            // Parts[0] = text before first literal, parts[1] = text after first literal, etc.
            // Editable regions between literal[N-1] and literal[N] should get parts[N].
            int partIndex = 0;
            int editableIndex = 0;

            for (int r = 0; r < _regions.Count && editableIndex < editableRegions.Count; r++)
            {
                if (_regions[r].Type == RegionType.Literal)
                {
                    // A literal was encountered - advance the part index
                    partIndex++;
                    continue;
                }

                if (_regions[r] == editableRegions[editableIndex])
                {
                    string part = partIndex < parts.Count ? parts[partIndex] : "";
                    var region = editableRegions[editableIndex];

                    if (region.Type == RegionType.Free)
                    {
                        if (!region.AcceptsFreeChar(_promptChar))
                            part = part.Replace(_promptChar.ToString(), "");

                        var cleaned = new StringBuilder();
                        foreach (char ch in part)
                        {
                            if (region.AcceptsFreeChar(ch))
                                cleaned.Append(ch);
                            else if (ch == '-' && cleaned.Length == 0)
                                cleaned.Append(ch);
                        }
                        region.FreeContent = cleaned.ToString();
                    }
                    else if (region.Type == RegionType.Fixed)
                    {
                        bool slotsAcceptPrompt = region.Slots.Count > 0 && region.Slots[0].Accepts(_promptChar);
                        if (!slotsAcceptPrompt)
                            part = part.Replace(_promptChar.ToString(), "");

                        for (int j = 0; j < region.Slots.Count; j++)
                        {
                            if (j < part.Length && region.Slots[j].Accepts(part[j]))
                                region.Slots[j].Value = part[j];
                            else
                                region.Slots[j].Value = null;
                        }
                    }

                    editableIndex++;
                }
            }
        }

        private List<string> SplitTextByLiterals(string text)
        {
            var literals = _regions
                .Where(r => r.Type == RegionType.Literal)
                .Select(r => r.LiteralText)
                .ToList();

            var parts = new List<string>();
            string remaining = text ?? string.Empty;

            foreach (var lit in literals)
            {
                int idx = remaining.IndexOf(lit, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    parts.Add(remaining.Substring(0, idx));
                    remaining = remaining.Substring(idx + lit.Length);
                }
                else
                {
                    break;
                }
            }

            parts.Add(remaining);
            return parts;
        }

        #endregion

        #region State

        private void UpdateMaskState()
        {
            var raw = new StringBuilder();
            bool complete = true;

            foreach (var r in _regions)
            {
                if (r.Type == RegionType.Free)
                {
                    raw.Append(r.FreeContent);
                    if (r.FreeIsRequired && r.FreeContent.Length == 0)
                        complete = false;
                }
                else if (r.Type == RegionType.Fixed)
                {
                    foreach (var slot in r.Slots)
                    {
                        if (slot.Value.HasValue)
                            raw.Append(slot.Value.Value);
                        else if (slot.IsRequired)
                            complete = false;
                    }
                }
            }

            UnmaskedValue = raw.ToString();
            IsMaskComplete = complete;
        }

        private void ResetRegions()
        {
            foreach (var r in _regions)
            {
                if (r.Type == RegionType.Free)
                {
                    r.FreeContent = "";
                }
                else if (r.Type == RegionType.Fixed)
                {
                    foreach (var slot in r.Slots)
                        slot.Value = null;
                }
            }
        }

        #endregion
    }
}
