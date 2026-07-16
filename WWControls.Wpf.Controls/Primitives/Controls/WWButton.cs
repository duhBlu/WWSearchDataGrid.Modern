using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WWControls.Core;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// The library's general-purpose button. One control covers the three stock button behaviors —
    /// <see cref="ButtonKind"/> selects simple (one <c>Click</c> per click), repeat (<c>Click</c>
    /// fires on press and repeats until release, paced by <see cref="Delay"/> /
    /// <see cref="Interval"/>), or toggle (each click cycles <see cref="IsChecked"/>, three-state
    /// when <see cref="IsThreeState"/> is set). A <see cref="Glyph"/> renders beside the content on
    /// the <see cref="GlyphAlignment"/> side, tinted by <see cref="GlyphBrush"/> (bound to
    /// <see cref="Control.Foreground"/> by the default style) so it follows hover / pressed /
    /// disabled states. <see cref="AsyncDisplayMode"/> visualizes an asynchronous command: the
    /// button shows a loading wheel while <see cref="IsAsyncOperationInProgress"/> is set — driven
    /// automatically when <see cref="System.Windows.Controls.Primitives.ButtonBase.Command"/>
    /// implements <see cref="IAsyncCommand"/> — and in
    /// <see cref="Primitives.AsyncDisplayMode.WaitCancel"/> mode hovering swaps the wheel for a
    /// cancel affordance that requests cancellation instead of clicking.
    /// </summary>
    /// <remarks>
    /// Derives <see cref="Button"/>, so <see cref="Button.IsDefault"/> / <see cref="Button.IsCancel"/>
    /// dialog semantics, commanding, and access keys all come along. The default style switches
    /// <see cref="System.Windows.Controls.Primitives.ButtonBase.ClickMode"/> to <c>Press</c> for the
    /// repeat kind so the first click lands on press, matching <see cref="System.Windows.Controls.Primitives.RepeatButton"/>.
    /// </remarks>
    public class WWButton : Button
    {
        private DispatcherTimer _repeatTimer;

        static WWButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWButton),
                new FrameworkPropertyMetadata(typeof(WWButton)));

            // Hook command swaps so the button can mirror an IAsyncCommand's execution state.
            CommandProperty.OverrideMetadata(typeof(WWButton),
                new FrameworkPropertyMetadata((d, e) => ((WWButton)d).OnCommandSwapped(e)));
        }

        // ─── Behavior ──────────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="ButtonKind"/> dependency property.</summary>
        public static readonly DependencyProperty ButtonKindProperty =
            DependencyProperty.Register(nameof(ButtonKind), typeof(ButtonKind), typeof(WWButton),
                new PropertyMetadata(ButtonKind.Simple, (d, e) => ((WWButton)d).StopRepeatTimer()));

        /// <summary>Identifies the <see cref="Delay"/> dependency property.</summary>
        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register(nameof(Delay), typeof(int), typeof(WWButton),
                new PropertyMetadata(GetKeyboardDelay()),
                value => (int)value >= 0);

        /// <summary>Identifies the <see cref="Interval"/> dependency property.</summary>
        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(nameof(Interval), typeof(int), typeof(WWButton),
                new PropertyMetadata(GetKeyboardSpeed()),
                value => (int)value > 0);

        /// <summary>How the button responds to clicks — simple, repeat, or toggle.</summary>
        public ButtonKind ButtonKind
        {
            get => (ButtonKind)GetValue(ButtonKindProperty);
            set => SetValue(ButtonKindProperty, value);
        }

        /// <summary>
        /// Milliseconds the repeat button waits while pressed before repeating starts.
        /// Defaults to the system keyboard repeat delay.
        /// </summary>
        public int Delay
        {
            get => (int)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        /// <summary>
        /// Milliseconds between repeats once repeating starts.
        /// Defaults to the system keyboard repeat speed.
        /// </summary>
        public int Interval
        {
            get => (int)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        // ─── Toggle state ──────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(WWButton),
                new FrameworkPropertyMetadata((bool?)false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                    (d, e) => ((WWButton)d).OnIsCheckedChanged((bool?)e.NewValue)));

        /// <summary>Identifies the <see cref="IsThreeState"/> dependency property.</summary>
        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(nameof(IsThreeState), typeof(bool), typeof(WWButton),
                new PropertyMetadata(false));

        /// <summary>Identifies the <see cref="Checked"/> routed event.</summary>
        public static readonly RoutedEvent CheckedEvent =
            EventManager.RegisterRoutedEvent(nameof(Checked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(WWButton));

        /// <summary>Identifies the <see cref="Unchecked"/> routed event.</summary>
        public static readonly RoutedEvent UncheckedEvent =
            EventManager.RegisterRoutedEvent(nameof(Unchecked), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(WWButton));

        /// <summary>Identifies the <see cref="Indeterminate"/> routed event.</summary>
        public static readonly RoutedEvent IndeterminateEvent =
            EventManager.RegisterRoutedEvent(nameof(Indeterminate), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(WWButton));

        /// <summary>The toggle state (toggle kind only). Nullable to carry the indeterminate value.</summary>
        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        /// <summary>Whether a toggle button cycles through the indeterminate state (checked → indeterminate → unchecked).</summary>
        public bool IsThreeState
        {
            get => (bool)GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        /// <summary>Raised when <see cref="IsChecked"/> becomes <see langword="true"/>.</summary>
        public event RoutedEventHandler Checked
        {
            add => AddHandler(CheckedEvent, value);
            remove => RemoveHandler(CheckedEvent, value);
        }

        /// <summary>Raised when <see cref="IsChecked"/> becomes <see langword="false"/>.</summary>
        public event RoutedEventHandler Unchecked
        {
            add => AddHandler(UncheckedEvent, value);
            remove => RemoveHandler(UncheckedEvent, value);
        }

        /// <summary>Raised when <see cref="IsChecked"/> becomes indeterminate (<see langword="null"/>).</summary>
        public event RoutedEventHandler Indeterminate
        {
            add => AddHandler(IndeterminateEvent, value);
            remove => RemoveHandler(IndeterminateEvent, value);
        }

        // ─── Async display ─────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="AsyncDisplayMode"/> dependency property.</summary>
        public static readonly DependencyProperty AsyncDisplayModeProperty =
            DependencyProperty.Register(nameof(AsyncDisplayMode), typeof(AsyncDisplayMode), typeof(WWButton),
                new PropertyMetadata(AsyncDisplayMode.None, (d, e) => d.CoerceValue(IsEnabledProperty)));

        /// <summary>Identifies the <see cref="AsyncWheelKind"/> dependency property.</summary>
        public static readonly DependencyProperty AsyncWheelKindProperty =
            DependencyProperty.Register(nameof(AsyncWheelKind), typeof(WheelKind), typeof(WWButton),
                new PropertyMetadata(WheelKind.Arc));

        /// <summary>Identifies the <see cref="IsAsyncOperationInProgress"/> dependency property.</summary>
        public static readonly DependencyProperty IsAsyncOperationInProgressProperty =
            DependencyProperty.Register(nameof(IsAsyncOperationInProgress), typeof(bool), typeof(WWButton),
                new PropertyMetadata(false, (d, e) => d.CoerceValue(IsEnabledProperty)));

        /// <summary>Identifies the <see cref="CancelClick"/> routed event.</summary>
        public static readonly RoutedEvent CancelClickEvent =
            EventManager.RegisterRoutedEvent(nameof(CancelClick), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(WWButton));

        /// <summary>How the button visualizes an asynchronous operation.</summary>
        public AsyncDisplayMode AsyncDisplayMode
        {
            get => (AsyncDisplayMode)GetValue(AsyncDisplayModeProperty);
            set => SetValue(AsyncDisplayModeProperty, value);
        }

        /// <summary>
        /// Visual kind of the wait wheel shown while <see cref="IsAsyncOperationInProgress"/> is
        /// set — <see cref="WheelKind.Arc"/> (default) or <see cref="WheelKind.Dots"/>.
        /// </summary>
        public WheelKind AsyncWheelKind
        {
            get => (WheelKind)GetValue(AsyncWheelKindProperty);
            set => SetValue(AsyncWheelKindProperty, value);
        }

        /// <summary>
        /// Whether an asynchronous operation is in flight. Driven automatically from the bound
        /// command's <see cref="IAsyncCommand.IsExecuting"/> when it implements
        /// <see cref="IAsyncCommand"/>; settable directly for hand-rolled async work.
        /// </summary>
        public bool IsAsyncOperationInProgress
        {
            get => (bool)GetValue(IsAsyncOperationInProgressProperty);
            set => SetValue(IsAsyncOperationInProgressProperty, value);
        }

        /// <summary>
        /// Raised when the button is clicked while showing the
        /// <see cref="Primitives.AsyncDisplayMode.WaitCancel"/> cancel affordance — in place of
        /// <see cref="System.Windows.Controls.Primitives.ButtonBase.Click"/>, after cancellation has
        /// been requested on the associated <see cref="IAsyncCommand"/>.
        /// </summary>
        public event RoutedEventHandler CancelClick
        {
            add => AddHandler(CancelClickEvent, value);
            remove => RemoveHandler(CancelClickEvent, value);
        }

        // ─── Chrome & glyph ────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="CornerRadius"/> dependency property.</summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(WWButton),
                new FrameworkPropertyMetadata(default(CornerRadius)));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(ImageSource), typeof(WWButton),
                new FrameworkPropertyMetadata(null, (d, e) => ((WWButton)d).UpdateGlyphMargin()));

        /// <summary>Identifies the <see cref="GlyphBrush"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphBrushProperty =
            DependencyProperty.Register(nameof(GlyphBrush), typeof(Brush), typeof(WWButton),
                new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="GlyphAlignment"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphAlignmentProperty =
            DependencyProperty.Register(nameof(GlyphAlignment), typeof(Dock), typeof(WWButton),
                new FrameworkPropertyMetadata(Dock.Left, (d, e) => ((WWButton)d).UpdateGlyphMargin()));

        /// <summary>Identifies the <see cref="GlyphWidth"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphWidthProperty =
            DependencyProperty.Register(nameof(GlyphWidth), typeof(double), typeof(WWButton),
                new PropertyMetadata(double.NaN));

        /// <summary>Identifies the <see cref="GlyphHeight"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphHeightProperty =
            DependencyProperty.Register(nameof(GlyphHeight), typeof(double), typeof(WWButton),
                new PropertyMetadata(double.NaN));

        /// <summary>Identifies the <see cref="GlyphStrokeThickness"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphStrokeThicknessProperty =
            DependencyProperty.Register(nameof(GlyphStrokeThickness), typeof(double), typeof(WWButton),
                new PropertyMetadata(double.NaN));

        /// <summary>Identifies the <see cref="GlyphToContentOffset"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphToContentOffsetProperty =
            DependencyProperty.Register(nameof(GlyphToContentOffset), typeof(double), typeof(WWButton),
                new FrameworkPropertyMetadata(4.0, (d, e) => ((WWButton)d).UpdateGlyphMargin()));

        private static readonly DependencyPropertyKey GlyphMarginPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(GlyphMargin), typeof(Thickness), typeof(WWButton),
                new FrameworkPropertyMetadata(default(Thickness)));

        /// <summary>Identifies the read-only <see cref="GlyphMargin"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphMarginProperty = GlyphMarginPropertyKey.DependencyProperty;

        /// <summary>Corner rounding of the button chrome — restylable per instance without retemplating.</summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// The button's icon. A monochrome <see cref="DrawingImage"/> (e.g. an <see cref="IconKeys"/>
        /// resource) is tinted by <see cref="GlyphBrush"/>; any other <see cref="ImageSource"/>
        /// renders as-is.
        /// </summary>
        public ImageSource Glyph
        {
            get => (ImageSource)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <summary>
        /// Tint applied to a <see cref="DrawingImage"/> glyph. The default style binds it to
        /// <see cref="Control.Foreground"/> so the glyph follows state triggers; set
        /// <see langword="null"/> to render the glyph untinted.
        /// </summary>
        public Brush GlyphBrush
        {
            get => (Brush)GetValue(GlyphBrushProperty);
            set => SetValue(GlyphBrushProperty, value);
        }

        /// <summary>Which side of the content the glyph docks to. Defaults to <see cref="Dock.Left"/>.</summary>
        public Dock GlyphAlignment
        {
            get => (Dock)GetValue(GlyphAlignmentProperty);
            set => SetValue(GlyphAlignmentProperty, value);
        }

        /// <summary>Rendered glyph width. <see cref="double.NaN"/> (default) uses the source's natural size.</summary>
        public double GlyphWidth
        {
            get => (double)GetValue(GlyphWidthProperty);
            set => SetValue(GlyphWidthProperty, value);
        }

        /// <summary>Rendered glyph height. <see cref="double.NaN"/> (default) uses the source's natural size.</summary>
        public double GlyphHeight
        {
            get => (double)GetValue(GlyphHeightProperty);
            set => SetValue(GlyphHeightProperty, value);
        }

        /// <summary>
        /// Stroke thickness override for a <see cref="DrawingImage"/> glyph, in the glyph's own
        /// source-coordinate units (so it is independent of <see cref="GlyphWidth"/> /
        /// <see cref="GlyphHeight"/>). Use it to even out glyphs that read too thin or too heavy at
        /// a given size. <see cref="double.NaN"/> (default) keeps each glyph's authored thickness.
        /// Forwarded to the glyph's <see cref="Icon.StrokeThickness"/>.
        /// </summary>
        public double GlyphStrokeThickness
        {
            get => (double)GetValue(GlyphStrokeThicknessProperty);
            set => SetValue(GlyphStrokeThicknessProperty, value);
        }

        /// <summary>Gap between the glyph and the content, applied on the <see cref="GlyphAlignment"/> side.</summary>
        public double GlyphToContentOffset
        {
            get => (double)GetValue(GlyphToContentOffsetProperty);
            set => SetValue(GlyphToContentOffsetProperty, value);
        }

        /// <summary>
        /// Computed margin the template applies to the glyph — <see cref="GlyphToContentOffset"/>
        /// on the content side, zero when there is no content or no glyph.
        /// </summary>
        public Thickness GlyphMargin => (Thickness)GetValue(GlyphMarginProperty);

        // ─── Click routing ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Keeps the button interactive while an async operation runs in
        /// <see cref="Primitives.AsyncDisplayMode.WaitCancel"/> mode, even though the executing
        /// command's <c>CanExecute</c> has gone false — the click is the cancel affordance.
        /// </summary>
        protected override bool IsEnabledCore =>
            base.IsEnabledCore ||
            (IsAsyncOperationInProgress && AsyncDisplayMode == AsyncDisplayMode.WaitCancel);

        protected override void OnClick()
        {
            if (IsAsyncOperationInProgress)
            {
                switch (AsyncDisplayMode)
                {
                    case AsyncDisplayMode.WaitCancel:
                        (Command as IAsyncCommand)?.Cancel();
                        RaiseEvent(new RoutedEventArgs(CancelClickEvent, this));
                        return;
                    case AsyncDisplayMode.Wait:
                        return;
                }
            }

            if (ButtonKind == ButtonKind.Toggle)
                Toggle();

            base.OnClick();
        }

        private void Toggle()
        {
            bool? newValue;
            if (IsChecked == true)
                newValue = IsThreeState ? (bool?)null : false;
            else
                newValue = IsChecked.HasValue; // false → true, indeterminate → false
            SetCurrentValue(IsCheckedProperty, newValue);
        }

        private void OnIsCheckedChanged(bool? newValue)
        {
            var routedEvent = newValue == true ? CheckedEvent
                            : newValue == false ? UncheckedEvent
                            : IndeterminateEvent;
            RaiseEvent(new RoutedEventArgs(routedEvent, this));
        }

        // ─── Repeat ────────────────────────────────────────────────────────────────────

        protected override void OnIsPressedChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsPressedChanged(e);
            if (ButtonKind != ButtonKind.Repeat) return;

            if (IsPressed)
                StartRepeatTimer();
            else
                StopRepeatTimer();
        }

        private void StartRepeatTimer()
        {
            if (_repeatTimer == null)
            {
                _repeatTimer = new DispatcherTimer();
                _repeatTimer.Tick += OnRepeatTimerTick;
            }
            _repeatTimer.Interval = TimeSpan.FromMilliseconds(Delay);
            _repeatTimer.Start();
        }

        private void StopRepeatTimer()
        {
            _repeatTimer?.Stop();
        }

        private void OnRepeatTimerTick(object sender, EventArgs e)
        {
            if (!IsPressed || ButtonKind != ButtonKind.Repeat)
            {
                StopRepeatTimer();
                return;
            }

            var interval = TimeSpan.FromMilliseconds(Interval);
            if (_repeatTimer.Interval != interval)
                _repeatTimer.Interval = interval;
            OnClick();
        }

        // ─── Async command tracking ────────────────────────────────────────────────────

        private void OnCommandSwapped(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is IAsyncCommand oldAsync && oldAsync is INotifyPropertyChanged oldNotify)
                PropertyChangedEventManager.RemoveHandler(oldNotify, OnAsyncCommandStateChanged, string.Empty);

            if (e.NewValue is IAsyncCommand newAsync && newAsync is INotifyPropertyChanged newNotify)
            {
                PropertyChangedEventManager.AddHandler(newNotify, OnAsyncCommandStateChanged, string.Empty);
                SetCurrentValue(IsAsyncOperationInProgressProperty, newAsync.IsExecuting);
            }
            else
            {
                SetCurrentValue(IsAsyncOperationInProgressProperty, false);
            }
        }

        private void OnAsyncCommandStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) && e.PropertyName != nameof(IAsyncCommand.IsExecuting))
                return;
            if (sender is IAsyncCommand command)
                SetCurrentValue(IsAsyncOperationInProgressProperty, command.IsExecuting);
        }

        // ─── Glyph layout ──────────────────────────────────────────────────────────────

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            UpdateGlyphMargin();
        }

        private void UpdateGlyphMargin()
        {
            Thickness margin = default;
            if (Glyph != null && Content != null)
            {
                double offset = GlyphToContentOffset;
                switch (GlyphAlignment)
                {
                    case Dock.Left: margin = new Thickness(0, 0, offset, 0); break;
                    case Dock.Right: margin = new Thickness(offset, 0, 0, 0); break;
                    case Dock.Top: margin = new Thickness(0, 0, 0, offset); break;
                    case Dock.Bottom: margin = new Thickness(0, offset, 0, 0); break;
                }
            }
            SetValue(GlyphMarginPropertyKey, margin);
        }

        // ─── System repeat defaults (mirrors RepeatButton) ─────────────────────────────

        private static int GetKeyboardDelay()
        {
            int delay = SystemParameters.KeyboardDelay;
            if (delay < 0 || delay > 3) delay = 0;
            return (delay + 1) * 250;
        }

        private static int GetKeyboardSpeed()
        {
            int speed = SystemParameters.KeyboardSpeed;
            if (speed < 0 || speed > 31) speed = 31;
            return (31 - speed) * (400 - 1000 / 30) / 31 + 1000 / 30;
        }
    }
}
