using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.Core.Services
{
    /// <summary>
    /// Service for tracking and categorizing values with proper null/empty/whitespace handling
    /// </summary>
    public class ValueTracker
    {
        /// <summary>
        /// Tracks values and returns a dictionary of ValueMetadata to counts
        /// </summary>
        /// <param name="values">Collection of values to track</param>
        /// <returns>Dictionary mapping ValueMetadata to occurrence counts</returns>
        public Dictionary<ValueMetadata, int> TrackValues(IEnumerable<object> values)
        {
            if (values == null)
                return new Dictionary<ValueMetadata, int>();

            var result = new Dictionary<ValueMetadata, int>();
            
            foreach (var value in values)
            {
                var metadata = ValueMetadata.Create(value);
                
                if (result.ContainsKey(metadata))
                {
                    result[metadata]++;
                }
                else
                {
                    result[metadata] = 1;
                }
            }

            return result;
        }

        /// <summary>
        /// Categorizes a single value
        /// </summary>
        /// <param name="value">The value to categorize</param>
        /// <returns>The category of the value</returns>
        public ValueCategory CategorizeValue(object value)
        {
            if (value == null)
                return ValueCategory.Null;

            if (value is string stringValue)
            {
                if (stringValue.Length == 0)
                    return ValueCategory.Empty;

                if (string.IsNullOrWhiteSpace(stringValue))
                    return ValueCategory.Whitespace;

                return ValueCategory.Normal;
            }

            return ValueCategory.Normal;
        }

        /// <summary>
        /// Determines if two values are equivalent based on categorization rules
        /// </summary>
        /// <param name="value1">First value to compare</param>
        /// <param name="value2">Second value to compare</param>
        /// <param name="treatNullAndEmptyAsSame">Whether to treat null and empty as equivalent</param>
        /// <returns>True if values are equivalent</returns>
        public bool AreValuesEquivalent(object value1, object value2, bool treatNullAndEmptyAsSame = false)
        {
            var category1 = CategorizeValue(value1);
            var category2 = CategorizeValue(value2);

            // If treating null and empty as the same, group them together
            if (treatNullAndEmptyAsSame)
            {
                var isBlank1 = category1 == ValueCategory.Null || category1 == ValueCategory.Empty || category1 == ValueCategory.Whitespace;
                var isBlank2 = category2 == ValueCategory.Null || category2 == ValueCategory.Empty || category2 == ValueCategory.Whitespace;
                
                if (isBlank1 && isBlank2)
                    return true;
                
                if (isBlank1 || isBlank2)
                    return false;
            }
            else
            {
                // Strict categorization - must be exact category match
                if (category1 != category2)
                    return false;
                
                // For null values, they are equivalent if both are null
                if (category1 == ValueCategory.Null)
                    return true;
            }

            // For non-null values, compare the actual values
            return Equals(value1, value2);
        }

        /// <summary>
        /// Converts a traditional value-count dictionary to use ValueMetadata
        /// </summary>
        /// <param name="valueCounts">Dictionary of values to counts</param>
        /// <returns>Dictionary of ValueMetadata to counts</returns>
        public Dictionary<ValueMetadata, int> ConvertToMetadata(Dictionary<object, int> valueCounts)
        {
            if (valueCounts == null)
                return new Dictionary<ValueMetadata, int>();

            var result = new Dictionary<ValueMetadata, int>();
            
            foreach (var kvp in valueCounts)
            {
                var metadata = ValueMetadata.Create(kvp.Key);
                
                if (result.ContainsKey(metadata))
                {
                    result[metadata] += kvp.Value;
                }
                else
                {
                    result[metadata] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts ValueAggregateMetadata collection to ValueMetadata dictionary
        /// </summary>
        /// <param name="aggregateMetadata">Collection of aggregate metadata</param>
        /// <returns>Dictionary of ValueMetadata to counts</returns>
        public Dictionary<ValueMetadata, int> ConvertFromAggregateMetadata(IEnumerable<ValueAggregateMetadata> aggregateMetadata)
        {
            if (aggregateMetadata == null)
                return new Dictionary<ValueMetadata, int>();

            var result = new Dictionary<ValueMetadata, int>();
            
            foreach (var aggregate in aggregateMetadata)
            {
                var metadata = ValueMetadata.Create(aggregate.Value);
                
                if (result.ContainsKey(metadata))
                {
                    result[metadata] += aggregate.Count;
                }
                else
                {
                    result[metadata] = aggregate.Count;
                }
            }

            return result;
        }
    }
}