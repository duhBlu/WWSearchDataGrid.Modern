using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Configuration for filter value display
    /// </summary>
    public class FilterValueConfiguration
    {
        public ColumnDataType DataType { get; set; }
        public FilterValueDisplayMode DisplayMode { get; set; }
        public string GroupByColumn { get; set; } // For grouped display
        public bool ShowItemCount { get; set; } = true;
    }
}
