using System;
using WWControls.Core.Display;

namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Base class for the data-annotation mask attributes. Applied to a model property, a mask
    /// attribute declares the input mask a <c>SearchDataGrid</c> column should adopt when the
    /// column is generated in <em>smart</em> mode (see <c>GridColumn.IsSmart</c>). The grid maps
    /// the attribute onto its text editor's <c>Mask</c> / <c>MaskType</c> surface — the same
    /// surface a consumer could configure by hand in XAML.
    /// </summary>
    /// <remarks>
    /// Each concrete subclass fixes the <see cref="MaskType"/> (the engine that interprets the
    /// pattern) and the consumer supplies the <see cref="Mask"/> pattern. Only the masks whose
    /// engines are implemented in <see cref="Core.Display.MaskFormatterFactory"/> ship as
    /// attributes today: <see cref="SimpleMaskAttribute"/>, <see cref="NumericMaskAttribute"/>,
    /// and <see cref="DateTimeMaskAttribute"/>. RegEx / regular masks are deferred until their
    /// Core engines exist.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class MaskAttribute : Attribute
    {
        /// <param name="mask">The mask pattern, interpreted per <see cref="MaskType"/>.</param>
        protected MaskAttribute(string mask)
        {
            Mask = mask;
        }

        /// <summary>The mask pattern. Grammar is determined by <see cref="MaskType"/>.</summary>
        public string Mask { get; }

        /// <summary>
        /// The engine used to interpret <see cref="Mask"/>. Fixed by the concrete attribute type.
        /// </summary>
        public abstract MaskType MaskType { get; }

        /// <summary>
        /// When <c>true</c>, the read-only display cell formats its value through the same mask
        /// used in edit mode (the column's display converter / string format are suppressed).
        /// Maps to <c>TextEditSettings.UseMaskAsDisplayFormat</c>. Default <c>false</c>.
        /// </summary>
        public bool UseMaskAsDisplayFormat { get; set; }
    }
}
