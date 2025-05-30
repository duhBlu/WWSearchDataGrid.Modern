using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Predefined date‐range intervals for filtering.
    /// </summary>
    public enum DateInterval
    {
        /// <summary>Dates 1 year the current date</summary>
        PriorThisYear,

        /// <summary>Dates from the start of the current year.</summary>
        EarlierThisYear,

        /// <summary>Later in the current year (but before “BeyondThisYear”).</summary>
        LaterThisYear,

        /// <summary>Dates beyond the end of the current year.</summary>
        BeyondThisYear,

        /// <summary>Earlier in the current month.</summary>
        EarlierThisMonth,

        /// <summary>Later in the current month.</summary>
        LaterThisMonth,

        /// <summary>Earlier in the current week.</summary>
        EarlierThisWeek,

        /// <summary>Later in the current week.</summary>
        LaterThisWeek,

        /// <summary>Last week (the full seven days before this week).</summary>
        LastWeek,

        /// <summary>The next calendar week.</summary>
        NextWeek,

        /// <summary>Yesterday’s date.</summary>
        Yesterday,

        /// <summary>Today’s date.</summary>
        Today,

        /// <summary>Tomorrow’s date.</summary>
        Tomorrow
    }

}
