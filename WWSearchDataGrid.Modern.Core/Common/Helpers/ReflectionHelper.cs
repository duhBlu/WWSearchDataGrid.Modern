using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
        // Cache for resolved property chains: (Type, propertyPath) -> PropertyDescriptor[].
        // Using PropertyDescriptor (not PropertyInfo) lets the same code path serve POCOs,
        // ITypedList sources like DataRowView (DataTable rows), and ICustomTypeDescriptor types.
        private static readonly ConcurrentDictionary<(Type, string), PropertyDescriptor[]> _propertyChainCache
            = new ConcurrentDictionary<(Type, string), PropertyDescriptor[]>();

        /// <summary>
        /// Gets property value from an object using a property path.
        /// Property chains are cached for performance when filtering large datasets.
        /// Uses <see cref="TypeDescriptor"/> so it handles POCOs, DataRowView (via ITypedList),
        /// and ICustomTypeDescriptor uniformly.
        /// </summary>
        public static object GetPropValue(object obj, string propPath)
        {
            if (obj == null || string.IsNullOrEmpty(propPath))
                return null;

            var type = obj.GetType();
            // The cache is keyed by Type, but ITypedList sources resolve descriptors per-instance.
            // Pass `obj` to the resolver so the first lookup for a given type uses real metadata
            // (e.g. the DataTable's columns); subsequent items of the same type reuse the chain.
            var chain = _propertyChainCache.GetOrAdd((type, propPath), key => ResolvePropertyChain(obj, key.Item2));

            if (chain == null)
                return null;

            object currentObj = obj;
            foreach (var pd in chain)
            {
                if (currentObj == null)
                    return null;

                currentObj = pd.GetValue(currentObj);
            }

            // Normalize DBNull → null so downstream evaluators (IsNull/IsNotNull, equality)
            // see the same shape regardless of whether the source is a POCO or a DataRowView.
            if (currentObj == DBNull.Value)
                return null;

            return currentObj;
        }

        /// <summary>
        /// Resolves a property path to an array of <see cref="PropertyDescriptor"/> objects.
        /// Walks the path against a sample instance so ITypedList sources (DataRowView) resolve
        /// correctly. Returns null if any segment in the path is invalid.
        /// </summary>
        private static PropertyDescriptor[] ResolvePropertyChain(object instance, string propPath)
        {
            var props = propPath.Split('.');
            var chain = new PropertyDescriptor[props.Length];
            object current = instance;

            for (int i = 0; i < props.Length; i++)
            {
                if (current == null)
                    return null;

                var pds = TypeDescriptor.GetProperties(current);
                var pd = pds[props[i]];
                if (pd == null)
                    return null;

                chain[i] = pd;
                if (i < props.Length - 1)
                    current = pd.GetValue(current);
            }

            return chain;
        }

        /// <summary>
        /// Sets property value on an object using a property path. Uses <see cref="TypeDescriptor"/>
        /// so it works on POCOs and DataRowView (DataTable rows) alike.
        /// </summary>
        public static void SetPropValue(object obj, string propPath, object value)
        {
            if (obj == null || string.IsNullOrEmpty(propPath))
                return;

            var props = propPath.Split('.');
            object currentObj = obj;

            // Navigate to the parent object for nested properties.
            for (int i = 0; i < props.Length - 1; i++)
            {
                if (currentObj == null) return;
                var pd = TypeDescriptor.GetProperties(currentObj)[props[i]];
                if (pd == null) return;
                currentObj = pd.GetValue(currentObj);
            }

            if (currentObj == null) return;

            var finalPd = TypeDescriptor.GetProperties(currentObj)[props[props.Length - 1]];
            if (finalPd != null && !finalPd.IsReadOnly)
            {
                // Round-trip null back to DBNull when the descriptor sits on a DataRowView column,
                // since DataTable cells reject CLR null.
                finalPd.SetValue(currentObj, value ?? (currentObj is System.Data.DataRowView ? DBNull.Value : null));
            }
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
