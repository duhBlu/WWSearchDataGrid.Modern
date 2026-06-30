namespace WWControls.Wpf
{
    /// <summary>
    /// How a <see cref="StatusIcon"/> animates when it becomes visible. Selected by the default
    /// template's triggers.
    /// </summary>
    public enum StatusIconAnimation
    {
        /// <summary>No animation — the badge simply appears.</summary>
        None = 0,

        /// <summary>Flash the badge three times (an opacity blink), then hold steady.</summary>
        Blink,

        /// <summary>Continuously pulse (scale in/out) while the badge is visible.</summary>
        Pulse,
    }
}
