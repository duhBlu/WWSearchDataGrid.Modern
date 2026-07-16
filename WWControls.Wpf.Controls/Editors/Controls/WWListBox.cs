using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace WWControls.Wpf.Controls.Editors
{
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

    #region WWListBox Behavior

    /// <summary>
    /// Behavior to bind WWListBox reorder events to ViewModel commands
    /// </summary>
    public class WWListBoxBehavior : Behavior<WWListBox>
    {
        #region Dependency Properties

        public static readonly DependencyProperty ItemDragStartingCommandProperty =
            DependencyProperty.Register(nameof(ItemDragStartingCommand), typeof(ICommand),
                typeof(WWListBoxBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty ItemReorderedCommandProperty =
            DependencyProperty.Register(nameof(ItemReorderedCommand), typeof(ICommand),
                typeof(WWListBoxBehavior), new PropertyMetadata(null));

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

    #endregion WWListBox Behavior

    #region WWListBox CustomControl

    /// <summary>
    /// A ListBox with a selection-glyph item surface and built-in drag-and-drop reordering.
    ///
    /// Selection behavior is the platform's: SelectionMode=Single, Multiple (plain click
    /// toggles), or Extended (Ctrl/Shift multi-select). <see cref="ItemKind"/> is the visual
    /// counterpart — Default rows read selection as the row highlight, Checked rows carry a
    /// checkbox, Radio rows a radio dot, each lit by the container's IsSelected.
    ///
    /// Reordering (opt-in via <see cref="AllowReorder"/>) runs on plain mouse capture, not OLE
    /// drag-drop, so move events keep flowing at full rate even when the pointer leaves the
    /// control - the edges need no special handling, and there is no system cursor/focus
    /// interference to fight.
    ///
    /// The visuals use a "traveling hole": the dragged item keeps its layout slot but is
    /// hidden, a bitmap ghost follows the pointer, and the other items slide between slots
    /// with animated RenderTransforms. Layout never runs during a drag, so the slot
    /// geometry the targeting math reads cannot shift underneath it, and the list's total
    /// height never changes - the gap is the dragged item's vacated slot, always visible,
    /// at the edges exactly like in the middle.
    ///
    /// Targeting keeps one integer: the hole's slot index, chosen as the slot whose
    /// resting position is nearest the ghost's center. All candidate positions are
    /// computed once from the frozen layout, so the choice is deterministic and cannot
    /// oscillate with the slide animations.
    ///
    /// Containers must be realized for the whole list when a drag starts (reorder lists
    /// are short by nature); with virtualized-away containers the drag simply won't start.
    /// The control therefore defaults VirtualizingPanel.IsVirtualizing to false - a scrolled
    /// virtualizing list would otherwise silently refuse every drag and fall back to the
    /// ListBox's native swipe-select. Re-enable it in XAML only if a list is too large to
    /// realize, accepting that reordering stops working there.
    /// </summary>
    public class WWListBox : ListBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty ItemKindProperty =
            DependencyProperty.Register(nameof(ItemKind), typeof(ListBoxItemKind), typeof(WWListBox),
                new PropertyMetadata(ListBoxItemKind.Default, OnItemKindChanged));

        public static readonly DependencyProperty AllowReorderProperty =
            DependencyProperty.Register(nameof(AllowReorder), typeof(bool), typeof(WWListBox),
                new PropertyMetadata(false, OnAllowReorderChanged));

        public static readonly DependencyProperty AutoScrollEdgeProperty =
            DependencyProperty.Register(nameof(AutoScrollEdge), typeof(double), typeof(WWListBox),
                new PropertyMetadata(28.0));

        public static readonly DependencyProperty AutoScrollMinSpeedProperty =
            DependencyProperty.Register(nameof(AutoScrollMinSpeed), typeof(double), typeof(WWListBox),
                new PropertyMetadata(2.0));

        public static readonly DependencyProperty AutoScrollMaxSpeedProperty =
            DependencyProperty.Register(nameof(AutoScrollMaxSpeed), typeof(double), typeof(WWListBox),
                new PropertyMetadata(10.0));

        public static readonly DependencyProperty ReorderAnimationDurationProperty =
            DependencyProperty.Register(nameof(ReorderAnimationDuration), typeof(int), typeof(WWListBox),
                new PropertyMetadata(200, OnReorderAnimationDurationChanged));

        public static readonly DependencyProperty AdornerOpacityProperty =
            DependencyProperty.Register(nameof(AdornerOpacity), typeof(double), typeof(WWListBox),
                new PropertyMetadata(0.8, OnAdornerOpacityChanged));

        private static readonly DependencyPropertyKey IsDraggingPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsDragging), typeof(bool), typeof(WWListBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets or sets the selection glyph the item containers render (none / checkbox / radio).
        /// Visual only — selection behavior stays with <see cref="ListBox.SelectionMode"/>.
        /// </summary>
        public ListBoxItemKind ItemKind
        {
            get => (ListBoxItemKind)GetValue(ItemKindProperty);
            set => SetValue(ItemKindProperty, value);
        }

        /// <summary>
        /// Gets or sets whether drag-and-drop reordering is enabled
        /// </summary>
        public bool AllowReorder
        {
            get => (bool)GetValue(AllowReorderProperty);
            set => SetValue(AllowReorderProperty, value);
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
        /// Gets or sets the minimum auto-scroll speed (pixels per 20ms tick)
        /// </summary>
        public double AutoScrollMinSpeed
        {
            get => (double)GetValue(AutoScrollMinSpeedProperty);
            set => SetValue(AutoScrollMinSpeedProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum auto-scroll speed (pixels per 20ms tick)
        /// </summary>
        public double AutoScrollMaxSpeed
        {
            get => (double)GetValue(AutoScrollMaxSpeedProperty);
            set => SetValue(AutoScrollMaxSpeedProperty, value);
        }

        /// <summary>
        /// Gets or sets the duration of the reorder slide animation in milliseconds.
        /// </summary>
        public int ReorderAnimationDuration
        {
            get => (int)GetValue(ReorderAnimationDurationProperty);
            set => SetValue(ReorderAnimationDurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the opacity of the drag ghost (0.0 to 1.0)
        /// </summary>
        public double AdornerOpacity
        {
            get => (double)GetValue(AdornerOpacityProperty);
            set => SetValue(AdornerOpacityProperty, value);
        }

        /// <summary>
        /// Gets whether an item is currently being dragged
        /// </summary>
        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            private set => SetValue(IsDraggingPropertyKey, value);
        }

        #endregion

        #region Routed Events

        public static readonly RoutedEvent ItemDragStartingEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemDragStarting), RoutingStrategy.Bubble,
                typeof(EventHandler<ItemDragEventArgs>), typeof(WWListBox));

        public static readonly RoutedEvent ItemReorderedEvent =
            EventManager.RegisterRoutedEvent(nameof(ItemReordered), RoutingStrategy.Bubble,
                typeof(EventHandler<ItemReorderedEventArgs>), typeof(WWListBox));

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

        private enum DragState
        {
            Idle,
            Pending,   // mouse down on an item, below the drag threshold
            Dragging   // mouse captured, ghost active
        }

        private DragState _state;

        // Pending
        private Point _pendingStartPoint;   // listbox space
        private ListBoxItem _pendingContainer;
        private double _grabOffsetY;        // pointer offset within the dragged item

        // Dragging
        private object _draggedData;
        private ListBoxItem _draggedContainer;
        private int _draggedIndex;
        private int _holeIndex;
        private double _draggedHeight;

        // Frozen layout snapshot (items-panel space), taken once at drag start
        private Panel _itemsPanel;
        private double[] _holeCenters;      // ghost-center resting position per hole slot
        private List<ListBoxItem> _containers;
        private Dictionary<ListBoxItem, Transform> _originalTransforms;

        // Ghost
        private DragAdorner _adorner;
        private AdornerLayer _adornerLayer;

        // Auto-scroll
        private ScrollViewer _scrollViewer;
        private DispatcherTimer _autoScrollTimer;
        private double _autoScrollVelocity;
        private Point _lastPointInListBox;
        private const int AutoScrollTickMs = 20;

        #endregion

        #region Constructor

        static WWListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWListBox),
                new FrameworkPropertyMetadata(typeof(WWListBox)));
            VirtualizingPanel.IsVirtualizingProperty.OverrideMetadata(typeof(WWListBox),
                new FrameworkPropertyMetadata(false));
        }

        public WWListBox()
        {
            // Pixel scrolling is required for reordering: with item scrolling
            // (CanContentScroll = true) the items panel re-arranges its children on every
            // scroll, which would invalidate the frozen slot snapshot mid-drag and detach
            // the gap from the pointer during auto-scroll. ScrollViewer.* attached
            // properties inherit, so an app-style setter on the control would otherwise
            // flow to the inner ScrollViewer; a local value here wins over style setters.
            SetValue(ScrollViewer.CanContentScrollProperty, false);

            PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            PreviewMouseMove += OnPreviewMouseMove;
            PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            LostMouseCapture += OnLostMouseCapture;
        }

        #endregion

        #region Item Containers

        protected override DependencyObject GetContainerForItemOverride() => new WWListBoxItem();

        protected override bool IsItemItsOwnContainerOverride(object item) => item is ListBoxItem;

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is WWListBoxItem container)
                container.SetKind(ItemKind);
        }

        private static void OnItemKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWListBox listBox)
                listBox.RefreshContainerKinds();
        }

        // Push the current kind onto every realized container. Recycled or newly realized
        // containers pick it up in PrepareContainerForItemOverride.
        private void RefreshContainerKinds()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is WWListBoxItem container)
                    container.SetKind(ItemKind);
            }
        }

        #endregion

        #region Drag Lifecycle

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!AllowReorder || _state != DragState.Idle) return;

            DependencyObject subject = GetSubjectForDrag(e.OriginalSource);
            var container = FindAncestor<ListBoxItem>(subject);

            if (container == null || IsMouseOverControl(subject)) return;
            if (ItemContainerGenerator.IndexFromContainer(container) < 0) return;

            // Only force-select in Single mode. In Multiple/Extended the native handlers
            // own the click semantics (toggle / Ctrl / Shift) — pre-selecting here would
            // invert the toggle for unselected items.
            if (SelectionMode == SelectionMode.Single)
                container.IsSelected = true;

            _state = DragState.Pending;
            _pendingContainer = container;
            _pendingStartPoint = e.GetPosition(this);
            _grabOffsetY = e.GetPosition(container).Y;
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_state == DragState.Pending)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    _state = DragState.Idle;
                    _pendingContainer = null;
                    return;
                }

                Vector diff = _pendingStartPoint - e.GetPosition(this);
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartDrag();
                }
            }
            else if (_state == DragState.Dragging)
            {
                UpdateDrag(e.GetPosition(this));
                e.Handled = true;
            }
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_state == DragState.Pending)
            {
                _state = DragState.Idle;
                _pendingContainer = null;
                return;
            }

            if (_state != DragState.Dragging) return;

            var data = _draggedData;
            var from = _draggedIndex;
            var to = _holeIndex;

            CleanupDrag();

            if (from != to)
                RaiseEvent(new ItemReorderedEventArgs(ItemReorderedEvent, this, data, from, to));

            // Restoring SelectedItem would clear a multi-selection; only Single mode
            // re-asserts the dragged item as the selection.
            if (SelectionMode == SelectionMode.Single)
                SelectedItem = data;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateLayout();
                ScrollIntoView(data);
                if (ItemContainerGenerator.ContainerFromItem(data) is ListBoxItem lbi)
                    lbi.Focus();
            }), DispatcherPriority.Loaded);

            e.Handled = true;
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            // Capture stolen mid-drag (alt-tab, a dialog, ...): cancel. Commit and cancel
            // paths set the state to Idle before releasing, so they don't re-enter here.
            if (_state == DragState.Dragging)
                CleanupDrag();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (_state != DragState.Dragging) return;

            if (e.Key == Key.Escape)
                CleanupDrag();

            // Swallow everything else too: keyboard navigation would move the
            // selection and scroll position underneath the drag.
            e.Handled = true;
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            // The frozen slot snapshot is invalid if the list changes under the drag.
            if (_state == DragState.Dragging)
                CleanupDrag();

            _state = DragState.Idle;
            _pendingContainer = null;
        }

        private void StartDrag()
        {
            var container = _pendingContainer;
            _state = DragState.Idle;
            _pendingContainer = null;

            // The ListBox grabbed SUBTREE capture on mouse down for its native
            // swipe-select. Release it before capturing again: capturing an element
            // that already holds capture does not change the capture mode, and under
            // subtree capture hit-testing stays live, so the items under the ghost
            // keep receiving MouseEnter and select themselves as the pointer passes.
            // Releasing here also covers every failed start below - a drag gesture
            // must never degrade into the native selection-follows-pointer behavior.
            if (Mouse.Captured == this)
                ReleaseMouseCapture();

            if (container == null || Items.Count == 0) return;

            var index = ItemContainerGenerator.IndexFromContainer(container);
            if (index < 0) return;

            var data = Items[index];

            var dragStartArgs = new ItemDragEventArgs(ItemDragStartingEvent, this, data);
            RaiseEvent(dragStartArgs);
            if (dragStartArgs.Cancel) return;

            // The slot snapshot needs every container realized.
            var containers = new List<ListBoxItem>(Items.Count);
            for (int i = 0; i < Items.Count; i++)
            {
                if (!(ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem c)) return;
                containers.Add(c);
            }

            _itemsPanel = VisualTreeHelper.GetParent(container) as Panel;
            if (_itemsPanel == null) return;

            _adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (_adornerLayer == null) return;

            container.UpdateLayout();
            _draggedHeight = container.RenderSize.Height;
            if (_draggedHeight <= 0) return;

            // Freeze the slot geometry (items-panel space, so scrolling doesn't move it)
            // and precompute the ghost-center resting position for each hole slot: below
            // the original slot the hole sits at the end of the items that slid up, above
            // it at the start of the items that slid down. The hole always has the dragged
            // item's height, so this is exact for mixed-height items too.
            var count = containers.Count;
            _holeCenters = new double[count];
            for (int h = 0; h < count; h++)
            {
                double top = containers[h].TranslatePoint(new Point(0, 0), _itemsPanel).Y;
                double holeTop = h <= index ? top : top + containers[h].RenderSize.Height - _draggedHeight;
                _holeCenters[h] = holeTop + _draggedHeight / 2.0;
            }

            // Guarantee the ghost snapshot shows the row's real selected state. A fast
            // click-then-drag can reach here in the same input cycle the row was selected in:
            // the mouse-down selection may not have committed yet (leaving the previously
            // selected row highlighted and this one looking plain) and, even once it has, the
            // selection background may not have rendered - either way the snapshot would bake
            // an unselected-looking ghost. Re-assert the selection (Single mode) and flush a
            // render pass so the bitmap captures the highlighted row. The flush runs at Render
            // priority (above Input), so it pumps only the pending render, never more input,
            // and IsSelected drives a render-only trigger, so the frozen slot geometry holds.
            if (SelectionMode == SelectionMode.Single)
                container.IsSelected = true;
            Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));

            // Snapshot the ghost while the row is still visible.
            _adorner = DragAdorner.FromElement(this, container, opacity: AdornerOpacity, scale: 1.0);
            _adornerLayer.Add(_adorner);

            _containers = containers;
            _originalTransforms = new Dictionary<ListBoxItem, Transform>();
            foreach (var c in containers)
            {
                _originalTransforms[c] = c.RenderTransform;
                c.RenderTransform = new TranslateTransform();
            }

            _draggedContainer = container;
            _draggedData = data;
            _draggedIndex = index;
            _holeIndex = index;

            // Hide in place: the container keeps its layout slot - that slot IS the gap.
            _draggedContainer.Opacity = 0;
            _draggedContainer.IsHitTestVisible = false;

            _scrollViewer = FindVisualChild<ScrollViewer>(this);

            _state = DragState.Dragging;
            IsDragging = true;

            Focus();
            CaptureMouse();

            UpdateDrag(Mouse.GetPosition(this));
        }

        private void UpdateDrag(Point posInListBox)
        {
            _lastPointInListBox = posInListBox;

            UpdateGhostPosition(posInListBox);
            UpdateHoleFromPoint(posInListBox);

            UpdateAutoScrollVelocity(posInListBox);
            if (_autoScrollVelocity != 0)
                StartAutoScroll();
            else
                StopAutoScroll();
        }

        // Moves the hole to the slot whose resting position is nearest the ghost's center.
        // All candidates come from the frozen snapshot, so the slide animations can never
        // feed back into this choice.
        private void UpdateHoleFromPoint(Point posInListBox)
        {
            Point panelPos = TranslatePoint(posInListBox, _itemsPanel);
            double ghostCenter = panelPos.Y - _grabOffsetY + _draggedHeight / 2.0;

            int target = 0;
            double best = double.MaxValue;
            for (int h = 0; h < _holeCenters.Length; h++)
            {
                double distance = Math.Abs(ghostCenter - _holeCenters[h]);
                if (distance < best)
                {
                    best = distance;
                    target = h;
                }
            }

            if (target == _holeIndex) return;

            _holeIndex = target;
            UpdateSlideTargets();
        }

        // Items between the original slot and the hole slide by exactly the hole's height;
        // everything else sits at rest.
        private void UpdateSlideTargets()
        {
            for (int i = 0; i < _containers.Count; i++)
            {
                if (i == _draggedIndex) continue;

                double offset = 0;
                if (_draggedIndex < _holeIndex && i > _draggedIndex && i <= _holeIndex)
                    offset = -_draggedHeight;
                else if (_holeIndex < _draggedIndex && i >= _holeIndex && i < _draggedIndex)
                    offset = _draggedHeight;

                AnimateSlide(_containers[i], offset);
            }
        }

        private void AnimateSlide(ListBoxItem container, double to)
        {
            if (!(container.RenderTransform is TranslateTransform transform)) return;

            int duration = Math.Max(0, ReorderAnimationDuration);
            if (duration == 0)
            {
                transform.BeginAnimation(TranslateTransform.YProperty, null);
                transform.Y = to;
                return;
            }

            var animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(duration))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            transform.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        // The adorner renders in the adorned element's (this listbox's) coordinate space —
        // the adorner layer applies the element-to-layer transform itself. Positioning is
        // therefore pure listbox space; adding the listbox's origin in the layer would
        // double-count the offset in any host that doesn't wrap the list in its own
        // AdornerDecorator.
        private void UpdateGhostPosition(Point posInListBox)
        {
            if (_adorner == null) return;

            double top = posInListBox.Y - _grabOffsetY;
            double maxTop = ActualHeight - _draggedHeight;
            if (maxTop >= 0)
                top = Math.Max(0, Math.Min(maxTop, top));

            _adorner.SetPosition(0, top);
        }

        // Tears down the drag and restores every container. Sets the state to Idle FIRST
        // so the capture release below can't re-enter through OnLostMouseCapture.
        private void CleanupDrag()
        {
            _state = DragState.Idle;
            IsDragging = false;
            StopAutoScroll();

            if (IsMouseCaptured)
                ReleaseMouseCapture();

            if (_adornerLayer != null && _adorner != null)
                _adornerLayer.Remove(_adorner);
            _adorner = null;
            _adornerLayer = null;

            if (_containers != null)
            {
                foreach (var c in _containers)
                {
                    if (c.RenderTransform is TranslateTransform t)
                        t.BeginAnimation(TranslateTransform.YProperty, null);
                    c.RenderTransform = _originalTransforms != null && _originalTransforms.TryGetValue(c, out var original)
                        ? original
                        : null;
                }
            }

            if (_draggedContainer != null)
            {
                _draggedContainer.ClearValue(OpacityProperty);
                _draggedContainer.ClearValue(IsHitTestVisibleProperty);
            }

            _containers = null;
            _originalTransforms = null;
            _draggedContainer = null;
            _draggedData = null;
            _holeCenters = null;
            _itemsPanel = null;
            _scrollViewer = null;
        }

        private static void OnAllowReorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Disabling mid-drag must not leave a ghost or hidden container behind.
            if (d is WWListBox listBox && !(bool)e.NewValue && listBox._state == DragState.Dragging)
                listBox.CleanupDrag();
        }

        private static void OnReorderAnimationDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWListBox listBox && (int)e.NewValue < 0)
            {
                listBox.ReorderAnimationDuration = 0;
            }
        }

        private static void OnAdornerOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWListBox listBox)
            {
                double newOpacity = (double)e.NewValue;

                if (newOpacity < 0.0)
                {
                    listBox.AdornerOpacity = 0.0;
                }
                else if (newOpacity > 1.0)
                {
                    listBox.AdornerOpacity = 1.0;
                }

                if (listBox._adorner != null)
                {
                    listBox._adorner.Opacity = listBox.AdornerOpacity;
                }
            }
        }

        #endregion

        #region Auto Scroll

        private void UpdateAutoScrollVelocity(Point posInListBox)
        {
            if (_scrollViewer == null || _scrollViewer.ScrollableHeight <= 0)
            {
                _autoScrollVelocity = 0;
                return;
            }

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
            if (_state != DragState.Dragging || _scrollViewer == null || _autoScrollVelocity == 0) return;

            double current = _scrollViewer.VerticalOffset;
            double target = current + _autoScrollVelocity;

            double max = _scrollViewer.ScrollableHeight;
            if (target < 0) target = 0;
            if (target > max) target = max;

            if (Math.Abs(target - current) > 0.1)
                _scrollViewer.ScrollToVerticalOffset(target);

            // The content moved under a stationary pointer; re-evaluate against the same
            // listbox-space point (the slot snapshot lives in panel space, so it is still
            // valid - only the pointer-to-panel mapping changed).
            UpdateDrag(_lastPointInListBox);
        }

        #endregion

        #region Helper Methods

        private DependencyObject GetSubjectForDrag(object originalSource)
        {
            // If the source is a content element (Run, Paragraph, FlowDocument, etc.),
            // walk up through the logical tree to find the Visual host (e.g. RichTextBox)
            if (originalSource is FrameworkContentElement)
            {
                DependencyObject current = originalSource as DependencyObject;
                while (current is FrameworkContentElement fce)
                {
                    current = fce.Parent;
                }
                return current ?? originalSource as DependencyObject;
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
                if (originalSource is Visual || originalSource is System.Windows.Media.Media3D.Visual3D)
                    originalSource = VisualTreeHelper.GetParent(originalSource);
                else
                    originalSource = LogicalTreeHelper.GetParent(originalSource);
            }
            return false;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
            {
                if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                    current = VisualTreeHelper.GetParent(current);
                else
                    current = LogicalTreeHelper.GetParent(current);
            }
            return current as T;
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                else
                {
                    var childResult = FindVisualChild<T>(child);
                    if (childResult != null)
                        return childResult;
                }
            }
            return null;
        }

        #endregion
    }

    #endregion WWListBox CustomControl

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
        /// The new index of the item (its final index after the move)
        /// </summary>
        public int NewIndex { get; }
    }

    #endregion
}
