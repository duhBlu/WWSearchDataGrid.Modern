using System;
using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Utility class for converting ColumnFilterInfo objects to filter tokens
    /// </summary>
    public static class FilterTokenConverter
    {
        /// <summary>
        /// Converts a collection of ColumnFilterInfo objects to filter tokens
        /// </summary>
        /// <param name="filters">The filters to convert</param>
        /// <returns>A flat list of filter tokens that can wrap independently</returns>
        public static List<IFilterToken> ConvertToTokens(IEnumerable<ColumnFilterInfo> filters)
        {
            var tokens = new List<IFilterToken>();
            
            if (filters == null || !filters.Any())
                return tokens;

            var filterList = filters.ToList();
            
            for (int filterIndex = 0; filterIndex < filterList.Count; filterIndex++)
            {
                var filter = filterList[filterIndex];
                var filterId = Guid.NewGuid().ToString();
                var orderIndex = 0;

                // Add logical connector for non-first filters
                if (filterIndex > 0 && !string.IsNullOrEmpty(filter.Conjunction))
                {
                    tokens.Add(new GroupLogicalConnectorToken(filter.Conjunction, filterId, orderIndex++, filter));
                }

                // Add opening bracket token
                tokens.Add(new OpenBracketToken(filterId, orderIndex++, filter));

                // Add column name token
                tokens.Add(new ColumnNameToken(filter.ColumnName, filterId, orderIndex++, filter));

                // Process filter components
                foreach (var component in filter.FilterComponents)
                {
                    tokens.AddRange(ConvertComponentToTokens(component, filterId, ref orderIndex, filter));
                }

                // Add closing bracket token
                tokens.Add(new CloseBracketToken(filterId, orderIndex++, filter));

                // Add remove action token at the end of each logical filter
                tokens.Add(new RemoveActionToken(filterId, orderIndex++, filter));
            }

            return tokens;
        }

        /// <summary>
        /// Converts a FilterChipComponents object to tokens
        /// </summary>
        private static List<IFilterToken> ConvertComponentToTokens(FilterChipComponents component, string filterId, ref int orderIndex, ColumnFilterInfo sourceFilter)
        {
            var tokens = new List<IFilterToken>();

            // Add conjunction if present
            if (!string.IsNullOrEmpty(component.Conjunction))
            {
                tokens.Add(new TemplateLogicalConnectorToken(component.Conjunction, filterId, orderIndex++, sourceFilter));
            }

            // Add search type token
            if (!string.IsNullOrEmpty(component.SearchTypeText))
            {
                tokens.Add(new SearchTypeToken(component.SearchTypeText, filterId, orderIndex++, sourceFilter));
            }

            // Handle multiple values
            if (component.HasMultipleValues)
            {
                component.ParsePrimaryValueAsMultipleValues(); // Ensure ValueItems is populated
                
                for (int i = 0; i < component.ValueItems.Count; i++)
                {
                    var value = component.ValueItems[i];
                    tokens.Add(new ValueToken(value, filterId, orderIndex++, sourceFilter));
                }
            }
            else
            {
                // Handle single or dual values
                if (!string.IsNullOrEmpty(component.PrimaryValue) && !component.HasNoInputValues)
                {
                    tokens.Add(new ValueToken(component.PrimaryValue, filterId, orderIndex++, sourceFilter));
                }

                // Add operator between values if present
                if (!string.IsNullOrEmpty(component.ValueOperatorText))
                {
                    tokens.Add(new OperatorToken(component.ValueOperatorText, filterId, orderIndex++, sourceFilter));
                }

                // Add secondary value if present
                if (!string.IsNullOrEmpty(component.SecondaryValue))
                {
                    tokens.Add(new ValueToken(component.SecondaryValue, filterId, orderIndex++, sourceFilter));
                }
            }

            return tokens;
        }
    }
}