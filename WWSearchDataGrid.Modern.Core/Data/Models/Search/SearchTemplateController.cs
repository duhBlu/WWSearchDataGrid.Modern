using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.Core.Caching;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Core controller for managing search templates and groups
    /// </summary>
    public class SearchTemplateController : ObservableObject, IDisposable
    {
        #region Events
        
        /// <summary>
        /// Occurs when filters should be applied due to valid changes in templates or structure
        /// </summary>
        public event EventHandler AutoApplyFilter;
        
        #endregion
        
        #region Fields

        private bool hasCustomExpression;
        private Type targetColumnType;
        private ColumnDataType columnDataType = ColumnDataType.String;
        
        // Service dependencies
        private readonly FilterExpressionBuilder _filterExpressionBuilder;

        // Cache-based column values (replaces ObservableCollection approach)
        private string _cacheKey;
        private Func<IEnumerable<object>> _columnValuesProvider;
        private ReadOnlyColumnValues _cachedColumnValues;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        public object ColumnName { get; set; }

        /// <summary>
        /// Gets the read-only collection of column values for UI binding
        /// Uses shared cache manager to eliminate data duplication
        /// Values are loaded lazily when first accessed
        /// </summary>
        public IReadOnlyList<object> ColumnValues 
        { 
            get 
            {
                EnsureColumnValuesLoaded();
                return _cachedColumnValues ?? (IReadOnlyList<object>)new List<object>();
            } 
        }

        /// <summary>
        /// Gets whether the column data contains any null values
        /// This is determined when column values are loaded
        /// </summary>
        public bool ContainsNullValues
        {
            get
            {
                EnsureColumnValuesLoaded();
                return _cachedColumnValues?.ContainsNullValues ?? false;
            }
        }

        /// <summary>
        /// Gets or sets the column data type
        /// </summary>
        public ColumnDataType ColumnDataType
        {
            get => columnDataType;
            set
            {
                SetProperty(value, ref columnDataType);
                UpdateTemplateDataTypes(value);
            }
        }
        
        /// <summary>
        /// Updates template data types while preserving valid SearchTypes
        /// </summary>
        private void UpdateTemplateDataTypes(ColumnDataType newDataType)
        {
            foreach (var group in SearchGroups)
            {
                foreach (var template in group.SearchTemplates.OfType<SearchTemplate>())
                {
                    // Update the data type (this will trigger UpdateValidSearchTypes)
                    template.ColumnDataType = newDataType;
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
        /// Gets or sets the dictionary of property values
        /// </summary>
        public Dictionary<string, List<object>> PropertyValues { get; set; } = new Dictionary<string, List<object>>();

        /// <summary>
        /// Gets the collection of search groups
        /// </summary>
        public ObservableCollection<SearchTemplateGroup> SearchGroups { get; } = new ObservableCollection<SearchTemplateGroup>();

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


        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SearchTemplateController class
        /// </summary>
        public SearchTemplateController()
        {
            // Initialize services
            _filterExpressionBuilder = new FilterExpressionBuilder();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all search groups and adds a default empty group back
        /// This ensures the UI always has something to display
        /// </summary>
        public void ClearAndReset()
        {
            UnsubscribeFromAllTemplates();

            SearchGroups.Clear();
            AddSearchGroup(true, false);
            HasCustomExpression = false;
        }
           
        
        /// <summary>
        /// Clears all data references that could prevent garbage collection
        /// </summary>
        public void ClearDataReferences()
        {
            // Clear PropertyValues dictionary while preserving the dictionary structure
            if (PropertyValues != null)
            {
                foreach (var key in PropertyValues.Keys.ToList())
                {
                    PropertyValues[key]?.Clear();
                }
                PropertyValues.Clear();
            }
            
            // Clear ColumnValuesByPath dictionary while preserving the dictionary structure
            if (ColumnValuesByPath != null)
            {
                foreach (var key in ColumnValuesByPath.Keys.ToList())
                {
                    ColumnValuesByPath[key]?.Clear();
                }
                ColumnValuesByPath.Clear();
            }
            
            _cachedColumnValues = null;
            _cacheKey = null;
            
            _columnValuesProvider = null;
            
            // Trigger cache cleanup to remove dead references
            ColumnValueCacheManager.Instance.Cleanup(clearAll: false);
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
        public void AddSearchTemplate(bool markAsChanged = true, SearchTemplate referenceTemplate = null, SearchTemplateGroup group = null)
        {
            SearchTemplateGroup targetGroup = DetermineTargetGroup(group, referenceTemplate);

            // Create new SearchTemplate with column data type
            var newTemplate = new SearchTemplate(ColumnDataType)
            {
                HasChanges = markAsChanged,
                SearchTemplateController = this // Ensure template has reference to controller
            };

            // Subscribe to property changes for auto-apply monitoring
            newTemplate.PropertyChanged += OnSearchTemplatePropertyChanged;
            newTemplate.SelectedValues.CollectionChanged += OnSearchTemplateValues_CollectionChanged;
            newTemplate.SelectedDates.CollectionChanged += OnSearchTemplateValues_CollectionChanged;
            foreach (var di in newTemplate.DateIntervals)
            {
                di.PropertyChanged += OnSearchTemplatePropertyChanged;
            }
            targetGroup.PropertyChanged += OnSearchGroup_PropertyChanged;


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
            InvokeAutoApplyFilter();
        }

        /// <summary>
        /// Sets a provider function for lazy loading column values
        /// This avoids loading values until they're actually needed
        /// </summary>
        /// <param name="valuesProvider">Function that provides column values when called</param>
        public void SetColumnValuesProvider(Func<IEnumerable<object>> valuesProvider)
        {
            _columnValuesProvider = valuesProvider;
            _cachedColumnValues = null;
            
            // Generate a cache key based on the provider hashcode and column name
            _cacheKey = $"{ColumnName?.GetHashCode() ?? 0}_{valuesProvider?.GetHashCode() ?? 0}";
        }
        
        /// <summary>
        /// Forces a reload of column values from the provider
        /// Used when data changes and values need to be refreshed
        /// </summary>
        public void RefreshColumnValues()
        {
            if (_columnValuesProvider != null)
            {
                _cachedColumnValues = null;
                // Update cache key to force new cache entry
                _cacheKey = $"{ColumnName?.GetHashCode() ?? 0}_{_columnValuesProvider?.GetHashCode() ?? 0}_{DateTime.UtcNow.Ticks}";
                // Values will be reloaded on next access
            }
            OnPropertyChanged(nameof(ColumnValues));
        }
        
        /// <summary>
        /// Forces immediate loading of column values (for filter editors)
        /// This ensures values are available and data type is correctly detected
        /// </summary>
        public void EnsureColumnValuesLoadedForFiltering()
        {
            // Force values to load immediately if not already loaded
            if (_cachedColumnValues == null && _columnValuesProvider != null)
            {
                EnsureColumnValuesLoaded(); // This will load values and determine data type
            }
        }
        
        /// <summary>
        /// Sets the column values for direct UI binding (legacy approach for backward compatibility)
        /// Consider using SetColumnValuesProvider for better performance
        /// </summary>
        /// <param name="values">Column values to set</param>
        public void SetColumnValues(IEnumerable<object> values)
        {
            // Create a cache key for direct values
            _cacheKey = $"direct_{ColumnName?.GetHashCode() ?? 0}_{DateTime.UtcNow.Ticks}";
            
            // Create cache entry directly
            _cachedColumnValues = ColumnValueCacheManager.Instance.GetOrCreateColumnValues(
                _cacheKey, 
                () => values ?? Enumerable.Empty<object>());
            
            _columnValuesProvider = null;
            
            // Determine column data type from the cached values
            DetermineColumnDataTypeFromValues();
        }
        
        /// <summary>
        /// Ensures column values are loaded from cache
        /// </summary>
        private void EnsureColumnValuesLoaded()
        {
            if (_cachedColumnValues == null && _columnValuesProvider != null && !string.IsNullOrEmpty(_cacheKey))
            {
                try
                {
                    _cachedColumnValues = ColumnValueCacheManager.Instance.GetOrCreateColumnValues(
                        _cacheKey, 
                        _columnValuesProvider);
                    
                    // Determine column data type from the cached values
                    DetermineColumnDataTypeFromValues();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading column values: {ex.Message}");
                    // Create empty cache entry to prevent repeated failures
                    _cachedColumnValues = ColumnValueCacheManager.Instance.GetOrCreateColumnValues(
                        _cacheKey, 
                        () => Enumerable.Empty<object>());
                }
            }
        }
        
        /// <summary>
        /// Determines column data type from cached values (called when values are first accessed)
        /// </summary>
        private void DetermineColumnDataTypeFromValues()
        {
            try
            {
                // Only determine data type if values are loaded
                if (_cachedColumnValues != null && _cachedColumnValues.Count > 0)
                {
                    var detectedType = ReflectionHelper.DetermineColumnDataType(new HashSet<object>(_cachedColumnValues.UniqueValues));
                    
                    // Always update with the detected type from the full dataset
                    // This overrides any previous sampling-based detection
                    ColumnDataType = detectedType;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining column data type: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Normalizes a value (null/empty/whitespace → NullDisplayValue for UI display)
        /// </summary>
        /// <param name="value">Value to normalize</param>
        /// <returns>Normalized value</returns>
        private object NormalizeValue(object value)
        {
            if (value == null) return NullDisplayValue.Instance;
            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                return NullDisplayValue.Instance;
            return value;
        }
        
        
        /// <summary>
        /// Adds or updates a single value in the column values (for incremental updates)
        /// Note: With cache-based approach, incremental updates require refreshing the entire cache
        /// </summary>
        /// <param name="value">Value to add or update</param>
        public void AddOrUpdateColumnValue(object value)
        {
            OnPropertyChanged(nameof(ColumnValues));
            if (_cachedColumnValues == null)
            {
                // If values aren't loaded yet, just mark them as needing refresh
                return;
            }
            
            var normalizedValue = NormalizeValue(value);
            if (!_cachedColumnValues.Contains(normalizedValue))
            {
                // For cache-based approach, we need to refresh the entire cache
                // This is less efficient for single updates but necessary for data consistency
                RefreshColumnValues();
            }
        }
        
        /// <summary>
        /// Removes a value from the column values (for incremental updates)
        /// Note: With cache-based approach, incremental updates require refreshing the entire cache
        /// </summary>
        /// <param name="value">Value to remove</param>
        public void RemoveColumnValue(object value)
        {
            OnPropertyChanged(nameof(ColumnValues));
            if (_cachedColumnValues == null)
            {
                // If values aren't loaded yet, just mark them as needing refresh
                return;
            }
            
            var normalizedValue = NormalizeValue(value);
            if (_cachedColumnValues.Contains(normalizedValue))
            {
                // For cache-based approach, we need to refresh the entire cache
                // This is less efficient for single updates but necessary for data consistency
                RefreshColumnValues();
            }
        }

        /// <summary>
        /// Sets up lazy loading for column data (new efficient approach)
        /// </summary>
        /// <param name="header">Column header</param>
        /// <param name="valuesProvider">Function that provides column values when needed</param>
        /// <param name="bindingPath">The binding path for the column</param>
        public void SetupColumnDataLazy(
            object header,
            Func<IEnumerable<object>> valuesProvider,
            string bindingPath = null)
        {
            ColumnName = header;
            SetColumnValuesProvider(valuesProvider);
            
            // We'll determine column data type when values are first loaded
            // For now, start with string as default
            //ColumnDataType = ColumnDataType.String;

            AddSearchGroup(SearchGroups.Count == 0, false);

            // Ensure operator visibility is properly set
            UpdateOperatorVisibility();
        }
        
        /// <summary>
        /// Loads column data into the search templates (legacy approach for backward compatibility)
        /// </summary>
        /// <param name="header">Column header</param>
        /// <param name="values">Column values</param>
        /// <param name="displayValueMappings">Mappings for display values</param>
        /// <param name="bindingPath">The binding path for the column</param>
        public void LoadColumnData(
            object header,
            HashSet<object> values,
            string bindingPath = null)
        {
            SetColumnValues(values); // Use legacy approach
            ColumnName = header;

            // Auto-detect column data type when values are accessed
            DetermineColumnDataTypeFromValues();

            // Store column values by binding path for global filtering
            if (!string.IsNullOrEmpty(bindingPath))
            {
                ColumnValuesByPath[bindingPath] = new HashSet<object>(ColumnValues);
            }

            AddSearchGroup(SearchGroups.Count == 0, false);

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
                    targetColumnType = _filterExpressionBuilder.DetermineTargetColumnType(ColumnDataType, new HashSet<object>(_cachedColumnValues?.UniqueValues ?? new HashSet<object>()));
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
        /// Evaluates a value against the search templates with collection context support
        /// </summary>
        /// <param name="value">The value to evaluate</param>
        /// <param name="collectionContext">Collection context for statistical operations</param>
        /// <returns>True if the value matches the search criteria</returns>
        internal bool EvaluateWithCollectionContext(object value, CollectionContext collectionContext)
        {
            if (!HasCustomExpression || SearchGroups == null || SearchGroups.Count == 0)
                return true;

            try
            {
                // Evaluate each search group
                bool overallResult = false;

                for (int groupIndex = 0; groupIndex < SearchGroups.Count; groupIndex++)
                {
                    var group = SearchGroups[groupIndex];
                    bool groupResult = EvaluateSearchGroupWithContext(value, group, collectionContext);

                    if (groupIndex == 0)
                    {
                        overallResult = groupResult;
                    }
                    else
                    {
                        // Apply the logical operator from this group
                        if (group.OperatorName == "Or")
                        {
                            overallResult = overallResult || groupResult;
                        }
                        else // AND is default
                        {
                            overallResult = overallResult && groupResult;
                        }
                    }
                }

                return overallResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EvaluateWithCollectionContext: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates a search group with collection context support
        /// </summary>
        private bool EvaluateSearchGroupWithContext(object value, SearchTemplateGroup group, CollectionContext collectionContext)
        {
            if (group.SearchTemplates == null || group.SearchTemplates.Count == 0)
                return true;

            bool groupResult = false;

            for (int templateIndex = 0; templateIndex < group.SearchTemplates.Count; templateIndex++)
            {
                var template = group.SearchTemplates[templateIndex];
                bool templateResult = EvaluateSearchTemplateWithContext(value, template, collectionContext);

                if (templateIndex == 0)
                {
                    groupResult = templateResult;
                }
                else
                {
                    // Apply the logical operator from this template
                    if (template.OperatorName == "Or")
                    {
                        groupResult = groupResult || templateResult;
                    }
                    else // AND is default
                    {
                        groupResult = groupResult && templateResult;
                    }
                }
            }

            return groupResult;
        }

        /// <summary>
        /// Evaluates a single search template with collection context support
        /// </summary>
        private bool EvaluateSearchTemplateWithContext(object value, SearchTemplate template, CollectionContext collectionContext)
        {
            if (template == null || template.SearchCondition == null)
                return true;

            try
            {
                // Use the SearchEngine with collection context
                return SearchEngine.EvaluateCondition(value, template.SearchCondition, collectionContext);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error evaluating template with context: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a search template
        /// </summary>
        /// <param name="template">Template to remove</param>
        public void RemoveSearchTemplate(SearchTemplate template)
        {
            var group = SearchGroups.FirstOrDefault(g => g.SearchTemplates.Contains(template));
            if (group == null) return;
            
            // Unsubscribe from the template being removed
            template.PropertyChanged -= OnSearchTemplatePropertyChanged;
            
            if (group.SearchTemplates.Count > 1)
            {
                group.SearchTemplates.Remove(template);
            }
            else
            {
                // If this is the last template, add a new empty one after removing
                group.SearchTemplates.Remove(template);
                AddSearchTemplate(true, null, group);
            }
            UpdateFilterExpression();
            UpdateOperatorVisibility();
            InvokeAutoApplyFilter();
        }

        /// <summary>
        /// Gets structured filter components for the current filter state
        /// </summary>
        /// <returns>Structured filter components for display</returns>
        public FilterChipComponents GetTokenizedFilter()
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
                            var excludedValue = firstValue?.ToString() ?? "null";
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
                        return FormatMultiValueFilter("In", template.SelectedValues);
                    case SearchType.IsNoneOf:
                        return FormatMultiValueFilter("Not in", template.SelectedValues);
                    case SearchType.IsOnAnyOfDates:
                        return FormatDateListFilter("In", template.SelectedDates);
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
                values.Add(item?.ToString() ?? "(null)");
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
                            components.PrimaryValue = firstValue?.ToString();
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
                        components.SearchTypeText = "In";
                        components.PrimaryValue = FormatMultiValueFilter("", template.SelectedValues);
                        PopulateValueItems(components, template.SelectedValues);
                        break;
                    case SearchType.IsNoneOf:
                        components.SearchTypeText = "Not in";
                        components.PrimaryValue = FormatMultiValueFilter("", template.SelectedValues);
                        PopulateValueItems(components, template.SelectedValues);
                        break;
                    case SearchType.IsOnAnyOfDates:
                        components.SearchTypeText = "In";
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
                if (item != null && !string.IsNullOrEmpty(item.ToString()))
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
        /// Gets whether all search templates in this controller represent valid, complete filters
        /// </summary>
        public bool HasValidFilters
        {
            get
            {
                if (SearchGroups == null || !SearchGroups.Any())
                    return false;

                return SearchGroups.All(group => 
                    group.SearchTemplates?.All(template => template.IsValidFilter) == true);
            }
        }

        /// <summary>
        /// Raises the AutoApplyFilter event when there are valid filters that should be applied
        /// </summary>
        protected virtual void InvokeAutoApplyFilter()
        {
            // Only fire if we have at least one valid filter to apply
            if (HasValidFilters)
            {
                AutoApplyFilter?.Invoke(this, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// Handle property changes from SearchTemplates to determine when to auto-apply
        /// </summary>
        private void OnSearchTemplatePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only trigger auto-apply for filter-relevant properties
            var filterRelevantProperties = new[] 
            {
                nameof(SearchTemplate.SearchType),
                nameof(SearchTemplate.SelectedValue),
                nameof(SearchTemplate.SelectedSecondaryValue),
                nameof(SearchTemplate.OperatorName),
                nameof(DateIntervalItem.IsSelected)
            };

            if (filterRelevantProperties.Contains(e.PropertyName))
            {
                InvokeAutoApplyFilter();
            }
        }

        private void OnSearchTemplateValues_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvokeAutoApplyFilter();
        }

        private void OnSearchGroup_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var filterRelevantProperties = new[]
            {
                nameof(SearchTemplate.SearchType),
            };
            if (e.PropertyName.Equals(nameof(SearchTemplateGroup.OperatorName)))
            {
                InvokeAutoApplyFilter();
            }
        }

        /// <summary>
        /// Unsubscribe from all existing search template property changes
        /// </summary>
        private void UnsubscribeFromAllTemplates()
        {
            if (SearchGroups == null) return;
            
            foreach (var group in SearchGroups)
            {
                if (group.SearchTemplates != null)
                {
                    foreach (var template in group.SearchTemplates)
                    {
                        template.PropertyChanged -= OnSearchTemplatePropertyChanged;
                        template.SelectedValues.CollectionChanged -= OnSearchTemplateValues_CollectionChanged;
                        template.SelectedDates.CollectionChanged -= OnSearchTemplateValues_CollectionChanged;
                    }
                }
                group.PropertyChanged -= OnSearchGroup_PropertyChanged;
            }
        }

        #endregion
        
        #region IDisposable Implementation
        
        private bool _disposed = false;
        
        /// <summary>
        /// Disposes the SearchTemplateController and releases all cached data references
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Protected disposal method
        /// </summary>
        /// <param name="disposing">True if disposing from Dispose(), false if from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from all template events
                    UnsubscribeFromAllTemplates();
                    
                    // Clear all data references
                    ClearDataReferences();
                    
                    // Clear search groups
                    SearchGroups?.Clear();
                    
                    // Clear filter expression
                    FilterExpression = null;
                    HasCustomExpression = false;
                }
                
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Finalizer to ensure cleanup if Dispose is not called explicitly
        /// </summary>
        ~SearchTemplateController()
        {
            Dispose(false);
        }
        
        /// <summary>
        /// Throws ObjectDisposedException if the controller has been disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SearchTemplateController));
            }
        }
        
        #endregion
    }
}