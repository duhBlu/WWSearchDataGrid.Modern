using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWSearchDataGrid.Modern.SampleApp.Models
{
    /// <summary>
    /// Sample data item modeled after the CabinetOrder.OrderHeader stored procedure.
    /// Loaded from embedded anonymized JSON data.
    /// </summary>
    public class OrderItem : ObservableObject
    {
        // Identity
        public int OrderHeaderId { get; set; }
        public int OrderNumber { get; set; }
        public string? ProductionNumber { get; set; }

        // Status
        public string? OrderStatusName { get; set; }
        public string? OrderLocationName { get; set; }
        public string? OrderTypeName { get; set; }
        public bool OrderCancelled { get; set; }
        public bool Submitted { get; set; }

        // Dates
        public DateTime? OrderDate { get; set; }
        public DateTime? ScheduleDate { get; set; }

        // People
        public int? CreateEmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? SalesSubRepName { get; set; }

        // Customer / Dealer
        public string? CustomerName { get; set; }
        public int? DealerNumber { get; set; }
        public string? DealerPO { get; set; }
        public string? DealerName { get; set; }
        public string? JobName { get; set; }

        // Product
        public string? ProductLineName { get; set; }

        // Pricing
        public int? OrderItemsTotalQuantity { get; set; }
        public decimal? OrderItemsTotalPrice { get; set; }
        public decimal? AmountDue { get; set; }
        public decimal? DiscountPercent { get; set; }

        // Shipping
        public string? Address1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? ShipViaTypeName { get; set; }
        public int? LoadNumber { get; set; }
        public int? DropNumber { get; set; }

        // Notes
        public string? SpecialInstructionsText { get; set; }

        // Header Options (XML - reserved for future custom column filter example)
        public string? HeaderOptionsXml { get; set; }
    }
}
