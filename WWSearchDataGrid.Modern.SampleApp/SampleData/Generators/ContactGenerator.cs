using System;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;

namespace WWSearchDataGrid.Modern.SampleApp.SampleData.Generators
{
    public static class ContactGenerator
    {
        private static readonly char[] Letters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        public static Contact Create(Random rnd, int index)
        {
            var first = Names.FirstNames[rnd.Next(Names.FirstNames.Length)];
            var last = Names.LastNames[rnd.Next(Names.LastNames.Length)];
            var birthYear = DateTime.Today.Year - rnd.Next(20, 65);
            var birthMonth = rnd.Next(1, 13);
            var birthDay = rnd.Next(1, DateTime.DaysInMonth(birthYear, birthMonth) + 1);
            var lastSeenDays = rnd.Next(0, 365);
            var lastSeenSeconds = rnd.Next(0, 86_400);
            var loggedDays = rnd.Next(0, 730);
            var loggedSeconds = rnd.Next(0, 86_400);
            var dayDays = rnd.Next(0, 365);
            var isoDays = rnd.Next(0, 90);
            var isoSeconds = rnd.Next(0, 86_400);

            // Empty raw values for some rows so the prompt characters in the mask are visible.
            bool emptyPhone = rnd.NextDouble() < 0.10;
            bool emptyAccount = rnd.NextDouble() < 0.15;
            bool emptySsn = rnd.NextDouble() < 0.08;
            bool shortZip = rnd.NextDouble() < 0.30;

            return new Contact
            {
                Id = 1 + index,
                FullName = $"{first} {last}",
                Phone = emptyPhone ? string.Empty : $"555{rnd.Next(1000000, 9999999)}",
                Ssn = emptySsn ? string.Empty : $"{rnd.Next(100, 999)}{rnd.Next(10, 99)}{rnd.Next(1000, 9999)}",
                ZipPlus4 = shortZip
                    ? $"{rnd.Next(10000, 99999)}"
                    : $"{rnd.Next(10000, 99999)}{rnd.Next(1000, 9999)}",
                LicensePlate = emptyAccount
                    ? string.Empty
                    : $"{Letters[rnd.Next(26)]}{Letters[rnd.Next(26)]}{Letters[rnd.Next(26)]}{rnd.Next(100, 999)}",
                AccountNumber = emptyAccount
                    ? string.Empty
                    : $"{rnd.Next(1000, 9999)}{rnd.Next(1000, 9999)}{rnd.Next(1000, 9999)}{rnd.Next(1000, 9999)}",
                Birthday = new DateTime(birthYear, birthMonth, birthDay),
                LastSeen = DateTime.Today.AddDays(-lastSeenDays).AddSeconds(lastSeenSeconds),
                Logged = DateTime.Today.AddDays(-loggedDays).AddSeconds(loggedSeconds),
                Day = DateTime.Today.AddDays(-dayDays),
                Iso = DateTime.Today.AddDays(-isoDays).AddSeconds(isoSeconds),
                Balance = Math.Round((decimal)(rnd.NextDouble() * 9999.99), 2),
                Discount = Math.Round((decimal)(rnd.NextDouble() * 0.30), 4),
                Margin = Math.Round((decimal)((rnd.NextDouble() - 0.2) * 1500), 2),
                CallDuration = TimeSpan.FromSeconds(rnd.Next(0, 60 * 60 * 24))
            };
        }
    }
}
