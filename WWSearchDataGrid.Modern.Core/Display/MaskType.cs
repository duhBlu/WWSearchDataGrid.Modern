namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Categorizes how a mask pattern is interpreted. Mirrors the conceptual model used by
    /// DevExpress editors and similar mask-input frameworks. Currently only
    /// <see cref="Simple"/> is fully implemented end-to-end; the others are reserved for
    /// forward compatibility and throw <see cref="System.NotSupportedException"/> when applied.
    /// </summary>
    /// <remarks>
    /// The active mask type controls which engine validates keystrokes, formats display text,
    /// and parses values back to the bound source. Each type expects a different mask grammar
    /// (the same string means different things under different types).
    /// </remarks>
    public enum MaskType
    {
        /// <summary>
        /// Fixed-format pattern with literal / placeholder grammar — the engine implemented
        /// in <see cref="MaskFormatter"/>. Mask grammar:
        /// <list type="bullet">
        ///   <item><c>0</c> required digit (0–9)</item>
        ///   <item><c>9</c> optional digit (0–9 or space)</item>
        ///   <item><c>#</c> optional digit, space, +, or –</item>
        ///   <item><c>L</c> required letter</item>
        ///   <item><c>?</c> optional letter</item>
        ///   <item><c>A</c> required alphanumeric</item>
        ///   <item><c>a</c> optional alphanumeric</item>
        ///   <item><c>+</c> quantifier — one or more of the preceding mask char</item>
        ///   <item><c>*</c> quantifier — zero or more of the preceding mask char</item>
        ///   <item><c>\</c> escape next character as literal</item>
        ///   <item>any other character — non-editable literal</item>
        /// </list>
        /// Examples: <c>(000) 000-0000</c>, <c>00/00/0000</c>, <c>0+\.00</c>, <c>LLL-000</c>.
        /// </summary>
        Simple = 0,

        /// <summary>
        /// <em>[Not yet implemented]</em> Numeric mask with culture-aware separators and
        /// currency / percent support. Uses standard .NET numeric format strings (e.g. <c>C</c>,
        /// <c>N2</c>, <c>P0</c>). For now, use the column's <c>DisplayStringFormat</c> for
        /// numeric formatting.
        /// </summary>
        Numeric,

        /// <summary>
        /// <em>[Not yet implemented]</em> Date / time mask using standard .NET date format
        /// strings (e.g. <c>MM/dd/yyyy</c>, <c>g</c>, <c>F</c>). For now, configure
        /// <c>DateEditSettings.Mask</c> with a <see cref="Simple"/>-grammar pattern (default
        /// <c>00/00/0000</c>) plus the column's <c>DisplayStringFormat</c> for the read-only view.
        /// </summary>
        DateTime,

        /// <summary>
        /// <em>[Not yet implemented]</em> .NET 6+ <c>DateOnly</c> mask using standard .NET
        /// date format strings.
        /// </summary>
        DateOnly,

        /// <summary>
        /// <em>[Not yet implemented]</em> .NET 6+ <c>TimeOnly</c> mask using standard .NET
        /// time format strings.
        /// </summary>
        TimeOnly,

        /// <summary>
        /// <em>[Not yet implemented]</em> <c>DateTimeOffset</c> mask. Like
        /// <see cref="DateTime"/>, but additionally renders a user time zone.
        /// </summary>
        DateTimeOffset,

        /// <summary>
        /// <em>[Not yet implemented]</em> Time-interval mask supporting day / hour / minute /
        /// second / fractional-second specifiers.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// <em>[Not yet implemented]</em> Extended regular-expression mask. Use for
        /// variable-length input, alternates, or auto-complete with custom character ranges.
        /// Mask expression syntax follows the POSIX ERE specification.
        /// </summary>
        RegEx,

        /// <summary>
        /// <em>[Not yet implemented]</em> Simplified-regex mask — legacy variant. Prefer
        /// <see cref="RegEx"/> for new code.
        /// </summary>
        SimpleRegEx,
    }
}
