using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace WWControls.Core
{
    /// <summary>
    /// Represents a group of search templates combined with a logical operator
    /// </summary>
    public class SearchTemplateGroup : ObservableObject
    {
        #region Fields

        private string operatorName = "And";
        private int groupNumber;
        private bool isOperatorVisible = true;

        private Func<Expression, Expression, Expression> operatorFunction = Expression.AndAlso;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the logical operator function
        /// </summary>
        public Func<Expression, Expression, Expression> OperatorFunction
        {
            get { return operatorFunction; }
            set { SetProperty(value, ref operatorFunction); }
        }
        /// <summary>
        /// Gets or sets the logical operator name. Accepts "And", "Or", "NotAnd", "NotOr"
        /// (case-insensitive). The negated variants keep their inner combiner in
        /// <see cref="OperatorFunction"/> (AndAlso for And/NotAnd, OrElse for Or/NotOr);
        /// callers detect negation via <see cref="IsNegated"/> and wrap the combined body
        /// in <c>Expression.Not</c> at build time.
        /// </summary>
        public string OperatorName
        {
            get => operatorName;
            set
            {
                if (SetProperty(value, ref operatorName))
                {
                    OperatorFunction = LogicalOperatorExtensions.Parse(value).InnerComposer();
                    OnPropertyChanged(nameof(IsNegated));
                }
            }
        }

        /// <summary>
        /// True when the operator is one of the negated variants ("NotAnd" / "NotOr").
        /// <see cref="FilterExpressionBuilder"/> wraps the combined group body in
        /// <see cref="Expression.Not(Expression)"/> when this is true.
        /// </summary>
        public bool IsNegated
            => string.Equals(operatorName, "NotAnd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(operatorName, "NotOr", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the group number
        /// </summary>
        public int GroupNumber
        {
            get => groupNumber;
            set => SetProperty(value, ref groupNumber);
        }

        /// <summary>
        /// Operator that joins other search template groups
        /// Visible when no other search template groups preceding this 
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

        /// <summary>
        /// Nested groups composed under this group. The per-column popup
        /// (<c>ColumnFilterPopup</c>) ignores this collection — only the Filter Editor and
        /// <see cref="FilterExpressionBuilder"/> recurse into it.
        /// </summary>
        public ObservableCollection<SearchTemplateGroup> ChildGroups { get; } = new ObservableCollection<SearchTemplateGroup>();

        #endregion
    }
}
