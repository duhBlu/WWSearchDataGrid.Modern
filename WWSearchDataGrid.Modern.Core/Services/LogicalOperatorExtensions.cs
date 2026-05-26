using System;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Conversion helpers between <see cref="LogicalOperator"/> values, the persisted
    /// <see cref="SearchTemplateGroup.OperatorName"/> strings, and the expression combiner
    /// used by <see cref="FilterExpressionBuilder"/>.
    /// </summary>
    public static class LogicalOperatorExtensions
    {
        /// <summary>
        /// Parses an <see cref="SearchTemplateGroup.OperatorName"/> token string into a
        /// <see cref="LogicalOperator"/>. Unknown values fall back to <see cref="LogicalOperator.And"/>.
        /// </summary>
        public static LogicalOperator Parse(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return LogicalOperator.And;
            if (string.Equals(token, "Or", StringComparison.OrdinalIgnoreCase)) return LogicalOperator.Or;
            if (string.Equals(token, "NotAnd", StringComparison.OrdinalIgnoreCase)) return LogicalOperator.NotAnd;
            if (string.Equals(token, "NotOr", StringComparison.OrdinalIgnoreCase)) return LogicalOperator.NotOr;
            return LogicalOperator.And;
        }

        /// <summary>
        /// Canonical persisted token string ("And" / "Or" / "NotAnd" / "NotOr") for an operator.
        /// </summary>
        public static string ToTokenString(this LogicalOperator op)
        {
            switch (op)
            {
                case LogicalOperator.Or: return "Or";
                case LogicalOperator.NotAnd: return "Not And";
                case LogicalOperator.NotOr: return "Not Or";
                default: return "And";
            }
        }

        /// <summary>
        /// Inner expression combiner — <c>AndAlso</c> for And / NotAnd, <c>OrElse</c> for Or / NotOr.
        /// Negation is applied separately by wrapping the combined body in <see cref="Expression.Not(Expression)"/>.
        /// </summary>
        public static Func<Expression, Expression, Expression> InnerComposer(this LogicalOperator op)
        {
            switch (op)
            {
                case LogicalOperator.Or:
                case LogicalOperator.NotOr:
                    return Expression.OrElse;
                default:
                    return Expression.AndAlso;
            }
        }

        /// <summary>
        /// True for the negated variants (<see cref="LogicalOperator.NotAnd"/>, <see cref="LogicalOperator.NotOr"/>).
        /// </summary>
        public static bool IsNegated(this LogicalOperator op)
            => op == LogicalOperator.NotAnd || op == LogicalOperator.NotOr;

        /// <summary>
        /// Human-readable label shown on the operator chip in the Filter Editor.
        /// </summary>
        public static string DisplayText(this LogicalOperator op)
        {
            switch (op)
            {
                case LogicalOperator.Or: return "Or";
                case LogicalOperator.NotAnd: return "Not And";
                case LogicalOperator.NotOr: return "Not Or";
                default: return "And";
            }
        }
    }
}
