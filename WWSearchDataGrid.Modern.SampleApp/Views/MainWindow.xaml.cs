using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.SampleApp.Services;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly UserSettings _userSettings;
        private readonly DispatcherTimer _saveDebounceTimer;
        private bool _positionTrackingEnabled;

        public MainWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));

            _userSettings = UserSettings.Load();
            _saveDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30000) };
            _saveDebounceTimer.Tick += OnSaveDebounceTick;

            ApplyRestoredWindowPosition();

            Loaded += (_, _) => _positionTrackingEnabled = true;
            LocationChanged += (_, _) => SchedulePositionSave();
            SizeChanged += (_, _) => SchedulePositionSave();
            StateChanged += (_, _) => SchedulePositionSave();
            Closing += OnClosing;
        }

        private void ApplyRestoredWindowPosition()
        {
            var pos = _userSettings.WindowPosition;
            if (pos == null || !IsWithinVirtualScreen(pos))
                return;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = pos.Left;
            Top = pos.Top;
            Width = pos.Width;
            Height = pos.Height;
            WindowState = pos.WindowState == WindowState.Minimized ? WindowState.Normal : pos.WindowState;
        }

        private static bool IsWithinVirtualScreen(WindowPositionSetting pos)
        {
            if (pos.Width <= 0 || pos.Height <= 0)
                return false;

            var screenLeft = SystemParameters.VirtualScreenLeft;
            var screenTop = SystemParameters.VirtualScreenTop;
            var screenRight = screenLeft + SystemParameters.VirtualScreenWidth;
            var screenBottom = screenTop + SystemParameters.VirtualScreenHeight;

            const double MinVisible = 100;
            var visibleLeft = Math.Max(pos.Left, screenLeft);
            var visibleTop = Math.Max(pos.Top, screenTop);
            var visibleRight = Math.Min(pos.Left + pos.Width, screenRight);
            var visibleBottom = Math.Min(pos.Top + 50, screenBottom);

            return visibleRight - visibleLeft >= MinVisible && visibleBottom - visibleTop > 0;
        }

        private void SchedulePositionSave()
        {
            if (!_positionTrackingEnabled)
                return;

            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        private void OnSaveDebounceTick(object? sender, EventArgs e)
        {
            _saveDebounceTimer.Stop();
            CapturePositionAndSave();
        }

        private void CapturePositionAndSave()
        {
            // RestoreBounds reflects the pre-maximize/minimize size/position; use it whenever the window
            // isn't in Normal state so we never persist the off-screen minimized rect or the maximized size.
            var bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, Width, Height)
                : RestoreBounds;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            _userSettings.WindowPosition = new WindowPositionSetting
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
                WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState
            };
            _userSettings.Save();
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            _saveDebounceTimer.Stop();
            CapturePositionAndSave();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //  CUSTOM ANIMATION EXAMPLES
        //  Wired up in MainWindow.xaml on the SearchDataGrid element.
        //  These handlers only fire when the corresponding mode is set to "Custom":
        //    - RowAnimationKind = Custom          → OnCustomRowAnimationBegin
        //    - ScrollAnimationMode = Custom        → OnCustomScrollAnimation
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Custom row animation example: fade in from 0 → 1 opacity while sliding
        /// in from the right with a cubic ease-out curve. Combines a RenderTransform
        /// with an opacity animation on the CellsPresenter for a polished entrance.
        /// </summary>
        private void OnCustomRowAnimationBegin(object? sender, RowAnimationBeginEventArgs e)
        {
            var cellsPresenter = e.CellsPresenter;

            // Slide-in: attach a TranslateTransform and animate its X from 30 → 0
            var transform = new TranslateTransform(30, 0);
            cellsPresenter.RenderTransform = transform;

            var slideAnim = new DoubleAnimation(30, 0, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            slideAnim.Freeze();
            transform.BeginAnimation(TranslateTransform.XProperty, slideAnim);

            // Fade-in: animate opacity from 0 → 1 with a matching easing curve
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fadeAnim.Freeze();
            cellsPresenter.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        /// <summary>
        /// Custom scroll animation example: uses a Storyboard with a bounce-style
        /// easing curve to animate the scroll position from its old offset to the
        /// new one. The Storyboard targets SmoothScrollBehavior.AnimatedVerticalOffset
        /// on the ScrollViewer, which drives ScrollToVerticalOffset when it changes.
        /// </summary>
        private void OnCustomScrollAnimation(object? sender, CustomScrollAnimationEventArgs e)
        {
            // Build a DoubleAnimation from the old offset to the new offset
            var animation = new DoubleAnimation
            {
                From = e.OldOffset,
                To = e.NewOffset,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new ExponentialEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Exponent = 6
                }
            };

            // Target the attached AnimatedVerticalOffset property on the ScrollViewer.
            // When this property changes, the ScrollViewer's ScrollToVerticalOffset is called.
            Storyboard.SetTargetProperty(animation,
                new PropertyPath("(0)", SmoothScrollBehavior.AnimatedVerticalOffsetProperty));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);

            // Hand the storyboard back to the behavior — it will Begin() it on the ScrollViewer
            e.Storyboard = storyboard;
        }
    }
}
