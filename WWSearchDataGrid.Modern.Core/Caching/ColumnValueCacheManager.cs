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
        
        /// <summary>
        /// Gets the singleton instance
        /// </summary>
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
        
        /// <summary>
        /// Gets cache statistics for monitoring
        /// </summary>
        /// <returns>Cache statistics</returns>
        public CacheStatistics GetStatistics()
        {
            lock (_lock)
            {
                int totalEntries = _cacheEntries.Count;
                int aliveEntries = 0;
                
                foreach (var weakRef in _cacheEntries.Values)
                {
                    if (weakRef.TryGetTarget(out _))
                    {
                        aliveEntries++;
                    }
                }
                
                return new CacheStatistics
                {
                    TotalEntries = totalEntries,
                    AliveEntries = aliveEntries,
                    DeadEntries = totalEntries - aliveEntries
                };
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
            if (normalizedValues.Count <= 100000)
            {
                normalizedValues.Sort((x, y) => CompareValues(x, y));
            }
            
            _values = normalizedValues;
        }
        
        public IReadOnlyList<object> Values => _values;
        public IReadOnlyCollection<object> UniqueValues => _uniqueValues;
        public bool ContainsNullValues => _containsNullValues;
        public int Count => _values.Count;
        
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
    
    /// <summary>
    /// Special class to represent null values with proper display text
    /// </summary>
    internal class NullDisplayValue
    {
        public static readonly NullDisplayValue Instance = new NullDisplayValue();
        
        private NullDisplayValue() { }
        
        public override string ToString() => "(null)";
        
        public override bool Equals(object obj) => obj is NullDisplayValue;
        
        public override int GetHashCode() => 0; // All null display values are equal
    }
    
    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    internal class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int AliveEntries { get; set; }
        public int DeadEntries { get; set; }
        public double MemoryEfficiency => TotalEntries > 0 ? (double)AliveEntries / TotalEntries : 0.0;
    }
}