using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Base class for filter tokens
    /// </summary>
    public abstract class FilterTokenBase : IFilterToken
    {
        public string DisplayText { get; protected set; }
        public FilterTokenType TokenType { get; protected set; }
        public string FilterId { get; protected set; }
        public int OrderIndex { get; protected set; }
        public bool IsFilterStart { get; protected set; }
        public bool IsFilterEnd { get; protected set; }
        public ColumnFilterInfo SourceFilter { get; protected set; }

        protected FilterTokenBase(string displayText, FilterTokenType tokenType, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
        {
            DisplayText = displayText;
            TokenType = tokenType;
            FilterId = filterId;
            OrderIndex = orderIndex;
            SourceFilter = sourceFilter;
        }
    }

    /// <summary>
    /// Token representing an opening bracket
    /// </summary>
    public class OpenBracketToken : FilterTokenBase
    {
        public OpenBracketToken(string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base("[", FilterTokenType.OpenBracket, filterId, orderIndex, sourceFilter)
        {
            IsFilterStart = true;
        }
    }

    /// <summary>
    /// Token representing a column name
    /// </summary>
    public class ColumnNameToken : FilterTokenBase
    {
        public ColumnNameToken(string columnName, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base(columnName, FilterTokenType.ColumnName, filterId, orderIndex, sourceFilter)
        {
        }
    }

    /// <summary>
    /// Token representing a search type operation
    /// </summary>
    public class SearchTypeToken : FilterTokenBase
    {
        public SearchTypeToken(string searchTypeText, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base(searchTypeText, FilterTokenType.SearchType, filterId, orderIndex, sourceFilter)
        {
        }
    }

    /// <summary>
    /// Token representing an individual value
    /// </summary>
    public class ValueToken : FilterTokenBase
    {
        public ValueToken(string value, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base($"{value}", FilterTokenType.Value, filterId, orderIndex, sourceFilter)
        {
        }
    }

    /// <summary>
    /// Token representing an operator between values
    /// </summary>
    public class OperatorToken : FilterTokenBase
    {
        public OperatorToken(string operatorText, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base(operatorText, FilterTokenType.Operator, filterId, orderIndex, sourceFilter)
        {
        }
    }

    /// <summary>
    /// Token representing a closing bracket
    /// </summary>
    public class CloseBracketToken : FilterTokenBase
    {
        public CloseBracketToken(string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base("]", FilterTokenType.CloseBracket, filterId, orderIndex, sourceFilter)
        {
        }
    }

    /// <summary>
    /// Token representing a logical connector between filters
    /// </summary>
    public class GroupLogicalConnectorToken : FilterTokenBase
    {
        public GroupLogicalConnectorToken(string connectorText, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base(connectorText, FilterTokenType.GroupLogicalConnectorToken, filterId, orderIndex, sourceFilter)
        {
        }
    }
    
    /// <summary>
    /// Token representing a logical connector between filters
    /// </summary>
    public class TemplateLogicalConnectorToken : FilterTokenBase
    {
        public TemplateLogicalConnectorToken(string connectorText, string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base(connectorText, FilterTokenType.TemplateLogicalConnectorToken, filterId, orderIndex, sourceFilter)
        {
        }
    }

    /// <summary>
    /// Token representing the remove action for a logical filter
    /// </summary>
    public class RemoveActionToken : FilterTokenBase
    {
        public RemoveActionToken(string filterId, int orderIndex, ColumnFilterInfo sourceFilter)
            : base("×", FilterTokenType.RemoveAction, filterId, orderIndex, sourceFilter)
        {
            IsFilterEnd = true;
        }
    }
}