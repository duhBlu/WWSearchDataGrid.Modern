using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.WPF;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.Services;

namespace WWSearchDataGrid.Modern.SampleApp
{
    /// <summary>
    /// Main ViewModel for the application
    /// </summary>
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
        private DataItem _selectedItem;

        [ObservableProperty]
        private int _itemCount;

        [ObservableProperty]
        private int _itemsToGenerate = 5000;

        [ObservableProperty]
        private string _currentThemeName = "Generic";

        // Reference to the SearchDataGrid for memory cleanup operations
        private SearchDataGrid _searchDataGrid;

        #endregion

        #region Static Data (Preallocated)

        private static readonly Random _random = new Random();
        private static readonly string[] FirstNames =
        {
            "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack",
            "Kate", "Leo", "Mary", "Nick", "Olivia", "Paul", "Quinn", "Rita", "Sam", "Tina",
            "Uma", "Victor", "Wendy", "Xavier", "Yara", "Zack", "Anna", "Ben", "Chloe", "Dan",
            "Emma", "Felix", "Gina", "Hugo", "Iris", "Jake", "Kara", "Liam", "Mia", "Noah",
            "Ava", "Blake", "Clara", "Dean", "Ella", "Finn", "Hailey", "Ian", "Jade", "Kyle"
        };
        private static readonly string[] LastNames =
        {
            "Anderson", "Brown", "Clark", "Davis", "Evans", "Fisher", "Garcia", "Harris", "Jackson", "Johnson",
            "King", "Lopez", "Martinez", "Nelson", "O'Connor", "Parker", "Quinn", "Rodriguez", "Smith", "Taylor",
            "Underwood", "Valdez", "Wilson", "Young", "Zhang", "Adams", "Baker", "Cooper", "Dixon", "Edwards",
            "Ford", "Green", "Hall", "Irving", "Jones", "Kelly", "Lewis", "Moore", "Nixon", "Owen",
            "Powell", "Reed", "Stone", "Thomas", "White", "Allen", "Bell", "Carter", "Foster", "Gray"
        };
        private static readonly string[] StringTypes =
        {
            "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
            "Product", "Service", "Item", "Component", "Module", "System", "Process",
            null, "", " ", "test", "TEST", "Test", "123", "abc123", "test@example.com"
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
            ItemCount = Items.Count;
        }

        [RelayCommand]
        private void AddItem()
        {
            Items.Add(CreateComprehensiveDataItem(_random));
            ItemCount = Items.Count;
        }

        [RelayCommand]
        private void RemoveItem()
        {
            if (Items.Count > 0)
            {
                Items.RemoveAt(Items.Count - 1);
                ItemCount = Items.Count;
            }
        }

        [RelayCommand]
        private void ClearData()
        {
            Items.Clear();
            ItemCount = 0;

            // Clear cached data in the SearchDataGrid to prevent memory leaks
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
        
        #region Public Methods
        
        /// <summary>
        /// Sets the SearchDataGrid reference for memory cleanup operations
        /// This should be called from the code-behind after the grid is initialized
        /// </summary>
        /// <param name="searchDataGrid">The SearchDataGrid instance</param>
        public void SetSearchDataGrid(SearchDataGrid searchDataGrid)
        {
            _searchDataGrid = searchDataGrid;
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// Generates sample data for the grid (optimized & fast)
        /// </summary>
        private void GenerateData(int count)
        {
            var baseDate = DateTime.Today.AddYears(-2);
            var buffer = new DataItem[count];

                Parallel.For(0, count, i =>
                {
                    var rnd = new Random(Guid.NewGuid().GetHashCode());
                    buffer[i] = CreateComprehensiveDataItem(rnd, baseDate);
                });

            var merged = new ObservableCollection<DataItem>(Items.Concat(buffer));
            Items = null;
            Items = merged; 
            ItemCount = Items.Count;
        }


        /// <summary>
        /// Creates a randomized DataItem
        /// </summary>
        private DataItem CreateComprehensiveDataItem(Random rnd, DateTime? baseDate = null)
        {
            baseDate ??= DateTime.Today.AddYears(-2);
            var orderDateTime = baseDate.Value.AddDays(rnd.Next(0, 730)).AddHours(rnd.Next(8, 18)).AddMinutes(rnd.Next(0, 60));
            var statusValue = (OrderStatus)rnd.Next(Enum.GetValues<OrderStatus>().Length);
            var priorityValue = (Priority)rnd.Next(Enum.GetValues<Priority>().Length);

            return new DataItem
            {
                CustomerName = $"{FirstNames[rnd.Next(FirstNames.Length)]} {LastNames[rnd.Next(LastNames.Length)]}",
                BoolValue = rnd.NextDouble() < 0.7,
                NullableBoolValue = rnd.NextDouble() < 0.1 ? null : (bool?)(rnd.NextDouble() < 0.6),
                IntValue = rnd.Next(1, 100000),
                NullableIntValue = rnd.NextDouble() < 0.15 ? null : rnd.Next(1, 50000),
                LongValue = rnd.NextInt64(1000000, 9999999999),
                NullableLongValue = rnd.NextDouble() < 0.12 ? null : rnd.NextInt64(1000, 999999),
                ShortValue = (short)rnd.Next(1, 32767),
                NullableShortValue = rnd.NextDouble() < 0.18 ? null : (short?)rnd.Next(1, 1000),
                ByteValue = (byte)rnd.Next(0, 255),
                NullableByteValue = rnd.NextDouble() < 0.20 ? null : (byte?)rnd.Next(0, 100),
                FloatValue = (float)(rnd.NextDouble() * 10000),
                NullableFloatValue = rnd.NextDouble() < 0.14 ? null : (float?)(rnd.NextDouble() * 1000),
                DoubleValue = rnd.NextDouble() * 100000,
                NullableDoubleValue = rnd.NextDouble() < 0.16 ? null : (double?)(rnd.NextDouble() * 10000),
                DecimalValue = (decimal)(rnd.NextDouble() * 50000),
                NullableDecimalValue = rnd.NextDouble() < 0.13 ? null : (decimal?)(rnd.NextDouble() * 5000),
                StringValue = StringTypes[rnd.Next(StringTypes.Length)],
                CharValue = (char)rnd.Next(65, 91),
                NullableCharValue = rnd.NextDouble() < 0.25 ? null : (char?)((char)rnd.Next(97, 123)),
                DateTimeValue = orderDateTime.AddDays(rnd.Next(-30, 30)),
                NullableDateTimeValue = rnd.NextDouble() < 0.17 ? null : (DateTime?)orderDateTime.AddHours(rnd.Next(-100, 100)),
                TimeSpanValue = TimeSpan.FromMinutes(rnd.Next(30, 480)),
                NullableTimeSpanValue = rnd.NextDouble() < 0.19 ? null : (TimeSpan?)TimeSpan.FromHours(rnd.NextDouble() * 24),
                GuidValue = Guid.NewGuid(),
                NullableGuidValue = rnd.NextDouble() < 0.22 ? null : (Guid?)Guid.NewGuid(),
                PriorityValue = priorityValue,
                NullablePriorityValue = rnd.NextDouble() < 0.23 ? null : (Priority?)priorityValue,
                IsActiveTemplate = rnd.NextDouble() < 0.65,
                CurrencyTemplate = (decimal)(rnd.NextDouble() * 50000),
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
