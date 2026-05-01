using System;
using System.Windows;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Launcher
{
    public sealed class SampleDefinition
    {
        public SampleDefinition(string name, string description, string[] tags, Func<Window> windowFactory)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
            WindowFactory = windowFactory ?? throw new ArgumentNullException(nameof(windowFactory));
        }

        public string Name { get; }
        public string Description { get; }
        public string[] Tags { get; }
        public Func<Window> WindowFactory { get; }
    }
}
