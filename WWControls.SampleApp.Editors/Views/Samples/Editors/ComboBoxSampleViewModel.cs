using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Backs the WWComboBox sample: plain-string selection, object selection with
    /// DisplayMemberPath, id-based SelectedValue, and free-text entry via IsEditable.
    /// </summary>
    public partial class ComboBoxSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedFruit = "Cherry";

        [ObservableProperty]
        private OrderStatus? _selectedStatus;

        [ObservableProperty]
        private int _selectedStatusId = 3;

        [ObservableProperty]
        private string _typedFruit = "Apple";

        public string[] Fruits { get; } = { "Apple", "Banana", "Cherry", "Date", "Elderberry" };

        public IReadOnlyList<OrderStatus> Statuses { get; } = new[]
        {
            new OrderStatus(1, "Draft"),
            new OrderStatus(2, "Submitted"),
            new OrderStatus(3, "In Production"),
            new OrderStatus(4, "Shipped"),
            new OrderStatus(5, "Delivered"),
        };

        public ComboBoxSampleViewModel()
        {
            SelectedStatus = Statuses[1];
        }
    }

    /// <summary>Lookup row for the object-items and id-based WWComboBox variants.</summary>
    public sealed record OrderStatus(int Id, string Name);
}
