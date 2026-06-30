using System;

namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Base class for the editor data-annotation attributes. Applied to a model property, an
    /// editor attribute declares which <see cref="EditorKind"/> a host should use to edit that
    /// property. The concrete subclass fixes the <see cref="Context"/> (which host the attribute
    /// targets); a property can carry one attribute per host.
    /// </summary>
    /// <remarks>
    /// Named <c>EditorAttributeBase</c> rather than <c>EditorAttribute</c> to avoid colliding with
    /// <see cref="System.ComponentModel.EditorAttribute"/>. A <c>SearchDataGrid</c> column in smart
    /// mode resolves its editor by preferring the <see cref="EditorContext.Grid"/> attribute, then
    /// the <see cref="EditorContext.Default"/> attribute; <see cref="EditorContext.LayoutControl"/>
    /// and <see cref="EditorContext.PropertyGrid"/> are recognized but currently inert (no host).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class EditorAttributeBase : Attribute
    {
        /// <param name="editor">The editor this property should use in the attribute's host.</param>
        protected EditorAttributeBase(EditorKind editor)
        {
            Editor = editor;
        }

        /// <summary>The editor to use in the host identified by <see cref="Context"/>.</summary>
        public EditorKind Editor { get; }

        /// <summary>The presentation host this attribute targets. Fixed by the concrete type.</summary>
        public abstract EditorContext Context { get; }
    }
}
