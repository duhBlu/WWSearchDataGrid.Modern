using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.WPF.Commands;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Extension methods for adding context menu functionality to SearchDataGrid
    /// </summary>
    internal static class ContextMenuExtensions
    {
        /// <summary>
        /// Initializes context menu functionality for the SearchDataGrid
        /// </summary>
        public static void InitializeContextMenu(this SearchDataGrid grid)
        {
            if (grid == null) return;

            // Subscribe to ContextMenuOpening event
            grid.ContextMenuOpening += OnContextMenuOpening;
        }

        /// <summary>
        /// Handles the context menu opening event
        /// </summary>
        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!(sender is SearchDataGrid grid))
                return;

            try
            {
                // Determine the context menu based on where the user clicked
                var contextMenu = BuildContextMenuForTarget(grid, e.OriginalSource as FrameworkElement);

                if (contextMenu != null && contextMenu.Items.Count > 0)
                {
                    grid.ContextMenu = contextMenu;
                }
                else
                {
                    // If no appropriate context menu, cancel the event
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building context menu: {ex.Message}");
                e.Handled = true;
            }
        }

        /// <summary>
        /// Builds the appropriate context menu based on the target element
        /// </summary>
        private static ContextMenu BuildContextMenuForTarget(SearchDataGrid grid, FrameworkElement target)
        {
            if (target == null || grid == null)
                return null;

            Core.CommandManager.InvalidateRequerySuggested();

            // Find the context type by walking up the visual tree
            var contextMenuContext = DetermineContextMenuContext(grid, target);
            ContextMenu cmu = contextMenuContext.ContextType switch
            {
                ContextMenuType.ColumnHeader => BuildColumnHeaderContextMenu(contextMenuContext),
                ContextMenuType.Cell => BuildCellContextMenu(contextMenuContext),
                ContextMenuType.Row => BuildRowContextMenu(contextMenuContext),
                ContextMenuType.GridBody => BuildGridBodyContextMenu(contextMenuContext),
                _ => null
            };
            return cmu;
        }

        /// <summary>
        /// Determines the context information for the clicked element
        /// </summary>
        private static ContextMenuContext DetermineContextMenuContext(SearchDataGrid grid, FrameworkElement target)
        {
            var contextMenuContext = new ContextMenuContext { Grid = grid, ContextType = ContextMenuType.GridBody }; 

            // Walk up the visual tree to determine context
            var element = target;
            while (element != null)
            {
                switch (element)
                {
                    case DataGridColumnHeader header:
                        contextMenuContext.ContextType = ContextMenuType.ColumnHeader;
                        contextMenuContext.Column = header.Column;
                        contextMenuContext.ColumnSearchBox = FindColumnSearchBox(grid, header.Column);
                        return contextMenuContext;

                    case DataGridCell cell:
                        contextMenuContext.ContextType = ContextMenuType.Cell;
                        contextMenuContext.Column = cell.Column;
                        contextMenuContext.ColumnSearchBox = FindColumnSearchBox(grid, cell.Column);
                        contextMenuContext.RowData = cell.DataContext;
                        contextMenuContext.CellValue = GetCellValue(cell);
                        return contextMenuContext;

                    case DataGridRow row:
                        contextMenuContext.ContextType = ContextMenuType.Row;
                        contextMenuContext.RowData = row.DataContext;
                        contextMenuContext.RowIndex = row.GetIndex();
                        return contextMenuContext;

                    case DataGridRowHeader rowHeader:
                        contextMenuContext.ContextType = ContextMenuType.Row;
                        if (rowHeader.DataContext != null)
                        {
                            contextMenuContext.RowData = rowHeader.DataContext;
                        }
                        return contextMenuContext;
                }

                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            return contextMenuContext;
        }

        /// <summary>
        /// Finds the ColumnSearchBox associated with a DataGridColumn
        /// </summary>
        private static ColumnSearchBox FindColumnSearchBox(SearchDataGrid grid, DataGridColumn column)
        {
            if (column == null) return null;

            return grid.DataColumns?.FirstOrDefault(c => c.CurrentColumn == column);
        }

        /// <summary>
        /// Determines if a column is currently frozen
        /// </summary>
        private static bool IsColumnFrozen(SearchDataGrid grid, DataGridColumn column)
        {
            if (grid == null || column == null) return false;

            // TODO: Implement actual frozen column detection
            // For now, return false as placeholder
            return false;
        }

        /// <summary>
        /// Determines if a column is currently pinned
        /// </summary>
        private static bool IsColumnPinned(SearchDataGrid grid, DataGridColumn column)
        {
            if (grid == null || column == null) return false;

            // TODO: Implement actual pinned column detection
            // For now, return false as placeholder
            return false;
        }

        /// <summary>
        /// Gets the value from a DataGridCell
        /// </summary>
        private static object GetCellValue(DataGridCell cell)
        {
            if (cell?.DataContext == null || cell.Column == null)
                return null;

            try
            {
                var bindingPath = GetBindingPath(cell.Column);
                if (!string.IsNullOrEmpty(bindingPath))
                {
                    return ReflectionHelper.GetPropValue(cell.DataContext, bindingPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting cell value: {ex.Message}");
            }

            return null;
        }

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

        #region Context Menu Builders

        private static MenuItem BuildMenuItem(string name, string header, ICommand command, object parameter = null, string inputGestureText = "")
        {
            var item = new MenuItem
            {
                Name = name,
                Header = header,
                Command = command,
                CommandParameter = parameter,
                InputGestureText = inputGestureText
            };

            // Set AutomationId to match Name
            AutomationProperties.SetAutomationId(item, name);

            return item;
        }

        private static ContextMenu BuildColumnHeaderContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuColumnHeader");

            // Column-specific Copy Operations (Primary focus for column headers)
            menu.Items.Add(BuildMenuItem(
                "miCopy",
                "Copy",
                ContextMenuCommands.CopySelectedCellValuesCommand,
                contextMenuContext.Grid,
                "Ctrl+C"));

            menu.Items.Add(BuildMenuItem(
                "miCopyWithHeaders",
                "Copy With Headers",
                ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand,
                contextMenuContext.Grid, "Ctrl+Shift+C"));

            menu.Items.Add(new Separator());

            // Sorting
            menu.Items.Add(BuildMenuItem(
                "miSortAscending",
                "Sort Ascending",
                ContextMenuCommands.SortAscendingCommand,
                contextMenuContext));

            menu.Items.Add(BuildMenuItem(
                "miSortDescending",
                "Sort Descending",
                ContextMenuCommands.SortDescendingCommand,
                contextMenuContext));

            menu.Items.Add(new Separator());

            // Filtering
            if (contextMenuContext.ColumnSearchBox != null)
            {
                menu.Items.Add(BuildMenuItem(
                    "miShowFilterEditor",
                    "Show Filter Editor (Not Implemented)",
                    ContextMenuCommands.ShowFilterEditorCommand,
                    contextMenuContext.ColumnSearchBox));

                menu.Items.Add(BuildMenuItem(
                    "miClearColumnFilter",
                    "Clear Column Filter (Not Implemented)",
                    ContextMenuCommands.ClearColumnFilterCommand,
                    contextMenuContext.ColumnSearchBox));

                menu.Items.Add(new Separator());
            }

            // Column Operations
            menu.Items.Add(BuildMenuItem(
                "miBestFitColumn",
                "Best Fit Column (Not Implemented)",
                ContextMenuCommands.BestFitColumnCommand,
                contextMenuContext));

            menu.Items.Add(BuildMenuItem(
                "miBestFitAllColumns",
                "Best Fit All Columns (Not Implemented)",
                ContextMenuCommands.BestFitAllColumnsCommand,
                contextMenuContext));

            menu.Items.Add(new Separator());

            // Alignment submenu
            var alignmentMenu = BuildMenuItem("miAlignment", "Alignment (Not Implemented)", null, null);
            alignmentMenu.Items.Add(BuildMenuItem(
                "miAlignLeft",
                "Left",
                ContextMenuCommands.SetLeftAlignmentCommand,
                contextMenuContext.Column));
            alignmentMenu.Items.Add(BuildMenuItem(
                "miAlignCenter",
                "Center",
                ContextMenuCommands.SetCenterAlignmentCommand,
                contextMenuContext.Column));
            alignmentMenu.Items.Add(BuildMenuItem(
                "miAlignRight",
                "Right",
                ContextMenuCommands.SetRightAlignmentCommand,
                contextMenuContext.Column));
            menu.Items.Add(alignmentMenu);

            menu.Items.Add(new Separator());

            // Visibility and Layout
            menu.Items.Add(BuildMenuItem(
                "miHideColumn",
                "Hide Column (Not Implemented)",
                ContextMenuCommands.HideSelectedColumnCommand,
                contextMenuContext.Column));

            // Dynamic Freeze/Unfreeze menu item
            var isFrozen = IsColumnFrozen(contextMenuContext.Grid, contextMenuContext.Column);
            var freezeName = isFrozen ? "miUnfreezeColumn" : "miFreezeColumn";
            var freezeHeader = isFrozen ? "Unfreeze Column (Not Implemented)" : "Freeze Column (Not Implemented)";
            menu.Items.Add(BuildMenuItem(
                freezeName,
                freezeHeader,
                isFrozen ? ContextMenuCommands.UnfreezeColumnCommand : ContextMenuCommands.FreezeColumnCommand,
                contextMenuContext.Column));

            // Dynamic Pin/Unpin menu item
            var isPinned = IsColumnPinned(contextMenuContext.Grid, contextMenuContext.Column);
            var pinName = isPinned ? "miUnpinColumn" : "miPinColumn";
            var pinHeader = isPinned ? "Unpin Column (Not Implemented)" : "Pin Column (Not Implemented)";
            menu.Items.Add(BuildMenuItem(
                pinName,
                pinHeader,
                isPinned ? ContextMenuCommands.UnpinColumnCommand : ContextMenuCommands.PinColumnCommand,
                contextMenuContext.Column));

            return menu;
        }


        private static ContextMenu BuildCellContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuCell");

            menu.Items.Add(BuildMenuItem(
                "miCopy",
                "Copy",
                ContextMenuCommands.CopySelectedCellValuesCommand,
                contextMenuContext.Grid,
                "Ctrl+C"));
            menu.Items.Add(BuildMenuItem(
                "miCopyWithHeaders",
                "Copy With Headers",
                ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand,
                contextMenuContext.Grid,
                "Shift+Ctrl+C"));

            return menu;
        }

        private static ContextMenu BuildRowContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuRowHeader");

            menu.Items.Add(BuildMenuItem(
                "miCopy",
                "Copy",
                ContextMenuCommands.CopySelectedCellValuesCommand,
                contextMenuContext.Grid,
                "Ctrl+C"));
            menu.Items.Add(BuildMenuItem(
                "miCopyWithHeaders",
                "Copy With Headers",
                ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand,
                contextMenuContext.Grid, 
                "Ctrl+C"));

            return menu;
        }

        private static ContextMenu BuildGridBodyContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuGridBody");

            // Filter operations
            menu.Items.Add(BuildMenuItem(
                "miClearAllFilters",
                "Clear All Filters (Not Implemented)",
                ContextMenuCommands.ClearAllFiltersCommand,
                contextMenuContext.Grid));
            menu.Items.Add(BuildMenuItem(
                "miToggleFilterPanel",
                "Toggle Filter Panel (Not Implemented)",
                ContextMenuCommands.ToggleFilterPanelCommand,
                contextMenuContext.Grid));
            menu.Items.Add(BuildMenuItem(
                "miSaveFilterPreset", 
                "Save Filter Preset (Not Implemented)",
                ContextMenuCommands.SaveFilterPresetCommand,
                contextMenuContext.Grid));
            menu.Items.Add(BuildMenuItem(
                "miLoadFilterPreset",
                "Load Filter Preset (Not Implemented)",
                ContextMenuCommands.LoadFilterPresetCommand,
                contextMenuContext.Grid));
            menu.Items.Add(new Separator());

            // Column Profiles submenu
            var profilesMenu = BuildMenuItem("miColumnProfiles", "Column Profiles", null, null);
            profilesMenu.Items.Add(BuildMenuItem(
                "miSaveCurrentProfile",
                "Save Current Profile (Not Implemented)",
                ContextMenuCommands.SaveCurrentProfileCommand,
                contextMenuContext.Grid));
            profilesMenu.Items.Add(BuildMenuItem(
                "miLoadProfile",
                "Load Profile... (Not Implemented)",
                ContextMenuCommands.LoadProfileCommand,
                contextMenuContext.Grid));
            profilesMenu.Items.Add(BuildMenuItem(
                "miManageProfiles",
                "Manage Profiles... (Not Implemented)",
                ContextMenuCommands.ManageProfilesCommand,
                contextMenuContext.Grid));
            menu.Items.Add(profilesMenu);

            menu.Items.Add(new Separator());

            // Export operations
            menu.Items.Add(BuildMenuItem(
                "miExportToCsv",
                "Export to CSV (Not Implemented)",
                ContextMenuCommands.ExportToCsvCommand,
                contextMenuContext.Grid));
            menu.Items.Add(BuildMenuItem(
                "miExportToExcel",
                "Export to Excel (Not Implemented)",
                ContextMenuCommands.ExportToExcelCommand,
                contextMenuContext.Grid));

            menu.Items.Add(new Separator());

            // Layout operations
            menu.Items.Add(BuildMenuItem(
                "miShowColumnEditor",
                "Show Column Editor (Not Implemented)",
                ContextMenuCommands.ShowColumnEditorCommand,
                contextMenuContext.Grid));
            menu.Items.Add(BuildMenuItem(
                "miResetLayout",
                "Reset Layout (Not Implemented)",
                ContextMenuCommands.ResetLayoutCommand,
                contextMenuContext.Grid));

            return menu;
        }

        #endregion
    }

    #region Context Menu Types

    /// <summary>
    /// Represents the type of context menu to show
    /// </summary>
    public enum ContextMenuType
    {
        ColumnHeader,
        Cell,
        Row,
        GridBody
    }
    #endregion
}