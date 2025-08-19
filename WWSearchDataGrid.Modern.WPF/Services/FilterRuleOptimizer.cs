using System;
using System.Collections.Generic;
using System.Linq;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Services
{
    /// <summary>
    /// Analyzes filter selections and generates optimized rules
    /// </summary>
    public class FilterRuleOptimizer
    {
        /// <summary>
        /// Selection pattern analysis result
        /// </summary>
        public class SelectionAnalysis
        {
            public SelectionPattern Pattern { get; set; }
            public List<ValueRange> SelectedRanges { get; set; } = new List<ValueRange>();
            public List<ValueRange> UnselectedRanges { get; set; } = new List<ValueRange>();
            public bool UseNegation { get; set; }
            public int EfficiencyScore { get; set; }
            public ColumnDataType DataType { get; set; }
        }

        /// <summary>
        /// Represents a contiguous range of values
        /// </summary>
        public class ValueRange
        {
            public object Start { get; set; }
            public object End { get; set; }
            public int Count { get; set; }
            public bool IsSingleValue => Equals(Start, End);
        }

        /// <summary>
        /// Pattern type for selection
        /// </summary>
        public enum SelectionPattern
        {
            AllSelected,
            AllUnselected,
            SingleSelected,
            SingleUnselected,
            ContinuousRange,
            MultipleRanges,
            MixedPattern,
            Sparse
        }

        /// <summary>
        /// Optimized rule suggestion
        /// </summary>
        public class OptimizedRule
        {
            public SearchType SearchType { get; set; }
            public object PrimaryValue { get; set; }
            public object SecondaryValue { get; set; }
            public List<object> Values { get; set; } = new List<object>();
            public string Description { get; set; }
            public int ComplexityScore { get; set; }
        }

        /// <summary>
        /// Analyzes selection patterns to determine optimization opportunities
        /// </summary>
        public SelectionAnalysis AnalyzeSelection(IEnumerable<object> allValues, IEnumerable<object> selectedValues, ColumnDataType dataType)
        {
            var allList = allValues?.ToList() ?? new List<object>();
            var selectedList = selectedValues?.ToList() ?? new List<object>();
            var selectedSet = new HashSet<object>(selectedList);
            var unselectedList = allList.Where(v => !selectedSet.Contains(v)).ToList();

            var analysis = new SelectionAnalysis
            {
                DataType = dataType
            };

            // Determine basic pattern
            if (selectedList.Count == 0)
            {
                analysis.Pattern = SelectionPattern.AllUnselected;
                return analysis;
            }
            
            if (selectedList.Count == allList.Count)
            {
                analysis.Pattern = SelectionPattern.AllSelected;
                return analysis;
            }

            if (selectedList.Count == 1)
            {
                analysis.Pattern = SelectionPattern.SingleSelected;
                return analysis;
            }

            if (unselectedList.Count == 1)
            {
                analysis.Pattern = SelectionPattern.SingleUnselected;
                analysis.UseNegation = true;
                return analysis;
            }

            // Sort values for range analysis
            var sortedAll = SortValues(allList, dataType);
            var sortedSelected = SortValues(selectedList, dataType);
            var sortedUnselected = SortValues(unselectedList, dataType);

            // Detect ranges in selected values
            analysis.SelectedRanges = DetectRanges(sortedSelected, dataType);
            analysis.UnselectedRanges = DetectRanges(sortedUnselected, dataType);

            // Determine if negation is more efficient
            analysis.UseNegation = ShouldUseNegation(selectedList.Count, unselectedList.Count, analysis.UnselectedRanges);

            // Classify pattern based on range analysis
            if (analysis.SelectedRanges.Count == 1 && analysis.SelectedRanges[0].Count == selectedList.Count)
            {
                analysis.Pattern = SelectionPattern.ContinuousRange;
            }
            else if (analysis.SelectedRanges.Count > 1)
            {
                analysis.Pattern = SelectionPattern.MultipleRanges;
            }
            else if (analysis.UseNegation && analysis.UnselectedRanges.Count <= 2)
            {
                analysis.Pattern = SelectionPattern.MixedPattern;
            }
            else
            {
                analysis.Pattern = SelectionPattern.Sparse;
            }

            // Calculate efficiency score
            analysis.EfficiencyScore = CalculateEfficiencyScore(analysis, selectedList.Count, unselectedList.Count);

            return analysis;
        }

        /// <summary>
        /// Generates optimized rules based on selection analysis
        /// </summary>
        public List<OptimizedRule> GenerateOptimizedRules(SelectionAnalysis analysis, IEnumerable<object> allValues, IEnumerable<object> selectedValues)
        {
            var rules = new List<OptimizedRule>();
            var selectedList = selectedValues.ToList();
            var allList = allValues.ToList();
            var unselectedList = allList.Where(v => !selectedList.Contains(v)).ToList();

            switch (analysis.Pattern)
            {
                case SelectionPattern.AllSelected:
                    // No rules needed - clear filter
                    break;

                case SelectionPattern.AllUnselected:
                    // This shouldn't happen in normal UI flow, but handle it
                    rules.Add(new OptimizedRule
                    {
                        SearchType = SearchType.IsNull,
                        Description = "No values selected (show empty)"
                    });
                    break;

                case SelectionPattern.SingleSelected:
                    rules.Add(new OptimizedRule
                    {
                        SearchType = SearchType.Equals,
                        PrimaryValue = selectedList.First(),
                        Description = $"Equals {selectedList.First()}",
                        ComplexityScore = 1
                    });
                    break;

                case SelectionPattern.SingleUnselected:
                    rules.Add(new OptimizedRule
                    {
                        SearchType = SearchType.NotEquals,
                        PrimaryValue = unselectedList.First(),
                        Description = $"Does not equal {unselectedList.First()}",
                        ComplexityScore = 1
                    });
                    break;

                case SelectionPattern.ContinuousRange:
                    var selectedRange = analysis.SelectedRanges.First();
                    if (selectedRange.IsSingleValue)
                    {
                        rules.Add(new OptimizedRule
                        {
                            SearchType = SearchType.Equals,
                            PrimaryValue = selectedRange.Start,
                            Description = $"Equals {selectedRange.Start}",
                            ComplexityScore = 1
                        });
                    }
                    else
                    {
                        var searchType = analysis.DataType == ColumnDataType.DateTime ? SearchType.BetweenDates : SearchType.Between;
                        rules.Add(new OptimizedRule
                        {
                            SearchType = searchType,
                            PrimaryValue = selectedRange.Start,
                            SecondaryValue = selectedRange.End,
                            Description = $"Between {selectedRange.Start} and {selectedRange.End}",
                            ComplexityScore = 2
                        });
                    }
                    break;

                case SelectionPattern.MultipleRanges:
                    foreach (var multiRange in analysis.SelectedRanges)
                    {
                        if (multiRange.IsSingleValue)
                        {
                            rules.Add(new OptimizedRule
                            {
                                SearchType = SearchType.Equals,
                                PrimaryValue = multiRange.Start,
                                Description = $"Equals {multiRange.Start}",
                                ComplexityScore = 1
                            });
                        }
                        else
                        {
                            var searchType = analysis.DataType == ColumnDataType.DateTime ? SearchType.BetweenDates : SearchType.Between;
                            rules.Add(new OptimizedRule
                            {
                                SearchType = searchType,
                                PrimaryValue = multiRange.Start,
                                SecondaryValue = multiRange.End,
                                Description = $"Between {multiRange.Start} and {multiRange.End}",
                                ComplexityScore = 2
                            });
                        }
                    }
                    break;

                case SelectionPattern.MixedPattern:
                    if (analysis.UseNegation)
                    {
                        // Generate NOT rules for unselected ranges
                        foreach (var unselectedRange in analysis.UnselectedRanges)
                        {
                            if (unselectedRange.IsSingleValue)
                            {
                                rules.Add(new OptimizedRule
                                {
                                    SearchType = SearchType.NotEquals,
                                    PrimaryValue = unselectedRange.Start,
                                    Description = $"Does not equal {unselectedRange.Start}",
                                    ComplexityScore = 1
                                });
                            }
                            else
                            {
                                var searchType = analysis.DataType == ColumnDataType.DateTime ? SearchType.BetweenDates : SearchType.NotBetween;
                                if (analysis.DataType != ColumnDataType.DateTime)
                                    searchType = SearchType.NotBetween;
                                    
                                rules.Add(new OptimizedRule
                                {
                                    SearchType = searchType,
                                    PrimaryValue = unselectedRange.Start,
                                    SecondaryValue = unselectedRange.End,
                                    Description = $"Not between {unselectedRange.Start} and {unselectedRange.End}",
                                    ComplexityScore = 2
                                });
                            }
                        }
                    }
                    else
                    {
                        // Fall back to selected values
                        goto case SelectionPattern.Sparse;
                    }
                    break;

                case SelectionPattern.Sparse:
                default:
                    // Check for null value patterns first
                    var nullRules = GenerateNullValueRules(selectedList, unselectedList);
                    if (nullRules.Any())
                    {
                        rules.AddRange(nullRules);
                        
                        // Remove null values from further processing
                        var nonNullSelected = selectedList.Where(v => v != null).ToList();
                        var nonNullUnselected = unselectedList.Where(v => v != null).ToList();
                        
                        // Add rules for non-null values if any exist
                        if (nonNullSelected.Any())
                        {
                            if (analysis.UseNegation && nonNullUnselected.Count <= 5 && nonNullUnselected.Any())
                            {
                                rules.Add(new OptimizedRule
                                {
                                    SearchType = SearchType.IsNoneOf,
                                    Values = nonNullUnselected,
                                    Description = $"Is none of {nonNullUnselected.Count} non-null values",
                                    ComplexityScore = nonNullUnselected.Count
                                });
                            }
                            else
                            {
                                rules.Add(new OptimizedRule
                                {
                                    SearchType = nonNullSelected.Count == 1 ? SearchType.Equals : SearchType.IsAnyOf,
                                    PrimaryValue = nonNullSelected.Count == 1 ? nonNullSelected.First() : null,
                                    Values = nonNullSelected.Count > 1 ? nonNullSelected : new List<object>(),
                                    Description = nonNullSelected.Count == 1 ? $"Equals {nonNullSelected.First()}" : $"Is any of {nonNullSelected.Count} values",
                                    ComplexityScore = nonNullSelected.Count
                                });
                            }
                        }
                    }
                    else
                    {
                        // Original logic for non-null scenarios
                        if (analysis.UseNegation && unselectedList.Count <= 5)
                        {
                            rules.Add(new OptimizedRule
                            {
                                SearchType = SearchType.IsNoneOf,
                                Values = unselectedList,
                                Description = $"Is none of {unselectedList.Count} values",
                                ComplexityScore = unselectedList.Count
                            });
                        }
                        else
                        {
                            // Use IsAnyOf for selected values
                            rules.Add(new OptimizedRule
                            {
                                SearchType = selectedList.Count == 1 ? SearchType.Equals : SearchType.IsAnyOf,
                                PrimaryValue = selectedList.Count == 1 ? selectedList.First() : null,
                                Values = selectedList.Count > 1 ? selectedList : new List<object>(),
                                Description = selectedList.Count == 1 ? $"Equals {selectedList.First()}" : $"Is any of {selectedList.Count} values",
                                ComplexityScore = selectedList.Count
                            });
                        }
                    }
                    break;
            }

            return rules;
        }

        /// <summary>
        /// Generates optimized rules for null value selections
        /// </summary>
        private List<OptimizedRule> GenerateNullValueRules(List<object> selectedList, List<object> unselectedList)
        {
            var nullRules = new List<OptimizedRule>();
            
            var hasNullSelected = selectedList.Contains(null);
            var hasNullUnselected = unselectedList.Contains(null);
            
            // Only generate null rules if null is involved in the selection
            if (!hasNullSelected && !hasNullUnselected)
                return nullRules;
            
            if (hasNullSelected && !hasNullUnselected)
            {
                // Null is selected and not in unselected - create IsNull rule
                nullRules.Add(new OptimizedRule
                {
                    SearchType = SearchType.IsNull,
                    Description = "Is null (empty)",
                    ComplexityScore = 1
                });
            }
            else if (!hasNullSelected && hasNullUnselected)
            {
                // Null is not selected but is in unselected - create IsNotNull rule  
                nullRules.Add(new OptimizedRule
                {
                    SearchType = SearchType.IsNotNull,
                    Description = "Is not null (has value)",
                    ComplexityScore = 1
                });
            }
            
            return nullRules;
        }

        private List<object> SortValues(List<object> values, ColumnDataType dataType)
        {
            try
            {
                return dataType switch
                {
                    ColumnDataType.Number => values.OrderBy(v => Convert.ToDecimal(v)).ToList(),
                    ColumnDataType.DateTime => values.OrderBy(v => Convert.ToDateTime(v)).ToList(),
                    _ => values.OrderBy(v => v?.ToString()).ToList()
                };
            }
            catch
            {
                return values.OrderBy(v => v?.ToString()).ToList();
            }
        }

        private List<ValueRange> DetectRanges(List<object> sortedValues, ColumnDataType dataType)
        {
            var ranges = new List<ValueRange>();
            
            if (!sortedValues.Any())
                return ranges;

            var currentRange = new ValueRange { Start = sortedValues[0], End = sortedValues[0], Count = 1 };

            for (int i = 1; i < sortedValues.Count; i++)
            {
                if (IsConsecutive(currentRange.End, sortedValues[i], dataType))
                {
                    currentRange.End = sortedValues[i];
                    currentRange.Count++;
                }
                else
                {
                    ranges.Add(currentRange);
                    currentRange = new ValueRange { Start = sortedValues[i], End = sortedValues[i], Count = 1 };
                }
            }
            
            ranges.Add(currentRange);
            return ranges;
        }

        private bool IsConsecutive(object current, object next, ColumnDataType dataType)
        {
            try
            {
                return dataType switch
                {
                    ColumnDataType.Number => Math.Abs(Convert.ToDecimal(next) - Convert.ToDecimal(current)) <= 1,
                    ColumnDataType.DateTime => (Convert.ToDateTime(next) - Convert.ToDateTime(current)).TotalDays <= 1,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldUseNegation(int selectedCount, int unselectedCount, List<ValueRange> unselectedRanges)
        {
            // Use negation if:
            // 1. Fewer unselected values than selected
            // 2. Unselected values form simple patterns (1-2 ranges)
            return unselectedCount < selectedCount && unselectedCount > 0 && unselectedRanges.Count <= 2;
        }

        private int CalculateEfficiencyScore(SelectionAnalysis analysis, int selectedCount, int unselectedCount)
        {
            // Lower score = more efficient
            return analysis.Pattern switch
            {
                SelectionPattern.AllSelected => 0,
                SelectionPattern.SingleSelected => 1,
                SelectionPattern.SingleUnselected => 1,
                SelectionPattern.ContinuousRange => 2,
                SelectionPattern.MultipleRanges => analysis.SelectedRanges.Count * 2,
                SelectionPattern.MixedPattern => analysis.UseNegation ? unselectedCount : selectedCount,
                _ => Math.Min(selectedCount, unselectedCount)
            };
        }
    }
}