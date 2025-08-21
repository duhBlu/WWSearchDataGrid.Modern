using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core.Strategies;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Implementation of collection context for statistical and ranking operations
    /// Provides lazy-loaded cached access to computed collection statistics
    /// </summary>
    public class CollectionContext : ICollectionContext
    {
        private readonly Lazy<double?> _average;
        private readonly Lazy<IEnumerable<object>> _sortedDescending;
        private readonly Lazy<IEnumerable<object>> _sortedAscending;
        private readonly Lazy<Dictionary<object, List<object>>> _valueGroups;

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
            Items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
            ColumnPath = columnPath ?? throw new ArgumentNullException(nameof(columnPath));

            // Initialize lazy computations
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
        /// Computes the average of numeric values in the column
        /// </summary>
        private double? ComputeAverage()
        {
            try
            {
                var numericValues = Items
                    .Select(item => ReflectionHelper.GetPropValue(item, ColumnPath))
                    .Where(value => value != null && ReflectionHelper.IsNumericValue(value))
                    .Select(ConvertToDouble)
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
        /// Computes items sorted by column value in descending order
        /// </summary>
        private IEnumerable<object> ComputeSortedDescending()
        {
            try
            {
                return Items
                    .Select(item => new { Item = item, Value = ReflectionHelper.GetPropValue(item, ColumnPath) })
                    .Where(x => x.Value != null && ReflectionHelper.IsComparableValue(x.Value))
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Item)
                    .ToList(); // Materialize to avoid re-evaluation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sorting descending for column {ColumnPath}: {ex.Message}");
                return Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// Computes items sorted by column value in ascending order
        /// </summary>
        private IEnumerable<object> ComputeSortedAscending()
        {
            try
            {
                return Items
                    .Select(item => new { Item = item, Value = ReflectionHelper.GetPropValue(item, ColumnPath) })
                    .Where(x => x.Value != null && ReflectionHelper.IsComparableValue(x.Value))
                    .OrderBy(x => x.Value)
                    .Select(x => x.Item)
                    .ToList(); // Materialize to avoid re-evaluation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sorting ascending for column {ColumnPath}: {ex.Message}");
                return Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// Computes value frequency groups for uniqueness operations
        /// </summary>
        private Dictionary<object, List<object>> ComputeValueGroups()
        {
            try
            {
                return Items
                    .GroupBy(item => ReflectionHelper.GetPropValue(item, ColumnPath)?.ToString() ?? string.Empty)
                    .ToDictionary(g => (object)g.Key, g => g.ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error computing value groups for column {ColumnPath}: {ex.Message}");
                return new Dictionary<object, List<object>>();
            }
        }

        /// <summary>
        /// Converts a value to double for averaging calculations
        /// </summary>
        private static double ConvertToDouble(object value)
        {
            if (value == null)
                return double.NaN;

            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return double.NaN;
            }
        }
    }
}