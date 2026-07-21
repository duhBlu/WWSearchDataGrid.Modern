namespace WWControls.Wpf
{
    /// <summary>
    /// Stable identifiers for the built-in <see cref="SearchDataGrid"/> right-click items, used to
    /// hide them (<see cref="SearchDataGrid.HiddenContextMenuItems"/>) or to find / relabel / remove
    /// them from a <c>ContextMenuInitializing</c> handler. IDs are <b>semantic</b>, not per-surface:
    /// the same action shares one ID across menus (e.g. <see cref="Copy"/> is the Copy item in the
    /// cell, row-header, and column-header menus), so hiding it hides it everywhere. For per-surface
    /// control, filter on the event's <c>MenuType</c> before acting.
    /// <para>
    /// The theme tags each built-in item with one of these via the attached
    /// <see cref="ContextMenuExtensions.ItemIdProperty"/>. Items with no tag report
    /// <see cref="None"/> and are not targetable by ID.
    /// </para>
    /// </summary>
    public enum GridContextMenuItem
    {
        /// <summary>Not a built-in item / untagged. Never matches a hide or lookup.</summary>
        None = 0,

        // ── Copy (cell, row-header, column-header menus) ──
        /// <summary>Copy selected cell values.</summary>
        Copy,
        /// <summary>Copy selected cell values with column headers.</summary>
        CopyWithHeaders,
        /// <summary>Copy the single right-clicked cell's value (cell menu).</summary>
        CopyCellValue,
        /// <summary>Filter the column by the right-clicked cell's value (cell menu).</summary>
        FilterByCellValue,

        // ── Sorting (column-header menu) ──
        /// <summary>Sort the column ascending.</summary>
        SortAscending,
        /// <summary>Sort the column descending.</summary>
        SortDescending,
        /// <summary>Clear sorting on the column.</summary>
        ClearSorting,

        // ── Grouping (column-header menu) ──
        /// <summary>Group by the clicked column.</summary>
        GroupByColumn,
        /// <summary>Show / hide the group panel.</summary>
        ToggleGroupPanel,

        // ── Total summaries (column-header menu) ──
        /// <summary>The "Total Summaries" submenu.</summary>
        TotalSummaries,
        /// <summary>Show / hide the total summary row.</summary>
        ToggleTotalSummaryRow,
        /// <summary>Show / hide the fixed total summary strip.</summary>
        ToggleFixedTotalSummary,
        /// <summary>Open the column-scoped totals editor.</summary>
        CustomizeTotals,

        // ── Sizing (column-header menu) ──
        /// <summary>Best-fit the clicked column.</summary>
        BestFitColumn,
        /// <summary>Best-fit all columns.</summary>
        BestFitAllColumns,

        // ── Visibility / layout (column-header + grid-body menus) ──
        /// <summary>Open the column chooser.</summary>
        ColumnChooser,
        /// <summary>Hide the clicked column.</summary>
        HideColumn,

        // ── Fixed (pinned) columns (column-header menu) ──
        /// <summary>The "Fixed" (pin) submenu.</summary>
        FixedColumn,
        /// <summary>Pin the column to the left.</summary>
        PinColumnLeft,
        /// <summary>Pin the column to the right.</summary>
        PinColumnRight,
        /// <summary>Unpin the column.</summary>
        UnpinColumn,

        // ── Filtering (column-header menu) ──
        /// <summary>Clear the clicked column's filter.</summary>
        ClearColumnFilter,
        /// <summary>Open the filter editor.</summary>
        OpenFilterEditor,

        // ── Grid-body menu ──
        /// <summary>Clear all filters.</summary>
        ClearAllFilters,
        /// <summary>Save the current layout to a file.</summary>
        SaveLayout,
        /// <summary>Load a saved layout from a file.</summary>
        LoadLayout,
        /// <summary>Export to CSV.</summary>
        ExportToCsv,
        /// <summary>Export to Excel.</summary>
        ExportToExcel,
        /// <summary>Reset the grid layout.</summary>
        ResetLayout,
    }
}
