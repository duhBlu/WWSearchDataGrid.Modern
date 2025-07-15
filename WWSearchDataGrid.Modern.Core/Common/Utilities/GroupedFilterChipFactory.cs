using System;
using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Factory class for creating filter chips from grouped filter combinations
    /// </summary>
    public static class GroupedFilterChipFactory
    {
        /// <summary>
        /// Creates filter chips for grouped filtering scenarios
        /// </summary>
        /// <param name="combinations">The selected group-child combinations</param>
        /// <param name="groupColumnPath">Path to the grouping column</param>
        /// <param name="valueColumnPath">Path to the value column</param>
        /// <param name="allGroupData">All available group data for analysis</param>
        /// <returns>List of filter chip components representing the grouped filter</returns>
        public static List<FilterChipComponents> CreateFilterChips(
            List<(object GroupKey, object ChildValue)> combinations,
            string groupColumnPath,
            string valueColumnPath,
            Dictionary<object, List<object>> allGroupData = null)
        {
            var filterChips = new List<FilterChipComponents>();

            if (!combinations.Any())
                return filterChips;

            // Analyze the combinations to determine optimal filter representation
            var analysisResult = AnalyzeGroupedCombinations(combinations, allGroupData);

            // Create filter chips based on the analysis
            filterChips.AddRange(CreateOptimalFilterChips(analysisResult, groupColumnPath, valueColumnPath));

            return filterChips;
        }

        /// <summary>
        /// Analyzes grouped combinations to determine optimal filter representation
        /// </summary>
        private static GroupedFilterAnalysis AnalyzeGroupedCombinations(
            List<(object GroupKey, object ChildValue)> combinations,
            Dictionary<object, List<object>> allGroupData)
        {
            var analysis = new GroupedFilterAnalysis();
            
            // Group combinations by group key
            var groupedCombinations = combinations.GroupBy(c => c.GroupKey).ToList();
            
            foreach (var groupCombos in groupedCombinations)
            {
                var groupKey = groupCombos.Key;
                var selectedValues = groupCombos.Select(c => c.ChildValue).ToList();
                
                // Determine if this is a positive or negative condition for the group
                List<object> allValuesInGroup = null;
                if (allGroupData?.ContainsKey(groupKey) == true)
                {
                    allValuesInGroup = allGroupData[groupKey];
                }
                
                var groupCondition = new GroupCondition
                {
                    GroupKey = groupKey,
                    SelectedValues = selectedValues,
                    AllValuesInGroup = allValuesInGroup,
                    IsPositiveGroupCondition = true // Group is included
                };

                // Determine if values are included (positive) or excluded (negative)
                if (allValuesInGroup != null)
                {
                    var unselectedValues = allValuesInGroup.Except(selectedValues).ToList();
                    
                    // Use negative condition if fewer excluded values than included
                    if (unselectedValues.Count < selectedValues.Count && unselectedValues.Any())
                    {
                        groupCondition.IsPositiveValueCondition = false;
                        groupCondition.ExcludedValues = unselectedValues;
                    }
                    else
                    {
                        groupCondition.IsPositiveValueCondition = true;
                        groupCondition.IncludedValues = selectedValues;
                    }
                }
                else
                {
                    // Default to positive condition when we don't have full data
                    groupCondition.IsPositiveValueCondition = true;
                    groupCondition.IncludedValues = selectedValues;
                }

                analysis.GroupConditions.Add(groupCondition);
            }

            return analysis;
        }

        /// <summary>
        /// Creates optimal filter chips based on the analysis
        /// </summary>
        private static List<FilterChipComponents> CreateOptimalFilterChips(
            GroupedFilterAnalysis analysis,
            string groupColumnPath,
            string valueColumnPath)
        {
            var chips = new List<FilterChipComponents>();

            foreach (var groupCondition in analysis.GroupConditions)
            {
                // Create group condition chip (always positive for the group itself)
                var groupChip = new FilterChipComponents
                {
                    SearchTypeText = "=",
                    PrimaryValue = groupCondition.GroupKey?.ToString() ?? "(blank)"
                };

                chips.Add(groupChip);

                // Create value condition chip(s)
                if (groupCondition.IsPositiveValueCondition && groupCondition.IncludedValues?.Any() == true)
                {
                    if (groupCondition.IncludedValues.Count == 1)
                    {
                        // Single value condition
                        var valueChip = new FilterChipComponents
                        {
                            SearchTypeText = "=",
                            PrimaryValue = groupCondition.IncludedValues.First()?.ToString() ?? "(blank)",
                            Operator = "And"
                        };
                        chips.Add(valueChip);
                    }
                    else
                    {
                        // Multiple value condition - use "Is any of"
                        var valueChip = new FilterChipComponents
                        {
                            SearchTypeText = "in",
                            PrimaryValue = $"[{string.Join(", ", groupCondition.IncludedValues.Select(v => $"'{v}'"))}]",
                            Operator = "And"
                        };
                        valueChip.ParsePrimaryValueAsMultipleValues();
                        chips.Add(valueChip);
                    }
                }
                else if (!groupCondition.IsPositiveValueCondition && groupCondition.ExcludedValues?.Any() == true)
                {
                    if (groupCondition.ExcludedValues.Count == 1)
                    {
                        // Single exclusion condition
                        var valueChip = new FilterChipComponents
                        {
                            SearchTypeText = "≠",
                            PrimaryValue = groupCondition.ExcludedValues.First()?.ToString() ?? "(blank)",
                            Operator = "And"
                        };
                        chips.Add(valueChip);
                    }
                    else
                    {
                        // Multiple exclusion condition - use "Is none of"
                        var valueChip = new FilterChipComponents
                        {
                            SearchTypeText = "Is none of",
                            PrimaryValue = $"[{string.Join(", ", groupCondition.ExcludedValues.Select(v => $"'{v}'"))}]",
                            Operator = "And"
                        };
                        valueChip.ParsePrimaryValueAsMultipleValues();
                        chips.Add(valueChip);
                    }
                }
            }

            // Handle multiple groups with OR Operator
            if (analysis.GroupConditions.Count > 1)
            {
                // Set OR Operator for subsequent groups
                bool isFirstGroup = true;
                int chipIndex = 0;
                
                foreach (var groupCondition in analysis.GroupConditions)
                {
                    if (!isFirstGroup && chipIndex < chips.Count)
                    {
                        chips[chipIndex].Operator = "OR";
                    }
                    
                    // Skip over chips for this group (group chip + value chips)
                    chipIndex += GetChipCountForGroup(groupCondition);
                    isFirstGroup = false;
                }
            }

            return chips;
        }

        /// <summary>
        /// Gets the number of chips generated for a group condition
        /// </summary>
        private static int GetChipCountForGroup(GroupCondition groupCondition)
        {
            // Always 1 chip for the group condition
            int count = 1;
            
            // Add 1 chip for the value condition if it exists
            if ((groupCondition.IsPositiveValueCondition && groupCondition.IncludedValues?.Any() == true) ||
                (!groupCondition.IsPositiveValueCondition && groupCondition.ExcludedValues?.Any() == true))
            {
                count++;
            }
            
            return count;
        }
    }

    /// <summary>
    /// Analysis result for grouped filter combinations
    /// </summary>
    internal class GroupedFilterAnalysis
    {
        public List<GroupCondition> GroupConditions { get; set; } = new List<GroupCondition>();
    }

    /// <summary>
    /// Represents a condition for a specific group
    /// </summary>
    internal class GroupCondition
    {
        public object GroupKey { get; set; }
        public List<object> SelectedValues { get; set; } = new List<object>();
        public List<object> AllValuesInGroup { get; set; }
        public bool IsPositiveGroupCondition { get; set; } = true;
        public bool IsPositiveValueCondition { get; set; } = true;
        public List<object> IncludedValues { get; set; }
        public List<object> ExcludedValues { get; set; }
    }
}