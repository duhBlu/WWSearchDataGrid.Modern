using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.WPF;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.Services;

namespace WWSearchDataGrid.Modern.SampleApp
{
    public partial class MainViewModel : ObservableObject
    {
        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<DataItem>? _items = new();

        [ObservableProperty]
        private ObservableCollection<ComboBoxItem> _comboBoxItems = new();

        [ObservableProperty]
        private ObservableCollection<string> _comboBoxStringValues = new();

        [ObservableProperty]
        private DataItem? _selectedItem;

        [ObservableProperty]
        private int _itemCount;

        [ObservableProperty]
        private int _itemsToGenerate = 5000;

        [ObservableProperty]
        private string _currentThemeName = "Generic";

        #endregion

        #region Static Data

        private static readonly string[] FirstNames =
        {
            "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack",
            "Kate", "Leo", "Mary", "Nick", "Olivia", "Paul", "Quinn", "Rita", "Sam", "Tina",
            "Uma", "Victor", "Wendy", "Xavier", "Yara", "Zack", "Anna", "Ben", "Chloe", "Dan",
            "Emma", "Felix", "Gina", "Hugo", "Iris", "Jake", "Kara", "Liam", "Mia", "Noah"
        };

        private static readonly string[] LastNames =
        {
            "Anderson", "Brown", "Clark", "Davis", "Evans", "Fisher", "Garcia", "Harris", "Jackson", "Johnson",
            "King", "Lopez", "Martinez", "Nelson", "Parker", "Quinn", "Rodriguez", "Smith", "Taylor", "Wilson",
            "Adams", "Baker", "Cooper", "Dixon", "Edwards", "Ford", "Green", "Hall", "Jones", "Lewis"
        };

        private static readonly string[] Products =
        {
            "Widget A", "Widget B", "Gadget Pro", "Gadget Lite", "Sensor Module", "Control Board",
            "Power Supply 12V", "Power Supply 24V", "Cable Assembly", "Mounting Bracket",
            "LED Panel", "Display Unit", "Motor 1HP", "Motor 2HP", "Relay Switch",
            "Fuse Block", "Terminal Strip", "Connector Kit", "Enclosure Small", "Enclosure Large"
        };

        private static readonly string[] Categories =
        {
            "Electronics", "Mechanical", "Electrical", "Accessories", "Assemblies"
        };

        private static readonly string?[] NoteTemplates =
        {
            null, null, null, // weighted null for ~60% null notes
            null, null, null,
            "Customer requested expedited shipping",
            "Backordered - ETA 2 weeks",
            "Replacement for defective unit",
            "Bulk discount applied",
            "Hold for customer confirmation",
            "Special packaging required",
            "Per contract #4521",
            "Drop ship to job site"
        };

        #endregion

        #region Constructor

        public MainViewModel()
        {
            InitializeComboBoxItems();
            InitializeComboBoxStringValues();
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void PopulateData()
        {
            GenerateData(ItemsToGenerate);
            ItemCount = Items?.Count ?? 0;
        }

        [RelayCommand]
        private void AddItem()
        {
            Items?.Add(CreateDataItem(new Random()));
            ItemCount = Items?.Count ?? 0;
        }

        [RelayCommand]
        private void RemoveItem()
        {
            if (Items?.Count > 0)
            {
                Items.RemoveAt(Items.Count - 1);
                ItemCount = Items?.Count ?? 0;
            }
        }

        [RelayCommand]
        private void ClearData()
        {
            Items?.Clear();
            ItemCount = 0;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            var currentTheme = ThemeManager.Instance.CurrentTheme;
            var newTheme = currentTheme == ThemeType.Custom ? ThemeType.Generic : ThemeType.Custom;

            ThemeManager.Instance.SwitchTheme(newTheme);
            CurrentThemeName = newTheme.ToString();
        }

        #endregion

        #region Private Methods

        private void GenerateData(int count)
        {
            var baseDate = DateTime.Today.AddYears(-1);
            var buffer = new DataItem[count];

            Parallel.For(0, count, i =>
            {
                var rnd = new Random(Guid.NewGuid().GetHashCode());
                buffer[i] = CreateDataItem(rnd, baseDate, 1000 + i);
            });

            var merged = new ObservableCollection<DataItem>((Items ?? new ObservableCollection<DataItem>()).Concat(buffer));
            Items = null;
            Items = merged;
            ItemCount = Items.Count;
        }

        private DataItem CreateDataItem(Random rnd, DateTime? baseDate = null, int index = 0)
        {
            baseDate ??= DateTime.Today.AddYears(-1);
            var orderDate = baseDate.Value.AddDays(rnd.Next(0, 365)).AddHours(rnd.Next(7, 18)).AddMinutes(rnd.Next(0, 60));
            var status = (OrderStatus)rnd.Next(Enum.GetValues<OrderStatus>().Length);
            var priority = (Priority)rnd.Next(Enum.GetValues<Priority>().Length);
            var qty = rnd.Next(1, 200);
            var unitPrice = Math.Round((decimal)(rnd.NextDouble() * 499.99) + 0.50m, 4);
            var discount = rnd.NextDouble() < 0.3 ? (decimal?)Math.Round((decimal)(rnd.NextDouble() * 0.25), 4) : null;
            var lineTotal = Math.Round(qty * unitPrice * (1m - (discount ?? 0m)), 2);

            // Ship date: only for shipped/delivered/completed orders, some days after order date
            DateTime? shipDate = null;
            if (status == OrderStatus.Shipped || status == OrderStatus.Delivered || status == OrderStatus.Completed)
                shipDate = orderDate.AddDays(rnd.Next(1, 14));

            return new DataItem
            {
                OrderNumber = $"ORD-{(index > 0 ? index : rnd.Next(1000, 99999)):D5}",
                CustomerName = $"{FirstNames[rnd.Next(FirstNames.Length)]} {LastNames[rnd.Next(LastNames.Length)]}",
                PhoneNumber = $"{rnd.Next(200, 999)}{rnd.Next(1000000, 9999999)}",
                ProductName = Products[rnd.Next(Products.Length)],
                Category = Categories[rnd.Next(Categories.Length)],
                Quantity = qty,
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                Discount = discount,
                Status = status,
                Priority = priority,
                IsRush = priority >= Priority.Urgent || rnd.NextDouble() < 0.1,
                IsApproved = rnd.NextDouble() < 0.15 ? null : (bool?)(rnd.NextDouble() < 0.8),
                OrderDate = orderDate,
                ShipDate = shipDate,
                Notes = NoteTemplates[rnd.Next(NoteTemplates.Length)]
            };
        }

        private void InitializeComboBoxItems()
        {
            ComboBoxItems =
            [
                new ComboBoxItem { Id = 1, Name = "Option 1" },
                new ComboBoxItem { Id = 2, Name = "Option 2" },
                new ComboBoxItem { Id = 3, Name = "Option 3" },
                new ComboBoxItem { Id = 4, Name = "Option 4" },
                new ComboBoxItem { Id = 5, Name = "Option 5" }
            ];
        }

        private void InitializeComboBoxStringValues()
        {
            ComboBoxStringValues = new ObservableCollection<string>
            {
                "String Option 1",
                "String Option 2",
                "String Option 3",
                "String Option 4",
                "String Option 5"
            };
        }

        #endregion
    }
}
