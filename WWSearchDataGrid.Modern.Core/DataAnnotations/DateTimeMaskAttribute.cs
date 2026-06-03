using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.Core.DataAnnotations
{
    /// <summary>
    /// Declares a <see cref="Core.Display.MaskType.DateTime"/> input mask for a property — a
    /// standard .NET date/time format string (<c>d</c>, <c>g</c>, <c>MM/dd/yyyy</c>, …), e.g.
    /// <c>"MM/dd/yyyy"</c>. The owning column adopts this mask when generated in smart mode.
    /// </summary>
    /// <example>
    /// <code>
    /// [DateTimeMask("MM/dd/yyyy")]
    /// public DateTime HireDate { get; set; }
    /// </code>
    /// </example>
    public sealed class DateTimeMaskAttribute : MaskAttribute
    {
        /// <param name="mask">The date/time format string.</param>
        public DateTimeMaskAttribute(string mask) : base(mask) { }

        /// <inheritdoc />
        public override MaskType MaskType => MaskType.DateTime;
    }
}
