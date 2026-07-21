using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Image presenter that recolors a monochrome <see cref="DrawingImage"/> with a caller-supplied
    /// <see cref="Brush"/>, so one icon resource serves every control state. Each <see cref="Icon"/>
    /// tints its own clone; the source resource is never mutated. A <see langword="null"/>
    /// <see cref="IconBrush"/> renders untinted. Source icons must be single-color.
    /// </summary>
    public class Icon : Image
    {
        /// <summary>Source image — typically a monochrome <see cref="DrawingImage"/> from
        /// the icon library. A non-<see cref="DrawingImage"/> source (e.g. a bitmap) renders
        /// as-is; tinting and stroke overrides apply only to drawings.</summary>
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource),
                typeof(ImageSource),
                typeof(Icon),
                new PropertyMetadata(null, OnIconChanged));

        /// <summary>Brush applied to every fill and stroke in <see cref="IconSource"/>.
        /// Bind to the hosting control's <see cref="Control.Foreground"/> for trigger-driven color changes.</summary>
        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.Register(
                nameof(IconBrush),
                typeof(Brush),
                typeof(Icon),
                new PropertyMetadata(null, OnIconChanged));

        /// <summary>Optional override for the stroke thickness of every <see cref="Pen"/> in
        /// <see cref="IconSource"/>. 
        /// Defaults to <see cref="double.NaN"/>, which preserves the authored thickness.</summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(Icon),
                new PropertyMetadata(double.NaN, OnIconChanged));

        /// <inheritdoc cref="IconSourceProperty"/>
        public ImageSource IconSource
        {
            get => (ImageSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        /// <inheritdoc cref="IconBrushProperty"/>
        public Brush IconBrush
        {
            get => (Brush)GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        /// <inheritdoc cref="StrokeThicknessProperty"/>
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Icon)d).RebuildSource();
        }

        private void RebuildSource()
        {
            var source = IconSource;
            if (!(source is DrawingImage drawingImage) || drawingImage.Drawing == null)
            {
                // Bitmaps and other non-drawing sources render untinted, as-is.
                Source = source;
                return;
            }

            var brush = IconBrush;
            var thickness = StrokeThickness;
            var hasThicknessOverride = !double.IsNaN(thickness);

            if (brush == null && !hasThicknessOverride)
            {
                Source = drawingImage;
                return;
            }

            Source = new DrawingImage(CloneAndTint(drawingImage.Drawing, brush, thickness));
        }

        private static Drawing CloneAndTint(Drawing source, Brush tint, double strokeThicknessOverride)
        {
            switch (source)
            {
                case DrawingGroup group:
                    var groupClone = new DrawingGroup
                    {
                        ClipGeometry = group.ClipGeometry,
                        Opacity = group.Opacity,
                        OpacityMask = group.OpacityMask,
                        Transform = group.Transform,
                        GuidelineSet = group.GuidelineSet,
                    };
                    foreach (var child in group.Children)
                    {
                        groupClone.Children.Add(CloneAndTint(child, tint, strokeThicknessOverride));
                    }
                    return groupClone;

                case GeometryDrawing geometry:
                    var geometryClone = new GeometryDrawing
                    {
                        Geometry = geometry.Geometry,
                        Brush = geometry.Brush != null ? (tint ?? geometry.Brush) : null,
                    };
                    if (geometry.Pen != null)
                    {
                        geometryClone.Pen = new Pen
                        {
                            Brush = tint ?? geometry.Pen.Brush,
                            Thickness = double.IsNaN(strokeThicknessOverride) ? geometry.Pen.Thickness : strokeThicknessOverride,
                            StartLineCap = geometry.Pen.StartLineCap,
                            EndLineCap = geometry.Pen.EndLineCap,
                            LineJoin = geometry.Pen.LineJoin,
                            DashCap = geometry.Pen.DashCap,
                            DashStyle = geometry.Pen.DashStyle,
                            MiterLimit = geometry.Pen.MiterLimit,
                        };
                    }
                    return geometryClone;

                default:
                    return source.Clone();
            }
        }
    }
}
