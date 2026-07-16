using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Indeterminate progress ring with two visual kinds selected by <see cref="WheelKind"/>:
    /// <see cref="Primitives.WheelKind.Arc"/> (default) is a rotating arc whose sweep breathes
    /// between <see cref="MinArc"/> and <see cref="MaxArc"/> degrees over a faint full-circle
    /// track — the template supplies the visuals (track ellipse + <c>PART_Arc</c> path) and this
    /// class animates two internal angles, rebuilding the arc geometry each frame.
    /// <see cref="Primitives.WheelKind.Dots"/> is the classic Windows progress ring — a chasing
    /// orbit of dots driven entirely by a template storyboard over the computed
    /// <see cref="DotDiameter"/> / <see cref="DotOffset"/> metrics. Either way, animation runs
    /// only while the control is visible, so a collapsed spinner (e.g. inside an idle
    /// <see cref="WWButton"/>) costs nothing.
    /// </summary>
    [TemplatePart(Name = "PART_Arc", Type = typeof(Path))]
    public class WWSpinningWheel : Control
    {
        static WWSpinningWheel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWSpinningWheel),
                new FrameworkPropertyMetadata(typeof(WWSpinningWheel)));
        }

        public WWSpinningWheel()
        {
            Loaded += (s, e) => { if (IsVisible) StartAnimations(); };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible) StartAnimations();
                else StopAnimations();
            };
            SizeChanged += (s, e) => UpdateDotMetrics();
        }

        #region Public Dependency Properties

        /// <summary>Visual kind of the wheel — breathing arc (default) or Windows-style dot orbit.</summary>
        public static readonly DependencyProperty WheelKindProperty =
            DependencyProperty.Register(nameof(WheelKind), typeof(WheelKind), typeof(WWSpinningWheel),
                new PropertyMetadata(WheelKind.Arc, OnAnimationPropertyChanged));
        public WheelKind WheelKind { get => (WheelKind)GetValue(WheelKindProperty); set => SetValue(WheelKindProperty, value); }

        /// <summary>Duration of one full breathe-and-rotate cycle.</summary>
        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(Duration), typeof(WWSpinningWheel),
                new PropertyMetadata(new Duration(TimeSpan.FromSeconds(2.0)), OnAnimationPropertyChanged));
        public Duration AnimationDuration { get => (Duration)GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

        /// <summary>Smallest arc sweep, in degrees.</summary>
        public static readonly DependencyProperty MinArcProperty =
            DependencyProperty.Register(nameof(MinArc), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(12.0, OnAnimationPropertyChanged));
        public double MinArc { get => (double)GetValue(MinArcProperty); set => SetValue(MinArcProperty, value); }

        /// <summary>Largest arc sweep, in degrees.</summary>
        public static readonly DependencyProperty MaxArcProperty =
            DependencyProperty.Register(nameof(MaxArc), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(220.0, OnAnimationPropertyChanged));
        public double MaxArc { get => (double)GetValue(MaxArcProperty); set => SetValue(MaxArcProperty, value); }

        /// <summary>Brush of the faint full-circle track behind the arc.</summary>
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(WWSpinningWheel),
                new PropertyMetadata(Brushes.Black));
        public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

        /// <summary>Brush of the animated arc.</summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(WWSpinningWheel),
                new PropertyMetadata(Brushes.Black));
        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        /// <summary>Stroke thickness of both the track and the arc.</summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(3.5));
        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        #endregion

        #region Dots Metrics (read-only, consumed by the Dots template)

        private static readonly DependencyPropertyKey DotDiameterPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(DotDiameter), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(0.0));
        public static readonly DependencyProperty DotDiameterProperty = DotDiameterPropertyKey.DependencyProperty;
        /// <summary>Diameter of each orbiting dot, derived from the control's size.</summary>
        public double DotDiameter => (double)GetValue(DotDiameterProperty);

        private static readonly DependencyPropertyKey DotOffsetPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(DotOffset), typeof(Thickness), typeof(WWSpinningWheel),
                new PropertyMetadata(default(Thickness)));
        public static readonly DependencyProperty DotOffsetProperty = DotOffsetPropertyKey.DependencyProperty;
        /// <summary>Top margin that places a dot on the orbit before its canvas rotates.</summary>
        public Thickness DotOffset => (Thickness)GetValue(DotOffsetProperty);

        private static readonly DependencyPropertyKey MaxSideLengthPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(MaxSideLength), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(0.0));
        public static readonly DependencyProperty MaxSideLengthProperty = MaxSideLengthPropertyKey.DependencyProperty;
        /// <summary>Smaller of the control's actual sides — caps the Dots template to a square.</summary>
        public double MaxSideLength => (double)GetValue(MaxSideLengthProperty);

        private static readonly DependencyPropertyKey IsLargeWheelPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsLargeWheel), typeof(bool), typeof(WWSpinningWheel),
                new PropertyMetadata(false));
        public static readonly DependencyProperty IsLargeWheelProperty = IsLargeWheelPropertyKey.DependencyProperty;
        /// <summary>True at 60px and above — the Dots template shows a sixth dot.</summary>
        public bool IsLargeWheel => (bool)GetValue(IsLargeWheelProperty);

        private void UpdateDotMetrics()
        {
            double side = Math.Min(ActualWidth, ActualHeight);
            if (side <= 0)
            {
                SetValue(DotDiameterPropertyKey, 0.0);
                SetValue(DotOffsetPropertyKey, default(Thickness));
                SetValue(MaxSideLengthPropertyKey, 0.0);
                SetValue(IsLargeWheelPropertyKey, false);
                return;
            }

            double diameter = side * 0.1 + (side <= 40.0 ? 1.0 : 0.0);
            SetValue(DotDiameterPropertyKey, diameter);
            SetValue(DotOffsetPropertyKey, new Thickness(0, side * 0.5 - diameter, 0, 0));
            SetValue(MaxSideLengthPropertyKey, side);
            SetValue(IsLargeWheelPropertyKey, side >= 60.0);
        }

        #endregion

        #region Internal Animated DPs

        private static readonly DependencyProperty HeadAngleProperty =
            DependencyProperty.Register("HeadAngle", typeof(double), typeof(WWSpinningWheel),
                new FrameworkPropertyMetadata(0.0, OnAngleChanged));

        private static readonly DependencyProperty TailAngleProperty =
            DependencyProperty.Register("TailAngle", typeof(double), typeof(WWSpinningWheel),
                new FrameworkPropertyMetadata(0.0, OnAngleChanged));

        #endregion

        #region Template

        private Path _arcPath;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _arcPath = GetTemplateChild("PART_Arc") as Path;
        }

        #endregion

        #region Arc Geometry

        private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWSpinningWheel)d).UpdateArcGeometry();

        private void UpdateArcGeometry()
        {
            if (_arcPath == null || ActualWidth == 0 || ActualHeight == 0) return;

            double thickness = StrokeThickness;
            double radius = (Math.Min(ActualWidth, ActualHeight) - thickness) / 2.0;
            if (radius <= 0) return;

            var center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            double head = (double)GetValue(HeadAngleProperty);
            double tail = (double)GetValue(TailAngleProperty);
            double sweep = Math.Max(head - tail, MinArc);
            if (sweep > 359.9) sweep = 359.9;

            double tailRad = (tail - 90) * Math.PI / 180.0;
            double headRad = (tail + sweep - 90) * Math.PI / 180.0;

            var startPoint = new Point(center.X + radius * Math.Cos(tailRad), center.Y + radius * Math.Sin(tailRad));
            var endPoint = new Point(center.X + radius * Math.Cos(headRad), center.Y + radius * Math.Sin(headRad));

            var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = sweep > 180.0,
                SweepDirection = SweepDirection.Clockwise,
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            _arcPath.Data = geometry;
        }

        #endregion

        #region Animation

        private static void OnAnimationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WWSpinningWheel control && control.IsVisible) control.StartAnimations();
        }

        private void StartAnimations()
        {
            if (WheelKind == WheelKind.Dots)
            {
                // The Dots template animates itself via an IsVisible-triggered storyboard; the
                // arc angle animations would only tick dead geometry updates.
                StopAnimations();
                return;
            }

            var duration = AnimationDuration.HasTimeSpan ? AnimationDuration.TimeSpan : TimeSpan.FromSeconds(2.4);
            double minSweep = MinArc;
            double maxGain = MaxArc - minSweep;
            int totalSegs = 16;
            double basePerSeg = 360.0 * 2.0 / totalSegs;
            double halfGain = maxGain / 2.0;

            var headAnim = new DoubleAnimationUsingKeyFrames { Duration = new Duration(duration), RepeatBehavior = RepeatBehavior.Forever };
            var tailAnim = new DoubleAnimationUsingKeyFrames { Duration = new Duration(duration), RepeatBehavior = RepeatBehavior.Forever };

            double headPos = 0.0, tailPos = 0.0;
            for (int i = 0; i < totalSegs; i++)
            {
                double pct = (double)(i + 1) / totalSegs;
                double valNext = Math.Pow(Math.Sin(Math.PI * (i + 1.0) / totalSegs), 2);
                double valCurr = Math.Pow(Math.Sin(Math.PI * (double)i / totalSegs), 2);
                double delta = valNext - valCurr;

                headPos += basePerSeg + halfGain * delta;
                tailPos += basePerSeg - halfGain * delta;

                headAnim.KeyFrames.Add(new LinearDoubleKeyFrame(headPos, KeyTime.FromPercent(pct)));
                tailAnim.KeyFrames.Add(new LinearDoubleKeyFrame(tailPos, KeyTime.FromPercent(pct)));
            }

            BeginAnimation(HeadAngleProperty, headAnim);
            BeginAnimation(TailAngleProperty, tailAnim);
        }

        private void StopAnimations()
        {
            BeginAnimation(HeadAngleProperty, null);
            BeginAnimation(TailAngleProperty, null);
        }

        #endregion
    }
}
