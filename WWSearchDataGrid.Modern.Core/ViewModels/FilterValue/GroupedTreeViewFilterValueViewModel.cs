using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced grouped tree view filter value view model with performance optimizations
    /// </summary>
    public class GroupedTreeViewFilterValueViewModel : FilterValueViewModel
    {
        private string _groupByColumn;
        private bool? _selectAllState = true;
        private bool _isBulkUpdating = false;
        private readonly ObservableCollection<FilterValueGroup> _allGroups;
        private readonly ObservableCollection<FilterValueGroup> _filteredGroups;
        private readonly Dictionary<string, Dictionary<object, List<object>>> _groupedData;
        private readonly Dictionary<string, FilterValueGroup> _groupIndex;
        private readonly object _updateLock = new object();

        public ObservableCollection<FilterValueGroup> GroupedValues => _filteredGroups;
        public ObservableCollection<FilterValueGroup> AllGroups => _allGroups;

        public string GroupByColumn
        {
            get => _groupByColumn;
            set
            {
                if (SetProperty(value, ref _groupByColumn))
                {
                    ReloadGroups();
                }
            }
        }

        public string SelectionSummary => GetSelectionSummary();

        public bool? SelectAllState
        {
            get => _selectAllState;
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
                    targetState = _selectAllState != true;
                }

                _isBulkUpdating = true;
                try
                {
                    lock (_updateLock)
                    {
                        foreach (var group in _allGroups)
                        {
                            group.IsSelected = targetState;
                        }
                    }
                }
                finally
                {
                    _isBulkUpdating = false;
                }
                
                // Always update the state and notify (don't rely on SetProperty)
                _selectAllState = targetState;
                OnPropertyChanged(nameof(SelectAllState));
                OnPropertyChanged(nameof(SelectionSummary));
            }
        }

        public GroupedTreeViewFilterValueViewModel()
        {
            _allGroups = new ObservableCollection<FilterValueGroup>();
            _filteredGroups = new ObservableCollection<FilterValueGroup>();
            _groupedData = new Dictionary<string, Dictionary<object, List<object>>>();
            _groupIndex = new Dictionary<string, FilterValueGroup>();
        }

        /// <summary>
        /// Sets the grouped data for a specific column with metadata
        /// </summary>
        public void SetGroupedData(string columnPath, Dictionary<object, List<object>> groupData,
            Dictionary<string, string> displayNames = null)
        {
            lock (_updateLock)
            {
                _groupedData[columnPath] = groupData;

                // Store display name mappings if provided
                if (displayNames != null)
                {
                    if (!_groupDisplayNames.ContainsKey(columnPath))
                    {
                        _groupDisplayNames[columnPath] = new Dictionary<string, string>();
                    }
                    _groupDisplayNames[columnPath] = displayNames;
                }
            }
        }

        private readonly Dictionary<string, Dictionary<string, string>> _groupDisplayNames =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Override the base class filter method for hierarchical filtering
        /// </summary>
        protected override void ApplyFilter()
        {
            _filteredGroups.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var group in _allGroups)
                {
                    _filteredGroups.Add(group);
                }
            }
            else
            {
                foreach (var group in _allGroups)
                {
                    // Check if group name matches
                    var groupMatches = MatchesSearchText(group.DisplayValue, SearchText);

                    // Check if any children match
                    var hasMatchingChildren = group.Children.Any(child =>
                        MatchesSearchText(child.DisplayValue, SearchText));

                    if (groupMatches || hasMatchingChildren)
                    {
                        // Create filtered group
                        var filteredGroup = new FilterValueGroup
                        {
                            DisplayValue = group.DisplayValue,
                            GroupKey = group.GroupKey,
                            IsSelected = group.IsSelected,
                            ItemCount = 0
                        };

                        // Add matching children
                        foreach (var child in group.Children)
                        {
                            if (groupMatches || MatchesSearchText(child.DisplayValue, SearchText))
                            {
                                filteredGroup.Children.Add(child);
                                filteredGroup.ItemCount += child.ItemCount;
                            }
                        }

                        if (filteredGroup.Children.Any())
                        {
                            _filteredGroups.Add(filteredGroup);
                        }
                    }
                }
            }

            OnPropertyChanged(nameof(SelectionSummary));
        }

        protected override void LoadValuesInternal(IEnumerable<object> values, Dictionary<object, int> valueCounts)
        {
            lock (_updateLock)
            {
                // Preserve existing groups and their selection states
                var existingGroups = _allGroups.ToDictionary(g => g.GroupKey?.ToString() ?? "null", g => g);
                var existingGroupIndex = new Dictionary<string, FilterValueGroup>(_groupIndex);
                
                _allGroups.Clear();
                _groupIndex.Clear();

                // If we have grouped data for the GroupByColumn, use it
                if (!string.IsNullOrEmpty(GroupByColumn) && _groupedData.ContainsKey(GroupByColumn))
                {
                    LoadGroupedValuesOptimized(values, valueCounts, existingGroups);
                }
                else
                {
                    // Fallback to smart grouping
                    LoadSmartGroupedValues(values, valueCounts, existingGroups);
                }
                
                // Add any existing groups that weren't in the new data (preserve unselected filtered groups)
                foreach (var existingGroup in existingGroups.Values)
                {
                    var groupKeyStr = existingGroup.GroupKey?.ToString() ?? "null";
                    if (!_groupIndex.ContainsKey(groupKeyStr))
                    {
                        // Reset item counts for children not in current data
                        foreach (var child in existingGroup.Children.OfType<FilterValueItem>())
                        {
                            child.ItemCount = 0;
                        }
                        existingGroup.ItemCount = 0;
                        
                        _allGroups.Add(existingGroup);
                        _groupIndex[groupKeyStr] = existingGroup;
                    }
                }
            }

            ApplyFilter();
            UpdateSelectAllState();
        }

        private void LoadGroupedValuesOptimized(IEnumerable<object> values, Dictionary<object, int> valueCounts, Dictionary<string, FilterValueGroup> existingGroups = null)
        {
            var valueSet = new HashSet<object>(values);
            var groupData = _groupedData[GroupByColumn];
            var displayNames = _groupDisplayNames.ContainsKey(GroupByColumn)
                ? _groupDisplayNames[GroupByColumn]
                : null;

            // Create groups with optimized loading
            var groups = groupData
                .Select(g => new
                {
                    GroupKey = g.Key,
                    GroupDisplay = GetGroupDisplayName(g.Key, displayNames),
                    Items = g.Value.Where(v => valueSet.Contains(v)).ToList(),
                    SortKey = GetGroupSortKey(g.Key)
                })
                .Where(g => g.Items.Any())
                .OrderBy(g => g.SortKey)
                .ThenBy(g => g.GroupDisplay);

            foreach (var group in groups)
            {
                var groupKeyStr = group.GroupKey?.ToString() ?? "null";
                FilterValueGroup groupItem;
                
                // Check if we have an existing group to preserve its selection state
                if (existingGroups != null && existingGroups.TryGetValue(groupKeyStr, out var existingGroup))
                {
                    groupItem = existingGroup;
                    groupItem.DisplayValue = group.GroupDisplay;
                    groupItem.ItemCount = 0; // Will be recalculated
                    
                    // Create a lookup of existing children to preserve their selection states
                    var existingChildren = new Dictionary<string, FilterValueItem>();
                    foreach (var child in groupItem.Children.OfType<FilterValueItem>())
                    {
                        var key = child.Value?.ToString() ?? "__NULL__";
                        existingChildren[key] = child;
                    }
                    
                    groupItem.Children.Clear();
                    
                    // Add children with preserved selection states
                    var itemGroups = group.Items
                        .GroupBy(v => v)
                        .Select(g => new
                        {
                            Value = g.Key,
                            Count = valueCounts?.ContainsKey(g.Key) == true ? valueCounts[g.Key] : g.Count(),
                            Display = g.Key?.ToString() ?? "(blank)",
                            SortKey = GetItemSortKey(g.Key)
                        })
                        .OrderBy(i => i.SortKey)
                        .ThenBy(i => i.Display);

                    var seenChildKeys = new HashSet<string>();
                    
                    foreach (var item in itemGroups)
                    {
                        FilterValueItem childItem;
                        
                        var childKey = item.Value?.ToString() ?? "__NULL__";
                        seenChildKeys.Add(childKey);
                        
                        if (existingChildren.TryGetValue(childKey, out var existingChild))
                        {
                            // Preserve existing child's selection state but update count
                            childItem = existingChild;
                            childItem.ItemCount = item.Count;
                            childItem.Parent = groupItem; // Ensure parent is set correctly
                        }
                        else
                        {
                            // Create new child with default selection (true for new items)
                            childItem = new FilterValueItem
                            {
                                Value = item.Value,
                                DisplayValue = item.Display,
                                ItemCount = item.Count,
                                IsSelected = true, // New items default to selected
                                Parent = groupItem
                            };
                            childItem.PropertyChanged += OnChildPropertyChanged;
                        }

                        groupItem.Children.Add(childItem);
                        groupItem.ItemCount += item.Count;
                    }
                    
                    // Add any existing children that weren't in the new data (preserve unselected filtered values)
                    foreach (var existingChildKvp in existingChildren)
                    {
                        if (!seenChildKeys.Contains(existingChildKvp.Key))
                        {
                            // Keep the child but set count to 0 to indicate it's not in current data
                            var filteredChild = existingChildKvp.Value;
                            filteredChild.ItemCount = 0;
                            filteredChild.Parent = groupItem;
                            groupItem.Children.Add(filteredChild);
                        }
                    }
                }
                else
                {
                    // Create new group
                    groupItem = new FilterValueGroup
                    {
                        DisplayValue = group.GroupDisplay,
                        GroupKey = group.GroupKey,
                        IsSelected = true,
                        ItemCount = 0 // Will be calculated from children
                    };

                    // Add children with counts
                    var itemGroups = group.Items
                        .GroupBy(v => v)
                        .Select(g => new
                        {
                            Value = g.Key,
                            Count = valueCounts?.ContainsKey(g.Key) == true ? valueCounts[g.Key] : g.Count(),
                            Display = g.Key?.ToString() ?? "(blank)",
                            SortKey = GetItemSortKey(g.Key)
                        })
                        .OrderBy(i => i.SortKey)
                        .ThenBy(i => i.Display);

                    foreach (var item in itemGroups)
                    {
                        var childItem = new FilterValueItem
                        {
                            Value = item.Value,
                            DisplayValue = item.Display,
                            ItemCount = item.Count,
                            IsSelected = true,
                            Parent = groupItem
                        };

                        childItem.PropertyChanged += OnChildPropertyChanged;
                        groupItem.Children.Add(childItem);
                        groupItem.ItemCount += item.Count;
                    }

                    groupItem.PropertyChanged += OnGroupPropertyChanged;
                }

                // Update group selection state based on children
                if (existingGroups != null && existingGroups.ContainsKey(groupKeyStr))
                {
                    // For existing groups, update the group's selection state based on its children
                    groupItem.UpdateGroupSelectionState();
                }
                
                // Register in index for fast lookup
                _groupIndex[groupKeyStr] = groupItem;
                _allGroups.Add(groupItem);
            }
        }

        private void LoadSmartGroupedValues(IEnumerable<object> values, Dictionary<object, int> valueCounts, Dictionary<string, FilterValueGroup> existingGroups = null)
        {
            // Smart grouping based on data type and patterns
            var groupingStrategy = DetermineGroupingStrategy(values);

            var groups = values
                .GroupBy(v => groupingStrategy(v))
                .Select(g => new
                {
                    GroupKey = g.Key,
                    Items = g.ToList(),
                    SortKey = GetGroupSortKey(g.Key)
                })
                .OrderBy(g => g.SortKey);

            foreach (var group in groups)
            {
                var groupKeyStr = group.GroupKey?.ToString() ?? "null";
                FilterValueGroup groupItem;
                
                // Check if we have an existing group to preserve its selection state
                if (existingGroups != null && existingGroups.TryGetValue(groupKeyStr, out var existingGroup))
                {
                    groupItem = existingGroup;
                    groupItem.DisplayValue = group.GroupKey?.ToString() ?? "(No Group)";
                    groupItem.ItemCount = 0; // Will be recalculated
                    
                    // Create a lookup of existing children to preserve their selection states
                    var existingChildren = new Dictionary<string, FilterValueItem>();
                    foreach (var child in groupItem.Children.OfType<FilterValueItem>())
                    {
                        var key = child.Value?.ToString() ?? "__NULL__";
                        existingChildren[key] = child;
                    }
                    
                    groupItem.Children.Clear();
                    
                    var seenChildKeys = new HashSet<string>();
                    
                    foreach (var value in group.Items.OrderBy(v => v?.ToString()))
                    {
                        var count = valueCounts?.ContainsKey(value) == true ? valueCounts[value] : 1;
                        FilterValueItem childItem;
                        
                        var childKey = value?.ToString() ?? "__NULL__";
                        seenChildKeys.Add(childKey);
                        
                        if (existingChildren.TryGetValue(childKey, out var existingChild))
                        {
                            // Preserve existing child's selection state but update count
                            childItem = existingChild;
                            childItem.ItemCount = count;
                            childItem.Parent = groupItem; // Ensure parent is set correctly
                        }
                        else
                        {
                            // Create new child with default selection (true for new items)
                            childItem = new FilterValueItem
                            {
                                Value = value,
                                DisplayValue = value?.ToString() ?? "(blank)",
                                ItemCount = count,
                                IsSelected = true, // New items default to selected
                                Parent = groupItem
                            };
                            childItem.PropertyChanged += OnChildPropertyChanged;
                        }

                        groupItem.Children.Add(childItem);
                        groupItem.ItemCount += count;
                    }
                    
                    // Add any existing children that weren't in the new data (preserve unselected filtered values)
                    foreach (var existingChildKvp in existingChildren)
                    {
                        if (!seenChildKeys.Contains(existingChildKvp.Key))
                        {
                            // Keep the child but set count to 0 to indicate it's not in current data
                            var filteredChild = existingChildKvp.Value;
                            filteredChild.ItemCount = 0;
                            filteredChild.Parent = groupItem;
                            groupItem.Children.Add(filteredChild);
                        }
                    }
                }
                else
                {
                    // Create new group
                    groupItem = new FilterValueGroup
                    {
                        DisplayValue = group.GroupKey?.ToString() ?? "(No Group)",
                        GroupKey = group.GroupKey,
                        IsSelected = true,
                        ItemCount = 0
                    };

                    foreach (var value in group.Items.OrderBy(v => v?.ToString()))
                    {
                        var count = valueCounts?.ContainsKey(value) == true ? valueCounts[value] : 1;
                        var childItem = new FilterValueItem
                        {
                            Value = value,
                            DisplayValue = value?.ToString() ?? "(blank)",
                            ItemCount = count,
                            IsSelected = true,
                            Parent = groupItem
                        };

                        childItem.PropertyChanged += OnChildPropertyChanged;
                        groupItem.Children.Add(childItem);
                        groupItem.ItemCount += count;
                    }

                    groupItem.PropertyChanged += OnGroupPropertyChanged;
                }

                _groupIndex[groupKeyStr] = groupItem;
                _allGroups.Add(groupItem);
            }
        }

        private Func<object, object> DetermineGroupingStrategy(IEnumerable<object> values)
        {
            if (!values.Any())
                return v => "All";

            var firstValue = values.First(v => v != null);
            if (firstValue == null)
                return v => v ?? "(Null)";

            var type = firstValue.GetType();

            // String grouping
            if (type == typeof(string))
            {
                return v =>
                {
                    if (v == null) return "(Null)";
                    var str = v.ToString();
                    if (string.IsNullOrEmpty(str)) return "(Empty)";

                    // Group by first letter for large sets, or by category patterns
                    if (values.Count() > 100)
                        return char.ToUpper(str[0]).ToString();

                    // Look for common patterns (e.g., "Category: Item")
                    var colonIndex = str.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < str.Length - 1)
                        return str.Substring(0, colonIndex).Trim();

                    return str.Length <= 20 ? str : str.Substring(0, 1).ToUpper();
                };
            }

            // Date grouping
            if (type == typeof(DateTime))
            {
                return v =>
                {
                    if (v == null) return "(Null)";
                    var date = (DateTime)v;

                    // Group by year/month for date ranges > 1 year
                    var dateRange = values.OfType<DateTime>().Max() - values.OfType<DateTime>().Min();
                    if (dateRange.TotalDays > 365)
                        return date.ToString("yyyy-MM");

                    // Group by month/day for smaller ranges
                    return date.ToString("MMM yyyy");
                };
            }

            // Numeric grouping
            if (IsNumericType(type))
            {
                return v =>
                {
                    if (v == null) return "(Null)";
                    var num = Convert.ToDouble(v);

                    // Determine range
                    var numericValues = values.Where(val => val != null).Select(val => Convert.ToDouble(val));
                    var min = numericValues.Min();
                    var max = numericValues.Max();
                    var range = max - min;

                    if (range <= 10)
                        return Math.Floor(num).ToString();
                    else if (range <= 100)
                        return $"{Math.Floor(num / 10) * 10}-{Math.Floor(num / 10) * 10 + 9}";
                    else if (range <= 1000)
                        return $"{Math.Floor(num / 100) * 100}-{Math.Floor(num / 100) * 100 + 99}";
                    else
                        return $"{Math.Floor(num / 1000) * 1000:N0}+";
                };
            }

            // Boolean grouping
            if (type == typeof(bool))
            {
                return v => v?.ToString() ?? "(Null)";
            }

            // Default grouping
            return v => v?.GetType().Name ?? "(Null)";
        }

        private string GetGroupDisplayName(object groupKey, Dictionary<string, string> displayNames)
        {
            if (groupKey == null)
                return "(No Value)";

            var key = groupKey.ToString();
            if (displayNames != null && displayNames.ContainsKey(key))
                return displayNames[key];

            return key;
        }

        private object GetGroupSortKey(object groupKey)
        {
            if (groupKey == null)
                return "";

            // Special handling for numeric strings
            if (groupKey is string str && double.TryParse(str, out var num))
                return num;

            return groupKey;
        }

        private object GetItemSortKey(object value)
        {
            if (value == null)
                return "";

            // Consistent sorting for different types
            if (value is string str && double.TryParse(str, out var num))
                return num;

            return value;
        }

        private void OnGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueItem.IsSelected))
            {
                // Don't update during bulk operations
                if (!_isBulkUpdating)
                {
                    UpdateSelectAllState();
                }
                OnPropertyChanged(nameof(SelectionSummary));
            }
        }

        private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueItem.IsSelected))
            {
                var item = sender as FilterValueItem;
                if (item?.Parent != null)
                {
                    // Update parent group state
                    item.Parent.UpdateGroupSelectionState();
                }
                
                // Don't update during bulk operations
                if (!_isBulkUpdating)
                {
                    UpdateSelectAllState();
                }
                OnPropertyChanged(nameof(SelectionSummary));
            }
        }

        public override void LoadValues(IEnumerable<object> values)
        {
            var counts = values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
            LoadValuesInternal(values.Distinct(), counts);
        }

        public override void UpdateValueIncremental(object value, bool isAdd)
        {
            // Determine which group this value belongs to
            var groupKey = GetGroupKeyForValue(value);
            var groupKeyStr = groupKey?.ToString() ?? "null";

            lock (_updateLock)
            {
                if (_groupIndex.TryGetValue(groupKeyStr, out var group))
                {
                    var existingItem = group.Children
                        .OfType<FilterValueItem>()
                        .FirstOrDefault(i => Equals(i.Value, value));

                    if (isAdd)
                    {
                        if (existingItem != null)
                        {
                            existingItem.ItemCount++;
                            group.ItemCount++;
                        }
                        else
                        {
                            // Add new item
                            var newItem = new FilterValueItem
                            {
                                Value = value,
                                DisplayValue = value?.ToString() ?? "(blank)",
                                ItemCount = 1,
                                IsSelected = group.IsSelected ?? false,
                                Parent = group
                            };

                            newItem.PropertyChanged += OnChildPropertyChanged;

                            // Insert in sorted position
                            var index = FindInsertIndex(group.Children, newItem);
                            group.Children.Insert(index, newItem);
                            group.ItemCount++;
                        }
                    }
                    else if (existingItem != null)
                    {
                        existingItem.ItemCount--;
                        group.ItemCount--;

                        if (existingItem.ItemCount <= 0)
                        {
                            existingItem.PropertyChanged -= OnChildPropertyChanged;
                            group.Children.Remove(existingItem);
                        }

                        // Remove group if empty
                        if (!group.Children.Any())
                        {
                            group.PropertyChanged -= OnGroupPropertyChanged;
                            _allGroups.Remove(group);
                            _groupIndex.Remove(groupKeyStr);
                        }
                    }
                }
                else if (isAdd)
                {
                    // Create new group
                    var newGroup = new FilterValueGroup
                    {
                        DisplayValue = groupKey?.ToString() ?? "(No Group)",
                        GroupKey = groupKey,
                        IsSelected = true,
                        ItemCount = 1
                    };

                    var newItem = new FilterValueItem
                    {
                        Value = value,
                        DisplayValue = value?.ToString() ?? "(blank)",
                        ItemCount = 1,
                        IsSelected = true,
                        Parent = newGroup
                    };

                    newItem.PropertyChanged += OnChildPropertyChanged;
                    newGroup.Children.Add(newItem);
                    newGroup.PropertyChanged += OnGroupPropertyChanged;

                    // Insert group in sorted position
                    var groupIndex = FindGroupInsertIndex(newGroup);
                    _allGroups.Insert(groupIndex, newGroup);
                    _groupIndex[groupKeyStr] = newGroup;
                }
            }

            // Reapply filter if needed
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                ApplyFilter();
            }
        }

        private object GetGroupKeyForValue(object value)
        {
            if (!string.IsNullOrEmpty(GroupByColumn) && _groupedData.ContainsKey(GroupByColumn))
            {
                var groupData = _groupedData[GroupByColumn];
                foreach (var kvp in groupData)
                {
                    if (kvp.Value.Contains(value))
                        return kvp.Key;
                }
            }

            // Fallback to grouping strategy
            var strategy = DetermineGroupingStrategy(new[] { value });
            return strategy(value);
        }

        private int FindInsertIndex(ObservableCollection<FilterValueItem> collection, FilterValueItem item)
        {
            var sortKey = GetItemSortKey(item.Value);

            for (int i = 0; i < collection.Count; i++)
            {
                var existingSortKey = GetItemSortKey(collection[i].Value);
                if (Comparer<object>.Default.Compare(sortKey, existingSortKey) < 0)
                    return i;
            }

            return collection.Count;
        }

        private int FindGroupInsertIndex(FilterValueGroup newGroup)
        {
            var sortKey = GetGroupSortKey(newGroup.GroupKey);

            for (int i = 0; i < _allGroups.Count; i++)
            {
                var existingSortKey = GetGroupSortKey(_allGroups[i].GroupKey);
                if (Comparer<object>.Default.Compare(sortKey, existingSortKey) < 0)
                    return i;
            }

            return _allGroups.Count;
        }

        private void ReloadGroups()
        {
            if (cachedValues != null && isLoaded)
            {
                LoadValuesWithCounts(cachedValues, cachedValueCounts);
            }
        }

        public override IEnumerable<object> GetSelectedValues()
        {
            var selectedValues = new List<object>();

            foreach (var group in _allGroups)
            {
                foreach (var child in group.Children.Where(c => c.IsSelected))
                {
                    // Add value based on count
                    for (int i = 0; i < child.ItemCount; i++)
                    {
                        selectedValues.Add(child.Value);
                    }
                }
            }

            return selectedValues.Distinct();
        }
        
        /// <summary>
        /// Gets the selected group-child combinations for proper grouped filtering
        /// </summary>
        /// <returns>List of tuples containing (GroupKey, ChildValue) pairs</returns>
        public IEnumerable<(object GroupKey, object ChildValue)> GetSelectedGroupChildCombinations()
        {
            var combinations = new List<(object, object)>();

            foreach (var group in _allGroups)
            {
                foreach (var child in group.Children.Where(c => c.IsSelected))
                {
                    combinations.Add((group.GroupKey, child.Value));
                }
            }

            return combinations;
        }
        
        /// <summary>
        /// Gets whether this is using grouped filtering (has a GroupByColumn set)
        /// </summary>
        public bool IsGroupedFiltering => !string.IsNullOrEmpty(GroupByColumn);

        public override void SelectAll()
        {
            foreach (var group in _allGroups)
            {
                group.IsSelected = true;
            }
        }

        public override void ClearAll()
        {
            foreach (var group in _allGroups)
            {
                group.IsSelected = false;
            }
        }

        private void UpdateSelectAllState()
        {
            if (_allGroups.Count == 0)
            {
                _selectAllState = false;
            }
            else
            {
                var selectedCount = 0;
                var totalCount = 0;

                foreach (var group in _allGroups)
                {
                    selectedCount += group.GetSelectedChildCount();
                    totalCount += group.GetTotalChildCount();
                }

                if (selectedCount == 0)
                    _selectAllState = false;
                else if (selectedCount == totalCount)
                    _selectAllState = true;
                else
                    _selectAllState = null;
            }
            OnPropertyChanged(nameof(SelectAllState));
        }

        public override string GetSelectionSummary()
        {
            var selectedCount = 0;
            var totalCount = 0;

            foreach (var group in _allGroups)
            {
                selectedCount += group.GetSelectedChildCount();
                totalCount += group.GetTotalChildCount();
            }

            if (selectedCount == 0)
                return "No values selected";
            else if (selectedCount == totalCount)
                return $"All {totalCount} values selected";
            else
                return $"{selectedCount} of {totalCount} values selected";
        }

        private bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }
    }
}