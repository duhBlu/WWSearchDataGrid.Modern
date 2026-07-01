using System;

namespace WWControls.Wpf
{
    /// <summary>
    /// Bitmask describing which <see cref="Core.SummaryItemType"/> aggregates a column allows.
    /// Surfaced as <see cref="GridColumn.AllowedTotalSummaries"/> — the runtime summary picker
    /// (the total-summary cell's context menu) only offers the allowed functions, and an
    /// allowed-but-unsupported function (e.g. Sum on a string column) is still gated off by
    /// <see cref="Core.SummaryCalculator.IsTypeSupported"/>.
    /// </summary>
    [Flags]
    public enum AllowedSummaries
    {
        /// <summary>No aggregates may be picked at runtime.</summary>
        None = 0,

        /// <summary>Row count.</summary>
        Count = 1,

        /// <summary>Sum of numeric values.</summary>
        Sum = 2,

        /// <summary>Smallest value.</summary>
        Min = 4,

        /// <summary>Largest value.</summary>
        Max = 8,

        /// <summary>Arithmetic mean of numeric values.</summary>
        Average = 16,

        /// <summary>Every aggregate (the default).</summary>
        All = Count | Sum | Min | Max | Average
    }
}
