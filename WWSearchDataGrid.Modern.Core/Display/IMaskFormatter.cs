namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Engine contract that <see cref="System.Windows.Controls.TextBox"/>-driven mask behaviors
    /// (notably <c>MaskInputBehavior</c>) and display-side adapters (<c>MaskDisplayProvider</c>,
    /// <c>MaskFormatConverter</c>) consume. Each <see cref="MaskType"/> ships its own
    /// implementation; the factory (<see cref="MaskFormatterFactory"/>) is the single place that
    /// maps a mask type to a concrete engine.
    /// </summary>
    /// <remarks>
    /// The region-navigation members below model the "literal characters + editable slots"
    /// pattern used by <see cref="SimpleMaskFormatter"/> (and date/time/timespan engines that
    /// delegate to it). Engines that don't have a region model (numeric, regex) return sentinel
    /// values so the consuming behavior falls back to default Tab navigation.
    /// </remarks>
    public interface IMaskFormatter
    {
        /// <summary>Formats a raw value through the mask, producing the display string.</summary>
        string Format(object rawValue);

        /// <summary>Parses a display string back to its unmasked raw value.</summary>
        string Parse(string displayText);

        /// <summary>Strips literal mask characters from text, returning only data characters.</summary>
        string StripLiterals(string text);

        /// <summary>Formats a value aligned to the end of the mask (e.g. for EndsWith chip display).</summary>
        string FormatEndAligned(string value);

        /// <summary>Rebuilds the display string from current internal state (no mutation).</summary>
        string BuildDisplayText();

        /// <summary>True when every required slot is filled.</summary>
        bool IsMaskComplete { get; }

        /// <summary>The raw, unmasked data string (no literals or prompts).</summary>
        string UnmaskedValue { get; }

        /// <summary>Total display length of the current mask state.</summary>
        int DisplayLength { get; }

        /// <summary>Inserts a character at <paramref name="caretPosition"/>, respecting mask rules.</summary>
        (string displayText, int newCaret) InsertChar(char c, int caretPosition);

        /// <summary>
        /// Deletes a character relative to <paramref name="caretPosition"/>.
        /// <paramref name="forward"/> is <c>true</c> for Delete (forward) and <c>false</c> for Backspace.
        /// </summary>
        (string displayText, int newCaret) DeleteChar(int caretPosition, bool forward);

        /// <summary>Clears mask content within the given selection range.</summary>
        void ClearSelection(int selectionStart, int selectionLength);

        /// <summary>Pastes <paramref name="text"/> at <paramref name="caretPosition"/>.</summary>
        (string displayText, int newCaret) Paste(string text, int caretPosition, int selectionLength);

        /// <summary>Fills empty required slots with defaults; called on focus loss.</summary>
        string Finalize();

        /// <summary>
        /// Returns the mask region containing <paramref name="caretPosition"/>.
        /// Engines without a region model return <c>(-1, 0)</c>.
        /// </summary>
        (int regionIndex, int localOffset) GetRegionAtCaret(int caretPosition);

        /// <summary>
        /// Returns the (start, length) bounds of an editable region.
        /// Engines without a region model return <c>(0, 0)</c>.
        /// </summary>
        (int start, int length) GetEditableRegionBounds(int regionIndex);

        /// <summary>
        /// Start position of the next editable region after <paramref name="fromRegionIndex"/>,
        /// or <c>-1</c> if none / not applicable.
        /// </summary>
        int GetNextEditableRegionStart(int fromRegionIndex);

        /// <summary>
        /// Start position of the previous editable region before <paramref name="fromRegionIndex"/>,
        /// or <c>-1</c> if none / not applicable.
        /// </summary>
        int GetPrevEditableRegionStart(int fromRegionIndex);

        /// <summary>
        /// Index of the first editable region, or <c>-1</c> if there are none / the engine doesn't
        /// model regions. Consumers use this to seed the focus-enter selection.
        /// </summary>
        int GetFirstEditableRegionIndex();
    }
}
