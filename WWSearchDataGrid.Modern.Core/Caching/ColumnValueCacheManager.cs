using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WWSearchDataGrid.Modern.Core.Caching
{
    /// <summary>
    /// Centralized cache manager for column values using WeakReference-based storage
    /// Eliminates data duplication and enables proper memory cleanup for large datasets
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
                // Try to get existing cache entry
                if (_cacheEntries.TryGetValue(cacheKey, out var weakRef) &&
                    weakRef.TryGetTarget(out var existingCache))
                {
                    return new ReadOnlyColumnValues(existingCache);
                }

                // Create new cache entry
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
                // Try to get existing cache entry
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
        private readonly bool _containsNullValues;
        
        public ColumnValueCache(IEnumerable<object> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            
            _uniqueValues = new HashSet<object>();
            var normalizedValues = new List<object>();
            bool hasNulls = false;
            
            // Single pass for normalization, deduplication, and null detection
            foreach (var value in values)
            {
                // Check for null values
                if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                {
                    hasNulls = true;
                }
                
                var normalizedValue = NormalizeValue(value);
                if (_uniqueValues.Add(normalizedValue))
                {
                    normalizedValues.Add(normalizedValue);
                }
            }
            
            _containsNullValues = hasNulls;
            
            // Sort if collection is reasonable size
            if (normalizedValues.Count <= 500000)
            {
                normalizedValues.Sort((x, y) => CompareValues(x, y));
            }
            
            _values = normalizedValues;
        }
        
        public IReadOnlyList<object> Values => _values;
        public IReadOnlyCollection<object> UniqueValues => _uniqueValues;
        public bool ContainsNullValues => _containsNullValues;
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
                bool hasNewNulls = _containsNullValues;

                // Process new values
                foreach (var value in valuesToAdd)
                {
                    // Check for null values
                    if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                    {
                        hasNewNulls = true;
                    }

                    var normalizedValue = NormalizeValue(value);
                    if (newUniqueValues.Add(normalizedValue))
                    {
                        additionalValues.Add(normalizedValue);
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

                // Re-sort if collection is reasonable size (same logic as constructor)
                if (combinedValues.Count <= 500000)
                {
                    combinedValues.Sort((x, y) => CompareValues(x, y));
                }

                // Create new cache instance
                return new ColumnValueCache(combinedValues, newUniqueValues, hasNewNulls);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddValues: {ex.Message}");
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
                var normalizedValuesToRemove = new HashSet<object>();
                bool removingNulls = false;

                // Normalize values to remove
                foreach (var value in valuesToRemove)
                {
                    if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                    {
                        removingNulls = true;
                    }
                    normalizedValuesToRemove.Add(NormalizeValue(value));
                }

                // Create filtered values
                var filteredValues = _values.Where(v => !normalizedValuesToRemove.Contains(v)).ToList();
                var filteredUniqueValues = new HashSet<object>(filteredValues);

                // Determine if nulls still exist after removal
                bool hasNullsAfterRemoval = _containsNullValues && !removingNulls;
                if (removingNulls && _containsNullValues)
                {
                    // Need to check if any nulls remain after removal
                    hasNullsAfterRemoval = filteredUniqueValues.Contains(NullDisplayValue.Instance);
                }

                // If no values were actually removed, return current cache
                if (filteredValues.Count == _values.Count)
                {
                    return this;
                }

                // Create new cache instance
                return new ColumnValueCache(filteredValues, filteredUniqueValues, hasNullsAfterRemoval);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RemoveValues: {ex.Message}");
                return null; // Signal that full refresh is needed
            }
        }

        /// <summary>
        /// Private constructor for creating cache instances from incremental operations
        /// </summary>
        private ColumnValueCache(List<object> values, HashSet<object> uniqueValues, bool containsNulls)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
            _uniqueValues = uniqueValues ?? throw new ArgumentNullException(nameof(uniqueValues));
            _containsNullValues = containsNulls;
        }
        
        private static object NormalizeValue(object value)
        {
            if (value == null) return NullDisplayValue.Instance;
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                return NullDisplayValue.Instance;
            return value;
        }
        
        private static int CompareValues(object x, object y)
        {
            var xStr = x?.ToString() ?? string.Empty;
            var yStr = y?.ToString() ?? string.Empty;
            return string.Compare(xStr, yStr, StringComparison.OrdinalIgnoreCase);
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
        
        public IEnumerator<object> GetEnumerator() => _cache.Values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        
        /// <summary>
        /// Checks if the collection contains a specific value
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True if value exists</returns>
        public bool Contains(object value)
        {
            var normalizedValue = value == null ? NullDisplayValue.Instance :
                (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)) ? NullDisplayValue.Instance : value;
            
            return _cache.UniqueValues.Contains(normalizedValue);
        }
    }
}