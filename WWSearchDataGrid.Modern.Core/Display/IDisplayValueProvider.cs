namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Provides bidirectional conversion between raw property values and their display representations.
    /// Used to ensure filtering matches what the user sees in the grid, not the underlying raw value.
    /// </summary>
    public interface IDisplayValueProvider
    {
        /// <summary>
        /// Converts a raw property value to its display string representation.
        /// </summary>
        /// <param name="rawValue">The raw value from the data source (e.g., 1.246m, true, DateTime)</param>
        /// <returns>The formatted display string (e.g., "1.25", "Yes", "03/30/2026")</returns>
        string FormatValue(object rawValue);

        /// <summary>
        /// Parses a display string back to a raw value. Used for range-based filters (Between, LessThan, etc.)
        /// where the user enters display-formatted text that needs to be compared numerically.
        /// </summary>
        /// <param name="displayText">The display-formatted text entered by the user</param>
        /// <returns>The parsed raw value, or null if parsing fails</returns>
        object ParseValue(string displayText);

        /// <summary>
        /// Whether this provider supports reverse parsing (display -> raw).
        /// If false, ParseValue will return null.
        /// </summary>
        bool CanParse { get; }

        /// <summary>
        /// When true, text-based filter evaluation compares raw/unmasked values instead of
        /// display values. This is needed for mask-based providers where structural characters
        /// (parentheses, dashes, spaces) are part of the display but not the searchable content.
        /// StringFormat and Converter providers return false (display-to-display comparison).
        /// </summary>
        bool UseRawComparison { get; }
    }
}
