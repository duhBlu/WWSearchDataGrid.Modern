using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Helper class for handling null values in dictionaries and counts
    /// </summary>
    public static class NullSafeValueHelper
    {
        private const string NullDisplayValue = "(blank)";

        /// <summary>
        /// Creates a null-safe dictionary for value counts
        /// </summary>
        public static Dictionary<object, int> CreateValueCountDictionary(IEnumerable<object> values)
        {
            var counts = new Dictionary<object, int>(NullSafeComparer.Instance);

            foreach (var value in values)
            {
                if (counts.ContainsKey(value))
                    counts[value]++;
                else
                    counts[value] = 1;
            }

            return counts;
        }

        /// <summary>
        /// Gets the count for a value, handling nulls safely
        /// </summary>
        public static int GetValueCount(object value, Dictionary<object, int> valueCounts)
        {
            if (valueCounts == null)
                return 1;

            // If the dictionary was created with NullSafeComparer, it will handle nulls
            if (valueCounts.ContainsKey(value))
                return valueCounts[value];

            return 1;
        }

        /// <summary>
        /// Gets display value for an object, handling nulls
        /// </summary>
        public static string GetDisplayValue(object value)
        {
            return value?.ToString() ?? NullDisplayValue;
        }

        /// <summary>
        /// Comparer that handles null values
        /// </summary>
        private class NullSafeComparer : IEqualityComparer<object>
        {
            public static readonly NullSafeComparer Instance = new NullSafeComparer();

            public new bool Equals(object x, object y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;
                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}
