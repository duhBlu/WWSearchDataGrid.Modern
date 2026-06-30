using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One per-column slot in a group footer row. Renders the group's computed footer results for
    /// its column (one right-aligned line per entry, stacked vertically), or empty chrome when the
    /// column carries no footer entries — every data column gets a cell so the row's surface and
    /// the right-click summary picker stay continuous. Combines <see cref="GroupSummaryCell"/>'s
    /// per-group results read (through <see cref="IGroupFooterSummarySource"/>) with
    /// <see cref="TotalSummaryCell"/>'s picker plumbing: the cell's context-menu commands read
    /// <see cref="Descriptor"/> / <see cref="OwnerGrid"/> off the placement target and toggle the
    /// column's <see cref="GridColumn.GroupFooterSummaries"/>. Width is bound to the column's
    /// resolved width by <see cref="GroupFooterCellsPresenter"/>, so the value sits under its column.
    /// </summary>
    public class GroupFooterCell : Control
    {
        static GroupFooterCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(GroupFooterCell),
                new FrameworkPropertyMetadata(typeof(GroupFooterCell)));
        }

        /// <summary>The generated WPF column this cell aligns under.</summary>
        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.Register(
                nameof(Column),
                typeof(DataGridColumn),
                typeof(GroupFooterCell),
                new PropertyMetadata(null));

        public DataGridColumn Column
        {
            get => (DataGridColumn)GetValue(ColumnProperty);
            set => SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// The column descriptor whose footer results this cell renders and whose
        /// <see cref="GridColumn.GroupFooterSummaries"/> the picker mutates. Null for columns the
        /// grid didn't generate from a <see cref="GridColumn"/> — the cell renders empty.
        /// </summary>
        public static readonly DependencyProperty DescriptorProperty =
            DependencyProperty.Register(
                nameof(Descriptor),
                typeof(GridColumn),
                typeof(GroupFooterCell),
                new PropertyMetadata(null, OnResultsInputChanged));

        public GridColumn Descriptor
        {
            get => (GridColumn)GetValue(DescriptorProperty);
            set => SetValue(DescriptorProperty, value);
        }

        /// <summary>The owning grid — the footer picker commands recompute through it.</summary>
        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(
                nameof(OwnerGrid),
                typeof(SearchDataGrid),
                typeof(GroupFooterCell),
                new PropertyMetadata(null));

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }

        /// <summary>
        /// The footer surface this cell reads its results from — a <see cref="GroupFooterRow"/>
        /// sentinel (anything implementing <see cref="IGroupFooterSummarySource"/>; other values
        /// read as empty). Re-pointed on container recycle (the presenter pushes its new
        /// DataContext); the cell re-pulls on the source's change notification so an in-place
        /// footer recompute lands without a reflatten.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(object),
                typeof(GroupFooterCell),
                new PropertyMetadata(null, OnSourceChanged));

        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static readonly DependencyPropertyKey ResultsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Results),
                typeof(IReadOnlyList<SummaryResult>),
                typeof(GroupFooterCell),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="Results"/> for bindings.</summary>
        public static readonly DependencyProperty ResultsProperty = ResultsPropertyKey.DependencyProperty;

        /// <summary>The computed footer results rendered in this cell — null/empty renders no lines.</summary>
        public IReadOnlyList<SummaryResult> Results => (IReadOnlyList<SummaryResult>)GetValue(ResultsProperty);

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GroupFooterCell cell) return;
            if (e.OldValue is IGroupFooterSummarySource oldSource)
                oldSource.PropertyChanged -= cell.OnSourcePropertyChanged;
            if (e.NewValue is IGroupFooterSummarySource newSource)
                newSource.PropertyChanged += cell.OnSourcePropertyChanged;
            cell.UpdateResults();
        }

        private static void OnResultsInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as GroupFooterCell)?.UpdateResults();

        private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
            => UpdateResults();

        private void UpdateResults()
            => SetValue(ResultsPropertyKey, (Source as IGroupFooterSummarySource)?.GetFooterResultsFor(Descriptor));
    }
}
