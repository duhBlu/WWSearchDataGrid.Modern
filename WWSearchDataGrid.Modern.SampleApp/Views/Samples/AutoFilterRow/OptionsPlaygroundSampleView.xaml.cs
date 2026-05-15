using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    public partial class OptionsPlaygroundSampleView : UserControl
    {
        public OptionsPlaygroundSampleView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not OptionsPlaygroundSampleViewModel vm)
                return;

            vm.RegisterColumn("OrderNumber", OrderNumberColumn);
            vm.RegisterColumn("CustomerName", CustomerNameColumn);
            vm.RegisterColumn("OrderStatusName", StatusColumn);
            vm.RegisterColumn("OrderDate", OrderDateColumn);
            vm.RegisterColumn("OrderItemsTotalPrice", TotalColumn);
            vm.RegisterColumn("OrderCancelled", CancelledColumn);
        }

        private void OnClearAllFiltersClicked(object sender, RoutedEventArgs e)
        {
            Grid.ClearAllFilters();
        }
    }
}
