using System;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    public class ValueAggregateMetadata
    {
        public object Value { get; set; }
        public int Count { get; set; }
        public DateTime LastSeen { get; set; }
        public int HashCode { get; set; }
        
        /// <summary>
        /// Category of the value (null, empty, whitespace, or normal)
        /// </summary>
        public ValueCategory Category { get; set; }
        
        /// <summary>
        /// Quick check for null values
        /// </summary>
        public bool IsNull => Category == ValueCategory.Null;
        
        /// <summary>
        /// Quick check for blank values (null, empty, or whitespace)
        /// </summary>
        public bool IsBlank => Category == ValueCategory.Null || Category == ValueCategory.Empty || Category == ValueCategory.Whitespace;
        
        /// <summary>
        /// Display text for the value
        /// </summary>
        public string DisplayText { get; set; }
        
        /// <summary>
        /// Constructor that properly categorizes the value
        /// </summary>
        public ValueAggregateMetadata()
        {
            // Default constructor
        }
        
        /// <summary>
        /// Constructor that properly categorizes the value
        /// </summary>
        /// <param name="value">The value to categorize</param>
        /// <param name="count">Initial count</param>
        public ValueAggregateMetadata(object value, int count = 1)
        {
            Value = value;
            Count = count;
            LastSeen = DateTime.Now;
            HashCode = value?.GetHashCode() ?? 0;
            
            // Categorize the value
            var metadata = ValueMetadata.Create(value);
            Category = metadata.Category;
            DisplayText = metadata.DisplayText;
        }
    }
}