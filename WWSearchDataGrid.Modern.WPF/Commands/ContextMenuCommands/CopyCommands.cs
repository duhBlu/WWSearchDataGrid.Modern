using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    internal partial class ContextMenuCommands
    {
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
    }
}
