using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.DataTableSample
{
    public partial class DataTableSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private DataTable _vendorProducts = BuildSampleTable();

        [ObservableProperty]
        private DataTable _emptyTable = BuildSchemaOnly();

        // Wrap DataView in a ListCollectionView so SearchDataGrid can assign predicate filters.
        // BindingListCollectionView (DataView's default WPF view) only supports string filters.
        public ICollectionView VendorProductsView { get; }
        public ICollectionView EmptyTableView { get; }

        public DataTableSampleViewModel()
        {
            VendorProductsView = new ListCollectionView(VendorProducts.DefaultView);
            EmptyTableView = new ListCollectionView(EmptyTable.DefaultView);
        }

        private static DataTable BuildSampleTable()
        {
            var table = new DataTable("VendorProducts");

            // Typed columns covering the shapes 2.3 has to handle.
            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("VendorName", typeof(string));
            table.Columns.Add("PartNumber", typeof(string));
            table.Columns.Add("UnitCost", typeof(decimal));     // nullable values via DBNull
            table.Columns.Add("LastReceived", typeof(DateTime)); // nullable values via DBNull
            table.Columns.Add("InStock", typeof(bool));
            table.Columns.Add("Lead Time (Days)", typeof(int));  // intentional space in column name

            // Computed/expression column — should expose its CLR type via column descriptor.
            var extended = new DataColumn("ExtendedCost", typeof(decimal), "UnitCost * 100")
            {
                ReadOnly = true
            };
            table.Columns.Add(extended);

            // Variety of rows including DBNull values for cost and date.
            table.Rows.Add(1, "Acme Hardware",      "ACM-1001", 12.50m,    new DateTime(2026, 03, 14), true,  3);
            table.Rows.Add(2, "Acme Hardware",      "ACM-1002", 8.75m,     new DateTime(2026, 04, 02), true,  3);
            table.Rows.Add(3, "Bluebird Supply",    "BB-2210",  DBNull.Value, DBNull.Value,            false, 14);
            table.Rows.Add(4, "Bluebird Supply",    "BB-2211",  45.00m,    new DateTime(2026, 02, 28), true,  10);
            table.Rows.Add(5, "Coastal Distributors","CD-X09",  103.20m,   new DateTime(2026, 04, 18), true,  7);
            table.Rows.Add(6, "Coastal Distributors","CD-X10",  DBNull.Value, new DateTime(2026, 04, 22), true,  7);
            table.Rows.Add(7, "Delta Industrial",   "DI-501",   75.40m,    DBNull.Value,               false, 21);
            table.Rows.Add(8, "Delta Industrial",   "DI-502",   75.40m,    new DateTime(2026, 01, 11), true,  21);
            table.Rows.Add(9, "Evergreen Mfg",      "EG-77",    19.99m,    new DateTime(2026, 04, 25), true,  5);
            table.Rows.Add(10, "Evergreen Mfg",     "EG-78",    DBNull.Value, DBNull.Value,            false, 5);
            table.Rows.Add(11, "Foundry Source",    "FS-100",   320.00m,   new DateTime(2026, 03, 30), true,  30);
            table.Rows.Add(12, "Foundry Source",    "FS-101",   320.00m,   new DateTime(2026, 03, 30), true,  30);

            return table;
        }

        private static DataTable BuildSchemaOnly()
        {
            // Same schema, no rows — exercises type resolution against an empty source.
            var table = new DataTable("VendorProductsEmpty");
            table.Columns.Add("ProductId", typeof(int));
            table.Columns.Add("VendorName", typeof(string));
            table.Columns.Add("UnitCost", typeof(decimal));
            return table;
        }
    }
}
