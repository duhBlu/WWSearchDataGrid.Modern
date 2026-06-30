using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// One per-column slot in a group header row's aligned-summary layer
    /// (<see cref="GroupSummaryDisplayMode.AlignByColumns"/>). Renders the header sentinel's
    /// computed results for its column (one right-aligned line per entry), or nothing when the
    /// column carries no group-summary entries. Width is bound to the column's resolved width
    /// by <see cref="GroupSummaryCellsPresenter"/>, so the value sits exactly under its column.
    /// </summary>
    public class GroupSummaryCell : Control
    {
        static GroupSummaryCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(GroupSummaryCell),
                new FrameworkPropertyMetadata(typeof(GroupSummaryCell)));
        }

        /// <summary>The generated WPF column this cell aligns under.</summary>
        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.Register(
                nameof(Column),
                typeof(DataGridColumn),
                typeof(GroupSummaryCell),
                new PropertyMetadata(null));

        public DataGridColumn Column
        {
            get => (DataGridColumn)GetValue(ColumnProperty);
            set => SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// The column descriptor whose aligned results this cell renders. Null for columns the
        /// grid didn't generate from a <see cref="GridColumn"/> — the cell then renders empty.
        /// </summary>
        public static readonly DependencyProperty DescriptorProperty =
            DependencyProperty.Register(
                nameof(Descriptor),
                typeof(GridColumn),
                typeof(GroupSummaryCell),
                new PropertyMetadata(null, OnResultsInputChanged));

        public GridColumn Descriptor
        {
            get => (GridColumn)GetValue(DescriptorProperty);
            set => SetValue(DescriptorProperty, value);
        }

        /// <summary>
        /// The header surface this cell reads its results from — an in-body
        /// <see cref="GroupHeaderRow"/> sentinel or a pinned-strip
        /// <see cref="FixedGroupHeaderEntry"/> (anything implementing
        /// <see cref="IAlignedGroupSummarySource"/>; other values read as empty). Re-pointed on
        /// container recycle (the presenter pushes its new DataContext); the cell re-pulls on
        /// the source's change notification so in-place summary recomputes land without a
        /// reflatten.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source),
                typeof(object),
                typeof(GroupSummaryCell),
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
                typeof(GroupSummaryCell),
                new PropertyMetadata(null));

        /// <summary>Read-only dependency property exposing <see cref="Results"/> for bindings.</summary>
        public static readonly DependencyProperty ResultsProperty = ResultsPropertyKey.DependencyProperty;

        /// <summary>The computed results rendered in this cell — null/empty renders no lines.</summary>
        public IReadOnlyList<SummaryResult> Results => (IReadOnlyList<SummaryResult>)GetValue(ResultsProperty);

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not GroupSummaryCell cell) return;
            if (e.OldValue is IAlignedGroupSummarySource oldSource)
                oldSource.PropertyChanged -= cell.OnSourcePropertyChanged;
            if (e.NewValue is IAlignedGroupSummarySource newSource)
                newSource.PropertyChanged += cell.OnSourcePropertyChanged;
            cell.UpdateResults();
        }

        private static void OnResultsInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as GroupSummaryCell)?.UpdateResults();

        private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
            => UpdateResults();

        private void UpdateResults()
            => SetValue(ResultsPropertyKey, (Source as IAlignedGroupSummarySource)?.GetAlignedResultsFor(Descriptor));
    }
}
