using System;
using System.Collections;
using System.Collections.Generic;

namespace WWControls.Core
{
    /// <summary>
    /// Computes column aggregates for summary rows. <see cref="SummaryItemType.Count"/> counts
    /// every row; the value aggregates skip nulls. Sum / Average accumulate in decimal and fall
    /// back to double on overflow, returning null when no numeric values exist. Min / Max compare
    /// numerics across widths, same-type <see cref="IComparable"/> values directly, and mixed
    /// types by their string form (matching the grouping engine's key comparer).
    /// </summary>
    public static class SummaryCalculator
    {
        /// <summary>
        /// Extracts one value per row at <paramref name="propertyPath"/> (nulls included, so
        /// Count sees every row). The list is intended to be extracted once per column and
        /// shared across that column's summary items.
        /// </summary>
        public static List<object> ExtractValues(IEnumerable rows, string propertyPath)
        {
            var values = new List<object>();
            if (rows == null || string.IsNullOrEmpty(propertyPath))
                return values;

            foreach (var row in rows)
                values.Add(ReflectionHelper.GetPropValue(row, propertyPath));
            return values;
        }

        /// <summary>
        /// Computes one aggregate over <paramref name="values"/> (one entry per row, nulls
        /// included). Returns null when the aggregate is undefined for the data — e.g. Sum over
        /// a column with no numeric values, or Min over all-null rows.
        /// </summary>
        public static object Compute(SummaryItemType type, IReadOnlyList<object> values)
        {
            if (values == null) return null;

            switch (type)
            {
                case SummaryItemType.Count:
                    return values.Count;

                case SummaryItemType.Sum:
                    int _;
                    return ComputeSum(values, out _);

                case SummaryItemType.Average:
                    int numericCount;
                    var sum = ComputeSum(values, out numericCount);
                    if (sum == null || numericCount == 0) return null;
                    if (sum is double d) return d / numericCount;
                    return (decimal)sum / numericCount;

                case SummaryItemType.Min:
                    return ComputeExtremum(values, max: false);

                case SummaryItemType.Max:
                    return ComputeExtremum(values, max: true);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether <paramref name="type"/> is computable for a column of
        /// <paramref name="fieldType"/>: Count always; Sum / Average need a numeric type;
        /// Min / Max need numeric or <see cref="IComparable"/>. A null
        /// <paramref name="fieldType"/> (unresolved column) gates everything but Count off.
        /// </summary>
        public static bool IsTypeSupported(SummaryItemType type, Type fieldType)
        {
            if (type == SummaryItemType.Count) return true;
            if (fieldType == null) return false;

            var underlying = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            if (type == SummaryItemType.Sum || type == SummaryItemType.Average)
                return ReflectionHelper.IsNumericType(underlying);

            return ReflectionHelper.IsNumericType(underlying)
                || typeof(IComparable).IsAssignableFrom(underlying);
        }

        private static object ComputeSum(IReadOnlyList<object> values, out int numericCount)
        {
            numericCount = 0;
            try
            {
                decimal acc = 0m;
                bool any = false;
                for (int i = 0; i < values.Count; i++)
                {
                    var v = values[i];
                    if (v == null || !ReflectionHelper.IsNumericValue(v)) continue;
                    acc += Convert.ToDecimal(v);
                    numericCount++;
                    any = true;
                }
                return any ? (object)acc : null;
            }
            catch (OverflowException)
            {
                // A value (or the running total) exceeded decimal's range — redo in double.
                numericCount = 0;
                double acc = 0;
                bool any = false;
                for (int i = 0; i < values.Count; i++)
                {
                    var v = values[i];
                    if (v == null || !ReflectionHelper.IsNumericValue(v)) continue;
                    acc += Convert.ToDouble(v);
                    numericCount++;
                    any = true;
                }
                return any ? (object)acc : null;
            }
        }

        private static object ComputeExtremum(IReadOnlyList<object> values, bool max)
        {
            object best = null;
            for (int i = 0; i < values.Count; i++)
            {
                var v = values[i];
                if (v == null || v is DBNull) continue;
                if (best == null)
                {
                    best = v;
                    continue;
                }

                int cmp = CompareValues(v, best);
                if (max ? cmp > 0 : cmp < 0)
                    best = v;
            }
            return best;
        }

        /// <summary>
        /// Compares two aggregate values the way Min / Max rank them: numerics across widths
        /// (decimal, double on overflow), same-type <see cref="IComparable"/> values directly,
        /// mixed types by their string form. Nulls sort first.
        /// </summary>
        public static int CompareValues(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (ReflectionHelper.IsNumericValue(x) && ReflectionHelper.IsNumericValue(y))
            {
                try
                {
                    return Convert.ToDecimal(x).CompareTo(Convert.ToDecimal(y));
                }
                catch (OverflowException)
                {
                    return Convert.ToDouble(x).CompareTo(Convert.ToDouble(y));
                }
            }

            if (x is IComparable cx && x.GetType() == y.GetType())
                return cx.CompareTo(y);

            return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
        }
    }
}
