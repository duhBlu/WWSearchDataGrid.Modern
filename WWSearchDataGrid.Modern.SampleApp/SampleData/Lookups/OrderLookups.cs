namespace WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups
{
    internal static class OrderLookups
    {
        public static readonly string[] ProductLines =
            { "Eclipse", "Aspect", "Vistora", "Shiloh", "Nevara", "Test ProductLine 1" };

        public static readonly string?[] OrderStatuses =
            { "Submitted", "In Production", "Shipped", "Delivered", null };

        public static readonly string[] OrderLocations =
            { "Order Entry", "Production", "Shipping", "Complete" };

        public static readonly string[] OrderTypes =
            { "Standard", "Replacement Door/Drawer Front", "ASAP", "Sample", "Warranty" };

        public static readonly string[] ShipViaTypes =
            { "Deliver", "Parcel", "Will Call", "LTL Freight" };

        public static readonly string?[] JobNames =
        {
            "SMITH", "JOHNSON", "WILLIAMS-RES", "MURPHY", "OAK GROVE", "LAKEVIEW", "RIVERSIDE",
            "GARCIA", "ANDERSON", "MARTINEZ", "TAYLOR-REMODEL", "DAVIS", "WILSON", "BROWN-KITCHEN",
            "THOMAS", "JACKSON", "WHITE", "HARRIS", "MARTIN", "THOMPSON", "MOORE-BATH",
            "CLARK", "LEWIS", "ROBINSON", "WALKER", "PEREZ", "HALL", "YOUNG-CUSTOM", null, null
        };

        public static readonly string?[] SpecialInstructions =
        {
            null, null, null, null, null, null, null, null, null,
            "Customer requests AM delivery", "Rush order - priority handling",
            "Handle with care", "Leave at loading dock", "Call before delivery",
            "Hold for customer confirmation", "Deliver to job site, not shop address"
        };
    }
}
