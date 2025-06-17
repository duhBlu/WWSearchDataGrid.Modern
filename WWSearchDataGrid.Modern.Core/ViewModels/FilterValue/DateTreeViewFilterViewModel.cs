using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.Core.Performance;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced date tree view filter value view model with proper hierarchy and search
    /// </summary>
    public class DateTreeViewFilterValueViewModel : FilterValueViewModel
    {
        private bool? _selectAllState = true;
        private bool _isBulkUpdating = false;
        private readonly ObservableCollection<FilterValueGroup> _allYearGroups;
        private readonly ObservableCollection<FilterValueGroup> _filteredYearGroups;
        private readonly Dictionary<int, FilterValueGroup> _yearIndex;
        private readonly Dictionary<string, FilterValueGroup> _monthIndex;

        public ObservableCollection<FilterValueGroup> GroupedValues => _filteredYearGroups;
        public ObservableCollection<FilterValueGroup> AllGroups => _allYearGroups;

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
                    foreach (var year in _allYearGroups)
                    {
                        year.IsSelected = targetState;
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

        public DateTreeViewFilterValueViewModel()
        {
            _allYearGroups = new ObservableCollection<FilterValueGroup>();
            _filteredYearGroups = new ObservableCollection<FilterValueGroup>();
            _yearIndex = new Dictionary<int, FilterValueGroup>();
            _monthIndex = new Dictionary<string, FilterValueGroup>();
        }

        /// <summary>
        /// Override the base class filter method for date-specific filtering
        /// </summary>
        protected override void ApplyFilter()
        {
            _filteredYearGroups.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // No filter - show all years
                foreach (var year in _allYearGroups)
                {
                    _filteredYearGroups.Add(year);
                }
            }
            else
            {
                foreach (var year in _allYearGroups)
                {
                    // Check if year matches
                    var yearMatches = MatchesSearchText(year.DisplayValue, SearchText);

                    // Check if any months match
                    var matchingMonths = new List<FilterValueGroup>();
                    foreach (var month in year.Children.OfType<FilterValueGroup>())
                    {
                        var monthMatches = MatchesSearchText(month.DisplayValue, SearchText);

                        // Check if any days match (including formatted date strings)
                        var matchingDays = month.Children.Where(day =>
                            MatchesSearchText(day.DisplayValue, SearchText) ||
                            (day.Value is DateTime dt && (
                                MatchesSearchText(dt.ToString("MMM dd, yyyy"), SearchText) ||
                                MatchesSearchText(dt.ToString("MM/dd/yyyy"), SearchText) ||
                                MatchesSearchText(dt.ToString("yyyy-MM-dd"), SearchText)
                            ))
                        ).ToList();

                        if (yearMatches || monthMatches || matchingDays.Any())
                        {
                            // Create filtered month with matching days
                            var filteredMonth = new FilterValueGroup
                            {
                                DisplayValue = month.DisplayValue,
                                GroupKey = month.GroupKey,
                                IsSelected = month.IsSelected,
                                ItemCount = 0
                            };

                            // Add days (all if year/month matches, otherwise only matching days)
                            var daysToAdd = (yearMatches || monthMatches) ? month.Children : new ObservableCollection<FilterValueItem>(matchingDays);
                            foreach (var day in daysToAdd)
                            {
                                filteredMonth.Children.Add(day);
                                filteredMonth.ItemCount += day.ItemCount;
                            }

                            if (filteredMonth.Children.Any())
                            {
                                matchingMonths.Add(filteredMonth);
                            }
                        }
                    }

                    if (matchingMonths.Any())
                    {
                        // Create filtered year
                        var filteredYear = new FilterValueGroup
                        {
                            DisplayValue = year.DisplayValue,
                            GroupKey = year.GroupKey,
                            IsSelected = year.IsSelected,
                            ItemCount = 0
                        };

                        foreach (var month in matchingMonths)
                        {
                            filteredYear.Children.Add(month);
                            filteredYear.ItemCount += month.ItemCount;
                        }

                        _filteredYearGroups.Add(filteredYear);
                    }
                }
            }

            OnPropertyChanged(nameof(GroupedValues));
        }

        protected override void LoadValuesInternal(IEnumerable<object> values, Dictionary<object, int> valueCounts)
        {
            _allYearGroups.Clear();
            _yearIndex.Clear();
            _monthIndex.Clear();

            // Filter to only DateTime values first, avoiding null issues
            var dateValues = values
                .Where(v => v is DateTime)
                .Cast<DateTime>()
                .OrderBy(d => d)
                .ToList();

            if (!dateValues.Any())
                return;

            // Group by year
            var yearGroups = dateValues.GroupBy(d => d.Year);

            foreach (var yearGroup in yearGroups)
            {
                var yearItem = new FilterValueGroup
                {
                    DisplayValue = yearGroup.Key.ToString(),
                    GroupKey = yearGroup.Key,
                    IsSelected = true,
                    ItemCount = 0 // Will calculate from children
                };

                _yearIndex[yearGroup.Key] = yearItem;

                // Group by month within year
                var monthGroups = yearGroup.GroupBy(d => d.Month);

                foreach (var monthGroup in monthGroups)
                {
                    var monthKey = $"{yearGroup.Key}_{monthGroup.Key}";
                    var monthItem = new FilterValueGroup
                    {
                        DisplayValue = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthGroup.Key),
                        GroupKey = monthGroup.Key,
                        IsSelected = true,
                        ItemCount = 0 // Will calculate from children
                    };

                    _monthIndex[monthKey] = monthItem;

                    // Add individual days
                    var dayGroups = monthGroup.GroupBy(d => d.Date);

                    foreach (var dayGroup in dayGroups)
                    {
                        var date = dayGroup.Key;
                        var count = GetSafeValueCount(date, valueCounts);

                        var dayItem = new FilterValueItem
                        {
                            Value = date,
                            DisplayValue = date.ToString("d MMM yyyy"),
                            ItemCount = count,
                            IsSelected = true,
                            Parent = monthItem
                        };

                        dayItem.PropertyChanged += OnChildPropertyChanged;
                        monthItem.Children.Add(dayItem);
                        monthItem.ItemCount += count;
                    }

                    monthItem.PropertyChanged += OnGroupPropertyChanged;
                    yearItem.Children.Add(monthItem);
                    yearItem.ItemCount += monthItem.ItemCount;
                }

                yearItem.PropertyChanged += OnGroupPropertyChanged;
                _allYearGroups.Add(yearItem);
            }

            ApplyFilter();
            UpdateSelectAllState();
        }

        private void OnGroupPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueItem.IsSelected))
            {               
                // Don't update during bulk operations
                if (!_isBulkUpdating)
                {
                    UpdateSelectAllState();
                }
            }
        }

        private void OnChildPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
            }
        }

        public override void LoadValues(IEnumerable<object> values)
        {
            // Create NullSafeDictionary for counting
            var counts = new NullSafeDictionary<object, int>();
            foreach (var value in values)
            {
                if (counts.ContainsKey(value))
                    counts[value]++;
                else
                    counts[value] = 1;
            }

            var dateValues = values.Where(v => v is DateTime).Distinct();
            LoadValuesInternal(dateValues, counts);
        }

        public override void UpdateValueIncremental(object value, bool isAdd)
        {
            if (!(value is DateTime dateValue))
                return;

            var year = dateValue.Year;
            var month = dateValue.Month;
            var monthKey = $"{year}_{month}";

            // Update year
            if (_yearIndex.TryGetValue(year, out var yearGroup))
            {
                yearGroup.ItemCount += isAdd ? 1 : -1;

                // Update month if loaded
                if (_monthIndex.TryGetValue(monthKey, out var monthGroup))
                {
                    monthGroup.ItemCount += isAdd ? 1 : -1;

                    // Update day if loaded
                    var dayItem = monthGroup.Children
                        .OfType<FilterValueItem>()
                        .FirstOrDefault(d => d.Value is DateTime dt && dt.Date == dateValue.Date);

                    if (dayItem != null)
                    {
                        dayItem.ItemCount += isAdd ? 1 : -1;
                        if (dayItem.ItemCount <= 0)
                        {
                            dayItem.PropertyChanged -= OnChildPropertyChanged;
                            monthGroup.Children.Remove(dayItem);
                        }
                    }
                    else if (isAdd)
                    {
                        // Add new day item in sorted order
                        var newDay = new FilterValueItem
                        {
                            Value = dateValue.Date,
                            DisplayValue = dateValue.ToString("d MMM yyyy"),
                            ItemCount = 1,
                            IsSelected = monthGroup.IsSelected ?? false,
                            Parent = monthGroup
                        };

                        newDay.PropertyChanged += OnChildPropertyChanged;

                        var index = 0;
                        foreach (var child in monthGroup.Children.OfType<FilterValueItem>())
                        {
                            if (child.Value is DateTime dt && dt > dateValue)
                                break;
                            index++;
                        }
                        monthGroup.Children.Insert(index, newDay);
                    }
                }
            }
            else if (isAdd)
            {
                // Add new year group
                var newYear = new FilterValueGroup
                {
                    DisplayValue = year.ToString(),
                    GroupKey = year,
                    IsSelected = true,
                    ItemCount = 1
                };

                _yearIndex[year] = newYear;

                // Create month and day structure
                var monthGroup = new FilterValueGroup
                {
                    DisplayValue = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    GroupKey = month,
                    IsSelected = true,
                    ItemCount = 1
                };

                var dayItem = new FilterValueItem
                {
                    Value = dateValue.Date,
                    DisplayValue = dateValue.ToString("d MMM yyyy"),
                    ItemCount = 1,
                    IsSelected = true,
                    Parent = monthGroup
                };

                dayItem.PropertyChanged += OnChildPropertyChanged;
                monthGroup.Children.Add(dayItem);
                monthGroup.PropertyChanged += OnGroupPropertyChanged;
                newYear.Children.Add(monthGroup);
                newYear.PropertyChanged += OnGroupPropertyChanged;

                _monthIndex[monthKey] = monthGroup;

                // Insert in sorted order
                var yearIndex = 0;
                foreach (var yg in _allYearGroups)
                {
                    if (int.Parse(yg.DisplayValue) > year)
                        break;
                    yearIndex++;
                }
                _allYearGroups.Insert(yearIndex, newYear);
            }

            // Reapply filter if there's search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                ApplyFilter();
            }
        }

        public override IEnumerable<object> GetSelectedValues()
        {
            var selectedDates = new List<DateTime>();

            foreach (var year in _allYearGroups)
            {
                if (year.IsSelected == true)
                {
                    AddAllDatesFromGroup(year, selectedDates);
                }
                else if (year.IsSelected == null)
                {
                    foreach (var month in year.Children.OfType<FilterValueGroup>())
                    {
                        if (month.IsSelected == true)
                        {
                            AddAllDatesFromGroup(month, selectedDates);
                        }
                        else if (month.IsSelected == null)
                        {
                            foreach (var day in month.Children)
                            {
                                if (day.IsSelected && day.Value is DateTime date)
                                {
                                    selectedDates.Add(date);
                                }
                            }
                        }
                    }
                }
            }

            return selectedDates.Cast<object>();
        }

        private void AddAllDatesFromGroup(FilterValueGroup group, List<DateTime> dates)
        {
            foreach (var child in group.Children)
            {
                if (child is FilterValueGroup childGroup)
                {
                    AddAllDatesFromGroup(childGroup, dates);
                }
                else if (child.Value is DateTime date)
                {
                    dates.Add(date);
                }
            }
        }

        public override void SelectAll()
        {
            foreach (var year in _allYearGroups)
            {
                year.IsSelected = true;
            }
        }

        public override void ClearAll()
        {
            foreach (var year in _allYearGroups)
            {
                year.IsSelected = false;
            }
        }

        public string SelectionSummary => GetSelectionSummary();

        public override string GetSelectionSummary()
        {
            var selectedCount = 0;
            var totalCount = 0;

            foreach (var year in _allYearGroups)
            {
                selectedCount += year.GetSelectedChildCount();
                totalCount += year.GetTotalChildCount();
            }

            if (selectedCount == totalCount && totalCount > 0)
                return "All dates selected";
            else if (selectedCount == 0)
                return "No dates selected";
            else
                return $"{selectedCount} of {totalCount} dates selected";
        }

        private void UpdateSelectAllState()
        {
            if (_allYearGroups.Count == 0)
            {
                _selectAllState = false;
            }
            else
            {
                var selectedCount = 0;
                var totalCount = 0;

                foreach (var year in _allYearGroups)
                {
                    selectedCount += year.GetSelectedChildCount();
                    totalCount += year.GetTotalChildCount();
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
    }
}