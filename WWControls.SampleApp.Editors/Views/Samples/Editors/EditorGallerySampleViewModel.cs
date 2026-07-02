using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the gallery sample: each of the five editor controls (WWTextBox, WWNumericUpDown,
    /// WWComboBox, WWDatePicker, WWCheckBox) is used directly on a form — no grid — with its value
    /// echoed back to prove the two-way Value/IsChecked binding round-trips and that the controls are
    /// genuinely grid-agnostic.
    /// </summary>
    public partial class EditorGallerySampleViewModel : ObservableObject
    {
        // WWTextBox — the chrome story (flat vs bordered vs masked vs read-only).
        [ObservableProperty]
        private string _plainText = "Editable text";

        [ObservableProperty]
        private string _borderedText = "Bordered editor";

        [ObservableProperty]
        private string _phone = "5551234567";

        [ObservableProperty]
        private string _readOnlyText = "Read-only value";

        // One of each remaining editor type, bordered (the standalone-form look).
        [ObservableProperty]
        private double _quantity = 12;

        [ObservableProperty]
        private string _selectedFruit = "Cherry";

        [ObservableProperty]
        private DateTime? _dueDate = DateTime.Today.AddDays(7);

        [ObservableProperty]
        private bool? _isActive = true;

        /// <summary>Items for the standalone WWComboBox.</summary>
        public string[] Fruits { get; } = { "Apple", "Banana", "Cherry", "Date", "Elderberry" };
    }
}
