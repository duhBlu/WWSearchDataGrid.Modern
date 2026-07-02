using System.Windows;
using System.Windows.Controls;

namespace WWControls.SampleApp.Grid.Views.Samples.Columns
{
    public partial class BestFitSampleView : UserControl
    {
        public BestFitSampleView() => InitializeComponent();

        private void OnBestFitAllColumns(object sender, RoutedEventArgs e)
            => Grid.BestFitAllColumns();

        private void OnBestFitCustomer(object sender, RoutedEventArgs e)
            => Grid.BestFitColumn(CustomerColumn);

        // BestFitFillViewport only changes what a best-fit-all pass does — it doesn't resize on
        // its own. Re-run best-fit-all so toggling the checkbox takes effect immediately.
        private void OnFillViewportToggled(object sender, RoutedEventArgs e)
            => Grid.BestFitAllColumns();
    }
}
