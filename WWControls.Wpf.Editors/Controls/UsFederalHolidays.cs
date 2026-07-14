using System;
using System.Collections.Generic;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// The eleven United States federal holidays, computed per year with the federal observance
    /// rule applied: a fixed-date holiday that lands on a Saturday is observed the preceding
    /// Friday, and one that lands on a Sunday is observed the following Monday (5 U.S.C. § 6103).
    /// Both the actual calendar date and — when they differ — the observed date resolve as
    /// holidays, so a picker accents whichever one a user is looking at (the observed date carries
    /// an "(observed)" suffix in its name).
    /// </summary>
    /// <remarks>
    /// The nth-weekday holidays (MLK, Washington's Birthday, Memorial Day, Labor Day, Columbus
    /// Day, Thanksgiving) never shift — they're already anchored to a weekday. Juneteenth is
    /// included from its 2021 federal establishment onward; earlier years omit it. Results are
    /// cached per year so repeated lookups from calendar-cell converters stay cheap.
    /// </remarks>
    public static class UsFederalHolidays
    {
        private const int JuneteenthFirstFederalYear = 2021;

        private static readonly object _sync = new object();
        private static readonly Dictionary<int, Dictionary<DateTime, string>> _cache =
            new Dictionary<int, Dictionary<DateTime, string>>();

        /// <summary>True when <paramref name="date"/> is a US federal holiday (actual or observed).</summary>
        public static bool IsHoliday(DateTime date) => GetHolidayName(date) != null;

        /// <summary>
        /// The holiday name for <paramref name="date"/>, or <c>null</c> when it isn't one. An
        /// observed date (e.g. Friday July 3 for a Saturday July 4) returns the name with an
        /// "(observed)" suffix.
        /// </summary>
        public static string GetHolidayName(DateTime date)
        {
            var map = GetYear(date.Year);
            return map.TryGetValue(date.Date, out var name) ? name : null;
        }

        private static Dictionary<DateTime, string> GetYear(int year)
        {
            lock (_sync)
            {
                if (_cache.TryGetValue(year, out var existing)) return existing;
                var map = Build(year);
                _cache[year] = map;
                return map;
            }
        }

        private static Dictionary<DateTime, string> Build(int year)
        {
            var map = new Dictionary<DateTime, string>();

            // Fixed-date holidays observe the nearest weekday when they fall on a weekend.
            AddFixed(map, new DateTime(year, 1, 1), "New Year's Day");
            if (year >= JuneteenthFirstFederalYear)
                AddFixed(map, new DateTime(year, 6, 19), "Juneteenth National Independence Day");
            AddFixed(map, new DateTime(year, 7, 4), "Independence Day");
            AddFixed(map, new DateTime(year, 11, 11), "Veterans Day");
            AddFixed(map, new DateTime(year, 12, 25), "Christmas Day");

            // New Year's Day of the following year is observed the preceding Friday (Dec 31) when
            // Jan 1 lands on a Saturday — that observed date falls inside this year.
            if (new DateTime(year + 1, 1, 1).DayOfWeek == DayOfWeek.Saturday)
                map[new DateTime(year, 12, 31)] = "New Year's Day (observed)";

            // Nth-weekday holidays are already anchored to a weekday — no observance shift.
            map[NthWeekday(year, 1, DayOfWeek.Monday, 3)] = "Birthday of Martin Luther King, Jr.";
            map[NthWeekday(year, 2, DayOfWeek.Monday, 3)] = "Washington's Birthday";
            map[LastWeekday(year, 5, DayOfWeek.Monday)] = "Memorial Day";
            map[NthWeekday(year, 9, DayOfWeek.Monday, 1)] = "Labor Day";
            map[NthWeekday(year, 10, DayOfWeek.Monday, 2)] = "Columbus Day";
            map[NthWeekday(year, 11, DayOfWeek.Thursday, 4)] = "Thanksgiving Day";

            return map;
        }

        private static void AddFixed(Dictionary<DateTime, string> map, DateTime actual, string name)
        {
            map[actual] = name;
            if (actual.DayOfWeek == DayOfWeek.Saturday)
                map[actual.AddDays(-1)] = name + " (observed)";
            else if (actual.DayOfWeek == DayOfWeek.Sunday)
                map[actual.AddDays(1)] = name + " (observed)";
        }

        /// <summary>The <paramref name="occurrence"/>-th <paramref name="weekday"/> of the month (1-based).</summary>
        private static DateTime NthWeekday(int year, int month, DayOfWeek weekday, int occurrence)
        {
            var first = new DateTime(year, month, 1);
            int offset = ((int)weekday - (int)first.DayOfWeek + 7) % 7;
            return first.AddDays(offset + 7 * (occurrence - 1));
        }

        /// <summary>The last <paramref name="weekday"/> of the month.</summary>
        private static DateTime LastWeekday(int year, int month, DayOfWeek weekday)
        {
            var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            int offset = ((int)last.DayOfWeek - (int)weekday + 7) % 7;
            return last.AddDays(-offset);
        }
    }
}
