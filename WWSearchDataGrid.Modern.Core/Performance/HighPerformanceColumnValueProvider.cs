using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// High-performance column value provider with O(1) lookups and background processing
    /// </summary>
    public class HighPerformanceColumnValueProvider : IColumnValueProvider
    {
        #region Constants
        
        private const int DefaultPageSize = 50;
        private const int MaxCacheSize = 100000;
        private const int MaxMemoryMB = 50;
        private const int BackgroundProcessingDelayMs = 100;
        
        #endregion

        #region Fields
        
        private readonly ConcurrentDictionary<string, ColumnValueStorage> _columnStorage;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _columnSemaphores;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _backgroundTasks;
        private readonly object _memoryLock = new object();
        private long _currentMemoryUsage;

        #endregion

        #region Constructor

        public HighPerformanceColumnValueProvider()
        {
            _columnStorage = new ConcurrentDictionary<string, ColumnValueStorage>();
            _columnSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _backgroundTasks = new ConcurrentDictionary<string, CancellationTokenSource>();
            _currentMemoryUsage = 0;
        }

        #endregion

        #region IColumnValueProvider Implementation

        public async Task<ColumnValueResponse> GetValuesAsync(ColumnValueRequest request)
        {
            if (string.IsNullOrEmpty(request.ColumnKey))
                throw new ArgumentException("ColumnKey cannot be null or empty", nameof(request));

            var storage = GetOrCreateStorage(request.ColumnKey);
            var semaphore = GetColumnSemaphore(request.ColumnKey);

            await semaphore.WaitAsync();
            try
            {
                var filteredValues = await FilterValuesAsync(storage, request);
                var pagedValues = ApplyPaging(filteredValues, request);

                return new ColumnValueResponse
                {
                    Values = pagedValues,
                    TotalCount = filteredValues.Count(),
                    HasMore = (request.Skip + request.Take) < filteredValues.Count(),
                    ColumnKey = request.ColumnKey,
                    IsFromCache = true
                };
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<int> GetTotalCountAsync(string columnKey)
        {
            if (string.IsNullOrEmpty(columnKey))
                return 0;

            var storage = GetOrCreateStorage(columnKey);
            var semaphore = GetColumnSemaphore(columnKey);

            await semaphore.WaitAsync();
            try
            {
                return storage.Values.Count;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void InvalidateColumn(string columnKey)
        {
            if (string.IsNullOrEmpty(columnKey))
                return;

            // Cancel any background processing
            if (_backgroundTasks.TryRemove(columnKey, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            // Remove storage
            if (_columnStorage.TryRemove(columnKey, out var storage))
            {
                UpdateMemoryUsage(-storage.EstimatedMemoryUsage);
            }

            // Remove semaphore
            if (_columnSemaphores.TryRemove(columnKey, out var semaphore))
            {
                semaphore.Dispose();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates column values with background processing for performance
        /// </summary>
        public async Task UpdateColumnValuesAsync(string columnKey, IEnumerable<object> sourceData, string bindingPath)
        {
            if (string.IsNullOrEmpty(columnKey) || sourceData == null)
                return;

            // Cancel any existing background task
            if (_backgroundTasks.TryGetValue(columnKey, out var existingCts))
            {
                existingCts.Cancel();
            }

            var newCts = new CancellationTokenSource();
            _backgroundTasks.AddOrUpdate(columnKey, newCts, (key, old) => 
            {
                old.Cancel();
                old.Dispose();
                return newCts;
            });

            try
            {
                await Task.Run(async () =>
                {
                    await ProcessColumnValuesAsync(columnKey, sourceData, bindingPath, newCts.Token);
                }, newCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Background task was cancelled, which is expected
            }
        }

        /// <summary>
        /// Adds a single value incrementally
        /// </summary>
        public async Task AddValueAsync(string columnKey, object item, string bindingPath)
        {
            if (string.IsNullOrEmpty(columnKey) || item == null)
                return;

            var storage = GetOrCreateStorage(columnKey);
            var semaphore = GetColumnSemaphore(columnKey);
            var value = ReflectionHelper.GetPropValue(item, bindingPath);

            await semaphore.WaitAsync();
            try
            {
                await AddValueToStorageAsync(storage, value);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Removes a single value incrementally
        /// </summary>
        public async Task RemoveValueAsync(string columnKey, object item, string bindingPath)
        {
            if (string.IsNullOrEmpty(columnKey) || item == null)
                return;

            var storage = GetOrCreateStorage(columnKey);
            var semaphore = GetColumnSemaphore(columnKey);
            var value = ReflectionHelper.GetPropValue(item, bindingPath);

            await semaphore.WaitAsync();
            try
            {
                await RemoveValueFromStorageAsync(storage, value);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Gets current memory usage in MB
        /// </summary>
        public long GetMemoryUsageMB()
        {
            return _currentMemoryUsage / (1024 * 1024);
        }

        /// <summary>
        /// Clears all cached data
        /// </summary>
        public void ClearAll()
        {
            // Cancel all background tasks
            foreach (var kvp in _backgroundTasks)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _backgroundTasks.Clear();

            // Clear storage
            _columnStorage.Clear();
            
            // Dispose semaphores
            foreach (var semaphore in _columnSemaphores.Values)
            {
                semaphore.Dispose();
            }
            _columnSemaphores.Clear();

            // Reset memory usage
            _currentMemoryUsage = 0;
        }

        #endregion

        #region Private Methods

        private ColumnValueStorage GetOrCreateStorage(string columnKey)
        {
            return _columnStorage.GetOrAdd(columnKey, key => new ColumnValueStorage(key));
        }

        private SemaphoreSlim GetColumnSemaphore(string columnKey)
        {
            return _columnSemaphores.GetOrAdd(columnKey, key => new SemaphoreSlim(1, 1));
        }

        private async Task ProcessColumnValuesAsync(string columnKey, IEnumerable<object> sourceData, string bindingPath, CancellationToken cancellationToken)
        {
            var storage = GetOrCreateStorage(columnKey);
            var semaphore = GetColumnSemaphore(columnKey);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // Clear existing values
                var oldMemoryUsage = storage.EstimatedMemoryUsage;
                storage.Clear();

                // Process items in batches to avoid blocking
                var batchSize = 1000;
                var batch = new List<object>(batchSize);
                
                foreach (var item in sourceData)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    batch.Add(item);
                    
                    if (batch.Count >= batchSize)
                    {
                        await ProcessBatchAsync(storage, batch, bindingPath, cancellationToken);
                        batch.Clear();
                        
                        // Brief pause to prevent CPU monopolization
                        await Task.Delay(1, cancellationToken);
                    }
                }

                // Process remaining items
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(storage, batch, bindingPath, cancellationToken);
                }

                // Update memory usage
                UpdateMemoryUsage(storage.EstimatedMemoryUsage - oldMemoryUsage);
                
                // Check memory limits and cleanup if needed
                await CheckMemoryLimitsAsync();
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ProcessBatchAsync(ColumnValueStorage storage, List<object> batch, string bindingPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                foreach (var item in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var value = ReflectionHelper.GetPropValue(item, bindingPath);
                    var hashCode = value?.GetHashCode() ?? 0;
                    
                    storage.Values.AddOrUpdate(hashCode, 
                        new ValueAggregateMetadata
                        {
                            Value = value,
                            Count = 1,
                            LastSeen = DateTime.UtcNow,
                            HashCode = hashCode
                        },
                        (key, existing) =>
                        {
                            existing.Count++;
                            existing.LastSeen = DateTime.UtcNow;
                            return existing;
                        });
                }
            }, cancellationToken);
        }

        private async Task AddValueToStorageAsync(ColumnValueStorage storage, object value)
        {
            await Task.Run(() =>
            {
                var hashCode = value?.GetHashCode() ?? 0;
                
                storage.Values.AddOrUpdate(hashCode,
                    new ValueAggregateMetadata
                    {
                        Value = value,
                        Count = 1,
                        LastSeen = DateTime.UtcNow,
                        HashCode = hashCode
                    },
                    (key, existing) =>
                    {
                        existing.Count++;
                        existing.LastSeen = DateTime.UtcNow;
                        return existing;
                    });

                UpdateMemoryUsage(EstimateValueMemoryUsage(value));
            });
        }

        private async Task RemoveValueFromStorageAsync(ColumnValueStorage storage, object value)
        {
            await Task.Run(() =>
            {
                var hashCode = value?.GetHashCode() ?? 0;
                
                if (storage.Values.TryGetValue(hashCode, out var metadata))
                {
                    metadata.Count--;
                    if (metadata.Count <= 0)
                    {
                        storage.Values.TryRemove(hashCode, out _);
                        UpdateMemoryUsage(-EstimateValueMemoryUsage(value));
                    }
                    else
                    {
                        metadata.LastSeen = DateTime.UtcNow;
                    }
                }
            });
        }

        private async Task<IEnumerable<ValueAggregateMetadata>> FilterValuesAsync(ColumnValueStorage storage, ColumnValueRequest request)
        {
            return await Task.Run(() =>
            {
                var values = storage.Values.Values.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    var searchText = request.SearchText.ToLowerInvariant();
                    values = values.Where(v => v.Value?.ToString().ToLowerInvariant().Contains(searchText) ?? false);
                }

                // Include/exclude null and empty values
                if (!request.IncludeNull)
                {
                    values = values.Where(v => v.Value != null);
                }

                if (!request.IncludeEmpty)
                {
                    values = values.Where(v => !string.IsNullOrEmpty(v.Value?.ToString()));
                }

                // Exclude specific values
                if (request.ExcludeValues != null)
                {
                    var excludeSet = new HashSet<int>(request.ExcludeValues.Select(v => v?.GetHashCode() ?? 0));
                    values = values.Where(v => !excludeSet.Contains(v.HashCode));
                }

                // Apply sorting
                if (request.GroupByFrequency)
                {
                    values = request.SortAscending
                        ? values.OrderBy(v => v.Count).ThenBy(v => v.Value?.ToString())
                        : values.OrderByDescending(v => v.Count).ThenBy(v => v.Value?.ToString());
                }
                else
                {
                    values = request.SortAscending
                        ? values.OrderBy(v => v.Value?.ToString())
                        : values.OrderByDescending(v => v.Value?.ToString());
                }

                return values;
            });
        }

        private IEnumerable<ValueAggregateMetadata> ApplyPaging(IEnumerable<ValueAggregateMetadata> values, ColumnValueRequest request)
        {
            var skip = Math.Max(0, request.Skip);
            var take = request.Take > 0 ? request.Take : DefaultPageSize;
            
            return values.Skip(skip).Take(take);
        }

        private void UpdateMemoryUsage(long deltaBytes)
        {
            lock (_memoryLock)
            {
                _currentMemoryUsage += deltaBytes;
            }
        }

        private long EstimateValueMemoryUsage(object value)
        {
            if (value == null) return 8; // Reference size
            
            // Rough estimation based on type
            if (value is string s) return s.Length * 2 + 24; // Unicode chars + string overhead
            if (value is int) return 4;
            if (value is long) return 8;
            if (value is double) return 8;
            if (value is decimal) return 16;
            if (value is DateTime) return 8;
            if (value is bool) return 1;
            
            return 24; // Default object overhead
        }

        private async Task CheckMemoryLimitsAsync()
        {
            if (GetMemoryUsageMB() > MaxMemoryMB)
            {
                await Task.Run(() =>
                {
                    // Remove least recently used columns
                    var sortedColumns = _columnStorage.Values
                        .OrderBy(s => s.Values.Values.Min(v => v.LastSeen))
                        .Take(_columnStorage.Count / 4); // Remove 25% of columns

                    foreach (var storage in sortedColumns)
                    {
                        InvalidateColumn(storage.ColumnKey);
                    }
                });
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// High-performance storage for column values using ConcurrentDictionary
        /// </summary>
        private class ColumnValueStorage
        {
            public string ColumnKey { get; }
            public ConcurrentDictionary<int, ValueAggregateMetadata> Values { get; }
            public long EstimatedMemoryUsage { get; private set; }

            public ColumnValueStorage(string columnKey)
            {
                ColumnKey = columnKey;
                Values = new ConcurrentDictionary<int, ValueAggregateMetadata>();
                EstimatedMemoryUsage = 0;
            }

            public void Clear()
            {
                Values.Clear();
                EstimatedMemoryUsage = 0;
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                ClearAll();
                _disposed = true;
            }
        }

        #endregion
    }
}