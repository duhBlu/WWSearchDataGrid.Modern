using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.WPF;

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
                Items.Add(CreateDataItem(i));
            }
        }

        /// <summary>
        /// Creates a data item with test values
        /// </summary>
        /// <param name="index">Index to use for generating values</param>
        /// <returns>A new data item</returns>
        private DataItem CreateDataItem(int index)
        {
            // Reuse the same pattern so that “random” is deterministic per index:
            var random = new Random(index);
            var baseIndex = index % 50; // forces a repeat every 50 rows

            // --- existing pools ---
            var stringPool = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry", null, "", "apple", "banana" };
            var floatSet = new[] { 100f, 200f, 300f, 400f, 500f, 600f, 700f };
            var doubleSet = new[] { 1.01, 1.02, 2.05, 2.06, 3.00 };
            var decimalSet = new[] { 107.005m, 108.005m, 109.005m, 107.005m }; // duplicates
            var comboOptions = ComboBoxItems.Select(c => c.Id).ToList();
            var stringComboOptions = ComboBoxStringValues.ToList();
            var today = DateTime.Today;

            // 1) Product names (so every row gets a recognizable product)
            var productNames = new[]
            {
                "Laptop", "Smartphone", "Tablet", "Headphones", "Monitor",
                "Keyboard", "Mouse", "Printer", "Webcam", "External HDD"
            };

            // 2) Categories (will repeat every few rows => grouping)
            var categories = new[]
            {
                "Electronics", "Accessories", "Office Supplies",
                "Gaming", "Home Appliances"
            };

            // 3) Currency codes (small set => duplicates)
            var currencyCodes = new[] { "USD", "EUR", "GBP", "JPY", "CAD" };

            // 4) Regions (duplicates periodically)
            var regions = new[]
            {
                "North America", "Europe", "Asia", "South America", "Australia"
            };

            // 5) Order statuses (duplicates frequently)
            var statuses = new[] { "Pending", "Shipped", "Delivered", "Cancelled" };

            // Now assemble the DataItem:
            return new DataItem
            {
                // --- existing fields ---
                BoolValue = index % 3 == 0,
                NullableBoolValue = index % 7 == 0 ? null : (bool?)(index % 2 == 0),

                IntValue = index,
                NullableIntValue = index % 13 == 0 ? null : (int?)(baseIndex),

                LongValue = index * 100_000L,

                FloatValue = floatSet[index % floatSet.Length],
                NullableFloatValue = index % 11 == 0 ? null : (float?)(floatSet[baseIndex % floatSet.Length]),

                DoubleValue = doubleSet[index % doubleSet.Length],
                NullableDoubleValue = index % 9 == 0 ? null : (double?)(doubleSet[baseIndex % doubleSet.Length]),

                DecimalValue = decimalSet[index % decimalSet.Length],
                NullableDecimalValue = index % 6 == 0 ? null : (decimal?)(decimalSet[baseIndex % decimalSet.Length]),

                StringValue = stringPool[index % stringPool.Length],

                DateTimeValue = today.AddDays(-(index % 365)),
                NullableDateTimeValue = index % 10 == 0 ? null : (DateTime?)(today.AddDays(-(baseIndex % 30))),

                ComboBoxValueId = comboOptions[index % comboOptions.Count],
                SelectedComboBoxStringValue = stringComboOptions[index % stringComboOptions.Count],

                PropertyValues = new List<Tuple<string, string>>
                {
                    Tuple.Create("KeyA", index % 5 == 0 ? null : $"Val{baseIndex % 3}"),
                    Tuple.Create("KeyB", $"Val{(index + 1) % 5}"),
                    Tuple.Create("KeyC", $"Val{(index + 2) % 4}")
                },
                        PropertyDictionary = new Dictionary<string, object>
                {
                    { "DictKeyA", baseIndex },
                    { "DictKeyB", today.AddDays(- (index % 90)) },
                    { "DictKeyC", stringPool[(index + 3) % stringPool.Length] },
                    { "DictKeyD", index % 2 == 0 ? (object)"SetA" : "SetB" }
                },

                // --- NEW realistic/groupable fields ---
                ProductName = productNames[index % productNames.Length],
                Category = categories[baseIndex % categories.Length],
                CurrencyCode = currencyCodes[baseIndex % currencyCodes.Length],

                // Price: anywhere from $5.00 to $999.99, deterministic via Random(index)
                Price = (decimal)(random.Next(500, 100_000)) / 100m,

                Quantity = random.Next(1, 101),                        // 1–100

                // OrderDate: pick any day in the past 365 days
                OrderDate = today.AddDays(-random.Next(0, 365)),

                Region = regions[index % regions.Length],
                Status = statuses[index % statuses.Length]
            };
        }


        /// <summary>
        /// Generates a random data item
        /// </summary>
        /// <returns>A new random data item</returns>
        private DataItem GenerateRandomDataItem()
        {
            var random = new Random();
            int index = random.Next(1, 1000);
            return CreateDataItem(index);
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

        #endregion
    }
}
