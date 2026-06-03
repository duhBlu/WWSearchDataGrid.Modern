namespace WWSearchDataGrid.Modern.Core.DataAnnotations
{
    /// <summary>
    /// Declares the editor a property should use when shown in a layout control. Defined for API
    /// symmetry with the other editor attributes; <strong>inert today</strong> — the library has
    /// no layout-control host, so this attribute is recognized but has no effect. Reserved so
    /// consumer models can annotate ahead of a future layout-control host without churn.
    /// </summary>
    public sealed class LayoutControlEditorAttribute : EditorAttributeBase
    {
        /// <param name="editor">The editor to use in a layout-control host (reserved).</param>
        public LayoutControlEditorAttribute(EditorKind editor) : base(editor) { }

        /// <inheritdoc />
        public override EditorContext Context => EditorContext.LayoutControl;
    }
}
