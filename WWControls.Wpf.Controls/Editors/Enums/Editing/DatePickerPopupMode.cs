namespace WWControls.Wpf
{
    /// <summary>
    /// Which picker surface the date editor's dropdown popup presents.
    /// </summary>
    public enum DatePickerPopupMode
    {
        /// <summary>
        /// A month-view <see cref="System.Windows.Controls.Calendar"/> for mouse picking, with an
        /// optional segmented time editor below it when time editing is enabled.
        /// </summary>
        Calendar = 0,

        /// <summary>
        /// Looping scrollable columns — month, day, year, plus hour / minute / AM-PM when time
        /// editing is enabled. Each column wraps around; the centered row is the selected unit.
        /// </summary>
        ScrollList,
    }
}
