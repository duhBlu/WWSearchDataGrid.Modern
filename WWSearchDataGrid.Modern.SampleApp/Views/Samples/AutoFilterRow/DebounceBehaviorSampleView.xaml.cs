using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    public partial class DebounceBehaviorSampleView : UserControl
    {
        public DebounceBehaviorSampleView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DebounceBehaviorSampleViewModel vm) return;

            vm.AttachGrid(Grid);
            foreach (GridColumn column in Grid.GridColumns)
                vm.RegisterColumn(column);

            // Seed the live-filtering label after the grid materializes and the row count is real.
            vm.RefreshEffectiveLabel();
        }

        private void OnClearAllFiltersClicked(object sender, RoutedEventArgs e)
        {
            Grid.ClearAllFilters();
        }
    }
}
