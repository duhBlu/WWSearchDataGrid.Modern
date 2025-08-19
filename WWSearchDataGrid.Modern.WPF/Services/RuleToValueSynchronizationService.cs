using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Service for synchronizing filter rules to value selections using existing SearchEngine
    /// </summary>
    public class RuleToValueSynchronizationService : IRuleToValueSynchronizationService
    {
        /// <summary>
        /// Evaluates which values should be selected based on current rules
        /// </summary>
        public IEnumerable<object> EvaluateRulesAgainstValues(SearchTemplateController rules, IEnumerable<object> allValues)
        {
            if (rules == null || allValues == null)
                return Enumerable.Empty<object>();

            var selectedValues = new List<object>();

            try
            {
                foreach (var value in allValues)
                {
                    if (ShouldValueBeSelected(value, rules))
                    {
                        selectedValues.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating rules against values: {ex.Message}");
            }

            return selectedValues;
        }

        /// <summary>
        /// Determines if a specific value should be selected based on current rules
        /// </summary>
        public bool ShouldValueBeSelected(object value, SearchTemplateController rules)
        {
            if (rules == null || !HasMeaningfulRules(rules))
                return true; // Default to selected if no meaningful rules

            try
            {
                // Use the controller's existing filter expression if available
                if (rules.FilterExpression != null)
                {
                    return rules.FilterExpression(value);
                }

                // Evaluate using search groups and templates
                return EvaluateSearchGroups(value, rules);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating value '{value}' against rules: {ex.Message}");
                return true; // Default to selected on error
            }
        }

        /// <summary>
        /// Evaluates value against search groups using SearchEngine
        /// </summary>
        private bool EvaluateSearchGroups(object value, SearchTemplateController rules)
        {
            if (rules.SearchGroups == null || rules.SearchGroups.Count == 0)
                return true;

            var groupResults = new List<bool>();

            foreach (var group in rules.SearchGroups)
            {
                var groupResult = EvaluateSearchGroup(value, group);
                groupResults.Add(groupResult);
            }

            // For multiple groups, use OR logic (any group match = include)
            // This matches typical filtering behavior where groups are combined with OR
            return groupResults.Any(result => result);
        }

        /// <summary>
        /// Evaluates value against a single search group
        /// </summary>
        private bool EvaluateSearchGroup(object value, SearchTemplateGroup group)
        {
            if (group.SearchTemplates == null || group.SearchTemplates.Count == 0)
                return true;

            var templateResults = new List<bool>();

            foreach (var template in group.SearchTemplates)
            {
                var templateResult = EvaluateSearchTemplate(value, template);
                templateResults.Add(templateResult);
            }

            // Combine template results based on group operator
            var operatorName = group.OperatorName?.ToLower() ?? "and";
            
            if (operatorName == "or")
            {
                return templateResults.Any(result => result);
            }
            else // Default to "and"
            {
                return templateResults.All(result => result);
            }
        }

        /// <summary>
        /// Evaluates value against a single search template using SearchEngine
        /// </summary>
        private bool EvaluateSearchTemplate(object value, SearchTemplate template)
        {
            if (template == null || !HasMeaningfulSearchCriteria(template))
                return true;

            try
            {
                // Handle special cases that need custom logic
                switch (template.SearchType)
                {
                    case SearchType.IsAnyOf:
                        if (template.SelectedValues?.Any() == true)
                        {
                            return template.SelectedValues.Any(v => Equals(v, value));
                        }
                        return false;

                    case SearchType.IsNoneOf:
                        if (template.SelectedValues?.Any() == true)
                        {
                            return !template.SelectedValues.Any(v => Equals(v, value));
                        }
                        return true;

                    case SearchType.IsOnAnyOfDates:
                        if (template.SelectedDates?.Any() == true)
                        {
                            if (value is DateTime dateValue)
                            {
                                return template.SelectedDates.Any(d => d.Date == dateValue.Date);
                            }
                        }
                        return false;

                    default:
                        // Create search condition from template
                        var searchCondition = CreateSearchCondition(template);
                        
                        // Use SearchEngine to evaluate the condition
                        return SearchEngine.EvaluateCondition(value, searchCondition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating template: {ex.Message}");
                return true; // Default to include on error
            }
        }

        /// <summary>
        /// Creates SearchCondition from SearchTemplate
        /// </summary>
        private SearchCondition CreateSearchCondition(SearchTemplate template)
        {
            var condition = new SearchCondition
            {
                SearchType = template.SearchType,
                TargetType = GetTargetTypeFromColumnDataType(template.ColumnDataType)
            };

            // Set values based on search type
            switch (template.SearchType)
            {
                case SearchType.Contains:
                case SearchType.DoesNotContain:
                case SearchType.StartsWith:
                case SearchType.EndsWith:
                case SearchType.Equals:
                case SearchType.NotEquals:
                case SearchType.LessThan:
                case SearchType.LessThanOrEqualTo:
                case SearchType.GreaterThan:
                case SearchType.GreaterThanOrEqualTo:
                case SearchType.IsLike:
                case SearchType.IsNotLike:
                case SearchType.DateInterval:
                case SearchType.TopN:
                case SearchType.BottomN:
                    condition.RawPrimaryValue = template.SelectedValue;
                    break;

                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    condition.RawPrimaryValue = template.SelectedValue;
                    condition.RawSecondaryValue = template.SelectedSecondaryValue;
                    break;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                case SearchType.IsOnAnyOfDates:
                    // For list-based search types, create a combined condition
                    // Since SearchCondition doesn't have a Values property, we'll handle this differently
                    var values = template.SelectedValues?.Select(v => v).ToList() ?? new List<object>();
                    if (values.Count == 1)
                    {
                        condition.SearchType = SearchType.Equals;
                        condition.RawPrimaryValue = values.First();
                    }
                    else if (values.Count > 1)
                    {
                        // For multiple values, we'll need to create multiple conditions
                        // This is a limitation - we'll handle it in the evaluation method
                        condition.RawPrimaryValue = values;
                    }
                    break;

                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.IsEmpty:
                case SearchType.IsNotEmpty:
                case SearchType.Yesterday:
                case SearchType.Today:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    // These don't need values
                    break;
            }

            // Convert the values after setting them
            condition.ConvertValues();
            return condition;
        }

        /// <summary>
        /// Gets the target type from ColumnDataType enum
        /// </summary>
        private Type GetTargetTypeFromColumnDataType(ColumnDataType columnDataType)
        {
            return columnDataType switch
            {
                ColumnDataType.String => typeof(string),
                ColumnDataType.Number => typeof(decimal),
                ColumnDataType.DateTime => typeof(DateTime),
                ColumnDataType.Boolean => typeof(bool),
                ColumnDataType.Enum => typeof(string),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Determines if the controller has meaningful rules (copied from FilterApplicationService)
        /// </summary>
        private bool HasMeaningfulRules(SearchTemplateController searchTemplateController)
        {
            if (searchTemplateController == null)
                return false;

            // Check if we have a custom expression already set
            if (searchTemplateController.HasCustomExpression && searchTemplateController.FilterExpression != null)
            {
                return true;
            }

            // Check if we have any groups with meaningful templates
            if (searchTemplateController.SearchGroups?.Any() == true)
            {
                return searchTemplateController.SearchGroups
                    .SelectMany(g => g.SearchTemplates ?? Enumerable.Empty<SearchTemplate>())
                    .Any(HasMeaningfulSearchCriteria);
            }

            return false;
        }

        /// <summary>
        /// Determines if a search template has meaningful search criteria (copied from FilterApplicationService)
        /// </summary>
        private bool HasMeaningfulSearchCriteria(SearchTemplate template)
        {
            if (template == null)
                return false;

            // Check if template has a non-default search type with values
            switch (template.SearchType)
            {
                case SearchType.Contains:
                case SearchType.DoesNotContain:
                case SearchType.StartsWith:
                case SearchType.EndsWith:
                case SearchType.Equals:
                case SearchType.NotEquals:
                case SearchType.LessThan:
                case SearchType.LessThanOrEqualTo:
                case SearchType.GreaterThan:
                case SearchType.GreaterThanOrEqualTo:
                case SearchType.IsLike:
                case SearchType.IsNotLike:
                    return !string.IsNullOrEmpty(template.SelectedValue?.ToString());

                case SearchType.Between:
                case SearchType.NotBetween:
                case SearchType.BetweenDates:
                    return template.SelectedValue != null && template.SelectedSecondaryValue != null;

                case SearchType.IsAnyOf:
                case SearchType.IsNoneOf:
                case SearchType.IsOnAnyOfDates:
                    return template.SelectedValues?.Any() == true;

                case SearchType.IsNull:
                case SearchType.IsNotNull:
                case SearchType.IsEmpty:
                case SearchType.IsNotEmpty:
                case SearchType.Yesterday:
                case SearchType.Today:
                case SearchType.AboveAverage:
                case SearchType.BelowAverage:
                case SearchType.Unique:
                case SearchType.Duplicate:
                    return true; // These don't need values

                case SearchType.DateInterval:
                    return template.SelectedValue != null; // Should have interval type

                case SearchType.TopN:
                case SearchType.BottomN:
                    return template.SelectedValue != null && int.TryParse(template.SelectedValue.ToString(), out var n) && n > 0;

                case SearchType.GroupedInclusion:
                case SearchType.GroupedExclusion:
                case SearchType.GroupedCombination:
                    return true; // These are complex grouping operations that are meaningful

                default:
                    return false;
            }
        }
    }
}