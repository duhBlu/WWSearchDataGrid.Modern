using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Attached behavior driving a group-header chevron's rotation: right (0°) when collapsed,
    /// down (90°) when expanded. The host element binds <see cref="IsExpandedProperty"/> to its
    /// DataContext's <c>IsExpanded</c> (a <see cref="GroupHeaderRow"/> or
    /// <see cref="FixedGroupHeaderEntry"/>) and sets <see cref="AttachProperty"/> so the behavior
    /// initializes with the container.
    /// </summary>
    /// <remarks>
    /// The rotation animates ONLY for a genuine in-place toggle — the value changing under an
    /// unchanged DataContext — and only while the owning grid's
    /// <see cref="SearchDataGrid.AllowGroupExpandAnimation"/> is on. Everything else snaps: the
    /// initial template bind, and a recycled container re-binding to a different header (scrolling
    /// must never play toggle animations). Rebinds are detected by comparing the DataContext seen
    /// at the previous change; a DataContext change arms a snap that either the next value change
    /// consumes or a deferred pass clears (covering rebinds where the bound value happens not to
    /// change).
    /// </remarks>
    public static class GroupChevron
    {
        private const double ExpandedAngle = 90;
        private static readonly Duration RotationDuration = new(TimeSpan.FromMilliseconds(150));

        /// <summary>Initializes the behavior on the host element. Set once, before the IsExpanded binding.</summary>
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached(
                "Attach",
                typeof(bool),
                typeof(GroupChevron),
                new PropertyMetadata(false, OnAttachChanged));

        public static bool GetAttach(DependencyObject obj) => (bool)obj.GetValue(AttachProperty);
        public static void SetAttach(DependencyObject obj, bool value) => obj.SetValue(AttachProperty, value);

        /// <summary>The bound expansion state the chevron mirrors.</summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.RegisterAttached(
                "IsExpanded",
                typeof(bool),
                typeof(GroupChevron),
                new PropertyMetadata(false, OnIsExpandedChanged));

        public static bool GetIsExpanded(DependencyObject obj) => (bool)obj.GetValue(IsExpandedProperty);
        public static void SetIsExpanded(DependencyObject obj, bool value) => obj.SetValue(IsExpandedProperty, value);

        /// <summary>Per-element behavior state (transform + snap arming). Private to the behavior.</summary>
        private static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "State",
                typeof(ChevronState),
                typeof(GroupChevron),
                new PropertyMetadata(null));

        private sealed class ChevronState
        {
            public RotateTransform Transform;

            /// <summary>
            /// The next value change must snap rather than animate — armed at attach and on every
            /// DataContext change, disarmed by the change that consumes it or by the deferred
            /// settle pass.
            /// </summary>
            public bool SnapNext = true;
        }

        private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe || !(bool)e.NewValue) return;

            var state = EnsureState(fe);
            fe.DataContextChanged += (_, _) =>
            {
                state.SnapNext = true;
                ScheduleSettle(fe, state);
            };
            ScheduleSettle(fe, state);
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe) return;

            var state = EnsureState(fe);
            double angle = (bool)e.NewValue ? ExpandedAngle : 0;
            var transform = EnsureTransform(fe, state);

            bool animate = !state.SnapNext
                && VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(fe)?.AllowGroupExpandAnimation == true;
            state.SnapNext = false;

            if (animate)
            {
                var rotate = new DoubleAnimation(angle, RotationDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                };
                transform.BeginAnimation(RotateTransform.AngleProperty, rotate);
            }
            else
            {
                transform.BeginAnimation(RotateTransform.AngleProperty, null);
                transform.Angle = angle;
            }
        }

        /// <summary>
        /// Disarms a pending snap after the (re)bind it covers has settled, snapping the angle to
        /// the now-current value. DataBind priority runs after the rebind's own binding pushes, so
        /// a rebind whose value didn't change (no <see cref="OnIsExpandedChanged"/> call) doesn't
        /// leave the armed snap to swallow the next real toggle's animation.
        /// </summary>
        private static void ScheduleSettle(FrameworkElement fe, ChevronState state)
        {
            fe.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
            {
                if (!state.SnapNext) return;
                state.SnapNext = false;
                var transform = EnsureTransform(fe, state);
                transform.BeginAnimation(RotateTransform.AngleProperty, null);
                transform.Angle = GetIsExpanded(fe) ? ExpandedAngle : 0;
            }));
        }

        private static ChevronState EnsureState(FrameworkElement fe)
        {
            if (fe.GetValue(StateProperty) is ChevronState state) return state;
            state = new ChevronState();
            fe.SetValue(StateProperty, state);
            return state;
        }

        /// <summary>
        /// The behavior owns the host's RenderTransform (the host sets RenderTransformOrigin to
        /// center). Re-created if something else replaced it.
        /// </summary>
        private static RotateTransform EnsureTransform(FrameworkElement fe, ChevronState state)
        {
            if (state.Transform == null || !ReferenceEquals(fe.RenderTransform, state.Transform))
            {
                state.Transform = new RotateTransform(GetIsExpanded(fe) ? ExpandedAngle : 0);
                fe.RenderTransform = state.Transform;
            }
            return state.Transform;
        }
    }
}
