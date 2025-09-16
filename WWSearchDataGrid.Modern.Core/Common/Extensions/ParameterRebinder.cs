using System.Collections.Generic;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Expression visitor for rebinding parameters in lambda expressions
    /// Used primarily for expression composition operations
    /// </summary>
    internal sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        public static Expression ReplaceParameters(
            Dictionary<ParameterExpression, ParameterExpression> map,
            Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            if (_map.TryGetValue(p, out ParameterExpression replacement))
            {
                return replacement;
            }
            return base.VisitParameter(p);
        }
    }
}