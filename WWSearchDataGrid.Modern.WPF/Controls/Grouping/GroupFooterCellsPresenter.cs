using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// The cells host of one group footer row: one <see cref="GroupFooterCell"/> per visible
    /// column, laid out contiguously in display order with each cell's width bound to its
    /// column's resolved width — the same extent-space layout the data cells (and the
    /// AlignByColumns header layer) use, so each footer value sits under its column and scrolls
    /// with it. Unlike <see cref="GroupSummaryCellsPresenter"/> (which only builds cells in
    /// AlignByColumns mode), the footer presenter always builds its cells — the footer row IS the
    /// aligned surface. Cells rebuild when the column layout changes (reorder, visibility,
    /// add/remove); the grid drives that through its footer-presenter registry so one column-state
    /// hook serves every realized footer row.
    /// </summary>
    public class GroupFooterCellsPresenter : Control
    {
        static GroupFooterCellsPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(GroupFooterCellsPresenter),
                new FrameworkPropertyMetadata(typeof(GroupFooterCellsPresenter)));
        }

        public const string PartFooterCellsPanelName = "PART_FooterCellsPanel";

        private Panel _panel;
        private SearchDataGrid _grid;

        public GroupFooterCellsPresenter()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _panel = GetTemplateChild(PartFooterCellsPanelName) as Panel;
            RebuildCells();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var grid = FindAncestorGrid();
            if (!ReferenceEquals(grid, _grid))
            {
                _grid?.UnregisterGroupFooterPresenter(this);
                _grid = grid;
                _grid?.RegisterGroupFooterPresenter(this);
            }
            RebuildCells();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _grid?.UnregisterGroupFooterPresenter(this);
            _grid = null;
        }

        /// <summary>
        /// Recycled containers re-bind to a different footer sentinel — re-point the existing
        /// cells instead of rebuilding them.
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_panel == null) return;
            foreach (var child in _panel.Children)
            {
                if (child is GroupFooterCell cell)
                    cell.Source = DataContext;
            }
        }

        /// <summary>
        /// Rebuilds one cell per visible column in display order. Width tracks the column live
        /// through a binding (descriptor's <c>ActualWidth</c> when the column was generated from a
        /// <see cref="GridColumn"/>, else the raw <see cref="DataGridColumn.ActualWidth"/>).
        /// </summary>
        internal void RebuildCells()
        {
            if (_panel == null || _grid == null) return;

            _panel.Children.Clear();

            foreach (var column in _grid.Columns
                         .Where(c => c.Visibility == Visibility.Visible)
                         .OrderBy(c => c.DisplayIndex))
            {
                var descriptor = _grid.FindGridColumnDescriptor(column);
                var cell = new GroupFooterCell
                {
                    Column = column,
                    Descriptor = descriptor,
                    OwnerGrid = _grid,
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
