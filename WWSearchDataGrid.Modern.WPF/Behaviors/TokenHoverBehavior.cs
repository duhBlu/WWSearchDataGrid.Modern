using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF.Behaviors
{
    /// <summary>
    /// Attached property behavior for handling token hover events
    /// </summary>
    public static class TokenHoverBehavior
    {
        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(TokenHoverBehavior));

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(TokenHoverBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseEnter += OnMouseEnter;
                    element.MouseLeave += OnMouseLeave;
                }
                else
                {
                    element.MouseEnter -= OnMouseEnter;
                    element.MouseLeave -= OnMouseLeave;
                }
            }
        }

        private static void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string filterId)
            {
                var filterPanel = FindAncestor<FilterPanel>(element);
                if (filterPanel != null)
                {
                    // Cancel any pending clear operation
                    var timer = (DispatcherTimer)filterPanel.GetValue(TimerProperty);
                    timer?.Stop();
                    
                    filterPanel.SetHoveredFilterCommand?.Execute(filterId);
                }
            }
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var filterPanel = FindAncestor<FilterPanel>(element);
                if (filterPanel != null)
                {
                    // Create or reuse a timer for delayed clearing
                    var timer = (DispatcherTimer)filterPanel.GetValue(TimerProperty);
                    if (timer == null)
                    {
                        timer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(100) };
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop();
                            filterPanel.ClearHoveredFilterCommand?.Execute(null);
                        };
                        filterPanel.SetValue(TimerProperty, timer);
                    }
                    timer.Stop();
                    timer.Start();
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : class
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;
                    
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}