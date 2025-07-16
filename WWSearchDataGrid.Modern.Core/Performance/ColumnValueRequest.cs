using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    public class ColumnValueRequest
    {
        public string ColumnKey { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public string SearchText { get; set; }
        public bool IncludeNull { get; set; }
        public bool IncludeEmpty { get; set; }
        public IEnumerable<object> ExcludeValues { get; set; }
        public bool SortAscending { get; set; } = true;
        public bool GroupByFrequency { get; set; }
    }
}