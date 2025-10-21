using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF.Behaviors
{
    /// <summary>
    /// Adds a two-step “confirm to delete” interaction to any <see cref="FrameworkElement"/>.
    /// When enabled, the first left-click enters a confirmation state; moving the mouse away
    /// starts a 1s timeout that clears the state, and re-entering cancels the timeout.
    /// </summary>
    /// <remarks>
    /// Set <see cref="IsEnabledProperty"/> to <c>true</c> on the target element.
    /// The first click is marked handled to avoid triggering other click behaviors.
    /// </remarks>
    public static class TokenConfirmationBehavior
    {
        /// <summary>
        /// Per-element 1s timer used to exit the confirmation state after mouse leave.
        /// </summary>
        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(TokenConfirmationBehavior));

        /// <summary>
        /// Attached property that turns the confirmation behavior on/off.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(TokenConfirmationBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// Attached property indicating whether the element is currently in the confirmation state.
        /// Typically bound by the UI to show a “Confirm” affordance.
        /// </summary>
        public static readonly DependencyProperty IsInConfirmationStateProperty =
            DependencyProperty.RegisterAttached("IsInConfirmationState", typeof(bool), typeof(TokenConfirmationBehavior),
                new PropertyMetadata(false));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsInConfirmationState(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsInConfirmationStateProperty);
        }

        public static void SetIsInConfirmationState(DependencyObject obj, bool value)
        {
            obj.SetValue(IsInConfirmationStateProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseLeftButtonDown += OnMouseLeftButtonDown;
                    element.MouseEnter += OnMouseEnter;
                    element.MouseLeave += OnMouseLeave;
                }
                else
                {
                    element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
                    element.MouseEnter -= OnMouseEnter;
                    element.MouseLeave -= OnMouseLeave;
                }
            }
        }

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                SetIsInConfirmationState(element, true);

                var timer = (DispatcherTimer)element.GetValue(TimerProperty);
                timer?.Stop();

                // Mark the event as handled to prevent other click behaviors
                e.Handled = true;
            }
        }

        private static void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (GetIsInConfirmationState(element))
                {
                    var timer = (DispatcherTimer)element.GetValue(TimerProperty);
                    timer?.Stop();
                }
            }
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (GetIsInConfirmationState(element))
                {
                    // Create or reuse a timer for delayed exit from confirmation state
                    var timer = (DispatcherTimer)element.GetValue(TimerProperty);
                    if (timer == null)
                    {
                        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop();
                            SetIsInConfirmationState(element, false);
                        };
                        element.SetValue(TimerProperty, timer);
                    }
                    timer.Stop();
                    timer.Start();
                }
            }
        }
    }
}