using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Maps one or more property names to a custom editor <see cref="DataTemplate"/> in
    /// <see cref="WWPropertyGrid"/>. Declared in the grid's <see cref="WWPropertyGrid.EditorDefinitions"/>
    /// collection; the grid attaches the matching template to each property item it builds.
    /// </summary>
    public class WWEditorDefinition
    {
        private HashSet<string> _names;

        /// <summary>Comma-separated property names this editor applies to.</summary>
        public string TargetProperties { get; set; }

        /// <summary>
        /// The template to render for matching properties. Its <c>DataContext</c> is the
        /// <see cref="WWPropertyItem"/>, so editors bind to <c>Value</c> (two-way).
        /// </summary>
        public DataTemplate EditingTemplate { get; set; }

        /// <summary>Returns true when this definition matches the given property name.</summary>
        public bool Matches(string propertyName)
        {
            if (string.IsNullOrEmpty(TargetProperties) || string.IsNullOrEmpty(propertyName))
                return false;

            if (_names == null)
            {
                _names = new HashSet<string>(
                    TargetProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()),
                    StringComparer.OrdinalIgnoreCase);
            }

            return _names.Contains(propertyName);
        }
    }
}
