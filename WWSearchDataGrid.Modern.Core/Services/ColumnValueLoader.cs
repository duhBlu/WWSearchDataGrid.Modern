using System;
using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core.Services
{
    /// <summary>
    /// Service for loading and managing column values and metadata
    /// </summary>
    public class ColumnValueLoader
    {
        /// <summary>
        /// Loads column data and determines data type
        /// </summary>
        /// <param name="header">Column header</param>
        /// <param name="values">Column values</param>
        /// <param name="displayValueMappings">Mappings for display values</param>
        /// <param name="bindingPath">The binding path for the column</param>
        /// <returns>Column data load result</returns>
        public ColumnDataLoadResult LoadColumnData(
            object header,
            HashSet<object> values,
            HashSet<Tuple<string, string>> displayValueMappings = null,
            string bindingPath = null)
        {
            var result = new ColumnDataLoadResult
            {
                ColumnName = header,
                ColumnValues = new HashSet<object>(values ?? new HashSet<object>()),
                DisplayValueMappings = displayValueMappings,
                BindingPath = bindingPath
            };

            // Auto-detect column data type
            if (values?.Any() == true)
            {
                result.ColumnDataType = ReflectionHelper.DetermineColumnDataType(values);
            }
            else
            {
                result.ColumnDataType = ColumnDataType.String;
            }

            return result;
        }

        /// <summary>
        /// Gets a property value from an object using a binding path
        /// </summary>
        /// <param name="item">The object to extract the value from</param>
        /// <param name="bindingPath">The property path (supports nested properties)</param>
        /// <returns>The property value or null if not found</returns>
        public object GetPropertyValue(object item, string bindingPath)
        {
            if (item == null || string.IsNullOrEmpty(bindingPath))
                return null;

            try
            {
                // Simple property access - split by dots for nested properties
                var properties = bindingPath.Split('.');
                object value = item;

                foreach (var prop in properties)
                {
                    var propInfo = value.GetType().GetProperty(prop);
                    if (propInfo == null)
                        return null;

                    value = propInfo.GetValue(value);
                    if (value == null)
                        return null;
                }

                return value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting property value for path '{bindingPath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts unique values from a collection of items for a specific binding path
        /// </summary>
        /// <param name="items">Collection of items to extract values from</param>
        /// <param name="bindingPath">Property path to extract values for</param>
        /// <returns>Set of unique values found</returns>
        public HashSet<object> ExtractColumnValues(IEnumerable<object> items, string bindingPath)
        {
            var values = new HashSet<object>();

            if (items == null || string.IsNullOrEmpty(bindingPath))
                return values;

            foreach (var item in items)
            {
                try
                {
                    var value = GetPropertyValue(item, bindingPath);
                    if (value != null)
                    {
                        values.Add(value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error extracting value from item: {ex.Message}");
                }
            }

            return values;
        }

        /// <summary>
        /// Extracts values with counts for statistical purposes
        /// </summary>
        /// <param name="items">Collection of items to extract values from</param>
        /// <param name="bindingPath">Property path to extract values for</param>
        /// <returns>Dictionary of values with their occurrence counts</returns>
        public Dictionary<object, int> ExtractColumnValuesWithCounts(IEnumerable<object> items, string bindingPath)
        {
            var valueCounts = new Dictionary<object, int>();

            if (items == null || string.IsNullOrEmpty(bindingPath))
                return valueCounts;

            foreach (var item in items)
            {
                try
                {
                    var value = GetPropertyValue(item, bindingPath);
                    if (value != null)
                    {
                        if (valueCounts.ContainsKey(value))
                        {
                            valueCounts[value]++;
                        }
                        else
                        {
                            valueCounts[value] = 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error extracting value from item: {ex.Message}");
                }
            }

            return valueCounts;
        }

        /// <summary>
        /// Validates that a binding path is valid for the given item type
        /// </summary>
        /// <param name="itemType">Type of items to validate against</param>
        /// <param name="bindingPath">Property path to validate</param>
        /// <returns>True if the binding path is valid</returns>
        public bool ValidateBindingPath(Type itemType, string bindingPath)
        {
            if (itemType == null || string.IsNullOrEmpty(bindingPath))
                return false;

            try
            {
                var properties = bindingPath.Split('.');
                Type currentType = itemType;

                foreach (var prop in properties)
                {
                    var propInfo = currentType.GetProperty(prop);
                    if (propInfo == null)
                        return false;

                    currentType = propInfo.PropertyType;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates grouped data for hierarchical filtering
        /// </summary>
        /// <param name="items">Source items</param>
        /// <param name="groupByPath">Property path to group by</param>
        /// <param name="valuePath">Property path for values within each group</param>
        /// <returns>Dictionary of grouped data</returns>
        public Dictionary<object, List<object>> CreateGroupedData(
            IEnumerable<object> items,
            string groupByPath,
            string valuePath)
        {
            var groupedData = new Dictionary<object, List<object>>();

            if (items == null || string.IsNullOrEmpty(groupByPath) || string.IsNullOrEmpty(valuePath))
                return groupedData;

            foreach (var item in items)
            {
                try
                {
                    var groupKey = GetPropertyValue(item, groupByPath);
                    var value = GetPropertyValue(item, valuePath);

                    if (!groupedData.ContainsKey(groupKey))
                    {
                        groupedData[groupKey] = new List<object>();
                    }

                    // Only add unique values to each group
                    if (!groupedData[groupKey].Contains(value))
                    {
                        groupedData[groupKey].Add(value);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating grouped data: {ex.Message}");
                }
            }

            return groupedData;
        }
    }

    /// <summary>
    /// Result of column data loading operation
    /// </summary>
    public class ColumnDataLoadResult
    {
        /// <summary>
        /// The column name/header
        /// </summary>
        public object ColumnName { get; set; }

        /// <summary>
        /// Set of unique column values
        /// </summary>
        public HashSet<object> ColumnValues { get; set; } = new HashSet<object>();

        /// <summary>
        /// Determined column data type
        /// </summary>
        public ColumnDataType ColumnDataType { get; set; } = ColumnDataType.String;

        /// <summary>
        /// Display value mappings
        /// </summary>
        public HashSet<Tuple<string, string>> DisplayValueMappings { get; set; }

        /// <summary>
        /// Binding path for the column
        /// </summary>
        public string BindingPath { get; set; }

        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Error message if loading failed
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}