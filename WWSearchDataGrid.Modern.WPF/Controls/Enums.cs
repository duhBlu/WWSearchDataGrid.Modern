namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Specifies the default search mode for simple textbox searches in column filters.
    /// </summary>
    public enum DefaultSearchMode
    {
        /// <summary>
        /// Finds matches anywhere in the value (default behavior).
        /// </summary>
        Contains = 0,

        /// <summary>
        /// Finds matches that start with the search text.
        /// </summary>
        StartsWith = 1,

        /// <summary>
        /// Finds matches that end with the search text.
        /// </summary>
        EndsWith = 2,

        /// <summary>
        /// Finds exact matches only.
        /// </summary>
        Equals = 3
    }

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

    /// <summary>
    /// Defines the scope of items affected by the select-all checkbox operation.
    /// </summary>
    public enum SelectAllScope
    {
        /// <summary>
        /// Affects only the currently filtered/visible rows in the data grid.
        /// </summary>
        FilteredRows = 0,

        /// <summary>
        /// Affects only the currently selected rows.
        /// </summary>
        SelectedRows = 1,

        /// <summary>
        /// Affects all items in the ItemsSource regardless of filtering or selection state.
        /// </summary>
        AllItems = 2
    }
}
