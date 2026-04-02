using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.SampleApp.Helpers
{
    /// <summary>
    /// Provides attached properties for configuring DWM (Desktop Window Manager) features on WPF windows.
    /// Adapted from wpfCabinetDesigner.
    /// </summary>
    public static class DwmWindowHelper
    {
        #region P/Invoke - DWM

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        private enum DwmWindowAttribute
        {
            DWMWA_NCRENDERING_POLICY = 2,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR = 34,
        }

        private const int DWMNCRP_ENABLED = 2;
        private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
        private const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

        #endregion

        #region P/Invoke - Monitor/Window

        private const int WM_GETMINMAXINFO = 0x0024;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor, rcWork;
            public uint dwFlags;
        }

        #endregion

        #region Enums

        public enum WindowCornerPreference
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3
        }

        #endregion

        #region IsEnabled Attached Property

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", typeof(bool), typeof(DwmWindowHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                if ((bool)e.NewValue)
                {
                    if (window.IsLoaded)
                        InitializeWindow(window);
                    else
                        window.SourceInitialized += Window_SourceInitialized;
                }
                else
                {
                    UnsubscribeFromWindowChanges(window);
                }
            }
        }

        private static void Window_SourceInitialized(object? sender, EventArgs e)
        {
            if (sender is Window window)
            {
                window.SourceInitialized -= Window_SourceInitialized;
                InitializeWindow(window);
            }
        }

        private static void InitializeWindow(Window window)
        {
            ApplyAllSettings(window);
            SubscribeToWindowChanges(window);
            AddWndProcHook(window);
        }

        #endregion

        #region EnableDropShadow Attached Property

        public static readonly DependencyProperty EnableDropShadowProperty =
            DependencyProperty.RegisterAttached(
                "EnableDropShadow", typeof(bool), typeof(DwmWindowHelper),
                new PropertyMetadata(true, OnDropShadowPropertyChanged));

        public static bool GetEnableDropShadow(DependencyObject obj) => (bool)obj.GetValue(EnableDropShadowProperty);
        public static void SetEnableDropShadow(DependencyObject obj, bool value) => obj.SetValue(EnableDropShadowProperty, value);

        private static void OnDropShadowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && GetIsEnabled(window))
                ApplyDropShadow(window);
        }

        #endregion

        #region ShadowMargin Attached Property

        public static readonly DependencyProperty ShadowMarginProperty =
            DependencyProperty.RegisterAttached(
                "ShadowMargin", typeof(int), typeof(DwmWindowHelper),
                new PropertyMetadata(1, OnDropShadowPropertyChanged));

        public static int GetShadowMargin(DependencyObject obj) => (int)obj.GetValue(ShadowMarginProperty);
        public static void SetShadowMargin(DependencyObject obj, int value) => obj.SetValue(ShadowMarginProperty, value);

        #endregion

        #region BorderColor Attached Property

        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.RegisterAttached(
                "BorderColor", typeof(Color?), typeof(DwmWindowHelper),
                new PropertyMetadata(null, OnBorderColorPropertyChanged));

        public static Color? GetBorderColor(DependencyObject obj) => (Color?)obj.GetValue(BorderColorProperty);
        public static void SetBorderColor(DependencyObject obj, Color? value) => obj.SetValue(BorderColorProperty, value);

        private static void OnBorderColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && GetIsEnabled(window))
                ApplyBorderColor(window);
        }

        #endregion

        #region BorderColorInactive Attached Property

        public static readonly DependencyProperty BorderColorInactiveProperty =
            DependencyProperty.RegisterAttached(
                "BorderColorInactive", typeof(Color?), typeof(DwmWindowHelper),
                new PropertyMetadata(null, OnBorderColorPropertyChanged));

        public static Color? GetBorderColorInactive(DependencyObject obj) => (Color?)obj.GetValue(BorderColorInactiveProperty);
        public static void SetBorderColorInactive(DependencyObject obj, Color? value) => obj.SetValue(BorderColorInactiveProperty, value);

        #endregion

        #region CornerPreference Attached Property

        public static readonly DependencyProperty CornerPreferenceProperty =
            DependencyProperty.RegisterAttached(
                "CornerPreference", typeof(WindowCornerPreference), typeof(DwmWindowHelper),
                new PropertyMetadata(WindowCornerPreference.Default, OnCornerPreferencePropertyChanged));

        public static WindowCornerPreference GetCornerPreference(DependencyObject obj) => (WindowCornerPreference)obj.GetValue(CornerPreferenceProperty);
        public static void SetCornerPreference(DependencyObject obj, WindowCornerPreference value) => obj.SetValue(CornerPreferenceProperty, value);

        private static void OnCornerPreferencePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && GetIsEnabled(window))
                ApplyCornerPreference(window);
        }

        #endregion

        #region RespectTaskbar Attached Property

        public static readonly DependencyProperty RespectTaskbarProperty =
            DependencyProperty.RegisterAttached(
                "RespectTaskbar", typeof(bool), typeof(DwmWindowHelper),
                new PropertyMetadata(true));

        public static bool GetRespectTaskbar(DependencyObject obj) => (bool)obj.GetValue(RespectTaskbarProperty);
        public static void SetRespectTaskbar(DependencyObject obj, bool value) => obj.SetValue(RespectTaskbarProperty, value);

        #endregion

        #region Window Change Subscriptions

        private static void SubscribeToWindowChanges(Window window)
        {
            var stateDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Window.WindowStateProperty, typeof(Window));
            stateDescriptor?.AddValueChanged(window, OnWindowStateChanged);

            var activeDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Window.IsActiveProperty, typeof(Window));
            activeDescriptor?.AddValueChanged(window, OnWindowActiveChanged);
        }

        private static void UnsubscribeFromWindowChanges(Window window)
        {
            var stateDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Window.WindowStateProperty, typeof(Window));
            stateDescriptor?.RemoveValueChanged(window, OnWindowStateChanged);

            var activeDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                Window.IsActiveProperty, typeof(Window));
            activeDescriptor?.RemoveValueChanged(window, OnWindowActiveChanged);
        }

        private static void OnWindowStateChanged(object? sender, EventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyDropShadow(window);
                    ApplyCornerPreference(window);
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private static void OnWindowActiveChanged(object? sender, EventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyBorderColor(window);
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        #endregion

        #region WndProc Hook

        private static void AddWndProcHook(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            var source = HwndSource.FromHwnd(handle);
            source?.RemoveHook(WndProc);
            source?.AddHook(WndProc);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var source = HwndSource.FromHwnd(hwnd);
                if (source?.RootVisual is Window window && GetRespectTaskbar(window))
                {
                    var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                    if (monitor != IntPtr.Zero)
                    {
                        var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                        if (GetMonitorInfo(monitor, ref monitorInfo))
                        {
                            var work = monitorInfo.rcWork;
                            var mon = monitorInfo.rcMonitor;

                            mmi.ptMaxPosition.X = work.Left - mon.Left;
                            mmi.ptMaxPosition.Y = work.Top - mon.Top;
                            mmi.ptMaxSize.X = work.Right - work.Left;
                            mmi.ptMaxSize.Y = work.Bottom - work.Top;

                            mmi.ptMinTrackSize.X = (int)window.MinWidth;
                            mmi.ptMinTrackSize.Y = (int)window.MinHeight;
                            mmi.ptMaxTrackSize.X = work.Right - work.Left;
                            mmi.ptMaxTrackSize.Y = work.Bottom - work.Top;

                            Marshal.StructureToPtr(mmi, lParam, true);
                        }
                    }
                }
                handled = true;
            }

            return IntPtr.Zero;
        }

        #endregion

        #region Apply Methods

        private static void ApplyAllSettings(Window window)
        {
            ApplyDropShadow(window);
            ApplyBorderColor(window);
            ApplyCornerPreference(window);
        }

        private static IntPtr GetWindowHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }

        private static void ApplyDropShadow(Window window)
        {
            try
            {
                var handle = GetWindowHandle(window);
                if (handle == IntPtr.Zero) return;
                if (!GetEnableDropShadow(window)) return;

                int margin = window.WindowState == WindowState.Maximized ? 0 : GetShadowMargin(window);

                int val = DWMNCRP_ENABLED;
                DwmSetWindowAttribute(handle, DwmWindowAttribute.DWMWA_NCRENDERING_POLICY, ref val, sizeof(int));

                var margins = new MARGINS
                {
                    leftWidth = margin, rightWidth = margin,
                    topHeight = margin, bottomHeight = margin
                };
                DwmExtendFrameIntoClientArea(handle, ref margins);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DwmWindowHelper: Failed to apply drop shadow: {ex.Message}");
            }
        }

        private static void ApplyBorderColor(Window window)
        {
            try
            {
                var handle = GetWindowHandle(window);
                if (handle == IntPtr.Zero) return;

                Color? activeColor = GetBorderColor(window);
                Color? inactiveColor = GetBorderColorInactive(window);
                Color? colorToUse = window.IsActive ? activeColor : (inactiveColor ?? activeColor);

                int colorRef = colorToUse.HasValue ? ColorToCOLORREF(colorToUse.Value) : DWMWA_COLOR_DEFAULT;
                DwmSetWindowAttribute(handle, DwmWindowAttribute.DWMWA_BORDER_COLOR, ref colorRef, sizeof(int));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DwmWindowHelper: Failed to apply border color: {ex.Message}");
            }
        }

        private static void ApplyCornerPreference(Window window)
        {
            try
            {
                var handle = GetWindowHandle(window);
                if (handle == IntPtr.Zero) return;

                var preference = GetCornerPreference(window);
                if (window.WindowState == WindowState.Maximized)
                    preference = WindowCornerPreference.DoNotRound;

                int value = (int)preference;
                DwmSetWindowAttribute(handle, DwmWindowAttribute.DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DwmWindowHelper: Failed to apply corner preference: {ex.Message}");
            }
        }

        private static int ColorToCOLORREF(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        #endregion
    }
}
