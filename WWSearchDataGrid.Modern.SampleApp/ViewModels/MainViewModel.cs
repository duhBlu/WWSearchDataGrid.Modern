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

namespace WWSearchDataGrid.Modern.SampleApp
{
    /// <summary>
    /// Main ViewModel for the application
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        #region Observable Properties

        /// <summary>
        /// Collection of data items
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<DataItem> _items = new();

        /// <summary>
        /// Collection of combo box items
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ComboBoxItem> _comboBoxItems = new();

        /// <summary>
        /// Collection of string values for combo boxes
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _comboBoxStringValues = new();

        /// <summary>
        /// Selected data item
        /// </summary>
        [ObservableProperty]
        private DataItem _selectedItem;

        /// <summary>
        /// Count of items in the collection
        /// </summary>
        [ObservableProperty]
        private int _itemCount;

        /// <summary>
        /// Count of items in the collection
        /// </summary>
        [ObservableProperty]
        private int _itemsToGenerate = 5000;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class
        /// </summary>
        public MainViewModel()
        {
            InitializeComboBoxItems();
            InitializeComboBoxStringValues();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to populate the data grid with sample data
        /// </summary>
        [RelayCommand]
        private void PopulateData()
        {
            GenerateData(ItemsToGenerate);
            ItemCount = Items.Count;
        }

        /// <summary>
        /// Command to add a single data item
        /// </summary>
        [RelayCommand]
        private void AddItem()
        {
            Items.Add(GenerateRandomDataItem());
            ItemCount = Items.Count;
            OnPropertyChanged(nameof(Items));
        }

        /// <summary>
        /// Command to remove the last data item
        /// </summary>
        [RelayCommand]
        private void RemoveItem()
        {
            if (Items.Count > 0)
            {
                Items.RemoveAt(Items.Count - 1);
                ItemCount = Items.Count;
            }
        }

        /// <summary>
        /// Command to clear all data
        /// </summary>
        [RelayCommand]
        private void ClearData()
        {
            Items.Clear();
            ItemCount = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates sample data for the grid
        /// </summary>
        /// <param name="count">Number of items to generate</param>
        private void GenerateData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Items.Add(CreateComprehensiveDataItem(i));
            }
        }

        /// <summary>
        /// Generates a random data item
        /// </summary>
        /// <returns>A new random data item</returns>
        private DataItem GenerateRandomDataItem()
        {
            var random = new Random();
            int index = random.Next(1, 1000);
            return CreateComprehensiveDataItem(index);
        }

        /// <summary>
        /// Initializes combo box items
        /// </summary>
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

        /// <summary>
        /// Initializes combo box string values
        /// </summary>
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

