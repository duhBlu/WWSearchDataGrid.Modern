using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF.Behaviors
{
    /// <summary>
    /// Attached property behavior for managing token confirmation state for deletion UI
    /// </summary>
    public static class TokenConfirmationBehavior
    {
        // Private timer property for managing focus loss
        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(TokenConfirmationBehavior));

        // Public attached property for enabling confirmation behavior
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(TokenConfirmationBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        // Public attached property for tracking confirmation state
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
                // Enter confirmation state on first click
                SetIsInConfirmationState(element, true);

                // Cancel any existing timer
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
                // Only react if we're in confirmation state
                if (GetIsInConfirmationState(element))
                {
                    // Cancel any existing timer - mouse is back over the token
                    var timer = (DispatcherTimer)element.GetValue(TimerProperty);
                    timer?.Stop();
                }
            }
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                // Only react if we're in confirmation state
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