using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a group of search templates combined with a logical operator
    /// </summary>
    public class SearchTemplateGroup : ObservableObject, ILogicalOperatorProvider
    {
        #region Fields

        private string operatorName = "And";
        private int groupNumber;
        private bool isOperatorVisible = true;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the logical operator function
        /// </summary>
        public Func<Expression, Expression, Expression> OperatorFunction { get; set; } = Expression.And;

        /// <summary>
        /// Gets or sets the logical operator name
        /// </summary>
        public string OperatorName
        {
            get => operatorName;
            set
            {
                if (SetProperty(value, ref operatorName))
                {
                    // Use case-insensitive comparison and handle edge cases
                    if (string.Equals(value, "And", StringComparison.OrdinalIgnoreCase))
                    {
                        OperatorFunction = Expression.And;
                    }
                    else if (string.Equals(value, "Or", StringComparison.OrdinalIgnoreCase))
                    {
                        OperatorFunction = Expression.Or;
                    }
                    else
                    {
                        // Handle unexpected values - default to And for safety
                        OperatorFunction = Expression.And;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the group number
        /// </summary>
        public int GroupNumber
        {
            get => groupNumber;
            set => SetProperty(value, ref groupNumber);
        }

        /// <summary>
        /// Gets or sets whether the logical operator is visible
        /// </summary>
        public bool IsOperatorVisible
        {
            get => isOperatorVisible;
            set => SetProperty(value, ref isOperatorVisible);
        }

        /// <summary>
        /// Gets the collection of search templates in this group
        /// </summary>
        public ObservableCollection<SearchTemplate> SearchTemplates { get; } = new ObservableCollection<SearchTemplate>();

        #endregion
    }
}
