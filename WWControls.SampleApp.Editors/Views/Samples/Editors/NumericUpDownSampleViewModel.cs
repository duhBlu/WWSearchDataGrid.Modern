using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWNumericUpDown sample: one editor per stepping/bounding configuration — unbounded,
    /// clamped, large-increment, fractional, and read-only.
    /// </summary>
    public partial class NumericUpDownSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _unbounded = -3;

        [ObservableProperty]
        private double _percentage = 40;

        [ObservableProperty]
        private double _stock = 250;

        [ObservableProperty]
        private double _hours = 7.75;

        [ObservableProperty]
        private double _locked = 99;
    }
}
