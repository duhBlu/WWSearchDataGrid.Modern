using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.Core.Services;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Core controller for managing search templates and groups
    /// </summary>
    public class SearchTemplateController : ObservableObject
    {
        #region Events
        
        /// <summary>
        /// Occurs when filters should be applied due to valid changes in templates or structure
        /// </summary>
        public event EventHandler FilterShouldApply;
        
        #endregion
        
        #region Fields

        private bool hasCustomExpression;
        private Type targetColumnType;
        private ColumnDataType columnDataType = ColumnDataType.String;
        
        // Service dependencies
        private readonly IFilterExpressionBuilder _filterExpressionBuilder;

        private SearchType? defaultSearchType;
        
        // Lazy loading fields
        private bool _columnValuesLoaded = false;
        private Func<IEnumerable<object>> _columnValuesProvider;
        private readonly ObservableCollection<object> _columnValues = new ObservableCollection<object>();
        private bool _containsNullValues = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        public object ColumnName { get; set; }

        /// <summary>
        /// Gets the bindable collection of column values for direct UI binding
        /// This replaces the HashSet approach with an observable collection
        /// Values are loaded lazily when first accessed
        /// </summary>
        public ObservableCollection<object> ColumnValues 
        { 
            get 
            {
                EnsureColumnValuesLoaded();
                return _columnValues;
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
                return _containsNullValues;
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
                if (SetProperty(value, ref columnDataType))
                {
                    // Update all templates with the new data type, but preserve existing SearchTypes if still valid
                    UpdateTemplateDataTypes(value);
                }
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
                    // Store the current search type to preserve it if still valid
                    var currentSearchType = template.SearchType;
                    
                    // Update the data type (this will trigger UpdateValidSearchTypes)
                    template.ColumnDataType = newDataType;
                    
                    // If the original search type is still valid, restore it
                    if (DefaultSearchType is SearchType dst && template.ValidSearchTypes.Contains(dst))
                    {
                        template.SearchType = dst;
                    }
                    else
                    {
                        template.SearchType = currentSearchType;
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
            // Unsubscribe from all existing templates before clearing
            UnsubscribeFromAllTemplates();
            
            SearchGroups.Clear();
            AddSearchGroup(true, false); // Add default group without marking as changed
            HasCustomExpression = false;
            
            // No need to trigger filter apply for clear - the clear operation handles it
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
        /// <param name="defaultSearchType">Optional default search type to use if compatible</param>
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

            // Apply default search type if provided and compatible
            ApplyDefaultSearchType(newTemplate, defaultSearchType);

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
            OnFilterShouldApply();
        }

        /// <summary>
        /// Sets a provider function for lazy loading column values
        /// This avoids loading values until they're actually needed
        /// </summary>
        /// <param name="valuesProvider">Function that provides column values when called</param>
        public void SetColumnValuesProvider(Func<IEnumerable<object>> valuesProvider)
        {
            _columnValuesProvider = valuesProvider;
            _columnValuesLoaded = false;
            _containsNullValues = false; // Reset nullability flag
            _columnValues.Clear();
        }
        
        /// <summary>
        /// Forces a reload of column values from the provider
        /// Used when data changes and values need to be refreshed
        /// </summary>
        public void RefreshColumnValues()
        {
            if (_columnValuesProvider != null)
            {
                _columnValuesLoaded = false;
                _containsNullValues = false; // Reset nullability flag
                _columnValues.Clear();
                // Values will be reloaded on next access
            }
        }
        
        /// <summary>
        /// Forces immediate loading of column values (for filter editors)
        /// This ensures values are available and data type is correctly detected
        /// </summary>
        public void EnsureColumnValuesLoadedForFiltering()
        {
            // Force values to load immediately if not already loaded
            if (!_columnValuesLoaded && _columnValuesProvider != null)
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
            // Clear provider when setting values directly
            _columnValuesProvider = null;
            _columnValuesLoaded = true;
            LoadValuesIntoCollection(values);
        }
        
        /// <summary>
        /// Ensures column values are loaded into the collection
        /// </summary>
        private void EnsureColumnValuesLoaded()
        {
            if (!_columnValuesLoaded && _columnValuesProvider != null)
            {
                try
                {
                    var values = _columnValuesProvider();
                    LoadValuesIntoCollection(values); 
                    _columnValuesLoaded = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading column values: {ex.Message}");
                    _columnValuesLoaded = true; // Mark as loaded to prevent repeated failures
                }
            }
        }
        
        /// <summary>
        /// Loads values into the ObservableCollection with performance optimizations
        /// Also analyzes the data for null values during loading
        /// </summary>
        /// <param name="values">Values to load</param>
        private void LoadValuesIntoCollection(IEnumerable<object> values)
        {
            _columnValues.Clear();
            _containsNullValues = false; // Reset nullability flag
            
            if (values == null) return;

            // Performance optimization: use HashSet for O(1) duplicate detection instead of LINQ Distinct()
            var uniqueValues = new HashSet<object>();
            var normalizedValues = new List<object>();

            // Single pass to normalize, deduplicate, and analyze for null values
            foreach (var value in values)
            {
                // Check for null values before normalization
                if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
                {
                    _containsNullValues = true;
                }
                
                var normalizedValue = NormalizeValue(value);
                if (uniqueValues.Add(normalizedValue))
                {
                    normalizedValues.Add(normalizedValue);
                }
            }

            // Detect data type before sorting using the actual values
            if (normalizedValues.Any())
            {
                var detectedType = ReflectionHelper.DetermineColumnDataType(new HashSet<object>(normalizedValues));
                ColumnDataType = detectedType;
            }

            // Only sort if we have a reasonable number of values (avoid expensive sort on huge datasets)
            if (normalizedValues.Count <= 100000)
            {
                normalizedValues.Sort(CreateTypeAwareComparer(ColumnDataType));
            }

            foreach (var value in normalizedValues)
            {
                _columnValues.Add(value);
            }

            // Notify templates that nullability analysis has been updated
            UpdateTemplatesAfterNullabilityAnalysis();
        }

        /// <summary>
        /// Updates all templates after nullability analysis is complete
        /// This ensures templates get the correct set of valid search types based on nullability
        /// </summary>
        private void UpdateTemplatesAfterNullabilityAnalysis()
        {
            foreach (var group in SearchGroups)
            {
                foreach (var template in group.SearchTemplates.OfType<SearchTemplate>())
                {
                    // Trigger update of valid search types now that nullability is known
                    template.ColumnDataType = template.ColumnDataType; // This will call UpdateValidSearchTypes
                }
            }
        }

        /// <summary>
        /// Determines column data type from loaded values (called when values are first accessed)
        /// </summary>
        private void DetermineColumnDataTypeFromValues()
        {
            try
            {
                // Only determine data type if values are loaded
                if (_columnValuesLoaded && _columnValues.Any())
                {
                    var detectedType = ReflectionHelper.DetermineColumnDataType(new HashSet<object>(_columnValues));
                    System.Diagnostics.Debug.WriteLine($"SearchTemplateController: Detected column data type {detectedType} from {_columnValues.Count} values (was {ColumnDataType})");
                    
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
        /// Special class to represent null values with proper display text
        /// </summary>
        public class NullDisplayValue
        {
            public static readonly NullDisplayValue Instance = new NullDisplayValue();
            
            private NullDisplayValue() { }
            
            public override string ToString() => "(null)";
            
            public override bool Equals(object obj) => obj is NullDisplayValue;
            
            public override int GetHashCode() => 0; // All null display values are equal
        }
        
        /// <summary>
        /// Creates a type-aware comparer for sorting column values based on data type
        /// </summary>
        /// <param name="dataType">The column data type to create comparer for</param>
        /// <returns>Comparison function for sorting</returns>
        private Comparison<object> CreateTypeAwareComparer(ColumnDataType dataType)
        {
            switch (dataType)
            {
                case ColumnDataType.Number:
                    return CompareNumericValues;
                case ColumnDataType.DateTime:
                    return CompareDateTimeValues;
                case ColumnDataType.Boolean:
                case ColumnDataType.Enum:
                case ColumnDataType.String:
                default:
                    return CompareStringValues; // Default to string comparison
            }
        }
        
        /// <summary>
        /// Compares numeric values with proper numeric ordering
        /// </summary>
        private int CompareNumericValues(object x, object y)
        {
            try
            {
                // Handle null display values - nulls come first
                if (x is NullDisplayValue && y is NullDisplayValue) return 0;
                if (x is NullDisplayValue) return -1;
                if (y is NullDisplayValue) return 1;
                
                // Handle actual null values - nulls come first
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                
                // Convert to decimal using TypeTranslatorHelper
                var xDecimal = TypeTranslatorHelper.ConvertToDecimal(x);
                var yDecimal = TypeTranslatorHelper.ConvertToDecimal(y);
                
                // Handle conversion failures - treat as null and sort to beginning
                if (!xDecimal.HasValue && !yDecimal.HasValue) return 0;
                if (!xDecimal.HasValue) return -1;
                if (!yDecimal.HasValue) return 1;
                
                return xDecimal.Value.CompareTo(yDecimal.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CompareNumericValues: {ex.Message}");
                // Fallback to string comparison
                return CompareStringValues(x, y);
            }
        }
        
        /// <summary>
        /// Compares DateTime values with chronological ordering
        /// </summary>
        private int CompareDateTimeValues(object x, object y)
        {
            try
            {
                // Handle null display values - nulls come first
                if (x is NullDisplayValue && y is NullDisplayValue) return 0;
                if (x is NullDisplayValue) return -1;
                if (y is NullDisplayValue) return 1;
                
                // Handle actual null values - nulls come first
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                
                // Convert to DateTime using TypeTranslatorHelper
                var xDateTime = TypeTranslatorHelper.ConvertToDateTime(x);
                var yDateTime = TypeTranslatorHelper.ConvertToDateTime(y);
                
                // Handle conversion failures - treat as null and sort to beginning
                if (!xDateTime.HasValue && !yDateTime.HasValue) return 0;
                if (!xDateTime.HasValue) return -1;
                if (!yDateTime.HasValue) return 1;
                
                return xDateTime.Value.CompareTo(yDateTime.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CompareDateTimeValues: {ex.Message}");
                // Fallback to string comparison
                return CompareStringValues(x, y);
            }
        }
        
        /// <summary>
        /// Compares Boolean values with logical ordering (null, false, true)
        /// </summary>
        private int CompareBooleanValues(object x, object y)
        {
            // Handle null display values - nulls come first
            if (x is NullDisplayValue && y is NullDisplayValue) return 0;
            if (x is NullDisplayValue) return -1;
            if (y is NullDisplayValue) return 1;
            
            // Handle actual null values - nulls come first
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            
            // Convert to boolean
            var xBool = ConvertToBoolean(x);
            var yBool = ConvertToBoolean(y);
            
            // Handle conversion failures - treat as null and sort to beginning
            if (!xBool.HasValue && !yBool.HasValue) return 0;
            if (!xBool.HasValue) return -1;
            if (!yBool.HasValue) return 1;
            
            // Sort: false before true
            return xBool.Value.CompareTo(yBool.Value);
        }
        
        /// <summary>
        /// Compares enum values by their numeric value, falls back to string comparison
        /// </summary>
        private int CompareEnumValues(object x, object y)
        {
            try
            {
                // Handle null display values - nulls come first
                if (x is NullDisplayValue && y is NullDisplayValue) return 0;
                if (x is NullDisplayValue) return -1;
                if (y is NullDisplayValue) return 1;
                
                // Handle actual null values - nulls come first
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                
                // If both are the same enum type, compare by numeric value
                if (x.GetType().IsEnum && y.GetType().IsEnum && x.GetType() == y.GetType())
                {
                    var xValue = Convert.ToInt32(x);
                    var yValue = Convert.ToInt32(y);
                    return xValue.CompareTo(yValue);
                }
                
                // Fallback to string comparison for mixed types or non-enum values
                return CompareStringValues(x, y);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CompareEnumValues: {ex.Message}");
                // Fallback to string comparison
                return CompareStringValues(x, y);
            }
        }
        
        /// <summary>
        /// Compares string values with case-insensitive ordering (current behavior)
        /// </summary>
        private int CompareStringValues(object x, object y)
        {
            var xStr = x?.ToString() ?? string.Empty;
            var yStr = y?.ToString() ?? string.Empty;
            return string.Compare(xStr, yStr, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Converts a value to boolean, handling various formats
        /// </summary>
        private bool? ConvertToBoolean(object value)
        {
            if (value is bool boolValue)
                return boolValue;
                
            if (value == null || value is NullDisplayValue)
                return null;
                
            var stringValue = value.ToString()?.ToLowerInvariant();
            switch (stringValue)
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    return false;
                default:
                    if (bool.TryParse(stringValue, out bool parsed))
                        return parsed;
                    return null;
            }
        }
        
        /// <summary>
        /// Adds or updates a single value in the column values (for incremental updates)
        /// </summary>
        /// <param name="value">Value to add or update</param>
        public void AddOrUpdateColumnValue(object value)
        {
            if (!_columnValuesLoaded)
            {
                // If values aren't loaded yet, just mark them as needing refresh
                return;
            }
            
            var normalizedValue = NormalizeValue(value);
            if (!_columnValues.Contains(normalizedValue))
            {
                // Insert in sorted order if collection is small enough
                if (_columnValues.Count <= 10000)
                {
                    var insertIndex = 0;
                    var comparer = CreateTypeAwareComparer(ColumnDataType);
                    
                    for (int i = 0; i < _columnValues.Count; i++)
                    {
                        if (comparer(normalizedValue, _columnValues[i]) < 0)
                        {
                            insertIndex = i;
                            break;
                        }
                        insertIndex = i + 1;
                    }
                    
                    _columnValues.Insert(insertIndex, normalizedValue);
                }
                else
                {
                    // For large collections, just append (sorting is disabled anyway)
                    _columnValues.Add(normalizedValue);
                }
            }
        }
        
        /// <summary>
        /// Removes a value from the column values (for incremental updates)
        /// </summary>
        /// <param name="value">Value to remove</param>
        public void RemoveColumnValue(object value)
        {
            if (!_columnValuesLoaded)
            {
                // If values aren't loaded yet, just mark them as needing refresh
                return;
            }
            
            var normalizedValue = NormalizeValue(value);
            _columnValues.Remove(normalizedValue);
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
            ColumnDataType = ColumnDataType.String;

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
                    targetColumnType = _filterExpressionBuilder.DetermineTargetColumnType(ColumnDataType, new HashSet<object>(ColumnValues));
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
        public bool EvaluateWithCollectionContext(object value, Strategies.ICollectionContext collectionContext)
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
        private bool EvaluateSearchGroupWithContext(object value, SearchTemplateGroup group, Strategies.ICollectionContext collectionContext)
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
        private bool EvaluateSearchTemplateWithContext(object value, SearchTemplate template, Strategies.ICollectionContext collectionContext)
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
            OnFilterShouldApply();
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
                    case SearchType.IsBlank:
                        return "Is blank";
                    case SearchType.IsNotBlank:
                        return "Is not blank";
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
                    case SearchType.IsBlank:
                        components.SearchTypeText = "Is blank";
                        break;
                    case SearchType.IsNotBlank:
                        components.SearchTypeText = "Is not blank";
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
                   searchType == SearchType.IsBlank ||
                   searchType == SearchType.IsNotBlank ||
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
        /// Raises the FilterShouldApply event when there are valid filters that should be applied
        /// </summary>
        protected virtual void OnFilterShouldApply()
        {
            // Only fire if we have at least one valid filter to apply
            if (HasValidFilters)
            {
                FilterShouldApply?.Invoke(this, EventArgs.Empty);
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
                nameof(SearchTemplate.SelectedValues),
                nameof(SearchTemplate.SelectedDates),
                nameof(SearchTemplate.DateIntervals)
            };

            if (filterRelevantProperties.Contains(e.PropertyName))
            {
                OnFilterShouldApply();
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
                    }
                }
            }
        }

        #endregion
    }
}