        /// <summary>
        /// Creates a data item with comprehensive test values covering all data types
        /// </summary>
        /// <param name="index">Index to use for generating values</param>
        /// <returns>A new data item</returns>
        private DataItem CreateComprehensiveDataItem(int index)
        {
            var random = new Random(index);
            var today = DateTime.Today;
            var baseDate = today.AddYears(-2);

            // Customer Names - 2500+ combinations (50 first × 50 last)
            var firstNames = new[]
            {
                "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack",
                "Kate", "Leo", "Mary", "Nick", "Olivia", "Paul", "Quinn", "Rita", "Sam", "Tina",
                "Uma", "Victor", "Wendy", "Xavier", "Yara", "Zack", "Anna", "Ben", "Chloe", "Dan",
                "Emma", "Felix", "Gina", "Hugo", "Iris", "Jake", "Kara", "Liam", "Mia", "Noah",
                "Ava", "Blake", "Clara", "Dean", "Ella", "Finn", "Hailey", "Ian", "Jade", "Kyle"
            };
            var lastNames = new[]
            {
                "Anderson", "Brown", "Clark", "Davis", "Evans", "Fisher", "Garcia", "Harris", "Jackson", "Johnson",
                "King", "Lopez", "Martinez", "Nelson", "O'Connor", "Parker", "Quinn", "Rodriguez", "Smith", "Taylor",
                "Underwood", "Valdez", "Wilson", "Young", "Zhang", "Adams", "Baker", "Cooper", "Dixon", "Edwards",
                "Ford", "Green", "Hall", "Irving", "Jones", "Kelly", "Lewis", "Moore", "Nixon", "Owen",
                "Powell", "Reed", "Stone", "Thomas", "White", "Allen", "Bell", "Carter", "Foster", "Gray"
            };

            // Product Categories - 15-20 distinct values
            var categories = new[]
            {
                "Electronics", "Computers", "Mobile Devices", "Audio Equipment", "Gaming",
                "Home Appliances", "Kitchen & Dining", "Office Supplies", "Furniture", "Sports & Outdoors",
                "Health & Beauty", "Automotive", "Books & Media", "Clothing", "Jewelry",
                "Tools & Hardware", "Garden & Patio", "Pet Supplies", "Baby & Kids", "Travel Accessories"
            };

            // Regions - 8 geographic regions
            var regions = new[]
            {
                "North America", "South America", "Europe", "Asia Pacific", 
                "Middle East", "Africa", "Central Asia", "Oceania"
            };

            // Generate comprehensive data
            var customerName = $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
            var orderDateTime = baseDate.AddDays(random.Next(0, 730)).AddHours(random.Next(8, 18)).AddMinutes(random.Next(0, 60));
            var statusValue = (OrderStatus)random.Next(0, Enum.GetValues<OrderStatus>().Length);
            var priorityValue = (Priority)random.Next(0, Enum.GetValues<Priority>().Length);

            return new DataItem
            {
                // Essential business context
                CustomerName = customerName,
                ProductCategory = categories[random.Next(categories.Length)],
                Region = regions[random.Next(regions.Length)],

                // Boolean types - weighted distributions
                BoolValue = random.NextDouble() < 0.7,
                NullableBoolValue = random.NextDouble() < 0.1 ? null : (bool?)(random.NextDouble() < 0.6),

                // Integer types - varied ranges
                IntValue = random.Next(1, 100000),
                NullableIntValue = random.NextDouble() < 0.15 ? null : (int?)random.Next(1, 50000),
                LongValue = random.NextInt64(1000000, 9999999999),
                NullableLongValue = random.NextDouble() < 0.12 ? null : (long?)random.NextInt64(1000, 999999),
                ShortValue = (short)random.Next(1, 32767),
                NullableShortValue = random.NextDouble() < 0.18 ? null : (short?)random.Next(1, 1000),
                ByteValue = (byte)random.Next(0, 255),
                NullableByteValue = random.NextDouble() < 0.20 ? null : (byte?)random.Next(0, 100),

                // Floating-point and decimal types
                FloatValue = (float)(random.NextDouble() * 10000),
                NullableFloatValue = random.NextDouble() < 0.14 ? null : (float?)(random.NextDouble() * 1000),
                DoubleValue = random.NextDouble() * 100000,
                NullableDoubleValue = random.NextDouble() < 0.16 ? null : (double?)(random.NextDouble() * 10000),
                DecimalValue = (decimal)(random.NextDouble() * 50000),
                NullableDecimalValue = random.NextDouble() < 0.13 ? null : (decimal?)(random.NextDouble() * 5000),

                // Text types
                StringValue = GenerateVariedString(random),
                CharValue = (char)random.Next(65, 91), // A-Z
                NullableCharValue = random.NextDouble() < 0.25 ? null : (char?)((char)random.Next(97, 123)), // a-z

                // Date and time types with precision
                DateTimeValue = orderDateTime.AddDays(random.Next(-30, 30)),
                NullableDateTimeValue = random.NextDouble() < 0.17 ? null : (DateTime?)orderDateTime.AddHours(random.Next(-100, 100)),
                TimeSpanValue = TimeSpan.FromMinutes(random.Next(30, 480)),
                NullableTimeSpanValue = random.NextDouble() < 0.19 ? null : (TimeSpan?)TimeSpan.FromHours(random.NextDouble() * 24),

                // GUID types
                GuidValue = Guid.NewGuid(),
                NullableGuidValue = random.NextDouble() < 0.22 ? null : (Guid?)Guid.NewGuid(),

                // Enum types
                StatusValue = statusValue,
                NullableStatusValue = random.NextDouble() < 0.21 ? null : (OrderStatus?)statusValue,
                PriorityValue = priorityValue,
                NullablePriorityValue = random.NextDouble() < 0.23 ? null : (Priority?)priorityValue,

                // Business datetimes with time precision
                OrderDateTime = orderDateTime,
                ShippedDateTime = statusValue >= OrderStatus.Shipped ? (DateTime?)orderDateTime.AddDays(random.Next(1, 7)) : null,
                DueDateTime = orderDateTime.AddDays(random.Next(7, 30)).AddHours(random.Next(8, 18)),
                CompletedDateTime = statusValue == OrderStatus.Completed ? (DateTime?)orderDateTime.AddDays(random.Next(3, 14)) : null,
                ProcessingTime = TimeSpan.FromMinutes(random.Next(15, 300)),
                DeliveryTime = statusValue >= OrderStatus.Delivered ? (TimeSpan?)TimeSpan.FromDays(random.Next(1, 10)) : null,
            };
        }

        /// <summary>
        /// Generates varied string values for testing
        /// </summary>
        private string GenerateVariedString(Random random)
        {
            var stringTypes = new[]
            {
                // Regular strings
                "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
                "Product", "Service", "Item", "Component", "Module", "System", "Process",
                // Special cases
                null, "", " ", "test", "TEST", "Test", "123", "abc123", "test@example.com"
            };
            
            return stringTypes[random.Next(stringTypes.Length)];
        }

        #endregion
    }
}
