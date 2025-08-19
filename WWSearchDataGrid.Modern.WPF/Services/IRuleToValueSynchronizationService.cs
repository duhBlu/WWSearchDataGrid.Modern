using System.Collections.Generic;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Service for synchronizing filter rules to value selections
    /// </summary>
    public interface IRuleToValueSynchronizationService
    {
        /// <summary>
        /// Evaluates which values should be selected based on current rules
        /// </summary>
        /// <param name="rules">Search template controller with current rules</param>
        /// <param name="allValues">All available values to evaluate</param>
        /// <returns>Values that should be selected based on the rules</returns>
        IEnumerable<object> EvaluateRulesAgainstValues(SearchTemplateController rules, IEnumerable<object> allValues);

        /// <summary>
        /// Determines if a specific value should be selected based on current rules
        /// </summary>
        /// <param name="value">Value to evaluate</param>
        /// <param name="rules">Search template controller with current rules</param>
        /// <returns>True if value should be selected, false otherwise</returns>
        bool ShouldValueBeSelected(object value, SearchTemplateController rules);
    }
}