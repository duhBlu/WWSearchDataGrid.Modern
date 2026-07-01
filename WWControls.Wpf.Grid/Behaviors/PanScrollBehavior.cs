using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// Adds middle-click autoscroll (panning) to a <see cref="ScrollViewer"/>. A middle-button
    /// click anchors an origin at the cursor; moving away from that origin scrolls the viewport
    /// in the direction of the offset, at a speed proportional to the distance. Scrolls along
    /// whichever axes have overflow, and draws an origin marker at the anchor point.
    /// </summary>
    /// <remarks>
    /// Set <see cref="EnableMiddleClickPanProperty"/> to <c>true</c> to activate. Speed is
    /// expressed in viewports per second so it behaves identically in item- and pixel-scroll
    /// modes. A press-move-release gesture pans only while the button is held; a clean click
    /// (press and release without moving) leaves panning active until the next click or Escape.
    /// </remarks>
    public static class PanScrollBehavior
    {
        #region EnableMiddleClickPan

        /// <summary>
        /// Attached property that enables middle-click autoscroll panning.
        /// </summary>
        public static readonly DependencyProperty EnableMiddleClickPanProperty =
            DependencyProperty.RegisterAttached(
                "EnableMiddleClickPan",
                typeof(bool),
                typeof(PanScrollBehavior),
                new PropertyMetadata(false, OnEnableMiddleClickPanChanged));

        public static bool GetEnableMiddleClickPan(DependencyObject d)
            => d.GetValue(EnableMiddleClickPanProperty) is true;

        public static void SetEnableMiddleClickPan(DependencyObject d, bool value)
            => d.SetValue(EnableMiddleClickPanProperty, value);

        private static void OnEnableMiddleClickPanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv) return;

            if ((bool)e.NewValue)
            {
                sv.PreviewMouseDown += ScrollViewer_PreviewMouseDown;
            }
            else
            {
                sv.PreviewMouseDown -= ScrollViewer_PreviewMouseDown;
                if (ReferenceEquals(activeScrollViewer, sv))
                    StopPan();
            }
        }

        #endregion

        #region Tuning

        // No scroll while the cursor sits within this radius of the anchor, so a stationary
        // origin doesn't drift. Matches the origin marker's ring so "inside the ring" reads as
        // "no scroll".
        private const double DeadZoneRadius = 12.0;

        // Cursor distance (px beyond the dead zone) at which pan speed saturates.
        private const double ReferenceDistance = 130.0;

        // Top speed, in viewports per second, reached at ReferenceDistance and beyond.
        private const double MaxViewportsPerSecond = 2.0;

        // A press that travels farther than this from the anchor before release is treated as
        // a held drag (pan ends on release) rather than a click (pan stays active).
        private const double StickyClickThreshold = 6.0;

        #endregion

        #region Pan Session (single active gesture — one mouse)

        private static ScrollViewer activeScrollViewer;
        private static Point anchor;
        private static bool movedBeyondThreshold;
        private static double accumulatedHorizontal;
        private static double accumulatedVertical;
        private static double lastAppliedHorizontal;
        private static double lastAppliedVertical;
        private static TimeSpan lastRenderTime;
        private static EventHandler renderHandler;
        private static Window keyHookWindow;
        private static AdornerLayer adornerLayer;
        private static PanOriginAdorner originAdorner;

        #endregion

        private static void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;

            // Any button press while a pan is active stops it (and is swallowed so the click
            // that ends panning doesn't also select or activate something underneath).
            if (activeScrollViewer != null)
            {
                StopPan();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton != MouseButton.Middle)
                return;

            // Nothing to pan — let the middle click fall through.
            if (sv.ScrollableWidth <= 0 && sv.ScrollableHeight <= 0)
                return;

            StartPan(sv, e.GetPosition(sv));
            e.Handled = true;
        }

        private static void StartPan(ScrollViewer sv, Point origin)
        {
            activeScrollViewer = sv;
            anchor = origin;
            movedBeyondThreshold = false;
            accumulatedHorizontal = sv.HorizontalOffset;
            accumulatedVertical = sv.VerticalOffset;
            lastAppliedHorizontal = Math.Round(sv.HorizontalOffset);
            lastAppliedVertical = Math.Round(sv.VerticalOffset);
            lastRenderTime = TimeSpan.Zero;

            sv.Cursor = PickCursor(sv);
            Mouse.Capture(sv);

            sv.PreviewMouseUp += ScrollViewer_PreviewMouseUp;
            sv.PreviewMouseMove += ScrollViewer_PreviewMouseMove;
            sv.LostMouseCapture += ScrollViewer_LostMouseCapture;

            keyHookWindow = Window.GetWindow(sv);
            if (keyHookWindow != null)
                keyHookWindow.PreviewKeyDown += Window_PreviewKeyDown;

            ShowOriginMarker(sv, origin);

            renderHandler = (s, e) => OnRendering((RenderingEventArgs)e);
            CompositionTarget.Rendering += renderHandler;
        }

        private static void StopPan()
        {
            var sv = activeScrollViewer;
            if (sv == null) return;

            if (renderHandler != null)
            {
                CompositionTarget.Rendering -= renderHandler;
                renderHandler = null;
            }

            sv.PreviewMouseUp -= ScrollViewer_PreviewMouseUp;
            sv.PreviewMouseMove -= ScrollViewer_PreviewMouseMove;
            sv.LostMouseCapture -= ScrollViewer_LostMouseCapture;
            sv.ClearValue(FrameworkElement.CursorProperty);

            if (Mouse.Captured == sv)
                sv.ReleaseMouseCapture();

            if (keyHookWindow != null)
            {
                keyHookWindow.PreviewKeyDown -= Window_PreviewKeyDown;
                keyHookWindow = null;
            }

            HideOriginMarker();

            activeScrollViewer = null;
        }

        private static void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (activeScrollViewer == null) return;

            var offset = e.GetPosition(activeScrollViewer) - anchor;
            if (offset.Length > StickyClickThreshold)
                movedBeyondThreshold = true;
        }

        private static void ScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (activeScrollViewer == null || e.ChangedButton != MouseButton.Middle)
                return;

            // Held drag → release ends the pan. Clean click → stay in autoscroll mode until the
            // next click or Escape.
            if (movedBeyondThreshold)
            {
                StopPan();
                e.Handled = true;
            }
        }

        private static void ScrollViewer_LostMouseCapture(object sender, MouseEventArgs e)
            => StopPan();

        private static void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && activeScrollViewer != null)
            {
                StopPan();
                e.Handled = true;
            }
        }

        private static Cursor PickCursor(ScrollViewer sv)
        {
            bool h = sv.ScrollableWidth > 0;
            bool v = sv.ScrollableHeight > 0;
            if (h && v) return Cursors.ScrollAll;
            if (v) return Cursors.ScrollNS;
            if (h) return Cursors.ScrollWE;
            return Cursors.ScrollAll;
        }

        private static void ShowOriginMarker(ScrollViewer sv, Point origin)
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(sv);
            if (adornerLayer == null) return;

            originAdorner = new PanOriginAdorner(sv, origin, sv.ScrollableWidth > 0, sv.ScrollableHeight > 0);
            adornerLayer.Add(originAdorner);
        }

        private static void HideOriginMarker()
        {
            if (adornerLayer != null && originAdorner != null)
                adornerLayer.Remove(originAdorner);

            adornerLayer = null;
            originAdorner = null;
        }

        private static void OnRendering(RenderingEventArgs e)
        {
            var sv = activeScrollViewer;
            if (sv == null) return;

            var currentTime = e.RenderingTime;
            if (lastRenderTime == TimeSpan.Zero)
            {
                lastRenderTime = currentTime;
                return;
            }
            if (currentTime == lastRenderTime) return;

            double dt = (currentTime - lastRenderTime).TotalSeconds;
            lastRenderTime = currentTime;
            if (dt > 0.1) dt = 0.1;

            var pos = Mouse.GetPosition(sv);
            double dx = pos.X - anchor.X;
            double dy = pos.Y - anchor.Y;

            if (sv.ScrollableWidth > 0)
            {
                double vps = AxisViewportsPerSecond(dx);
                if (vps != 0)
                {
                    accumulatedHorizontal = Math.Clamp(
                        accumulatedHorizontal + vps * sv.ViewportWidth * dt, 0, sv.ScrollableWidth);

                    // Apply on whole-unit changes only. A fractional ScrollToHorizontalOffset every
                    // frame re-measures the panel each tick even when nothing visibly moved.
                    double target = Math.Round(accumulatedHorizontal);
                    if (target != lastAppliedHorizontal)
                    {
                        sv.ScrollToHorizontalOffset(target);
                        lastAppliedHorizontal = target;
                    }
                }
            }

            if (sv.ScrollableHeight > 0)
            {
                double vps = AxisViewportsPerSecond(dy);
                if (vps != 0)
                {
                    accumulatedVertical = Math.Clamp(
                        accumulatedVertical + vps * sv.ViewportHeight * dt, 0, sv.ScrollableHeight);

                    double target = Math.Round(accumulatedVertical);
                    if (target != lastAppliedVertical)
                    {
                        sv.ScrollToVerticalOffset(target);
                        lastAppliedVertical = target;
                    }
                }
            }
        }

        // Maps cursor offset (px from the anchor along one axis) to a signed speed in viewports
        // per second: linear ramp from the dead zone up to ReferenceDistance, then capped.
        private static double AxisViewportsPerSecond(double axisOffset)
        {
            double over = Math.Abs(axisOffset) - DeadZoneRadius;
            if (over <= 0) return 0;

            double t = Math.Min(over / ReferenceDistance, 1.0);
            return Math.Sign(axisOffset) * t * MaxViewportsPerSecond;
        }
    }

    /// <summary>
    /// Draws the middle-click pan origin: a ring with a center dot and outward arrows on the
    /// axes that can scroll. Rendered in the adorner layer above the <see cref="ScrollViewer"/>,
    /// anchored at the click point (in the ScrollViewer's coordinate space).
    /// </summary>
    internal sealed class PanOriginAdorner : Adorner
    {
        private const double Radius = 13.0;

        private static readonly Brush FillBrush;
        private static readonly Brush GlyphBrush;
        private static readonly Pen RingPen;

        static PanOriginAdorner()
        {
            FillBrush = new SolidColorBrush(Color.FromArgb(0xE6, 0xFF, 0xFF, 0xFF));
            FillBrush.Freeze();
            GlyphBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
            GlyphBrush.Freeze();
            RingPen = new Pen(new SolidColorBrush(Color.FromArgb(0xAA, 0x33, 0x33, 0x33)), 1.0);
            RingPen.Freeze();
        }

        private readonly Point origin;
        private readonly bool horizontal;
        private readonly bool vertical;

        public PanOriginAdorner(UIElement adornedElement, Point origin, bool horizontal, bool vertical)
            : base(adornedElement)
        {
            this.origin = origin;
            this.horizontal = horizontal;
            this.vertical = vertical;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawEllipse(FillBrush, RingPen, origin, Radius, Radius);
            dc.DrawEllipse(GlyphBrush, null, origin, 2.0, 2.0);

            if (vertical)
            {
                DrawArrow(dc, 0, -1);
                DrawArrow(dc, 0, 1);
            }
            if (horizontal)
            {
                DrawArrow(dc, -1, 0);
                DrawArrow(dc, 1, 0);
            }
        }

        // Small filled triangle pointing along the unit axis (dirX, dirY), seated near the ring.
        private void DrawArrow(DrawingContext dc, double dirX, double dirY)
        {
            const double tip = 10.0;      // apex distance from center
            const double baseDist = 6.0;  // base distance from center
            const double half = 3.0;      // half-width of the base

            double perpX = dirY;
            double perpY = -dirX;

            var apex = new Point(origin.X + dirX * tip, origin.Y + dirY * tip);
            var b1 = new Point(origin.X + dirX * baseDist + perpX * half, origin.Y + dirY * baseDist + perpY * half);
            var b2 = new Point(origin.X + dirX * baseDist - perpX * half, origin.Y + dirY * baseDist - perpY * half);

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(apex, true, true);
                ctx.LineTo(b1, true, false);
                ctx.LineTo(b2, true, false);
            }
            geo.Freeze();

            dc.DrawGeometry(GlyphBrush, null, geo);
        }
    }
}
