using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        /// Gets the property type for a given object and property path
        /// </summary>
        public static Type GetPropertyType(object obj, string propertyPath)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(propertyPath))
                    return null;

                var properties = propertyPath.Split('.');
                var currentType = obj.GetType();

                foreach (var propertyName in properties)
                {
                    var propertyInfo = currentType.GetProperty(propertyName);
                    if (propertyInfo == null)
                        return null;

                    currentType = propertyInfo.PropertyType;
                }

                return currentType;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting property type: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets property info from a type using a property path (for type inspection without instances)
        /// </summary>
        public static PropertyInfo GetPropertyInfoFromType(Type type, string propertyPath)
        {
            try
            {
                if (type == null || string.IsNullOrEmpty(propertyPath))
                    return null;

                var properties = propertyPath.Split('.');
                var currentType = type;

                foreach (var propertyName in properties)
                {
                    var propertyInfo = currentType.GetProperty(propertyName);
                    if (propertyInfo == null)
                        return null;

                    if (propertyName == properties.Last())
                        return propertyInfo; // Return the final property

                    currentType = propertyInfo.PropertyType;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting property info from type: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the property type from a Type using a property path (without needing an instance)
        /// </summary>
        public static Type GetPropertyTypeFromType(Type type, string propertyPath)
        {
            var propertyInfo = GetPropertyInfoFromType(type, propertyPath);
            return propertyInfo?.PropertyType;
        }

        /// <summary>
        /// Determines if a property path points to a boolean property by examining a Type
        /// </summary>
        public static bool IsBooleanProperty(Type type, string propertyPath)
        {
            var propertyType = GetPropertyTypeFromType(type, propertyPath);
            return propertyType == typeof(bool) || propertyType == typeof(bool?);
        }

        /// <summary>
        /// Determines the data type of a column based on its values
        /// </summary>
        public static ColumnDataType DetermineColumnDataType(IEnumerable<object> values)
        {
            var nonNullValues = values.Where(v => v != null).Skip(1).Take(100).ToList(); // skip 1 incase of the null value display string at the front

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
