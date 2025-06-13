using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Core controller for managing search templates and groups
    /// </summary>
    public class SearchTemplateController : ObservableObject
    {
        #region Fields

        private bool hasCustomExpression;
        private HashSet<Tuple<string, string>> displayValueMappings;
        private bool isTemplateItemMoving;
        private Type targetColumnType;
        private ColumnDataType columnDataType = ColumnDataType.String;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        public object ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the set of column values
        /// </summary>
        public HashSet<object> ColumnValues { get; set; } = new HashSet<object>();

        /// <summary>
        /// Gets or sets the column data type
        /// </summary>
        public ColumnDataType ColumnDataType
        {
            get => columnDataType;
            set
            {
                if (SetProperty(value, ref columnDataType))
                {
                    // Update all templates with the new data type
                    foreach (var group in SearchGroups)
                    {
                        foreach (var template in group.SearchTemplates.OfType<SearchTemplate>())
                        {
                            template.ColumnDataType = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of available logical operators
        /// </summary>
        [JsonIgnore]
        public List<string> LogicalOperators { get; } = new List<string> { "And", "Or" };

        /// <summary>
        /// Gets or sets the custom filter expression
        /// </summary>
        [JsonIgnore]
        public Func<object, bool> FilterExpression { get; set; }

        /// <summary>
        /// Gets or sets whether a custom expression has been applied
        /// </summary>
        public bool HasCustomExpression
        {
            get => hasCustomExpression;
            set => SetProperty(value, ref hasCustomExpression);
        }

        /// <summary>
        /// Gets whether any search templates have unsaved changes
        /// </summary>
        public bool HasUnsavedChanges => SearchGroups.Any(g => g.SearchTemplates.Any(t => t.HasChanges));

        /// <summary>
        /// Gets or sets the dictionary of property values
        /// </summary>
        public Dictionary<string, List<object>> PropertyValues { get; set; } = new Dictionary<string, List<object>>();

        /// <summary>
        /// Gets the collection of search groups
        /// </summary>
        public ObservableCollection<SearchTemplateGroup> SearchGroups { get; } = new ObservableCollection<SearchTemplateGroup>();

        /// <summary>
        /// Gets or sets the type of search template to create (always SearchTemplate now)
        /// </summary>
        public Type SearchTemplateType { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of column values by binding path
        /// </summary>
        public Dictionary<string, HashSet<object>> ColumnValuesByPath { get; set; } = new Dictionary<string, HashSet<object>>();

        /// <summary>
        /// Gets whether group management UI should be visible (false for single group per column)
        /// </summary>
        public bool AllowMultipleGroups => false; // Enforce single group per column

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SearchTemplateController class
        /// </summary>
        public SearchTemplateController() : this(typeof(SearchTemplate))
        {
        }

        /// <summary>
        /// Initializes a new instance of the SearchTemplateController class
        /// </summary>
        /// <param name="searchTemplateType">Type of search template to create (legacy parameter, always uses SearchTemplate)</param>
        public SearchTemplateController(Type searchTemplateType)
        {
            // Always use SearchTemplate regardless of what's passed
            SearchTemplateType = typeof(SearchTemplate);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluates collection-context filters like TopN, BottomN, AboveAverage, etc.
        /// </summary>
        /// <param name="items">The complete collection of items to filter</param>
        /// <param name="bindingPath">The property path to evaluate</param>
        /// <param name="template">The search template with the filter criteria</param>
        /// <returns>A filtered collection of items</returns>
        public IEnumerable<object> EvaluateCollectionContextFilters(IEnumerable<object> items, string bindingPath, SearchTemplate template)
        {
            if (items == null || string.IsNullOrEmpty(bindingPath))
                return items;

            var searchCondition = new SearchCondition(
                targetColumnType,
                template.SearchType,
                template.SelectedValue,
                template.SelectedSecondaryValue,
                template.SelectedValue != null ? (int?)Convert.ToInt32(template.SelectedValue) : null);

            // Get property values from all items
            var propertyValues = items
                .Select(item => GetPropertyValue(item, bindingPath))
                .Where(value => value != null)
                .ToList();

            switch (template.SearchType)
            {
                case SearchType.TopN:
                    if (searchCondition.CountValue.HasValue)
                    {
                        // For numeric values, sort and take top N
                        if (propertyValues.All(v => v is IComparable))
                        {
                            var sortedItems = items
                                .Select(item => new { Item = item, Value = GetPropertyValue(item, bindingPath) })
                                .Where(x => x.Value != null && x.Value is IComparable)
                                .OrderByDescending(x => x.Value)
                                .Take(searchCondition.CountValue.Value)
                                .Select(x => x.Item);

                            return sortedItems;
                        }
                    }
                    break;

                case SearchType.BottomN:
                    if (searchCondition.CountValue.HasValue)
                    {
                        // For numeric values, sort and take bottom N
                        if (propertyValues.All(v => v is IComparable))
                        {
                            var sortedItems = items
                                .Select(item => new { Item = item, Value = GetPropertyValue(item, bindingPath) })
                                .Where(x => x.Value != null && x.Value is IComparable)
                                .OrderBy(x => x.Value)
                                .Take(searchCondition.CountValue.Value)
                                .Select(x => x.Item);

                            return sortedItems;
                        }
                    }
                    break;

                case SearchType.AboveAverage:
                    // For numeric values, calculate average and filter
                    if (propertyValues.All(v => v is IConvertible))
                    {
                        double average = propertyValues
                            .Cast<IConvertible>()
                            .Select(v => Convert.ToDouble(v))
                            .Average();

                        return items
                            .Where(item =>
                            {
                                var value = GetPropertyValue(item, bindingPath);
                                return value != null && value is IConvertible &&
                                       Convert.ToDouble(value) > average;
                            });
                    }
                    break;

                case SearchType.BelowAverage:
                    // For numeric values, calculate average and filter
                    if (propertyValues.All(v => v is IConvertible))
                    {
                        double average = propertyValues
                            .Cast<IConvertible>()
                            .Select(v => Convert.ToDouble(v))
                            .Average();

                        return items
                            .Where(item =>
                            {
                                var value = GetPropertyValue(item, bindingPath);
                                return value != null && value is IConvertible &&
                                       Convert.ToDouble(value) < average;
                            });
                    }
                    break;

                case SearchType.Unique:
                    // Group by value and select items with unique values
                    return items
                        .GroupBy(item => GetPropertyValue(item, bindingPath)?.ToString())
                        .Where(g => g.Count() == 1)
                        .SelectMany(g => g);

                case SearchType.Duplicate:
                    // Group by value and select items with duplicate values
                    return items
                        .GroupBy(item => GetPropertyValue(item, bindingPath)?.ToString())
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g);
            }

            return items;
        }

        /// <summary>
        /// Gets a property value from an object using a binding path
        /// </summary>
        private object GetPropertyValue(object item, string bindingPath)
        {
            if (item == null || string.IsNullOrEmpty(bindingPath))
                return null;

            // Simple property access - in a real implementation, you'd want to use
            // reflection or a more robust property path resolver
            var properties = bindingPath.Split('.');
            object value = item;

            foreach (var prop in properties)
            {
                var propInfo = value.GetType().GetProperty(prop);
                if (propInfo == null)
                    return null;

                value = propInfo.GetValue(value);
                if (value == null)
                    return null;
            }

            return value;
        }

        /// <summary>
        /// Adds a new search group (limited to one group per column)
        /// </summary>
        /// <param name="canAddGroup">Whether a group can be added</param>
        /// <param name="markAsChanged">Whether to mark the group as changed</param>
        /// <param name="referenceGroup">Reference group for positioning (ignored in single group mode)</param>
        public void AddSearchGroup(bool canAddGroup = true, bool markAsChanged = true, SearchTemplateGroup referenceGroup = null)
        {
            if (canAddGroup && SearchGroups.Count == 0)
            {
                // Only allow one group per column
                var newGroup = new SearchTemplateGroup();
                SearchGroups.Add(newGroup);
                AddSearchTemplate(true, markAsChanged, null, newGroup);
            }

            if (SearchGroups.Count > 0)
            {
                UpdateOperatorVisibility(SearchGroups[0]);
                UpdateGroupNumbers();
            }
        }

        /// <summary>
        /// Adds a new search template to a group
        /// </summary>
        /// <param name="canAddTemplate">Whether a template can be added</param>
        /// <param name="markAsChanged">Whether to mark the template as changed</param>
        /// <param name="referenceTemplate">Reference template for positioning</param>
        /// <param name="group">Group to add the template to</param>
        public void AddSearchTemplate(bool canAddTemplate = true, bool markAsChanged = true, SearchTemplate referenceTemplate = null, SearchTemplateGroup group = null)
        {
            if (canAddTemplate)
            {
                var targetGroup = group ?? SearchGroups.First(g => g.SearchTemplates.Contains(referenceTemplate));

                // Create new SearchTemplate with column data type
                var newTemplate = new SearchTemplate(ColumnValues, ColumnDataType);
                newTemplate.HasChanges = markAsChanged;

                if (referenceTemplate == null)
                {
                    targetGroup.SearchTemplates.Add(newTemplate);
                }
                else
                {
                    targetGroup.SearchTemplates.Insert(targetGroup.SearchTemplates.IndexOf(referenceTemplate) + 1, newTemplate);
                }
            }

            SearchGroups.ForEach(g =>
            {
                if (g.SearchTemplates.Count > 0)
                {
                    UpdateOperatorVisibility(g.SearchTemplates[0]);
                }
            });
        }

        /// <summary>
        /// Loads column data into the search templates
        /// </summary>
        /// <param name="header">Column header</param>
        /// <param name="values">Column values</param>
        /// <param name="displayValueMappings">Mappings for display values</param>
        /// <param name="bindingPath">The binding path for the column</param>
        public void LoadColumnData(
            object header,
            HashSet<object> values,
            HashSet<Tuple<string, string>> displayValueMappings = null,
            string bindingPath = null)
        {
            this.displayValueMappings = displayValueMappings;
            ColumnValues = new HashSet<object>(values);
            ColumnName = header;

            // Auto-detect column data type
            if (values.Any())
            {
                ColumnDataType = ReflectionHelper.DetermineColumnDataType(values);
            }

            // Store column values by binding path for global filtering
            if (!string.IsNullOrEmpty(bindingPath))
            {
                ColumnValuesByPath[bindingPath] = new HashSet<object>(values);
            }

            AddSearchGroup(SearchGroups.Count == 0, false);
            SearchGroups.ForEach(g => g.SearchTemplates.ForEach(t => t.LoadAvailableValues(ColumnValues)));
        }

        /// <summary>
        /// Updates the filter expression based on current templates
        /// </summary>
        /// <param name="forceTargetTypeAsString">Whether to force the target type to string</param>
        public void UpdateFilterExpression(bool forceTargetTypeAsString = false)
        {
            try
            {
                Expression<Func<object, bool>> groupExpression = null;

                if (forceTargetTypeAsString)
                {
                    targetColumnType = typeof(string);
                }
                else
                {
                    // Determine column type from available values or metadata
                    targetColumnType = DetermineTargetColumnType();
                }

                // Track if we have collection-context filters that need special handling
                bool hasCollectionContextFilters = false;

                foreach (var group in SearchGroups)
                {
                    Expression<Func<object, bool>> templateExpression = null;

                    foreach (var template in group.SearchTemplates)
                    {
                        template.HasChanges = false;

                        // Check if this is a collection-context filter
                        if (template.SearchType == SearchType.TopN ||
                            template.SearchType == SearchType.BottomN ||
                            template.SearchType == SearchType.AboveAverage ||
                            template.SearchType == SearchType.BelowAverage ||
                            template.SearchType == SearchType.Unique ||
                            template.SearchType == SearchType.Duplicate)
                        {
                            hasCollectionContextFilters = true;
                            continue; // Skip for now, will be handled separately
                        }

                        Expression<Func<object, bool>> currentExpression;

                        try
                        {
                            // All templates now implement BuildExpression
                            currentExpression = template.BuildExpression(targetColumnType);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error building expression for template: {ex.Message}");

                            // Fallback to basic expression
                            var searchCondition = new SearchCondition(
                                targetColumnType,
                                template.SearchType,
                                template.SelectedValue,
                                template.SelectedSecondaryValue);

                            currentExpression = obj => SearchEngine.EvaluateCondition(obj, searchCondition);
                        }

                        // Combine with previous expressions in this group
                        templateExpression = templateExpression == null
                            ? currentExpression
                            : templateExpression.Compose(currentExpression, template.OperatorFunction);
                    }

                    // Skip empty groups
                    if (templateExpression == null)
                        continue;

                    // Combine with previous group expressions
                    groupExpression = groupExpression == null
                        ? templateExpression
                        : groupExpression.Compose(templateExpression, group.OperatorFunction);
                }

                // Compile the expression for non-collection-context filters
                if (groupExpression != null)
                {
                    FilterExpression = groupExpression.Compile();
                }
                else if (hasCollectionContextFilters)
                {
                    // For collection-context filters, we'll need to handle them differently
                    // This would typically be done at the data grid level or in the UI
                    FilterExpression = obj => true; // Placeholder, actual filtering done elsewhere
                }
                else
                {
                    FilterExpression = null;
                }

                // Update the custom expression flag
                HasCustomExpression = SearchGroups.Count > 0 &&
                    (SearchGroups.Count > 1 || SearchGroups.Any(g => g.SearchTemplates.Any(t => t.HasCustomFilter)));

                OnPropertyChanged(string.Empty);
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                Console.WriteLine($"Error updating filter expression: {ex}");
            }
        }

        /// <summary>
        /// Moves a search template from one group to another
        /// </summary>
        /// <param name="sourceGroup">Source group</param>
        /// <param name="targetGroup">Target group</param>
        /// <param name="template">Template to move</param>
        /// <param name="targetIndex">Target index in the target group</param>
        public void MoveSearchTemplate(
            SearchTemplateGroup sourceGroup,
            SearchTemplateGroup targetGroup,
            SearchTemplate template,
            int targetIndex)
        {
            isTemplateItemMoving = true;

            sourceGroup.SearchTemplates.Remove(template);
            targetGroup.SearchTemplates.Insert(targetIndex, template);

            if (sourceGroup.SearchTemplates.Count == 0)
            {
                SearchGroups.Remove(sourceGroup);
            }

            if (SearchGroups.Count > 0)
            {
                UpdateOperatorVisibility(SearchGroups[0]);

                SearchGroups.ForEach(g =>
                {
                    if (g.SearchTemplates.Count > 0)
                    {
                        g.SearchTemplates.Skip(1).ForEach(t => t.IsOperatorVisible = true);
                        UpdateOperatorVisibility(g.SearchTemplates[0]);
                    }
                });

                UpdateGroupNumbers();
            }

            isTemplateItemMoving = false;
        }

        /// <summary>
        /// Removes a search group (prevented in single group mode)
        /// </summary>
        /// <param name="group">Group to remove</param>
        public void RemoveSearchGroup(SearchTemplateGroup group)
        {
            // In single group mode, don't allow removing the only group
            // Instead, clear its templates and reset it
            if (SearchGroups.Count == 1 && SearchGroups.Contains(group))
            {
                group.SearchTemplates.Clear();
                AddSearchTemplate(true, false, null, group);
                UpdateFilterExpression();
            }
            else if (SearchGroups.Count > 1)
            {
                // Legacy support for multiple groups (though we enforce single group)
                group.SearchTemplates.Clear();
                SearchGroups.Remove(group);
                UpdateFilterExpression();
                AddSearchGroup(SearchGroups.Count == 0);
                UpdateGroupNumbers();
            }
            
            OnPropertyChanged(nameof(SearchGroups));
        }

        /// <summary>
        /// Removes a search template
        /// </summary>
        /// <param name="template">Template to remove</param>
        public void RemoveSearchTemplate(SearchTemplate template)
        {
            var group = SearchGroups.First(g => g.SearchTemplates.Contains(template));

            if (group.SearchTemplates.Count > 1)
            {
                group.SearchTemplates.Remove(template);
                UpdateFilterExpression();
                AddSearchTemplate(group.SearchTemplates.Count == 0, true, template, group);
            }
        }

        /// <summary>
        /// Gets column values for a specific binding path (for global filtering)
        /// </summary>
        /// <param name="bindingPath">Binding path to retrieve values for</param>
        /// <returns>HashSet of values for the specified binding path</returns>
        public HashSet<object> GetColumnValuesForPath(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath))
                return new HashSet<object>();

            if (ColumnValuesByPath.TryGetValue(bindingPath, out var values))
                return values;

            return new HashSet<object>();
        }

        /// <summary>
        /// Gets a human-readable display text for the current filter state
        /// </summary>
        /// <returns>Description of the active filters</returns>
        public string GetFilterDisplayText()
        {
            try
            {
                if (!HasCustomExpression || SearchGroups.Count == 0)
                    return "No filter";

                var groupTexts = new List<(string text, SearchTemplateGroup group)>();

                foreach (var group in SearchGroups)
                {
                    var templateTexts = new List<(string text, SearchTemplate template)>();

                    foreach (var template in group.SearchTemplates)
                    {
                        if (template.HasCustomFilter)
                        {
                            var templateText = GetTemplateDisplayText(template);
                            if (!string.IsNullOrWhiteSpace(templateText))
                            {
                                templateTexts.Add((templateText, template));
                            }
                        }
                    }

                    if (templateTexts.Count > 0)
                    {
                        string groupText;
                        if (templateTexts.Count == 1)
                        {
                            groupText = templateTexts[0].text;
                        }
                        else
                        {
                            // Combine templates within the group using their operators
                            var combinedText = new System.Text.StringBuilder();
                            combinedText.Append(templateTexts[0].text);

                            for (int i = 1; i < templateTexts.Count; i++)
                            {
                                var operatorName = templateTexts[i].template.OperatorName?.ToUpper() ?? "AND";
                                combinedText.Append($" {operatorName} ");
                                combinedText.Append(templateTexts[i].text);
                            }

                            groupText = $"({combinedText})";
                        }

                        groupTexts.Add((groupText, group));
                    }
                }

                if (groupTexts.Count == 0)
                    return "No filter";

                if (groupTexts.Count == 1)
                    return groupTexts[0].text;

                // Combine groups using their operators
                var result = new System.Text.StringBuilder();
                result.Append(groupTexts[0].text);

                for (int i = 1; i < groupTexts.Count; i++)
                {
                    var operatorName = groupTexts[i].group.OperatorName?.ToUpper() ?? "OR";
                    result.Append($" {operatorName} ");
                    result.Append(groupTexts[i].text);
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetFilterDisplayText: {ex.Message}");
                return "Advanced filter";
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Gets display text for a single search template
        /// </summary>
        private string GetTemplateDisplayText(SearchTemplate template)
        {
            try
            {
                var value = template.SelectedValue?.ToString();
                var secondaryValue = template.SelectedSecondaryValue?.ToString();

                switch (template.SearchType)
                {
                    case SearchType.Contains:
                        return $"Contains '{value}'";
                    case SearchType.DoesNotContain:
                        return $"Does not contain '{value}'";
                    case SearchType.Equals:
                        return $"= '{value}'";
                    case SearchType.NotEquals:
                        return $"≠ '{value}'";
                    case SearchType.StartsWith:
                        return $"Starts with '{value}'";
                    case SearchType.EndsWith:
                        return $"Ends with '{value}'";
                    case SearchType.IsEmpty:
                        return "Is empty";
                    case SearchType.IsNotEmpty:
                        return "Is not empty";
                    case SearchType.Between:
                        return $"Between '{value}' and '{secondaryValue}'";
                    case SearchType.NotBetween:
                        return $"Not between '{value}' and '{secondaryValue}'";
                    case SearchType.BetweenDates:
                        return $"Between dates '{FormatDateValue(value)}' and '{FormatDateValue(secondaryValue)}'";
                    case SearchType.GreaterThan:
                        return $"> '{value}'";
                    case SearchType.GreaterThanOrEqualTo:
                        return $">= '{value}'";
                    case SearchType.LessThan:
                        return $"< '{value}'";
                    case SearchType.LessThanOrEqualTo:
                        return $"<= '{value}'";
                    case SearchType.IsLike:
                        return $"Is like '{value}'";
                    case SearchType.IsNotLike:
                        return $"Is not like '{value}'";
                    case SearchType.IsNull:
                        return "Is null";
                    case SearchType.IsNotNull:
                        return "Is not null";
                    case SearchType.TopN:
                        return $"Top {value}";
                    case SearchType.BottomN:
                        return $"Bottom {value}";
                    case SearchType.AboveAverage:
                        return "Above average";
                    case SearchType.BelowAverage:
                        return "Below average";
                    case SearchType.Unique:
                        return "Unique values";
                    case SearchType.Duplicate:
                        return "Duplicate values";
                    case SearchType.Yesterday:
                        return "Is yesterday";
                    case SearchType.Today:
                        return "Is today";

                    // Multi-value filters
                    case SearchType.IsAnyOf:
                        return FormatMultiValueFilter("Is any of", template.SelectedValues);
                    case SearchType.IsNoneOf:
                        return FormatMultiValueFilter("Is none of", template.SelectedValues);
                    case SearchType.IsOnAnyOfDates:
                        return FormatDateListFilter("Is on any of", template.SelectedDates);
                    case SearchType.DateInterval:
                        return FormatDateIntervalFilter(template.DateIntervals);

                    default:
                        return template.SearchType.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetTemplateDisplayText: {ex.Message}");
                return "Filter";
            }
        }

        private string FormatDateValue(string value)
        {
            if (DateTime.TryParse(value, out DateTime date))
            {
                return date.ToString("yyyy-MM-dd");
            }
            return value;
        }

        private string FormatMultiValueFilter(string prefix, System.Collections.IEnumerable selectedValues)
        {
            if (selectedValues == null)
                return prefix + " (no values)";

            var values = new List<string>();
            foreach (var item in selectedValues)
            {
                if (item is FilterListValue filterValue)
                {
                    values.Add(filterValue.Value?.ToString() ?? "(null)");
                }
                else
                {
                    values.Add(item?.ToString() ?? "(null)");
                }
            }

            if (values.Count == 0)
                return prefix + " (no values)";

            if (values.Count <= 3)
            {
                return $"{prefix} [{string.Join(", ", values.Select(v => $"'{v}'"))}]";
            }
            else
            {
                return $"{prefix} [{string.Join(", ", values.Take(2).Select(v => $"'{v}'"))} and {values.Count - 2} more]";
            }
        }

        private string FormatDateListFilter(string prefix, System.Collections.IEnumerable selectedDates)
        {
            if (selectedDates == null)
                return prefix + " (no dates)";

            var dates = new List<string>();
            foreach (var date in selectedDates)
            {
                if (date is DateTime dt)
                {
                    dates.Add(dt.ToString("yyyy-MM-dd"));
                }
            }

            if (dates.Count == 0)
                return prefix + " (no dates)";

            if (dates.Count <= 3)
            {
                return $"{prefix} [{string.Join(", ", dates)}]";
            }
            else
            {
                return $"{prefix} [{string.Join(", ", dates.Take(2))} and {dates.Count - 2} more]";
            }
        }

        private string FormatDateIntervalFilter(System.Collections.IEnumerable dateIntervals)
        {
            if (dateIntervals == null)
                return "Date intervals (none)";

            var selectedIntervals = new List<string>();
            foreach (var item in dateIntervals)
            {
                if (item is DateIntervalItem intervalItem && intervalItem.IsSelected)
                {
                    selectedIntervals.Add(intervalItem.DisplayName);
                }
            }

            if (selectedIntervals.Count == 0)
                return "Date intervals (none selected)";

            if (selectedIntervals.Count <= 2)
            {
                return $"Date intervals [{string.Join(", ", selectedIntervals)}]";
            }
            else
            {
                return $"Date intervals [{string.Join(", ", selectedIntervals.Take(2))} and {selectedIntervals.Count - 2} more]";
            }
        }

        /// <summary>
        /// Updates the visibility of logical operators
        /// </summary>
        /// <param name="operator">Operator to update</param>
        private void UpdateOperatorVisibility(ILogicalOperatorProvider @operator)
        {
            @operator.IsOperatorVisible = false;
            @operator.OperatorFunction = Expression.Or;
            @operator.OperatorName = "Or";
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Updates group numbers for all groups
        /// </summary>
        private void UpdateGroupNumbers()
        {
            for (int i = 0; i < SearchGroups.Count; i++)
            {
                SearchGroups[i].GroupNumber = i + 1;
            }
        }

        private Type DetermineTargetColumnType()
        {
            // First, try to use the explicitly set ColumnDataType
            switch (ColumnDataType)
            {
                case ColumnDataType.DateTime:
                    return typeof(DateTime);
                case ColumnDataType.Number:
                    // Try to determine the specific numeric type from values
                    return DetermineNumericType();
                case ColumnDataType.Boolean:
                    return typeof(bool);
                case ColumnDataType.Enum:
                    // Try to determine the enum type from values
                    return DetermineEnumType();
                default:
                    return typeof(string);
            }
        }

        private Type DetermineNumericType()
        {
            if (ColumnValues?.Any() == true)
            {
                var firstNumericValue = ColumnValues.FirstOrDefault(v => v != null && IsNumericValue(v));
                if (firstNumericValue != null)
                {
                    return firstNumericValue.GetType();
                }
            }
            return typeof(decimal); // Default to decimal for numeric operations
        }

        private Type DetermineEnumType()
        {
            if (ColumnValues?.Any() == true)
            {
                var firstEnumValue = ColumnValues.FirstOrDefault(v => v != null && v.GetType().IsEnum);
                if (firstEnumValue != null)
                {
                    return firstEnumValue.GetType();
                }
            }
            return typeof(string); // Fallback to string
        }

        private bool IsNumericValue(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        #endregion
    }
}