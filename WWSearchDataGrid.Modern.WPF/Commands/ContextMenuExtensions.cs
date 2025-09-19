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
            var contextMenuContext = DeterminecontextMenuContext(grid, target);
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
        private static ContextMenuContext DeterminecontextMenuContext(SearchDataGrid grid, FrameworkElement target)
        {
            var contextMenuContext = new ContextMenuContext { Grid = grid };

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

        /// <summary>
        /// Builds context menu for column headers
        /// </summary>
        private static ContextMenu BuildColumnHeaderContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuColumnHeader");

            // Column-specific Copy Operations (Primary focus for column headers)
            menu.Items.Add(new MenuItem
            {
                Header = "Copy",
                Command = ContextMenuCommands.CopySelectedCellValuesCommand,
                CommandParameter = contextMenuContext.Grid,
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Copy With Headers",
                Command = ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new Separator());
            // Sorting
            menu.Items.Add(new MenuItem
            {
                Name = "miSortAscending",
                Header = "Sort Ascending",
                Command = ContextMenuCommands.SortAscendingCommand,
                CommandParameter = contextMenuContext
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Sort Descending",
                Command = ContextMenuCommands.SortDescendingCommand,
                CommandParameter = contextMenuContext
            });
            menu.Items.Add(new Separator());

            // Filtering
            if (contextMenuContext.ColumnSearchBox != null)
            {
                menu.Items.Add(new MenuItem
                {
                    Header = "Show Filter Editor",
                    Command = ContextMenuCommands.ShowFilterEditorCommand,
                    CommandParameter = contextMenuContext.ColumnSearchBox
                });
                menu.Items.Add(new MenuItem
                {
                    Header = "Clear Column Filter",
                    Command = ContextMenuCommands.ClearColumnFilterCommand,
                    CommandParameter = contextMenuContext.ColumnSearchBox
                });
                menu.Items.Add(new Separator());
            }

            // Column Operations
            menu.Items.Add(new MenuItem
            {
                Header = "Best Fit Column",
                Command = ContextMenuCommands.BestFitColumnCommand,
                CommandParameter = contextMenuContext.Column
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Best Fit All Columns",
                Command = ContextMenuCommands.BestFitAllColumnsCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new Separator());

            // Alignment submenu
            var alignmentMenu = new MenuItem { Header = "Alignment" };
            alignmentMenu.Items.Add(new MenuItem
            {
                Header = "Left",
                Command = ContextMenuCommands.SetLeftAlignmentCommand,
                CommandParameter = contextMenuContext.Column
            });
            alignmentMenu.Items.Add(new MenuItem
            {
                Header = "Center",
                Command = ContextMenuCommands.SetCenterAlignmentCommand,
                CommandParameter = contextMenuContext.Column
            });
            alignmentMenu.Items.Add(new MenuItem
            {
                Header = "Right",
                Command = ContextMenuCommands.SetRightAlignmentCommand,
                CommandParameter = contextMenuContext.Column
            });
            menu.Items.Add(alignmentMenu);
            menu.Items.Add(new Separator());

            // Visibility and Layout
            menu.Items.Add(new MenuItem
            {
                Header = "Hide Column",
                Command = ContextMenuCommands.HideSelectedColumnCommand,
                CommandParameter = contextMenuContext.Column
            });

            // Dynamic Freeze/Unfreeze menu item
            var isFrozen = IsColumnFrozen(contextMenuContext.Grid, contextMenuContext.Column);
            menu.Items.Add(new MenuItem
            {
                Header = isFrozen ? "Unfreeze Column" : "Freeze Column",
                Command = isFrozen ? ContextMenuCommands.UnfreezeColumnCommand : ContextMenuCommands.FreezeColumnCommand,
                CommandParameter = contextMenuContext.Column
            });

            // Dynamic Pin/Unpin menu item
            var isPinned = IsColumnPinned(contextMenuContext.Grid, contextMenuContext.Column);
            menu.Items.Add(new MenuItem
            {
                Header = isPinned ? "Unpin Column" : "Pin Column",
                Command = isPinned ? ContextMenuCommands.UnpinColumnCommand : ContextMenuCommands.PinColumnCommand,
                CommandParameter = contextMenuContext.Column
            });

            return menu;
        }

        /// <summary>
        /// Builds context menu for cells
        /// </summary>
        private static ContextMenu BuildCellContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuCell");

            // Cell-specific Copy Operations (Primary focus for cells)
            menu.Items.Add(new MenuItem
            {
                Header = "Copy",
                Command = ContextMenuCommands.CopySelectedCellValuesCommand,
                CommandParameter = contextMenuContext.Grid,
                InputGestureText = "Ctrl + C",
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Copy With Headers",
                Command = ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand,
                CommandParameter = contextMenuContext.Grid,
                InputGestureText = "Shift + Ctrl + C"
            });

            return menu;
        }

        /// <summary>
        /// Builds context menu for rows (including row headers)
        /// </summary>
        private static ContextMenu BuildRowContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();
            AutomationProperties.SetAutomationId(menu, "cmuRowHeader");

            // Row-specific Copy Operations (Primary focus for row headers)
            menu.Items.Add(new MenuItem
            {
                Header = "Copy",
                Command = ContextMenuCommands.CopySelectedCellValuesCommand,
                CommandParameter = contextMenuContext.Grid,
                FontWeight = FontWeights.Bold
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Copy With Headers",
                Command = ContextMenuCommands.CopySelectedCellValuesWithHeadersCommand,
                CommandParameter = contextMenuContext.Grid,
                InputGestureText = "Shift + Ctrl + C"
            });

            return menu;
        }

        /// <summary>
        /// Builds context menu for general grid body
        /// </summary>
        private static ContextMenu BuildGridBodyContextMenu(ContextMenuContext contextMenuContext)
        {
            var menu = new ContextMenu();

            // Filter operations
            menu.Items.Add(new MenuItem
            {
                Header = "Clear All Filters",
                Command = ContextMenuCommands.ClearAllFiltersCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Toggle Filter Panel",
                Command = ContextMenuCommands.ToggleFilterPanelCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Save Filter Preset",
                Command = ContextMenuCommands.SaveFilterPresetCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Load Filter Preset",
                Command = ContextMenuCommands.LoadFilterPresetCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new Separator());

            // Column Profiles submenu
            var profilesMenu = new MenuItem { Header = "Column Profiles" };
            profilesMenu.Items.Add(new MenuItem
            {
                Header = "Save Current Profile",
                Command = ContextMenuCommands.SaveCurrentProfileCommand,
                CommandParameter = contextMenuContext.Grid
            });
            profilesMenu.Items.Add(new MenuItem
            {
                Header = "Load Profile...",
                Command = ContextMenuCommands.LoadProfileCommand,
                CommandParameter = contextMenuContext.Grid
            });
            profilesMenu.Items.Add(new MenuItem
            {
                Header = "Manage Profiles...",
                Command = ContextMenuCommands.ManageProfilesCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(profilesMenu);
            menu.Items.Add(new Separator());

            // Export operations
            menu.Items.Add(new MenuItem
            {
                Header = "Export to CSV",
                Command = ContextMenuCommands.ExportToCsvCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Export to Excel",
                Command = ContextMenuCommands.ExportToExcelCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new Separator());

            // Layout operations
            menu.Items.Add(new MenuItem
            {
                Header = "Show Column Editor",
                Command = ContextMenuCommands.ShowColumnEditorCommand,
                CommandParameter = contextMenuContext.Grid
            });
            menu.Items.Add(new MenuItem
            {
                Header = "Reset Layout",
                Command = ContextMenuCommands.ResetLayoutCommand,
                CommandParameter = contextMenuContext.Grid
            });

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