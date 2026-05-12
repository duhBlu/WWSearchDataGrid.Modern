using System;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;

namespace WWSearchDataGrid.Modern.SampleApp.SampleData.Generators
{
    public static class OrderGenerator
    {
        public static OrderItem Create(Random rnd, int index)
        {
            var baseDate = DateTime.Today.AddYears(-1);
            var orderDate = baseDate.AddDays(rnd.Next(0, 365))
                                    .AddHours(rnd.Next(7, 18))
                                    .AddMinutes(rnd.Next(0, 60));

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
            if (submitted && rnd.NextDouble() < 0.4)
                scheduleDate = orderDate.AddDays(rnd.Next(7, 45));

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
    }
}
