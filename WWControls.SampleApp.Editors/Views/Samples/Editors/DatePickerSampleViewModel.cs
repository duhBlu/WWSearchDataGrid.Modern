using System;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWDatePicker playground: one demo picker whose whole option surface —
    /// mask, popup mode, time input, null input, footer buttons, week numbers, cycle
    /// modifier, display format, min/max clamp, default date — is driven live from the
    /// options panel.
    /// </summary>
    public partial class DatePickerSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime? _value = DateTime.Today;

        [ObservableProperty]
        private string _mask = "d";

        [ObservableProperty]
        private DatePickerPopupMode _popupMode = DatePickerPopupMode.Calendar;

        [ObservableProperty]
        private TimeInputMode _timeInput = TimeInputMode.Auto;

        [ObservableProperty]
        private ModifierKeys _cycleModifier = ModifierKeys.None;

        [ObservableProperty]
        private bool _allowNullInput = true;

        [ObservableProperty]
        private bool _showClearButton = true;

        [ObservableProperty]
        private bool _showTodayButton = true;

        [ObservableProperty]
        private bool _showNowButton = true;

        [ObservableProperty]
        private bool _showWeekNumbers;

        [ObservableProperty]
        private string _displayFormat;

        /// <summary>When on, MinDate/MaxDate clamp the editor to the current calendar year.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MinDate), nameof(MaxDate))]
        private bool _clampToThisYear;

        /// <summary>When on, the popup seeds at the first of next month while the value is null.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DefaultDate))]
        private bool _seedNextMonth;

        public DateTime? MinDate => ClampToThisYear ? new DateTime(DateTime.Today.Year, 1, 1) : null;

        public DateTime? MaxDate => ClampToThisYear ? new DateTime(DateTime.Today.Year, 12, 31) : null;

        public DateTime? DefaultDate => SeedNextMonth
            ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1)
            : null;

        /// <summary>Mask presets — the combo is editable, so any custom pattern can be typed too.</summary>
        public IReadOnlyList<string> MaskPresets { get; } = new[]
        {
            "d",                     // culture short date
            "MM/dd/yyyy",
            "g",                     // culture short date + short time
            "MM/dd/yyyy HH:mm",      // 24-hour time
            "MM/dd/yyyy hh:mm tt",   // 12-hour time
            "MMM dd, yyyy",
            "dddd, MMMM dd, yyyy",
            "t",                     // culture short time (time-only)
        };

        public IReadOnlyList<string> DisplayFormatPresets { get; } = new[]
        {
            "",
            "D",
            "MMM d, yyyy",
            "yyyy-MM-dd",
        };

        public IReadOnlyList<DatePickerPopupMode> PopupModes { get; } =
            new[] { DatePickerPopupMode.Calendar, DatePickerPopupMode.ScrollList };

        public IReadOnlyList<TimeInputMode> TimeInputModes { get; } =
            new[] { TimeInputMode.Auto, TimeInputMode.Enabled, TimeInputMode.Disabled };

        public IReadOnlyList<ModifierKeys> CycleModifiers { get; } =
            new[] { ModifierKeys.None, ModifierKeys.Control, ModifierKeys.Shift, ModifierKeys.Alt };
    }
}
