using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WWSearchDataGrid.Modern.WPF;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.Services;

namespace WWSearchDataGrid.Modern.SampleApp
{
    public partial class MainViewModel : ObservableObject
    {
        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<OrderItem>? _orderItems = new();

        [ObservableProperty]
        private ObservableCollection<ComboBoxItem> _comboBoxItems = new();

        [ObservableProperty]
        private ObservableCollection<string> _comboBoxStringValues = new();

        [ObservableProperty]
        private int _itemCount;

        [ObservableProperty]
        private int _itemsToGenerate = 5000;

        [ObservableProperty]
        private string _currentThemeName = "Generic";

        [ObservableProperty]
        private bool _isGenerating;

        [ObservableProperty]
        private string _generatingStatus = string.Empty;

        private CancellationTokenSource? _generateCts;

        #endregion

        #region Static Data — Patterns from real order data

        private static readonly string[] CustomerNames =
        {
            "Sunrise Kitchen & Bath", "Metro Cabinet Supply", "Valley Home Design", "Premier Woodworks Inc",
            "Coastal Cabinetry LLC", "Highland Custom Homes", "Lakeside Interiors", "Summit Home Solutions",
            "Pacific Remodel Group", "Eastwood Cabinet Co", "Mountain View Kitchens", "Liberty Home Center",
            "Crossroads Building Supply", "Harbor Design Studio", "Ridgeline Construction", "Oakwood Interiors",
            "Greenfield Home Builders", "Pinnacle Renovations", "Stonegate Cabinetry", "Cedar Creek Lumber",
            "Bayview Kitchen & Bath", "Cornerstone Home Design", "Westport Custom Cabinets", "Maple Leaf Interiors",
            "Ironwood Building Co", "Heritage Home Solutions", "Northern Star Millwork", "Southwind Cabinetry",
            "Prairie Home Supply", "Golden Gate Woodworks", "Riverstone Design Group", "Hilltop Cabinet Works"
        };

        private static readonly string[] EmployeeNames =
        {
            "Sarah Mitchell", "James Cooper", "Maria Rodriguez", "David Chen", "Rachel Thompson",
            "Michael Barnes", "Jennifer Walsh", "Robert Kim", "Amanda Foster", "Daniel Morales",
            "Lisa Nguyen", "Christopher Taylor", "Emily Sanders", "Brian O'Neill", "Stephanie Cruz"
        };

        private static readonly string[] SalesReps =
        {
            "Pacific Sales Group LLC", "Mountain West Associates", "Atlantic Coast Partners", "Heartland Sales Co",
            "Great Lakes Distribution", "Southwest Marketing Group", "Northern Alliance Sales", "Coastal Rep Network"
        };

        private static readonly string[] ProductLines = { "Eclipse", "Aspect", "Horizon", "Summit" };
        private static readonly string?[] OrderStatuses = { "Submitted", "In Production", "Shipped", "Delivered", null };
        private static readonly string[] OrderLocations = { "Order Entry", "Production", "Shipping", "Complete" };
        private static readonly string[] OrderTypes = { "Standard", "Replacement Door/Drawer Front", "ASAP", "Sample", "Warranty" };
        private static readonly string[] ShipViaTypes = { "Deliver", "Parcel", "Will Call", "LTL Freight" };

        private static readonly string?[] JobNames =
        {
            "SMITH", "JOHNSON", "WILLIAMS-RES", "MURPHY", "OAK GROVE", "LAKEVIEW", "RIVERSIDE",
            "GARCIA", "ANDERSON", "MARTINEZ", "TAYLOR-REMODEL", "DAVIS", "WILSON", "BROWN-KITCHEN",
            "THOMAS", "JACKSON", "WHITE", "HARRIS", "MARTIN", "THOMPSON", "MOORE-BATH",
            "CLARK", "LEWIS", "ROBINSON", "WALKER", "PEREZ", "HALL", "YOUNG-CUSTOM", null, null
        };

        private static readonly string[] Streets =
        {
            "123 Main St", "4567 Oak Avenue", "890 Industrial Pkwy", "2100 Elm Drive", "750 Cedar Lane",
            "315 Maple Court", "1820 Pine Ridge Rd", "422 Walnut Blvd", "9300 Birch Way", "601 Spruce Circle",
            "1455 Willow St", "280 Hickory Ave", "3200 Cypress Dr", "715 Aspen Terrace", "1100 Poplar Rd"
        };

        private static readonly (string City, string State, string Zip)[] CityStateZips =
        {
            ("Chicago", "IL", "60601"), ("Denver", "CO", "80202"), ("Austin", "TX", "78701"),
            ("Portland", "OR", "97201"), ("Nashville", "TN", "37201"), ("Charlotte", "NC", "28202"),
            ("Omaha", "NE", "68102"), ("Wichita", "KS", "67202"), ("Phoenix", "AZ", "85001"),
            ("Atlanta", "GA", "30301"), ("Indianapolis", "IN", "46201"), ("Columbus", "OH", "43201"),
            ("Milwaukee", "WI", "53201"), ("Kansas City", "MO", "64101"), ("Raleigh", "NC", "27601"),
            ("Tampa", "FL", "33601"), ("Minneapolis", "MN", "55401"), ("St. Louis", "MO", "63101")
        };

