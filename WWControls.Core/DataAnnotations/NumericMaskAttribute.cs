using WWControls.Core.Display;

namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Declares a <see cref="Core.Display.MaskType.Numeric"/> input mask for a property — a
    /// standard .NET numeric format string (<c>C</c>/<c>N</c>/<c>F</c>/<c>P</c>, optional
    /// precision) with culture-aware separators and currency / percent chrome, e.g. <c>"C2"</c>
    /// for currency. The owning column adopts this mask when generated in smart mode.
    /// </summary>
    /// <example>
    /// <code>
    /// [NumericMask("C2")]
    /// public decimal Salary { get; set; }
    /// </code>
    /// </example>
    public sealed class NumericMaskAttribute : MaskAttribute
    {
        /// <param name="mask">The numeric format string.</param>
        public NumericMaskAttribute(string mask) : base(mask) { }

        /// <inheritdoc />
        public override MaskType MaskType => MaskType.Numeric;
    }
}
