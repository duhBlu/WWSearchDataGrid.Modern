using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Attached behavior driving the expand/collapse animation in
    /// <c>AnimatedGroupExpanderTemplate</c>. Attach <see cref="IsHostProperty"/> to the
    /// content-host <see cref="FrameworkElement"/> inside the Expander's template; the
    /// behavior resolves the owning <see cref="Expander"/> via
    /// <see cref="FrameworkElement.TemplatedParent"/>, wires
    /// <see cref="Expander.Expanded"/> / <see cref="Expander.Collapsed"/>, and animates the
    /// host's <see cref="FrameworkElement.Height"/> between 0 and a viewport-bounded
    /// natural height. The host carries <see cref="Visibility.Collapsed"/> in the steady
    /// collapsed state so the inner <see cref="VirtualizingStackPanel"/> skips measure
    /// work — and the natural-height measure is capped at the enclosing
    /// <see cref="ScrollViewer.ViewportHeight"/> so the panel realizes only the items that
    /// currently fit, keeping the grid's grouping virtualization intact.
    /// </summary>
    public static class GroupExpansionAnimator
    {
        /// <summary>
        /// Marks a <see cref="FrameworkElement"/> as the animated content host inside the
        /// animated group-expander template. The host must live within an
        /// <see cref="Expander.Template"/> so <see cref="FrameworkElement.TemplatedParent"/>
        /// resolves to the owning Expander.
        /// </summary>
        public static readonly DependencyProperty IsHostProperty =
            DependencyProperty.RegisterAttached(
                "IsHost",
                typeof(bool),
                typeof(GroupExpansionAnimator),
                new PropertyMetadata(false, OnIsHostChanged));

        /// <inheritdoc cref="IsHostProperty"/>
        public static void SetIsHost(DependencyObject element, bool value)
            => element.SetValue(IsHostProperty, value);

        /// <inheritdoc cref="IsHostProperty"/>
        public static bool GetIsHost(DependencyObject element)
            => (bool)element.GetValue(IsHostProperty);

        /// <summary>
        /// Per-host slot recording which Expander we currently have event subscriptions
        /// on. Lets <see cref="DetachExpander"/> unhook cleanly without re-walking the
        /// visual tree, and lets a re-Loaded host (virtualization recycle) detect and
        /// rebuild its subscription.
        /// </summary>
        private static readonly DependencyProperty SubscribedExpanderProperty =
            DependencyProperty.RegisterAttached(
                "SubscribedExpander",
                typeof(Expander),
                typeof(GroupExpansionAnimator));

        /// <summary>
        /// Animation duration for both expand and collapse. Short enough to feel
        /// responsive on rapid toggles; long enough that the chevron rotation completes
        /// alongside the height change.
        /// </summary>
        private static readonly Duration s_duration = new Duration(TimeSpan.FromMilliseconds(180));

        /// <summary>
        /// Fallback measurement budget when no <see cref="ScrollViewer"/> ancestor is
        /// available. Bounds how many item containers the inner virtualizing panel
        /// realizes during the natural-height probe.
        /// </summary>
        private const double FallbackMeasureBudget = 800d;

        private static void OnIsHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement host) return;
            if ((bool)e.NewValue)
            {
                host.Loaded += OnHostLoaded;
                host.Unloaded += OnHostUnloaded;
                // If the host was already loaded when the property flipped (e.g. attached
                // imperatively after first layout), wire up immediately — Loaded won't fire.
                if (host.IsLoaded) OnHostLoaded(host, new RoutedEventArgs(FrameworkElement.LoadedEvent, host));
            }
            else
            {
                host.Loaded -= OnHostLoaded;
                host.Unloaded -= OnHostUnloaded;
                DetachExpander(host);
                host.BeginAnimation(FrameworkElement.HeightProperty, null);
            }
        }

        private static void OnHostLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement host) return;
            // Container recycling re-fires Loaded on the same host with a new TemplatedParent;
            // unhook the prior expander before re-subscribing so we don't accumulate handlers.
            DetachExpander(host);
            if (host.TemplatedParent is Expander expander)
            {
                expander.Expanded += OnExpanderExpanded;
                expander.Collapsed += OnExpanderCollapsed;
                host.SetValue(SubscribedExpanderProperty, expander);
                ApplyImmediateState(host, expander.IsExpanded);
            }
        }

        private static void OnHostUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement host) return;
            DetachExpander(host);
            host.BeginAnimation(FrameworkElement.HeightProperty, null);
        }

        private static void DetachExpander(FrameworkElement host)
        {
            if (host.GetValue(SubscribedExpanderProperty) is Expander expander)
            {
                expander.Expanded -= OnExpanderExpanded;
                expander.Collapsed -= OnExpanderCollapsed;
                host.ClearValue(SubscribedExpanderProperty);
            }
        }

        /// <summary>
        /// Snaps the host to the steady state matching the current
        /// <see cref="Expander.IsExpanded"/>. Called on Loaded so a freshly-realized
        /// (or recycled) host starts in the right Visibility without playing an animation
        /// the user didn't trigger.
        /// </summary>
        private static void ApplyImmediateState(FrameworkElement host, bool isExpanded)
        {
            host.BeginAnimation(FrameworkElement.HeightProperty, null);
            host.ClearValue(FrameworkElement.HeightProperty);
            host.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnExpanderExpanded(object sender, RoutedEventArgs e)
        {
            // Expanded is a bubbling routed event — nested expanders' events surface here
            // too. Only animate when the originating expander matches our subscription.
            if (sender is not Expander subscribed) return;
            if (e.OriginalSource != subscribed) return;
            if (FindHost(subscribed) is FrameworkElement host)
                AnimateExpand(host);
        }

        private static void OnExpanderCollapsed(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander subscribed) return;
            if (e.OriginalSource != subscribed) return;
            if (FindHost(subscribed) is FrameworkElement host)
                AnimateCollapse(host);
        }

        /// <summary>
        /// Walks the visual tree under <paramref name="expander"/> to find the host
        /// flagged with <see cref="IsHostProperty"/>. The host lives inside the Expander's
        /// own template so the walk is shallow.
        /// </summary>
        private static FrameworkElement FindHost(Expander expander)
        {
            return FindHostCore(expander);
        }

        private static FrameworkElement FindHostCore(DependencyObject node)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);
                if (child is FrameworkElement fe && GetIsHost(fe)) return fe;
                var nested = FindHostCore(child);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void AnimateExpand(FrameworkElement host)
        {
            host.BeginAnimation(FrameworkElement.HeightProperty, null);
            // Visibility must be Visible before Measure — a Collapsed host's MeasureCore
            // short-circuits to Size(0,0) without invoking the children.
            host.Visibility = Visibility.Visible;
            host.ClearValue(FrameworkElement.HeightProperty);

            double width = ResolveMeasureWidth(host);
            double budget = GetMeasureBudget(host);
            host.Measure(new Size(width, budget));
            double target = host.DesiredSize.Height;
            if (target <= 0 || double.IsNaN(target))
            {
                // Nothing to animate to — leave the host Visible with Auto height.
                return;
            }

            host.Height = 0;
            var animation = new DoubleAnimation
            {
                From = 0,
                To = target,
                Duration = s_duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            animation.Completed += (_, _) =>
            {
                // Clear the local 0 BEFORE detaching the animation so we don't snap to 0
                // for a frame between "animation released" and "local cleared". Once
                // detached, Auto sizing takes over and the rest of the group's items
                // realize on demand as the user scrolls.
                host.ClearValue(FrameworkElement.HeightProperty);
                host.BeginAnimation(FrameworkElement.HeightProperty, null);
            };
            host.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        private static void AnimateCollapse(FrameworkElement host)
        {
            host.BeginAnimation(FrameworkElement.HeightProperty, null);
            double from = host.ActualHeight;
            if (from <= 0 || double.IsNaN(from))
            {
                // Already at zero height (or never laid out) — just hide.
                host.Visibility = Visibility.Collapsed;
                host.ClearValue(FrameworkElement.HeightProperty);
                return;
            }
            host.Height = from;
            var animation = new DoubleAnimation
            {
                From = from,
                To = 0,
                Duration = s_duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            animation.Completed += (_, _) =>
            {
                host.Visibility = Visibility.Collapsed;
                host.ClearValue(FrameworkElement.HeightProperty);
                host.BeginAnimation(FrameworkElement.HeightProperty, null);
            };
            host.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// <summary>
        /// Resolves the width to pass to <see cref="UIElement.Measure"/> when probing the
        /// host's natural height. A host coming out of <see cref="Visibility.Collapsed"/>
        /// has <see cref="FrameworkElement.ActualWidth"/> = 0, so fall through to the
        /// templated parent's width (the Expander, which spans the GroupItem).
        /// </summary>
        private static double ResolveMeasureWidth(FrameworkElement host)
        {
            if (host.ActualWidth > 0) return host.ActualWidth;
            if (host.TemplatedParent is FrameworkElement parent && parent.ActualWidth > 0)
                return parent.ActualWidth;
            return double.PositiveInfinity;
        }

        /// <summary>
        /// Available-height budget for the natural-height measure. Bounded by the
        /// enclosing <see cref="ScrollViewer.ViewportHeight"/> so the inner
        /// <see cref="VirtualizingStackPanel"/> realizes only the items that fit the
        /// viewport — measuring against infinity here would force every container in the
        /// group to realize up front and defeat virtualization.
        /// </summary>
        private static double GetMeasureBudget(FrameworkElement host)
        {
            var sv = FindAncestor<ScrollViewer>(host);
            if (sv != null && sv.ViewportHeight > 0 && !double.IsNaN(sv.ViewportHeight))
                return sv.ViewportHeight;
            return FallbackMeasureBudget;
        }

        private static T FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            DependencyObject current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
