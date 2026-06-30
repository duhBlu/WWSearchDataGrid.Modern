using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// The aligned-summary layer of one group header row
    /// (<see cref="GroupSummaryDisplayMode.AlignByColumns"/>): one <see cref="GroupSummaryCell"/>
    /// per visible column, laid out contiguously in display order with each cell's width bound
    /// to its column's resolved width — the same extent-space cumulative layout the data cells
    /// use, so the values sit under their columns and scroll with them (unlike the rest of the
    /// header content, which is viewport-pinned). Cells rebuild when the column layout changes
    /// (reorder, visibility, add/remove); the grid drives that through its presenter registry so
    /// a single column-state hook serves every realized header. Builds nothing while the grid's
    /// display mode is <c>Header</c>, so header rows pay nothing for the feature when it's off.
    /// </summary>
    public class GroupSummaryCellsPresenter : Control
    {
        static GroupSummaryCellsPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(GroupSummaryCellsPresenter),
                new FrameworkPropertyMetadata(typeof(GroupSummaryCellsPresenter)));
        }

        public const string PartSummaryCellsPanelName = "PART_SummaryCellsPanel";

        private Panel _panel;
        private SearchDataGrid _grid;

        public GroupSummaryCellsPresenter()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _panel = GetTemplateChild(PartSummaryCellsPanelName) as Panel;
            RebuildCells();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var grid = FindAncestorGrid();
            if (!ReferenceEquals(grid, _grid))
            {
                _grid?.UnregisterGroupSummaryPresenter(this);
                _grid = grid;
                _grid?.RegisterGroupSummaryPresenter(this);
            }
            RebuildCells();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _grid?.UnregisterGroupSummaryPresenter(this);
            _grid = null;
        }

        /// <summary>
        /// Recycled containers re-bind to a different header sentinel — re-point the existing
        /// cells instead of rebuilding them.
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_panel == null) return;
            foreach (var child in _panel.Children)
            {
                if (child is GroupSummaryCell cell)
                    cell.Source = DataContext;
            }
        }

        /// <summary>
        /// Rebuilds one cell per visible column in display order. Width tracks the column live
        /// through a binding (descriptor's <c>ActualWidth</c> when the column was generated from
        /// a <see cref="GridColumn"/>, else the raw <see cref="DataGridColumn.ActualWidth"/>).
        /// Clears instead while the grid isn't in AlignByColumns mode.
        /// </summary>
        internal void RebuildCells()
        {
            if (_panel == null || _grid == null) return;

            _panel.Children.Clear();
            if (_grid.GroupSummaryDisplayMode != GroupSummaryDisplayMode.AlignByColumns) return;

            foreach (var column in _grid.Columns
                         .Where(c => c.Visibility == Visibility.Visible)
                         .OrderBy(c => c.DisplayIndex))
            {
                var descriptor = _grid.FindGridColumnDescriptor(column);
                var cell = new GroupSummaryCell
                {
                    Column = column,
                    Descriptor = descriptor,
                    Source = DataContext,
                };

                var widthSource = (object)descriptor ?? column;
                cell.SetBinding(WidthProperty, new Binding(nameof(DataGridColumn.ActualWidth))
                {
                    Source = widthSource,
                    Mode = BindingMode.OneWay,
                });

                _panel.Children.Add(cell);
            }
        }

        private SearchDataGrid FindAncestorGrid()
        {
            DependencyObject d = this;
            while (d != null)
            {
                if (d is SearchDataGrid grid) return grid;
                d = VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
