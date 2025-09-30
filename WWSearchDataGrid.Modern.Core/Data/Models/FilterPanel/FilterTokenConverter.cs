using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                if (filterIndex > 0 && !string.IsNullOrEmpty(filter.Operator))
                {
                    tokens.Add(new GroupLogicalConnectorToken(filter.Operator, filterId, orderIndex++, filter, filterIndex));
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
        /// <param name="component">The component containing filter information and indices</param>
        /// <param name="filterId">The unique filter ID</param>
        /// <param name="orderIndex">The current order index (will be incremented)</param>
        /// <param name="sourceFilter">The source filter info</param>
        private static List<IFilterToken> ConvertComponentToTokens(FilterChipComponents component, string filterId, ref int orderIndex, ColumnFilterInfo sourceFilter)
        {
            var tokens = new List<IFilterToken>();

            // Add operator if present
            if (!string.IsNullOrEmpty(component.Operator))
            {
                // Determine if this is a group-level or template-level operator
                if (component.IsGroupLevelOperator)
                {
                    tokens.Add(new GroupLogicalConnectorToken(component.Operator, filterId, orderIndex++, sourceFilter, component.GroupIndex));
                }
                else
                {
                    tokens.Add(new TemplateLogicalConnectorToken(component.Operator, filterId, orderIndex++, sourceFilter, component.GroupIndex, component.TemplateIndex));
                }
            }

            // Add search type token
            if (!string.IsNullOrEmpty(component.SearchTypeText))
            {
                if (!component.HasNoInputValues)
                {
                    tokens.Add(new SearchTypeToken(component.SearchTypeText, filterId, orderIndex++, sourceFilter));
                }
                else
                {
                    // UnarySearchType tokens can be removed entirely
                    var unaryRemovalContext = CreateValueRemovalContext(sourceFilter, ValueType.UnarySearchType, component.SearchTypeText, null, component.GroupIndex, component.TemplateIndex);
                    tokens.Add(new UnarySearchTypeToken(component.SearchTypeText, filterId, orderIndex++, sourceFilter, unaryRemovalContext));
                }
            }

            // Handle multiple values
            if (component.HasMultipleValues)
            {
                component.ParsePrimaryValueAsMultipleValues(); // Ensure ValueItems is populated

                for (int i = 0; i < component.ValueItems.Count; i++)
                {
                    var value = component.ValueItems[i];
                    var removalContext = CreateValueRemovalContext(sourceFilter, ValueType.CollectionItem, value, i, component.GroupIndex, component.TemplateIndex);
                    tokens.Add(new ValueToken(value, filterId, orderIndex++, sourceFilter, removalContext));
                }
            }
            else
            {
                // Handle single or dual values
                if (!string.IsNullOrEmpty(component.PrimaryValue) && !component.HasNoInputValues)
                {
                    var primaryRemovalContext = CreateValueRemovalContext(sourceFilter, ValueType.Primary, component.PrimaryValue, null, component.GroupIndex, component.TemplateIndex);
                    tokens.Add(new ValueToken(component.PrimaryValue, filterId, orderIndex++, sourceFilter, primaryRemovalContext));
                }

                // Add operator between values if present
                if (!string.IsNullOrEmpty(component.ValueOperatorText))
                {
                    tokens.Add(new OperatorToken(component.ValueOperatorText, filterId, orderIndex++, sourceFilter));
                }

                // Add secondary value if present
                if (!string.IsNullOrEmpty(component.SecondaryValue))
                {
                    var secondaryRemovalContext = CreateValueRemovalContext(sourceFilter, ValueType.Secondary, component.SecondaryValue, null, component.GroupIndex, component.TemplateIndex);
                    tokens.Add(new ValueToken(component.SecondaryValue, filterId, orderIndex++, sourceFilter, secondaryRemovalContext));
                }
            }

            return tokens;
        }

        /// <summary>
        /// Creates a ValueRemovalContext for a token if possible
        /// </summary>
        /// <param name="sourceFilter">The source filter info containing the template reference</param>
        /// <param name="valueType">The type of value being represented</param>
        /// <param name="originalValue">The original value being displayed</param>
        /// <param name="valueIndex">The index of the value in collections (optional)</param>
        /// <param name="groupIndex">The index of the SearchTemplateGroup this value belongs to</param>
        /// <param name="templateIndex">The index of the SearchTemplate within the group</param>
        /// <returns>A ValueRemovalContext if the template can be accessed, null otherwise</returns>
        private static ValueRemovalContext CreateValueRemovalContext(ColumnFilterInfo sourceFilter, ValueType valueType, object originalValue, int? valueIndex, int groupIndex, int templateIndex)
        {
            // Try to get the SearchTemplate from the source filter data
            SearchTemplate template = null;

            // Check if FilterData is a ColumnSearchBox with SearchTemplateController
            if (sourceFilter?.FilterData != null)
            {
                var filterData = sourceFilter.FilterData;

                // Using reflection to access SearchTemplateController from the column object
                var searchTemplateControllerProperty = filterData.GetType().GetProperty("SearchTemplateController");
                if (searchTemplateControllerProperty != null)
                {
                    var controller = searchTemplateControllerProperty.GetValue(filterData) as SearchTemplateController;

                    // Use the groupIndex and templateIndex to find the correct template
                    if (controller?.SearchGroups != null && groupIndex >= 0 && groupIndex < controller.SearchGroups.Count)
                    {
                        var group = controller.SearchGroups[groupIndex];
                        var templatesWithFilter = group.SearchTemplates.Where(t => t.HasCustomFilter).ToList();

                        if (templateIndex >= 0 && templateIndex < templatesWithFilter.Count)
                        {
                            template = templatesWithFilter[templateIndex];
                        }
                    }
                }
            }

            if (template == null)
                return null;

            return new ValueRemovalContext
            {
                ParentTemplate = template,
                ValueType = valueType,
                OriginalValue = originalValue,
                ValueIndex = valueIndex
            };
        }
    }
}