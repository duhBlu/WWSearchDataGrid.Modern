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

        public static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        public static bool IsNumericValue(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        /// <summary>
        /// Determines if a Type allows null values
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type can be null, false otherwise</returns>
        public static bool IsNullableType(Type type)
        {
            if (type == null)
                return false;

            // Reference types (including string) are always nullable
            if (!type.IsValueType)
                return true;

            // Check if it's a nullable value type (Nullable<T>)
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Determines if a Type allows null values by examining column values
        /// </summary>
        /// <param name="columnValues">Column values to analyze for null presence and actual types</param>
        /// <returns>True if the type can be null, false otherwise</returns>
        public static bool IsNullableFromValues(IEnumerable<object> columnValues)
        {
            if (columnValues == null)
                return true;

            var valuesList = columnValues.ToList();
            
            // If we have null values in the data, the type must be nullable
            if (valuesList.Any(v => v == null))
                return true;

            // If we have no values, assume nullable
            if (!valuesList.Any())
                return true;

            // Get the actual type from the first non-null value
            var firstValue = valuesList.FirstOrDefault(v => v != null);
            if (firstValue == null)
                return true;

            return IsNullableType(firstValue.GetType());
        }

        /// <summary>
        /// Determines if a value is comparable (implements IComparable)
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value is comparable</returns>
        public static bool IsComparableValue(object value)
        {
            return value is IComparable;
        }
    }
}
