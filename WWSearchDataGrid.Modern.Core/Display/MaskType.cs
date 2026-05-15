namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Categorizes how a mask pattern is interpreted. Mirrors the conceptual model used by
    /// DevExpress editors and similar mask-input frameworks. Implemented engines:
    /// <see cref="Simple"/>, <see cref="Numeric"/>, <see cref="DateTime"/> /
    /// <see cref="DateOnly"/> / <see cref="TimeOnly"/> (single engine), and
    /// <see cref="TimeSpan"/>. The remaining types are reserved for forward compatibility and
    /// throw <see cref="System.NotSupportedException"/> when applied.
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
        /// Numeric mask with culture-aware separators and currency / percent support — the
        /// engine implemented in <c>NumericMaskFormatter</c>. Accepts standard .NET numeric
        /// format strings: <c>C</c>, <c>C&lt;n&gt;</c>, <c>N</c>, <c>N&lt;n&gt;</c>,
        /// <c>F</c>, <c>F&lt;n&gt;</c>, <c>P</c>, <c>P&lt;n&gt;</c> (the optional integer is
        /// the fractional digit count). Custom format strings (<c>#,##0.00</c>) and
        /// exponential / general formats are out of scope.
        /// </summary>
        Numeric,

        /// <summary>
        /// Date / time mask using standard .NET date format strings (e.g. <c>MM/dd/yyyy</c>,
        /// <c>g</c>, <c>F</c>) — the engine implemented in <c>DateTimeMaskFormatter</c>, shared
        /// with <see cref="DateOnly"/> and <see cref="TimeOnly"/>. Per-type validation
        /// (rejecting time specifiers in DateOnly, etc.) is a future refinement.
        /// </summary>
        DateTime,

        /// <summary>
        /// .NET 6+ <c>DateOnly</c> mask using standard .NET date format strings. Shares the
        /// <c>DateTimeMaskFormatter</c> engine with <see cref="DateTime"/> and
        /// <see cref="TimeOnly"/>.
        /// </summary>
        DateOnly,

        /// <summary>
        /// .NET 6+ <c>TimeOnly</c> mask using standard .NET time format strings. Shares the
        /// <c>DateTimeMaskFormatter</c> engine with <see cref="DateTime"/> and
        /// <see cref="DateOnly"/>.
        /// </summary>
        TimeOnly,

        /// <summary>
        /// <em>[Not yet implemented]</em> <c>DateTimeOffset</c> mask. Like
        /// <see cref="DateTime"/>, but additionally renders a user time zone.
        /// </summary>
        DateTimeOffset,

        /// <summary>
        /// Time-interval mask supporting day / hour / minute / second / fractional-second
        /// specifiers — the engine implemented in <c>TimeSpanMaskFormatter</c>.
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
