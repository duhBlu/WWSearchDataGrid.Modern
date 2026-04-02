using System;
using CommunityToolkit.Mvvm.ComponentModel;
using WWSearchDataGrid.Modern.SampleApp.Models;

namespace WWSearchDataGrid.Modern.SampleApp
{
    /// <summary>
    /// Sample data item representing a sales order line.
    /// Designed to demonstrate all SearchDataGrid features with realistic, meaningful data.
    /// </summary>
    public class DataItem : ObservableObject
    {
        // Identity
        public string? OrderNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }

        // Product
        public string? ProductName { get; set; }
        public string? Category { get; set; }

        // Quantities & pricing
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal? Discount { get; set; }

        // Status & priority
        public OrderStatus Status { get; set; }
        public Priority Priority { get; set; }
        public bool IsRush { get; set; }
        public bool? IsApproved { get; set; }

        // Dates
        public DateTime OrderDate { get; set; }
        public DateTime? ShipDate { get; set; }

        // Notes
        public string? Notes { get; set; }
    }
}
