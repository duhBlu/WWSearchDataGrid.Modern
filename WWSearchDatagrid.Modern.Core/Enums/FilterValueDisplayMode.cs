using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Display modes for filter values tab
    /// </summary>
    public enum FilterValueDisplayMode
    {
        FlatList,           // Simple list with checkboxes
        GroupedByColumn,    // TreeView grouped by another column
        GroupedByYear,      // TreeView grouped by year (for dates)
        GroupedByMonth,     // TreeView grouped by month (for dates)
        Custom              // Custom grouping logic
    }
}
