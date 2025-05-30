using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Utility class for reflection operations
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Gets property value from an object using a property path
        /// </summary>
        public static object GetPropValue(object obj, string propPath)
        {
            if (obj == null || string.IsNullOrEmpty(propPath))
                return null;

            // Handle nested properties
            var props = propPath.Split('.');
            var currentObj = obj;

            foreach (var prop in props)
            {
                if (currentObj == null)
                    return null;

                var propInfo = currentObj.GetType().GetProperty(prop);
                if (propInfo == null)
                    return null;

                currentObj = propInfo.GetValue(currentObj);
            }

            return currentObj;
        }

        /// <summary>
        /// Determines the data type of a column based on its values
        /// </summary>
        public static ColumnDataType DetermineColumnDataType(IEnumerable<object> values)
        {
            var nonNullValues = values.Where(v => v != null).Take(100).ToList();

            if (!nonNullValues.Any())
                return ColumnDataType.String;

            var firstValue = nonNullValues.First();

            if (firstValue is DateTime)
                return ColumnDataType.DateTime;

            if (firstValue is bool)
                return ColumnDataType.Boolean;

            if (IsNumericType(firstValue.GetType()))
                return ColumnDataType.Number;

            if (firstValue.GetType().IsEnum)
                return ColumnDataType.Enum;

            return ColumnDataType.String;
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}
