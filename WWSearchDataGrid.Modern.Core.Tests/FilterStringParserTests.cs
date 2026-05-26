using System.Linq;
using WWSearchDataGrid.Modern.Core;
using Xunit;

namespace WWSearchDataGrid.Modern.Core.Tests
{
    public class FilterStringParserTests
    {
        [Fact]
        public void Empty_input_returns_no_clauses_no_diagnostics()
        {
            var result = FilterStringParser.Parse(string.Empty);
            Assert.Empty(result.Clauses);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.IsFatal);
        }

        [Fact]
        public void Whitespace_input_returns_empty()
        {
            var result = FilterStringParser.Parse("   \t  ");
            Assert.Empty(result.Clauses);
            Assert.False(result.IsFatal);
        }

        [Fact]
        public void Simple_equality_with_string_literal()
        {
            var result = FilterStringParser.Parse("[Status] = 'Open'");
            Assert.False(result.IsFatal);
            Assert.Single(result.Clauses);
            var c = result.Clauses[0];
            Assert.Equal("Status", c.FieldName);
            Assert.Equal(SearchType.Equals, c.SearchType);
            Assert.Equal("Open", c.RawPrimary);
            Assert.Equal(0, c.GroupIndex);
            Assert.Null(c.Combinator);
        }

        [Fact]
        public void Doubled_quote_escape_inside_string()
        {
            var result = FilterStringParser.Parse("[Name] = 'O''Reilly'");
            Assert.Single(result.Clauses);
            Assert.Equal("O'Reilly", result.Clauses[0].RawPrimary);
        }

        [Fact]
        public void Between_consumes_two_operands_with_inner_And()
        {
            var result = FilterStringParser.Parse("[Qty] Between 5 And 10");
            Assert.False(result.IsFatal);
            Assert.Single(result.Clauses);
            var c = result.Clauses[0];
            Assert.Equal(SearchType.Between, c.SearchType);
            Assert.Equal("5", c.RawPrimary);
            Assert.Equal("10", c.RawSecondary);
        }

        [Fact]
        public void In_function_form_yields_IsAnyOf()
        {
            var result = FilterStringParser.Parse("[Region] In ('A','B','C')");
            Assert.Single(result.Clauses);
            var c = result.Clauses[0];
            Assert.Equal(SearchType.IsAnyOf, c.SearchType);
            Assert.NotNull(c.RawValues);
            Assert.Equal(3, c.RawValues.Count);
            Assert.Equal(new[] { "A", "B", "C" }, c.RawValues);
        }

        [Fact]
        public void Date_literal_with_hash_delimiters()
        {
            var result = FilterStringParser.Parse("[Created] > #2025-01-01#");
            Assert.Single(result.Clauses);
            var c = result.Clauses[0];
            Assert.Equal(SearchType.GreaterThan, c.SearchType);
            Assert.Equal("2025-01-01", c.RawPrimary);
        }

