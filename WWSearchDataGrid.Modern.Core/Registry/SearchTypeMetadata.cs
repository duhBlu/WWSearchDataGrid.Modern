using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Metadata for a filter type including which data types it supports
    /// </summary>
    internal class SearchTypeMetadata
    {
        public SearchType SearchType { get; set; }
        public string DisplayName { get; set; }
        public FilterInputTemplate InputTemplate { get; set; }
        public HashSet<ColumnDataType> SupportedDataTypes { get; set; }
        public bool RequiresCollection { get; set; }

        public SearchTypeMetadata(SearchType searchType, string displayName,
            FilterInputTemplate inputTemplate, params ColumnDataType[] supportedTypes)
        {
            SearchType = searchType;
            DisplayName = displayName;
            InputTemplate = inputTemplate;
            SupportedDataTypes = new HashSet<ColumnDataType>(supportedTypes);
            RequiresCollection = false;
        }
    }
}
