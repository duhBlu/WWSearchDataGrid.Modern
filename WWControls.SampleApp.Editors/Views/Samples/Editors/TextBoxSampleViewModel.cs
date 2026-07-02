using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWTextBox sample: chrome variants (bordered / flat / read-only / aligned), Simple
    /// masks over string values, and Numeric masks over numeric values. Echo TextBlocks prove the
    /// two-way Value binding round-trips through the mask engine.
    /// </summary>
    public partial class TextBoxSampleViewModel : ObservableObject
    {
        // Chrome
        [ObservableProperty]
        private string _borderedText = "Bordered editor";

        [ObservableProperty]
        private string _plainText = "Editable text";

        [ObservableProperty]
        private string _readOnlyText = "Read-only value";

        [ObservableProperty]
        private string _rightAlignedText = "Right-aligned";

        // Simple masks — the bound value stores the raw characters; the mask owns the literals.
        [ObservableProperty]
        private string _phone = "5551234567";

        [ObservableProperty]
        private string _ssn = "123456789";

        [ObservableProperty]
        private string _plate = "ABC123";

        // Numeric masks — standard .NET numeric format strings over numeric values.
        [ObservableProperty]
        private double _price = 1299.99;

        [ObservableProperty]
        private double _rate = 0.25;

        [ObservableProperty]
        private double _weight = 42.5;
    }
}
