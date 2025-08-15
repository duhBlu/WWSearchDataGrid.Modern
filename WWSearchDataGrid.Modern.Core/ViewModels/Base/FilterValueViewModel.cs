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


        /// <summary>
        /// PRIMARY METHOD: Loads values directly from ValueAggregateMetadata
        /// This is the preferred method for loading filter values as it:
        /// - Provides accurate counts without redundant calculation
        /// - Handles null values properly through NullHandlingDictionary
        /// - Includes value categorization and display text
        /// - Maintains optimal performance for large datasets
        /// </summary>
        public void LoadValuesWithMetadata(IEnumerable<ValueAggregateMetadata> metadata)
        {
            cachedMetadata = metadata;
            LoadValuesFromMetadata(metadata);
            isLoaded = true;
        }

        /// <summary>
        /// Load values from metadata - should be implemented by derived classes
        /// This provides accurate counts and proper null handling
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
            cachedMetadata = null;
            isLoaded = false;
        }

        /// <summary>
        /// Ensures the view model is loaded with metadata. If not loaded or metadata has changed, reloads.
        /// </summary>
        /// <param name="metadata">The metadata to load</param>
        public void EnsureLoadedWithMetadata(IEnumerable<ValueAggregateMetadata> metadata)
        {
            if (!isLoaded || cachedMetadata != metadata)
            {
                LoadValuesWithMetadata(metadata);
            }
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

    }
}