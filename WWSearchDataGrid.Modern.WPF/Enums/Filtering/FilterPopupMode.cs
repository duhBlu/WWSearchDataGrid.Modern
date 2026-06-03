namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Layout strategy for the column-filter popup. Surfaced as
    /// <see cref="ColumnDataBase.FilterPopupMode"/>. Popup UI is deferred — these values are the
    /// intended shapes when it ships.
    /// </summary>
    public enum FilterPopupMode
    {
        /// <summary>
        /// Tabbed Excel-style popup: Filter Rules tab (condition builder) plus Filter Values tab.
        /// The Values tab renders a checkbox list for non-DateTime columns and a date tree
        /// (Year → Month → Day) for DateTime columns. Range / two-thumb numeric editors are
        /// NOT included automatically — request one explicitly by populating
        /// <see cref="ColumnDataBase.CustomColumnFilterTabs"/> with a tab whose template hosts a
        /// <see cref="RangeFilterElement"/>; any non-empty tabs collection makes the editor
        /// skip the default tabs entirely.
        /// </summary>
        Default = 0
    }
}
