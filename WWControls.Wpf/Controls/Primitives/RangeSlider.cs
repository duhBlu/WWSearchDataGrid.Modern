using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;

namespace WWControls.Wpf
{
    /// <summary>
    /// Horizontal range slider with two thumbs sharing one track. <see cref="LowValue"/> and
    /// <see cref="HighValue"/> are clamped so <c>LowValue ≤ HighValue</c> at all times; the
    /// thumbs cannot cross each other.
    /// </summary>
    /// <remarks>
    /// Template contract — the default style provides these named parts:
    /// <list type="bullet">
    /// <item><c>PART_Track</c> — the <see cref="Canvas"/> that hosts the two thumbs and the
    ///   selected-range highlight. Layout is computed against its <see cref="FrameworkElement.ActualWidth"/>.</item>
    /// <item><c>PART_LowThumb</c> / <c>PART_HighThumb</c> — <see cref="Thumb"/> instances whose
    ///   <see cref="Thumb.DragDelta"/> events the control wires up to move the corresponding value.</item>
    /// <item><c>PART_Range</c> — a <see cref="FrameworkElement"/> that the control sizes /
    ///   positions to highlight the region between the two thumbs. Optional.</item>
    /// </list>
    /// </remarks>
    public class RangeSlider : Control
    {
        static RangeSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(typeof(RangeSlider)));
        }

        public const string TrackPartName = "PART_Track";
        public const string LowThumbPartName = "PART_LowThumb";
        public const string HighThumbPartName = "PART_HighThumb";
        public const string RangePartName = "PART_Range";

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsArrange, OnRangeChanged));

        public static readonly DependencyProperty LowValueProperty =
            DependencyProperty.Register(
                nameof(LowValue),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLowValueChanged,
                    CoerceLowValue));

        public static readonly DependencyProperty HighValueProperty =
            DependencyProperty.Register(
                nameof(HighValue),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(
                    100.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnHighValueChanged,
                    CoerceHighValue));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(double),
                typeof(RangeSlider),
                new PropertyMetadata(1.0));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double LowValue
        {
            get => (double)GetValue(LowValueProperty);
            set => SetValue(LowValueProperty, value);
        }

        public double HighValue
        {
            get => (double)GetValue(HighValueProperty);
            set => SetValue(HighValueProperty, value);
        }

        /// <summary>
        /// Step used to round drag deltas. <c>0</c> or negative disables snapping (raw double).
        /// </summary>
        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        private Canvas _track;
        private Thumb _lowThumb;
        private Thumb _highThumb;
        private FrameworkElement _range;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_lowThumb != null)
                _lowThumb.DragDelta -= OnLowThumbDragDelta;
            if (_highThumb != null)
                _highThumb.DragDelta -= OnHighThumbDragDelta;
            if (_track != null)
                _track.SizeChanged -= OnTrackSizeChanged;

            _track = GetTemplateChild(TrackPartName) as Canvas;
            _lowThumb = GetTemplateChild(LowThumbPartName) as Thumb;
            _highThumb = GetTemplateChild(HighThumbPartName) as Thumb;
            _range = GetTemplateChild(RangePartName) as FrameworkElement;

            if (_lowThumb != null)
                _lowThumb.DragDelta += OnLowThumbDragDelta;
            if (_highThumb != null)
                _highThumb.DragDelta += OnHighThumbDragDelta;
            if (_track != null)
                _track.SizeChanged += OnTrackSizeChanged;

            UpdateThumbPositions();
        }

        private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e) => UpdateThumbPositions();

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var result = base.ArrangeOverride(arrangeBounds);
            UpdateThumbPositions();
            return result;
        }

        private void OnLowThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            double delta = PixelsToValue(e.HorizontalChange);
            if (delta == 0) return;
            LowValue = ClampAndSnap(LowValue + delta, Minimum, HighValue);
        }

        private void OnHighThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            double delta = PixelsToValue(e.HorizontalChange);
            if (delta == 0) return;
            HighValue = ClampAndSnap(HighValue + delta, LowValue, Maximum);
        }

        /// <summary>
        /// Converts a pixel offset along the track into a value offset. Returns <c>0</c> when
        /// the track hasn't laid out yet or the range collapsed to zero — keeps drag handlers
        /// from generating NaN values during the first frame.
        /// </summary>
        private double PixelsToValue(double pixels)
        {
            double trackWidth = TrackWidth;
            double range = Maximum - Minimum;
            if (trackWidth <= 0 || range <= 0) return 0;
            return pixels * range / trackWidth;
        }

        private double TrackWidth => (_track != null && _track.ActualWidth > 0)
            ? _track.ActualWidth - ThumbWidth
            : 0;

        private double ThumbWidth => _lowThumb != null && _lowThumb.ActualWidth > 0
            ? _lowThumb.ActualWidth
            : 12;

        private double ClampAndSnap(double value, double min, double max)
        {
            double clamped = Math.Max(min, Math.Min(max, value));
            if (Step > 0)
            {
                double steps = Math.Round((clamped - Minimum) / Step);
                clamped = Minimum + steps * Step;
                clamped = Math.Max(min, Math.Min(max, clamped));
            }
            return clamped;
        }

        private void UpdateThumbPositions()
        {
            if (_track == null) return;
            double range = Maximum - Minimum;
            if (range <= 0) return;

            double trackWidth = TrackWidth;
            if (trackWidth <= 0) return;

            double lowFrac = (LowValue - Minimum) / range;
            double highFrac = (HighValue - Minimum) / range;
            double lowX = lowFrac * trackWidth;
            double highX = highFrac * trackWidth;

            double trackHeight = _track.ActualHeight;

            if (_lowThumb != null)
            {
                Canvas.SetLeft(_lowThumb, lowX);
                Canvas.SetTop(_lowThumb, (trackHeight - _lowThumb.ActualHeight) / 2);
            }
            if (_highThumb != null)
            {
                Canvas.SetLeft(_highThumb, highX);
                Canvas.SetTop(_highThumb, (trackHeight - _highThumb.ActualHeight) / 2);
            }

            if (_range != null)
            {
                Canvas.SetLeft(_range, lowX + ThumbWidth / 2);
                Canvas.SetTop(_range, (trackHeight - _range.ActualHeight) / 2);
                _range.Width = Math.Max(0, highX - lowX);
            }
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (RangeSlider)d;
            slider.CoerceValue(LowValueProperty);
            slider.CoerceValue(HighValueProperty);
            slider.UpdateThumbPositions();
        }

        private static void OnLowValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (RangeSlider)d;
            slider.CoerceValue(HighValueProperty);
            slider.UpdateThumbPositions();
        }

        private static void OnHighValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = (RangeSlider)d;
            slider.CoerceValue(LowValueProperty);
            slider.UpdateThumbPositions();
        }

        private static object CoerceLowValue(DependencyObject d, object baseValue)
        {
            var slider = (RangeSlider)d;
            double v = (double)baseValue;
            return Math.Max(slider.Minimum, Math.Min(v, slider.HighValue));
        }

        private static object CoerceHighValue(DependencyObject d, object baseValue)
        {
            var slider = (RangeSlider)d;
            double v = (double)baseValue;
            return Math.Min(slider.Maximum, Math.Max(v, slider.LowValue));
        }
    }
}
