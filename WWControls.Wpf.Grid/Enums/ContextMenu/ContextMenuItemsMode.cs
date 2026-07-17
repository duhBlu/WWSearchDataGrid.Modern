namespace WWControls.Wpf
{
    /// <summary>
    /// How a <see cref="SearchDataGrid"/> surface's custom item collection
    /// (e.g. <see cref="SearchDataGrid.CellContextMenuItems"/>) combines with the built-in items.
    /// </summary>
    public enum ContextMenuItemsMode
    {
        /// <summary>
        /// Default. The custom items are appended beneath the built-ins (under a separator). The
        /// built-ins still show, minus anything in <see cref="SearchDataGrid.HiddenContextMenuItems"/>.
        /// </summary>
        Append,

        /// <summary>
        /// The built-ins are dropped entirely and the custom items <b>are</b> the menu — author them
        /// with the public <see cref="Commands.ContextMenuCommands"/> to keep any built-in behavior
        /// you still want.
        /// </summary>
        Replace,
    }
}
