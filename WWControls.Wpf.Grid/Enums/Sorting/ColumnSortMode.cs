namespace WWControls.Wpf
{
    /// <summary>
    /// How the column's values are compared when sorting. Surfaced as
    /// <see cref="ColumnDataBase.SortMode"/>. The WPF DataGrid currently honors only
    /// <see cref="Value"/> — the other modes are reserved for future wiring.
    /// </summary>
    public enum ColumnSortMode
    {
        /// <summary>Sort using the raw underlying value of the bound field.</summary>
        Value = 0,

        /// <summary>Sort using the displayed text (post-converter / format) instead of the raw value.</summary>
        DisplayText = 1,

        /// <summary>Sort using a consumer-supplied comparer. Not yet wired.</summary>
        Custom = 2
    }
}
