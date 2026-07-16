namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>How a <see cref="WWButton"/> responds to being clicked.</summary>
    public enum ButtonKind
    {
        /// <summary>A plain button — <c>Click</c> fires once per click.</summary>
        Simple,

        /// <summary>
        /// A repeat button — <c>Click</c> fires on press and then repeatedly until release,
        /// paced by <see cref="WWButton.Delay"/> and <see cref="WWButton.Interval"/>.
        /// </summary>
        Repeat,

        /// <summary>
        /// A toggle button — each click cycles <see cref="WWButton.IsChecked"/> between pressed and
        /// released (and indeterminate when <see cref="WWButton.IsThreeState"/> is set).
        /// </summary>
        Toggle,
    }
}