        private static readonly string?[] SpecialInstructions =
        {
            null, null, null, null, null, null, null, null, null,
            "Customer requests AM delivery", "Rush order - priority handling",
            "Handle with care", "Leave at loading dock", "Call before delivery",
            "Hold for customer confirmation", "Deliver to job site, not shop address"
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
        private async Task GenerateItemsAsync(object? countParam)
        {
            int count = countParam switch
            {
                int i => i,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => 0
            };
            if (count == 0) return;

            _generateCts?.Cancel();
            _generateCts = new CancellationTokenSource();
            var token = _generateCts.Token;

            IsGenerating = true;

            try
            {
                int batchSize = Math.Max(100, count / 50);
                var existing = OrderItems?.ToList() ?? new List<OrderItem>();
                int remaining = count;
                var baseDate = DateTime.Today.AddYears(-1);
                int startIndex = existing.Count;

                GeneratingStatus = $"Generating 0 / {count:N0}...";

                var generated = new List<OrderItem>(count);

                await Task.Run(() =>
                {
                    while (remaining > 0 && !token.IsCancellationRequested)
                    {
                        int thisBatch = Math.Min(batchSize, remaining);
                        int offset = startIndex + generated.Count;

                        var batch = new OrderItem[thisBatch];
                        Parallel.For(0, thisBatch, i =>
                        {
                            var rnd = new Random(Guid.NewGuid().GetHashCode());
                            batch[i] = CreateOrderItem(rnd, baseDate, offset + i);
                        });

                        generated.AddRange(batch);
                        remaining -= thisBatch;

                        int done = generated.Count;
                        int target = count;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            GeneratingStatus = done < target
                                ? $"Generating {done:N0} / {target:N0}..."
                                : $"Loading {done:N0} items...";
                        });
                    }

                    if (token.IsCancellationRequested) return;

                    existing.AddRange(generated);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OrderItems = new ObservableCollection<OrderItem>(existing);
                        ItemCount = OrderItems.Count;
                        GeneratingStatus = string.Empty;
                    });
                }, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsGenerating = false;
                GeneratingStatus = string.Empty;
            }
        }

        [RelayCommand]
        private void ClearData()
        {
            _generateCts?.Cancel();
            OrderItems = new ObservableCollection<OrderItem>();
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

        [RelayCommand]
        private void LoadSampleData()
        {
            _generateCts?.Cancel();
            var orders = SampleDataLoader.LoadSampleOrders();
            OrderItems = new ObservableCollection<OrderItem>(orders);
            ItemCount = OrderItems.Count;
        }

        #endregion

        #region Private Methods

        private static OrderItem CreateOrderItem(Random rnd, DateTime baseDate, int index)
        {
            var orderDate = baseDate.AddDays(rnd.Next(0, 365)).AddHours(rnd.Next(7, 18)).AddMinutes(rnd.Next(0, 60));
            var statusIdx = rnd.Next(OrderStatuses.Length);
            var status = OrderStatuses[statusIdx];
            var location = status == null ? OrderLocations[0] : OrderLocations[Math.Min(statusIdx, OrderLocations.Length - 1)];
            var cancelled = rnd.NextDouble() < 0.03;
            var submitted = status != null;
            var qty = rnd.Next(1, 50);
            var totalPrice = Math.Round((decimal)(rnd.NextDouble() * 25000) + 100m, 2);
            var amountDue = Math.Round(totalPrice * (decimal)(0.3 + rnd.NextDouble() * 0.5), 2);
            var discount = rnd.NextDouble() < 0.7 ? (decimal?)null : Math.Round((decimal)(rnd.NextDouble() * 15), 0);
            var csz = CityStateZips[rnd.Next(CityStateZips.Length)];
            var customerIdx = rnd.Next(CustomerNames.Length);

            DateTime? scheduleDate = null;
            if (submitted && rnd.NextDouble() < 0.4)
                scheduleDate = orderDate.AddDays(rnd.Next(7, 45));

            return new OrderItem
            {
                OrderHeaderId = 10001 + index,
                OrderNumber = 50001 + index,
                ProductionNumber = submitted && rnd.NextDouble() < 0.3 ? $"PR-{rnd.Next(10000, 99999)}" : null,
                OrderStatusName = cancelled ? "Cancelled" : status,
                OrderLocationName = location,
                OrderDate = orderDate,
                CreateEmployeeId = 1000 + rnd.Next(1, 200),
                EmployeeName = EmployeeNames[rnd.Next(EmployeeNames.Length)],
                CustomerName = CustomerNames[customerIdx],
                DealerNumber = 1000 + customerIdx * 37 % 9000,
                DealerPO = $"PO-{rnd.Next(100000, 999999)}",
                DealerName = CustomerNames[customerIdx],
                JobName = JobNames[rnd.Next(JobNames.Length)],
                ProductLineName = ProductLines[rnd.Next(ProductLines.Length)],
                OrderTypeName = OrderTypes[rnd.Next(OrderTypes.Length)],
                OrderCancelled = cancelled,
                OrderItemsTotalQuantity = qty,
                OrderItemsTotalPrice = totalPrice,
                AmountDue = amountDue,
                DiscountPercent = discount,
                SalesSubRepName = SalesReps[rnd.Next(SalesReps.Length)],
                Address1 = Streets[rnd.Next(Streets.Length)],
                City = csz.City,
                State = csz.State,
                ZipCode = csz.Zip,
                ShipViaTypeName = ShipViaTypes[rnd.Next(ShipViaTypes.Length)],
                SpecialInstructionsText = SpecialInstructions[rnd.Next(SpecialInstructions.Length)],
                LoadNumber = rnd.NextDouble() < 0.85 ? null : rnd.Next(1, 80),
                DropNumber = rnd.NextDouble() < 0.85 ? null : rnd.Next(1, 10),
                ScheduleDate = scheduleDate,
                Submitted = submitted,
                HeaderOptionsXml = null
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
