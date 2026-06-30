namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Declares the editor a property should use when shown in a property grid. Defined for API
    /// symmetry with the other editor attributes; <strong>inert today</strong> — the library has
    /// no property-grid host, so this attribute is recognized but has no effect. Reserved so
    /// consumer models can annotate ahead of a future property-grid host without churn.
    /// </summary>
    public sealed class PropertyGridEditorAttribute : EditorAttributeBase
    {
        /// <param name="editor">The editor to use in a property-grid host (reserved).</param>
        public PropertyGridEditorAttribute(EditorKind editor) : base(editor) { }

        /// <inheritdoc />
        public override EditorContext Context => EditorContext.PropertyGrid;
    }
}
