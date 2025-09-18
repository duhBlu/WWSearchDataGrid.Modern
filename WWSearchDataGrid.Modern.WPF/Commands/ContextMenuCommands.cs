using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    /// <summary>
    /// Collection of placeholder context menu commands for SearchDataGrid
    /// All commands are placeholder implementations that log the action and can be implemented later
    /// </summary>
    internal static class ContextMenuCommands
    {
        #region Sorting Commands

        /// <summary>
        /// Sorts the column in ascending order
        /// </summary>
        public static ICommand SortAscendingCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Sort Ascending: Column '{column?.Header}' - Not implemented");
            // TODO: Implement ascending sort logic
        }, column => column != null);

        /// <summary>
        /// Sorts the column in descending order
        /// </summary>
        public static ICommand SortDescendingCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Sort Descending: Column '{column?.Header}' - Not implemented");
            // TODO: Implement descending sort logic
        }, column => column != null);

        #endregion

        #region Visibility & Layout Commands

        /// <summary>
        /// Opens the column management editor dialog
        /// </summary>
        public static ICommand ShowColumnEditorCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Column Editor - Not implemented");
            // TODO: Create and show ColumnEditor dialog
        }, grid => grid != null);

        /// <summary>
        /// Hides the selected column
        /// </summary>
        public static ICommand HideSelectedColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Hide Column: '{column?.Header}' - Not implemented");
            // TODO: Set column visibility to false
        }, column => column != null);

        /// <summary>
        /// Toggles the freeze state of a column (freezes if unfrozen, unfreezes if frozen)
        /// </summary>
        public static ICommand FreezeColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Freeze Column: '{column?.Header}' - Not implemented");
            // TODO: Set column to frozen state to keep it visible during horizontal scroll
        }, column => column != null);

        /// <summary>
        /// Toggles the freeze state of a column (unfreezes if frozen, freezes if unfrozen)
        /// </summary>
        public static ICommand UnfreezeColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Unfreeze Column: '{column?.Header}' - Not implemented");
            // TODO: Remove column from frozen state
        }, column => column != null);

        /// <summary>
        /// Toggles the pin state of a column (pins if unpinned, unpins if pinned)
        /// </summary>
        public static ICommand PinColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Pin Column: '{column?.Header}' - Not implemented");
            // TODO: Move column to pinned zone, persisting across layout changes
        }, column => column != null);

        /// <summary>
        /// Toggles the pin state of a column (unpins if pinned, pins if unpinned)
        /// </summary>
        public static ICommand UnpinColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Unpin Column: '{column?.Header}' - Not implemented");
            // TODO: Remove column from pinned zone
        }, column => column != null);

        /// <summary>
        /// Resets the layout to default
        /// </summary>
        public static ICommand ResetLayoutCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Reset Layout - Not implemented");
            // TODO: Reset column widths, order, and visibility to defaults
        }, grid => grid != null);

        /// <summary>
        /// Saves the current column layout as a named profile
        /// </summary>
        public static ICommand SaveCurrentProfileCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Save Current Profile - Not implemented");
            // TODO: Save current column layout (visibility, order, widths, filters) as named profile
        }, grid => grid != null);

        /// <summary>
        /// Loads a saved column profile
        /// </summary>
        public static ICommand LoadProfileCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Load Profile - Not implemented");
            // TODO: Show profile selection dialog and apply selected profile
        }, grid => grid != null);

        /// <summary>
        /// Opens the profile management dialog
        /// </summary>
        public static ICommand ManageProfilesCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Manage Profiles - Not implemented");
            // TODO: Open dialog to rename, delete, or organize saved profiles
        }, grid => grid != null);

        #endregion

        #region Sizing & Alignment Commands

        /// <summary>
        /// Auto-sizes the current column to fit content
        /// </summary>
        public static ICommand BestFitColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Best Fit Column: '{column?.Header}' - Not implemented");
            // TODO: Auto-size column to fit content
        }, column => column != null);

        /// <summary>
        /// Auto-sizes all columns to fit content
        /// </summary>
        public static ICommand BestFitAllColumnsCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Best Fit All Columns - Not implemented");
            // TODO: Auto-size all columns to fit content
        }, grid => grid != null);

        /// <summary>
        /// Sets column alignment to left
        /// </summary>
        public static ICommand SetLeftAlignmentCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Set Left Alignment: Column '{column?.Header}' - Not implemented");
            // TODO: Set column text alignment to left
        }, column => column != null);

        /// <summary>
        /// Sets column alignment to center
        /// </summary>
        public static ICommand SetCenterAlignmentCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Set Center Alignment: Column '{column?.Header}' - Not implemented");
            // TODO: Set column text alignment to center
        }, column => column != null);

        /// <summary>
        /// Sets column alignment to right
        /// </summary>
        public static ICommand SetRightAlignmentCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Set Right Alignment: Column '{column?.Header}' - Not implemented");
            // TODO: Set column text alignment to right
        }, column => column != null);

        #endregion

        #region Filtering & Searching Commands

        /// <summary>
        /// Shows the advanced filter editor for the column
        /// </summary>
        public static ICommand ShowFilterEditorCommand => new RelayCommand<ColumnSearchBox>(columnSearchBox =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Filter Editor: Column '{columnSearchBox?.CurrentColumn?.Header}' - Not implemented");
            // TODO: Open existing ColumnFilterEditor for this column
        }, columnSearchBox => columnSearchBox != null);

        /// <summary>
        /// Clears the filter on the current column
        /// </summary>
        public static ICommand ClearColumnFilterCommand => new RelayCommand<ColumnSearchBox>(columnSearchBox =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Clear Column Filter: Column '{columnSearchBox?.CurrentColumn?.Header}' - Not implemented");
            // TODO: Call columnSearchBox.ClearFilter()
        }, columnSearchBox => columnSearchBox?.HasActiveFilter == true);

        /// <summary>
        /// Clears all filters on the grid
        /// </summary>
        public static ICommand ClearAllFiltersCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Clear All Filters - Not implemented");
            // TODO: Call grid.ClearAllFilters()
        }, grid => grid?.DataColumns?.Any(c => c.HasActiveFilter) == true);

        /// <summary>
        /// Saves the current set of filters as a reusable preset
        /// </summary>
        public static ICommand SaveFilterPresetCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Save Filter Preset - Not implemented");
            // TODO: Store current filter configuration as named preset for reuse
        }, grid => grid?.DataColumns?.Any(c => c.HasActiveFilter) == true);

        /// <summary>
        /// Applies a previously saved filter preset
        /// </summary>
        public static ICommand LoadFilterPresetCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Load Filter Preset - Not implemented");
            // TODO: Show preset selection dialog and apply selected filter preset
        }, grid => grid != null);

        /// <summary>
        /// Toggles the visibility of the filter panel
        /// </summary>
        public static ICommand ToggleFilterPanelCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Filter Panel - Not implemented");
            // TODO: Toggle FilterPanel visibility
        }, grid => grid?.FilterPanel != null);

        /// <summary>
        /// Toggles the visibility of search controls
        /// </summary>
        public static ICommand ToggleSearchControlCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Search Controls - Not implemented");
            // TODO: Toggle search box visibility in column headers
        }, grid => grid != null);

        #endregion

        #region Grouping Commands

        /// <summary>
        /// Groups the data by the selected column
        /// </summary>
        public static ICommand GroupByColumnCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Group By Column: '{column?.Header}' - Not implemented");
            // TODO: Implement grouping by column
        }, column => column != null);

        /// <summary>
        /// Toggles the visibility of the grouping panel
        /// </summary>
        public static ICommand ToggleGroupPanelCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Group Panel - Not implemented");
            // TODO: Toggle grouping panel visibility
        }, grid => grid != null);

        #endregion

        #region Data & Export Commands

        /// <summary>
        /// Copies values of all selected cells to clipboard
        /// </summary>
        public static ICommand CopySelectedCellValuesCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            try
            {
                if (grid?.SelectedCells is { Count: > 0 })
                {
                    // Group by row item
                    var lines = grid.SelectedCells
                        .GroupBy(sc => sc.Item)
                        .Select(rowGroup =>
                        {
                            // For this row, collect (displayIndex, value) for cells that have a binding
                            var cellsForRow = rowGroup
                                .Select(sc =>
                                {
                                    var path = GetBindingPath(sc.Column);
                                    if (string.IsNullOrEmpty(path) || sc.Item is null) return null;

                                    var val = ReflectionHelper.GetPropValue(sc.Item, path);
                                    return new
                                    {
                                        DisplayIndex = sc.Column.DisplayIndex,
                                        Value = val?.ToString() ?? "NULL"
                                    };
                                })
                                .Where(x => x != null)
                                // In case the same column is selected multiple times for a row, keep the first
                                .GroupBy(x => x!.DisplayIndex)
                                .Select(g => g.First()!)
                                .OrderBy(x => x.DisplayIndex)
                                .Select(x => x.Value);

                            return string.Join("\t", cellsForRow);
                        })
                        // Only keep non-empty lines
                        .Where(line => !string.IsNullOrEmpty(line))
                        .ToList();

                    if (lines.Count > 0)
                    {
                        var result = string.Join(Environment.NewLine, lines);
                        Clipboard.SetText(result);
                        Debug.WriteLine($"Copied {lines.Count} row(s) of selected cell values to clipboard");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error copying selected cell values: {ex.Message}");
            }
        }, grid => grid?.SelectedCells?.Count > 0);


        /// <summary>
        /// Copies values of all selected cells with headers to clipboard
        /// </summary>
        public static ICommand CopySelectedCellValuesWithHeadersCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            try
            {
                if (grid?.SelectedCells is { Count: > 0 })
                {
                    // 1) Determine the distinct selected columns in DisplayIndex order (for headers & alignment)
                    var selectedColumns = grid.SelectedCells
                        .Select(sc => sc.Column)
                        .Distinct()
                        .OrderBy(c => c.DisplayIndex)
                        .ToList();

                    if (selectedColumns.Count == 0) return;

                    // 2) Build header row from those columns
                    var headers = selectedColumns
                        .Select(col => col.Header?.ToString() ?? string.Empty)
                        .ToList();

                    // 3) Group selected cells by row item, then order rows by the grid's visual order
                    var rowGroups = grid.SelectedCells
                        .GroupBy(sc => sc.Item)
                        .OrderBy(g => grid.Items.IndexOf(g.Key)) // preserves row order as displayed
                        .ToList();

                    // 4) For each row, emit values aligned to headers/columns.
                    //    Only cells that were actually selected in that row/column get a value; others are blank.
                    var lines = new List<string> { string.Join("\t", headers) };

                    foreach (var rowGroup in rowGroups)
                    {
                        var rowItem = rowGroup.Key;

                        // Build a quick lookup of selected cells by column for this row
                        var cellsByColumn = rowGroup
                            .GroupBy(sc => sc.Column)
                            .ToDictionary(g => g.Key, g => g.First()); // if multiple, take the first

                        var rowValues = new List<string>(selectedColumns.Count);

                        foreach (var col in selectedColumns)
                        {
                            if (!cellsByColumn.TryGetValue(col, out var cellForCol))
                            {
                                // Column not selected for this row -> blank
                                rowValues.Add(string.Empty);
                                continue;
                            }

                            var path = GetBindingPath(col);
                            if (string.IsNullOrEmpty(path) || rowItem is null)
                            {
                                rowValues.Add(string.Empty);
                                continue;
                            }

                            var value = ReflectionHelper.GetPropValue(rowItem, path);
                            rowValues.Add(value?.ToString() ?? "NULL");
                        }

                        lines.Add(string.Join("\t", rowValues));
                    }

                    var result = string.Join(Environment.NewLine, lines);
                    Clipboard.SetText(result);
                    Debug.WriteLine($"Copied {rowGroups.Count} row(s) with {selectedColumns.Count} column header(s) to clipboard");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error copying selected cell values with headers: {ex.Message}");
            }
        }, grid => grid?.SelectedCells?.Count > 0);

        /// <summary>
        /// Exports the grid data to CSV format
        /// </summary>
        public static ICommand ExportToCsvCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Export To CSV - Not implemented");
            // TODO: Export grid data to CSV file
        }, grid => grid?.Items?.Count > 0);

        /// <summary>
        /// Exports the grid data to Excel format
        /// </summary>
        public static ICommand ExportToExcelCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Export To Excel - Not implemented");
            // TODO: Export grid data to Excel file
        }, grid => grid?.Items?.Count > 0);

        #endregion

        #region Summaries & Aggregates Commands

        /// <summary>
        /// Shows count summary for the column
        /// </summary>
        public static ICommand ShowCountSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Count Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show count summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows sum summary for the column
        /// </summary>
        public static ICommand ShowSumSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Sum Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show sum summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows average summary for the column
        /// </summary>
        public static ICommand ShowAverageSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Average Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show average summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows minimum value summary for the column
        /// </summary>
        public static ICommand ShowMinSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Min Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show minimum summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows maximum value summary for the column
        /// </summary>
        public static ICommand ShowMaxSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Max Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show maximum summary in footer
        }, column => column != null);

        /// <summary>
        /// Toggles the visibility of the totals/summary row
        /// </summary>
        public static ICommand ToggleTotalsRowCommand => new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Totals Row - Not implemented");
            // TODO: Toggle summary row visibility
        }, grid => grid != null);

        #endregion

        #region Helper Methods

        /// <summary>
        /// Extracts the binding path from a DataGridColumn
        /// </summary>
        private static string GetBindingPath(DataGridColumn column)
        {
            switch (column)
            {
                case DataGridBoundColumn boundColumn:
                    return (boundColumn.Binding as System.Windows.Data.Binding)?.Path?.Path;
                case DataGridTemplateColumn templateColumn:
                    // For template columns, we'd need more complex logic to extract the binding
                    return null;
                default:
                    return null;
            }
        }

        #endregion
    }

    #region Context Menu Data Types

    /// <summary>
    /// Information about a cell for context menu operations
    /// </summary>
    public class CellInfo
    {
        public object Value { get; set; }
        public DataGridColumn Column { get; set; }
        public object RowData { get; set; }
        public SearchDataGrid Grid { get; set; }
    }

    /// <summary>
    /// Information about a column for context menu operations
    /// </summary>
    public class ColumnInfo
    {
        public DataGridColumn Column { get; set; }
        public ColumnSearchBox ColumnSearchBox { get; set; }
        public SearchDataGrid Grid { get; set; }
    }

    /// <summary>
    /// Information about a row for context menu operations
    /// </summary>
    public class RowInfo
    {
        public object Data { get; set; }
        public SearchDataGrid Grid { get; set; }
        public int RowIndex { get; set; }
    }

    #endregion
}