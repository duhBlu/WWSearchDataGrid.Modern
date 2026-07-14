using System.Windows;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Inheritable attached properties the date editor sets on its popup <c>Calendar</c> so the
    /// keyed <c>CalendarDayButton</c> style — which lives in the theme layer and has no reference
    /// back to the editor — can react to per-picker options. Because both properties are
    /// <see cref="FrameworkPropertyMetadataOptions.Inherits"/>, setting them once on the
    /// <c>Calendar</c> flows the value down to every generated day button, where day-button
    /// triggers read it via a self-relative binding.
    /// </summary>
    public static class CalendarDecorations
    {
        /// <summary>
        /// When <c>true</c>, day-button triggers render Saturday / Sunday cells as disabled
        /// (faded, non-interactive). The editor still guards selection in code — this attached
        /// flag only drives the visual + mouse block; the authoritative rejection lives in
        /// <see cref="SegmentedDateTimeEditor"/>.
        /// </summary>
        public static readonly DependencyProperty DisableWeekendsProperty =
            DependencyProperty.RegisterAttached(
                "DisableWeekends", typeof(bool), typeof(CalendarDecorations),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetDisableWeekends(DependencyObject element, bool value)
            => element.SetValue(DisableWeekendsProperty, value);

        public static bool GetDisableWeekends(DependencyObject element)
            => (bool)element.GetValue(DisableWeekendsProperty);

        /// <summary>
        /// When <c>true</c>, day-button triggers accent US federal holidays (see
        /// <see cref="UsFederalHolidays"/>) with a marker dot and a naming tooltip. Highlight
        /// only — holiday dates stay selectable.
        /// </summary>
        public static readonly DependencyProperty HighlightHolidaysProperty =
            DependencyProperty.RegisterAttached(
                "HighlightHolidays", typeof(bool), typeof(CalendarDecorations),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetHighlightHolidays(DependencyObject element, bool value)
            => element.SetValue(HighlightHolidaysProperty, value);

        public static bool GetHighlightHolidays(DependencyObject element)
            => (bool)element.GetValue(HighlightHolidaysProperty);
    }
}
