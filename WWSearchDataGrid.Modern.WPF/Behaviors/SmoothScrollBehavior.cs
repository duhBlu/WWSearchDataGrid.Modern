using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Attached behavior that adds animated momentum scrolling to a <see cref="ScrollViewer"/>.
    /// Supports multiple easing modes via <see cref="AnimationModeProperty"/> and uses a
    /// decoupled two-offset system for smooth content interpolation.
    /// </summary>
    public static class SmoothScrollBehavior
    {
        #region Attached Properties

        public static readonly DependencyProperty EnableSmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableSmoothScroll", typeof(bool), typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnEnableSmoothScrollChanged));

        public static readonly DependencyProperty FrictionProperty =
            DependencyProperty.RegisterAttached(
                "Friction", typeof(double), typeof(SmoothScrollBehavior),
                new PropertyMetadata(0.92));

        public static readonly DependencyProperty AnimationModeProperty =
            DependencyProperty.RegisterAttached(
                "AnimationMode", typeof(ScrollAnimationMode), typeof(SmoothScrollBehavior),
                new PropertyMetadata(ScrollAnimationMode.EaseOut));

        /// <summary>
        /// Animatable attached property — when changed, calls ScrollToVerticalOffset on the
        /// target ScrollViewer. Consumers can target this property from a Storyboard to
        /// drive custom scroll animations.
        /// </summary>
        public static readonly DependencyProperty AnimatedVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset", typeof(double), typeof(SmoothScrollBehavior),
                new PropertyMetadata(0.0, OnAnimatedVerticalOffsetChanged));

        /// <summary>
        /// The SearchDataGrid that owns this ScrollViewer — used to raise the
        /// CustomScrollAnimation event when in Custom mode. Set by the grid itself.
        /// </summary>
        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.RegisterAttached(
                "OwnerGrid", typeof(SearchDataGrid), typeof(SmoothScrollBehavior),
                new PropertyMetadata(null));

        // Custom mode: accumulated target offset so rapid wheel ticks chain together
        private static readonly DependencyProperty PendingCustomTargetProperty =
            DependencyProperty.RegisterAttached("PendingCustomTarget", typeof(double),
                typeof(SmoothScrollBehavior), new PropertyMetadata(0.0));

        private static readonly DependencyProperty HasPendingCustomTargetProperty =
            DependencyProperty.RegisterAttached("HasPendingCustomTarget", typeof(bool),
                typeof(SmoothScrollBehavior), new PropertyMetadata(false));

        private static readonly DependencyProperty CurrentCustomStoryboardProperty =
            DependencyProperty.RegisterAttached("CurrentCustomStoryboard", typeof(Storyboard),
                typeof(SmoothScrollBehavior), new PropertyMetadata(null));

        public static bool GetEnableSmoothScroll(DependencyObject d) => d.GetValue(EnableSmoothScrollProperty) is true;
        public static void SetEnableSmoothScroll(DependencyObject d, bool value) => d.SetValue(EnableSmoothScrollProperty, value);
        public static double GetFriction(DependencyObject d) => (double)d.GetValue(FrictionProperty);
        public static void SetFriction(DependencyObject d, double value) => d.SetValue(FrictionProperty, value);
        public static ScrollAnimationMode GetAnimationMode(DependencyObject d) => (ScrollAnimationMode)d.GetValue(AnimationModeProperty);
        public static void SetAnimationMode(DependencyObject d, ScrollAnimationMode value) => d.SetValue(AnimationModeProperty, value);
        public static double GetAnimatedVerticalOffset(DependencyObject d) => (double)d.GetValue(AnimatedVerticalOffsetProperty);
        public static void SetAnimatedVerticalOffset(DependencyObject d, double value) => d.SetValue(AnimatedVerticalOffsetProperty, value);
        public static SearchDataGrid GetOwnerGrid(DependencyObject d) => (SearchDataGrid)d.GetValue(OwnerGridProperty);
        public static void SetOwnerGrid(DependencyObject d, SearchDataGrid value) => d.SetValue(OwnerGridProperty, value);

        private static void OnAnimatedVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
                sv.ScrollToVerticalOffset((double)e.NewValue);
        }

        #endregion

        #region State (per ScrollViewer)

        private static readonly DependencyProperty VelocityProperty =
            DependencyProperty.RegisterAttached("Velocity", typeof(double),
                typeof(SmoothScrollBehavior), new PropertyMetadata(0.0));

        // Impulse smoothing buffer. Wheel ticks add into this instead of directly into
        // Velocity; each render frame drains a fraction into Velocity. This turns the
        // step-change on each tick into a short ramp (~6 frames at 60fps), so free-spin
        // mouse wheels produce a visibly smooth acceleration/deceleration curve instead
        // of a choppy jump per tick. Total displacement per tick is preserved — the ramp
        // just redistributes velocity over time without changing the integral.
        private static readonly DependencyProperty PendingImpulseProperty =
            DependencyProperty.RegisterAttached("PendingImpulse", typeof(double),
                typeof(SmoothScrollBehavior), new PropertyMetadata(0.0));

        private static readonly DependencyProperty TargetOffsetProperty =
            DependencyProperty.RegisterAttached("TargetOffset", typeof(double),
                typeof(SmoothScrollBehavior), new PropertyMetadata(0.0));

        // For Linear/EaseInOut: track animation start time and initial velocity
        private static readonly DependencyProperty AnimStartTimeProperty =
            DependencyProperty.RegisterAttached("AnimStartTime", typeof(TimeSpan),
                typeof(SmoothScrollBehavior), new PropertyMetadata(TimeSpan.Zero));

        private static readonly DependencyProperty InitialVelocityProperty =
            DependencyProperty.RegisterAttached("InitialVelocity", typeof(double),
                typeof(SmoothScrollBehavior), new PropertyMetadata(0.0));

        private static readonly DependencyProperty IsAnimatingProperty =
            DependencyProperty.RegisterAttached("IsAnimating", typeof(bool),
                typeof(SmoothScrollBehavior), new PropertyMetadata(false));

        private static readonly DependencyProperty LastRenderTimeProperty =
            DependencyProperty.RegisterAttached("LastRenderTime", typeof(TimeSpan),
                typeof(SmoothScrollBehavior), new PropertyMetadata(TimeSpan.Zero));

        private static readonly DependencyProperty RenderHandlerProperty =
            DependencyProperty.RegisterAttached("RenderHandler", typeof(EventHandler),
                typeof(SmoothScrollBehavior), new PropertyMetadata(null));

        #endregion

        #region Enable/Disable

        private static void OnEnableSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv) return;

            if ((bool)e.NewValue)
                sv.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            else
            {
                sv.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                StopAnimation(sv);
            }
        }

        #endregion

        #region Mouse Wheel Handling

        // Velocity impulse added to the ScrollViewer per wheel notch. 500 px/s produces
        // ~100 px of coasting distance per notch under default friction, roughly 2× the
        // native WPF wheel-scroll distance — snappy but still controllable.
        private const double BaseImpulsePerNotch = 500.0;

        // Soft ceiling on accumulated velocity. Above this value, additional impulses
        // from continuous wheel spinning are damped asymptotically (not hard-clamped),
        // so users can still push past it with effort but can't trivially reach absurd
        // speeds. Raised from 10000 → 20000 in response to "feels capped too early" —
        // continuous scrolling now reaches ~4000 px/coast before the damping becomes
        // obvious, which covers a full screen on most monitors.
        private const double SoftCapVelocity = 20000.0;

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer sv) return;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) return;

            e.Handled = true;

            // Custom mode: delegate the entire scroll action to the consumer's Storyboard.
            // Accumulate targets across rapid wheel ticks so multiple ticks don't all
            // collapse to a single-tick scroll distance. Stop any running Storyboard
            // before starting a new one to prevent target conflicts.
            if (GetAnimationMode(sv) == ScrollAnimationMode.Custom)
            {
                var grid = GetOwnerGrid(sv);
                if (grid != null)
                {
                    double pixelsPerNotch = 48.0;
                    double delta = -(e.Delta / 120.0) * pixelsPerNotch * 3;

                    // Accumulate: if an animation is currently running, build on its target
                    // so rapid ticks add up. Otherwise, start from the current offset.
                    double pending = (double)sv.GetValue(PendingCustomTargetProperty);
                    bool hasPending = (bool)sv.GetValue(HasPendingCustomTargetProperty);
                    double fromTarget = hasPending ? pending : sv.VerticalOffset;

                    double oldOffset = sv.VerticalOffset;
                    double newOffset = Math.Clamp(fromTarget + delta, 0, sv.ScrollableHeight);

                    sv.SetValue(PendingCustomTargetProperty, newOffset);
                    sv.SetValue(HasPendingCustomTargetProperty, true);

                    var storyboard = grid.RaiseCustomScrollAnimation(oldOffset, newOffset);
                    if (storyboard != null)
                    {
                        // Stop any previously running storyboard so it doesn't fight the new one
                        var previous = (Storyboard)sv.GetValue(CurrentCustomStoryboardProperty);
                        previous?.Stop(sv);

                        // Seed the animated offset with current position so animation starts from "now"
                        SetAnimatedVerticalOffset(sv, oldOffset);

                        // Clear the pending target when this storyboard finishes
                        storyboard.Completed += (s, args) =>
                        {
                            sv.SetValue(HasPendingCustomTargetProperty, false);
                            sv.SetValue(CurrentCustomStoryboardProperty, null);
                        };

                        sv.SetValue(CurrentCustomStoryboardProperty, storyboard);
                        storyboard.Begin(sv, true);
                        return;
                    }
                    // No storyboard provided — fall through to default physics
                }
            }

            double impulse = -(e.Delta / 120.0) * BaseImpulsePerNotch;
            var currentVelocity = (double)sv.GetValue(VelocityProperty);
            var pendingImpulse = (double)sv.GetValue(PendingImpulseProperty);

            // Effective direction combines what's currently moving plus what's queued to be
            // applied. This prevents a mid-drain tick from being mis-classified as a reversal.
            double effectiveDirectional = currentVelocity + pendingImpulse;

            if (effectiveDirectional == 0.0 || Math.Sign(impulse) == Math.Sign(effectiveDirectional))
            {
                // Same direction (or starting from rest): accumulate into the impulse buffer.
                // The soft cap is evaluated against the current "committed" velocity so that
                // a long stream of same-direction ticks still plateaus at the intended ceiling.
                double absVel = Math.Abs(currentVelocity);
                double scale = absVel < SoftCapVelocity
                    ? 1.0
                    : SoftCapVelocity / (absVel + SoftCapVelocity);

                pendingImpulse += impulse * scale;
                sv.SetValue(PendingImpulseProperty, pendingImpulse);
            }
            else
            {
                // Direction reversal: the user wants an immediate response, not a gradual
                // blend. Clear everything and drop the new impulse straight onto velocity.
                currentVelocity = impulse;
                pendingImpulse = 0.0;
                sv.SetValue(VelocityProperty, currentVelocity);
                sv.SetValue(PendingImpulseProperty, 0.0);
            }

            // For Linear/EaseInOut, track the peak of the *effective* velocity (what the user
            // has asked for — already-applied + still-pending). Without this, the Linear
            // deceleration rate would be computed from a mid-ramp velocity that's lower than
            // the user's actual target, producing a too-short coast.
            double effectivePeak = currentVelocity + pendingImpulse;
            if (Math.Abs(effectivePeak) > Math.Abs((double)sv.GetValue(InitialVelocityProperty)))
                sv.SetValue(InitialVelocityProperty, effectivePeak);

            if (!(bool)sv.GetValue(IsAnimatingProperty))
            {
                sv.SetValue(TargetOffsetProperty, sv.VerticalOffset);
                sv.SetValue(AnimStartTimeProperty, TimeSpan.Zero);
                StartAnimation(sv);
            }
        }

        #endregion

        #region Animation Loop

        private const double CatchUpAlpha = 0.55;

        // Fraction of the pending impulse that remains after one 60fps frame's drain.
        // 0.46^6 ≈ 0.01 → 99% of a tick's impulse lands in velocity within ~100ms.
        // Chosen to be slightly longer than typical free-spin tick intervals during
        // deceleration, so consecutive ticks overlap and blend instead of stepping.
        private const double PendingImpulseRemainderPerFrame = 0.46;

        private static void StartAnimation(ScrollViewer sv)
        {
            sv.SetValue(IsAnimatingProperty, true);
            sv.SetValue(LastRenderTimeProperty, TimeSpan.Zero);

            EventHandler handler = (s, e) => OnRendering(sv, (RenderingEventArgs)e);
            sv.SetValue(RenderHandlerProperty, handler);
            CompositionTarget.Rendering += handler;
        }

        private static void StopAnimation(ScrollViewer sv)
        {
            var handler = (EventHandler)sv.GetValue(RenderHandlerProperty);
            if (handler != null)
            {
                CompositionTarget.Rendering -= handler;
                sv.SetValue(RenderHandlerProperty, null);
            }

            sv.SetValue(IsAnimatingProperty, false);
            sv.SetValue(VelocityProperty, 0.0);
            sv.SetValue(InitialVelocityProperty, 0.0);
            sv.SetValue(PendingImpulseProperty, 0.0);
        }

        private static void OnRendering(ScrollViewer sv, RenderingEventArgs e)
        {
            var lastTime = (TimeSpan)sv.GetValue(LastRenderTimeProperty);
            var currentTime = e.RenderingTime;

            if (lastTime == TimeSpan.Zero)
            {
                sv.SetValue(LastRenderTimeProperty, currentTime);
                sv.SetValue(AnimStartTimeProperty, currentTime);
                return;
            }

            if (currentTime == lastTime) return;

            double dt = (currentTime - lastTime).TotalSeconds;
            sv.SetValue(LastRenderTimeProperty, currentTime);
            if (dt > 0.1) dt = 0.1;

            var velocity = (double)sv.GetValue(VelocityProperty);
            var pendingImpulse = (double)sv.GetValue(PendingImpulseProperty);
            var friction = GetFriction(sv);
            var mode = GetAnimationMode(sv);

            // Drain a fraction of the pending impulse buffer into velocity. This turns
            // the step-change that each wheel tick would cause into a smooth ramp that
            // blends with any velocity already in motion. Frame-rate independent:
            // drainFactor asymptotes to the same per-wall-clock-second behavior regardless
            // of whether the monitor is 60Hz, 120Hz, or frames are stretched by load.
            if (pendingImpulse != 0.0)
            {
                double drainFactor = 1.0 - Math.Pow(PendingImpulseRemainderPerFrame, dt * 60.0);
                double drain = pendingImpulse * drainFactor;
                velocity += drain;
                pendingImpulse -= drain;

                // Snap to zero when the residue is below a pixel's worth of velocity to
                // keep the animation loop from spinning indefinitely on floating-point dust.
                if (Math.Abs(pendingImpulse) < 0.5)
                    pendingImpulse = 0.0;

                sv.SetValue(PendingImpulseProperty, pendingImpulse);
            }

            // Decay velocity based on animation mode
            switch (mode)
            {
                case ScrollAnimationMode.EaseOut:
                    // Exponential decay — fast start, slow finish
                    velocity *= Math.Pow(friction, dt * 60.0);
                    break;

                case ScrollAnimationMode.EaseInOut:
                    // Cubic ease in/out on the friction — starts gentle, peaks, then gentle stop
                    var animStart = (TimeSpan)sv.GetValue(AnimStartTimeProperty);
                    double elapsed = (currentTime - animStart).TotalSeconds;
                    double totalDuration = -1.0 / (Math.Log(friction) * 60.0) * Math.Log(0.01);
                    double progress = Math.Clamp(elapsed / Math.Max(totalDuration, 0.1), 0.0, 1.0);
                    // Smoothstep: stronger friction at start and end, less in middle
                    double smoothStep = progress * progress * (3.0 - 2.0 * progress);
                    double easedFriction = friction + (1.0 - friction) * 0.5 * (1.0 - Math.Sin(smoothStep * Math.PI));
                    velocity *= Math.Pow(easedFriction, dt * 60.0);
                    break;

                case ScrollAnimationMode.Linear:
                    // Constant deceleration — uniform slowdown
                    var initVel = (double)sv.GetValue(InitialVelocityProperty);
                    if (Math.Abs(initVel) > 0)
                    {
                        double decelPerSec = Math.Abs(initVel) / (-Math.Log(0.01) / (-Math.Log(friction) * 60.0));
                        double reduction = decelPerSec * dt;
                        if (Math.Abs(velocity) <= reduction)
                            velocity = 0;
                        else
                            velocity -= Math.Sign(velocity) * reduction;
                    }
                    break;

                case ScrollAnimationMode.Custom:
                    // Use the same EaseOut physics; the Custom event on SearchDataGrid
                    // is for per-scroll-input customization, not per-frame.
                    velocity *= Math.Pow(friction, dt * 60.0);
                    break;
            }

            // Advance target offset
            var targetOffset = (double)sv.GetValue(TargetOffsetProperty);
            targetOffset += velocity * dt;
            targetOffset = Math.Clamp(targetOffset, 0, sv.ScrollableHeight);
            sv.SetValue(TargetOffsetProperty, targetOffset);

            if ((targetOffset <= 0 && velocity < 0) ||
                (targetOffset >= sv.ScrollableHeight && velocity > 0))
                velocity = 0;

            // Interpolate actual scroll position toward target
            double lerpFactor = 1.0 - Math.Pow(1.0 - CatchUpAlpha, dt * 60.0);
            double currentOffset = sv.VerticalOffset;
            double gap = targetOffset - currentOffset;
            double newOffset = Math.Abs(gap) < 1.0
                ? targetOffset
                : currentOffset + gap * lerpFactor;

            sv.ScrollToVerticalOffset(newOffset);

            // Stop when velocity is negligible, content has caught up, AND no impulse
            // is still waiting to be drained. Without the pending check, a very slow
            // trickle tick could land in pending after the loop decides to stop.
            if (Math.Abs(velocity) < 10.0
                && Math.Abs(targetOffset - sv.VerticalOffset) < 2.0
                && pendingImpulse == 0.0)
            {
                sv.ScrollToVerticalOffset(targetOffset);
                StopAnimation(sv);
                return;
            }

            sv.SetValue(VelocityProperty, velocity);
        }

        #endregion
    }
}
