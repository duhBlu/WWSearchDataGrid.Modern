using System.Diagnostics;
using System.Reflection;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace WWControls.SampleApp.Controls
{
    /// <summary>
    /// Loads the custom <c>.xshd</c> syntax-highlighting definitions from
    /// <c>Themes/SyntaxHighlighting/</c> and registers them under the names AvalonEdit's bundled
    /// definitions use ("XML" / "C#"). Registering with the same name overwrites the dictionary
    /// entry in <see cref="HighlightingManager"/>, so subsequent
    /// <see cref="HighlightingManager.GetDefinition(string)"/> calls return the customized
    /// palette.
    /// </summary>
    public static class CustomSyntaxHighlighting
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            RegisterFromResource(
                "WWControls.SampleApp.Themes.SyntaxHighlighting.CSharp.xshd",
                name: "C#",
                extensions: new[] { ".cs" });

            RegisterFromResource(
                "WWControls.SampleApp.Themes.SyntaxHighlighting.Xaml.xshd",
                name: "XML",
                extensions: new[] { ".xml", ".xaml", ".xshd", ".config", ".csproj", ".targets" });
        }

        private static void RegisterFromResource(string resourceName, string name, string[] extensions)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.WriteLine($"CustomSyntaxHighlighting: resource not found — {resourceName}");
                return;
            }

            using var reader = XmlReader.Create(stream);
            // HighlightingLoader.Load resolves cross-definition references (e.g. the C# XSHD's
            // import of "XmlDoc/DocCommentSet") through the manager passed in.
            var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting(name, extensions, definition);
        }
    }
}
