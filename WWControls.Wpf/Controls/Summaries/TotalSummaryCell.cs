using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// One per-column cell in the pinned total summary row. Renders the owning descriptor's
    /// computed <see cref="GridColumn.TotalSummaryText"/> (styled by
    /// <see cref="GridColumn.ActualTotalSummaryContentStyle"/>) and carries the right-click
    /// summary picker menu, whose commands read <see cref="Descriptor"/> off the placement
    /// target. Created by <see cref="TotalSummaryRowPresenter"/>, one per data column,
    /// including columns without summaries — every column needs a cell so the row's borders
    /// and click surface stay continuous.
    /// </summary>
    public class TotalSummaryCell : Control
    {
        static TotalSummaryCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TotalSummaryCell),
                new FrameworkPropertyMetadata(typeof(TotalSummaryCell)));
        }

        /// <summary>The generated WPF column this cell aligns under.</summary>
        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.Register(
                nameof(Column),
                typeof(DataGridColumn),
                typeof(TotalSummaryCell),
                new PropertyMetadata(null));

        public DataGridColumn Column
        {
            get => (DataGridColumn)GetValue(ColumnProperty);
            set => SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// The column descriptor whose summaries this cell renders. Null for columns the grid
        /// didn't generate from a <see cref="GridColumn"/> — the cell then renders empty chrome.
        /// </summary>
        public static readonly DependencyProperty DescriptorProperty =
            DependencyProperty.Register(
                nameof(Descriptor),
                typeof(GridColumn),
                typeof(TotalSummaryCell),
                new PropertyMetadata(null));

        public GridColumn Descriptor
        {
            get => (GridColumn)GetValue(DescriptorProperty);
            set => SetValue(DescriptorProperty, value);
        }

        /// <summary>The owning grid — the summary picker commands recompute through it.</summary>
        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(
                nameof(OwnerGrid),
                typeof(SearchDataGrid),
                typeof(TotalSummaryCell),
                new PropertyMetadata(null));

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }
    }
}
