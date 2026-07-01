using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Custom column header. Subclassing is needed because the stock header is a
    /// <see cref="ButtonBase"/> that triggers sort on every click — including clicks on
    /// descendant buttons (filter popup, etc.). <see cref="OnClick"/> suppresses sort when
    /// the click came from a descendant button. Also hosts the attached property
    /// <see cref="DescriptorProperty"/>, which the column generator pins on each created
    /// <see cref="DataGridColumn"/> so the header template can bind directly to its matching
    /// <see cref="ColumnDataBase"/> descriptor — e.g. <c>Column.(sdg:SearchDataGridColumnHeader.Descriptor).IsFiltered</c>
    /// for the "filtered column" highlight, without any C# subscription wiring.
    /// </summary>
    public class SearchDataGridColumnHeader : DataGridColumnHeader
    {
        static SearchDataGridColumnHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SearchDataGridColumnHeader),
                new FrameworkPropertyMetadata(typeof(SearchDataGridColumnHeader)));
        }

        /// <summary>
        /// Attached on the generated <see cref="DataGridColumn"/> (the header's <c>Column</c>)
        /// during <see cref="ColumnDataBase.CreateDataGridColumn"/> and pointing at the originating
        /// descriptor. Used by the header template to bind directly against descriptor-side DPs
        /// such as <see cref="ColumnDataBase.IsFiltered"/>.
        /// </summary>
        public static readonly DependencyProperty DescriptorProperty =
            DependencyProperty.RegisterAttached(
                "Descriptor",
                typeof(ColumnDataBase),
                typeof(SearchDataGridColumnHeader),
                new PropertyMetadata(null));

        public static ColumnDataBase GetDescriptor(DependencyObject obj) =>
            (ColumnDataBase)obj.GetValue(DescriptorProperty);

        public static void SetDescriptor(DependencyObject obj, ColumnDataBase value) =>
            obj.SetValue(DescriptorProperty, value);

        // Captured in OnPreviewMouseLeftButtonDown, consulted (and reset) in OnClick.
        private bool _clickFromChildButton;

        // Drag-to-group state: where the mouse came down, whether we've already started a drag
        // for this gesture (so we don't keep firing DoDragDrop on every subsequent MouseMove).
        private Point? _dragStartPoint;
        private bool _dragStarted;

        // Resize grippers, re-resolved on each template apply so the best-fit double-click
        // intercept survives re-templating.
        private Thumb _leftGripper;
        private Thumb _rightGripper;

        public override void OnApplyTemplate()
        {
            if (_leftGripper != null)
                _leftGripper.PreviewMouseDoubleClick -= OnGripperPreviewMouseDoubleClick;
            if (_rightGripper != null)
                _rightGripper.PreviewMouseDoubleClick -= OnGripperPreviewMouseDoubleClick;

            base.OnApplyTemplate();

            _leftGripper = GetTemplateChild("PART_LeftHeaderGripper") as Thumb;
            _rightGripper = GetTemplateChild("PART_RightHeaderGripper") as Thumb;
            if (_leftGripper != null)
                _leftGripper.PreviewMouseDoubleClick += OnGripperPreviewMouseDoubleClick;
            if (_rightGripper != null)
                _rightGripper.PreviewMouseDoubleClick += OnGripperPreviewMouseDoubleClick;
        }

        /// <summary>
        /// Double-click on a resize gripper runs the measurement-based best-fit instead of the
        /// stock <c>Width = Auto</c> (which only fits realized cells). Handling the tunneling
        /// double-click suppresses the base header's gripper handler. Columns with
        /// <see cref="ColumnLayoutBase.ActualAllowBestFit"/> <c>false</c> are left to the stock
        /// behavior — best-fit opt-out disables the smarter fit, not resizing itself. The left
        /// gripper targets the previous visible column, matching the stock semantics.
        /// </summary>
        private void OnGripperPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this);
            if (grid == null)
                return;

            var targetColumn = ReferenceEquals(sender, _leftGripper)
                ? FindPreviousVisibleColumn(grid)
                : Column;
            if (targetColumn == null || !targetColumn.CanUserResize)
                return;

            var descriptor = grid.FindGridColumnDescriptor(targetColumn);
            if (descriptor == null || !descriptor.ActualAllowBestFit)
                return;

            grid.BestFitColumn(descriptor);
            e.Handled = true;
        }

        private DataGridColumn FindPreviousVisibleColumn(SearchDataGrid grid)
        {
            if (Column == null)
                return null;

            DataGridColumn previous = null;
            foreach (var candidate in grid.Columns)
            {
                if (candidate == null
                    || candidate.Visibility != Visibility.Visible
                    || candidate.DisplayIndex >= Column.DisplayIndex)
                {
                    continue;
                }

                if (previous == null || candidate.DisplayIndex > previous.DisplayIndex)
                    previous = candidate;
            }

            return previous;
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _clickFromChildButton = IsClickFromChildButton(e.OriginalSource as DependencyObject);
            // Capture the starting point so PreviewMouseMove can decide whether the gesture is
            // an upward drag toward the group panel (start a DragDrop) vs. a click or sideways
            // drag (let the base header handle sort / reorder).
            _dragStartPoint = e.GetPosition(this);
            _dragStarted = false;
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (_dragStarted) return;
            if (_dragStartPoint is not Point start) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var current = e.GetPosition(this);
            double dx = current.X - start.X;
            double dy = current.Y - start.Y;

            // Upward drag past the system drag threshold = the user is pulling the column out of
            // the header strip toward the group panel. Sideways drags (column reorder) and short
            // gestures (clicks) are left untouched so the base header behavior keeps working.
            if (dy < -SystemParameters.MinimumVerticalDragDistance
                && System.Math.Abs(dy) > System.Math.Abs(dx))
            {
                var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this);
                var descriptor = grid?.FindGridColumnDescriptor(Column);
                if (descriptor != null && descriptor.ActualAllowGrouping)
                {
                    _dragStarted = true;
                    var data = new DataObject(GroupPanel.DragDataFormat, descriptor);
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            _dragStartPoint = null;
            _dragStarted = false;
        }

        /// <summary>
        /// Skips base.OnClick (and thus the sort gesture) when the click came from a descendant
        /// button. The descendant's own Click has already raised through its own machinery.
        /// </summary>
        protected override void OnClick()
        {
            if (_clickFromChildButton)
            {
                _clickFromChildButton = false;
                return;
            }
            base.OnClick();
        }

        private bool IsClickFromChildButton(DependencyObject source)
        {
            var cursor = source;
            while (cursor != null && !ReferenceEquals(cursor, this))
            {
                if (cursor is ButtonBase) return true;
                cursor = VisualTreeHelper.GetParent(cursor) ?? LogicalTreeHelper.GetParent(cursor);
            }
            return false;
        }
    }
}
