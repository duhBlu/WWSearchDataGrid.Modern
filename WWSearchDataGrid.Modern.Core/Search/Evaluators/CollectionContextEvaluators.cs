using System;
using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Evaluator for TopN search type - returns the top N items by value
    /// </summary>
    internal class TopNEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.TopN;
        public override bool RequiresCollectionContext => true;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            throw new InvalidOperationException("TopN evaluator requires collection context. Use the overload with CollectionContext.");
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            if (collectionContext == null)
                return false;

            try
            {
                int count = GetTopNCount(searchCondition);
                if (count <= 0)
                    return false;

                var topNItems = collectionContext.GetSortedDescending().Take(count);
                return topNItems.Contains(GetItemContainingValue(columnValue, collectionContext));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TopN evaluation: {ex.Message}");
                return false;
            }
        }

        private int GetTopNCount(SearchCondition searchCondition)
        {
            if (int.TryParse(searchCondition.StringValue, out int count))
                return count;
            return 0;
        }

        private object GetItemContainingValue(object columnValue, CollectionContext collectionContext)
        {
            return collectionContext.Items
                .FirstOrDefault(item => 
                {
                    var itemValue = ReflectionHelper.GetPropValue(item, collectionContext.ColumnPath);
                    return object.Equals(itemValue, columnValue);
                });
        }
    }

    /// <summary>
    /// Evaluator for BottomN search type - returns the bottom N items by value
    /// </summary>
    internal class BottomNEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.BottomN;
        public override bool RequiresCollectionContext => true;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            throw new InvalidOperationException("BottomN evaluator requires collection context. Use the overload with CollectionContext.");
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            if (collectionContext == null)
                return false;

            try
            {
                int count = GetBottomNCount(searchCondition);
                if (count <= 0)
                    return false;

                var bottomNItems = collectionContext.GetSortedAscending().Take(count);
                return bottomNItems.Contains(GetItemContainingValue(columnValue, collectionContext));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in BottomN evaluation: {ex.Message}");
                return false;
            }
        }

        private int GetBottomNCount(SearchCondition searchCondition)
        {
            if (int.TryParse(searchCondition.StringValue, out int count))
                return count;
            return 0;
        }

        private object GetItemContainingValue(object columnValue, CollectionContext collectionContext)
        {
            return collectionContext.Items
                .FirstOrDefault(item => 
                {
                    var itemValue = ReflectionHelper.GetPropValue(item, collectionContext.ColumnPath);
                    return object.Equals(itemValue, columnValue);
                });
        }
    }

    /// <summary>
    /// Evaluator for AboveAverage search type - returns items with values above the column average
    /// </summary>
    internal class AboveAverageEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.AboveAverage;
        public override bool RequiresCollectionContext => true;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            throw new InvalidOperationException("AboveAverage evaluator requires collection context. Use the overload with CollectionContext.");
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            if (collectionContext == null || columnValue == null)
                return false;

            try
            {
                var average = collectionContext.GetAverage();
                if (!average.HasValue)
                    return false;

                if (!ReflectionHelper.IsNumericValue(columnValue))
                    return false;

                double numericValue = ConvertToDouble(columnValue);
                return !double.IsNaN(numericValue) && numericValue > average.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AboveAverage evaluation: {ex.Message}");
                return false;
            }
        }

        private static double ConvertToDouble(object value)
        {
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

    /// <summary>
    /// Evaluator for BelowAverage search type - returns items with values below the column average
    /// </summary>
    internal class BelowAverageEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.BelowAverage;
        public override bool RequiresCollectionContext => true;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            throw new InvalidOperationException("BelowAverage evaluator requires collection context. Use the overload with CollectionContext.");
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            if (collectionContext == null || columnValue == null)
                return false;

            try
            {
                var average = collectionContext.GetAverage();
                if (!average.HasValue)
                    return false;

                if (!ReflectionHelper.IsNumericValue(columnValue))
                    return false;

                double numericValue = ConvertToDouble(columnValue);
                return !double.IsNaN(numericValue) && numericValue < average.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in BelowAverage evaluation: {ex.Message}");
                return false;
            }
        }

        private static double ConvertToDouble(object value)
        {
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

    /// <summary>
    /// Evaluator for Unique search type - returns items where the column value appears only once
    /// </summary>
    internal class UniqueEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Unique;
        public override bool RequiresCollectionContext => true;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            throw new InvalidOperationException("Unique evaluator requires collection context. Use the overload with CollectionContext.");
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            if (collectionContext == null)
                return false;

            try
            {
                var valueGroups = collectionContext.GetValueGroups();
                string columnValueString = columnValue?.ToString() ?? string.Empty;
                
                return valueGroups.TryGetValue(columnValueString, out var items) && items.Count == 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Unique evaluation: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Evaluator for Duplicate search type - returns items where the column value appears multiple times
    /// </summary>
    internal class DuplicateEvaluator : SearchEvaluatorBase
    {
        public override SearchType SearchType => SearchType.Duplicate;
        public override bool RequiresCollectionContext => true;

        public override bool Evaluate(object columnValue, SearchCondition searchCondition)
        {
            throw new InvalidOperationException("Duplicate evaluator requires collection context. Use the overload with CollectionContext.");
        }

        public override bool Evaluate(object columnValue, SearchCondition searchCondition, CollectionContext collectionContext)
        {
            if (collectionContext == null)
                return false;

            try
            {
                var valueGroups = collectionContext.GetValueGroups();
                string columnValueString = columnValue?.ToString() ?? string.Empty;
                
                return valueGroups.TryGetValue(columnValueString, out var items) && items.Count > 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Duplicate evaluation: {ex.Message}");
                return false;
            }
        }
    }
}