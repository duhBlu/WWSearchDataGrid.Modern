using System;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData.Lookups;

namespace WWControls.SampleApp.Grid.SampleData.Generators
{
    public static class OrderGenerator
    {
        public static OrderItem Create(Random rnd, int index)
        {
            // Deterministic per-index bucketing so every DateInterval predicate
            // (IsToday, IsYesterday, IsThisWeek, IsLastWeek, IsThisMonth, IsThisYear,
            // IsLastYear, IsTomorrow, IsNextWeek, …) is guaranteed a non-empty row set
            // regardless of sample size.
            var orderDate = OrderDateForIndex(index, rnd);

            var statusIdx = rnd.Next(OrderLookups.OrderStatuses.Length);
            var status = OrderLookups.OrderStatuses[statusIdx];
            var location = status == null
                ? OrderLookups.OrderLocations[0]
                : OrderLookups.OrderLocations[Math.Min(statusIdx, OrderLookups.OrderLocations.Length - 1)];

            var cancelled = rnd.NextDouble() < 0.03;
            var submitted = status != null;
            var qty = rnd.Next(1, 50);
            var totalPrice = Math.Round((decimal)(rnd.NextDouble() * 25000) + 100m, 2);
            var amountDue = Math.Round(totalPrice * (decimal)(0.3 + rnd.NextDouble() * 0.5), 2);
            var discount = rnd.NextDouble() < 0.7
                ? (decimal?)null
                : Math.Round((decimal)(rnd.NextDouble() * 15), 0);
            var csz = Cities.CityStateZips[rnd.Next(Cities.CityStateZips.Length)];
            var customerIdx = rnd.Next(Names.CustomerNames.Length);

            DateTime? scheduleDate = null;
            if (submitted)
                scheduleDate = ScheduleDateForIndex(index, orderDate, rnd);

            return new OrderItem
            {
                OrderHeaderId = 10001 + index,
                OrderNumber = 50001 + index,
                ProductionNumber = submitted && rnd.NextDouble() < 0.3
                    ? $"PR-{rnd.Next(10000, 99999)}"
                    : null,
                OrderStatusName = cancelled ? "Cancelled" : status,
                OrderLocationName = location,
                OrderDate = orderDate,
                CreateEmployeeId = 1000 + rnd.Next(1, 200),
                EmployeeName = Names.EmployeeNames[rnd.Next(Names.EmployeeNames.Length)],
                CustomerName = Names.CustomerNames[customerIdx],
                DealerNumber = 1000 + customerIdx * 37 % 9000,
                DealerPO = $"PO-{rnd.Next(100000, 999999)}",
                DealerName = Names.CustomerNames[customerIdx],
                JobName = OrderLookups.JobNames[rnd.Next(OrderLookups.JobNames.Length)],
                ProductLineName = OrderLookups.ProductLines[rnd.Next(OrderLookups.ProductLines.Length)],
                OrderTypeName = OrderLookups.OrderTypes[rnd.Next(OrderLookups.OrderTypes.Length)],
                OrderCancelled = cancelled,
                OrderItemsTotalQuantity = qty,
                OrderItemsTotalPrice = totalPrice,
                AmountDue = amountDue,
                DiscountPercent = discount,
                SalesSubRepName = Names.SalesReps[rnd.Next(Names.SalesReps.Length)],
                Address1 = Cities.Streets[rnd.Next(Cities.Streets.Length)],
                City = csz.City,
                State = csz.State,
                ZipCode = csz.Zip,
                ShipViaTypeName = OrderLookups.ShipViaTypes[rnd.Next(OrderLookups.ShipViaTypes.Length)],
                SpecialInstructionsText = OrderLookups.SpecialInstructions[rnd.Next(OrderLookups.SpecialInstructions.Length)],
                LoadNumber = rnd.NextDouble() < 0.85 ? null : rnd.Next(1, 80),
                DropNumber = rnd.NextDouble() < 0.85 ? null : rnd.Next(1, 10),
                ScheduleDate = scheduleDate,
                Submitted = submitted,
                HeaderOptionsXml = null
            };
        }

        /// <summary>
        /// Maps an index to a guaranteed coverage bucket for OrderDate so every past-leaning
        /// DateInterval predicate has rows. Buckets are deterministic by index modulo 100,
        /// time-of-day randomised so chips show varied values.
        /// </summary>
        private static DateTime OrderDateForIndex(int index, Random rnd)
        {
            var bucket = index % 100;
            var today = DateTime.Today;
            DateTime date;

            if (bucket < 10)
                date = today;                                       // 10% IsToday
            else if (bucket < 20)
                date = today.AddDays(-1);                            // 10% IsYesterday
            else if (bucket < 30)
                date = EarlierThisWeek(today, rnd);                  // 10% IsThisWeek (excl. today/yesterday)
            else if (bucket < 40)
                date = LastWeek(today, rnd);                         // 10% IsLastWeek
            else if (bucket < 55)
                date = EarlierThisMonth(today, rnd);                 // 15% IsThisMonth (before this week)
            else if (bucket < 70)
                date = EarlierThisYear(today, rnd);                  // 15% IsThisYear (before this month)
            else if (bucket < 85)
                date = LastYear(today, rnd);                         // 15% IsLastYear (PriorThisYear)
            else
                date = RandomPastTwoYears(today, rnd);               // 15% filler

            return date
                .AddHours(rnd.Next(7, 18))
                .AddMinutes(rnd.Next(0, 60));
        }

