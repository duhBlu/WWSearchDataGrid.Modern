namespace WWControls.Core
{
    /// <summary>
    /// The aggregate function a summary item computes over a column's rows
    /// (see <see cref="SummaryCalculator"/>).
    /// </summary>
    public enum SummaryItemType
    {
        /// <summary>Number of rows. The only aggregate that counts null values.</summary>
        Count,

        /// <summary>Sum of the column's non-null numeric values.</summary>
        Sum,

        /// <summary>Smallest non-null value (numeric, or any uniform IComparable type).</summary>
        Min,

        /// <summary>Largest non-null value (numeric, or any uniform IComparable type).</summary>
        Max,

        /// <summary>Arithmetic mean of the column's non-null numeric values.</summary>
        Average
    }
}
