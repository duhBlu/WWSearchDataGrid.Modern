namespace WWControls.Wpf
{
    /// <summary>
    /// Selects how a <see cref="SearchDataGrid"/> presents full-row editing as an edit
    /// <em>form</em> — caption/editor pairs in a form layout — instead of the column-aligned
    /// "edit entire row" strip. When this is anything but <see cref="None"/>, the row promotes
    /// into the form (rather than the strip) at the moment <see cref="RowEditTrigger"/> fires, or
    /// on an explicit <see cref="SearchDataGrid.ShowEditForm(object)"/> call.
    /// </summary>
    public enum EditFormShowMode
    {
        /// <summary>
        /// Edit-form mode is off. Full-row editing (when <see cref="RowEditTrigger"/> is not
        /// <see cref="RowEditTrigger.Never"/>) uses the column-aligned editor strip. This is the
        /// default.
        /// </summary>
        None = 0,

        /// <summary>
        /// The form appears inline beneath the edited row (hosted in the row's details area) while
        /// the row's own cells stay visible above it.
        /// </summary>
        Inline,

        /// <summary>
        /// The form appears inline in place of the edited row — the row's cells are hidden so only
        /// the form shows for that row.
        /// </summary>
        InlineHideRow,
    }
}
