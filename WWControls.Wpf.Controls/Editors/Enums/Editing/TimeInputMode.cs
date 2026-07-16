namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Whether the date editor lets the user set a time-of-day (the popup's time surface — the
    /// segmented time editor under the calendar, or the hour / minute / AM-PM scroll columns).
    /// Independent of the text mask: a mask with time specifiers always edits time in the text
    /// field; this mode governs the popup surfaces.
    /// </summary>
    public enum TimeInputMode
    {
        /// <summary>
        /// Derive from the mask — time editing is enabled exactly when the resolved mask pattern
        /// contains time specifiers (<c>H</c>/<c>h</c>/<c>m</c>/<c>s</c>/<c>t</c>).
        /// </summary>
        Auto = 0,

        /// <summary>Time editing is always available in the popup, even with a date-only mask.</summary>
        Enabled,

        /// <summary>The popup never offers time editing, even when the mask carries time specifiers.</summary>
        Disabled,
    }
}
