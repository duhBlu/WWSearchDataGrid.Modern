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

        /// <summary>
        /// Loads date values from metadata and groups them by year/month/day hierarchy
        /// </summary>
        protected override void LoadValuesFromMetadata(IEnumerable<ValueAggregateMetadata> metadata)
        {
            var metadataList = metadata.ToList();
            
            // Build date hierarchy directly from metadata
            BuildDateHierarchyFromMetadata(metadataList);
            
            ApplyFilter();
            UpdateSelectAllState();
        }

        /// <summary>
        /// Builds the date hierarchy structure directly from metadata
        /// </summary>
        private void BuildDateHierarchyFromMetadata(List<ValueAggregateMetadata> metadataList)
        {
            _allYearGroups.Clear();
            _yearIndex.Clear();
            _monthIndex.Clear();

            // Filter to only DateTime values first, avoiding null issues
            var dateMetadata = metadataList
                .Where(m => m.Value is DateTime)
                .Select(m => new { Date = (DateTime)m.Value, Metadata = m })
                .ToList();

            if (!dateMetadata.Any())
                return;

            // Group by year, then month, then day
            var yearGroups = dateMetadata
                .GroupBy(dm => dm.Date.Year)
                .OrderByDescending(g => g.Key);

            foreach (var yearGroup in yearGroups)
            {
                var yearItem = new FilterValueGroup
                {
                    GroupKey = yearGroup.Key,
                    DisplayValue = yearGroup.Key.ToString(),
                    IsSelected = true,
                    ItemCount = yearGroup.Sum(dm => dm.Metadata.Count)
                };
                yearItem.PropertyChanged += OnGroupPropertyChanged;

                var monthGroups = yearGroup
                    .GroupBy(dm => dm.Date.Month)
                    .OrderByDescending(g => g.Key);

                foreach (var monthGroup in monthGroups)
                {
                    var monthItem = new FilterValueGroup
                    {
                        GroupKey = monthGroup.Key,
                        DisplayValue = GetMonthName(monthGroup.Key),
                        IsSelected = yearItem.IsSelected,
                        ItemCount = monthGroup.Sum(dm => dm.Metadata.Count)
                    };
                    monthItem.PropertyChanged += OnGroupPropertyChanged;

                    // Add individual dates as children
                    var dayItems = monthGroup
                        .OrderByDescending(dm => dm.Date.Day)
                        .Select(dm => new FilterValueItem
                        {
                            Value = dm.Date,
                            DisplayValue = dm.Date.Day.ToString(),
                            ItemCount = dm.Metadata.Count,
                            IsSelected = monthItem.IsSelected ?? false
                        });

                    foreach (var dayItem in dayItems)
                    {
                        dayItem.PropertyChanged += OnChildPropertyChanged;
                        monthItem.Children.Add(dayItem);
                    }

                    yearItem.Children.Add(monthItem);
                    _monthIndex[$"{yearGroup.Key}-{monthGroup.Key}"] = monthItem;
                }

                _allYearGroups.Add(yearItem);
                _yearIndex[yearGroup.Key] = yearItem;
            }
        }

        /// <summary>
        /// Gets the display name for a month number
        /// </summary>
        private string GetMonthName(int monthNumber)
        {
            var monthNames = new[]
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            };
            
            return monthNumber >= 1 && monthNumber <= 12 ? monthNames[monthNumber - 1] : monthNumber.ToString();
        }

        private void OnGroupPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueGroup.IsSelected))
            {
                var group = (FilterValueGroup)sender;

                // if this was a month, also refresh the containing year
                var year = _allYearGroups.FirstOrDefault(y => y.Children.Contains(group));
                if (year != null)
                    year.UpdateGroupSelectionState();

                // update the SelectAll checkbox
                if (!_isBulkUpdating)
                    UpdateSelectAllState();
            }
        }

        private void OnChildPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterValueItem.IsSelected))
            {
                var day = (FilterValueItem)sender;

                // 1) update the month
                var month = day.Parent;
                month.UpdateGroupSelectionState();

                // 2) update the year that contains that month
                var year = _allYearGroups.FirstOrDefault(y => y.Children.OfType<FilterValueGroup>().Contains(month));
                if (year != null)
                    year.UpdateGroupSelectionState();

                // 3) update the SelectAll checkbox
                if (!_isBulkUpdating)
                    UpdateSelectAllState();
            }
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

        public override List<FilterValueItem> GetAllValues()
        {
            var allDates = new List<FilterValueItem>();

            foreach (var year in _allYearGroups)
            {
                foreach (var month in year.Children.OfType<FilterValueGroup>())
                {
                    foreach (var day in month.Children.OfType<FilterValueItem>())
                    {
                        allDates.Add(day);
                    }
                }
            }

            return allDates;
        }

        public override List<FilterValueItem> GetUnselectedValues()
        {
            var unselectedDates = new List<FilterValueItem>();

            foreach (var year in _allYearGroups)
            {
                foreach (var month in year.Children.OfType<FilterValueGroup>())
                {
                    foreach (var day in month.Children.OfType<FilterValueItem>())
                    {
                        if (!day.IsSelected)
                        {
                            unselectedDates.Add(day);
                        }
                    }
                }
            }

            return unselectedDates;
        }
    }
}