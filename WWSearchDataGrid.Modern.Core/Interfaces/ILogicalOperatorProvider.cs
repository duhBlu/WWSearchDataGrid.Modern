using System;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Interface for logical operator providers
    /// </summary>
    public interface ILogicalOperatorProvider
    {
        /// <summary>
        /// Gets or sets the logical operator function
        /// </summary>
        Func<Expression, Expression, Expression> OperatorFunction { get; set; }

        /// <summary>
        /// Gets or sets the logical operator name
        /// </summary>
        string OperatorName { get; set; }

        /// <summary>
        /// Gets or sets whether the logical operator is visible
        /// </summary>
        bool IsOperatorVisible { get; set; }
    }
}