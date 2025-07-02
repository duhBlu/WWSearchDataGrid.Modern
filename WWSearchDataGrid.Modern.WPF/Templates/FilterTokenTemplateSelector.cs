using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Templates
{
    /// <summary>
    /// Template selector for filter tokens based on their type
    /// </summary>
    public class FilterTokenTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template for opening bracket tokens
        /// </summary>
        public DataTemplate OpenBracketTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for column name tokens
        /// </summary>
        public DataTemplate ColumnNameTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for search type tokens
        /// </summary>
        public DataTemplate SearchTypeTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for value tokens
        /// </summary>
        public DataTemplate ValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for operator tokens
        /// </summary>
        public DataTemplate OperatorTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for closing bracket tokens
        /// </summary>
        public DataTemplate CloseBracketTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for logical connector tokens
        /// </summary>
        public DataTemplate LogicalConnectorTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for remove action tokens
        /// </summary>
        public DataTemplate RemoveActionTemplate { get; set; }

        /// <summary>
        /// Selects the appropriate template based on the token type
        /// </summary>
        /// <param name="item">The filter token</param>
        /// <param name="container">The container element</param>
        /// <returns>The appropriate DataTemplate</returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is IFilterToken token)
            {
                return token.TokenType switch
                {
                    FilterTokenType.OpenBracket => OpenBracketTemplate,
                    FilterTokenType.ColumnName => ColumnNameTemplate,
                    FilterTokenType.SearchType => SearchTypeTemplate,
                    FilterTokenType.Value => ValueTemplate,
                    FilterTokenType.Operator => OperatorTemplate,
                    FilterTokenType.CloseBracket => CloseBracketTemplate,
                    FilterTokenType.LogicalConnector => LogicalConnectorTemplate,
                    FilterTokenType.RemoveAction => RemoveActionTemplate,
                    _ => base.SelectTemplate(item, container)
                };
            }

            return base.SelectTemplate(item, container);
        }
    }
}