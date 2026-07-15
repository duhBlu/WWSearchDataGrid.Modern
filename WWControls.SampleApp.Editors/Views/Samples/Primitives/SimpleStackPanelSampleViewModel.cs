using System.Collections.Generic;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Primitives
{
    /// <summary>
    /// Backs the SimpleStackPanel playground: Orientation and Spacing drive the demo panel live, and
    /// collapsing the middle tile shows that a Collapsed child contributes no spacing gap — the
    /// neighbours close up flush rather than leaving the phantom gap a per-child Margin would.
    /// </summary>
    public partial class SimpleStackPanelSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private Orientation _orientation = Orientation.Horizontal;

        [ObservableProperty]
        private double _spacing = 8;

        [ObservableProperty]
        private bool _hideMiddleTile;

        public IReadOnlyList<Orientation> Orientations { get; } =
            new[] { Orientation.Horizontal, Orientation.Vertical };
    }
}
