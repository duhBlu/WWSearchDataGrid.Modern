using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WWSearchDataGrid.Modern.SampleApp.Controls
{
    public enum RotationDirection { Clockwise, CounterClockwise }

    /// <summary>
    /// Modern indeterminate progress ring using Path + ArcSegment.
    /// Adapted from wpfCabinetDesigner.
    /// </summary>
    public class WWSpinningWheel : Control
    {
        static WWSpinningWheel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWSpinningWheel),
                new FrameworkPropertyMetadata(typeof(WWSpinningWheel)));
        }

        #region Public Dependency Properties

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(Duration), typeof(WWSpinningWheel),
                new PropertyMetadata(new Duration(TimeSpan.FromSeconds(2.0)), OnAnimationPropertyChanged));
        public Duration AnimationDuration { get => (Duration)GetValue(AnimationDurationProperty); set => SetValue(AnimationDurationProperty, value); }

        public static readonly DependencyProperty MinArcProperty =
            DependencyProperty.Register(nameof(MinArc), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(12.0, OnAnimationPropertyChanged));
        public double MinArc { get => (double)GetValue(MinArcProperty); set => SetValue(MinArcProperty, value); }

        public static readonly DependencyProperty MaxArcProperty =
            DependencyProperty.Register(nameof(MaxArc), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(220.0, OnAnimationPropertyChanged));
        public double MaxArc { get => (double)GetValue(MaxArcProperty); set => SetValue(MaxArcProperty, value); }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(WWSpinningWheel),
                new PropertyMetadata(Brushes.Black));
        public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(WWSpinningWheel),
                new PropertyMetadata(Brushes.Black));
        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(WWSpinningWheel),
                new PropertyMetadata(3.5));
        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        public static readonly DependencyProperty RotationDirectionProperty =
            DependencyProperty.Register(nameof(RotationDirection), typeof(RotationDirection), typeof(WWSpinningWheel),
                new PropertyMetadata(RotationDirection.Clockwise));
        public RotationDirection RotationDirection { get => (RotationDirection)GetValue(RotationDirectionProperty); set => SetValue(RotationDirectionProperty, value); }

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

        private Path? _arcPath;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _arcPath = GetTemplateChild("PART_Arc") as Path;
            Loaded += (s, e) => { if (IsVisible) StartAnimations(); };
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible) StartAnimations();
                else StopAnimations();
            };
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
            if (_arcPath == null) return;

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
