using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Vertical looping item selector — the column primitive behind the date editor's scroll-list
    /// popup. Items render as text rows around a fixed center band; the centered row is the
    /// selection. Scrolling wraps around the item list (last → first → last), so every value is
    /// reachable in either direction. Interaction: mouse wheel steps one row, click on a visible
    /// row selects it, drag scrolls with snap-to-row on release, Up/Down arrows step when focused.
    /// </summary>
    /// <remarks>
    /// Self-rendering (no ControlTemplate): rows are drawn in <see cref="OnRender"/> from
    /// <see cref="ItemsSource"/> via <c>ToString</c>, positioned by modular index math around
    /// <see cref="SelectedIndex"/> plus the in-flight drag offset. A templated ItemsControl can't
    /// express the wraparound window cheaply; direct drawing keeps the control a few hundred lines
    /// with no per-item container cost. Typography comes from the inherited <see cref="Control"/>
    /// font properties; the center band uses <see cref="AccentBrush"/>.
    /// </remarks>
    public class WWLoopingSelector : Control
    {
        static WWLoopingSelector()
        {
            FocusableProperty.OverrideMetadata(typeof(WWLoopingSelector), new FrameworkPropertyMetadata(true));
        }

        public WWLoopingSelector()
        {
            ClipToBounds = true;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(WWLoopingSelector),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(WWLoopingSelector),
                new FrameworkPropertyMetadata(0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSelectedIndexChanged, CoerceSelectedIndex));

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(WWLoopingSelector),
                new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Whether scrolling wraps past the ends. When <c>false</c> the list clamps at the first /
        /// last item and rows beyond the range render blank.
        /// </summary>
        public static readonly DependencyProperty IsLoopingProperty =
            DependencyProperty.Register(nameof(IsLooping), typeof(bool), typeof(WWLoopingSelector),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Brush for the horizontal rules bounding the center selection band.</summary>
        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(WWLoopingSelector),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), FrameworkPropertyMetadataOptions.AffectsRender));

        public IList ItemsSource
        {
            get => (IList)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public bool IsLooping
        {
            get => (bool)GetValue(IsLoopingProperty);
            set => SetValue(IsLoopingProperty, value);
        }

        public Brush AccentBrush
        {
            get => (Brush)GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        /// <summary>Raised after <see cref="SelectedIndex"/> changes (user interaction or programmatic).</summary>
        public event EventHandler SelectedIndexChanged;

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (WWLoopingSelector)d;
            self.CoerceValue(SelectedIndexProperty);
        }

        private static object CoerceSelectedIndex(DependencyObject d, object baseValue)
        {
            var self = (WWLoopingSelector)d;
            int count = self.ItemsSource?.Count ?? 0;
            if (count == 0) return 0;
            int index = (int)baseValue;
            if (self.IsLooping) return ((index % count) + count) % count;
            return Math.Max(0, Math.Min(count - 1, index));
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (WWLoopingSelector)d;
            self.SelectedIndexChanged?.Invoke(self, EventArgs.Empty);
        }

        #endregion

        #region Interaction

        // Drag state: _dragOrigin is the mouse-down Y; _dragPixelOffset shifts every row during a
        // drag and is folded back into SelectedIndex in whole-row steps as the pointer moves.
        private double _dragOrigin;
        private double _dragPixelOffset;
        private bool _mouseDown;
        private bool _isDragging;
        private const double DragThreshold = 3.0;

        private int Count => ItemsSource?.Count ?? 0;

        private void Step(int delta)
        {
            if (Count == 0) return;
            SelectedIndex = SelectedIndex + delta; // coercion wraps / clamps
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            Step(e.Delta > 0 ? -1 : +1);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Up) { Step(-1); e.Handled = true; }
            else if (e.Key == Key.Down) { Step(+1); e.Handled = true; }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (Count == 0) return;
            Focus();
            _mouseDown = true;
            _isDragging = false;
            _dragOrigin = e.GetPosition(this).Y;
            _dragPixelOffset = 0;
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_mouseDown) return;

            double y = e.GetPosition(this).Y;
            double delta = y - _dragOrigin;
            if (!_isDragging && Math.Abs(delta) < DragThreshold) return;
            _isDragging = true;

            // Fold whole rows into SelectedIndex as the pointer crosses row boundaries; keep the
            // remainder as the pixel offset so rows track the pointer smoothly. Dragging down
            // moves rows down, revealing earlier items — index decreases.
            _dragPixelOffset = delta;
            double itemHeight = Math.Max(1, ItemHeight);
            int wholeRows = (int)(_dragPixelOffset / itemHeight);
            if (wholeRows != 0)
            {
                int before = SelectedIndex;
                Step(-wholeRows);
                // In clamped mode the step can hit the end and move less than requested; re-anchor
                // the origin to the actually-applied movement so the offset doesn't accumulate.
                int applied = IsLooping ? -wholeRows : SelectedIndex - before;
                _dragOrigin += -applied * itemHeight;
                _dragPixelOffset = y - _dragOrigin;
            }
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (!_mouseDown) return;
            _mouseDown = false;
            ReleaseMouseCapture();

            if (_isDragging)
            {
                // Snap: fold any remaining partial-row offset into the selection.
                double itemHeight = Math.Max(1, ItemHeight);
                int rounded = (int)Math.Round(_dragPixelOffset / itemHeight);
                if (rounded != 0) Step(-rounded);
                _dragPixelOffset = 0;
                _isDragging = false;
                InvalidateVisual();
            }
            else if (Count > 0)
            {
                // Plain click: select the row under the pointer.
                double centerY = ActualHeight / 2;
                int rowOffset = (int)Math.Round((e.GetPosition(this).Y - centerY) / Math.Max(1, ItemHeight));
                if (rowOffset != 0) Step(rowOffset);
            }
            e.Handled = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            if (_isDragging)
            {
                _dragPixelOffset = 0;
                _isDragging = false;
                InvalidateVisual();
            }
            _mouseDown = false;
        }

        #endregion

        #region Rendering

        protected override Size MeasureOverride(Size constraint)
        {
            // Natural size: 5 visible rows tall; width from the widest item at the current font.
            double height = double.IsInfinity(constraint.Height) ? ItemHeight * 5 : constraint.Height;
            double width = 40;
            var items = ItemsSource;
            if (items != null && items.Count > 0)
            {
                var typeface = new Typeface(FontFamily, FontStyle, FontWeights.SemiBold, FontStretch);
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                foreach (var item in items)
                {
                    var ft = new FormattedText(item?.ToString() ?? string.Empty, CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, typeface, FontSize, Brushes.Black, pixelsPerDip);
                    if (ft.Width > width) width = ft.Width;
                }
                width += 16; // horizontal breathing room
            }
            if (!double.IsInfinity(constraint.Width)) width = Math.Min(width, constraint.Width);
            return new Size(width, height);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // Background rect also makes the whole surface hit-testable.
            dc.DrawRectangle(Background ?? Brushes.Transparent, null, new Rect(0, 0, w, h));

            int count = Count;
            double itemHeight = Math.Max(1, ItemHeight);
            double centerY = h / 2;

            if (count > 0)
            {
                var culture = CultureInfo.CurrentCulture;
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                var normalTypeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                var selectedTypeface = new Typeface(FontFamily, FontStyle, FontWeights.SemiBold, FontStretch);
                var normalBrush = Foreground?.Clone() ?? Brushes.Black.Clone();
                normalBrush.Opacity = 0.55;
                if (normalBrush.CanFreeze) normalBrush.Freeze();

                int visibleHalf = (int)Math.Ceiling(centerY / itemHeight) + 1;
                for (int row = -visibleHalf; row <= visibleHalf; row++)
                {
                    int index = SelectedIndex + row;
                    if (IsLooping) index = ((index % count) + count) % count;
                    else if (index < 0 || index >= count) continue;

                    double rowCenter = centerY + row * itemHeight + _dragPixelOffset;
                    bool isCenter = !_isDragging && row == 0;

                    var ft = new FormattedText(ItemsSource[index]?.ToString() ?? string.Empty, culture,
                        FlowDirection.LeftToRight,
                        isCenter ? selectedTypeface : normalTypeface, FontSize,
                        isCenter ? (Foreground ?? Brushes.Black) : normalBrush,
                        pixelsPerDip);
                    dc.DrawText(ft, new Point((w - ft.Width) / 2, rowCenter - ft.Height / 2));
                }
            }

            // Center selection band rules on top of the rows.
            var pen = new Pen(AccentBrush, 1);
            if (pen.CanFreeze) pen.Freeze();
            double topY = Math.Round(centerY - itemHeight / 2) + 0.5;
            double bottomY = Math.Round(centerY + itemHeight / 2) + 0.5;
            dc.DrawLine(pen, new Point(4, topY), new Point(w - 4, topY));
            dc.DrawLine(pen, new Point(4, bottomY), new Point(w - 4, bottomY));
        }

        #endregion
    }
}
