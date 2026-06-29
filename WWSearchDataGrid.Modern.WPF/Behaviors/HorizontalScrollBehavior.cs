using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Enables horizontal scrolling for a <see cref="ScrollViewer"/> using Shift + Mouse Wheel or a mouse tilt wheel.
    /// Shift + vertical wheel is handled through the bubbling <see cref="UIElement.PreviewMouseWheel"/> event; native
    /// tilt-wheel input (WM_MOUSEHWHEEL) is captured through a window-message hook on the ScrollViewer's
    /// <see cref="HwndSource"/>. The hook is (re)attached on load and whenever the presentation source changes, so it
    /// survives the startup race and unload/reload cycles.
    /// </summary>
    /// <remarks>
    /// Set <see cref="EnableShiftMouseWheelScrollProperty"/> to <c>true</c> to activate.
    /// Horizontal scrolling only applies while horizontal overflow exists, and the offset is clamped within
    /// <see cref="ScrollViewer.ScrollableWidth"/>.
    /// </remarks>
    public static class HorizontalScrollBehavior
    {
        #region EnableShiftMouseWheelScroll

        /// <summary>
        /// Attached property that enables Shift + Wheel and tilt-based horizontal scrolling.
        /// </summary>
        public static readonly DependencyProperty EnableShiftMouseWheelScrollProperty =
                DependencyProperty.RegisterAttached(
                    "EnableShiftMouseWheelScroll",
                    typeof(bool),
                    typeof(HorizontalScrollBehavior),
                    new PropertyMetadata(false, OnEnableShiftMouseWheelScrollChanged));

        public static bool GetEnableShiftMouseWheelScroll(DependencyObject d)
            => d.GetValue(EnableShiftMouseWheelScrollProperty) is true;

        public static void SetEnableShiftMouseWheelScroll(DependencyObject d, bool value)
            => d.SetValue(EnableShiftMouseWheelScrollProperty, value);

        private static void OnEnableShiftMouseWheelScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv) return;

            if ((bool)e.NewValue)
            {
                Debug.WriteLine($"[HScroll] EnableShiftMouseWheelScroll applied to sv='{sv.Name}' (IsLoaded={sv.IsLoaded})");

                // Shift + vertical wheel routes through the element itself.
                sv.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;

                // Tilt wheel arrives as a Win32 message and needs a hook on the hosting HwndSource.
                // Attach on every signal that the source might now be available — whichever fires
                // first wins, the rest are idempotent.
                sv.Loaded += ScrollViewer_Loaded;
                sv.Unloaded += ScrollViewer_Unloaded;
                PresentationSource.AddSourceChangedHandler(sv, ScrollViewer_SourceChanged);

                if (sv.IsLoaded)
                    TryAttach(sv);
            }
            else
            {
                sv.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                sv.Loaded -= ScrollViewer_Loaded;
                sv.Unloaded -= ScrollViewer_Unloaded;
                PresentationSource.RemoveSourceChangedHandler(sv, ScrollViewer_SourceChanged);
                TryDetach(sv);
            }
        }

        // Shift + standard mouse wheel → horizontal scroll
        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv ||
                !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ||
                sv.ExtentWidth <= sv.ViewportWidth)
                return;

            double delta = -e.Delta * SystemParameters.WheelScrollLines / 15.0;
            sv.ScrollToHorizontalOffset(
                Math.Clamp(sv.HorizontalOffset + delta, 0, sv.ScrollableWidth));
            e.Handled = true;
        }

        private static void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer sv) TryAttach(sv);
        }

        private static void ScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer sv) TryDetach(sv);
        }

        private static void ScrollViewer_SourceChanged(object sender, SourceChangedEventArgs e)
        {
            if (sender is ScrollViewer sv) TryAttach(sv);
        }

        #region Hook attach/detach (refcounted per source, keyed per ScrollViewer)

        // Which source each enabled ScrollViewer is currently counted against, and the live hook
        // count per source. Keeping the per-ScrollViewer mapping makes TryAttach idempotent — the
        // Loaded / SourceChanged / immediate paths can all call it without double-counting.
        private static readonly Dictionary<ScrollViewer, HwndSource> attachedSourceForSv = new();
        private static readonly Dictionary<HwndSource, int> hookRefCount = new();

        private static void TryAttach(ScrollViewer sv)
        {
            var src = ResolveHwndSource(sv);
            if (src == null)
            {
                Debug.WriteLine($"[HScroll] TryAttach: no HwndSource yet (sv='{sv.Name}', IsLoaded={sv.IsLoaded})");
                // No source yet (or it went away) — drop any stale attachment; a later Loaded /
                // SourceChanged will bring us back once a source exists.
                TryDetach(sv);
                return;
            }

            if (attachedSourceForSv.TryGetValue(sv, out var existing))
            {
                if (ReferenceEquals(existing, src))
                    return; // already hooked against this source
                TryDetach(sv); // moved to a different source — release the old one first
            }

            attachedSourceForSv[sv] = src;
            int count = hookRefCount.TryGetValue(src, out var c) ? c : 0;
            hookRefCount[src] = count + 1;
            if (count == 0)
            {
                src.AddHook(HwndSourceHook);
                Debug.WriteLine($"[HScroll] hook ATTACHED to hwnd=0x{src.Handle.ToInt64():X} (sv='{sv.Name}')");
            }
            else
            {
                Debug.WriteLine($"[HScroll] sv='{sv.Name}' joined existing hook on hwnd=0x{src.Handle.ToInt64():X} (refcount={count + 1})");
            }
        }

        private static void TryDetach(ScrollViewer sv)
        {
            if (!attachedSourceForSv.TryGetValue(sv, out var src))
                return;

            attachedSourceForSv.Remove(sv);
            if (!hookRefCount.TryGetValue(src, out var count))
                return;

            if (count <= 1)
            {
                hookRefCount.Remove(src);
                src.RemoveHook(HwndSourceHook);
                Debug.WriteLine($"[HScroll] hook DETACHED from hwnd=0x{src.Handle.ToInt64():X}");
            }
            else
            {
                hookRefCount[src] = count - 1;
            }
        }

        private static HwndSource ResolveHwndSource(ScrollViewer sv)
        {
            if (PresentationSource.FromVisual(sv) is HwndSource direct)
                return direct;

            var window = Window.GetWindow(sv);
            if (window != null && PresentationSource.FromVisual(window) is HwndSource viaWindow)
                return viaWindow;

            return null;
        }

        #endregion

        private const int WM_MOUSEHWHEEL = 0x020E;

        // Vertical wheel — not handled here; logged only so diagnostics can prove the hook is
        // receiving wheel traffic at all. If WM_MOUSEWHEEL logs but WM_MOUSEHWHEEL never does,
        // the tilt message isn't reaching this window (a driver/routing issue, not this code).
        private const int WM_MOUSEWHEEL = 0x020A;

        private static IntPtr HwndSourceHook(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_MOUSEHWHEEL)
            {
                int delta = (short)((wParam.ToInt64() >> 16) & 0xffff);

                // lParam carries the cursor position in screen coordinates (signed shorts, so
                // negative values on multi-monitor setups survive).
                var screenPoint = new Point(
                    (short)(lParam.ToInt64() & 0xffff),
                    (short)((lParam.ToInt64() >> 16) & 0xffff));

                var sv = FindRegisteredScrollViewerAt(screenPoint);

                if (sv != null && sv.ExtentWidth > sv.ViewportWidth)
                {
                    // adjust horizontal offset
                    double offset = sv.HorizontalOffset + (delta * SystemParameters.WheelScrollLines / 15.0);
                    sv.ScrollToHorizontalOffset(
                        Math.Clamp(offset, 0, sv.ScrollableWidth));
                    handled = true;
                    Debug.WriteLine($"[HScroll] -> scrolled, HorizontalOffset now {sv.HorizontalOffset:0.#}");
                }
            }
            return IntPtr.Zero;
        }

        // Finds the enabled ScrollViewer under the given screen point by checking the point
        // against each registered ScrollViewer's bounds. Avoids visual-tree hit-testing
        // entirely — transparent regions, custom chrome, and capture state can all defeat a
        // root-down HitTest, and only opted-in ScrollViewers should receive tilt input anyway.
        // When registered ScrollViewers are nested, the smallest containing one wins.
        private static ScrollViewer FindRegisteredScrollViewerAt(Point screenPoint)
        {
            ScrollViewer best = null;
            double bestArea = double.MaxValue;

            foreach (var sv in attachedSourceForSv.Keys)
            {
                if (!sv.IsVisible || PresentationSource.FromVisual(sv) == null)
                    continue;

                Point local;
                try
                {
                    local = sv.PointFromScreen(screenPoint);
                }
                catch (InvalidOperationException)
                {
                    continue; // detached from its source between checks
                }

                if (local.X < 0 || local.Y < 0 || local.X >= sv.ActualWidth || local.Y >= sv.ActualHeight)
                    continue;

                double area = sv.ActualWidth * sv.ActualHeight;
                if (area < bestArea)
                {
                    best = sv;
                    bestArea = area;
                }
            }

            return best;
        }

        #endregion
    }
}
