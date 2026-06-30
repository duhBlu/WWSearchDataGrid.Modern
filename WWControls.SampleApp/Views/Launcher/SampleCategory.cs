using System;
using System.Collections.Generic;

namespace WWControls.SampleApp.Views.Launcher
{
    public sealed class SampleCategory
    {
        public SampleCategory(string name, string description, IReadOnlyList<SampleDefinition> samples)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Samples = samples ?? Array.Empty<SampleDefinition>();
        }

        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<SampleDefinition> Samples { get; }
    }
}
