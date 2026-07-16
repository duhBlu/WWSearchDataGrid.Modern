using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.PropertyGrid
{
    /// <summary>
    /// Backs the "Basics" property-grid sample: a plain model whose properties the grid reflects,
    /// groups by category, labels, and edits through auto-resolved typed editors — no per-property
    /// definitions of any kind.
    /// </summary>
    public partial class PropertyGridBasicsSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private BasicsProduct _product = new BasicsProduct();
    }

    public enum BasicsStatus { Draft, Active, Discontinued, BackOrdered }

    public enum BasicsCurrency { USD, CAD, EUR, GBP }

    /// <summary>
    /// A plain model with no property-grid definitions. Each property gets an editor picked purely
    /// from its CLR type — <c>string</c> → text, <c>bool</c> → checkbox, an enum → combo, a numeric
    /// type → a plain text box (the up/down spinner is opt-in, never the default), <c>DateTime</c> →
    /// date, and a type with no natural editor (<c>Guid</c>) → the
    /// read-only placeholder. Metadata comes from DataAnnotations <c>[Display]</c> (name / group /
    /// order / description) and the classic <c>System.ComponentModel</c> attributes, showing both
    /// paths side by side.
    /// </summary>
    public class BasicsProduct : INotifyPropertyChanged
    {
        private string _name = "Shaker Base Cabinet";
        private string _sku = "SBC-2436";
        private bool _isActive = true;
        private decimal _unitPrice = 249.99m;
        private BasicsCurrency _currency = BasicsCurrency.USD;
        private int _quantityOnHand = 42;
        private BasicsStatus _status = BasicsStatus.Active;
        private DateTime _launchDate = new DateTime(2026, 6, 1);

        [Display(Name = "Product Name", GroupName = "General", Order = 1,
            Description = "The display name shown to customers on the order and in catalogs.")]
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [Display(Name = "SKU", GroupName = "General", Order = 2,
            Description = "Stock-keeping unit — the unique identifier used across ordering and inventory.")]
        public string Sku
        {
            get => _sku;
            set => Set(ref _sku, value);
        }

        [Display(Name = "Active", GroupName = "General", Order = 3,
            Description = "Whether this product can be added to new orders.")]
        public bool IsActive
        {
            get => _isActive;
            set => Set(ref _isActive, value);
        }

        // Classic System.ComponentModel attributes — read as the fallback when [Display] is absent.
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
        public BasicsCurrency Currency
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
        public BasicsStatus Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        [Category("Inventory")]
        [DisplayName("Launch Date")]
        [Description("The date the product becomes available to order.")]
        public DateTime LaunchDate
        {
            get => _launchDate;
            set => Set(ref _launchDate, value);
        }

        // No setter → auto read-only; the date editor renders disabled.
        [Category("Metadata")]
        [DisplayName("Created On")]
        [Description("When the product record was first created.")]
        public DateTime CreatedOn { get; } = new DateTime(2026, 3, 14);

        // Guid has no natural editor → the read-only placeholder shows the value as text.
        [Category("Metadata")]
        [DisplayName("Internal Id")]
        [Description("Immutable internal identifier. No built-in editor for this type.")]
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
