using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced flat list filter view model with virtualization support
    /// </summary>
    public class FlatListFilterValueViewModel : FilterValueViewModel
    {
        private bool? selectAllState = true;
        private bool _isBulkUpdating = false;
        private readonly ObservableCollection<FilterValueItem> _allItems;
        private readonly ObservableCollection<FilterValueItem> _filteredItems;
        private readonly object _updateLock = new object();

        public ObservableCollection<FilterValueItem> FilterValues => _allItems;
        public ObservableCollection<FilterValueItem> FilteredValues => _filteredItems;

        public bool? SelectAllState
        {
            get => selectAllState;
            set
            {
                // Handle the different states properly
                bool targetState;
                
                if (value == true)
                {
                    // User wants to select all
                    targetState = true;
                }
                else if (value == false)
                {
                    // User wants to unselect all
                    targetState = false;
                }
                else
                {
                    // Null/indeterminate - determine intent based on current state
                    // If currently true (all selected), user wants to unselect
                    // If currently false (none selected), user wants to select
                    // If currently null (some selected), user wants to select all
                    targetState = selectAllState != true;
                }

                // Batch update all items
                _isBulkUpdating = true;
                try
                {
                    lock (_updateLock)
                    {
                        foreach (var item in _allItems)
                        {
                            item.SetIsSelected(targetState);
                        }
                    }
                }
                finally
                {
                    _isBulkUpdating = false;
                }
                
                // Always update the state and notify (don't rely on SetProperty)
                selectAllState = targetState;
                OnPropertyChanged(nameof(SelectAllState));
                OnPropertyChanged(nameof(SelectedItemsCount));
            }
        }

        public string SelectedItemsCount
        {
            get
            {
                var selected = _allItems.Count(v => v.IsSelected);
                var total = _allItems.Count;
                return $"{selected} of {total} selected";
            }
        }

        public ICommand ClearSearchTextCommand => new RelayCommand(_ => ClearFilter());

        public FlatListFilterValueViewModel()
        {
            _allItems = new ObservableCollection<FilterValueItem>();
            _filteredItems = new ObservableCollection<FilterValueItem>();
        }

        public void ClearFilter()
        {
            try
            {
                // Clear the search text (this will trigger ApplyFilter via the base class)
                SearchText = string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearFilter: {ex.Message}");
            }
        }

        /// <summary>
        /// Override the base class filter method for flat list specific filtering
        /// </summary>
        protected override void ApplyFilter()
        {
            _filteredItems.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allItems
                : _allItems.Where(item => MatchesSearchText(item.DisplayValue, SearchText));

            foreach (var item in filtered)
            {
                _filteredItems.Add(item);
            }
        }

        /// <summary>
        /// Loads values from metadata, preserving selection state while efficiently updating counts
        /// </summary>
        protected override void LoadValuesFromMetadata(IEnumerable<ValueAggregateMetadata> metadata)
        {
            lock (_updateLock)
            {
                // Create a lookup of existing items to preserve their selection state
                var existingItems = new Dictionary<string, FilterValueItem>();
                foreach (var item in _allItems)
                {
                    var key = item.Value?.ToString() ?? "__NULL__";
                    existingItems[key] = item;
                }
                
                // Track which existing items we've seen in the new data
                var seenKeys = new HashSet<string>();
                
                // Update existing items and add new ones directly from metadata
                var newItems = metadata
                    .Select(m => 
                    {
                        var key = m.Value?.ToString() ?? "__NULL__";
                        seenKeys.Add(key);
                        
                        if (existingItems.TryGetValue(key, out var existingItem))
                        {
                            // Update existing item's count but preserve selection state
                            existingItem.ItemCount = m.Count;
                            return existingItem;
                        }
                        else
                        {
                            // Create new item with default selected state using proper display text
                            var newItem = new FilterValueItem
                            {
                                Value = m.Value,
                                DisplayValue = m.DisplayText ?? GetValueDisplayText(m.Value),
                                ItemCount = m.Count,
                                IsSelected = true
                            };
                            newItem.PropertyChanged += OnItemPropertyChanged;
                            return newItem;
                        }
                    })
                    .ToList();
                
                // Add any existing items that weren't in the new data (preserve unselected filtered values)
                foreach (var kvp in existingItems)
                {
                    if (!seenKeys.Contains(kvp.Key))
                    {
                        // Keep the item but set count to 0 to indicate it's not in current data
                        kvp.Value.ItemCount = 0;
                        newItems.Add(kvp.Value);
                    }
                }
                
                // Clear and rebuild the collection
                _allItems.Clear();
                
                foreach (var item in newItems.OrderBy(i => i.DisplayValue))
                {
                    _allItems.Add(item);
                }
            }

            ApplyFilter();
            UpdateSelectAllState();
        }



        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueItem.IsSelected))
            {
                // Don't update during bulk operations
                if (!_isBulkUpdating)
                {
                    UpdateSelectAllState();
                }
                OnPropertyChanged(nameof(SelectedItemsCount));
                
                // Trigger selection changed event for synchronization
                OnSelectionChanged();
            }
        }

        private void UpdateSelectAllState()
        {
            if (_allItems.Count == 0)
            {
                selectAllState = false;
            }
            else
            {
                var selectedCount = _allItems.Count(v => v.IsSelected);
                if (selectedCount == 0)
                    selectAllState = false;
                else if (selectedCount == _allItems.Count)
                    selectAllState = true;
                else
                    selectAllState = null;
            }
            OnPropertyChanged(nameof(SelectAllState));
        }

        public override void UpdateValueIncremental(object value, bool isAdd)
        {
            lock (_updateLock)
            {
                var existingItem = _allItems.FirstOrDefault(i => Equals(i.Value, value));

                if (isAdd)
                {
                    if (existingItem != null)
                    {
                        existingItem.ItemCount++;
                    }
                    else
                    {
                        var newItem = new FilterValueItem
                        {
                            Value = value,
                            DisplayValue = GetValueDisplayText(value),
                            ItemCount = 1,
                            IsSelected = selectAllState ?? false
                        };
                        newItem.PropertyChanged += OnItemPropertyChanged;

                        // Insert in sorted order
                        var index = FindInsertIndex(_allItems, newItem);
                        _allItems.Insert(index, newItem);

                        // Also add to filtered if it matches search criteria
                        if (MatchesSearchText(newItem.DisplayValue, SearchText))
                        {
                            var filteredIndex = FindInsertIndex(_filteredItems, newItem);
                            _filteredItems.Insert(filteredIndex, newItem);
                        }
                    }
                }
                else if (existingItem != null)
                {
                    existingItem.ItemCount--;
                    if (existingItem.ItemCount <= 0)
                    {
                        existingItem.PropertyChanged -= OnItemPropertyChanged;
                        _allItems.Remove(existingItem);
                        _filteredItems.Remove(existingItem);
                    }
                }
            }

            UpdateSelectAllState();
        }

        private int FindInsertIndex(ObservableCollection<FilterValueItem> collection, FilterValueItem item)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (string.Compare(collection[i].DisplayValue, item.DisplayValue, StringComparison.Ordinal) > 0)
                    return i;
            }
            return collection.Count;
        }

        public override IEnumerable<object> GetSelectedValues()
        {
            return _allItems.Where(v => v.IsSelected).Select(v => v.Value);
        }

        public override void SelectAll()
        {
            SelectAllState = true;
        }

        public override void ClearAll()
        {
            SelectAllState = false;
        }

        public override string GetSelectionSummary()
        {
            return SelectedItemsCount;
        }

        public override List<FilterValueItem> GetAllValues()
        {
            return _allItems.ToList();
        }

        public override List<FilterValueItem> GetUnselectedValues()
        {
            return _allItems.Where(item => !item.IsSelected).ToList();
        }
    }
}