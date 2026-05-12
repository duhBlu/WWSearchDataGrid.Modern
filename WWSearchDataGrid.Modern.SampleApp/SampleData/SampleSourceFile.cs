namespace WWSearchDataGrid.Modern.SampleApp.SampleData
{
    /// <summary>
    /// One source file in a sample's source list, loaded as an embedded resource and shown in the
    /// sample host's combobox-driven source viewer. <see cref="Language"/> matches an AvalonEdit
    /// highlighting definition name (<c>"XML"</c>, <c>"C#"</c>) so the editor switches highlighting
    /// when the user picks a different file.
    /// </summary>
    public sealed class SampleSourceFile
    {
        public SampleSourceFile(string name, string language, string content)
        {
            Name = name;
            Language = language;
            Content = content;
        }

        /// <summary>File name shown in the combobox (last segment of the source path).</summary>
        public string Name { get; }

        /// <summary>AvalonEdit highlighting name — <c>"XML"</c> for .xaml, <c>"C#"</c> for .cs.</summary>
        public string Language { get; }

        /// <summary>Full file contents.</summary>
        public string Content { get; }

        public override string ToString() => Name;
    }
}
