using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
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
