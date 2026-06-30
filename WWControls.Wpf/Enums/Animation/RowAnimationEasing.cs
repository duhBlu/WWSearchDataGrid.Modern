namespace WWControls.Wpf
{
    /// <summary>
    /// Specifies the easing curve for the cascade update row opacity animation.
    /// </summary>
    public enum RowAnimationEasing
    {
        /// <summary>
        /// No easing — instant transition to full opacity (effectively disables the visual animation).
        /// </summary>
        None = 0,

        /// <summary>
        /// Constant rate interpolation from transparent to opaque. No acceleration or deceleration.
        /// </summary>
        Linear = 1,

        /// <summary>
        /// Starts fast, then decelerates to a stop. The most natural-feeling fade-in.
        /// </summary>
        EaseOut = 2,

        /// <summary>
        /// Starts slowly, then accelerates toward full opacity.
        /// </summary>
        EaseIn = 3,

        /// <summary>
        /// Starts slowly, accelerates through the middle, then decelerates at the end.
        /// </summary>
        EaseInOut = 4
    }
}
