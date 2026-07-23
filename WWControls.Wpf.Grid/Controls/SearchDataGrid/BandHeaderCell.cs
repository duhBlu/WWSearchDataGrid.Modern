using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One caption cell in the banded column-header area — the visual for a
    /// <see cref="GridColumnBand"/>. Produced by the band header presenter (one per
    /// <see cref="BandLayoutNode"/>), positioned to span the band's member columns. Lookless;
    /// styled by <see cref="GridThemeKeys.GridSearchDataGridBandHeaderCell"/>.
    /// </summary>
    public class BandHeaderCell : Control
    {
        /// <summary>
        /// Caption row this cell occupies (0 = topmost band row). Set by the band header
        /// presenter from the source <see cref="BandLayoutNode.Level"/>; read by the panel to
        /// place the cell vertically. Not a DP — set once per rebuild.
        /// </summary>
        internal int BandLevel { get; set; }

        /// <summary>
        /// The generated columns this band spans, in display order. Set by the presenter from the
        /// band's member <see cref="GridColumn"/>s (resolved to their <c>InternalColumn</c>); read
        /// by the panel to compute the cell's horizontal bounds (first member's left edge to last
        /// member's right edge). Empty until the columns are generated.
        /// </summary>
        internal IReadOnlyList<DataGridColumn> MemberColumns { get; set; } = System.Array.Empty<DataGridColumn>();

        /// <summary>
        /// True when this band has nested child bands (which occupy the rows below it). When false
        /// — a leaf band whose children are columns — the caption spans down to the column headers
        /// so it sits directly above them instead of leaving a gap under ragged nesting.
        /// </summary>
        internal bool HasChildBands { get; set; }

        static BandHeaderCell()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(BandHeaderCell),
                new FrameworkPropertyMetadata(typeof(BandHeaderCell)));
        }

        /// <summary>Template part: the right-edge resize gripper that resizes the band's columns.</summary>
        public const string PartRightGripperName = "PART_BandRightGripper";

        private Thumb _rightGripper;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_rightGripper != null)
                _rightGripper.DragDelta -= OnRightGripperDragDelta;
            _rightGripper = GetTemplateChild(PartRightGripperName) as Thumb;
            if (_rightGripper != null)
                _rightGripper.DragDelta += OnRightGripperDragDelta;
        }

        /// <summary>
        /// Resizes the band's member columns as its right edge is dragged, distributing the
        /// horizontal change equally so the band's columns grow / shrink together. Honors each
        /// column's <see cref="DataGridColumn.CanUserResize"/> and Min/Max width; the current
        /// rendered width is the per-column base.
        /// </summary>
        private void OnRightGripperDragDelta(object sender, DragDeltaEventArgs e)
        {
            var columns = MemberColumns;
            if (columns == null || columns.Count == 0 || e.HorizontalChange == 0)
                return;

            int resizable = 0;
            foreach (var column in columns)
                if (column != null && column.Visibility == Visibility.Visible && column.CanUserResize)
                    resizable++;
            if (resizable == 0)
                return;

            double per = e.HorizontalChange / resizable;
            foreach (var column in columns)
            {
                if (column == null || column.Visibility != Visibility.Visible || !column.CanUserResize)
                    continue;

                double target = column.ActualWidth + per;
                double min = column.MinWidth > 0 ? column.MinWidth : 20;
                if (target < min) target = min;
                double max = column.MaxWidth;
                if (max > 0 && !double.IsInfinity(max) && target > max) target = max;

                column.Width = new DataGridLength(target);
            }
        }

        /// <summary>The band's caption content (mirrors <see cref="GridColumnBand.Header"/>).</summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(BandHeaderCell),
                new PropertyMetadata(null));

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Optional template for <see cref="Header"/> (mirrors
        /// <see cref="GridColumnBand.HeaderTemplate"/>). Null renders the caption as text.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(
                nameof(HeaderTemplate),
                typeof(DataTemplate),
                typeof(BandHeaderCell),
                new PropertyMetadata(null));

        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }
    }
}
