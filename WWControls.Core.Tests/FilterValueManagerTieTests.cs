using System;
using System.Collections.Generic;
using System.Linq;
using WWControls.Core;
using WWControls.Core.Display;
using Xunit;
using Xunit.Abstractions;

namespace WWControls.Core.Tests
{
    /// <summary>
    /// Reproduction harness for the "(no values)" chip bug reported in the Filter Values tab:
    /// "whenever I select as many selected values as unselected it shows (no values)".
    /// Drives FilterValueManager exactly like the checkbox UI does and inspects the chip text
    /// produced by SearchTemplateController.GetTemplateComponents.
    /// </summary>
    public class FilterValueManagerTieTests
    {
        private readonly ITestOutputHelper _out;

        public FilterValueManagerTieTests(ITestOutputHelper output) => _out = output;

        private static SearchTemplateController MakeController(IEnumerable<object> rawValues)
        {
            var controller = new SearchTemplateController { ColumnName = "Col" };
            controller.SetColumnValues(rawValues);
            controller.EnsureColumnValuesLoadedForFiltering();
            return controller;
        }

        private string DescribeChips(SearchTemplateController controller)
        {
            var parts = new List<string>();
            foreach (var group in controller.SearchGroups)
            {
                foreach (var t in group.SearchTemplates)
                {
                    var c = controller.GetTemplateComponents(t);
                    parts.Add($"[{c.Operator}] {c.SearchTypeText} {c.PrimaryValue}".Trim());
                }
            }
            return string.Join("  ", parts);
        }

        private bool AnyChipShowsNoValues(SearchTemplateController controller)
        {
            foreach (var group in controller.SearchGroups)
                foreach (var t in group.SearchTemplates)
                {
                    var c = controller.GetTemplateComponents(t);
                    if ((c.PrimaryValue ?? "").Contains("no values"))
                        return true;
                }
            return false;
        }

