using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Interface for search templates
    /// </summary>
    public interface ISearchTemplate : ILogicalOperatorProvider
    {
        /// <summary>
        /// Gets or sets the search type
        /// </summary>
        SearchType SearchType { get; set; }

        /// <summary>
        /// Gets or sets the set of available values
        /// </summary>
        HashSet<object> AvailableValues { get; set; }

        /// <summary>
        /// Gets or sets the selected value
        /// </summary>
        object SelectedValue { get; set; }

        /// <summary>
        /// Gets or sets the selected secondary value (for range operations)
        /// </summary>
        object SelectedSecondaryValue { get; set; }

        /// <summary>
        /// Gets or sets whether the template has unsaved changes
        /// </summary>
        bool HasChanges { get; set; }

        /// <summary>
        /// Gets whether the template has a custom filter applied
        /// </summary>
        bool HasCustomFilter { get; }

        /// <summary>
        /// Gets or sets the search template controller that manages this template
        /// </summary>
        SearchTemplateController SearchTemplateController { get; set; }

        /// <summary>
        /// Builds an expression for evaluating this search template
        /// </summary>
        /// <param name="targetType">Type of object being filtered</param>
        /// <returns>Expression representing the search condition</returns>
        Expression<Func<object, bool>> BuildExpression(Type targetType);

        /// <summary>
        /// Loads available values for the template
        /// </summary>
        /// <param name="columnValues">Set of available values</param>
        void LoadAvailableValues(HashSet<object> columnValues);
    }
}