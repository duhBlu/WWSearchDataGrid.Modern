using System;
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
    /// When enabled, vertical wheel input with Shift or native WM_MOUSEHWHEEL messages scroll horizontally
    /// (only when horizontal overflow exists). The window hook is attached on load and removed on unload.
    /// </summary>
    /// <remarks>
    /// Set <see cref="EnableShiftMouseWheelScrollProperty"/> to <c>true</c> to activate.
    /// Automatically clamps the offset within <see cref="ScrollViewer.ScrollableWidth"/>.
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
                // vertical wheel + Shift
                sv.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;

                // hook window messages for horizontal tilt
                sv.Loaded += ScrollViewer_Loaded;
                sv.Unloaded += ScrollViewer_Unloaded;
            }
            else
            {
                sv.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                sv.Loaded -= ScrollViewer_Loaded;
                sv.Unloaded -= ScrollViewer_Unloaded;
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

        // When loaded, attach our HwndSource hook to catch WM_MOUSEHWHEEL
        private static void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            var window = Window.GetWindow(sv);
            if (window == null) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var src = HwndSource.FromHwnd(hwnd);
            if (src != null)
                src.AddHook(HwndSourceHook);
        }

        private static void ScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            var window = Window.GetWindow(sv);
            if (window == null) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var src = HwndSource.FromHwnd(hwnd);
            if (src != null)
                src.RemoveHook(HwndSourceHook);
        }

        private const int WM_MOUSEHWHEEL = 0x020E;

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
                var sv = GetScrollViewerUnderMouse();
                if (sv != null && sv.ExtentWidth > sv.ViewportWidth)
                {
                    // adjust horizontal offset
                    double offset = sv.HorizontalOffset + (delta * SystemParameters.WheelScrollLines / 15.0);
                    sv.ScrollToHorizontalOffset(
                        Math.Clamp(offset, 0, sv.ScrollableWidth));
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private static ScrollViewer GetScrollViewerUnderMouse()
        {
            // hit-test the window under the pointer
            var win = Application.Current.Windows
                       .OfType<Window>()
                       .FirstOrDefault(w => w.IsMouseOver);
            if (win == null) return null;

            var pos = Mouse.GetPosition(win);
            var result = VisualTreeHelper.HitTest(win, pos);
            if (result == null) return null;

            var current = result.VisualHit;
            while (current != null)
            {
                if (current is ScrollViewer sv) return sv;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        #endregion
    }
}
