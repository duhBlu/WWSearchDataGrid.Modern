using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Extension methods for the cache system
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// Converts value counts dictionary to be null-safe
        /// </summary>
        public static Dictionary<object, int> ToNullSafeDictionary(this Dictionary<object, int> source)
        {
            if (source == null)
                return new Dictionary<object, int>();

            var result = new Dictionary<object, int>();

            foreach (var kvp in source)
            {
                // Skip any special null key objects and use actual null
                if (kvp.Key?.GetType().Name == "NullValueKey")
                {
                    result[null] = kvp.Value;
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }
}
