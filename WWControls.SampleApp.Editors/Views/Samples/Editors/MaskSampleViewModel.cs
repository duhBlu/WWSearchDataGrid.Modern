using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the masking sample: one bound value per masked editor, seeded with raw content the mask
    /// engine formats on load. Values are strings so every MaskType (Simple / Numeric / DateTime /
    /// TimeSpan) round-trips through the same property type — the engine's underlying value is shown
    /// separately via WWTextBox.UnmaskedValue. The prompt-char and readback rows start partially
    /// filled so their prompt characters (and the incomplete state) are visible without focusing.
    /// </summary>
    public partial class MaskSampleViewModel : ObservableObject
    {
        // Simple masks — literal/placeholder grammar over string values.
        [ObservableProperty] private string _phone = "5551234567";
        [ObservableProperty] private string _ssn = "123456789";
        [ObservableProperty] private string _zip = "981094321";
        [ObservableProperty] private string _card = "4111111111111111";
        [ObservableProperty] private string _plate = "ABC123";
        [ObservableProperty] private string _amount = "123456";

        // Numeric masks — .NET numeric format strings, culture-aware.
        [ObservableProperty] private string _currency = "1299.99";
        [ObservableProperty] private string _number = "1234567";
        [ObservableProperty] private string _fixed = "42.5";
        [ObservableProperty] private string _percent = "0.25";

        // Date / time / duration masks — numeric-only patterns (text specifiers aren't maskable).
        [ObservableProperty] private string _date = "07/15/2026";
        [ObservableProperty] private string _time = "09:30:00";
        [ObservableProperty] private string _duration = "01:30:00";

        // PromptChar showcase — same phone mask, three prompt characters. Partially filled so the
        // empty slots (and their prompt chars) render without needing focus.
        [ObservableProperty] private string _promptUnderscore = "555";
        [ObservableProperty] private string _promptHash = "555";
        [ObservableProperty] private string _promptStar = "555";

        // Readback showcase — deliberately incomplete so IsMaskComplete reads false until filled.
        [ObservableProperty] private string _readback = "555123";
    }
}
