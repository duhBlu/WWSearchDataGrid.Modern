using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWDatePicker sample: default short-date mask, custom format masks, a date+time
    /// format, and a MinDate/MaxDate-clamped editor.
    /// </summary>
    public partial class DatePickerSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime? _dueDate = DateTime.Today.AddDays(7);

        [ObservableProperty]
        private DateTime? _shipDate = DateTime.Today.AddDays(14);

        [ObservableProperty]
        private DateTime? _appointment = DateTime.Today.AddDays(1).AddHours(9).AddMinutes(30);

        [ObservableProperty]
        private DateTime? _inYearDate = DateTime.Today;

        /// <summary>Bounds for the clamped editor — the current calendar year.</summary>
        public DateTime YearStart { get; } = new(DateTime.Today.Year, 1, 1);

        public DateTime YearEnd { get; } = new(DateTime.Today.Year, 12, 31);
    }
}
