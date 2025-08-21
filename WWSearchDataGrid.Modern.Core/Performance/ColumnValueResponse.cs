using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    public class ColumnValueResponse
    {
        public IEnumerable<object> Values { get; set; }
        public int TotalCount { get; set; }
        public bool HasMore { get; set; }
        public string ColumnKey { get; set; }
        public bool IsFromCache { get; set; }
    }
}