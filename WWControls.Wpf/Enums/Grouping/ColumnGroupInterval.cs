namespace WWControls.Wpf
{
    /// <summary>
    /// How a grouped column buckets its rows. <see cref="Default"/> and <see cref="Value"/> group by
    /// the whole value (WPF's <see cref="System.Windows.Data.PropertyGroupDescription"/>); the other
    /// modes derive a bucket key from each value and are backed by
    /// <see cref="IntervalGroupDescription"/>. The date modes require a
    /// <see cref="System.DateTime"/> / <see cref="System.DateTimeOffset"/> column (enforced by
    /// <see cref="GridColumn.Validate"/>).
    /// </summary>
    public enum ColumnGroupInterval
    {
        /// <summary>Group by the whole value — the default, identical to <see cref="Value"/>.</summary>
        Default,

        /// <summary>Group by the whole value (one group per distinct value).</summary>
        Value,

        /// <summary>Group by the first character of the value's text, upper-cased.</summary>
        Alphabetical,

        /// <summary>Group by calendar year (e.g. <c>2026</c>).</summary>
        DateYear,

        /// <summary>Group by calendar month within year (e.g. <c>June 2026</c>).</summary>
        DateMonth,

        /// <summary>Group by calendar day (date only, time component dropped).</summary>
        DateDay,

        /// <summary>Group by day of week (e.g. <c>Tuesday</c>).</summary>
        DateWeekDay,

        /// <summary>Group into relative ranges (Today / Yesterday / This Week / This Month / This Year / Older).</summary>
        DateRange,
    }
}
