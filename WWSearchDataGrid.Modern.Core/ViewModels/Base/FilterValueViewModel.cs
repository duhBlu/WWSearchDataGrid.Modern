using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using WWSearchDataGrid.Modern.Core.Performance;
using WWSearchDataGrid.Modern.Core.Services;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced base class for filter value view models - handles both Dictionary types and search functionality
    /// </summary>
    public abstract class FilterValueViewModel : ObservableObject
    {
        protected bool isLoaded = false;
        protected IEnumerable<object> cachedValues;
        protected Dictionary<object, int> cachedValueCounts;
        protected IEnumerable<ValueAggregateMetadata> cachedMetadata;
        private string _searchText = string.Empty;

        public bool IsLoaded => isLoaded;

        /// <summary>
        /// Gets or sets the search text for filtering values
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(value, ref _searchText))
                {
                    ApplyFilter();
                }
            }
        }

        public void EnsureLoaded(IEnumerable<object> values)
        {
            if (!isLoaded || cachedValues != values)
            {
                cachedValues = values;
                LoadValues(values);
                isLoaded = true;
            }
        }

        public void LoadValuesWithCounts(IEnumerable<object> values, Dictionary<object, int> valueCounts)
        {
            cachedValues = values;
            cachedValueCounts = valueCounts;
            LoadValuesInternal(values, valueCounts);
            isLoaded = true;
        }

        /// <summary>
        /// Loads values directly from ValueAggregateMetadata - preferred method
        /// </summary>
        public void LoadValuesWithMetadata(IEnumerable<ValueAggregateMetadata> metadata)
        {
            cachedMetadata = metadata;
            
            // For backward compatibility, also populate the legacy properties
            cachedValues = metadata.Select(m => m.Value);
            // Create value counts dictionary that properly handles null keys
            var regularDict = new Dictionary<object, int>();
            int nullCount = 0;
            
            foreach (var item in metadata)
            {
                if (item.Value == null)
                {
                    nullCount = item.Count;
                }
                else
                {
                    regularDict[item.Value] = item.Count;
                }
            }
            
            cachedValueCounts = new NullHandlingDictionary(regularDict, nullCount);
            
            LoadValuesFromMetadata(metadata);
            isLoaded = true;
        }

        protected abstract void LoadValuesInternal(IEnumerable<object> values, Dictionary<object, int> valueCounts);
        
        /// <summary>
        /// Abstract method for loading values from metadata - should be implemented by derived classes
        /// </summary>
        protected abstract void LoadValuesFromMetadata(IEnumerable<ValueAggregateMetadata> metadata);

        /// <summary>
        /// Virtual method to apply search filter - can be overridden by derived classes
        /// </summary>
        protected virtual void ApplyFilter()
        {
            // Default implementation - derived classes should override this
            // to provide their specific filtering logic
        }

        /// <summary>
        /// Helper method for simple string matching - can be used by derived classes
        /// </summary>
        protected virtual bool MatchesSearchText(string displayValue, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            if (string.IsNullOrEmpty(displayValue))
                return false;

            return displayValue.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public abstract void LoadValues(IEnumerable<object> values);
        public abstract IEnumerable<object> GetSelectedValues();
        public abstract void SelectAll();
        public abstract void ClearAll();
        public abstract string GetSelectionSummary();

        /// <summary>
        /// Gets all available filter items regardless of selection state
        /// </summary>
        /// <returns>List of all filter value items</returns>
        public abstract List<FilterValueItem> GetAllValues();

        /// <summary>
        /// Gets filter items that are NOT currently selected
        /// </summary>
        /// <returns>List of unselected filter value items</returns>
        public abstract List<FilterValueItem> GetUnselectedValues();

        public virtual void ClearCache()
        {
            cachedValues = null;
            cachedValueCounts = null;
            isLoaded = false;
        }

        public virtual void UpdateValueIncremental(object value, bool isAdd)
        {
            // Default implementation - override in derived classes
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Helper method to get value metadata for proper categorization
        /// </summary>
        protected ValueMetadata GetValueMetadata(object value)
        {
            return ValueMetadata.Create(value);
        }

        /// <summary>
        /// Helper method to get value count from metadata dictionary
        /// </summary>
        protected int GetValueCount(ValueMetadata metadata, Dictionary<ValueMetadata, int> counts)
        {
            return counts?.ContainsKey(metadata) == true ? counts[metadata] : 1;
        }

        /// <summary>
        /// Helper method to get display text for a value
        /// </summary>
        protected string GetValueDisplayText(object value)
        {
            return ValueMetadata.Create(value).GetDisplayText();
        }

        /// <summary>
        /// Helper method to safely get value count - properly handles null values
        /// Deprecated: Use GetValueCount with ValueMetadata instead
        /// </summary>
        [Obsolete("Use GetValueCount with ValueMetadata instead")]
        protected int GetSafeValueCount(object value, Dictionary<object, int> valueCounts)
        {
            if (valueCounts == null)
                return 1;

            // Check if this is a NullHandlingDictionary (new implementation)
            if (valueCounts is NullHandlingDictionary nullHandlingDict)
            {
                return nullHandlingDict.ContainsKey(value) ? nullHandlingDict[value] : 1;
            }

            // For regular dictionaries, handle null values specially
            if (value == null)
            {
                // Since regular Dictionary<object, int> throws on null keys,
                // we'll return 1 as a fallback for null values
                return 1;
            }

            // Safe to check non-null values in regular dictionary
            return valueCounts.ContainsKey(value) ? valueCounts[value] : 1;
        }
    }
}