namespace WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups
{
    internal static class Cities
    {
        public static readonly (string City, string State, string Zip)[] CityStateZips =
        {
            ("Chicago", "IL", "60601"), ("Denver", "CO", "80202"), ("Austin", "TX", "78701"),
            ("Portland", "OR", "97201"), ("Nashville", "TN", "37201"), ("Charlotte", "NC", "28202"),
            ("Omaha", "NE", "68102"), ("Wichita", "KS", "67202"), ("Phoenix", "AZ", "85001"),
            ("Atlanta", "GA", "30301"), ("Indianapolis", "IN", "46201"), ("Columbus", "OH", "43201"),
            ("Milwaukee", "WI", "53201"), ("Kansas City", "MO", "64101"), ("Raleigh", "NC", "27601"),
            ("Tampa", "FL", "33601"), ("Minneapolis", "MN", "55401"), ("St. Louis", "MO", "63101")
        };

        public static readonly string[] Streets =
        {
            "123 Main St", "4567 Oak Avenue", "890 Industrial Pkwy", "2100 Elm Drive", "750 Cedar Lane",
            "315 Maple Court", "1820 Pine Ridge Rd", "422 Walnut Blvd", "9300 Birch Way", "601 Spruce Circle",
            "1455 Willow St", "280 Hickory Ave", "3200 Cypress Dr", "715 Aspen Terrace", "1100 Poplar Rd"
        };
    }
}
