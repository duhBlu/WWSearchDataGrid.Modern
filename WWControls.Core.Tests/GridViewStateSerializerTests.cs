using System.Collections.Generic;
using WWControls.Core;
using Xunit;

namespace WWControls.Core.Tests
{
    public class GridViewStateSerializerTests
    {
        private static GridViewState MakeFullSample()
        {
            return new GridViewState
            {
                Name = "My View",
                Layout = new GridLayoutState
                {
                    IsGroupPanelVisible = true,
                    Columns = new List<GridColumnLayout>
                    {
                        new GridColumnLayout { FieldName = "OrderNumber", DisplayIndex = 0, Width = 120.5, Visible = true, Fixed = "Left", SortOrder = "Ascending", SortIndex = 0 },
                        new GridColumnLayout { FieldName = "Amount", DisplayIndex = 1, Width = 90, Visible = true, Fixed = "None", SortOrder = "Descending", SortIndex = 1 },
                        new GridColumnLayout { FieldName = "Notes", DisplayIndex = 2, Visible = false, Fixed = "None", SortOrder = "None" },
                    },
                    Grouping = new List<GridGroupLayout>
                    {
                        new GridGroupLayout { FieldName = "Status", GroupInterval = "Value", SortDirection = "Ascending" },
                        new GridGroupLayout { FieldName = "OrderDate", GroupInterval = "DateMonth", SortDirection = "Descending" },
                    },
                },
                Filters = new GridFilterState
                {
                    Columns = new List<GridColumnFilter>
                    {
                        new GridColumnFilter
                        {
                            FieldName = "Amount",
                            ColumnDataType = ColumnDataType.Number,
                            Groups = new List<GridFilterGroup>
                            {
                                new GridFilterGroup
                                {
                                    Operator = "And",
                                    Conditions = new List<GridFilterCondition>
                                    {
                                        new GridFilterCondition { Operator = "And", SearchType = SearchType.Between, Primary = "10", Secondary = "20" },
                                        new GridFilterCondition { Operator = "Or", SearchType = SearchType.IsAnyOf, Values = new List<string> { "5", "7", "9" } },
                                    },
                                },
                            },
                        },
                        new GridColumnFilter
                        {
                            FieldName = "OrderDate",
                            ColumnDataType = ColumnDataType.DateTime,
                            Groups = new List<GridFilterGroup>
                            {
                                new GridFilterGroup
                                {
                                    Operator = "And",
                                    Conditions = new List<GridFilterCondition>
                                    {
                                        new GridFilterCondition { Operator = "And", SearchType = SearchType.DateInterval, Intervals = new List<string> { "Today", "LastWeek" } },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        [Fact]
        public void RoundTrip_FullView_IsLossless()
        {
            var state = MakeFullSample();

            var json1 = GridViewStateSerializer.Serialize(state);
            var restored = GridViewStateSerializer.Deserialize(json1);
            var json2 = GridViewStateSerializer.Serialize(restored);

            // Re-serializing the restored graph must reproduce the original JSON byte-for-byte.
            Assert.Equal(json1, json2);
        }

        [Fact]
        public void RoundTrip_FullView_PreservesKeyFields()
        {
            var restored = GridViewStateSerializer.Deserialize(GridViewStateSerializer.Serialize(MakeFullSample()));

            Assert.Equal(GridViewState.CurrentSchemaVersion, restored.SchemaVersion);
            Assert.Equal("My View", restored.Name);

            Assert.Equal(3, restored.Layout.Columns.Count);
            Assert.True(restored.Layout.IsGroupPanelVisible);
            var amount = restored.Layout.Columns[1];
            Assert.Equal("Amount", amount.FieldName);
            Assert.Equal(90d, amount.Width);
            Assert.Equal("Descending", amount.SortOrder);
            Assert.Equal(1, amount.SortIndex);
            Assert.False(restored.Layout.Columns[2].Visible);

            Assert.Equal(2, restored.Layout.Grouping.Count);
            Assert.Equal("OrderDate", restored.Layout.Grouping[1].FieldName);
            Assert.Equal("DateMonth", restored.Layout.Grouping[1].GroupInterval);

            var amountFilter = restored.Filters.Columns[0];
            Assert.Equal(ColumnDataType.Number, amountFilter.ColumnDataType);
            Assert.Equal(SearchType.Between, amountFilter.Groups[0].Conditions[0].SearchType);
            Assert.Equal("20", amountFilter.Groups[0].Conditions[0].Secondary);
            Assert.Equal(new[] { "5", "7", "9" }, amountFilter.Groups[0].Conditions[1].Values);

            var dateFilter = restored.Filters.Columns[1];
            Assert.Equal(SearchType.DateInterval, dateFilter.Groups[0].Conditions[0].SearchType);
            Assert.Equal(new[] { "Today", "LastWeek" }, dateFilter.Groups[0].Conditions[0].Intervals);
        }

        [Fact]
        public void RoundTrip_FiltersOnly_LeavesLayoutNull()
        {
            var state = MakeFullSample();
            state.Layout = null;

            var restored = GridViewStateSerializer.Deserialize(GridViewStateSerializer.Serialize(state));

            Assert.Null(restored.Layout);
            Assert.NotNull(restored.Filters);
        }

        [Fact]
        public void RoundTrip_LayoutOnly_LeavesFiltersNull()
        {
            var state = MakeFullSample();
            state.Filters = null;

            var restored = GridViewStateSerializer.Deserialize(GridViewStateSerializer.Serialize(state));

            Assert.Null(restored.Filters);
            Assert.NotNull(restored.Layout);
        }

        [Fact]
        public void Serialize_OmitsNullSections()
        {
            var state = MakeFullSample();
            state.Layout = null;

            var json = GridViewStateSerializer.Serialize(state);

            Assert.DoesNotContain("\"Layout\"", json);
            Assert.Contains("\"Filters\"", json);
        }

        [Fact]
        public void Serialize_WritesEnumsAsNames_NotOrdinals()
        {
            var json = GridViewStateSerializer.Serialize(MakeFullSample());

            Assert.Contains("\"SearchType\": \"Between\"", json);
            Assert.Contains("\"ColumnDataType\": \"Number\"", json);
        }

        [Fact]
        public void Deserialize_IsCaseInsensitive()
        {
            var restored = GridViewStateSerializer.Deserialize("{ \"schemaversion\": 1, \"name\": \"lower\" }");

            Assert.Equal("lower", restored.Name);
            Assert.Equal(1, restored.SchemaVersion);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deserialize_NullOrBlank_ReturnsNull(string input)
        {
            Assert.Null(GridViewStateSerializer.Deserialize(input));
        }
    }
}
