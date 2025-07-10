using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using WWSearchDataGrid.Modern.Core.Services;

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
        private Type targetColumnType;
        private ColumnDataType columnDataType = ColumnDataType.String;
        
        // Service dependencies
        private readonly IFilterExpressionBuilder _filterExpressionBuilder;
        private readonly SearchTemplateValidator _validator;
        private readonly ColumnValueLoader _columnValueLoader;

        private SearchType? defaultSearchType;

        // Cache connection tracking
        private string _connectedColumnKey;
        private Performance.ColumnValueCache _connectedCache;
        private bool _providersRegistered;

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
        /// Gets or sets the default search type for new templates
        /// </summary>
        public SearchType? DefaultSearchType
        {
            get => defaultSearchType;
            set => SetProperty(value, ref defaultSearchType);
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

        private bool allowMultipleGroups = false;

        /// <summary>
        /// Gets or sets whether multiple groups are allowed (defaults to false for backward compatibility)
        /// </summary>
        public bool AllowMultipleGroups 
        { 
            get => allowMultipleGroups; 
            set => SetProperty(value, ref allowMultipleGroups); 
        }

        /// <summary>
        /// Gets or sets the grouped filter combinations for grouped filtering scenarios
        /// </summary>
        [JsonIgnore]
        public List<(object GroupKey, object ChildValue)> GroupedFilterCombinations { get; set; }

        /// <summary>
        /// Gets or sets the group by column path for grouped filtering
        /// </summary>
        [JsonIgnore]
        public string GroupByColumnPath { get; set; }

        /// <summary>
        /// Gets or sets the current column path for grouped filtering
        /// </summary>
        [JsonIgnore]
        public string CurrentColumnPath { get; set; }

        /// <summary>
        /// Gets or sets all group data for grouped filtering analysis
        /// </summary>
        [JsonIgnore]
        public Dictionary<object, List<object>> AllGroupData { get; set; }

        /// <summary>
        /// Gets whether this is a grouped filtering scenario
        /// </summary>
        [JsonIgnore]
        public bool IsGroupedFiltering => GroupedFilterCombinations?.Any() == true && 
                                          !string.IsNullOrEmpty(GroupByColumnPath) && 
                                          !string.IsNullOrEmpty(CurrentColumnPath);

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
            
            // Initialize services
            _filterExpressionBuilder = new FilterExpressionBuilder();
            _validator = new SearchTemplateValidator();
            _columnValueLoader = new ColumnValueLoader();
        }
        
        /// <summary>
        /// Initializes a new instance with custom service dependencies for testing
        /// </summary>
        /// <param name="filterExpressionBuilder">Filter expression builder service</param>
        /// <param name="validator">Template validator service</param>
        /// <param name="columnValueLoader">Column value loader service</param>
        internal SearchTemplateController(
            IFilterExpressionBuilder filterExpressionBuilder,
            SearchTemplateValidator validator,
            ColumnValueLoader columnValueLoader)
        {
            SearchTemplateType = typeof(SearchTemplate);
            _filterExpressionBuilder = filterExpressionBuilder ?? throw new ArgumentNullException(nameof(filterExpressionBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _columnValueLoader = columnValueLoader ?? throw new ArgumentNullException(nameof(columnValueLoader));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all search groups and adds a default empty group back
        /// This ensures the UI always has something to display
        /// </summary>
        public void ClearAndReset()
        {
            SearchGroups.Clear();
            AddSearchGroup(true, false); // Add default group without marking as changed
            HasCustomExpression = false;
        }

        /// <summary>
        /// Adds a new search group
        /// </summary>
        /// <param name="canAddGroup">Whether a group can be added</param>
        /// <param name="markAsChanged">Whether to mark the group as changed</param>
        /// <param name="referenceGroup">Reference group for positioning</param>
        public void AddSearchGroup(bool canAddGroup = true, bool markAsChanged = true, SearchTemplateGroup referenceGroup = null)
        {
            if (!canAddGroup) return;
            
            // Validate the operation
            var validationResult = _validator.ValidateAddSearchGroup(SearchGroups, AllowMultipleGroups);
            if (!validationResult.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot add search group: {validationResult.ErrorMessage}");
                return;
            }

            var newGroup = new SearchTemplateGroup();
            
            if (referenceGroup != null && SearchGroups.Contains(referenceGroup))
            {
                int insertIndex = SearchGroups.IndexOf(referenceGroup) + 1;
                SearchGroups.Insert(insertIndex, newGroup);
            }
            else
            {
                SearchGroups.Add(newGroup);
            }

            AddSearchTemplate(markAsChanged, null, newGroup);

            if (SearchGroups.Count > 0)
            {
                UpdateOperatorVisibility();
                UpdateGroupNumbers();
            }
        }


        /// <summary>
        /// Adds a new search template to a group with an optional default search type
        /// </summary>
        /// <param name="markAsChanged">Whether to mark the template as changed</param>
        /// <param name="referenceTemplate">Reference template for positioning</param>
        /// <param name="group">Group to add the template to</param>
        /// <param name="defaultSearchType">Optional default search type to use if compatible</param>
        public void AddSearchTemplate(bool markAsChanged = true, SearchTemplate referenceTemplate = null, SearchTemplateGroup group = null)
        {
            SearchTemplateGroup targetGroup = DetermineTargetGroup(group, referenceTemplate);
            
            // Validate the operation
            var validationResult = _validator.ValidateAddSearchTemplate(targetGroup, SearchGroups);
            if (!validationResult.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot add search template: {validationResult.ErrorMessage}");
                return;
            }

            // Create new SearchTemplate with column data type
            var newTemplate = new SearchTemplate(ColumnDataType)
            {
                HasChanges = markAsChanged
            };

            // Apply default search type if provided and compatible
            ApplyDefaultSearchType(newTemplate, defaultSearchType);

            // Connect to cache if available
            if (_connectedCache != null && !string.IsNullOrEmpty(_connectedColumnKey))
            {
                newTemplate.ConnectToSharedSource(_connectedColumnKey, _connectedCache);
                
                // Note: Provider registration will be handled by the WPF layer
                // We cannot register here because we don't have access to the WPF dispatcher
            }
            else if (ColumnValues != null && ColumnValues.Any())
            {
                // Fallback to traditional method if cache not connected
                newTemplate.LoadAvailableValues(ColumnValues);
            }

            // Add the template at the appropriate position
            if (referenceTemplate == null)
            {
                // No reference template - add to the end
                targetGroup.SearchTemplates.Add(newTemplate);
            }
            else
            {
                // Insert after the reference template if it exists in the target group
                int referenceIndex = targetGroup.SearchTemplates.IndexOf(referenceTemplate);
                if (referenceIndex >= 0)
                {
                    targetGroup.SearchTemplates.Insert(referenceIndex + 1, newTemplate);
                }
                else
                {
                    // Reference template not in target group - add to the end
                    targetGroup.SearchTemplates.Add(newTemplate);
                }
            }

            UpdateOperatorVisibility();
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
            // Use the column value loader service
            var loadResult = _columnValueLoader.LoadColumnData(header, values, displayValueMappings, bindingPath);
            
            if (!loadResult.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading column data: {loadResult.ErrorMessage}");
                return;
            }
            
            this.displayValueMappings = loadResult.DisplayValueMappings;
            ColumnValues = loadResult.ColumnValues;
            ColumnName = loadResult.ColumnName;
            ColumnDataType = loadResult.ColumnDataType;

            // Store column values by binding path for global filtering
            if (!string.IsNullOrEmpty(bindingPath))
            {
                ColumnValuesByPath[bindingPath] = new HashSet<object>(values);
            }

            AddSearchGroup(SearchGroups.Count == 0, false);
            SearchGroups.ForEach(g => g.SearchTemplates.ForEach(t => t.LoadAvailableValues(ColumnValues)));
            
            // Ensure operator visibility is properly set after loading
            UpdateOperatorVisibility();
        }

        /// <summary>
        /// Updates the filter expression based on current templates
        /// </summary>
        /// <param name="forceTargetTypeAsString">Whether to force the target type to string</param>
        public void UpdateFilterExpression(bool forceTargetTypeAsString = false)
        {
            try
            {
                // Determine target column type
                if (forceTargetTypeAsString)
                {
                    targetColumnType = typeof(string);
                }
                else
                {
                    targetColumnType = _filterExpressionBuilder.DetermineTargetColumnType(ColumnDataType, ColumnValues);
                }

                // Use the filter expression builder service
                var result = _filterExpressionBuilder.BuildFilterExpression(SearchGroups, targetColumnType, forceTargetTypeAsString);
                
                if (!result.IsSuccess)
                {
                    FilterExpression = null;
                    HasCustomExpression = false;
                }
                else
                {
                    FilterExpression = result.FilterExpression;
                    HasCustomExpression = result.HasCustomExpression;
                }

                OnPropertyChanged(string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating filter expression: {ex}");
                FilterExpression = null;
                HasCustomExpression = false;
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
            // Validate the move operation
            var validationResult = _validator.ValidateTemplateMoveOperation(
                sourceGroup, targetGroup, template, targetIndex, SearchGroups);
                
            if (!validationResult.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot move template: {validationResult.ErrorMessage}");
                return;
            }

            sourceGroup.SearchTemplates.Remove(template);
            targetGroup.SearchTemplates.Insert(targetIndex, template);

            if (sourceGroup.SearchTemplates.Count == 0)
            {
                SearchGroups.Remove(sourceGroup);
            }

            if (SearchGroups.Count > 0)
            {
                UpdateOperatorVisibility();
                UpdateGroupNumbers();
            }
        }

        /// <summary>
        /// Removes a search group
        /// </summary>
        /// <param name="group">Group to remove</param>
        public void RemoveSearchGroup(SearchTemplateGroup group)
        {
            // Validate the removal operation
            var validationResult = _validator.ValidateRemoveSearchGroup(group, SearchGroups, AllowMultipleGroups);
            if (!validationResult.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot remove search group: {validationResult.ErrorMessage}");
                return;
            }
            
            if (!SearchGroups.Contains(group)) return;

            // If multiple groups are allowed, or this isn't the last group, remove it normally
            if (AllowMultipleGroups && SearchGroups.Count > 1)
            {
                group.SearchTemplates.Clear();
                SearchGroups.Remove(group);
                UpdateFilterExpression();
                UpdateGroupNumbers();
                
                // Update operator visibility on the first remaining group
                if (SearchGroups.Count > 0)
                {
                    UpdateOperatorVisibility();
                }
            }
            else if (SearchGroups.Count == 1)
            {
                // For the last remaining group, clear and reset instead of removing
                group.SearchTemplates.Clear();
                AddSearchTemplate(false, null, group);
                UpdateFilterExpression();
            }
            else
            {
                // Multiple groups exist but AllowMultipleGroups is false - shouldn't happen normally
                group.SearchTemplates.Clear();
                SearchGroups.Remove(group);
                UpdateFilterExpression();
                if (SearchGroups.Count == 0)
                {
                    AddSearchGroup(true, false);
                }
                else
                {
                    // Update operator visibility on the first remaining group
                    UpdateOperatorVisibility();
                }
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
            var group = SearchGroups.FirstOrDefault(g => g.SearchTemplates.Contains(template));
            if (group == null) return;
            
            // Validate the removal operation
            var validationResult = _validator.ValidateRemoveSearchTemplate(template, group);
            if (!validationResult.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot remove search template: {validationResult.ErrorMessage}");
                return;
            }

            if (group.SearchTemplates.Count > 1)
            {
                group.SearchTemplates.Remove(template);
                UpdateFilterExpression();
            }
            else
            {
                // If this is the last template, add a new empty one after removing
                group.SearchTemplates.Remove(template);
                AddSearchTemplate(true, null, group);
                UpdateFilterExpression();
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
        /// Gets structured filter components for the current filter state
        /// </summary>
        /// <returns>Structured filter components for display</returns>
        public FilterChipComponents GetFilterComponents()
        {
            try
            {
                if (!HasCustomExpression || SearchGroups.Count == 0)
                {
                    return new FilterChipComponents
                    {
                        SearchTypeText = "No filter",
                        HasNoInputValues = true
                    };
                }

                // For now, use the first template for simplicity
                // In complex cases with multiple groups/templates, we'll combine them
                var firstTemplate = SearchGroups.FirstOrDefault()?.SearchTemplates?.FirstOrDefault(t => t.HasCustomFilter);
                if (firstTemplate != null)
                {
                    return GetTemplateComponents(firstTemplate);
                }

                // Fallback to parsing the display text
                var displayText = GetFilterDisplayText();
                return FilterDisplayTextParser.ParseDisplayText(displayText, SearchType.Contains);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetFilterComponents: {ex.Message}");
                return new FilterChipComponents
                {
                    SearchTypeText = "Advanced filter",
                    HasNoInputValues = false
                };
            }
        }

        /// <summary>
        /// Gets all filter components for display, including multiple search conditions with operators
        /// </summary>
        /// <returns>Collection of all filter components</returns>
        public List<FilterChipComponents> GetAllFilterComponents()
        {
            var components = new List<FilterChipComponents>();

            try
            {
                // Check for grouped filtering scenario first
                if (IsGroupedFiltering)
                {
                    return GetGroupedFilterComponents();
                }

                if (!HasCustomExpression || SearchGroups.Count == 0)
                {
                    components.Add(new FilterChipComponents
                    {
                        SearchTypeText = "No filter",
                        HasNoInputValues = true
                    });
                    return components;
                }

                bool isFirstComponent = true;

                foreach (var group in SearchGroups)
                {
                    bool isFirstTemplateInGroup = true;
                    foreach (var template in group.SearchTemplates.Where(t => t.HasCustomFilter))
                    {
                        var component = GetTemplateComponents(template);
                        
                        // Set operators for templates within the group
                        if (!isFirstTemplateInGroup)
                        {
                            component.Operator = template.OperatorName?.ToUpper() ?? "And";
                        }
                        else if (!isFirstComponent)
                        {
                            // First template in a group gets the group's operator
                            component.Operator = group.OperatorName?.ToUpper() ?? "And";
                        }

                        components.Add(component);
                        isFirstTemplateInGroup = false;
                        isFirstComponent = false;
                    }
                }

                return components;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllFilterComponents: {ex.Message}");
                components.Add(new FilterChipComponents
                {
                    SearchTypeText = "Advanced filter",
                    HasNoInputValues = false
                });
                return components;
            }
        }

        /// <summary>
        /// Gets filter components for grouped filtering scenarios
        /// </summary>
        /// <returns>Collection of filter components representing the grouped filter</returns>
        private List<FilterChipComponents> GetGroupedFilterComponents()
        {
            try
            {
                if (GroupedFilterCombinations == null || !GroupedFilterCombinations.Any())
                {
                    return new List<FilterChipComponents>
                    {
                        new FilterChipComponents
                        {
                            SearchTypeText = "No filter",
                            HasNoInputValues = true
                        }
                    };
                }

                // Use the GroupedFilterChipFactory to create optimal filter chips
                return GroupedFilterChipFactory.CreateFilterChips(
                    GroupedFilterCombinations,
                    GroupByColumnPath,
                    CurrentColumnPath,
                    AllGroupData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetGroupedFilterComponents: {ex.Message}");
                return new List<FilterChipComponents>
                {
                    new FilterChipComponents
                    {
                        SearchTypeText = "Advanced filter",
                        HasNoInputValues = false
                    }
                };
            }
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
                                var operatorName = templateTexts[i].template.OperatorName?.ToUpper() ?? "And";
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
                    var operatorName = groupTexts[i].group.OperatorName?.ToUpper() ?? "AND";
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
                        // For NotEquals (exclusion logic), the actual excluded value is in SelectedValues
                        if (template.SelectedValues?.Any() == true)
                        {
                            var firstValue = template.SelectedValues.First();
                            string excludedValue;
                            if (firstValue is FilterListValue filterValue)
                            {
                                excludedValue = filterValue.Value?.ToString() ?? "null";
                            }
                            else
                            {
                                excludedValue = firstValue?.ToString() ?? "null";
                            }
                            return $"≠ '{excludedValue}'";
                        }
                        else
                        {
                            return $"≠ '{value}'"; // Fallback to SelectedValue
                        }
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

        /// <summary>
        /// Gets structured components for a single search template
        /// </summary>
        private FilterChipComponents GetTemplateComponents(SearchTemplate template)
        {
            try
            {
                var value = template.SelectedValue?.ToString();
                var secondaryValue = template.SelectedSecondaryValue?.ToString();
                var components = new FilterChipComponents
                {
                    IsDateInterval = IsDateIntervalType(template.SearchType),
                    HasNoInputValues = IsNoInputValueType(template.SearchType)
                };

                switch (template.SearchType)
                {
                    case SearchType.Contains:
                        components.SearchTypeText = "Contains";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.DoesNotContain:
                        components.SearchTypeText = "Does not contain";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.Equals:
                        components.SearchTypeText = "=";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.NotEquals:
                        components.SearchTypeText = "≠";
                        // For NotEquals (exclusion logic), the actual excluded value is in SelectedValues
                        if (template.SelectedValues?.Any() == true)
                        {
                            var firstValue = template.SelectedValues.First();
                            if (firstValue is FilterListValue filterValue)
                            {
                                components.PrimaryValue = filterValue.Value?.ToString();
                            }
                            else
                            {
                                components.PrimaryValue = firstValue?.ToString();
                            }
                        }
                        else
                        {
                            components.PrimaryValue = value; // Fallback to SelectedValue
                        }
                        break;
                    case SearchType.StartsWith:
                        components.SearchTypeText = "Starts with";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.EndsWith:
                        components.SearchTypeText = "Ends with";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.Between:
                        components.SearchTypeText = "Between";
                        components.PrimaryValue = value;
                        components.SecondaryValue = secondaryValue;
                        components.ValueOperatorText = "and";
                        break;
                    case SearchType.NotBetween:
                        components.SearchTypeText = "Not between";
                        components.PrimaryValue = value;
                        components.SecondaryValue = secondaryValue;
                        components.ValueOperatorText = "and";
                        break;
                    case SearchType.BetweenDates:
                        components.SearchTypeText = "Between dates";
                        components.PrimaryValue = FormatDateValue(value);
                        components.SecondaryValue = FormatDateValue(secondaryValue);
                        components.ValueOperatorText = "and";
                        break;
                    case SearchType.GreaterThan:
                        components.SearchTypeText = ">";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.GreaterThanOrEqualTo:
                        components.SearchTypeText = ">=";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.LessThan:
                        components.SearchTypeText = "<";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.LessThanOrEqualTo:
                        components.SearchTypeText = "<=";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.IsLike:
                        components.SearchTypeText = "Is like";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.IsNotLike:
                        components.SearchTypeText = "Is not like";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.TopN:
                        components.SearchTypeText = "Top";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.BottomN:
                        components.SearchTypeText = "Bottom";
                        components.PrimaryValue = value;
                        break;
                    case SearchType.IsAnyOf:
                        components.SearchTypeText = "Is any of";
                        components.PrimaryValue = FormatMultiValueFilter("", template.SelectedValues);
                        PopulateValueItems(components, template.SelectedValues);
                        break;
                    case SearchType.IsNoneOf:
                        components.SearchTypeText = "Is none of";
                        components.PrimaryValue = FormatMultiValueFilter("", template.SelectedValues);
                        PopulateValueItems(components, template.SelectedValues);
                        break;
                    case SearchType.IsOnAnyOfDates:
                        components.SearchTypeText = "Is on any of";
                        components.PrimaryValue = FormatDateListFilter("", template.SelectedDates);
                        PopulateDateValueItems(components, template.SelectedDates);
                        break;
                    case SearchType.DateInterval:
                        components.SearchTypeText = "Date interval";
                        components.PrimaryValue = FormatDateIntervalFilter(template.DateIntervals);
                        PopulateDateIntervalItems(components, template.DateIntervals);
                        break;
                    // No-input types
                    case SearchType.IsNull:
                        components.SearchTypeText = "Is null";
                        break;
                    case SearchType.IsNotNull:
                        components.SearchTypeText = "Is not null";
                        break;
                    case SearchType.IsEmpty:
                        components.SearchTypeText = "Is empty";
                        break;
                    case SearchType.IsNotEmpty:
                        components.SearchTypeText = "Is not empty";
                        break;
                    case SearchType.AboveAverage:
                        components.SearchTypeText = "Above average";
                        break;
                    case SearchType.BelowAverage:
                        components.SearchTypeText = "Below average";
                        break;
                    case SearchType.Unique:
                        components.SearchTypeText = "Unique values";
                        break;
                    case SearchType.Duplicate:
                        components.SearchTypeText = "Duplicate values";
                        break;
                    case SearchType.Today:
                        components.SearchTypeText = "Is today";
                        break;
                    case SearchType.Yesterday:
                        components.SearchTypeText = "Is yesterday";
                        break;
                    default:
                        components.SearchTypeText = template.SearchType.ToString();
                        break;
                }

                // Try to parse PrimaryValue as multiple values if no explicit ValueItems were set
                components.ParsePrimaryValueAsMultipleValues();

                return components;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetTemplateComponents: {ex.Message}");
                return new FilterChipComponents
                {
                    SearchTypeText = "Filter",
                    HasNoInputValues = false
                };
            }
        }

        private static bool IsDateIntervalType(SearchType searchType)
        {
            return searchType == SearchType.DateInterval ||
                   searchType == SearchType.BetweenDates ||
                   searchType == SearchType.IsOnAnyOfDates;
        }

        private static bool IsNoInputValueType(SearchType searchType)
        {
            return searchType == SearchType.IsNull ||
                   searchType == SearchType.IsNotNull ||
                   searchType == SearchType.IsEmpty ||
                   searchType == SearchType.IsNotEmpty ||
                   searchType == SearchType.AboveAverage ||
                   searchType == SearchType.BelowAverage ||
                   searchType == SearchType.Unique ||
                   searchType == SearchType.Duplicate ||
                   searchType == SearchType.Today ||
                   searchType == SearchType.Yesterday;
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

            return $"{string.Join(", ", selectedIntervals)}";
        }

        /// <summary>
        /// Populates ValueItems collection from SelectedValues
        /// </summary>
        private void PopulateValueItems(FilterChipComponents components, System.Collections.IEnumerable selectedValues)
        {
            if (selectedValues == null) return;

            components.ValueItems.Clear();
            foreach (var item in selectedValues)
            {
                if (item is FilterListValue filterValue)
                {
                    if (!string.IsNullOrEmpty(filterValue.Value?.ToString()))
                    {
                        components.ValueItems.Add(filterValue.Value.ToString());
                    }
                }
                else if (item != null)
                {
                    components.ValueItems.Add(item.ToString());
                }
            }
        }

        /// <summary>
        /// Populates ValueItems collection from SelectedDates
        /// </summary>
        private void PopulateDateValueItems(FilterChipComponents components, System.Collections.IEnumerable selectedDates)
        {
            if (selectedDates == null) return;

            components.ValueItems.Clear();
            foreach (var date in selectedDates)
            {
                if (date is DateTime dt)
                {
                    components.ValueItems.Add(dt.ToString("yyyy-MM-dd"));
                }
            }
        }

        /// <summary>
        /// Populates ValueItems collection from DateIntervals
        /// </summary>
        private void PopulateDateIntervalItems(FilterChipComponents components, System.Collections.IEnumerable dateIntervals)
        {
            if (dateIntervals == null) return;

            components.ValueItems.Clear();
            foreach (var item in dateIntervals)
            {
                if (item is DateIntervalItem intervalItem && intervalItem.IsSelected)
                {
                    components.ValueItems.Add(intervalItem.DisplayName);
                }
            }
        }

        /// <summary>
        /// Updates the visibility of logical operators for all groups and templates
        /// </summary>
        /// <param name="firstOperator">First operator (optional parameter for backward compatibility)</param>
        internal void UpdateOperatorVisibility()
        {
            // Update visibility for all search groups (group-level operators)
            for (int i = 0; i < SearchGroups.Count; i++)
            {
                // First group should have operator hidden, subsequent groups should show it
                SearchGroups[i].IsOperatorVisible = i > 0;
                
                // Update visibility for templates within each group (template-level operators)
                for (int j = 0; j < SearchGroups[i].SearchTemplates.Count; j++)
                {
                    // First template in each group should have operator hidden, subsequent templates should show it
                    SearchGroups[i].SearchTemplates[j].IsOperatorVisible = j > 0;
                }
            }
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

        /// <summary>
        /// Determines the target group for adding a template
        /// </summary>
        private SearchTemplateGroup DetermineTargetGroup(SearchTemplateGroup group, SearchTemplate referenceTemplate)
        {
            // Handle null parameters with proper fallback logic
            if (group != null)
            {
                // Use the explicitly provided group
                return group;
            }
            else if (referenceTemplate != null)
            {
                // Find the group containing the reference template
                var targetGroup = SearchGroups.FirstOrDefault(g => g.SearchTemplates.Contains(referenceTemplate));
                if (targetGroup == null)
                {
                    // Fallback: if reference template not found, use the first group or create one
                    targetGroup = SearchGroups.FirstOrDefault() ?? new SearchTemplateGroup();
                    if (!SearchGroups.Contains(targetGroup))
                    {
                        SearchGroups.Add(targetGroup);
                    }
                }
                return targetGroup;
            }
            else
            {
                // Both group and referenceTemplate are null - use first available group or create one
                var targetGroup = SearchGroups.FirstOrDefault();
                if (targetGroup == null)
                {
                    targetGroup = new SearchTemplateGroup();
                    SearchGroups.Add(targetGroup);
                }
                return targetGroup;
            }
        }

        /// <summary>
        /// Applies the default search type to a template if it's compatible with the column data type
        /// </summary>
        /// <param name="template">The template to apply the default search type to</param>
        /// <param name="defaultSearchType">The default search type to apply</param>
        private void ApplyDefaultSearchType(SearchTemplate template, SearchType? defaultSearchType)
        {
            // Use the parameter first, then fall back to the property
            var searchTypeToApply = defaultSearchType ?? DefaultSearchType;

            if (searchTypeToApply.HasValue)
            {
                // Check if the search type is compatible with the column data type
                if (SearchTypeRegistry.IsValidForDataType(searchTypeToApply.Value, ColumnDataType))
                {
                    template.SearchType = searchTypeToApply.Value;
                }
            }
        }

        /// <summary>
        /// Connects all SearchTemplates in this controller to use shared cache sources
        /// </summary>
        public void ConnectToCache(string columnKey, Performance.ColumnValueCache cache)
        {
            if (cache == null || string.IsNullOrEmpty(columnKey))
                return;

            // Reset registration flag if connecting to a different cache or column
            if (_connectedColumnKey != columnKey || _connectedCache != cache)
            {
                _providersRegistered = false;
            }

            // Store connection parameters for new templates
            _connectedColumnKey = columnKey;
            _connectedCache = cache;

            // Connect all existing templates to the shared source
            foreach (var group in SearchGroups)
            {
                foreach (var template in group.SearchTemplates.OfType<SearchTemplate>())
                {
                    template.ConnectToSharedSource(columnKey, cache);
                }
            }
        }

        /// <summary>
        /// Registers providers for all SearchTemplates with proper WPF dispatcher handling
        /// This method should be called from the WPF layer after ConnectToCache
        /// </summary>
        public void RegisterProvidersWithCache(string columnKey, Performance.ColumnValueCache cache, Func<System.Collections.ObjectModel.ObservableCollection<object>, Performance.ISharedItemsSourceProvider> providerFactory)
        {
            if (cache == null || string.IsNullOrEmpty(columnKey) || providerFactory == null)
                return;

            // Avoid duplicate registrations
            if (_providersRegistered && _connectedColumnKey == columnKey && _connectedCache == cache)
                return;

            foreach (var group in SearchGroups)
            {
                foreach (var template in group.SearchTemplates.OfType<SearchTemplate>())
                {
                    var provider = providerFactory(template.AvailableValues);
                    cache.RegisterSharedItemsSourceProvider(columnKey, provider);
                }
            }

            _providersRegistered = true;
        }

        /// <summary>
        /// Registers a provider for a single SearchTemplate - used when new templates are added after initial connection
        /// This method should be called from the WPF layer
        /// </summary>
        public void RegisterSingleTemplateProvider(SearchTemplate template, string columnKey, Performance.ColumnValueCache cache, Func<System.Collections.ObjectModel.ObservableCollection<object>, Performance.ISharedItemsSourceProvider> providerFactory)
        {
            if (template == null || cache == null || string.IsNullOrEmpty(columnKey) || providerFactory == null)
                return;

            var provider = providerFactory(template.AvailableValues);
            cache.RegisterSharedItemsSourceProvider(columnKey, provider);
        }

        #endregion
    }
}