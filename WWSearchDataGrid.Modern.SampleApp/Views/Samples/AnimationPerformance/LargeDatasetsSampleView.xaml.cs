using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AnimationPerformance
{
    public partial class LargeDatasetsSampleView : UserControl
    {
        public LargeDatasetsSampleView() => InitializeComponent();

        private void OnScaleClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;
            if (!int.TryParse(tag, out var count)) return;
            if (DataContext is not LargeDatasetsSampleViewModel vm) return;

            vm.RowCount = count;
            if (vm.GenerateCommand?.CanExecute(count) == true)
                vm.GenerateCommand.Execute(count);
        }
    }
}
