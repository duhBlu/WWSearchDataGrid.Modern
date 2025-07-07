using System;
using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Engine for applying data transformations that operate on entire datasets
    /// before traditional row-by-row filtering
    /// </summary>
    public static class DataTransformationEngine
    {
        /// <summary>
        /// Applies a data transformation to the provided dataset
        /// </summary>
        /// <param name="items">The original dataset</param>
        /// <param name="transformation">The transformation to apply</param>
        /// <returns>The transformed dataset</returns>
        public static IEnumerable<object> ApplyTransformation(IEnumerable<object> items, DataTransformation transformation)
        {
            if (items == null || transformation == null || transformation.Type == DataTransformationType.None)
                return items;

            if (!transformation.IsValid())
                throw new ArgumentException($"Invalid transformation: {transformation.GetDescription()}");

            var itemsList = items.ToList();
            if (itemsList.Count == 0)
                return itemsList;

            try
            {
                switch (transformation.Type)
                {
                    case DataTransformationType.TopN:
                        return ApplyTopN(itemsList, transformation);

                    case DataTransformationType.BottomN:
                        return ApplyBottomN(itemsList, transformation);

                    case DataTransformationType.AboveAverage:
                        return ApplyAboveAverage(itemsList, transformation);

                    case DataTransformationType.BelowAverage:
                        return ApplyBelowAverage(itemsList, transformation);

                    case DataTransformationType.Unique:
                        return ApplyUnique(itemsList, transformation);

                    case DataTransformationType.Duplicate:
                        return ApplyDuplicate(itemsList, transformation);

                    default:
                        return itemsList;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying transformation {transformation.Type}: {ex.Message}");
                return itemsList; // Return original data on error
            }
        }

        /// <summary>
        /// Applies multiple transformations in sequence
        /// </summary>
        /// <param name="items">The original dataset</param>
        /// <param name="transformations">The transformations to apply in order</param>
        /// <returns>The transformed dataset</returns>
        public static IEnumerable<object> ApplyTransformations(IEnumerable<object> items, IEnumerable<DataTransformation> transformations)
        {
            if (items == null || transformations == null)
                return items;

            var result = items;
            foreach (var transformation in transformations.Where(t => t != null && t.Type != DataTransformationType.None))
            {
                result = ApplyTransformation(result, transformation);
            }

            return result;
        }

        /// <summary>
        /// Applies TopN transformation - returns the top N items by value
        /// </summary>
        private static IEnumerable<object> ApplyTopN(List<object> items, DataTransformation transformation)
        {
            int count = transformation.GetParameterAsInt();
            if (count <= 0)
                return items;

            return items
                .Select(item => new { Item = item, Value = ReflectionHelper.GetPropValue(item, transformation.ColumnPath) })
                .Where(x => x.Value != null && ReflectionHelper.IsComparableValue(x.Value))
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Item);
        }

        /// <summary>
        /// Applies BottomN transformation - returns the bottom N items by value
        /// </summary>
        private static IEnumerable<object> ApplyBottomN(List<object> items, DataTransformation transformation)
        {
            int count = transformation.GetParameterAsInt();
            if (count <= 0)
                return items;

            return items
                .Select(item => new { Item = item, Value = ReflectionHelper.GetPropValue(item, transformation.ColumnPath) })
                .Where(x => x.Value != null && ReflectionHelper.IsComparableValue(x.Value))
                .OrderBy(x => x.Value)
                .Take(count)
                .Select(x => x.Item);
        }

        /// <summary>
        /// Applies AboveAverage transformation - returns items with values above the column average
        /// </summary>
        private static IEnumerable<object> ApplyAboveAverage(List<object> items, DataTransformation transformation)
        {
            var numericValues = items
                .Select(item => ReflectionHelper.GetPropValue(item, transformation.ColumnPath))
                .Where(value => value != null && ReflectionHelper.IsNumericValue(value))
                .Select(value => ConvertToDouble(value))
                .Where(value => !double.IsNaN(value))
                .ToList();

            if (numericValues.Count == 0)
                return Enumerable.Empty<object>();

            double average = numericValues.Average();

            return items.Where(item =>
            {
                var value = ReflectionHelper.GetPropValue(item, transformation.ColumnPath);
                if (value == null || !ReflectionHelper.IsNumericValue(value))
                    return false;

                double numericValue = ConvertToDouble(value);
                return !double.IsNaN(numericValue) && numericValue > average;
            });
        }

        /// <summary>
        /// Applies BelowAverage transformation - returns items with values below the column average
        /// </summary>
        private static IEnumerable<object> ApplyBelowAverage(List<object> items, DataTransformation transformation)
        {
            var numericValues = items
                .Select(item => ReflectionHelper.GetPropValue(item, transformation.ColumnPath))
                .Where(value => value != null && ReflectionHelper.IsNumericValue(value))
                .Select(value => ConvertToDouble(value))
                .Where(value => !double.IsNaN(value))
                .ToList();

            if (numericValues.Count == 0)
                return Enumerable.Empty<object>();

            double average = numericValues.Average();

            return items.Where(item =>
            {
                var value = ReflectionHelper.GetPropValue(item, transformation.ColumnPath);
                if (value == null || !ReflectionHelper.IsNumericValue(value))
                    return false;

                double numericValue = ConvertToDouble(value);
                return !double.IsNaN(numericValue) && numericValue < average;
            });
        }

        /// <summary>
        /// Applies Unique transformation - returns items where the column value appears only once
        /// </summary>
        private static IEnumerable<object> ApplyUnique(List<object> items, DataTransformation transformation)
        {
            return items
                .GroupBy(item => ReflectionHelper.GetPropValue(item, transformation.ColumnPath)?.ToString() ?? "")
                .Where(group => group.Count() == 1)
                .SelectMany(group => group);
        }

        /// <summary>
        /// Applies Duplicate transformation - returns items where the column value appears multiple times
        /// </summary>
        private static IEnumerable<object> ApplyDuplicate(List<object> items, DataTransformation transformation)
        {
            return items
                .GroupBy(item => ReflectionHelper.GetPropValue(item, transformation.ColumnPath)?.ToString() ?? "")
                .Where(group => group.Count() > 1)
                .SelectMany(group => group);
        }


        /// <summary>
        /// Converts a value to double for averaging calculations
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The value as a double, or NaN if conversion fails</returns>
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

        /// <summary>
        /// Converts a SearchType to the corresponding DataTransformationType
        /// </summary>
        /// <param name="searchType">The SearchType to convert</param>
        /// <returns>The corresponding DataTransformationType or None if not a transformation type</returns>
        public static DataTransformationType SearchTypeToTransformationType(SearchType searchType)
        {
            switch (searchType)
            {
                case SearchType.TopN:
                    return DataTransformationType.TopN;
                case SearchType.BottomN:
                    return DataTransformationType.BottomN;
                case SearchType.AboveAverage:
                    return DataTransformationType.AboveAverage;
                case SearchType.BelowAverage:
                    return DataTransformationType.BelowAverage;
                case SearchType.Unique:
                    return DataTransformationType.Unique;
                case SearchType.Duplicate:
                    return DataTransformationType.Duplicate;
                default:
                    return DataTransformationType.None;
            }
        }

        /// <summary>
        /// Determines if a SearchType represents a data transformation
        /// </summary>
        /// <param name="searchType">The SearchType to check</param>
        /// <returns>True if the SearchType is a data transformation</returns>
        public static bool IsTransformationType(SearchType searchType)
        {
            return SearchTypeToTransformationType(searchType) != DataTransformationType.None;
        }
    }
}