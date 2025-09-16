using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Extension methods for LINQ expressions
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Composes two expressions using the specified combining function
        /// </summary>
        /// <typeparam name="T">Type of the parameter</typeparam>
        /// <param name="first">First expression</param>
        /// <param name="second">Second expression</param>
        /// <returns>Combined expression</returns>
        /// <summary>
        /// Composes two expressions with a given operator
        /// </summary>
        public static Expression<Func<T, bool>> Compose<T>(
            this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second,
            Func<Expression, Expression, Expression> merge)
        {
            var map = first.Parameters
                .Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
            return Expression.Lambda<Func<T, bool>>(merge(first.Body, secondBody), first.Parameters);
        }

    }
}
