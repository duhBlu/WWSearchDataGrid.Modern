using System.Collections.ObjectModel;
using System.Linq.Expressions;
using WWControls.Core;
using Xunit;

namespace WWControls.Core.Tests
{
    public class LogicalOperatorTests
    {
        [Theory]
        [InlineData("And", LogicalOperator.And)]
        [InlineData("and", LogicalOperator.And)]
        [InlineData("Or", LogicalOperator.Or)]
        [InlineData("OR", LogicalOperator.Or)]
        [InlineData("NotAnd", LogicalOperator.NotAnd)]
        [InlineData("notand", LogicalOperator.NotAnd)]
        [InlineData("NotOr", LogicalOperator.NotOr)]
        [InlineData("NOTOR", LogicalOperator.NotOr)]
        [InlineData("Bogus", LogicalOperator.And)]
        [InlineData("", LogicalOperator.And)]
        [InlineData(null, LogicalOperator.And)]
        public void Parse_handles_known_tokens_and_falls_back_to_And(string token, LogicalOperator expected)
        {
            Assert.Equal(expected, LogicalOperatorExtensions.Parse(token));
        }

        [Theory]
        [InlineData(LogicalOperator.And, "And")]
        [InlineData(LogicalOperator.Or, "Or")]
        [InlineData(LogicalOperator.NotAnd, "Not And")]
        [InlineData(LogicalOperator.NotOr, "Not Or")]
        public void ToTokenString_matches_persisted_form(LogicalOperator op, string expected)
        {
            Assert.Equal(expected, op.ToTokenString());
        }

        [Theory]
        [InlineData(LogicalOperator.And, ExpressionType.AndAlso)]
        [InlineData(LogicalOperator.NotAnd, ExpressionType.AndAlso)]
        [InlineData(LogicalOperator.Or, ExpressionType.OrElse)]
        [InlineData(LogicalOperator.NotOr, ExpressionType.OrElse)]
        public void InnerComposer_returns_correct_combiner(LogicalOperator op, ExpressionType expected)
        {
            var a = Expression.Constant(true);
            var b = Expression.Constant(true);
            Assert.Equal(expected, op.InnerComposer()(a, b).NodeType);
        }

        [Theory]
        [InlineData(LogicalOperator.And, false)]
        [InlineData(LogicalOperator.Or, false)]
        [InlineData(LogicalOperator.NotAnd, true)]
        [InlineData(LogicalOperator.NotOr, true)]
        public void IsNegated_marks_only_the_Not_variants(LogicalOperator op, bool expected)
        {
            Assert.Equal(expected, op.IsNegated());
        }

        [Theory]
        [InlineData(LogicalOperator.And, "And")]
        [InlineData(LogicalOperator.Or, "Or")]
        [InlineData(LogicalOperator.NotAnd, "Not And")]
        [InlineData(LogicalOperator.NotOr, "Not Or")]
        public void DisplayText_matches_chip_label(LogicalOperator op, string expected)
        {
            Assert.Equal(expected, op.DisplayText());
        }

        [Fact]
        public void SearchTemplateGroup_exposes_ChildGroups_collection()
        {
            var group = new SearchTemplateGroup();
            Assert.NotNull(group.ChildGroups);
            Assert.Empty(group.ChildGroups);
            group.ChildGroups.Add(new SearchTemplateGroup());
            Assert.Single(group.ChildGroups);
        }

        [Theory]
        [InlineData("And", false)]
        [InlineData("Or", false)]
        [InlineData("NotAnd", true)]
        [InlineData("notor", true)]
        public void SearchTemplateGroup_IsNegated_reflects_OperatorName(string token, bool expected)
        {
            var group = new SearchTemplateGroup { OperatorName = token };
            Assert.Equal(expected, group.IsNegated);
        }

        [Theory]
        [InlineData("And", ExpressionType.AndAlso)]
        [InlineData("NotAnd", ExpressionType.AndAlso)]
        [InlineData("Or", ExpressionType.OrElse)]
        [InlineData("NotOr", ExpressionType.OrElse)]
        public void SearchTemplateGroup_OperatorFunction_uses_inner_combiner(string token, ExpressionType expected)
        {
            var group = new SearchTemplateGroup { OperatorName = token };
            var a = Expression.Constant(true);
            var b = Expression.Constant(true);
            Assert.Equal(expected, group.OperatorFunction(a, b).NodeType);
        }

        [Fact]
        public void NotAnd_group_wraps_combined_body_in_Expression_Not()
        {
            // NOT(value contains "abc" AND value contains "xyz") — true unless both substrings are present.
            // SearchTemplate's default per-template OperatorName is "Or"; the inner combiner for a
            // NotAnd group must be explicitly AND on the non-leading template.
            var group = new SearchTemplateGroup { OperatorName = "NotAnd" };
            group.SearchTemplates.Add(MakeContainsTemplate("abc"));
            var second = MakeContainsTemplate("xyz");
            second.OperatorName = "And";
            group.SearchTemplates.Add(second);

            var predicate = BuildPredicate(group);

            Assert.False(predicate("abcxyz"));      // both → AND true → NOT false
            Assert.True(predicate("abc only"));     // only first → AND false → NOT true
            Assert.True(predicate("xyz only"));     // only second → AND false → NOT true
            Assert.True(predicate("neither here")); // none → AND false → NOT true
        }

        [Fact]
        public void NotOr_group_wraps_combined_body_in_Expression_Not()
        {
            // NOT(value contains "abc" OR value contains "xyz") — true only when neither substring is present.
            var group = new SearchTemplateGroup { OperatorName = "NotOr" };
            group.SearchTemplates.Add(MakeContainsTemplate("abc"));
            // Inner OrElse comes from the per-template operator name on the non-leading template.
            var second = MakeContainsTemplate("xyz");
            second.OperatorName = "Or";
            group.SearchTemplates.Add(second);

            var predicate = BuildPredicate(group);

            Assert.False(predicate("abc only"));    // first true → OR true → NOT false
            Assert.False(predicate("xyz only"));    // second true → OR true → NOT false
            Assert.False(predicate("abcxyz"));      // both → OR true → NOT false
            Assert.True(predicate("neither here")); // neither → OR false → NOT true
        }

        [Fact]
        public void NotAnd_with_single_template_negates_that_predicate()
        {
            var group = new SearchTemplateGroup { OperatorName = "NotAnd" };
            group.SearchTemplates.Add(MakeContainsTemplate("abc"));

            var predicate = BuildPredicate(group);

            Assert.False(predicate("xx abc yy"));
            Assert.True(predicate("nothing"));
        }

        private static SearchTemplate MakeContainsTemplate(string value)
        {
            var template = new SearchTemplate(ColumnDataType.String)
            {
                SearchType = SearchType.Contains,
                SelectedValue = value
            };
            return template;
        }

        private static System.Func<object, bool> BuildPredicate(SearchTemplateGroup group)
        {
            var builder = new FilterExpressionBuilder();
            var groups = new ObservableCollection<SearchTemplateGroup> { group };
            var result = builder.BuildFilterExpression(groups, typeof(string));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotNull(result.FilterExpression);
            return result.FilterExpression;
        }
    }
}
