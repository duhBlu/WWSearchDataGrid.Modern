using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    public partial class DebounceBehaviorSampleView : UserControl
    {
        public DebounceBehaviorSampleView()
        {
            InitializeComponent();
        }

        private void OnClearAllFiltersClicked(object sender, RoutedEventArgs e)
        {
            Grid.ClearAllFilters();
        }
    }
}
