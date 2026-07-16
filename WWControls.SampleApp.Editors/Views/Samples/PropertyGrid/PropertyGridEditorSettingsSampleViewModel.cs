using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.PropertyGrid
{
    /// <summary>
    /// Backs the "Editor settings" property-grid sample: the same <c>BaseEditorSettings</c> family
    /// the SearchDataGrid uses for its cells, attached per property through
    /// <c>WWPropertyGrid.PropertyDefinitions</c> — a numeric/currency mask, a bound combo, a
    /// bounded spinner, and a date editor.
    /// </summary>
    public partial class PropertyGridEditorSettingsSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private SettingsProduct _product = new SettingsProduct();

        /// <summary>Bound to the combo editor's ItemsSource via the definition's EditSettings — the
        /// binding resolves against this view model because the grid propagates its DataContext down
        /// to each definition (and each definition to its EditSettings).</summary>
        public IReadOnlyList<string> Warehouses { get; } =
            new[] { "Seattle", "Denver", "Atlanta", "Toronto" };
    }

    /// <summary>
    /// A model edited entirely through per-property <c>EditSettings</c>. The properties are plain;
    /// the mask, item source, bounds, and increment all come from the definitions in the view.
    /// </summary>
    public class SettingsProduct : INotifyPropertyChanged
    {
        private decimal _unitPrice = 249.99m;
        private string _contactPhone = "2065551234";
        private int _quantityOnHand = 42;
        private string _warehouse = "Seattle";
        private System.DateTime _reorderDate = new System.DateTime(2026, 8, 15);

        [Category("Pricing")]
        [DisplayName("Unit Price")]
        [Description("Edited through a TextBoxSettings currency mask (C2) that also formats the display.")]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => Set(ref _unitPrice, value);
        }

        [Category("Contact")]
        [DisplayName("Contact Phone")]
        [Description("Edited through a TextBoxSettings Simple mask — (000) 000-0000.")]
        public string ContactPhone
        {
            get => _contactPhone;
            set => Set(ref _contactPhone, value);
        }

        [Category("Inventory")]
        [DisplayName("Quantity On Hand")]
        [Description("Edited through a NumericUpDownSettings with Minimum 0, Maximum 1000, Increment 5.")]
        public int QuantityOnHand
        {
            get => _quantityOnHand;
            set => Set(ref _quantityOnHand, value);
        }

        [Category("Inventory")]
        [DisplayName("Warehouse")]
        [Description("Edited through a ComboBoxSettings whose ItemsSource is bound to the view model.")]
        public string Warehouse
        {
            get => _warehouse;
            set => Set(ref _warehouse, value);
        }

        [Category("Inventory")]
        [DisplayName("Reorder Date")]
        [Description("Edited through a DatePickerSettings.")]
        public System.DateTime ReorderDate
        {
            get => _reorderDate;
            set => Set(ref _reorderDate, value);
        }

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
