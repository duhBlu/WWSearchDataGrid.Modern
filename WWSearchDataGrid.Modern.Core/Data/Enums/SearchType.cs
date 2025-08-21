using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Defines the available search operation types
    /// </summary>
    public enum SearchType
    {
        // Basic value comparisons
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqualTo,
        LessThan,
        LessThanOrEqualTo,
        Between,
        NotBetween,

        // String/text matching
        Contains,
        DoesNotContain,
        StartsWith,
        EndsWith,
        IsLike,
        IsNotLike,

        // Set membership
        IsAnyOf,
        IsNoneOf,
        IsOnAnyOfDates,

        // Null/empty checks
        IsNull,
        IsNotNull,
        IsBlank,
        IsNotBlank,

        // Date-specific
        Today,
        Yesterday,
        BetweenDates,
        DateInterval,

        // Statistical
        TopN,
        BottomN,
        AboveAverage,
        BelowAverage,

        // Uniqueness
        Unique,
        Duplicate,
    }
}
