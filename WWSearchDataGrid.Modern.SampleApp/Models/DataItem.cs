using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.SampleApp
{
    /// <summary>
    /// Model representing various data types for testing DataGrid filtering
    /// </summary>
    public class DataItem : ObservableObject
    {
        public bool BoolValue { get; set; }
        public bool? NullableBoolValue { get; set; }
        public int IntValue { get; set; }
        public int? NullableIntValue { get; set; }
        public long LongValue { get; set; }
        public float FloatValue { get; set; }
        public float? NullableFloatValue { get; set; }
        public double DoubleValue { get; set; }
        public double? NullableDoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public decimal? NullableDecimalValue { get; set; }
        public string StringValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTime? NullableDateTimeValue { get; set; }
        public int ComboBoxValueId { get; set; }
        public string SelectedComboBoxStringValue { get; set; }
        public List<Tuple<string, string>> PropertyValues { get; set; }
        public Dictionary<string, object> PropertyDictionary { get; set; }

        // --- new "realistic" fields ---
        /// <summary>
        /// A product or item name (e.g. "Laptop", "Headphones", etc.)
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// A broad category (so rows naturally duplicate every few items)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Unit price in some currency
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// ISO currency code (USD, EUR, GBP, etc.)
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Geographic region (duplicates every few rows)
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Quantity ordered (1–100)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Order date somewhere in the last 12 months
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Order status (Pending, Shipped, Delivered, Cancelled)
        /// </summary>
        public string Status { get; set; }
    }
}