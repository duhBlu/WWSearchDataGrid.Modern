using System;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;

namespace WWSearchDataGrid.Modern.SampleApp.SampleData.Generators
{
    public static class CustomerGenerator
    {
        public static Customer Create(Random rnd, int index)
        {
            var first = Names.FirstNames[rnd.Next(Names.FirstNames.Length)];
            var last = Names.LastNames[rnd.Next(Names.LastNames.Length)];
            var joinedDaysAgo = rnd.Next(30, 365 * 3);
            var creditLimit = rnd.NextDouble() < 0.15
                ? 0m
                : Math.Round(((decimal)(rnd.NextDouble() * 19500) + 500m) / 100m) * 100m;

            return new Customer
            {
                InternalId = 100000 + index,
                Id = 1 + index,
                FirstName = first,
                LastName = last,
                AccountNumber = $"ACME-{(1 + index):D4}",
                Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@example.com",
                JoinedOn = DateTime.Today.AddDays(-joinedDaysAgo),
                IsActive = rnd.NextDouble() > 0.18,
                CreditLimit = creditLimit,
                InternalNotes = rnd.NextDouble() < 0.15 ? "VIP" : null
            };
        }
    }
}
