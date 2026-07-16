using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf.Controls.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWComboBox playground: one shared set of feature toggles (incremental
    /// filtering + mode, auto-complete, ShowNone, size grip, open-on-focus, radio glyphs,
    /// templated selection box) applied to three live combos — non-editable, editable, and
    /// checkbox multi-select.
    /// </summary>
    public partial class ComboBoxSampleViewModel : ObservableObject
    {
        // Feature toggles shared by the demo combos.

        [ObservableProperty]
        private bool _enableFiltering = true;

        [ObservableProperty]
        private bool _filterDropdownItems = true;

        [ObservableProperty]
        private IncrementalFilteringMode _filterMode = IncrementalFilteringMode.Smart;

        [ObservableProperty]
        private bool _enableAutoComplete = true;

        [ObservableProperty]
        private bool _enableShowNone;

        [ObservableProperty]
        private bool _enableSelectAll = true;

        [ObservableProperty]
        private bool _enableSizeGrip;

        [ObservableProperty]
        private bool _openOnFocus;

        [ObservableProperty]
        private bool _useRadio;

        [ObservableProperty]
        private bool _applyItemTemplate;

        /// <summary>Single normally, Radio when the glyph toggle is on (non-editable combo).</summary>
        public ComboBoxSelectionMode NonEditableSelectionMode =>
            UseRadio ? ComboBoxSelectionMode.Radio : ComboBoxSelectionMode.Single;

        partial void OnUseRadioChanged(bool value) => OnPropertyChanged(nameof(NonEditableSelectionMode));

        // Demo selections.

        [ObservableProperty]
        private OrderStatus? _selectedStatus;

        [ObservableProperty]
        private string _selectedWood;

        /// <summary>Checkbox-mode selection target.</summary>
        public ObservableCollection<object> CheckedWoods { get; } = new ObservableCollection<object>();

        /// <summary>Filtering corpus — plenty of shared substrings ("Oak", "Rustic", "Knotty").</summary>
        public string[] Woods { get; } =
        {
            "Alder", "Ash", "Beech", "Birch", "Cherry", "Hickory", "Knotty Alder", "Knotty Pine",
            "Mahogany", "Maple", "Oak", "Paint Grade Maple", "Pine", "Poplar", "Quarter Sawn Oak",
            "Red Oak", "Rustic Cherry", "Rustic Hickory", "Walnut", "White Oak",
        };

        /// <summary>Object items for the non-editable combo — DisplayMemberPath + ItemTemplate shape.</summary>
        public IReadOnlyList<OrderStatus> Statuses { get; } = new[]
        {
            new OrderStatus(1, "Draft"),
            new OrderStatus(2, "Submitted"),
            new OrderStatus(3, "Scheduled"),
            new OrderStatus(4, "In Production"),
            new OrderStatus(5, "QC Hold"),
            new OrderStatus(6, "Staged"),
            new OrderStatus(7, "Shipped"),
            new OrderStatus(8, "Delivered"),
            new OrderStatus(9, "Invoiced"),
            new OrderStatus(10, "Remake"),
            new OrderStatus(11, "Cancelled"),
        };

        public IReadOnlyList<IncrementalFilteringMode> FilterModes { get; } = new[]
        {
            IncrementalFilteringMode.Smart,
            IncrementalFilteringMode.StartsWith,
            IncrementalFilteringMode.Contains,
            IncrementalFilteringMode.EndsWith,
        };

        public ComboBoxSampleViewModel()
        {
            SelectedStatus = Statuses[1];
            SelectedWood = "Cherry";
        }
    }

    /// <summary>Lookup row for the object-items WWComboBox variant.</summary>
    public sealed record OrderStatus(int Id, string Name);
}
