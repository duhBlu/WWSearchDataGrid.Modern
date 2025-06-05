using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Extension methods for string manipulation in search operations
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns empty string if object is null, otherwise returns string representation
        /// </summary>
        public static string ToStringEmptyIfNull(this object value)
        {
            return value?.ToString() ?? string.Empty;
        }
    }
}
