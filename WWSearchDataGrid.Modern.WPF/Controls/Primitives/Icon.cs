using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Image presenter that recolors a monochrome <see cref="DrawingImage"/> with a caller-supplied
    /// <see cref="Brush"/> at render time. Designed for button-content icons defined under
    /// <see cref="IconKeys"/> so a single resource serves every state (default / hover / pressed /
    /// disabled) of every button, driven by the host control's <see cref="Control.Foreground"/>.
    /// </summary>
    /// <remarks>
    /// Usage inside a control template:
    /// <code>
    /// &lt;sdg:Icon IconSource="{StaticResource {x:Static sdg:IconKeys.IconClose}}"
    ///              IconBrush="{TemplateBinding Foreground}"
    ///              Width="12" Height="12" /&gt;
    /// </code>
    /// When the templated parent's <c>Foreground</c> changes (e.g. via an <c>IsMouseOver</c> trigger),
    /// <see cref="IconBrushProperty"/> updates and the underlying <see cref="DrawingImage"/> is
    /// rebuilt with the new brush. The resource referenced by <see cref="IconSource"/> is never
    /// mutated — each <see cref="Icon"/> owns its tinted clone.
    ///
    /// <para>If <see cref="IconBrush"/> is <see langword="null"/>, the original
    /// <see cref="IconSource"/> is rendered untinted — useful when the same icon set is reused in a
    /// purely decorative context.</para>
    ///
    /// <para>Source icons should be authored as a single foreground color
    /// (any opaque <see cref="GeometryDrawing.Brush"/> or <see cref="Pen.Brush"/>). All fills and
    /// strokes in the drawing tree are replaced — multi-color icons are not supported here and
    /// should remain in <see cref="SearchTypeIconKeys"/>, rendered with a plain
    /// <see cref="Image"/>.</para>
    /// </remarks>
    public class Icon : Image
    {
        /// <summary>Source <see cref="DrawingImage"/> resource — typically a <see cref="IconKeys"/> entry.</summary>
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource),
                typeof(DrawingImage),
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
        /// <see cref="IconSource"/>, expressed in source-coordinate units. Use to compensate when an
        /// icon authored in a 24-unit viewport is rendered at a smaller size and its strokes look
        /// thin. Defaults to <see cref="double.NaN"/>, which preserves the authored thickness.</summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(Icon),
                new PropertyMetadata(double.NaN, OnIconChanged));

        /// <inheritdoc cref="IconSourceProperty"/>
        public DrawingImage IconSource
        {
            get => (DrawingImage)GetValue(IconSourceProperty);
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
            if (source?.Drawing == null)
            {
                Source = null;
                return;
            }

            var brush = IconBrush;
            var thickness = StrokeThickness;
            var hasThicknessOverride = !double.IsNaN(thickness);

            if (brush == null && !hasThicknessOverride)
            {
                Source = source;
                return;
            }

            Source = new DrawingImage(CloneAndTint(source.Drawing, brush, thickness));
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