        // Build datasets: N distinct non-null values, each appearing twice, optional nulls.
        public static IEnumerable<object[]> Scenarios()
        {
            // distinctCount, includeNulls
            for (int n = 2; n <= 8; n++)
            {
                yield return new object[] { n, false };
                yield return new object[] { n, true };
            }
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void Toggling_to_every_checked_count_never_shows_no_values(int distinctCount, bool includeNulls)
        {
            var raw = new List<object>();
            for (int i = 0; i < distinctCount; i++)
            {
                raw.Add("V" + i);
                raw.Add("V" + i); // count 2 each
            }
            if (includeNulls)
            {
                raw.Add(null);
                raw.Add(null);
            }

            // For every possible number of checked NON-blank values, drive the manager and inspect.
            for (int checkedTarget = 0; checkedTarget <= distinctCount; checkedTarget++)
            {
                var controller = MakeController(raw);
                var fvm = new FilterValueManager();
                fvm.Initialize(controller, raw.Count, applyFilterAction: null);

                // Start: all checked (no filter). Now set the desired final state, one toggle at a time
                // (each set raises PropertyChanged -> SyncToRulesAndApply, exactly like the UI).
                var nonBlank = fvm.ValueItems.Where(v => !v.IsBlank).ToList();
                for (int i = 0; i < nonBlank.Count; i++)
                    nonBlank[i].IsChecked = i < checkedTarget;

                // Leave the (Blank) row checked (default) so we cover the "blank checked" path.

                int checkedCount = fvm.ValueItems.Count(v => v.IsChecked);
                int uncheckedCount = fvm.ValueItems.Count(v => !v.IsChecked);
                var chips = DescribeChips(controller);

                _out.WriteLine($"distinct={distinctCount} nulls={includeNulls} checkedNonBlank={checkedTarget} " +
                               $"checkedTotal={checkedCount} uncheckedTotal={uncheckedCount} => {chips}");

                Assert.False(AnyChipShowsNoValues(controller),
                    $"'(no values)' chip at distinct={distinctCount} nulls={includeNulls} " +
                    $"checkedNonBlank={checkedTarget} (checkedTotal={checkedCount}, uncheckedTotal={uncheckedCount}): {chips}");
            }
        }

        // DateTime datasets with different shapes: fully contiguous (consecutive days),
        // scattered (every other day), and a mix. The contiguous-range logic in
        // BuildDateTimeTemplates is the most intricate branch, so probe each shape at every
        // checked count, including exact ties.
        public static IEnumerable<object[]> DateScenarios()
        {
            foreach (var shape in new[] { "contiguous", "scattered", "mixed" })
                for (int n = 2; n <= 8; n++)
                    yield return new object[] { n, shape };
        }

        [Theory]
        [MemberData(nameof(DateScenarios))]
        public void DateTime_toggling_to_every_checked_count_never_shows_no_values(int distinctCount, string shape)
        {
            var baseDate = new DateTime(2024, 1, 1);
            var dates = new List<DateTime>();
            for (int i = 0; i < distinctCount; i++)
            {
                int dayOffset = shape switch
                {
                    "contiguous" => i,        // 1,2,3,4...
                    "scattered" => i * 3,     // gaps everywhere
                    _ => (i % 2 == 0) ? i : i + 10, // mixed clusters
                };
                dates.Add(baseDate.AddDays(dayOffset));
            }

            var raw = new List<object>();
            foreach (var d in dates) { raw.Add(d); raw.Add(d); }

            for (int checkedTarget = 0; checkedTarget <= distinctCount; checkedTarget++)
            {
                var controller = MakeController(raw.Cast<object>());
                Assert.Equal(ColumnDataType.DateTime, controller.ColumnDataType);

                var fvm = new FilterValueManager();
                fvm.Initialize(controller, raw.Count, applyFilterAction: null);

                // Toggle the flat value items (sorted ascending by RawValue for determinism).
                var nonBlank = fvm.ValueItems.Where(v => !v.IsBlank)
                    .OrderBy(v => (DateTime)v.RawValue).ToList();
                for (int i = 0; i < nonBlank.Count; i++)
                    nonBlank[i].IsChecked = i < checkedTarget;

                var chips = DescribeChips(controller);
                _out.WriteLine($"date shape={shape} distinct={distinctCount} checked={checkedTarget} => {chips}");

                Assert.False(AnyChipShowsNoValues(controller),
                    $"DateTime '(no values)' at shape={shape} distinct={distinctCount} checked={checkedTarget}: {chips}");
            }
        }

        /// <summary>
        /// Mimics ComboBoxLookupDisplayProvider (the WPF Priority-column provider): the raw cell
        /// value is a foreign-key id (int) and the display text is looked up by matching that id.
        /// The match uses object Equals, so a stringified id ("1") never matches the int key (1).
        /// </summary>
        private sealed class IdLookupDisplayProvider : IDisplayValueProvider
        {
            private readonly Dictionary<object, string> _byId;
            public IdLookupDisplayProvider(Dictionary<object, string> byId) => _byId = byId;

            public string FormatValue(object rawValue)
            {
                if (rawValue == null) return string.Empty;
                foreach (var kvp in _byId)
                    if (Equals(kvp.Key, rawValue)) // type-sensitive: int 1 != "1"
                        return kvp.Value;
                return string.Empty;
            }

            public object ParseValue(string displayText) => null;
            public bool CanParse => false;
            public bool UseRawComparison => false;
        }

        [Fact]
        public void IsAnyOf_SearchCondition_does_not_carry_multi_values()
        {
            // Documents the trap behind the "all rows filtered out" bug: the SearchCondition
            // property only carries SelectedValue (singular), which is null for IsAnyOf. The
            // display-aware row evaluator used that condition, so IsAnyOfEvaluator saw a null
            // RawPrimaryValue and returned false for every row.
            var template = new SearchTemplate(ColumnDataType.Number) { SearchType = SearchType.IsAnyOf };
            template.SelectedValues.Add(new SelectableValueItem(1));
            template.SelectedValues.Add(new SelectableValueItem(2));

            Assert.Null(template.SearchCondition.RawPrimaryValue);
            Assert.False(SearchEngine.EvaluateCondition(1, template.SearchCondition));
        }

        [Fact]
        public void IsAnyOf_compiled_expression_matches_typed_values_by_string_form()
        {
            // The working path (no display provider): the compiled expression compares the cell
            // value's string form against the stored value strings. The display-aware evaluator
            // must mirror exactly this so lookup columns filter the same way.
            var template = new SearchTemplate(ColumnDataType.Number) { SearchType = SearchType.IsAnyOf };
            template.SelectedValues.Add(new SelectableValueItem(1));
            template.SelectedValues.Add(new SelectableValueItem(2));

            var predicate = template.BuildExpression(typeof(int)).Compile();

            Assert.True(predicate(1));
            Assert.True(predicate(2));
            Assert.False(predicate(3));
            Assert.False(predicate(null));

            var noneTemplate = new SearchTemplate(ColumnDataType.Number) { SearchType = SearchType.IsNoneOf };
            noneTemplate.SelectedValues.Add(new SelectableValueItem(1));
            noneTemplate.SelectedValues.Add(new SelectableValueItem(2));
            var nonePredicate = noneTemplate.BuildExpression(typeof(int)).Compile();

            Assert.False(nonePredicate(1));
            Assert.True(nonePredicate(3));
            Assert.True(nonePredicate(null)); // IsNoneOf admits null
        }

        [Fact]
        public void Priority_style_lookup_column_renders_names_for_multi_value_chips()
        {
            // 4 distinct integer ids, each appearing a few times — like the Priority column.
            var raw = new List<object> { 1, 1, 1, 2, 2, 3, 3, 3, 4, 4 };
            var names = new Dictionary<object, string> { { 1, "Low" }, { 2, "Medium" }, { 3, "High" }, { 4, "Critical" } };

            for (int checkedTarget = 0; checkedTarget <= 4; checkedTarget++)
            {
                var controller = MakeController(raw);
                controller.DisplayValueProvider = new IdLookupDisplayProvider(names);

                var fvm = new FilterValueManager();
                fvm.Initialize(controller, raw.Count, applyFilterAction: null);

                var nonBlank = fvm.ValueItems.Where(v => !v.IsBlank)
                    .OrderBy(v => (int)v.RawValue).ToList();
                for (int i = 0; i < nonBlank.Count; i++)
                    nonBlank[i].IsChecked = i < checkedTarget;

                var chips = DescribeChips(controller);
                _out.WriteLine($"priority checked={checkedTarget} => {chips}");

                Assert.False(AnyChipShowsNoValues(controller),
                    $"Priority-style '(no values)' at checked={checkedTarget}: {chips}");
            }
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void Roundtrip_sync_from_rules_then_back_never_shows_no_values(int distinctCount, bool includeNulls)
        {
            var raw = new List<object>();
            for (int i = 0; i < distinctCount; i++)
            {
                raw.Add("V" + i);
                raw.Add("V" + i);
            }
            if (includeNulls)
            {
                raw.Add(null);
                raw.Add(null);
            }

            for (int checkedTarget = 0; checkedTarget <= distinctCount; checkedTarget++)
            {
                var controller = MakeController(raw);
                var fvm = new FilterValueManager();
                fvm.Initialize(controller, raw.Count, applyFilterAction: null);

                var nonBlank = fvm.ValueItems.Where(v => !v.IsBlank).ToList();
                for (int i = 0; i < nonBlank.Count; i++)
                    nonBlank[i].IsChecked = i < checkedTarget;

                // Simulate switching away and back to the Values tab: re-derive checkboxes from
                // the rules, then push them back to rules again.
                fvm.SyncFromRules();
                fvm.SyncToRules();

                var chips = DescribeChips(controller);
                _out.WriteLine($"distinct={distinctCount} nulls={includeNulls} checkedNonBlank={checkedTarget} => {chips}");

                Assert.False(AnyChipShowsNoValues(controller),
                    $"roundtrip '(no values)' at distinct={distinctCount} nulls={includeNulls} checkedNonBlank={checkedTarget}: {chips}");
            }
        }
    }
}
