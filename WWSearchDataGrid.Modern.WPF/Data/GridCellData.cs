using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Per-cell data context exposed to <see cref="GridColumn.FilterRowDisplayTemplate"/>
    /// and <see cref="GridColumn.FilterRowEditTemplate"/>. Per D6 in the filter-row
    /// plan, the same type backs filter-row and (future) cell-edit contexts — in the
    /// filter-row case <see cref="RowData"/> is always <c>null</c> and selection slots stay
    /// at their defaults, so a template that binds to <c>RowData.X</c> simply produces a
    /// silent binding error rather than throwing.
    /// </summary>
    public class GridCellData : GridColumnData
    {
        public static readonly DependencyProperty RowDataProperty =
            DependencyProperty.Register(
                nameof(RowData),
                typeof(object),
                typeof(GridCellData),
                new PropertyMetadata(null));

        public object RowData
        {
            get => GetValue(RowDataProperty);
            set => SetValue(RowDataProperty, value);
        }

        public static readonly DependencyProperty IsFocusedCellProperty =
            DependencyProperty.Register(
                nameof(IsFocusedCell),
                typeof(bool),
                typeof(GridCellData),
                new PropertyMetadata(false));

        public bool IsFocusedCell
        {
            get => (bool)GetValue(IsFocusedCellProperty);
            set => SetValue(IsFocusedCellProperty, value);
        }

        /// <summary>
        /// CLR slot — not a DP. The filter-row host doesn't drive selection so the simpler
        /// shape suffices; future cell-edit consumers can switch to a DP if/when bindings
        /// need change notifications.
        /// </summary>
        public bool IsSelected { get; internal set; }

        /// <inheritdoc cref="SelectionState"/>
        public SelectionState SelectionState { get; internal set; }

        public static readonly DependencyProperty DisplayMemberBindingValueProperty =
            DependencyProperty.Register(
                nameof(DisplayMemberBindingValue),
                typeof(object),
                typeof(GridCellData),
                new PropertyMetadata(null));

        public object DisplayMemberBindingValue
        {
            get => GetValue(DisplayMemberBindingValueProperty);
            set => SetValue(DisplayMemberBindingValueProperty, value);
        }

        /// <summary>
        /// Reference to the owning <see cref="SearchDataGrid"/> so templates can reach the
        /// grid's <see cref="FrameworkElement.DataContext"/> via
        /// <c>{Binding View.DataContext.X}</c>.
        /// </summary>
        public SearchDataGrid View { get; internal set; }
    }
}
