using System.Windows;

namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// Attached properties that extend any control with restylable chrome knobs the library's
    /// templates bind to — the same pattern ModernWpf uses with its <c>ControlHelper</c>. A style
    /// sets the property (typically from a shared theme resource), the control template consumes it
    /// via <c>{TemplateBinding ww:ControlHelper.CornerRadius}</c>, and a consumer overrides it per
    /// instance or per style without retemplating.
    /// </summary>
    public static class ControlHelper
    {
        /// <summary>
        /// Corner rounding of a control's chrome border. The editor styles default it to the shared
        /// <c>PrimitiveThemeKeys.ControlCornerRadius</c> resource; hosts that flatten their editors
        /// (grid cells, the row-edit strip) square it so the editor sits flush in its cell.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached("CornerRadius", typeof(CornerRadius), typeof(ControlHelper),
                new FrameworkPropertyMetadata(default(CornerRadius)));

        public static CornerRadius GetCornerRadius(DependencyObject obj) => (CornerRadius)obj.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(DependencyObject obj, CornerRadius value) => obj.SetValue(CornerRadiusProperty, value);
    }
}
