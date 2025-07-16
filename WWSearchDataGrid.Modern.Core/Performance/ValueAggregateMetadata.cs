using System;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    public class ValueAggregateMetadata
    {
        public object Value { get; set; }
        public int Count { get; set; }
        public DateTime LastSeen { get; set; }
        public int HashCode { get; set; }
    }
}