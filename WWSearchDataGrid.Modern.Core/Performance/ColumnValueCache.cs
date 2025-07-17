using System;
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
                ValueCounts = new NullSafeDictionary<object, int>(),
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

                        // Track value counts - NullSafeDictionary handles null keys
                        if (metadata.ValueCounts.ContainsKey(value))
                            metadata.ValueCounts[value]++;
                        else
                            metadata.ValueCounts[value] = 1;
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

                                    if (metadata.ValueCounts.ContainsKey(value))
                                        metadata.ValueCounts[value]++;
                                    else
                                        metadata.ValueCounts[value] = 1;
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
                                    if (metadata.ValueCounts.ContainsKey(value))
                                    {
                                        metadata.ValueCounts[value]--;
                                        if (metadata.ValueCounts[value] <= 0)
                                        {
                                            metadata.Values.Remove(value);
                                            metadata.ValueCounts.Remove(value);
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
            ColumnDataType dataType,
            FilterValueConfiguration configuration = null)
        {
            var displayMode = configuration?.DisplayMode ?? FilterValueDisplayMode.FlatList;
            var cacheKey = $"{columnKey}_{dataType}_{displayMode}";

            return _filterViewModelCache.GetOrAdd(cacheKey, key =>
            {
                FilterValueViewModel viewModel;

                if (configuration != null)
                {
                    switch (configuration.DisplayMode)
                    {
                        case FilterValueDisplayMode.GroupedByColumn:
                            viewModel = new GroupedTreeViewFilterValueViewModel
                            {
                                GroupByColumn = configuration.GroupByColumn
                            };
                            break;
                        case FilterValueDisplayMode.GroupedByYear:
                        case FilterValueDisplayMode.GroupedByMonth:
                            viewModel = new DateTreeViewFilterValueViewModel();
                            break;
                        default:
                            viewModel = new FlatListFilterValueViewModel();
                            break;
                    }
                }
                else
                {
                    switch (dataType)
                    {
                        case ColumnDataType.DateTime:
                            viewModel = new DateTreeViewFilterValueViewModel();
                            break;
                        default:
                            viewModel = new FlatListFilterValueViewModel();
                            break;
                    }
                }

                // Load values from metadata
                if (_columnMetadata.TryGetValue(columnKey, out var metadata))
                {
                    viewModel.LoadValuesWithCounts(metadata.Values, metadata.ValueCounts);
                }

                return viewModel;
            });
        }

        /// <summary>
        /// Incremental update for single item addition
        /// </summary>
        public void AddItemValue(string columnKey, object item, string bindingPath)
        {
            // Update column value provider
            _ = _columnValueProvider.Value.AddValueAsync(columnKey, item, bindingPath);
            
            var metadata = GetOrCreateMetadata(columnKey, bindingPath);
            var value = ReflectionHelper.GetPropValue(item, bindingPath);

            lock (metadata.SyncRoot)
            {
                metadata.Values.Add(value);

                // NullSafeDictionary handles null keys
                if (metadata.ValueCounts.ContainsKey(value))
                    metadata.ValueCounts[value]++;
                else
                    metadata.ValueCounts[value] = 1;

                // Efficiently update sorted values
                InsertSortedValue(metadata, value);
                metadata.LastUpdated = DateTime.Now;
            }

            // Update view models incrementally
            UpdateFilterViewModelsIncremental(columnKey, value, true);
        }

        /// <summary>
        /// Incremental update for single item removal
        /// </summary>
        public void RemoveItemValue(string columnKey, object item, string bindingPath)
        {
            // Update column value provider
            _ = _columnValueProvider.Value.RemoveValueAsync(columnKey, item, bindingPath);
            
            var metadata = GetOrCreateMetadata(columnKey, bindingPath);
            var value = ReflectionHelper.GetPropValue(item, bindingPath);

            lock (metadata.SyncRoot)
            {
                if (metadata.ValueCounts.ContainsKey(value))
                {
                    metadata.ValueCounts[value]--;
                    if (metadata.ValueCounts[value] <= 0)
                    {
                        metadata.Values.Remove(value);
                        metadata.ValueCounts.Remove(value);
                        metadata.SortedValues.Remove(value);
                    }
                }

                metadata.LastUpdated = DateTime.Now;
            }

            // Update view models incrementally
            UpdateFilterViewModelsIncremental(columnKey, value, false);
        }

        /// <summary>
        /// Clears cache for a specific column
        /// </summary>
        public void ClearColumnCache(string columnKey)
        {
            // Clear column value provider
            _columnValueProvider.Value.InvalidateColumn(columnKey);
            
            _columnMetadata.TryRemove(columnKey, out _);

            // Remove related view models
            var keysToRemove = _filterViewModelCache.Keys.Where(k => k.StartsWith(columnKey)).ToList();
            foreach (var key in keysToRemove)
            {
                _filterViewModelCache.TryRemove(key, out _);
            }
            
            // Remove bulk operation tracker
            if (_bulkOperationTrackers.TryRemove(columnKey, out var tracker))
            {
                tracker.BatchTimer?.Dispose();
            }
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
        public NullSafeDictionary<object, int> ValueCounts { get; set; }
        public ColumnDataType DataType { get; set; } = ColumnDataType.String;
        public DateTime LastUpdated { get; set; }
        public object SyncRoot { get; } = new object();
    }
}