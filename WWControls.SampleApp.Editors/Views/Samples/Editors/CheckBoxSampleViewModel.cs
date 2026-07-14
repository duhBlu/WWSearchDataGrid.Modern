using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWCheckBox playground: one demo checkbox whose option surface —
    /// three-state, read-only, click mode, hover modifier — is driven live from the
    /// options panel, plus a sweepable checkbox list shown while ClickMode is Hover.
    /// </summary>
    public partial class CheckBoxSampleViewModel : ObservableObject
    {
        public CheckBoxSampleViewModel()
        {
            HoverItems = new[]
            {
                "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday",
            }.Select(day => new HoverListItem(day)).ToArray();

            foreach (var item in HoverItems)
                item.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CheckedSummary));
        }

        [ObservableProperty]
        private bool? _isChecked = true;

        [ObservableProperty]
        private bool _isThreeState = true;

        [ObservableProperty]
        private bool _isReadOnly;

        [ObservableProperty]
        private ClickMode _clickMode = ClickMode.Release;

        [ObservableProperty]
        private ModifierKeys _hoverModifier = ModifierKeys.None;

        public IReadOnlyList<ClickMode> ClickModes { get; } =
            new[] { ClickMode.Release, ClickMode.Press, ClickMode.Hover };

        public IReadOnlyList<ModifierKeys> HoverModifiers { get; } =
            new[] { ModifierKeys.None, ModifierKeys.Control, ModifierKeys.Shift, ModifierKeys.Alt };

        /// <summary>Rows for the hover playground list.</summary>
        public IReadOnlyList<HoverListItem> HoverItems { get; }

        public string CheckedSummary =>
            $"Checked: {HoverItems.Count(i => i.IsChecked == true)} of {HoverItems.Count}";
    }

    /// <summary>One row in the hover playground list.</summary>
    public partial class HoverListItem : ObservableObject
    {
        public HoverListItem(string name) => Name = name;

        public string Name { get; }

        [ObservableProperty]
        private bool? _isChecked = false;
    }
}
