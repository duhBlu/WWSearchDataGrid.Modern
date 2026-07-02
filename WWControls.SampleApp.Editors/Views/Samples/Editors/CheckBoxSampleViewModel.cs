using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWCheckBox sample: two-state, three-state (nullable), and read-only variants.
    /// </summary>
    public partial class CheckBoxSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool? _isActive = true;

        [ObservableProperty]
        private bool? _approval;

        [ObservableProperty]
        private bool? _locked = true;
    }
}