        [Fact]
        public void IsNull_function_form()
        {
            var result = FilterStringParser.Parse("IsNull([Notes])");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.IsNull, result.Clauses[0].SearchType);
            Assert.Equal("Notes", result.Clauses[0].FieldName);
        }

        [Fact]
        public void Suffix_Is_Null_form()
        {
            var result = FilterStringParser.Parse("[Notes] Is Null");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.IsNull, result.Clauses[0].SearchType);
        }

        [Fact]
        public void Suffix_Is_Not_Null_form()
        {
            var result = FilterStringParser.Parse("[Notes] Is Not Null");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.IsNotNull, result.Clauses[0].SearchType);
        }

        [Fact]
        public void Equals_Null_coerces_to_IsNull()
        {
            var result = FilterStringParser.Parse("[Notes] = Null");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.IsNull, result.Clauses[0].SearchType);
        }

        [Fact]
        public void Not_on_single_clause_inverts_search_type()
        {
            var result = FilterStringParser.Parse("Not [Status] = 'Open'");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.NotEquals, result.Clauses[0].SearchType);
        }

        [Fact]
        public void Not_on_IsNull_yields_IsNotNull()
        {
            var result = FilterStringParser.Parse("Not IsNull([Notes])");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.IsNotNull, result.Clauses[0].SearchType);
        }

        [Fact]
        public void Not_on_parenthesised_group_is_rejected()
        {
            var result = FilterStringParser.Parse("Not ([A] = 1 And [B] = 2)");
            Assert.True(result.IsFatal);
            Assert.Contains(result.Diagnostics, d => d.Message.Contains("Not"));
        }

        [Fact]
        public void IsOutlookIntervalToday_maps_to_Today_searchtype()
        {
            var result = FilterStringParser.Parse("IsOutlookIntervalToday([SalesDate])");
            Assert.Single(result.Clauses);
            Assert.Equal(SearchType.Today, result.Clauses[0].SearchType);
            Assert.Equal("SalesDate", result.Clauses[0].FieldName);
        }

        [Fact]
        public void IsOutlookIntervalYesterday_maps_to_Yesterday()
        {
            var result = FilterStringParser.Parse("IsOutlookIntervalYesterday([SalesDate])");
            Assert.Equal(SearchType.Yesterday, result.Clauses[0].SearchType);
        }

        [Fact]
        public void IsTomorrow_maps_to_DateInterval_with_Tomorrow()
        {
            var result = FilterStringParser.Parse("IsTomorrow([D])");
            Assert.Equal(SearchType.DateInterval, result.Clauses[0].SearchType);
            Assert.Equal(DateInterval.Tomorrow, result.Clauses[0].DateInterval);
        }

        [Fact]
        public void Contains_function_form_maps_with_value()
        {
            var result = FilterStringParser.Parse("Contains([Name], 'Smith')");
            Assert.Single(result.Clauses);
            var c = result.Clauses[0];
            Assert.Equal(SearchType.Contains, c.SearchType);
            Assert.Equal("Smith", c.RawPrimary);
        }

        [Fact]
        public void Precedence_And_binds_tighter_than_Or()
        {
            // [A] = 1 And [B] = 2 Or [C] = 3  →  (A And B) Or C
            // Group 0: [A, B] joined within by And; Group 1: [C] joined to Group 0 by Or.
            var result = FilterStringParser.Parse("[A] = 1 And [B] = 2 Or [C] = 3");
            Assert.False(result.IsFatal);
            Assert.Equal(3, result.Clauses.Count);

            Assert.Equal(0, result.Clauses[0].GroupIndex);
            Assert.Null(result.Clauses[0].Combinator);

            Assert.Equal(0, result.Clauses[1].GroupIndex);
            Assert.Equal("And", result.Clauses[1].Combinator);

            Assert.Equal(1, result.Clauses[2].GroupIndex);
            Assert.Equal("Or", result.Clauses[2].Combinator);
        }

        [Fact]
        public void UserExample_full_devexpress_style_normalises_to_two_groups()
        {
            const string filter =
                "([ModelPrice] >= 25000 And [ModelPrice] <= 80000) And (IsOutlookIntervalToday([SalesDate]) Or IsOutlookIntervalYesterday([SalesDate]))";

            var result = FilterStringParser.Parse(filter);
            Assert.False(result.IsFatal);
            Assert.Equal(4, result.Clauses.Count);

            // Group 0: ModelPrice >= 25000 And ModelPrice <= 80000
            Assert.Equal(0, result.Clauses[0].GroupIndex);
            Assert.Null(result.Clauses[0].Combinator);
            Assert.Equal(SearchType.GreaterThanOrEqualTo, result.Clauses[0].SearchType);
            Assert.Equal("ModelPrice", result.Clauses[0].FieldName);

            Assert.Equal(0, result.Clauses[1].GroupIndex);
            Assert.Equal("And", result.Clauses[1].Combinator);
            Assert.Equal(SearchType.LessThanOrEqualTo, result.Clauses[1].SearchType);

            // Group 1: Today Or Yesterday, joined to Group 0 by And
            Assert.Equal(1, result.Clauses[2].GroupIndex);
            Assert.Equal("And", result.Clauses[2].Combinator);
            Assert.Equal(SearchType.Today, result.Clauses[2].SearchType);

            Assert.Equal(1, result.Clauses[3].GroupIndex);
            Assert.Equal("Or", result.Clauses[3].Combinator);
            Assert.Equal(SearchType.Yesterday, result.Clauses[3].SearchType);
        }

        [Fact]
        public void Three_level_nesting_is_rejected()
        {
            // ((A Or B) And C) Or D  — needs three levels: inner Or, middle And, outer Or.
            var result = FilterStringParser.Parse("(([A] = 1 Or [B] = 2) And [C] = 3) Or [D] = 4");
            Assert.True(result.IsFatal);
            Assert.Contains(result.Diagnostics, d => d.Severity == FilterStringDiagnosticSeverity.Error);
        }

        [Fact]
        public void Parenthesised_uniform_inner_operator_collapses_to_one_group()
        {
            var result = FilterStringParser.Parse("([A] = 1 And [B] = 2)");
            Assert.False(result.IsFatal);
            Assert.Equal(2, result.Clauses.Count);
            Assert.All(result.Clauses, c => Assert.Equal(0, c.GroupIndex));
        }

        [Fact]
        public void Unterminated_string_emits_diagnostic()
        {
            var result = FilterStringParser.Parse("[Name] = 'unclosed");
            Assert.True(result.IsFatal);
        }

        [Fact]
        public void Mismatched_paren_emits_diagnostic()
        {
            var result = FilterStringParser.Parse("([A] = 1 And [B] = 2");
            Assert.True(result.IsFatal);
        }

        [Fact]
        public void Unknown_function_name_emits_diagnostic()
        {
            var result = FilterStringParser.Parse("ToUpper([A])");
            Assert.True(result.IsFatal);
            Assert.Contains(result.Diagnostics, d => d.Message.Contains("Unknown function"));
        }

        [Fact]
        public void Number_negation_in_operand()
        {
            var result = FilterStringParser.Parse("[Score] >= -5");
            Assert.Single(result.Clauses);
            Assert.Equal("-5", result.Clauses[0].RawPrimary);
        }

        [Fact]
        public void Shorthand_omitting_field_after_Or_is_rejected()
        {
            // Mirrors the chip-rendering rule: every search template must name its column.
            // "[Total] = 10000 Or = 2000" used to render misleadingly in the FilterPanel and
            // would also be parser-illegal — this test pins that down so a future "implicit
            // field reuse" extension has to revisit the chip-rendering contract first.
            var result = FilterStringParser.Parse("[Total] = 10000 Or = 2000");
            Assert.True(result.IsFatal);
            Assert.Contains(result.Diagnostics,
                d => d.Severity == FilterStringDiagnosticSeverity.Error
                  && d.Message.Contains("Expected field reference or function name"));
        }

        [Fact]
        public void Shorthand_omitting_field_after_And_is_rejected()
        {
            var result = FilterStringParser.Parse("[Total] = 10000 And > 2000");
            Assert.True(result.IsFatal);
            Assert.Contains(result.Diagnostics,
                d => d.Severity == FilterStringDiagnosticSeverity.Error
                  && d.Message.Contains("Expected field reference or function name"));
        }

        [Fact]
        public void Shorthand_bare_value_after_Or_is_rejected()
        {
            // Guards a tempting natural-language form: "[Total] = 10000 Or 2000".
            var result = FilterStringParser.Parse("[Total] = 10000 Or 2000");
            Assert.True(result.IsFatal);
            Assert.Contains(result.Diagnostics,
                d => d.Severity == FilterStringDiagnosticSeverity.Error
                  && d.Message.Contains("Expected field reference or function name"));
        }

        [Fact]
        public void Multiple_Or_at_top_level_each_become_a_group()
        {
            // [A] = 1 Or [B] = 2 Or [C] = 3  →  three groups joined by Or.
            var result = FilterStringParser.Parse("[A] = 1 Or [B] = 2 Or [C] = 3");
            Assert.False(result.IsFatal);
            Assert.Equal(3, result.Clauses.Count);
            Assert.Equal(0, result.Clauses[0].GroupIndex);
            Assert.Equal(1, result.Clauses[1].GroupIndex);
            Assert.Equal(2, result.Clauses[2].GroupIndex);
            Assert.Equal("Or", result.Clauses[1].Combinator);
            Assert.Equal("Or", result.Clauses[2].Combinator);
        }
    }
}
