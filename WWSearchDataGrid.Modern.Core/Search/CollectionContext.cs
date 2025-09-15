using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WWSearchDataGrid.Modern.Core.Strategies;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Implementation of collection context for statistical and ranking operations
    /// Provides lazy-loaded cached access to computed collection statistics
    /// Optimized for performance with large datasets
    /// </summary>
    public class CollectionContext : ICollectionContext, IDisposable
    {
        private readonly Lazy<double?> _average;
        private readonly Lazy<IEnumerable<object>> _sortedDescending;
        private readonly Lazy<IEnumerable<object>> _sortedAscending;
        private readonly Lazy<Dictionary<object, List<object>>> _valueGroups;
        private readonly Lazy<List<(object item, object value)>> _extractedValues;

        /// <summary>
        /// Gets the full collection being filtered
        /// </summary>
        public IEnumerable<object> Items { get; }

        /// <summary>
        /// Gets the column path for context-sensitive operations
        /// </summary>
        public string ColumnPath { get; }

        /// <summary>
        /// Creates a new collection context for the specified items and column
        /// </summary>
        /// <param name="items">Collection of items to analyze</param>
        /// <param name="columnPath">Column path for value extraction</param>
        public CollectionContext(IEnumerable<object> items, string columnPath)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            ColumnPath = columnPath ?? throw new ArgumentNullException(nameof(columnPath));

            // Initialize lazy computations - extractedValues is shared across all operations
            _extractedValues = new Lazy<List<(object item, object value)>>(ExtractAllValues);
            _average = new Lazy<double?>(ComputeAverage);
            _sortedDescending = new Lazy<IEnumerable<object>>(ComputeSortedDescending);
            _sortedAscending = new Lazy<IEnumerable<object>>(ComputeSortedAscending);
            _valueGroups = new Lazy<Dictionary<object, List<object>>>(ComputeValueGroups);
        }

        /// <summary>
        /// Gets the average value for the column (lazy loaded)
        /// </summary>
        public double? GetAverage() => _average.Value;

        /// <summary>
        /// Gets items sorted by column value in descending order (lazy loaded)
        /// </summary>
        public IEnumerable<object> GetSortedDescending() => _sortedDescending.Value;

        /// <summary>
        /// Gets items sorted by column value in ascending order (lazy loaded)
        /// </summary>
        public IEnumerable<object> GetSortedAscending() => _sortedAscending.Value;

        /// <summary>
        /// Gets value frequency map for uniqueness operations (lazy loaded)
        /// </summary>
        public Dictionary<object, List<object>> GetValueGroups() => _valueGroups.Value;

        /// <summary>
        /// Extracts all item-value pairs once to share across multiple operations
        /// This eliminates redundant reflection calls across different statistical operations
        /// </summary>
        private List<(object item, object value)> ExtractAllValues()
        {
            try
            {
                return Items
                    .Select(item => (item, value: ReflectionHelper.GetPropValue(item, ColumnPath)))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting values for column {ColumnPath}: {ex.Message}");
                return new List<(object item, object value)>();
            }
        }

        /// <summary>
        /// Computes the average of numeric values in the column using pre-extracted values
        /// </summary>
        private double? ComputeAverage()
        {
            try
            {
                var extractedValues = _extractedValues.Value;
                var numericValues = extractedValues
                    .Where(x => x.value != null && ReflectionHelper.IsNumericValue(x.value))
                    .Select(x => ((object)x is null) 
                                    ? double.NaN
                                    : Convert.ToDouble(x, CultureInfo.InvariantCulture))
                    .Where(value => !double.IsNaN(value))
                    .ToList();

                return numericValues.Count > 0 ? (double?)numericValues.Average() : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error computing average for column {ColumnPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Computes items sorted by column value in descending order using pre-extracted values
        /// </summary>
        private IEnumerable<object> ComputeSortedDescending()
        {
            try
            {
                var extractedValues = _extractedValues.Value;
                return extractedValues
                    .Where(x => x.value != null && ReflectionHelper.IsComparableValue(x.value))
                    .OrderByDescending(x => x.value)
                    .Select(x => x.item)
                    .ToList(); // Materialize to avoid re-evaluation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sorting descending for column {ColumnPath}: {ex.Message}");
                return Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// Computes items sorted by column value in ascending order using pre-extracted values
        /// </summary>
        private IEnumerable<object> ComputeSortedAscending()
        {
            try
            {
                var extractedValues = _extractedValues.Value;
                return extractedValues
                    .Where(x => x.value != null && ReflectionHelper.IsComparableValue(x.value))
                    .OrderBy(x => x.value)
                    .Select(x => x.item)
                    .ToList(); // Materialize to avoid re-evaluation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sorting ascending for column {ColumnPath}: {ex.Message}");
                return Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// Computes value frequency groups for uniqueness operations using pre-extracted values
        /// </summary>
        private Dictionary<object, List<object>> ComputeValueGroups()
        {
            try
            {
                var extractedValues = _extractedValues.Value;
                return extractedValues
                    .GroupBy(x => x.value?.ToString() ?? string.Empty)
                    .ToDictionary(g => (object)g.Key, g => g.Select(x => x.item).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error computing value groups for column {ColumnPath}: {ex.Message}");
                return new Dictionary<object, List<object>>();
            }
        }
        
        /// <summary>
        /// Disposes of the collection context and releases all cached references
        /// This allows garbage collection of the underlying data items
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // The lazy objects hold references to computed data that may reference original items
                // Unfortunately, Lazy<T> doesn't provide a way to clear its cached value
                // But when this CollectionContext is disposed and goes out of scope,
                // the lazy objects will be eligible for garbage collection along with their cached values
                
                // Clear any direct references we might have
                // (Currently we don't hold any direct references to items beyond the constructor parameter)
                
                // The best we can do is ensure this object becomes eligible for GC
                // which will allow the lazy-loaded caches to be collected as well
            }
        }
        
        /// <summary>
        /// Finalizer to ensure cleanup if Dispose is not called
        /// </summary>
        ~CollectionContext()
        {
            Dispose(false);
        }
    }
}