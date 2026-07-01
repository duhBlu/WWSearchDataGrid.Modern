using System;
using System.ComponentModel;
using System.Globalization;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// A <see cref="GroupDescription"/> that buckets rows by a derived key rather than the whole
    /// value, supporting the non-<see cref="ColumnGroupInterval.Value"/> modes of
    /// <see cref="ColumnGroupInterval"/> (alphabetical first-letter, date year/month/day/weekday,
    /// and relative date ranges). WPF's built-in <see cref="PropertyGroupDescription"/> only buckets
    /// whole values or first letters, so the date and range modes need this.
    /// </summary>
    /// <remarks>
    /// Group ordering still comes from the leading group <see cref="System.ComponentModel.SortDescription"/>
    /// on the same property path that the grouping engine maintains (D2): because the underlying
    /// rows are sorted by the raw value, the derived buckets surface in the natural order
    /// (chronological for dates, A→Z for alphabetical).
    /// </remarks>
    public sealed class IntervalGroupDescription : GroupDescription
    {
        private readonly string _propertyName;
        private readonly ColumnGroupInterval _interval;

        public IntervalGroupDescription(string propertyName, ColumnGroupInterval interval)
        {
            _propertyName = propertyName;
            _interval = interval;
        }

        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            object value = ReflectionHelper.GetPropValue(item, _propertyName);

            switch (_interval)
            {
                case ColumnGroupInterval.Alphabetical:
                    return AlphabeticalKey(value, culture);

                case ColumnGroupInterval.DateYear:
                case ColumnGroupInterval.DateMonth:
                case ColumnGroupInterval.DateDay:
                case ColumnGroupInterval.DateWeekDay:
                case ColumnGroupInterval.DateRange:
                    return DateKey(value, culture);

                default:
                    // Default / Value — whole value (this type isn't used for those paths, but stay safe).
                    return value ?? string.Empty;
            }
        }

        private static string AlphabeticalKey(object value, CultureInfo culture)
        {
            string text = value?.ToString();
            if (string.IsNullOrEmpty(text)) return "(empty)";
            char first = char.ToUpper(text[0], culture);
            return first.ToString(culture);
        }

        private string DateKey(object value, CultureInfo culture)
        {
            if (!TryGetDate(value, out DateTime date)) return "(no date)";

            switch (_interval)
            {
                case ColumnGroupInterval.DateYear:
                    return date.Year.ToString(culture);

                case ColumnGroupInterval.DateMonth:
                    // e.g. "June 2026" — month name in the supplied culture.
                    return $"{culture.DateTimeFormat.GetMonthName(date.Month)} {date.Year.ToString(culture)}";

                case ColumnGroupInterval.DateDay:
                    return date.ToString("d", culture);

                case ColumnGroupInterval.DateWeekDay:
                    return culture.DateTimeFormat.GetDayName(date.DayOfWeek);

                case ColumnGroupInterval.DateRange:
                    return RelativeRangeKey(date);

                default:
                    return date.ToString(culture);
            }
        }

        /// <summary>
        /// Buckets a date relative to today. Minimal set: Today / Yesterday / This Week / This Month
        /// / This Year / Older / Future.
        /// </summary>
        private static string RelativeRangeKey(DateTime date)
        {
            DateTime today = DateTime.Today;
            DateTime day = date.Date;

            if (day == today) return "Today";
            if (day == today.AddDays(-1)) return "Yesterday";
            if (day > today) return "Future";

            // Start of the current week (Sunday) so "This Week" excludes today/yesterday already handled.
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            if (day >= startOfWeek) return "This Week";

            if (day.Year == today.Year && day.Month == today.Month) return "This Month";
            if (day.Year == today.Year) return "This Year";
            return "Older";
        }

        private static bool TryGetDate(object value, out DateTime date)
        {
            switch (value)
            {
                case DateTime dt:
                    date = dt;
                    return true;
                case DateTimeOffset dto:
                    date = dto.LocalDateTime;
                    return true;
                default:
                    date = default;
                    return false;
            }
        }
    }
}
