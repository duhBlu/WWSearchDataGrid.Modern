namespace WWControls.Wpf
{
    /// <summary>
    /// Severity of a <see cref="StatusIcon"/>. Drives the badge's color and glyph via the default
    /// template's triggers; <see cref="None"/> hides the badge entirely.
    /// </summary>
    public enum StatusKind
    {
        /// <summary>No status — the badge is collapsed.</summary>
        None = 0,

        /// <summary>Informational (blue).</summary>
        Info,

        /// <summary>Success / valid (green).</summary>
        Success,

        /// <summary>Warning (amber).</summary>
        Warning,

        /// <summary>Error (red).</summary>
        Error,
    }
}
