using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;
using Xunit;

namespace WWSearchDataGrid.Modern.Core.Tests
{
    public class SummaryCalculatorTests
    {
        private sealed class Row
        {
            public decimal? Total { get; set; }
            public string Name { get; set; }
            public DateTime? Date { get; set; }
        }

        [Fact]
        public void Count_counts_every_row_including_nulls()
        {
            var values = new List<object> { 1m, null, 3m };
            Assert.Equal(3, SummaryCalculator.Compute(SummaryItemType.Count, values));
        }

        [Fact]
        public void Sum_skips_nulls_and_non_numerics()
        {
            var values = new List<object> { 10m, null, "text", 2.5m };
            Assert.Equal(12.5m, SummaryCalculator.Compute(SummaryItemType.Sum, values));
        }

        [Fact]
        public void Sum_returns_null_when_no_numeric_values()
        {
            var values = new List<object> { null, "a", "b" };
            Assert.Null(SummaryCalculator.Compute(SummaryItemType.Sum, values));
        }

        [Fact]
        public void Sum_mixes_numeric_widths()
        {
            var values = new List<object> { 1, 2L, 3.0, 4.5f, 5m };
            Assert.Equal(15.5m, SummaryCalculator.Compute(SummaryItemType.Sum, values));
        }

        [Fact]
        public void Sum_falls_back_to_double_on_decimal_overflow()
        {
            var values = new List<object> { double.MaxValue / 2, double.MaxValue / 2 };
            var result = SummaryCalculator.Compute(SummaryItemType.Sum, values);
            Assert.IsType<double>(result);
            Assert.Equal(double.MaxValue, (double)result, 5);
        }

        [Fact]
        public void Average_divides_by_non_null_count_only()
        {
            var values = new List<object> { 10m, null, 20m };
            Assert.Equal(15m, SummaryCalculator.Compute(SummaryItemType.Average, values));
        }

        [Fact]
        public void Min_and_Max_compare_dates()
        {
            var early = new DateTime(2024, 1, 1);
            var late = new DateTime(2026, 6, 1);
            var values = new List<object> { late, null, early };
            Assert.Equal(early, SummaryCalculator.Compute(SummaryItemType.Min, values));
            Assert.Equal(late, SummaryCalculator.Compute(SummaryItemType.Max, values));
        }

        [Fact]
        public void Min_and_Max_compare_mixed_width_numerics()
        {
            var values = new List<object> { 5, 2.5, 10m };
            Assert.Equal(2.5, SummaryCalculator.Compute(SummaryItemType.Min, values));
            Assert.Equal(10m, SummaryCalculator.Compute(SummaryItemType.Max, values));
        }

        [Fact]
        public void Extremum_of_all_nulls_is_null()
        {
            var values = new List<object> { null, null };
            Assert.Null(SummaryCalculator.Compute(SummaryItemType.Min, values));
        }

        [Fact]
        public void ExtractValues_reads_property_path_per_row()
        {
            var rows = new[]
            {
                new Row { Total = 1m },
                new Row { Total = null },
                new Row { Total = 3m },
            };

            var values = SummaryCalculator.ExtractValues(rows, nameof(Row.Total));
            Assert.Equal(new object[] { 1m, null, 3m }, values);
        }

        [Theory]
        [InlineData(SummaryItemType.Count, typeof(string), true)]
        [InlineData(SummaryItemType.Sum, typeof(string), false)]
        [InlineData(SummaryItemType.Sum, typeof(decimal?), true)]
        [InlineData(SummaryItemType.Average, typeof(int), true)]
        [InlineData(SummaryItemType.Min, typeof(DateTime), true)]
        [InlineData(SummaryItemType.Max, typeof(string), true)]
        [InlineData(SummaryItemType.Sum, null, false)]
        [InlineData(SummaryItemType.Count, null, true)]
        public void IsTypeSupported_gates_by_field_type(SummaryItemType type, Type fieldType, bool expected)
        {
            Assert.Equal(expected, SummaryCalculator.IsTypeSupported(type, fieldType));
        }

        [Fact]
        public void CompareValues_sorts_nulls_first()
        {
            Assert.Equal(0, SummaryCalculator.CompareValues(null, null));
            Assert.True(SummaryCalculator.CompareValues(null, 1m) < 0);
            Assert.True(SummaryCalculator.CompareValues(1m, null) > 0);
        }

        [Fact]
        public void CompareValues_compares_numerics_across_widths()
        {
            Assert.True(SummaryCalculator.CompareValues(2, 1.5m) > 0);
            Assert.True(SummaryCalculator.CompareValues(1.5f, 2L) < 0);
            Assert.Equal(0, SummaryCalculator.CompareValues(3.0, 3m));
        }

        [Fact]
        public void CompareValues_compares_same_type_comparables_directly()
        {
            Assert.True(SummaryCalculator.CompareValues(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1)) > 0);
            Assert.True(SummaryCalculator.CompareValues("apple", "banana") < 0);
        }
    }
}
