using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// High-performance cache for column values - .NET Standard 2.0 compatible
    /// </summary>
    public class ColumnValueCache
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

        #endregion

        #region Public Methods

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
            _columnMetadata.TryRemove(columnKey, out _);

            // Remove related view models
            var keysToRemove = _filterViewModelCache.Keys.Where(k => k.StartsWith(columnKey)).ToList();
            foreach (var key in keysToRemove)
            {
                _filterViewModelCache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Clears all caches
        /// </summary>
        public void ClearAllCaches()
        {
            _columnMetadata.Clear();
            _filterViewModelCache.Clear();
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

        #endregion

        #region Helper Classes

        private class ObjectStringComparer : IComparer<object>
        {
            public int Compare(object x, object y)
            {
                return string.Compare(x?.ToString() ?? string.Empty,
                                    y?.ToString() ?? string.Empty,
                                    StringComparison.Ordinal);
            }
        }

        #endregion
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
