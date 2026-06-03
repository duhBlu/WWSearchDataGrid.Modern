namespace WWSearchDataGrid.Modern.Core.DataAnnotations
{
    /// <summary>
    /// Declares the editor a property should use in any host that has no more-specific editor
    /// attribute. A <see cref="GridEditorAttribute"/> on the same property wins over this one
    /// inside a grid.
    /// </summary>
    /// <example>
    /// <code>
    /// [DefaultEditor(EditorKind.ComboBox)]
    /// public string Status { get; set; }
    /// </code>
    /// </example>
    public sealed class DefaultEditorAttribute : EditorAttributeBase
    {
        /// <param name="editor">The editor to use as the default across hosts.</param>
        public DefaultEditorAttribute(EditorKind editor) : base(editor) { }

        /// <inheritdoc />
        public override EditorContext Context => EditorContext.Default;
    }
}
