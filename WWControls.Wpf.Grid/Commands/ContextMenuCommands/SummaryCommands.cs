using System;
using System.Diagnostics;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Commands
{
    public partial class ContextMenuCommands
    {
        #region Total Summary Cell Commands

        // The runtime summary picker — the total-summary cell's right-click menu. Each command
        // takes the originating TotalSummaryCell (the menu's PlacementTarget) and toggles one
        // aggregate on the cell's column descriptor. Gating is two-layer: the column's
        // AllowedTotalSummaries flags (consumer policy) AND SummaryCalculator.IsTypeSupported
        // (the FieldType can actually compute it). Mutating TotalSummaries raises the
        // collection's Freezable.Changed, which routes to the grid's summary engine — no
        // explicit recompute here.

        private static ICommand CreateToggleTotalSummaryCommand(SummaryItemType type, AllowedSummaries flag) =>
            new RelayCommand<TotalSummaryCell>(
                cell =>
                {
                    try { ToggleTotalSummary(cell?.Descriptor, type); }
                    catch (Exception ex) { Debug.WriteLine($"Error toggling {type} total summary: {ex.Message}"); }
                },
                cell => CanPickTotalSummary(cell?.Descriptor, type, flag));

        private static bool CanPickTotalSummary(GridColumn descriptor, SummaryItemType type, AllowedSummaries flag)
        {
            if (descriptor?.TotalSummaries == null) return false;
            if ((descriptor.AllowedTotalSummaries & flag) == 0) return false;
            return SummaryCalculator.IsTypeSupported(type, descriptor.FieldType);
        }

        private static void ToggleTotalSummary(GridColumn descriptor, SummaryItemType type)
        {
            var items = descriptor?.TotalSummaries;
            if (items == null) return;

            // The quick picker manages only the cell column's OWN entries (no FieldName).
            // FieldName-targeted entries — another column's aggregate rendered under this cell —
            // belong to the Customize editor; the picker must neither read them as checked
            // state nor remove them when toggling this column's function off.
            bool removed = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] is { } item
                    && item.SummaryType == type
                    && string.IsNullOrEmpty(item.FieldName))
                {
                    items.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed)
                items.Add(new SummaryItem { SummaryType = type });
        }

        private static ICommand _toggleCountTotalSummaryCommand;
        /// <summary>Adds/removes a Count total summary on the right-clicked summary cell's column.</summary>
        public static ICommand ToggleCountTotalSummaryCommand => _toggleCountTotalSummaryCommand ??=
            CreateToggleTotalSummaryCommand(SummaryItemType.Count, AllowedSummaries.Count);

        private static ICommand _toggleSumTotalSummaryCommand;
        /// <summary>Adds/removes a Sum total summary on the right-clicked summary cell's column.</summary>
        public static ICommand ToggleSumTotalSummaryCommand => _toggleSumTotalSummaryCommand ??=
            CreateToggleTotalSummaryCommand(SummaryItemType.Sum, AllowedSummaries.Sum);

        private static ICommand _toggleMinTotalSummaryCommand;
        /// <summary>Adds/removes a Min total summary on the right-clicked summary cell's column.</summary>
        public static ICommand ToggleMinTotalSummaryCommand => _toggleMinTotalSummaryCommand ??=
            CreateToggleTotalSummaryCommand(SummaryItemType.Min, AllowedSummaries.Min);

        private static ICommand _toggleMaxTotalSummaryCommand;
        /// <summary>Adds/removes a Max total summary on the right-clicked summary cell's column.</summary>
        public static ICommand ToggleMaxTotalSummaryCommand => _toggleMaxTotalSummaryCommand ??=
            CreateToggleTotalSummaryCommand(SummaryItemType.Max, AllowedSummaries.Max);

        private static ICommand _toggleAverageTotalSummaryCommand;
        /// <summary>Adds/removes an Average total summary on the right-clicked summary cell's column.</summary>
        public static ICommand ToggleAverageTotalSummaryCommand => _toggleAverageTotalSummaryCommand ??=
            CreateToggleTotalSummaryCommand(SummaryItemType.Average, AllowedSummaries.Average);

        private static ICommand _clearTotalSummariesCommand;
        /// <summary>Removes every total summary from the right-clicked summary cell's column.</summary>
        public static ICommand ClearTotalSummariesCommand => _clearTotalSummariesCommand ??=
            new RelayCommand<TotalSummaryCell>(
                cell =>
                {
                    try { cell?.Descriptor?.TotalSummaries?.Clear(); }
                    catch (Exception ex) { Debug.WriteLine($"Error clearing total summaries: {ex.Message}"); }
                },
                cell => cell?.Descriptor?.TotalSummaries is { Count: > 0 });

        private static ICommand _customizeTotalSummariesCommand;
        /// <summary>
        /// Opens the column-scoped totals editor ("Totals for 'X'") from a right-clicked
        /// summary cell — that column's summary set, with cross-column aggregation targets.
        /// </summary>
        public static ICommand CustomizeTotalSummariesCommand => _customizeTotalSummariesCommand ??=
            new RelayCommand<TotalSummaryCell>(
                cell =>
                {
                    try { GroupSummaryEditor.ShowColumnTotalsDialog(cell?.OwnerGrid, cell?.Descriptor); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CustomizeTotalSummariesCommand: {ex.Message}"); }
                },
                cell => cell?.OwnerGrid != null && cell?.Descriptor != null);

        #endregion

        #region Grid-Level Totals Commands (column header + fixed panel menus)

        // Discoverability entries for when the summary surfaces themselves are hidden — a
        // collapsed totals row / hidden fixed panel has no right-click surface, so the column
        // header menu carries show/hide toggles + the editor launcher.

        private static ICommand _openTotalSummaryEditorCommand;
        /// <summary>
        /// Opens the column-scoped totals editor ("Totals for 'X'") from a column header's
        /// context (the shared menu's <see cref="ContextMenuContext"/>).
        /// </summary>
        public static ICommand OpenTotalSummaryEditorCommand => _openTotalSummaryEditorCommand ??=
            new RelayCommand<ContextMenuContext>(
                context =>
                {
                    try
                    {
                        var grid = context?.Grid;
                        var descriptor = grid?.FindGridColumnDescriptor(context.Column);
                        GroupSummaryEditor.ShowColumnTotalsDialog(grid, descriptor);
                    }
                    catch (Exception ex) { Debug.WriteLine($"Error in OpenTotalSummaryEditorCommand: {ex.Message}"); }
                },
                context => context?.Grid != null
                    && context.Grid.AllowTotalSummary
                    && context.Column != null
                    && context.Grid.FindGridColumnDescriptor(context.Column) != null);

        private static ICommand _toggleFixedTotalRowCountCommand;
        /// <summary>
        /// Toggles the fixed panel's grid row-count entry — a no-FieldName Count item in
        /// <see cref="SearchDataGrid.FixedTotalSummaries"/> (the same entry the editor's
        /// "Show row count" checkbox manages).
        /// </summary>
        public static ICommand ToggleFixedTotalRowCountCommand => _toggleFixedTotalRowCountCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try
                    {
                        var items = grid?.FixedTotalSummaries;
                        if (items == null) return;

                        bool removed = false;
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            if (items[i] is { SummaryType: SummaryItemType.Count } item
                                && string.IsNullOrEmpty(item.FieldName))
                            {
                                items.RemoveAt(i);
                                removed = true;
                            }
                        }

                        if (!removed)
                            items.Add(new SummaryItem { SummaryType = SummaryItemType.Count });
                    }
                    catch (Exception ex) { Debug.WriteLine($"Error in ToggleFixedTotalRowCountCommand: {ex.Message}"); }
                },
                grid => grid?.FixedTotalSummaries != null && grid.AllowFixedTotalSummary);

        private static ICommand _openFixedTotalSummaryEditorCommand;
        /// <summary>Opens the fixed-panel summary editor (the panel's own definition set).</summary>
        public static ICommand OpenFixedTotalSummaryEditorCommand => _openFixedTotalSummaryEditorCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try { GroupSummaryEditor.ShowFixedTotalsDialog(grid); }
                    catch (Exception ex) { Debug.WriteLine($"Error in OpenFixedTotalSummaryEditorCommand: {ex.Message}"); }
                },
                grid => grid != null && grid.AllowFixedTotalSummary);

        private static ICommand _toggleTotalSummaryRowCommand;
        /// <summary>
        /// Toggles the column-aligned totals row. Showing it also re-arms
        /// <see cref="SearchDataGrid.TotalSummaryPosition"/> when it was <c>None</c> (otherwise
        /// the row would stay suppressed and the toggle would look like a no-op).
        /// </summary>
        public static ICommand ToggleTotalSummaryRowCommand => _toggleTotalSummaryRowCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try
                    {
                        if (grid == null) return;
                        bool effectivelyOn = grid.ShowTotalSummary
                            && grid.TotalSummaryPosition != TotalSummaryPosition.None;
                        if (effectivelyOn)
                            grid.ShowTotalSummary = false;
                        else
                            grid.EnsureTotalSummaryRowVisible();
                    }
                    catch (Exception ex) { Debug.WriteLine($"Error in ToggleTotalSummaryRowCommand: {ex.Message}"); }
                },
                grid => grid != null && grid.AllowTotalSummary);

        private static ICommand _toggleFixedTotalSummaryCommand;
        /// <summary>Toggles the fixed total summary panel beneath the items.</summary>
        public static ICommand ToggleFixedTotalSummaryCommand => _toggleFixedTotalSummaryCommand ??=
            new RelayCommand<SearchDataGrid>(
                grid =>
                {
                    try { if (grid != null) grid.ShowFixedTotalSummary = !grid.ShowFixedTotalSummary; }
                    catch (Exception ex) { Debug.WriteLine($"Error in ToggleFixedTotalSummaryCommand: {ex.Message}"); }
                },
                grid => grid != null && grid.AllowFixedTotalSummary);

        #endregion

        #region Group Footer Summary Cell Commands

        // The footer counterpart of the total-summary cell picker — the group footer cell's
        // right-click menu. Each command takes the originating GroupFooterCell (the menu's
        // PlacementTarget) and toggles one aggregate on the cell's column descriptor's
        // GroupFooterSummaries. Gating mirrors the totals picker: the column's
        // AllowedGroupFooterSummaries flags AND SummaryCalculator.IsTypeSupported. Mutating the
        // collection raises Freezable.Changed, which routes to the grid's footer engine (a
        // reflatten that recomputes every group's footer) — no explicit recompute here.

        private static ICommand CreateToggleFooterSummaryCommand(SummaryItemType type, AllowedSummaries flag) =>
            new RelayCommand<GroupFooterCell>(
                cell =>
                {
                    try { ToggleFooterSummary(cell?.Descriptor, type); }
                    catch (Exception ex) { Debug.WriteLine($"Error toggling {type} footer summary: {ex.Message}"); }
                },
                cell => CanPickFooterSummary(cell?.Descriptor, type, flag));

        private static bool CanPickFooterSummary(GridColumn descriptor, SummaryItemType type, AllowedSummaries flag)
        {
            if (descriptor?.GroupFooterSummaries == null) return false;
            if ((descriptor.AllowedGroupFooterSummaries & flag) == 0) return false;
            return SummaryCalculator.IsTypeSupported(type, descriptor.FieldType);
        }

        private static void ToggleFooterSummary(GridColumn descriptor, SummaryItemType type)
        {
            var items = descriptor?.GroupFooterSummaries;
            if (items == null) return;

            // Like the totals picker, the quick picker manages only the cell column's OWN entries
            // (no FieldName). FieldName-targeted footer entries belong to the Customize editor.
            bool removed = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] is { } item
                    && item.SummaryType == type
                    && string.IsNullOrEmpty(item.FieldName))
                {
                    items.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed)
                items.Add(new SummaryItem { SummaryType = type });
        }

        private static ICommand _toggleCountFooterSummaryCommand;
        /// <summary>Adds/removes a Count footer summary on the right-clicked footer cell's column.</summary>
        public static ICommand ToggleCountFooterSummaryCommand => _toggleCountFooterSummaryCommand ??=
            CreateToggleFooterSummaryCommand(SummaryItemType.Count, AllowedSummaries.Count);

        private static ICommand _toggleSumFooterSummaryCommand;
        /// <summary>Adds/removes a Sum footer summary on the right-clicked footer cell's column.</summary>
        public static ICommand ToggleSumFooterSummaryCommand => _toggleSumFooterSummaryCommand ??=
            CreateToggleFooterSummaryCommand(SummaryItemType.Sum, AllowedSummaries.Sum);

        private static ICommand _toggleMinFooterSummaryCommand;
        /// <summary>Adds/removes a Min footer summary on the right-clicked footer cell's column.</summary>
        public static ICommand ToggleMinFooterSummaryCommand => _toggleMinFooterSummaryCommand ??=
            CreateToggleFooterSummaryCommand(SummaryItemType.Min, AllowedSummaries.Min);

        private static ICommand _toggleMaxFooterSummaryCommand;
        /// <summary>Adds/removes a Max footer summary on the right-clicked footer cell's column.</summary>
        public static ICommand ToggleMaxFooterSummaryCommand => _toggleMaxFooterSummaryCommand ??=
            CreateToggleFooterSummaryCommand(SummaryItemType.Max, AllowedSummaries.Max);

        private static ICommand _toggleAverageFooterSummaryCommand;
        /// <summary>Adds/removes an Average footer summary on the right-clicked footer cell's column.</summary>
        public static ICommand ToggleAverageFooterSummaryCommand => _toggleAverageFooterSummaryCommand ??=
            CreateToggleFooterSummaryCommand(SummaryItemType.Average, AllowedSummaries.Average);

        private static ICommand _clearFooterSummariesCommand;
        /// <summary>Removes every footer summary from the right-clicked footer cell's column.</summary>
        public static ICommand ClearFooterSummariesCommand => _clearFooterSummariesCommand ??=
            new RelayCommand<GroupFooterCell>(
                cell =>
                {
                    try { cell?.Descriptor?.GroupFooterSummaries?.Clear(); }
                    catch (Exception ex) { Debug.WriteLine($"Error clearing footer summaries: {ex.Message}"); }
                },
                cell => cell?.Descriptor?.GroupFooterSummaries is { Count: > 0 });

        private static ICommand _customizeFooterSummariesCommand;
        /// <summary>
        /// Opens the column-scoped group-footer editor ("Footer for 'X'") from a right-clicked
        /// footer cell — that column's footer summary set, with cross-column aggregation targets.
        /// </summary>
        public static ICommand CustomizeFooterSummariesCommand => _customizeFooterSummariesCommand ??=
            new RelayCommand<GroupFooterCell>(
                cell =>
                {
                    try { GroupSummaryEditor.ShowGroupFooterDialog(cell?.OwnerGrid, cell?.Descriptor); }
                    catch (Exception ex) { Debug.WriteLine($"Error in CustomizeFooterSummariesCommand: {ex.Message}"); }
                },
                cell => cell?.OwnerGrid != null && cell?.Descriptor != null);

        #endregion
    }
}
