using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.Core.Display;
using WWSearchDataGrid.Modern.WPF.Display;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        #region Data & Export Commands

        private static ICommand _copySelectedCellValuesCommand;
        /// <summary>
        /// Copies display values of all selected cells to clipboard
        /// </summary>
        public static ICommand CopySelectedCellValuesCommand => _copySelectedCellValuesCommand ??= new RelayCommand<SearchDataGrid>(grid =>
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
                                    var path = GetBindingPath(sc.Column, grid);
                                    if (string.IsNullOrEmpty(path) || sc.Item is null) return null;

                                    var rawVal = ReflectionHelper.GetPropValue(sc.Item, path);
                                    var displayVal = FormatDisplayValue(rawVal, sc.Column, grid);
                                    return new
                                    {
                                        DisplayIndex = sc.Column.DisplayIndex,
                                        Value = displayVal
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


        private static ICommand _copySelectedCellValuesWithHeadersCommand;
        /// <summary>
        /// Copies display values of all selected cells with headers to clipboard
        /// </summary>
        public static ICommand CopySelectedCellValuesWithHeadersCommand => _copySelectedCellValuesWithHeadersCommand ??= new RelayCommand<SearchDataGrid>(grid =>
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

                            var path = GetBindingPath(col, grid);
                            if (string.IsNullOrEmpty(path) || rowItem is null)
                            {
                                rowValues.Add(string.Empty);
                                continue;
                            }

                            var rawVal = ReflectionHelper.GetPropValue(rowItem, path);
                            rowValues.Add(FormatDisplayValue(rawVal, col, grid));
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

        private static ICommand _copyCellValueCommand;
        /// <summary>
        /// Copies the display value of the single cell the context menu was opened on, ignoring
        /// any wider selection. Useful when SelectionUnit is FullRow / CellOrRowHeader and the
        /// default Copy would dump every cell in every selected row.
        /// </summary>
        public static ICommand CopyCellValueCommand => _copyCellValueCommand ??= new RelayCommand<ContextMenuContext>(ctx =>
        {
            try
            {
                if (ctx?.Grid == null || ctx.Column == null || ctx.RowData == null)
                    return;

                var path = GetBindingPath(ctx.Column, ctx.Grid);
                if (string.IsNullOrEmpty(path)) return;

                var rawVal = ReflectionHelper.GetPropValue(ctx.RowData, path);
                var displayVal = FormatDisplayValue(rawVal, ctx.Column, ctx.Grid);
                Clipboard.SetText(displayVal ?? string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error copying cell value: {ex.Message}");
            }
        }, ctx => ctx?.Grid != null && ctx.Column != null && ctx.RowData != null);

        /// <summary>
        /// Exports the grid data to CSV format
        /// </summary>
        private static ICommand _exportToCsvCommand;
        public static ICommand ExportToCsvCommand => _exportToCsvCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Export To CSV - Not implemented");
            // TODO: Export grid data to CSV file
        }, grid => grid?.Items?.Count > 0);

        /// <summary>
        /// Exports the grid data to Excel format
        /// </summary>
        private static ICommand _exportToExcelCommand;
        public static ICommand ExportToExcelCommand => _exportToExcelCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Export To Excel - Not implemented");
            // TODO: Export grid data to Excel file
        }, grid => grid?.Items?.Count > 0);

        #endregion

        #region Display Value Formatting

        /// <summary>
        /// Formats a raw value using the column's display configuration.
        /// Priority: DisplayValueProvider (mask/converter/format) > Binding.StringFormat > ToString.
        /// A column with <see cref="ColumnDataBase.CopyValueAsDisplayText"/> set to <c>false</c>
        /// short-circuits the display pipeline and copies the raw value's <c>ToString()</c>.
        /// </summary>
        private static string FormatDisplayValue(object rawVal, DataGridColumn column, SearchDataGrid grid)
        {
            if (rawVal is null)
                return "NULL";

            // 1) Check GridColumn descriptor for display providers (DisplayMask > DisplayValueConverter > DisplayStringFormat)
            var descriptor = grid?.FindGridColumnDescriptor(column);

            // Column opted out of display-text copy: emit the raw editing value verbatim.
            if (descriptor is { CopyValueAsDisplayText: false })
                return rawVal.ToString() ?? "NULL";

            var provider = DisplayValueProviderFactory.Create(descriptor);
            if (provider != null)
                return provider.FormatValue(rawVal) ?? rawVal.ToString() ?? "NULL";

            // 2) Check the WPF Binding.StringFormat
            if (column is DataGridBoundColumn boundCol && boundCol.Binding is Binding binding)
            {
                if (!string.IsNullOrEmpty(binding.StringFormat))
                {
                    try
                    {
                        return string.Format(binding.StringFormat, rawVal);
                    }
                    catch
                    {
                        // Format failed, fall through to ToString
                    }
                }
            }

            // 3) Fallback: raw ToString
            return rawVal.ToString() ?? "NULL";
        }

        #endregion
    }
}
