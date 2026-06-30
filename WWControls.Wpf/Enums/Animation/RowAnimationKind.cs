namespace WWControls.Wpf
{
    /// <summary>
    /// Specifies the animation played while data rows are being asynchronously loaded
    /// during cascading data updates.
    /// </summary>
    public enum RowAnimationKind
    {
        /// <summary>
        /// No animation is played. Rows load synchronously as normal.
        /// </summary>
        None = 0,

        /// <summary>
        /// Displays rows that are being loaded by animating their opacity from 0 to 1.
        /// The easing curve is controlled by <see cref="SearchDataGrid.RowAnimationEasing"/>.
        /// </summary>
        Opacity = 1,

        /// <summary>
        /// A custom animation, implemented within the
        /// <see cref="SearchDataGrid.RowAnimationBegin"/> event handler, is played.
        /// </summary>
        Custom = 2
    }
}
