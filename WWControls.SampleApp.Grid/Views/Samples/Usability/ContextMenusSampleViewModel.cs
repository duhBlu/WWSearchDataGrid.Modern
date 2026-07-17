using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.Usability
{
    public sealed partial class ContextMenusSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        /// <summary>
        /// Feedback line shown in the side panel so the injected commands are visibly firing —
        /// stands in for whatever real work (navigation, dialog, service call) a consumer would do.
        /// </summary>
        [ObservableProperty]
        private string _lastAction = "Right-click a cell, a column header, or a row header, then pick one of the custom items below.";

        public ContextMenusSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(100, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }

        // ── Cell-menu commands ───────────────────────────────────────────────
        // Bound with CommandParameter="{Binding RowData}" (the clicked row's data item) and
        // "{Binding CellValue}" (the clicked cell's value).

        [RelayCommand]
        private void ShowOrderDetails(OrderItem order)
        {
            if (order is null) return;
            LastAction = $"Cell → Show details for order #{order.OrderNumber} — {order.CustomerName} ({order.OrderStatusName}).";
        }

        [RelayCommand]
        private void UseCellValue(object value)
            => LastAction = $"Cell → Used this cell value: \"{value}\".";

        // ── Column-header command ────────────────────────────────────────────
        // Bound with CommandParameter="{Binding Column}" (the clicked DataGridColumn).

        [RelayCommand]
        private void PinColumnToReport(DataGridColumn column)
            => LastAction = $"Header → Pinned column \"{column?.Header}\" to my report.";

        // ── Row-header command ───────────────────────────────────────────────
        [RelayCommand]
        private void FlagOrder(OrderItem order)
        {
            if (order is null) return;
            LastAction = $"Row → Flagged order #{order.OrderNumber} for follow-up.";
        }

        // ── Command wired only to the dynamically-injected item (see code-behind) ──
        [RelayCommand]
        private void ExpediteOrder(OrderItem order)
        {
            if (order is null) return;
            LastAction = $"Cell (dynamic) → Expedited order #{order.OrderNumber}, previously {order.OrderStatusName}.";
        }
    }
}
