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
    /// Enhanced date tree view filter value view model with proper hierarchy
    /// </summary>
    public class DateTreeViewFilterValueViewModel : FilterValueViewModel
    {
        private readonly ObservableCollection<FilterValueGroup> _yearGroups;
        private readonly Dictionary<int, FilterValueGroup> _yearIndex;
        private readonly Dictionary<string, FilterValueGroup> _monthIndex;

        public ObservableCollection<FilterValueGroup> GroupedValues => _yearGroups;

        public DateTreeViewFilterValueViewModel()
        {
            _yearGroups = new ObservableCollection<FilterValueGroup>();
            _yearIndex = new Dictionary<int, FilterValueGroup>();
            _monthIndex = new Dictionary<string, FilterValueGroup>();
        }

        protected override void LoadValuesInternal(IEnumerable<object> values, Dictionary<object, int> valueCounts)
        {
            GroupedValues.Clear();

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
                    IsSelected = true,
                    ItemCount = 0 // Will calculate from children
                };

                // Group by month within year
                var monthGroups = yearGroup.GroupBy(d => d.Month);

                foreach (var monthGroup in monthGroups)
                {
                    var monthItem = new FilterValueGroup
                    {
                        DisplayValue = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthGroup.Key),
                        IsSelected = true,
                        ItemCount = 0 // Will calculate from children
                    };

                    // Add individual days
                    var dayGroups = monthGroup.GroupBy(d => d.Date);

                    foreach (var dayGroup in dayGroups)
                    {
                        var date = dayGroup.Key;
                        var count = GetSafeValueCount(date, valueCounts);

                        monthItem.Children.Add(new FilterValueItem
                        {
                            Value = date,
                            DisplayValue = date.ToString("dd"),
                            ItemCount = count,
                            IsSelected = true
                        });

                        monthItem.ItemCount += count;
                    }

                    yearItem.Children.Add(monthItem);
                    yearItem.ItemCount += monthItem.ItemCount;
                }

                GroupedValues.Add(yearItem);
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
                            monthGroup.Children.Remove(dayItem);
                        }
                    }
                    else if (isAdd)
                    {
                        // Add new day item in sorted order
                        var newDay = new FilterValueItem
                        {
                            Value = dateValue.Date,
                            DisplayValue = dateValue.ToString("MMM dd, yyyy"),
                            ItemCount = 1,
                            IsSelected = monthGroup.IsSelected ?? false
                        };

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
                    IsSelected = true,
                    ItemCount = 1
                };

                _yearIndex[year] = newYear;

                // Create month and day structure
                var monthGroup = new FilterValueGroup
                {
                    DisplayValue = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    IsSelected = true,
                    ItemCount = 1
                };

                monthGroup.Children.Add(new FilterValueItem
                {
                    Value = dateValue.Date,
                    DisplayValue = dateValue.ToString("MMM dd, yyyy"),
                    ItemCount = 1,
                    IsSelected = true
                });

                newYear.Children.Add(monthGroup);
                _monthIndex[monthKey] = monthGroup;

                // Insert in sorted order
                var yearIndex = 0;
                foreach (var yg in _yearGroups)
                {
                    if (int.Parse(yg.DisplayValue) > year)
                        break;
                    yearIndex++;
                }
                _yearGroups.Insert(yearIndex, newYear);
            }
        }

        public override IEnumerable<object> GetSelectedValues()
        {
            var selectedDates = new List<DateTime>();

            foreach (var year in _yearGroups)
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
            foreach (var year in _yearGroups)
            {
                year.IsSelected = true;
            }
        }

        public override void ClearAll()
        {
            foreach (var year in _yearGroups)
            {
                year.IsSelected = false;
            }
        }

        public override string GetSelectionSummary()
        {
            var selectedCount = 0;
            var totalCount = 0;

            foreach (var year in _yearGroups)
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
    }
}
