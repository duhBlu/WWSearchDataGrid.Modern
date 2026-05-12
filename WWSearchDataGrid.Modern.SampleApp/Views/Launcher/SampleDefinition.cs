using System;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Launcher
{
    public sealed class SampleDefinition
    {
        public SampleDefinition(string name, string description, string[] tags, Func<UserControl> viewFactory)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
            ViewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public string Name { get; }
        public string Description { get; }
        public string[] Tags { get; }
        public Func<UserControl> ViewFactory { get; }
    }
}
