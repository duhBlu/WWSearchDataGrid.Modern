using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWPropertyGrid playground: a single demo object (<see cref="Product"/>) whose
    /// public properties the grid reflects, groups by category, and edits through the custom editor
    /// templates declared in the view.
    /// </summary>
    public partial class PropertyGridSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private DemoProduct _product = new DemoProduct();
    }

    public enum ProductStatus
    {
        Draft,
        Active,
        Discontinued,
        BackOrdered,
    }

    public enum Currency
    {
        USD,
        CAD,
        EUR,
        GBP,
    }

    /// <summary>
    /// A plain demo model. Its properties carry the standard metadata attributes the grid reads by
    /// reflection — <c>[Category]</c> groups the rows, <c>[DisplayName]</c> labels them,
    /// <c>[Description]</c> feeds the description panel, and <c>[ReadOnly]</c> blocks edits.
    /// </summary>
    public class DemoProduct : INotifyPropertyChanged
    {
        private string _name = "Shaker Base Cabinet";
        private string _sku = "SBC-2436";
        private bool _isActive = true;
        private decimal _unitPrice = 249.99m;
        private Currency _currency = Currency.USD;
        private int _quantityOnHand = 42;
        private ProductStatus _status = ProductStatus.Active;

        [Category("General")]
        [DisplayName("Product Name")]
        [Description("The display name shown to customers on the order and in catalogs.")]
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [Category("General")]
        [DisplayName("SKU")]
        [Description("Stock-keeping unit — the unique identifier used across ordering and inventory.")]
        public string Sku
        {
            get => _sku;
            set => Set(ref _sku, value);
        }

        [Category("General")]
        [DisplayName("Active")]
        [Description("Whether this product can be added to new orders.")]
        public bool IsActive
        {
            get => _isActive;
            set => Set(ref _isActive, value);
        }

        [Category("Pricing")]
        [DisplayName("Unit Price")]
        [Description("List price per unit before any dealer multiplier or discount.")]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => Set(ref _unitPrice, value);
        }

        [Category("Pricing")]
        [DisplayName("Currency")]
        [Description("Currency the unit price is quoted in.")]
        public Currency Currency
        {
            get => _currency;
            set => Set(ref _currency, value);
        }

        [Category("Inventory")]
        [DisplayName("Quantity On Hand")]
        [Description("Units currently in stock across all warehouses.")]
        public int QuantityOnHand
        {
            get => _quantityOnHand;
            set => Set(ref _quantityOnHand, value);
        }

        [Category("Inventory")]
        [DisplayName("Status")]
        [Description("Lifecycle state of the product.")]
        public ProductStatus Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        [Category("Metadata")]
        [DisplayName("Created On")]
        [Description("When the product record was first created. Read-only.")]
        [ReadOnly(true)]
        public DateTime CreatedOn { get; } = new DateTime(2026, 3, 14);

        [Category("Metadata")]
        [DisplayName("Internal Id")]
        [Description("Immutable internal identifier. Read-only.")]
        [ReadOnly(true)]
        public Guid InternalId { get; } = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