        /// <summary>
        /// Maps an index to a bucket for ScheduleDate so every future-leaning DateInterval
        /// predicate (IsTomorrow, IsThisWeek-after-today, IsNextWeek, LaterThisMonth,
        /// LaterThisYear, BeyondThisYear) has rows. Falls back to <c>orderDate + 7..45</c>
        /// outside the targeted future buckets so most schedules still trail their order date.
        /// </summary>
        private static DateTime ScheduleDateForIndex(int index, DateTime orderDate, Random rnd)
        {
            var bucket = (index * 7) % 100;
            var today = DateTime.Today;

            if (bucket < 8)
                return today.AddHours(rnd.Next(8, 18));              // 8% IsToday
            if (bucket < 16)
                return today.AddDays(1).AddHours(rnd.Next(8, 18));   // 8% IsTomorrow
            if (bucket < 24)
                return LaterThisWeek(today, rnd).AddHours(rnd.Next(8, 18));    // 8% LaterThisWeek
            if (bucket < 32)
                return NextWeek(today, rnd).AddHours(rnd.Next(8, 18));         // 8% IsNextWeek
            if (bucket < 44)
                return LaterThisMonth(today, rnd).AddHours(rnd.Next(8, 18));   // 12% LaterThisMonth
            if (bucket < 56)
                return LaterThisYear(today, rnd).AddHours(rnd.Next(8, 18));    // 12% LaterThisYear
            if (bucket < 64)
                return BeyondThisYear(today, rnd).AddHours(rnd.Next(8, 18));   // 8% BeyondThisYear

            // 36% — schedule based on the order date, typically near-future
            return orderDate.AddDays(rnd.Next(7, 45));
        }

        // ── Date-bucket helpers ──────────────────────────────────────────────────────

        private static DateTime EarlierThisWeek(DateTime today, Random rnd)
        {
            // Days between Sunday-of-this-week and yesterday (exclusive of today / yesterday).
            int daysSinceSunday = (int)today.DayOfWeek;
            if (daysSinceSunday <= 2)
                return today.AddDays(-2);                  // Mon/Tue: nothing earlier in week — fall back to last week's tail
            int offset = rnd.Next(2, daysSinceSunday + 1); // 2..daysSinceSunday (avoids today and yesterday)
            return today.AddDays(-offset);
        }

        private static DateTime LaterThisWeek(DateTime today, Random rnd)
        {
            int daysUntilSaturday = 6 - (int)today.DayOfWeek;
            if (daysUntilSaturday < 1)
                return today.AddDays(1);                   // Saturday: roll to tomorrow as a graceful fallback
            int offset = rnd.Next(1, daysUntilSaturday + 1);
            return today.AddDays(offset);
        }

        private static DateTime LastWeek(DateTime today, Random rnd)
        {
            // Sunday..Saturday of the prior calendar week.
            int daysSinceSunday = (int)today.DayOfWeek;
            var startOfLastWeek = today.AddDays(-daysSinceSunday - 7);
            return startOfLastWeek.AddDays(rnd.Next(0, 7));
        }

        private static DateTime NextWeek(DateTime today, Random rnd)
        {
            int daysSinceSunday = (int)today.DayOfWeek;
            var startOfNextWeek = today.AddDays(7 - daysSinceSunday);
            return startOfNextWeek.AddDays(rnd.Next(0, 7));
        }

        private static DateTime EarlierThisMonth(DateTime today, Random rnd)
        {
            // From the 1st of this month to the day before this week.
            int daysSinceSunday = (int)today.DayOfWeek;
            var startOfThisWeek = today.AddDays(-daysSinceSunday);
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);
            int days = Math.Max(1, (startOfThisWeek - firstOfMonth).Days);
            if (days <= 1) return firstOfMonth;            // first week of the month: nothing "earlier" — pin to the 1st
            return firstOfMonth.AddDays(rnd.Next(0, days));
        }

        private static DateTime LaterThisMonth(DateTime today, Random rnd)
        {
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var startOfNextWeek = today.AddDays(7 - (int)today.DayOfWeek);
            int day = Math.Min(daysInMonth, startOfNextWeek.Day + rnd.Next(0, 7));
            return new DateTime(today.Year, today.Month, Math.Max(1, day));
        }

        private static DateTime EarlierThisYear(DateTime today, Random rnd)
        {
            // From January 1st up to the day before this month.
            var firstOfYear = new DateTime(today.Year, 1, 1);
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);
            int days = Math.Max(1, (firstOfMonth - firstOfYear).Days);
            if (days <= 1) return firstOfYear;             // January: nothing earlier in the year
            return firstOfYear.AddDays(rnd.Next(0, days));
        }

        private static DateTime LaterThisYear(DateTime today, Random rnd)
        {
            // From the start of next month through December 31st.
            int monthsLeft = 12 - today.Month;
            if (monthsLeft < 1)
                return new DateTime(today.Year, 12, Math.Min(31, today.Day + rnd.Next(1, 10)));
            int targetMonth = today.Month + rnd.Next(1, monthsLeft + 1);
            int dayCap = DateTime.DaysInMonth(today.Year, targetMonth);
            return new DateTime(today.Year, targetMonth, rnd.Next(1, dayCap + 1));
        }

        private static DateTime LastYear(DateTime today, Random rnd)
        {
            var firstOfLastYear = new DateTime(today.Year - 1, 1, 1);
            return firstOfLastYear.AddDays(rnd.Next(0, 365));
        }

        private static DateTime BeyondThisYear(DateTime today, Random rnd)
        {
            // Next calendar year (a couple of months in to keep dates "real").
            return new DateTime(today.Year + 1, rnd.Next(1, 13), rnd.Next(1, 28));
        }

        private static DateTime RandomPastTwoYears(DateTime today, Random rnd)
        {
            return today.AddYears(-2).AddDays(rnd.Next(0, 730));
        }
    }
}
