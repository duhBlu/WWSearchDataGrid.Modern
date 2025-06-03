using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
                if (SetProperty(value, ref selectAllState) && value.HasValue)
                {
                    // Batch update all items
                    lock (_updateLock)
                    {
                        foreach (var item in _allItems)
                        {
                            item.SetIsSelected(value.Value);
                        }
                    }
                    OnPropertyChanged(nameof(SelectedItemsCount));
                }
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

        protected override void LoadValuesInternal(IEnumerable<object> values, Dictionary<object, int> valueCounts)
        {
            lock (_updateLock)
            {
                _allItems.Clear();

                var items = values
                    .Select(v => new FilterValueItem
                    {
                        Value = v,
                        DisplayValue = v?.ToString() ?? "(blank)",
                        ItemCount = GetSafeValueCount(v, valueCounts),
                        IsSelected = true
                    })
                    .OrderBy(i => i.DisplayValue);

                foreach (var item in items)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                    _allItems.Add(item);
                }
            }

            ApplyFilter();
            UpdateSelectAllState();
        }

        public override void LoadValues(IEnumerable<object> values)
        {
            var counts = new NullSafeDictionary<object, int>();
            foreach (var value in values)
            {
                if (counts.ContainsKey(value))
                    counts[value]++;
                else
                    counts[value] = 1;
            }
            LoadValuesInternal(values.Distinct(), counts);
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueItem.IsSelected))
            {
                UpdateSelectAllState();
                OnPropertyChanged(nameof(SelectedItemsCount));
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
                            DisplayValue = value?.ToString() ?? "(blank)",
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
    }
}