using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.SampleApp.Views.Samples.Grouping
{
    public partial class BasicGroupingSampleView : UserControl
    {
        public BasicGroupingSampleView() => InitializeComponent();

        // The "+N" rail buttons carry their increment in Tag; append that many rows to the live
        // collection (the view-model handles generation + the busy overlay off the UI thread).
        private void AddRows_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe
                && fe.Tag is string tag
                && int.TryParse(tag, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                && DataContext is BasicGroupingSampleViewModel vm)
            {
                vm.AddRows(count);
            }
        }

        // Each handler demonstrates the string-based GroupBy API. Repeated clicks on different
        // columns build nested group levels; Ungroup is reached via the column-header context menu.
        private void GroupByStatus_Click(object sender, RoutedEventArgs e) => Grid.GroupBy("OrderStatusName");

        private void GroupByProductLine_Click(object sender, RoutedEventArgs e) => Grid.GroupBy("ProductLineName");

        private void GroupByState_Click(object sender, RoutedEventArgs e) => Grid.GroupBy("State");

        // The OrderDate column declares GroupInterval="DateMonth", so grouping by it buckets rows
        // by calendar month rather than by exact timestamp.
        private void GroupByMonth_Click(object sender, RoutedEventArgs e) => Grid.GroupBy("OrderDate");

        private void ClearGrouping_Click(object sender, RoutedEventArgs e) => Grid.ClearGrouping();

        private void ExpandAll_Click(object sender, RoutedEventArgs e) => Grid.ExpandAllGroups();

        private void CollapseAll_Click(object sender, RoutedEventArgs e) => Grid.CollapseAllGroups();

        // Exercises Phase 3.1.c expansion-state persistence: refresh tears down every realized
        // GroupItem, but the grid replays each group's last-toggled state on re-realization.
        private void RefreshItems_Click(object sender, RoutedEventArgs e) => Grid.Items.Refresh();
    }
}
