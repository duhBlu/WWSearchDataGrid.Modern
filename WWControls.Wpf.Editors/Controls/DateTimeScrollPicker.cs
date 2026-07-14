using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Scroll-list date/time picker — the <see cref="DatePickerPopupMode.ScrollList"/> popup
    /// surface of <see cref="SegmentedDateTimeEditor"/>. Presents one <see cref="WWLoopingSelector"/>
    /// column per date/time unit (month, day, year, then hour, minute, AM/PM when
    /// <see cref="ShowTime"/> is set); the centered row of each column composes the selected
    /// <see cref="Value"/>. Selection commits live — scrolling a column immediately writes the
    /// composed date back to <see cref="Value"/>, clamped to <see cref="MinDate"/> /
    /// <see cref="MaxDate"/>.
    /// </summary>
    /// <remarks>
    /// The day column's item list tracks the month/year selection (28–31 rows); an out-of-range
    /// day clamps to the new month's last day. The year column spans
    /// <see cref="MinDate"/>.Year–<see cref="MaxDate"/>.Year (defaults 1900–2100) and clamps at
    /// its ends rather than looping — wrapping a century range surprises more than it helps.
    /// With <see cref="Value"/> null, the columns position at <see cref="DefaultDate"/> (or now)
    /// as a starting point without committing; the first user scroll commits.
    /// </remarks>
    [TemplatePart(Name = PartMonth, Type = typeof(WWLoopingSelector))]
    [TemplatePart(Name = PartDay, Type = typeof(WWLoopingSelector))]
    [TemplatePart(Name = PartYear, Type = typeof(WWLoopingSelector))]
    [TemplatePart(Name = PartHour, Type = typeof(WWLoopingSelector))]
    [TemplatePart(Name = PartMinute, Type = typeof(WWLoopingSelector))]
    [TemplatePart(Name = PartAmPm, Type = typeof(WWLoopingSelector))]
    public class DateTimeScrollPicker : Control
    {
        private const string PartMonth = "PART_MonthSelector";
        private const string PartDay = "PART_DaySelector";
        private const string PartYear = "PART_YearSelector";
        private const string PartHour = "PART_HourSelector";
        private const string PartMinute = "PART_MinuteSelector";
        private const string PartAmPm = "PART_AmPmSelector";

        private WWLoopingSelector _month, _day, _year, _hour, _minute, _amPm;
        private List<int> _years = new List<int>();
        private bool _suppressSync;

        static DateTimeScrollPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimeScrollPicker),
                new FrameworkPropertyMetadata(typeof(DateTimeScrollPicker)));
        }

        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(DateTime?), typeof(DateTimeScrollPicker),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(DateTimeScrollPicker),
                new PropertyMetadata(null, OnRangeChanged));

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(DateTimeScrollPicker),
                new PropertyMetadata(null, OnRangeChanged));

        /// <summary>Starting position for the columns while <see cref="Value"/> is null. Falls back to now.</summary>
        public static readonly DependencyProperty DefaultDateProperty =
            DependencyProperty.Register(nameof(DefaultDate), typeof(DateTime?), typeof(DateTimeScrollPicker),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ShowDateProperty =
            DependencyProperty.Register(nameof(ShowDate), typeof(bool), typeof(DateTimeScrollPicker),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ShowTimeProperty =
            DependencyProperty.Register(nameof(ShowTime), typeof(bool), typeof(DateTimeScrollPicker),
                new PropertyMetadata(false));

        /// <summary>24-hour clock: the hour column runs 00–23 and the AM/PM column is hidden.</summary>
        public static readonly DependencyProperty Is24HourProperty =
            DependencyProperty.Register(nameof(Is24Hour), typeof(bool), typeof(DateTimeScrollPicker),
                new PropertyMetadata(false, OnRangeChanged));

        public DateTime? Value
        {
            get => (DateTime?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public DateTime? MinDate
        {
            get => (DateTime?)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        public DateTime? MaxDate
        {
            get => (DateTime?)GetValue(MaxDateProperty);
            set => SetValue(MaxDateProperty, value);
        }

        public DateTime? DefaultDate
        {
            get => (DateTime?)GetValue(DefaultDateProperty);
            set => SetValue(DefaultDateProperty, value);
        }

        public bool ShowDate
        {
            get => (bool)GetValue(ShowDateProperty);
            set => SetValue(ShowDateProperty, value);
        }

        public bool ShowTime
        {
            get => (bool)GetValue(ShowTimeProperty);
            set => SetValue(ShowTimeProperty, value);
        }

        public bool Is24Hour
        {
            get => (bool)GetValue(Is24HourProperty);
            set => SetValue(Is24HourProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DateTimeScrollPicker)d;
            if (self._suppressSync) return;
            self.PositionSelectors();
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DateTimeScrollPicker)d;
            self.PopulateItemSources();
            self.PositionSelectors();
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            foreach (var s in Selectors()) s.SelectedIndexChanged -= OnSelectorChanged;

            _month = GetTemplateChild(PartMonth) as WWLoopingSelector;
            _day = GetTemplateChild(PartDay) as WWLoopingSelector;
            _year = GetTemplateChild(PartYear) as WWLoopingSelector;
            _hour = GetTemplateChild(PartHour) as WWLoopingSelector;
            _minute = GetTemplateChild(PartMinute) as WWLoopingSelector;
            _amPm = GetTemplateChild(PartAmPm) as WWLoopingSelector;

            PopulateItemSources();
            PositionSelectors();

            foreach (var s in Selectors()) s.SelectedIndexChanged += OnSelectorChanged;
        }

        private IEnumerable<WWLoopingSelector> Selectors()
        {
            if (_month != null) yield return _month;
            if (_day != null) yield return _day;
            if (_year != null) yield return _year;
            if (_hour != null) yield return _hour;
            if (_minute != null) yield return _minute;
            if (_amPm != null) yield return _amPm;
        }

        /// <summary>The date the columns show right now — <see cref="Value"/>, or the uncommitted starting position.</summary>
        private DateTime AnchorDate() => Value ?? DefaultDate ?? DateTime.Now;

        private void PopulateItemSources()
        {
            var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;

            if (_month != null)
                _month.ItemsSource = dtfi.AbbreviatedMonthNames.Where(n => !string.IsNullOrEmpty(n)).ToList();

            if (_year != null)
            {
                int minYear = MinDate?.Year ?? 1900;
                int maxYear = MaxDate?.Year ?? 2100;
                if (maxYear < minYear) maxYear = minYear;
                _years = Enumerable.Range(minYear, maxYear - minYear + 1).ToList();
                _year.ItemsSource = _years;
                _year.IsLooping = false;
            }

            if (_hour != null)
                _hour.ItemsSource = Is24Hour
                    ? Enumerable.Range(0, 24).Select(h => h.ToString("00", CultureInfo.CurrentCulture)).ToList()
                    : (System.Collections.IList)Enumerable.Range(1, 12).Select(h => h.ToString(CultureInfo.CurrentCulture)).ToList();

            if (_minute != null)
                _minute.ItemsSource = Enumerable.Range(0, 60).Select(m => m.ToString("00", CultureInfo.CurrentCulture)).ToList();

            if (_amPm != null)
                _amPm.ItemsSource = new List<string> { dtfi.AMDesignator, dtfi.PMDesignator };

            RebuildDayItems(AnchorDate().Year, AnchorDate().Month);
        }

        private void RebuildDayItems(int year, int month)
        {
            if (_day == null) return;
            int days = DateTime.DaysInMonth(year, month);
            var current = _day.ItemsSource;
            if (current != null && current.Count == days) return;
            int keep = Math.Min(_day.SelectedIndex, days - 1);
            _day.ItemsSource = Enumerable.Range(1, days).Select(d => d.ToString(CultureInfo.CurrentCulture)).ToList();
            _day.SelectedIndex = keep;
        }

        /// <summary>Moves every column to <see cref="AnchorDate"/> without committing a value.</summary>
        private void PositionSelectors()
        {
            if (_suppressSync) return;
            _suppressSync = true;
            try
            {
                var dt = AnchorDate();
                if (_month != null) _month.SelectedIndex = dt.Month - 1;
                if (_year != null && _years.Count > 0)
                    _year.SelectedIndex = Math.Max(0, Math.Min(_years.Count - 1, dt.Year - _years[0]));
                RebuildDayItems(dt.Year, dt.Month);
                if (_day != null) _day.SelectedIndex = dt.Day - 1;
                if (_hour != null)
                    _hour.SelectedIndex = Is24Hour ? dt.Hour : (dt.Hour % 12 == 0 ? 12 : dt.Hour % 12) - 1;
                if (_minute != null) _minute.SelectedIndex = dt.Minute;
                if (_amPm != null) _amPm.SelectedIndex = dt.Hour < 12 ? 0 : 1;
            }
            finally { _suppressSync = false; }
        }

        private void OnSelectorChanged(object sender, EventArgs e)
        {
            if (_suppressSync) return;
            _suppressSync = true;
            try
            {
                var anchor = AnchorDate();

                int year = _year != null && _years.Count > 0 ? _years[_year.SelectedIndex] : anchor.Year;
                int month = _month != null ? _month.SelectedIndex + 1 : anchor.Month;

                // Month / year moves can shrink the day list; clamp the day before reading it.
                RebuildDayItems(year, month);
                int daysInMonth = DateTime.DaysInMonth(year, month);
                int day = _day != null ? Math.Min(_day.SelectedIndex + 1, daysInMonth) : Math.Min(anchor.Day, daysInMonth);
                if (_day != null && _day.SelectedIndex != day - 1) _day.SelectedIndex = day - 1;

                int hour, minute, second = anchor.Second;
                if (ShowTime && _hour != null && _minute != null)
                {
                    if (Is24Hour)
                    {
                        hour = _hour.SelectedIndex;
                    }
                    else
                    {
                        int hour12 = _hour.SelectedIndex + 1;
                        bool isPm = _amPm != null && _amPm.SelectedIndex == 1;
                        hour = (hour12 % 12) + (isPm ? 12 : 0);
                    }
                    minute = _minute.SelectedIndex;
                }
                else
                {
                    hour = anchor.Hour;
                    minute = anchor.Minute;
                }

                var composed = new DateTime(year, month, day, hour, minute, second);
                if (MinDate.HasValue && composed < MinDate.Value) composed = MinDate.Value;
                if (MaxDate.HasValue && composed > MaxDate.Value) composed = MaxDate.Value;

                Value = composed;

                // If the range clamp moved the date away from the raw column positions, reflect it.
                _suppressSync = false;
                if (composed != new DateTime(year, month, day, hour, minute, second))
                    PositionSelectors();
            }
            finally { _suppressSync = false; }
        }
    }
}
