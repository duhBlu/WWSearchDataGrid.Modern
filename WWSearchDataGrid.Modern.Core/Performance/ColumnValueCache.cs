﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Cache for column values
    /// </summary>
    public class ColumnValueCache : IDisposable
    {
        #region Singleton

        private static readonly Lazy<ColumnValueCache> _instance =
            new Lazy<ColumnValueCache>(() => new ColumnValueCache());

        public static ColumnValueCache Instance => _instance.Value;

        private ColumnValueCache() { }

        #endregion

        #region Fields

        private readonly ConcurrentDictionary<string, ColumnValueMetadata> _columnMetadata =
            new ConcurrentDictionary<string, ColumnValueMetadata>();

        private readonly ConcurrentDictionary<string, FilterValueViewModel> _filterViewModelCache =
            new ConcurrentDictionary<string, FilterValueViewModel>();


        private readonly ConcurrentDictionary<string, BulkOperationTracker> _bulkOperationTrackers =
            new ConcurrentDictionary<string, BulkOperationTracker>();

        private readonly Lazy<ColumnValueProvider> _columnValueProvider =
            new Lazy<ColumnValueProvider>(() => new ColumnValueProvider());

        #endregion

        #region Events

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the column value provider
        /// </summary>
        public ColumnValueProvider ColumnValueProvider => _columnValueProvider.Value;

        /// <summary>
        /// Gets or creates column metadata for efficient access
        /// </summary>
        public ColumnValueMetadata GetOrCreateMetadata(string columnKey, string bindingPath)
        {
            return _columnMetadata.GetOrAdd(columnKey, key => new ColumnValueMetadata
            {
                ColumnKey = key,
                BindingPath = bindingPath,
                Values = new HashSet<object>(),
                SortedValues = new List<object>(),
                ValueCounts = new NullSafeValueCounts(),
                LastUpdated = DateTime.MinValue
            });
        }



        /// <summary>
        /// Updates column values efficiently with incremental updates
        /// </summary>
        public Task UpdateColumnValuesAsync(string columnKey, IEnumerable<object> items, string bindingPath)
        {
            // Use column value provider for large datasets
            _ = _columnValueProvider.Value.UpdateColumnValuesAsync(columnKey, items, bindingPath);
            
            return Task.Run(() =>
            {
                var metadata = GetOrCreateMetadata(columnKey, bindingPath);

                lock (metadata.SyncRoot)
                {
                    // Clear existing data
                    metadata.Values.Clear();
                    metadata.ValueCounts.Clear();

                    // Process all items
                    foreach (var item in items)
                    {
                        var value = ReflectionHelper.GetPropValue(item, bindingPath);
                        metadata.Values.Add(value);

                        // Track value counts - handle null keys with helper method
                        IncrementValueCount(metadata.ValueCounts, value);
                    }

                    // Update sorted values
                    UpdateSortedValues(metadata);

                    // Determine data type if not set
                    if (metadata.DataType == ColumnDataType.String && metadata.Values.Any())
                    {
                        metadata.DataType = ReflectionHelper.DetermineColumnDataType(metadata.Values);
                    }

                    metadata.LastUpdated = DateTime.Now;
                }

                // Invalidate related view models
                InvalidateFilterViewModels(columnKey);
            });
        }

        /// <summary>
        /// Updates the cache incrementally when items are added or removed
        /// </summary>
        public async Task UpdateColumnValuesIncrementalAsync(string columnKey, NotifyCollectionChangedEventArgs e, string bindingPath)
        {
            var metadata = GetOrCreateMetadata(columnKey, bindingPath);

            // Check for bulk operations
            if (DetectBulkOperation(columnKey, e))
            {
                var tracker = _bulkOperationTrackers.GetOrAdd(columnKey, _ => new BulkOperationTracker());
                tracker.IsBulkOperation = true;
                
                // Start or reset the batch timer
                if (tracker.BatchTimer == null)
                {
                    tracker.BatchTimer = new Timer(200); // 200ms delay
                    tracker.BatchTimer.Elapsed += (s, args) => HandleBulkOperationEnd(columnKey);
                    tracker.BatchTimer.AutoReset = false;
                }
                
                tracker.BatchTimer.Stop();
                tracker.BatchTimer.Start();
                
                // Don't process individual updates during bulk operations
                return;
            }

            await Task.Run(() =>
            {
                lock (metadata.SyncRoot)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            if (e.NewItems != null)
                            {
                                foreach (var item in e.NewItems)
                                {
                                    var value = ReflectionHelper.GetPropValue(item, bindingPath);
                                    metadata.Values.Add(value);

                                    IncrementValueCount(metadata.ValueCounts, value);
                                }
                                UpdateSortedValues(metadata);
                            }
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            if (e.OldItems != null)
                            {
                                foreach (var item in e.OldItems)
                                {
                                    var value = ReflectionHelper.GetPropValue(item, bindingPath);
                                    var currentCount = GetValueCount(metadata.ValueCounts, value);
                                    if (currentCount > 0)
                                    {
                                        var newCount = currentCount - 1;
                                        SetValueCount(metadata.ValueCounts, value, newCount);
                                        if (newCount <= 0)
                                        {
                                            metadata.Values.Remove(value);
                                        }
                                    }
                                }
                                UpdateSortedValues(metadata);
                            }
                            break;

                        default:
                            // For other actions, do a full reload
                            return;
                    }

                    metadata.LastUpdated = DateTime.Now;
                }
            });

            // Notify any listening filter view models
            InvalidateFilterViewModels(columnKey);
        }

        /// <summary>
        /// Gets or creates a cached filter value view model
        /// </summary>
        public FilterValueViewModel GetOrCreateFilterViewModel(
            string columnKey,
            ColumnDataType dataType)
        {
            var cacheKey = $"{columnKey}_{dataType}";

            return _filterViewModelCache.GetOrAdd(cacheKey, key =>
            {
                FilterValueViewModel viewModel;

                switch (dataType)
                {
                    // grouped is handled separately
                    case ColumnDataType.DateTime:
                        viewModel = new DateTreeViewFilterValueViewModel();
                        break;
                    default:
                        viewModel = new FlatListFilterValueViewModel();
                        break;
                }

                // Load values from metadata using the new method
                if (_columnMetadata.TryGetValue(columnKey, out var metadata))
                {
                    // Create ValueAggregateMetadata from the ColumnValueMetadata
                    var metadataList = metadata.Values.Select(value => new ValueAggregateMetadata(value, GetValueCount(metadata.ValueCounts, value) > 0 ? GetValueCount(metadata.ValueCounts, value) : 1));
                    
                    viewModel.LoadValuesWithMetadata(metadataList);
                }

                return viewModel;
            });
        }

        /// <summary>
        /// Clears all caches
        /// </summary>
        public void ClearAllCaches()
        {
            // Clear column value provider first (largest memory consumer)
            _columnValueProvider.Value.ClearAll();
            
            // Clear filter view models (dispose if they implement IDisposable)
            var viewModelsToDispose = _filterViewModelCache.Values.ToList();
            _filterViewModelCache.Clear();
            
            foreach (var viewModel in viewModelsToDispose)
            {
                if (viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            // Clear column metadata
            var metadataToDispose = _columnMetadata.Values.ToList();
            _columnMetadata.Clear();
            
            // Dispose all bulk operation trackers
            foreach (var tracker in _bulkOperationTrackers.Values)
            {
                tracker.BatchTimer?.Dispose();
            }
            _bulkOperationTrackers.Clear();
            
            // Force garbage collection for coordinated cleanup
            if (viewModelsToDispose.Count > 5 || metadataToDispose.Count > 10)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the count for a value from the NullSafeValueCounts
        /// </summary>
        private static int GetValueCount(NullSafeValueCounts valueCounts, object value)
        {
            return valueCounts[value];
        }

        /// <summary>
        /// Sets the count for a value in the NullSafeValueCounts
        /// </summary>
        private static void SetValueCount(NullSafeValueCounts valueCounts, object value, int count)
        {
            valueCounts[value] = count;
        }

        /// <summary>
        /// Increments the count for a value in the NullSafeValueCounts
        /// </summary>
        private static void IncrementValueCount(NullSafeValueCounts valueCounts, object value)
        {
            valueCounts[value] = valueCounts[value] + 1;
        }

        /// <summary>
        /// Decrements the count for a value in the NullSafeValueCounts
        /// </summary>
        private static void DecrementValueCount(NullSafeValueCounts valueCounts, object value)
        {
            var currentCount = valueCounts[value];
            if (currentCount > 0)
            {
                valueCounts[value] = currentCount - 1;
            }
        }

        private void UpdateSortedValues(ColumnValueMetadata metadata)
        {
            metadata.SortedValues = metadata.Values
                .OrderBy(v => v?.ToString() ?? string.Empty)
                .ToList();
        }

        private void InsertSortedValue(ColumnValueMetadata metadata, object value)
        {
            var index = BinarySearchCompat(metadata.SortedValues, value,
                new ObjectStringComparer());

            if (index < 0)
            {
                metadata.SortedValues.Insert(~index, value);
            }
        }

        // .NET Standard 2.0 compatible binary search
        private static int BinarySearchCompat<T>(List<T> list, T value, IComparer<T> comparer)
        {
            int low = 0;
            int high = list.Count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                int comp = comparer.Compare(list[mid], value);

                if (comp == 0)
                    return mid;

                if (comp < 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return ~low;
        }

        private void InvalidateFilterViewModels(string columnKey)
        {
            var relatedKeys = _filterViewModelCache.Keys.Where(k => k.StartsWith(columnKey)).ToList();
            foreach (var key in relatedKeys)
            {
                if (_filterViewModelCache.TryGetValue(key, out var viewModel))
                {
                    viewModel.ClearCache();
                }
            }
        }

        private void UpdateFilterViewModelsIncremental(string columnKey, object value, bool isAdd)
        {
            var relatedKeys = _filterViewModelCache.Keys.Where(k => k.StartsWith(columnKey)).ToList();
            foreach (var key in relatedKeys)
            {
                if (_filterViewModelCache.TryGetValue(key, out var viewModel))
                {
                    viewModel.UpdateValueIncremental(value, isAdd);
                }
            }
        }

        private bool DetectBulkOperation(string columnKey, NotifyCollectionChangedEventArgs e)
        {
            var tracker = _bulkOperationTrackers.GetOrAdd(columnKey, _ => new BulkOperationTracker());
            
            // Consider bulk if multiple items being added at once
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.Count > 1)
                return true;
            
            // Or if rapid successive operations (within 50ms)
            var now = DateTime.Now;
            if ((now - tracker.LastOperationTime).TotalMilliseconds < 50)
            {
                tracker.OperationCount++;
                if (tracker.OperationCount > 5) // Threshold for bulk detection
                    return true;
            }
            else
            {
                tracker.OperationCount = 1;
            }
            
            tracker.LastOperationTime = now;
            return false;
        }

        private void HandleBulkOperationEnd(string columnKey)
        {
            if (_bulkOperationTrackers.TryGetValue(columnKey, out var tracker))
            {
                tracker.IsBulkOperation = false;
                tracker.OperationCount = 0;
                
                
                // Invalidate filter view models
                InvalidateFilterViewModels(columnKey);
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Tracks bulk operations for performance optimization
        /// </summary>
        public class BulkOperationTracker
        {
            public bool IsBulkOperation { get; set; }
            public DateTime LastOperationTime { get; set; }
            public int OperationCount { get; set; }
            public Timer BatchTimer { get; set; }
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                ClearAllCaches();
                _disposed = true;
            }
        }

        ~ColumnValueCache()
        {
            Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Helper class for object comparison
    /// </summary>
    internal class ObjectStringComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            return string.Compare(x?.ToString() ?? string.Empty,
                                y?.ToString() ?? string.Empty,
                                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Metadata for cached column values
    /// </summary>
    public class ColumnValueMetadata
    {
        public string ColumnKey { get; set; }
        public string BindingPath { get; set; }
        public HashSet<object> Values { get; set; }
        public List<object> SortedValues { get; set; }
        public NullSafeValueCounts ValueCounts { get; set; }
        public ColumnDataType DataType { get; set; } = ColumnDataType.String;
        public DateTime LastUpdated { get; set; }
        public object SyncRoot { get; } = new object();
    }

    /// <summary>
    /// A simple wrapper around Dictionary that handles null keys properly
    /// </summary>
    public class NullSafeValueCounts
    {
        private readonly Dictionary<object, int> _dictionary = new Dictionary<object, int>();
        private int _nullCount = 0;

        public int this[object key]
        {
            get
            {
                if (key == null)
                    return _nullCount;
                return _dictionary.ContainsKey(key) ? _dictionary[key] : 0;
            }
            set
            {
                if (key == null)
                {
                    _nullCount = value;
                }
                else
                {
                    if (value <= 0)
                        _dictionary.Remove(key);
                    else
                        _dictionary[key] = value;
                }
            }
        }

        public bool ContainsKey(object key)
        {
            if (key == null)
                return _nullCount > 0;
            return _dictionary.ContainsKey(key);
        }

        public void Clear()
        {
            _dictionary.Clear();
            _nullCount = 0;
        }

        public void Remove(object key)
        {
            if (key == null)
                _nullCount = 0;
            else
                _dictionary.Remove(key);
        }

        public Dictionary<object, int> ToRegularDictionary()
        {
            // Return a custom dictionary that handles null keys
            return new NullHandlingDictionary(_dictionary, _nullCount);
        }
    }

    /// <summary>
    /// A dictionary that extends Dictionary<object, int> to handle null keys
    /// </summary>
    public class NullHandlingDictionary : Dictionary<object, int>
    {
        private int _nullCount;

        public NullHandlingDictionary(Dictionary<object, int> source, int nullCount) : base(source)
        {
            _nullCount = nullCount;
        }

        public new int this[object key]
        {
            get
            {
                if (key == null)
                    return _nullCount;
                return base.ContainsKey(key) ? base[key] : 0;
            }
            set
            {
                if (key == null)
                    _nullCount = value;
                else
                    base[key] = value;
            }
        }

        public new bool ContainsKey(object key)
        {
            if (key == null)
                return _nullCount > 0;
            return base.ContainsKey(key);
        }

        public new bool TryGetValue(object key, out int value)
        {
            if (key == null)
            {
                value = _nullCount;
                return _nullCount > 0;
            }
            return base.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Helper class to create dictionaries that can handle null keys
    /// </summary>
    public static class NullSafeDictionaryHelper
    {
        /// <summary>
        /// Creates a dictionary that can handle null keys
        /// </summary>
        /// <returns>A dictionary that can handle null keys</returns>
        public static Dictionary<object, int> CreateNullSafeDictionary()
        {
            return new NullHandlingDictionary(new Dictionary<object, int>(), 0);
        }
    }
}