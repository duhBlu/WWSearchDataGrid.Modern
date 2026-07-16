using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf.Controls.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.PropertyGrid
{
    /// <summary>
    /// Backs the "Dynamic metadata" property-grid sample, showing both live-metadata mechanisms:
    /// (A) a <c>WWPropertyDefinition</c> whose <c>IsReadOnly</c> / <c>IsVisible</c> are bound to this
    /// view model, and (B) a model that implements <see cref="IObservablePropertyMetadataProvider"/>
    /// and signals its own changes. Flipping a toggle updates the grid without reassigning the object.
    /// </summary>
    public partial class PropertyGridDynamicMetadataSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private DeclarativeProduct _declarative = new DeclarativeProduct();

        [ObservableProperty]
        private ProviderProduct _provider = new ProviderProduct();

        // Mechanism A: the definitions bind IsReadOnly / IsVisible to these (the grid propagates its
        // DataContext — this view model — down to each definition).
        [ObservableProperty]
        private bool _isLocked;

        [ObservableProperty]
        private bool _showAdvanced;
    }

    /// <summary>
    /// Mechanism A model — plain. The view's definitions bind the grid's read-only / visibility to
    /// the view model's toggles; the model itself declares nothing dynamic.
    /// </summary>
    public class DeclarativeProduct : INotifyPropertyChanged
    {
        private string _name = "Shaker Base Cabinet";
        private string _sku = "SBC-2436";
        private decimal _unitPrice = 249.99m;
        private decimal _cost = 138.40m;
        private decimal _margin = 0.45m;

        [Category("General")]
        [DisplayName("Product Name")]
        public string Name { get => _name; set => Set(ref _name, value); }

        [Category("General")]
        [DisplayName("SKU")]
        public string Sku { get => _sku; set => Set(ref _sku, value); }

        [Category("Pricing")]
        [DisplayName("Unit Price")]
        public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }

        [Category("Advanced")]
        [DisplayName("Cost")]
        public decimal Cost { get => _cost; set => Set(ref _cost, value); }

        [Category("Advanced")]
        [DisplayName("Margin")]
        public decimal Margin { get => _margin; set => Set(ref _margin, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Mechanism B model — implements <see cref="IObservablePropertyMetadataProvider"/>. Toggling
    /// <see cref="IsLocked"/> raises <see cref="PropertyMetadataChanged"/>, and the grid re-pulls
    /// <see cref="GetPropertyMetadata"/> to make the editable rows read-only live. The lock itself is
    /// <c>[Browsable(false)]</c> so it does not show up as a row.
    /// </summary>
    public class ProviderProduct : INotifyPropertyChanged, IObservablePropertyMetadataProvider
    {
        private string _name = "Wall Cabinet";
        private string _sku = "WC-3030";
        private decimal _unitPrice = 179.99m;
        private bool _isLocked;

        [Category("General")]
        [DisplayName("Product Name")]
        public string Name { get => _name; set => Set(ref _name, value); }

        [Category("General")]
        [DisplayName("SKU")]
        public string Sku { get => _sku; set => Set(ref _sku, value); }

        [Category("Pricing")]
        [DisplayName("Unit Price")]
        public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }

        /// <summary>Not a data row — drives the provider's read-only decision instead.</summary>
        [Browsable(false)]
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked == value) return;
                _isLocked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLocked)));
                // Signal the grid to re-pull metadata for every property.
                PropertyMetadataChanged?.Invoke(this, new PropertyMetadataChangedEventArgs(null));
            }
        }

        public PropertyMetadataOverride? GetPropertyMetadata(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Name):
                case nameof(Sku):
                case nameof(UnitPrice):
                    return new PropertyMetadataOverride { IsReadOnly = IsLocked };
                default:
                    return null;
            }
        }

        public event EventHandler<PropertyMetadataChangedEventArgs>? PropertyMetadataChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
