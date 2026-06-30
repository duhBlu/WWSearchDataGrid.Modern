using WWControls.Core.Display;

namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Declares a <see cref="Core.Display.MaskType.Simple"/> input mask for a property — the
    /// slot grammar (<c>0</c>/<c>9</c>/<c>L</c>/… with literal characters), e.g.
    /// <c>"(000) 000-0000"</c> for a phone number. The owning column adopts this mask when
    /// generated in smart mode.
    /// </summary>
    /// <example>
    /// <code>
    /// [SimpleMask("(000) 000-0000")]
    /// public string Phone { get; set; }
    /// </code>
    /// </example>
    public sealed class SimpleMaskAttribute : MaskAttribute
    {
        /// <param name="mask">The simple-grammar mask pattern.</param>
        public SimpleMaskAttribute(string mask) : base(mask) { }

        /// <inheritdoc />
        public override MaskType MaskType => MaskType.Simple;
    }
}
