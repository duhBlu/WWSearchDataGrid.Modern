using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Xaml.Behaviors;

namespace WWSearchDataGrid.Modern.WPF
{
    #region Attached Property Helper Class
    
    public static class DragHelpers
    {
        public static readonly DependencyProperty IsDraggedProperty =
            DependencyProperty.RegisterAttached(
                "IsDragged",
                typeof(bool),
                typeof(DragHelpers),
                new FrameworkPropertyMetadata(false));

        public static void SetIsDragged(DependencyObject d, bool value) => d.SetValue(IsDraggedProperty, value);
        public static bool GetIsDragged(DependencyObject d) => (bool)d.GetValue(IsDraggedProperty);
    }

    #endregion Attached Property Helper Class

    #region Drag Adorner

    /// <summary>
    /// Adorner that paints a bitmap snapshot of the dragged element.
    /// This keeps working even if the original ListBoxItem is collapsed/ghosted.
    /// </summary>
    public sealed class DragAdorner : Adorner
    {
        private readonly ImageSource _snapshot;
        private readonly double _width;
        private readonly double _height;
        private double _left;
        private double _top;
        private readonly double _opacity;

        /// <summary>
        /// Create directly from an already-made snapshot (ImageSource) and its size.
        /// </summary>
        public DragAdorner(UIElement adornedElement, ImageSource snapshot, Size size, double opacity = 0.75)
            : base(adornedElement)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _width = Math.Max(1.0, size.Width);
            _height = Math.Max(1.0, size.Height);
            _opacity = Math.Max(0.0, Math.Min(1.0, opacity));
            IsHitTestVisible = false;
        }

        /// <summary>
        /// Convenience factory: snapshots <paramref name="elementToSnapshot"/> and adorns <paramref name="adornerRoot"/>.
        /// </summary>
        public static DragAdorner FromElement(UIElement adornerRoot, UIElement elementToSnapshot, double opacity = 0.75, double scale = 1.0)
        {
            if (adornerRoot == null) throw new ArgumentNullException(nameof(adornerRoot));
            if (elementToSnapshot == null) throw new ArgumentNullException(nameof(elementToSnapshot));

            // Ensure layout is valid before we read sizes
            elementToSnapshot.UpdateLayout();
            var size = elementToSnapshot.RenderSize;
            if (size.Width <= 0 || size.Height <= 0)
                size = new Size(Math.Max(1, size.Width), Math.Max(1, size.Height));

            // Render to bitmap
            int pixelWidth = (int)Math.Ceiling(size.Width * scale);
            int pixelHeight = (int)Math.Ceiling(size.Height * scale);
            if (pixelWidth <= 0) pixelWidth = 1;
            if (pixelHeight <= 0) pixelHeight = 1;

            // Use 96 DPI; you can read actual DPI from PresentationSource if you prefer
            var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(elementToSnapshot);
                dc.DrawRectangle(vb, null, new Rect(0, 0, pixelWidth, pixelHeight));
            }
            rtb.Render(dv);

