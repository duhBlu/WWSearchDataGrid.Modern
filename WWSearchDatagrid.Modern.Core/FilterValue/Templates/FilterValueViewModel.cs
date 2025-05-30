using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced base class for filter value view models - handles both Dictionary types
    /// </summary>
    public abstract class FilterValueViewModel : ObservableObject
    {
        protected bool isLoaded = false;
        protected IEnumerable<object> cachedValues;
        protected Dictionary<object, int> cachedValueCounts;

        public bool IsLoaded => isLoaded;

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

        protected abstract void LoadValuesInternal(IEnumerable<object> values, Dictionary<object, int> valueCounts);

        public abstract void LoadValues(IEnumerable<object> values);
        public abstract IEnumerable<object> GetSelectedValues();
        public abstract void SelectAll();
        public abstract void ClearAll();
        public abstract string GetSelectionSummary();

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
        /// Helper method to safely get value count - properly handles null values
        /// </summary>
        protected int GetSafeValueCount(object value, Dictionary<object, int> valueCounts)
        {
            if (valueCounts == null)
                return 1;

            // Special handling for NullSafeDictionary
            if (valueCounts is NullSafeDictionary<object, int> nullSafeDict)
            {
                return nullSafeDict.ContainsKey(value) ? nullSafeDict[value] : 1;
            }

            // For regular dictionary, we can't use null as key
            if (value == null)
            {
                // Look for a special marker or return default
                return 1;
            }

            // Safe to check non-null values in regular dictionary
            return valueCounts.ContainsKey(value) ? valueCounts[value] : 1;
        }
    }
}
