using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WWSearchDataGrid.Modern.Core.Caching
{
    /// <summary>
    /// Centralized cache manager for column values using WeakReference-based storage
    /// </summary>
    internal class ColumnValueCacheManager
    {
        private static readonly Lazy<ColumnValueCacheManager> _instance = 
            new Lazy<ColumnValueCacheManager>(() => new ColumnValueCacheManager());
        
        private readonly Dictionary<string, WeakReference<ColumnValueCache>> _cacheEntries = 
            new Dictionary<string, WeakReference<ColumnValueCache>>();
        
        private readonly object _lock = new object();
        
        public static ColumnValueCacheManager Instance => _instance.Value;
        
        private ColumnValueCacheManager() { }
        
        /// <summary>
        /// Gets or creates cached column values for the specified key
        /// </summary>
        /// <param name="cacheKey">Unique cache key for the column data</param>
        /// <param name="valuesProvider">Function to provide values if not cached</param>
        /// <returns>Read-only collection of column values</returns>
        public ReadOnlyColumnValues GetOrCreateColumnValues(string cacheKey, Func<IEnumerable<object>> valuesProvider)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentException("Cache key cannot be null or empty", nameof(cacheKey));

            if (valuesProvider == null)
                throw new ArgumentNullException(nameof(valuesProvider));

            lock (_lock)
            {
                if (_cacheEntries.TryGetValue(cacheKey, out var weakRef) &&
                    weakRef.TryGetTarget(out var existingCache))
                {
                    return new ReadOnlyColumnValues(existingCache);
                }

                var newCache = new ColumnValueCache(valuesProvider());
                _cacheEntries[cacheKey] = new WeakReference<ColumnValueCache>(newCache);

                return new ReadOnlyColumnValues(newCache);
            }
        }

        /// <summary>
        /// Attempts to add values to an existing cache entry incrementally
        /// </summary>
        /// <param name="cacheKey">The cache key to update</param>
        /// <param name="valuesToAdd">Values to add to the cache</param>
        /// <returns>True if successful, false if cache needs full refresh</returns>
        public bool TryAddValuesToCache(string cacheKey, IEnumerable<object> valuesToAdd)
        {
            if (string.IsNullOrEmpty(cacheKey) || valuesToAdd == null)
                return false;

            lock (_lock)
            {
                if (!_cacheEntries.TryGetValue(cacheKey, out var weakRef) ||
                    !weakRef.TryGetTarget(out var existingCache))
                {
                    return false; // Cache entry not found, needs full refresh
                }

                // Create new cache with added values
                var updatedCache = existingCache.AddValues(valuesToAdd);
                if (updatedCache == null)
                {
                    return false; // Addition failed, needs full refresh
                }

                // Update the cache entry
                _cacheEntries[cacheKey] = new WeakReference<ColumnValueCache>(updatedCache);
                return true;
            }
        }

        /// <summary>
        /// Attempts to remove values from an existing cache entry incrementally
        /// </summary>
        /// <param name="cacheKey">The cache key to update</param>
        /// <param name="valuesToRemove">Values to remove from the cache</param>
        /// <returns>True if successful, false if cache needs full refresh</returns>
        public bool TryRemoveValuesFromCache(string cacheKey, IEnumerable<object> valuesToRemove)
        {
            if (string.IsNullOrEmpty(cacheKey) || valuesToRemove == null)
                return false;

            lock (_lock)
            {
                // Try to get existing cache entry
                if (!_cacheEntries.TryGetValue(cacheKey, out var weakRef) ||
                    !weakRef.TryGetTarget(out var existingCache))
                {
                    return false; // Cache entry not found, needs full refresh
                }

                // Create new cache with removed values
                var updatedCache = existingCache.RemoveValues(valuesToRemove);
                if (updatedCache == null)
                {
                    return false; // Removal failed, needs full refresh
                }

                // Update the cache entry
                _cacheEntries[cacheKey] = new WeakReference<ColumnValueCache>(updatedCache);
                return true;
            }
        }
        
        /// <summary>
        /// Clears dead weak references and optionally all cache entries
        /// </summary>
        /// <param name="clearAll">If true, clears all entries; if false, only dead references</param>
        public void Cleanup(bool clearAll = false)
        {
            lock (_lock)
            {
                if (clearAll)
                {
                    _cacheEntries.Clear();
                    return;
                }
                
                // Remove dead weak references
                var keysToRemove = new List<string>();
                foreach (var kvp in _cacheEntries)
                {
                    if (!kvp.Value.TryGetTarget(out _))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    _cacheEntries.Remove(key);
                }
            }
        }
    }
    
    /// <summary>
    /// Internal cache storage for column values
    /// Uses efficient data structures for large datasets
    /// </summary>
    internal class ColumnValueCache
    {
        private readonly List<object> _values;
        private readonly HashSet<object> _uniqueValues;
        private readonly Dictionary<object, int> _valueCounts;
        private readonly bool _containsNullValues;
        private readonly ColumnDataType _dataType;

        public ColumnValueCache(IEnumerable<object> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            _uniqueValues = new HashSet<object>();
            _valueCounts = new Dictionary<object, int>();
            var normalizedValues = new List<object>();
            bool hasNulls = false;

            // Single pass for normalization, deduplication, null detection, and counting
            foreach (var value in values)
            {
                // Skip null and blank values entirely - users can use IsNull/IsNotNull search types instead
                if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                {
                    hasNulls = true;
                    continue; // Don't add to available values
                }

                // Add non-null values and count occurrences
                if (_uniqueValues.Add(value))
                {
                    normalizedValues.Add(value);
                    _valueCounts[value] = 1;
                }
                else
                {
                    _valueCounts[value]++;
                }
            }

            _containsNullValues = hasNulls;

            // Detect data type from values
            _dataType = DetectColumnDataType(normalizedValues);

            // Always sort values using type-aware comparison
            SortValuesByDataType(normalizedValues, _dataType);

            _values = normalizedValues;
        }

        public IReadOnlyList<object> Values => _values;
        public IReadOnlyCollection<object> UniqueValues => _uniqueValues;
        public IReadOnlyDictionary<object, int> ValueCounts => _valueCounts;
        public bool ContainsNullValues => _containsNullValues;
        public ColumnDataType DataType => _dataType;
        public int Count => _values.Count;

        /// <summary>
        /// Creates a new cache with additional values added incrementally
        /// </summary>
        /// <param name="valuesToAdd">Values to add to the cache</param>
        /// <returns>New cache instance with added values, or null if operation should fall back to full refresh</returns>
        public ColumnValueCache AddValues(IEnumerable<object> valuesToAdd)
        {
            if (valuesToAdd == null)
                return this;

            try
            {
                var additionalValues = new List<object>();
                var newUniqueValues = new HashSet<object>(_uniqueValues);
                var newValueCounts = new Dictionary<object, int>(_valueCounts);
                bool hasNewNulls = _containsNullValues;

                // Process new values
                foreach (var value in valuesToAdd)
                {
                    // Skip null and blank values - don't add to available values
                    if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                    {
                        hasNewNulls = true;
                        continue;
                    }

                    // Add non-null values and update counts
                    if (newUniqueValues.Add(value))
                    {
                        additionalValues.Add(value);
                        newValueCounts[value] = 1;
                    }
                    else
                    {
                        if (newValueCounts.TryGetValue(value, out var existingCount))
                        {
                            newValueCounts[value] = existingCount + 1;
                        }
                        else
                        {
                            newValueCounts[value] = 1;
                        }
                    }
                }

                // If no new values were added, return current cache
                if (additionalValues.Count == 0 && hasNewNulls == _containsNullValues)
                {
                    return this;
                }

                // Create combined values list
                var combinedValues = new List<object>(_values);
                combinedValues.AddRange(additionalValues);

                // Create new cache instance (which will detect type and sort automatically)
                return new ColumnValueCache(combinedValues, newUniqueValues, newValueCounts, hasNewNulls);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddValues: {ex.Message}");
                return null; // Signal that full refresh is needed
            }
        }

        /// <summary>
        /// Creates a new cache with specified values removed incrementally
        /// </summary>
        /// <param name="valuesToRemove">Values to remove from the cache</param>
        /// <returns>New cache instance with removed values, or null if operation should fall back to full refresh</returns>
        public ColumnValueCache RemoveValues(IEnumerable<object> valuesToRemove)
        {
            if (valuesToRemove == null)
                return this;

            try
            {
                var valuesToRemoveSet = new HashSet<object>();
                bool removingNulls = false;

                // Collect values to remove (skip nulls as they're not in the cache anyway)
                foreach (var value in valuesToRemove)
                {
                    if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                    {
                        removingNulls = true;
                        continue; // Nulls aren't in available values, so nothing to remove
                    }
                    valuesToRemoveSet.Add(value);
                }

                var filteredValues = _values.Where(v => !valuesToRemoveSet.Contains(v)).ToList();
                var filteredUniqueValues = new HashSet<object>(filteredValues);
                var filteredValueCounts = new Dictionary<object, int>();

                // Rebuild counts for remaining values
                foreach (var value in filteredValues)
                {
                    if (_valueCounts.TryGetValue(value, out var count))
                    {
                        filteredValueCounts[value] = count;
                    }
                }

                // Update null tracking
                bool hasNullsAfterRemoval = _containsNullValues && !removingNulls;

                // If no values were actually removed, return current cache
                if (filteredValues.Count == _values.Count)
                {
                    return this;
                }

                return new ColumnValueCache(filteredValues, filteredUniqueValues, filteredValueCounts, hasNullsAfterRemoval);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RemoveValues: {ex.Message}");
                return null; // Signal that full refresh is needed
            }
        }

        /// <summary>
        /// Private constructor for creating cache instances from incremental operations
        /// </summary>
        private ColumnValueCache(List<object> values, HashSet<object> uniqueValues, Dictionary<object, int> valueCounts, bool containsNulls)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _uniqueValues = uniqueValues ?? throw new ArgumentNullException(nameof(uniqueValues));
            _valueCounts = valueCounts ?? throw new ArgumentNullException(nameof(valueCounts));
            _containsNullValues = containsNulls;

            // Detect data type and sort
            _dataType = DetectColumnDataType(_values);
            SortValuesByDataType(_values, _dataType);
        }

        /// <summary>
        /// Sorts a list of values using type-aware comparison based on detected ColumnDataType
        /// </summary>
        private static void SortValuesByDataType(List<object> values, ColumnDataType dataType)
        {
            if (values == null || values.Count <= 1)
                return;

            // Use type-specific sorting based on ColumnDataType
            switch (dataType)
            {
                case ColumnDataType.Number:
                    values.Sort(CompareAsNumber);
                    break;
                case ColumnDataType.DateTime:
                    values.Sort(CompareAsDateTime);
                    break;
                case ColumnDataType.Boolean:
                    values.Sort(CompareAsBoolean);
                    break;
                case ColumnDataType.Enum:
                case ColumnDataType.String:
                default:
                    values.Sort(CompareAsString);
                    break;
            }
        }

        /// <summary>
        /// Detects the dominant ColumnDataType in a collection
        /// Samples up to 100 values for performance
        /// Uses the same logic as ReflectionHelper.DetermineColumnDataType
        /// </summary>
        private static ColumnDataType DetectColumnDataType(List<object> values)
        {
            if (values == null || values.Count == 0)
                return ColumnDataType.String;

            int sampleSize = Math.Min(100, values.Count);

            for (int i = 0; i < sampleSize; i++)
            {
                var value = values[i];
                if (value == null) continue;

                var type = value.GetType();

                // Check in priority order: DateTime > Boolean > Number > Enum > String
                if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                {
                    return ColumnDataType.DateTime;
                }

                if (type == typeof(bool))
                {
                    return ColumnDataType.Boolean;
                }

                if (IsNumericType(type))
                {
                    return ColumnDataType.Number;
                }

                if (type.IsEnum)
                {
                    return ColumnDataType.Enum;
                }
            }

            return ColumnDataType.String;
        }

        /// <summary>
        /// Checks if a type is numeric (same logic as ReflectionHelper)
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        /// <summary>
        /// Compares two values as numbers (handles both integers and decimals)
        /// Falls back to string comparison if conversion fails
        /// </summary>
        private static int CompareAsNumber(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Try to convert to double for universal numeric comparison
            if (TryConvertToDouble(x, out double xDouble) && TryConvertToDouble(y, out double yDouble))
            {
                return xDouble.CompareTo(yDouble);
            }

            // Fallback to string comparison
            return CompareAsString(x, y);
        }

        /// <summary>
        /// Compares two values as DateTimes
        /// </summary>
        private static int CompareAsDateTime(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Direct DateTime comparison
            if (x is DateTime xDateTime && y is DateTime yDateTime)
            {
                return xDateTime.CompareTo(yDateTime);
            }

            // DateTimeOffset comparison
            if (x is DateTimeOffset xDateTimeOffset && y is DateTimeOffset yDateTimeOffset)
            {
                return xDateTimeOffset.CompareTo(yDateTimeOffset);
            }

            // Fallback to string comparison
            return CompareAsString(x, y);
        }

        /// <summary>
        /// Compares two values as booleans (false < true)
        /// </summary>
        private static int CompareAsBoolean(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (x is bool xBool && y is bool yBool)
            {
                return xBool.CompareTo(yBool);
            }

            // Fallback to string comparison
            return CompareAsString(x, y);
        }

        /// <summary>
        /// Compares two values as strings using ordinal ignore case
        /// </summary>
        private static int CompareAsString(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var xStr = x.ToString();
            var yStr = y.ToString();

            return string.Compare(xStr, yStr, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tries to convert a value to double for decimal comparison
        /// </summary>
        private static bool TryConvertToDouble(object value, out double result)
        {
            result = 0;
            if (value == null) return false;

            var type = value.GetType();

            // Direct conversions (fastest)
            if (type == typeof(double)) { result = (double)value; return true; }
            if (type == typeof(float)) { result = (float)value; return true; }
            if (type == typeof(decimal)) { result = (double)(decimal)value; return true; }
            if (type == typeof(int)) { result = (int)value; return true; }
            if (type == typeof(long)) { result = (long)value; return true; }
            if (type == typeof(short)) { result = (short)value; return true; }
            if (type == typeof(byte)) { result = (byte)value; return true; }

            // Try parse for other types
            return double.TryParse(value.ToString(), out result);
        }
    }
    
    /// <summary>
    /// Read-only wrapper for cached column values
    /// Prevents modification of cached data while providing necessary interfaces
    /// </summary>
    internal class ReadOnlyColumnValues : IReadOnlyList<object>
    {
        private readonly ColumnValueCache _cache;

        internal ReadOnlyColumnValues(ColumnValueCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public object this[int index] => _cache.Values[index];
        public int Count => _cache.Count;
        public bool ContainsNullValues => _cache.ContainsNullValues;
        public IReadOnlyCollection<object> UniqueValues => _cache.UniqueValues;
        public IReadOnlyDictionary<object, int> ValueCounts => _cache.ValueCounts;
        public ColumnDataType DataType => _cache.DataType;

        public IEnumerator<object> GetEnumerator() => _cache.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Checks if the collection contains a specific value
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if value exists</returns>
        public bool Contains(object value)
        {
            // Null values are not in the available values collection
            if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                return false;

            return _cache.UniqueValues.Contains(value);
        }
    }
}