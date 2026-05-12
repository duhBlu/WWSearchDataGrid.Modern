using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace WWSearchDataGrid.Modern.SampleApp.SampleData
{
    /// <summary>
    /// Reads sample-app source files (.xaml, .xaml.cs, .ViewModel.cs) embedded in the assembly so
    /// the source-preview panel in each sample can show the live markup / code that produced the grid.
    /// Resource naming follows MSBuild's default: assembly default namespace + relative path with
    /// directory separators replaced by dots.
    /// </summary>
    public static class SampleSourceLoader
    {
        private const string ResourcePrefix = "WWSearchDataGrid.Modern.SampleApp.";

        /// <summary>
        /// Loads a single embedded resource by its project-relative path
        /// (e.g. "Views/Samples/DataBinding/DataBindingSampleView.xaml").
        /// </summary>
        public static string Load(string relativePath)
        {
            var resourceName = ResourcePrefix + relativePath.Replace('/', '.').Replace('\\', '.');
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return $"// Resource not found: {resourceName}";
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Loads each path as a separate <see cref="SampleSourceFile"/> — preserves order so the
        /// combobox in the sample host shows files in the same order they're declared. Language is
        /// inferred from the extension: <c>.xaml</c> → <c>"XML"</c>, anything else → <c>"C#"</c>.
        /// </summary>
        public static IReadOnlyList<SampleSourceFile> LoadFiles(params string[] relativePaths)
        {
            var list = new List<SampleSourceFile>(relativePaths.Length);
            foreach (var path in relativePaths)
            {
                var name = System.IO.Path.GetFileName(path);
                var lang = path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ? "XML" : "C#";
                list.Add(new SampleSourceFile(name, lang, Load(path)));
            }
            return list;
        }
    }
}
