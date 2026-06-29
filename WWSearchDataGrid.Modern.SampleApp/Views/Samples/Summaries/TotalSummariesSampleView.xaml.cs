using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Summaries
{
    public partial class TotalSummariesSampleView : UserControl
    {
        public TotalSummariesSampleView()
        {
            InitializeComponent();

            // Default both column pickers to the Total column — the most useful editor target.
            var totalColumn = Grid.GridColumns.OfType<GridColumn>()
                .FirstOrDefault(c => c.FieldName == "OrderItemsTotalPrice");
            TotalColumnPicker.SelectedItem = totalColumn ?? Grid.GridColumns.OfType<GridColumn>().FirstOrDefault();
            FooterColumnPicker.SelectedItem = TotalColumnPicker.SelectedItem;
        }

        // ── Total Summary ──────────────────────────────────────────────
        private void TotalSummaryEditor_Click(object sender, RoutedEventArgs e)
        {
            if (TotalColumnPicker.SelectedItem is GridColumn column)
                GroupSummaryEditor.ShowColumnTotalsDialog(Grid, column);
        }

        // ── Fixed Total Summary Panel ──────────────────────────────────
        private void FixedToggle_Click(object sender, RoutedEventArgs e)
            => Grid.ShowFixedTotalSummary = !Grid.ShowFixedTotalSummary;

        private void FixedEditor_Click(object sender, RoutedEventArgs e)
            => GroupSummaryEditor.ShowFixedTotalsDialog(Grid);

        // ── Group Summary ──────────────────────────────────────────────
        private void GroupModeDefault_Checked(object sender, RoutedEventArgs e)
        {
            if (Grid != null) Grid.GroupSummaryDisplayMode = GroupSummaryDisplayMode.Header;
        }

        private void GroupModeAligned_Checked(object sender, RoutedEventArgs e)
        {
            if (Grid != null) Grid.GroupSummaryDisplayMode = GroupSummaryDisplayMode.AlignByColumns;
        }

        private void GroupSummaryEditor_Click(object sender, RoutedEventArgs e)
            => GroupSummaryEditor.ShowGroupDialog(Grid);

        private void GroupByStatus_Click(object sender, RoutedEventArgs e) => Grid.GroupBy("OrderStatusName");

        private void GroupByProductLine_Click(object sender, RoutedEventArgs e) => Grid.GroupBy("ProductLineName");

        private void ClearGrouping_Click(object sender, RoutedEventArgs e) => Grid.ClearGrouping();

        // ── Group Footer Summary ───────────────────────────────────────
        private void GroupFooterEditor_Click(object sender, RoutedEventArgs e)
        {
            if (FooterColumnPicker.SelectedItem is GridColumn column)
                GroupSummaryEditor.ShowGroupFooterDialog(Grid, column);
        }

        // ── Sort groups by summary ─────────────────────────────────────
        private void SortGroupsBySum_Click(object sender, RoutedEventArgs e)
            => Grid.SortGroupsBySummary(SummaryItemType.Sum, "OrderItemsTotalPrice");

        private void SortGroupsByCount_Click(object sender, RoutedEventArgs e)
            => Grid.SortGroupsBySummary(SummaryItemType.Count);

        private void ClearSummarySort_Click(object sender, RoutedEventArgs e) => Grid.ClearGroupSummarySort();

        private void RefreshSummaries_Click(object sender, RoutedEventArgs e) => Grid.RefreshSummaries();
    }
}
