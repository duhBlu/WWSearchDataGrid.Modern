namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Specifies the easing mode for scroll animations when
    /// <see cref="SearchDataGrid.AllowScrollAnimation"/> is true.
    /// </summary>
    public enum ScrollAnimationMode
    {
        /// <summary>
        /// Starts quickly and then decelerates.
        /// </summary>
        EaseOut = 0,

        /// <summary>
        /// Starts slowly, accelerates and then decelerates.
        /// </summary>
        EaseInOut = 1,

        /// <summary>
        /// Moves smoothly at a constant deceleration rate.
        /// </summary>
        Linear = 2,

        /// <summary>
        /// Handle the <see cref="SearchDataGrid.CustomScrollAnimation"/> event
        /// to provide a custom animation effect.
        /// </summary>
        Custom = 3
    }
}
