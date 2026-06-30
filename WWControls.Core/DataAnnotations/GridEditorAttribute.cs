namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Declares the editor a property should use specifically when shown in a data grid. Takes
    /// precedence over <see cref="DefaultEditorAttribute"/> for the grid host. This is the editor
    /// attribute a <c>SearchDataGrid</c> column resolves first when generated in smart mode.
    /// </summary>
    /// <example>
    /// <code>
    /// [GridEditor(EditorKind.Spin)]
    /// public int Quantity { get; set; }
    /// </code>
    /// </example>
    public sealed class GridEditorAttribute : EditorAttributeBase
    {
        /// <param name="editor">The editor to use when the property is shown in a grid.</param>
        public GridEditorAttribute(EditorKind editor) : base(editor) { }

        /// <inheritdoc />
        public override EditorContext Context => EditorContext.Grid;
    }
}
