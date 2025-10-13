using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Specifies the level at which an operator toggle occurred
    /// </summary>
    public enum OperatorLevel
    {
        /// <summary>
        /// Operator between SearchTemplates within a group
        /// </summary>
        Template,

        /// <summary>
        /// Operator between SearchTemplateGroups
        /// </summary>
        Group
    }

    /// <summary>
    /// Event arguments for operator toggle events in the filter panel
    /// </summary>
    public class OperatorToggledEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the filter token that was clicked
        /// </summary>
        public IFilterToken OperatorToken { get; }

        /// <summary>
        /// Gets the level at which the operator toggle occurred
        /// </summary>
        public OperatorLevel Level { get; }

        /// <summary>
        /// Gets the new operator value ("And" or "Or")
        /// </summary>
        public string NewOperator { get; }

        /// <summary>
        /// Initializes a new instance of the OperatorToggledEventArgs class
        /// </summary>
        /// <param name="operatorToken">The operator token that was clicked</param>
        /// <param name="level">The level at which the operator toggle occurred</param>
        /// <param name="newOperator">The new operator value</param>
        public OperatorToggledEventArgs(IFilterToken operatorToken, OperatorLevel level, string newOperator)
        {
            OperatorToken = operatorToken ?? throw new ArgumentNullException(nameof(operatorToken));
            Level = level;
            NewOperator = newOperator ?? throw new ArgumentNullException(nameof(newOperator));
        }
    }
}