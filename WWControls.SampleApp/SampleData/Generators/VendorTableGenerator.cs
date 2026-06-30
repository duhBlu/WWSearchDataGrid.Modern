using System;
using System.Data;
using WWControls.SampleApp.SampleData.Lookups;

namespace WWControls.SampleApp.SampleData.Generators
{
    public static class VendorTableGenerator
    {
        public static DataTable Create(int rowCount, int? seed = null)
        {
            var rnd = seed.HasValue ? new Random(seed.Value) : new Random();
            var table = new DataTable("VendorProducts");

            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("VendorName", typeof(string));
            table.Columns.Add("PartNumber", typeof(string));
            table.Columns.Add("UnitCost", typeof(decimal));
            table.Columns.Add("LastReceived", typeof(DateTime));
            table.Columns.Add("InStock", typeof(bool));
            table.Columns.Add("Lead Time (Days)", typeof(int));

            var extended = new DataColumn("ExtendedCost", typeof(decimal), "UnitCost * 0.1")
            {
                ReadOnly = true
            };
            table.Columns.Add(extended);

            for (int i = 0; i < rowCount; i++)
            {
                var vendor = Names.VendorNames[rnd.Next(Names.VendorNames.Length)];
                var prefix = new string(vendor[0], 1) + (vendor.Length > 1 ? vendor[1].ToString() : "");
                object unitCost = rnd.NextDouble() < 0.15
                    ? DBNull.Value
                    : Math.Round((decimal)(rnd.NextDouble() * 320) + 5m, 2);
                object lastReceived = rnd.NextDouble() < 0.20
                    ? DBNull.Value
                    : DateTime.Today.AddDays(-rnd.Next(1, 120));

                table.Rows.Add(
                    1 + i,
                    vendor,
                    $"{prefix.ToUpperInvariant()}-{rnd.Next(100, 9999)}",
                    unitCost,
                    lastReceived,
                    rnd.NextDouble() > 0.25,
                    rnd.Next(1, 35));
            }

            return table;
        }
    }
}