            return new DragAdorner(adornerRoot, rtb, size, opacity);
        }

        public void SetPosition(double left, double top)
        {
            _left = left;
            _top = top;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_snapshot == null) return;

            var rect = new Rect(_left, _top, _width, _height);
            dc.PushOpacity(_opacity);
            dc.DrawImage(_snapshot, rect);
            dc.Pop();
        }
    }

    #endregion Drag Adorner

    #region Reorderable Listbox Behavior

    /// <summary>
    /// Behavior to bind OrderableListBox events to ViewModel commands
    /// </summary>
    public class ReorderableListBoxBehavior : Behavior<ReorderableListBox>
    {
        #region Dependency Properties

        public static readonly DependencyProperty ItemDragStartingCommandProperty =
            DependencyProperty.Register(nameof(ItemDragStartingCommand), typeof(ICommand),
                typeof(ReorderableListBoxBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty ItemReorderedCommandProperty =
            DependencyProperty.Register(nameof(ItemReorderedCommand), typeof(ICommand),
                typeof(ReorderableListBoxBehavior), new PropertyMetadata(null));

        /// <summary>
        /// Command executed when item drag is starting. 
        /// CommandParameter: ItemDragEventArgs (set Cancel = true to prevent drag)
        /// </summary>
        public ICommand ItemDragStartingCommand
        {
            get => (ICommand)GetValue(ItemDragStartingCommandProperty);
            set => SetValue(ItemDragStartingCommandProperty, value);
        }

        /// <summary>
        /// Command executed when item has been reordered.
        /// CommandParameter: ItemReorderedEventArgs
        /// </summary>
        public ICommand ItemReorderedCommand
        {
            get => (ICommand)GetValue(ItemReorderedCommandProperty);
            set => SetValue(ItemReorderedCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ItemDragStarting += OnItemDragStarting;
            AssociatedObject.ItemReordered += OnItemReordered;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ItemDragStarting -= OnItemDragStarting;
            AssociatedObject.ItemReordered -= OnItemReordered;
        }

        private void OnItemDragStarting(object sender, ItemDragEventArgs e)
        {
            if (ItemDragStartingCommand?.CanExecute(e) == true)
            {
                ItemDragStartingCommand.Execute(e);
            }
        }

        private void OnItemReordered(object sender, ItemReorderedEventArgs e)
        {
            if (ItemReorderedCommand?.CanExecute(e) == true)
            {
                ItemReorderedCommand.Execute(e);
            }
        }
    }

    #endregion Reorderable Listbox Behavior

    #region ReorderableListBox CustomControl
    
    /// <summary>
    /// A ListBox with built-in drag-and-drop reordering support
    /// </summary>
    public class ReorderableListBox : ListBox
    {
        #region Cursor Control

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion Cursor Control

        #region Dependency Properties

        public static readonly DependencyProperty EnableDragDropProperty =
            DependencyProperty.Register(nameof(EnableDragDrop), typeof(bool), typeof(ReorderableListBox),
                new PropertyMetadata(true));

        public static readonly DependencyProperty AutoScrollEdgeProperty =
            DependencyProperty.Register(nameof(AutoScrollEdge), typeof(double), typeof(ReorderableListBox),
                new PropertyMetadata(28.0));

        public static readonly DependencyProperty AutoScrollMinSpeedProperty =
            DependencyProperty.Register(nameof(AutoScrollMinSpeed), typeof(double), typeof(ReorderableListBox),
                new PropertyMetadata(0.5));

        public static readonly DependencyProperty AutoScrollMaxSpeedProperty =
            DependencyProperty.Register(nameof(AutoScrollMaxSpeed), typeof(double), typeof(ReorderableListBox),
                new PropertyMetadata(2.0));

        public static readonly DependencyProperty MarginAnimationDurationProperty =
            DependencyProperty.Register(nameof(MarginAnimationDuration), typeof(int), typeof(ReorderableListBox),
                new PropertyMetadata(200, OnMarginAnimationDurationChanged));

        public static readonly DependencyProperty AdornerOpacityProperty =
            DependencyProperty.Register(nameof(AdornerOpacity), typeof(double), typeof(ReorderableListBox),
                new PropertyMetadata(0.8, OnAdornerOpacityChanged));

        /// <summary>
        /// Gets or sets whether drag-and-drop reordering is enabled
        /// </summary>
        public bool EnableDragDrop
        {
            get => (bool)GetValue(EnableDragDropProperty);
            set => SetValue(EnableDragDropProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance from top/bottom edge to start auto-scrolling (in pixels)
        /// </summary>
        public double AutoScrollEdge
        {
            get => (double)GetValue(AutoScrollEdgeProperty);
            set => SetValue(AutoScrollEdgeProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum auto-scroll speed (pixels per tick)
        /// </summary>
        public double AutoScrollMinSpeed
        {
            get => (double)GetValue(AutoScrollMinSpeedProperty);
            set => SetValue(AutoScrollMinSpeedProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum auto-scroll speed (pixels per tick)
        /// </summary>
        public double AutoScrollMaxSpeed
        {
            get => (double)GetValue(AutoScrollMaxSpeedProperty);
            set => SetValue(AutoScrollMaxSpeedProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration of margin animations in milliseconds
        /// </summary>
        public int MarginAnimationDuration
        {
            get => (int)GetValue(MarginAnimationDurationProperty);
            set => SetValue(MarginAnimationDurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the opacity of the drag adorner (0.0 to 1.0)
        /// </summary>
        public double AdornerOpacity
        {
            get => (double)GetValue(AdornerOpacityProperty);
            set => SetValue(AdornerOpacityProperty, value);
        }

        #endregion

        #region Routed Events

        public static readonly RoutedEvent ItemDragStartingEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemDragStarting), RoutingStrategy.Bubble,
                typeof(EventHandler<ItemDragEventArgs>), typeof(ReorderableListBox));

        public static readonly RoutedEvent ItemReorderedEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemReordered), RoutingStrategy.Bubble,
                typeof(EventHandler<ItemReorderedEventArgs>), typeof(ReorderableListBox));

        /// <summary>
        /// Occurs when an item drag operation is starting. Set Cancel = true to prevent dragging.
        /// </summary>
        public event EventHandler<ItemDragEventArgs> ItemDragStarting
        {
            add => AddHandler(ItemDragStartingEvent, value);
            remove => RemoveHandler(ItemDragStartingEvent, value);
        }

        /// <summary>
        /// Occurs when an item has been reordered. Handle this to update your data source.
        /// </summary>
        public event EventHandler<ItemReorderedEventArgs> ItemReordered
        {
            add => AddHandler(ItemReorderedEvent, value);
            remove => RemoveHandler(ItemReorderedEvent, value);
        }

        #endregion

        #region Private Fields

        private ScrollViewer _scrollViewer;
        private DispatcherTimer _autoScrollTimer;
        private double _autoScrollVelocity;
        private Point _lastDragPointInListBox;
        private const int AutoScrollTickMs = 20;

        private Point _startPoint;
        private Point _adornerOffset;
        private ListBoxItem _draggingItem;
        private DragAdorner _adorner;
        private AdornerLayer _adornerLayer;
        private ListBoxItem _gapItem;
        private bool _gapOnTop;
        private double _draggedHeight;
        private Point _listBoxOriginInLayer;
        private bool _isOverControl;

        private bool _dragStartQueued;
        private Point _queuedMousePos;
        private object _queuedDraggedData;
        private bool _isDragInProgress;

        private DispatcherTimer _cursorConstraintTimer;

        #endregion

        #region Constructor

        static ReorderableListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ReorderableListBox),
                new FrameworkPropertyMetadata(typeof(ReorderableListBox)));
        }

        public ReorderableListBox()
        {
            AllowDrop = true;
            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            PreviewMouseMove += OnPreviewMouseMove;
            DragOver += OnDragOver;
            Drop += OnDrop;
        }

        #endregion

        #region Event Handlers

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!EnableDragDrop) return;

            _startPoint = e.GetPosition(this);

            DependencyObject subject = GetSubjectForDrag(e.OriginalSource);
            _draggingItem = VisualTreeHelperMethods.FindAncestor<ListBoxItem>(subject);

            if (_draggingItem != null)
            {
                if (SelectedItem != null && SelectedItem is ListBoxItem current)
                {
                    current.IsSelected = false;
                }
                _draggingItem.IsSelected = true;
            }

            _isOverControl = IsMouseOverControl(subject);

            if (_draggingItem != null && !_isOverControl)
            {
                _adornerOffset = e.GetPosition(_draggingItem);
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!EnableDragDrop) return;

            if (_scrollViewer == null)
                _scrollViewer = VisualTreeHelperMethods.FindVisualChild<ScrollViewer>(this);

            Point mousePos = e.GetPosition(this);
            Vector diff = _startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (_draggingItem == null || _isOverControl) return;

                object draggedData = _draggingItem.DataContext;
                if (draggedData == null) return;

                if (!_dragStartQueued)
                {
                    _dragStartQueued = true;
                    _queuedMousePos = mousePos;
                    _queuedDraggedData = draggedData;

                    QueueDragStart();
                }
            }
        }

        // Deferred drag starting to allow MouseOver/Selected states to update before creating adorner
        private async void QueueDragStart()
        {
            try
            {
                // Let IsSelected/IsMouseOver & layout/measure/arrange apply
                await Dispatcher.Yield(DispatcherPriority.Input);
                await Dispatcher.Yield(DispatcherPriority.Render);
                await Dispatcher.Yield(DispatcherPriority.Loaded);

                // Revalidate: user might have released, moved off, disabled drag, etc.
                if (!EnableDragDrop) return;
                if (_draggingItem == null) return;
                if (Mouse.LeftButton != MouseButtonState.Pressed) return;

                var draggedData = _queuedDraggedData;
                if (draggedData == null) return;

                _draggingItem.UpdateLayout();

                // Raise ItemDragStarting (allow cancel)
                var dragStartArgs = new ItemDragEventArgs(ItemDragStartingEvent, this, draggedData);
                RaiseEvent(dragStartArgs);
                if (dragStartArgs.Cancel) return;

                // Create adorner AFTER visual states have applied
                _adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (_adornerLayer != null && _adorner == null)
                {
                    _adorner = DragAdorner.FromElement(this, _draggingItem, opacity: AdornerOpacity, scale: 1.0);
                    _adornerLayer.Add(_adorner);
                    _listBoxOriginInLayer = TranslatePoint(new Point(0, 0), _adornerLayer);

                    // Position once using the queued mouse pos
                    var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
                    var constrainedPos = new Point(
                        Math.Max(bounds.Left, Math.Min(bounds.Right, _queuedMousePos.X)),
                        Math.Max(bounds.Top, Math.Min(bounds.Bottom, _queuedMousePos.Y)));
                    UpdateAdornerPosition(constrainedPos);
                }

                _draggedHeight = _draggingItem.ActualHeight;
                DragHelpers.SetIsDragged(_draggingItem, true);
                _isDragInProgress = true;

                StartCursorConstraining();

                try
                {
                    DragDrop.DoDragDrop(
                        _draggingItem,
                        new DataObject("DraggableListBoxFormat", draggedData),
                        DragDropEffects.Move);
                }
                finally
                {
                    _isDragInProgress = false;
                    StopAutoScroll();

                    StopCursorConstraining();

                    if (_adornerLayer != null && _adorner != null)
                    {
                        _adornerLayer.Remove(_adorner);
                        _adorner = null;
                    }

                    if (_draggingItem != null)
                        DragHelpers.SetIsDragged(_draggingItem, false);

                    if (ItemContainerGenerator.ContainerFromItem(draggedData) is ListBoxItem newContainer)
                        DragHelpers.SetIsDragged(newContainer, false);

                    // Reset all margins
                    foreach (var item in Items)
                        if (ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem c)
                            AnimateMargin(c, new Thickness(0), 0);
                }
            }
            finally
            {
                _dragStartQueued = false;
                _queuedDraggedData = null;
            }
        }

        // Add new MouseMove handler that works during drag operations
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragInProgress || !EnableDragDrop) return;

            Point position = e.GetPosition(this);

            // Constrain mouse position within ListBox bounds
            double constrainedY = Math.Max(0, Math.Min(ActualHeight, position.Y));
            double constrainedX = Math.Max(0, Math.Min(ActualWidth, position.X));

            Point constrainedPosition = new Point(constrainedX, constrainedY);

            _lastDragPointInListBox = constrainedPosition;

            UpdateAdornerPosition(constrainedPosition);

            var (target, insertBefore) = GetInsertionTarget(constrainedPosition);
            ShowInsertionGap(target, insertBefore);

            UpdateAutoScrollVelocity(constrainedPosition);
            if (_autoScrollVelocity != 0)
                StartAutoScroll();
            else
                StopAutoScroll();
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (!EnableDragDrop || !e.Data.GetDataPresent("DraggableListBoxFormat"))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            e.Effects = DragDropEffects.Move;

            Point position = e.GetPosition(this);
            _lastDragPointInListBox = position;
            UpdateAdornerPosition(position);

            var (target, insertBefore) = GetInsertionTarget(position);
            ShowInsertionGap(target, insertBefore);

            UpdateAutoScrollVelocity(position);
            if (_autoScrollVelocity != 0)
                StartAutoScroll();
            else
                StopAutoScroll();

            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            StopAutoScroll();

            if (!EnableDragDrop || !e.Data.GetDataPresent("DraggableListBoxFormat"))
                return;

            var droppedData = e.Data.GetData("DraggableListBoxFormat");
            var (targetItem, insertBefore) = GetInsertionTarget(e.GetPosition(this));

            int targetIdx;
            if (targetItem == null)
            {
                targetIdx = Items.Count;
            }
            else
            {
                targetIdx = ItemContainerGenerator.IndexFromContainer(targetItem);
                if (!insertBefore) targetIdx++;
            }

            int removedIdx = Items.IndexOf(droppedData);
            if (removedIdx != -1 && removedIdx < targetIdx) targetIdx--;

            if (removedIdx == targetIdx)
                goto Cleanup;

            // Raise ItemReordered event
            var reorderArgs = new ItemReorderedEventArgs(ItemReorderedEvent, this, droppedData, removedIdx, targetIdx);
            RaiseEvent(reorderArgs);

            Cleanup:
            if (_gapItem != null)
            {
                AnimateMargin(_gapItem, new Thickness(0), 0, true);
                _gapItem = null;
            }

            foreach (var item in Items)
                if (ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem c)
                    AnimateMargin(c, new Thickness(0), 0, true);

            SelectedItem = droppedData;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateLayout();
                ScrollIntoView(droppedData);
                if (ItemContainerGenerator.ContainerFromItem(droppedData) is ListBoxItem lbi)
                    lbi.Focus();
            }), DispatcherPriority.Loaded);

            e.Handled = true;
        }

        private static void OnMarginAnimationDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReorderableListBox listBox)
            {
                int newDuration = (int)e.NewValue;

                if (newDuration < 0)
                {
                    listBox.MarginAnimationDuration = 0;
                }
            }
        }

        private static void OnAdornerOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReorderableListBox listBox)
            {
                double newOpacity = (double)e.NewValue;

                // Clamp opacity between 0.0 and 1.0
                if (newOpacity < 0.0)
                {
                    listBox.AdornerOpacity = 0.0;
                }
                else if (newOpacity > 1.0)
                {
                    listBox.AdornerOpacity = 1.0;
                }

                // Note: Cannot update existing adorner opacity after creation.
                // The adorner will use the new opacity on next drag operation.
            }
        }

        #endregion

        #region Helper Methods

        private void StartCursorConstraining()
        {
            if (_cursorConstraintTimer == null)
            {
                _cursorConstraintTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(10), // Check every 10ms
                    DispatcherPriority.Input,
                    OnCursorConstraintTick,
                    Dispatcher);
            }

            if (!_cursorConstraintTimer.IsEnabled)
                _cursorConstraintTimer.Start();
        }

        private void StopCursorConstraining()
        {
            if (_cursorConstraintTimer != null && _cursorConstraintTimer.IsEnabled)
                _cursorConstraintTimer.Stop();
        }

        private void OnCursorConstraintTick(object sender, EventArgs e)
        {
            if (!_isDragInProgress) return;

            ConstrainCursorToListBox();
        }

        private void ConstrainCursorToListBox()
        {
            if (!_isDragInProgress || !IsLoaded) return;

            try
            {
                // Get current cursor position in screen coordinates
                if (!GetCursorPos(out POINT cursorPos))
                    return;

                // Get ListBox bounds in screen coordinates
                Point topLeft = PointToScreen(new Point(0, 0));
                Point bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));

                // Calculate constrained position
                int newX = cursorPos.X;
                int newY = cursorPos.Y;
                bool needsConstraint = false;

                if (cursorPos.X < topLeft.X)
                {
                    newX = (int)topLeft.X + 1;
                    needsConstraint = true;
                }
                else if (cursorPos.X > bottomRight.X)
                {
                    newX = (int)bottomRight.X - 1;
                    needsConstraint = true;
                }

                if (cursorPos.Y < topLeft.Y)
                {
                    newY = (int)topLeft.Y + 1;
                    needsConstraint = true;
                }
                else if (cursorPos.Y > bottomRight.Y)
                {
                    newY = (int)bottomRight.Y - 1;
                    needsConstraint = true;
                }

                // Move cursor back into bounds if needed
                if (needsConstraint)
                {
                    SetCursorPos(newX, newY);
                }
            }
            catch
            {
                // Silently ignore any errors
            }
        }

        private DependencyObject GetSubjectForDrag(object originalSource)
        {
            // Handle FlowDocument and Paragraph for RichTextBox
            if (originalSource is FlowDocument fd)
            {
                return fd.Parent;
            }
            else if (originalSource is Paragraph pg)
            {
                if (pg.Parent is FlowDocument fd2)
                {
                    return fd2.Parent;
                }
            }
            return originalSource as DependencyObject;
        }

        private bool IsMouseOverControl(DependencyObject originalSource)
        {
            while (originalSource != null && !(originalSource is ListBoxItem))
            {
                if (originalSource is TextBox || originalSource is ComboBox || originalSource is Button)
                {
                    return true;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }
            return false;
        }

        private (ListBoxItem item, bool before) GetInsertionTarget(Point pos)
        {
            ListBoxItem last = null;

            for (int i = 0; i < Items.Count; i++)
            {
                var c = ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (c == null || c == _draggingItem) continue;

                Rect r = c.TransformToAncestor(this)
                          .TransformBounds(new Rect(new Point(0, 0), c.RenderSize));

                if (pos.Y < r.Top + (r.Height / 2.0))
                    return (c, true);

                last = c;
            }

            return (last, false);
        }

        private void ShowInsertionGap(ListBoxItem item, bool before)
        {
            if (_gapItem == item && _gapOnTop == before) return;

            if (_gapItem != null)
                AnimateMargin(_gapItem, new Thickness(0), null, true);

            _gapItem = item;
            _gapOnTop = before;

            if (_gapItem == null) return;

            double gap = _draggedHeight > 0
                ? _draggedHeight
                : (_draggingItem?.ActualHeight > 0 ? (_draggingItem.ActualHeight + 6) : 30.0);

            AnimateMargin(_gapItem,
                before ? new Thickness(0, gap, 0, 0) : new Thickness(0, 0, 0, gap));
        }

        private void UpdateAdornerPosition(Point position)
        {
            if (_adorner == null || _draggingItem == null || _adornerLayer == null) return;

            Point inLayer = TranslatePoint(position, _adornerLayer);
            double left = _listBoxOriginInLayer.X;
            double top = inLayer.Y - _adornerOffset.Y;

            double minTop = _listBoxOriginInLayer.Y;
            double maxTop = _listBoxOriginInLayer.Y + ActualHeight - _draggingItem.ActualHeight;

            if (!double.IsNaN(maxTop) && !double.IsInfinity(maxTop))
                top = Math.Max(minTop, Math.Min(maxTop, top));

            _adorner.SetPosition(left, top);
        }

        private void AnimateMargin(FrameworkElement element, Thickness toValue, int? duration = null, bool isDrop = false)
        {
            if (element == null) return;

            int actualDuration = duration ?? MarginAnimationDuration;

            if (actualDuration == 0)
            {
                element.BeginAnimation(FrameworkElement.MarginProperty, null);
                element.Margin = toValue;
                return;
            }

            var animation = new ThicknessAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(actualDuration),
                FillBehavior = isDrop ? FillBehavior.Stop : FillBehavior.HoldEnd
            };

            if (isDrop)
            {
                animation.Completed += (s, e) =>
                {
                    element.BeginAnimation(FrameworkElement.MarginProperty, null);
                    if (toValue == new Thickness(0))
                        element.Margin = new Thickness(0);
                };
            }

            element.BeginAnimation(FrameworkElement.MarginProperty, animation);
        }

        private void UpdateAutoScrollVelocity(Point posInListBox)
        {
            if (_scrollViewer == null) { _autoScrollVelocity = 0; return; }

            double height = ActualHeight;
            double y = posInListBox.Y;
            double v = 0;

            double topDist = y;
            if (topDist <= AutoScrollEdge)
            {
                double t = 1.0 - Math.Max(0, Math.Min(1, topDist / AutoScrollEdge));
                v = -(AutoScrollMinSpeed + t * (AutoScrollMaxSpeed - AutoScrollMinSpeed));
            }

            double bottomDist = height - y;
            if (bottomDist <= AutoScrollEdge)
            {
                double t = 1.0 - Math.Max(0, Math.Min(1, bottomDist / AutoScrollEdge));
                v = (AutoScrollMinSpeed + t * (AutoScrollMaxSpeed - AutoScrollMinSpeed));
            }

            _autoScrollVelocity = v;
        }

        private void StartAutoScroll()
        {
            if (_autoScrollTimer == null)
            {
                _autoScrollTimer = new DispatcherTimer(
                    TimeSpan.FromMilliseconds(AutoScrollTickMs),
                    DispatcherPriority.Input,
                    OnAutoScrollTick,
                    Dispatcher);
            }

            if (!_autoScrollTimer.IsEnabled)
                _autoScrollTimer.Start();
        }

        private void StopAutoScroll()
        {
            if (_autoScrollTimer != null && _autoScrollTimer.IsEnabled)
                _autoScrollTimer.Stop();
            _autoScrollVelocity = 0;
        }

        private void OnAutoScrollTick(object sender, EventArgs e)
        {
            if (_scrollViewer == null || _autoScrollVelocity == 0) return;

            double current = _scrollViewer.VerticalOffset;
            double target = current + _autoScrollVelocity;

            double max = _scrollViewer.ScrollableHeight;
            if (target < 0) target = 0;
            if (target > max) target = max;

            if (Math.Abs(target - current) > 0.1)
                _scrollViewer.ScrollToVerticalOffset(target);

            UpdateAdornerPosition(_lastDragPointInListBox);

            var (targetItem, insertBefore) = GetInsertionTarget(_lastDragPointInListBox);
            ShowInsertionGap(targetItem, insertBefore);
        }

        #endregion
    }

    #endregion ReorderableListBox CustomControl

    #region Event Args

    /// <summary>
    /// Event arguments for when an item drag is starting
    /// </summary>
    public class ItemDragEventArgs : RoutedEventArgs
    {
        public ItemDragEventArgs(RoutedEvent routedEvent, object source, object item)
            : base(routedEvent, source)
        {
            Item = item;
        }

        /// <summary>
        /// The item being dragged
        /// </summary>
        public object Item { get; }

        /// <summary>
        /// Set to true to cancel the drag operation
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Event arguments for when an item has been reordered
    /// </summary>
    public class ItemReorderedEventArgs : RoutedEventArgs
    {
        public ItemReorderedEventArgs(RoutedEvent routedEvent, object source, object item, int oldIndex, int newIndex)
            : base(routedEvent, source)
        {
            Item = item;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        /// <summary>
        /// The item that was reordered
        /// </summary>
        public object Item { get; }

        /// <summary>
        /// The original index of the item
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// The new index of the item
        /// </summary>
        public int NewIndex { get; }
    }

    #endregion
}
