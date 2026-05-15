using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;
using WWSearchDataGrid.Modern.WPF.Commands;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Provides attached properties and infrastructure for XAML-based context menu support
    /// </summary>
    public static class ContextMenuExtensions
    {
        #region Attached Properties

        /// <summary>
        /// Attached property that stores the ContextMenuContext for any element
        /// This enables XAML bindings to access context information
        /// </summary>
        public static readonly DependencyProperty ContextProperty =
            DependencyProperty.RegisterAttached(
                "Context",
                typeof(ContextMenuContext),
                typeof(ContextMenuExtensions),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets the ContextMenuContext attached to an element
        /// </summary>
        public static ContextMenuContext GetContext(DependencyObject obj)
        {
            return (ContextMenuContext)obj.GetValue(ContextProperty);
        }

        /// <summary>
        /// Sets the ContextMenuContext attached to an element
        /// </summary>
        public static void SetContext(DependencyObject obj, ContextMenuContext value)
        {
            obj.SetValue(ContextProperty, value);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes context menu functionality for the SearchDataGrid
        /// </summary>
        public static void InitializeContextMenu(this SearchDataGrid grid)
        {
            if (grid == null) return;

            grid.ContextMenuOpening += OnContextMenuOpening;
        }

        /// <summary>
        /// Handles the context menu opening event - determines context and sets attached property
        /// </summary>
        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not SearchDataGrid grid) return;

            Core.CommandManager.InvalidateRequerySuggested();

            // Determine context from the source element
            var sourceElement = e.OriginalSource as FrameworkElement;
            var context = DetermineContextMenuContext(grid, sourceElement);

            if (context == null)
            {
                e.Handled = true;
                return;
            }

            // Find the target element that will show the context menu
            var targetElement = FindContextMenuTarget(sourceElement, context.ContextType);

            if (targetElement != null)
            {
                // Set the context on the target element
                SetContext(targetElement, context);

                // Also set it on the ContextMenu itself for easier binding
                var contextMenu = GetContextMenuForElement(targetElement);
                if (contextMenu != null)
                {
                    contextMenu.DataContext = context;
                }
                else
                {
                    // No context menu defined in XAML
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the ContextMenu from an element
        /// </summary>
        private static ContextMenu GetContextMenuForElement(FrameworkElement element)
        {
            if (element == null) return null;

            return element switch
            {
                DataGridColumnHeader header => header.ContextMenu,
                DataGridCell cell => cell.ContextMenu,
                DataGridRowHeader rowHeader => rowHeader.ContextMenu,
                DataGridRow row => row.ContextMenu,
                SearchDataGrid grid => grid.ContextMenu,
                _ => null
            };
        }

        /// <summary>
        /// Finds the appropriate target element for setting the context based on context type
        /// </summary>
        private static FrameworkElement FindContextMenuTarget(FrameworkElement source, ContextMenuType contextType)
        {
            if (source == null) return null;

            var element = source;
            while (element != null)
            {
                switch (contextType)
                {
                    case ContextMenuType.ColumnHeader when element is DataGridColumnHeader:
                    case ContextMenuType.Cell when element is DataGridCell:
                    case ContextMenuType.Row when element is DataGridRowHeader or DataGridRow:
                    case ContextMenuType.GridBody when element is SearchDataGrid:
                        return element;
                }

                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            return null;
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
        /// Finds the <see cref="IColumnFilterHost"/> associated with a <see cref="DataGridColumn"/>.
        /// Name preserved (<c>FindColumnSearchBox</c>) so call sites read consistently against
        /// the existing context-menu surface; return type generalized in Phase 5.
        /// </summary>
        private static IColumnFilterHost FindColumnSearchBox(SearchDataGrid grid, DataGridColumn column)
        {
            if (column == null) return null;

            return grid.DataColumns?.FirstOrDefault(c => c.CurrentColumn == column);
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
                var grid = FindAncestor<SearchDataGrid>(cell);
                var bindingPath = GetBindingPath(cell.Column, grid);
                if (!string.IsNullOrEmpty(bindingPath))
                {
                    return ReflectionHelper.GetPropValue(cell.DataContext, bindingPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting cell value: {ex.Message}");
            }

            return null;
        }

        private static T FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            var current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Extracts the binding path from a DataGridColumn for context menu operations.
        /// Prefers the <see cref="GridColumn.FieldName"/> from the parent grid's descriptor when
        /// available — that is the canonical source identifier and resolves correctly through
        /// <see cref="System.ComponentModel.TypeDescriptor"/> for both POCO and DataRowView items,
        /// regardless of the binding-path syntax (dot path vs. indexer) the WPF column was
        /// generated with.
        /// </summary>
        private static string GetBindingPath(DataGridColumn column, SearchDataGrid grid = null)
        {
            if (column == null) return null;

            if (grid != null)
            {
                var descriptor = grid.FindGridColumnDescriptor(column);
                if (descriptor != null && !string.IsNullOrEmpty(descriptor.FieldName))
                    return descriptor.FieldName;
            }

            // ClipboardContentBinding is the universal "value for copy" binding — set explicitly
            // by GridColumn when generating a DataGridTemplateColumn from EditSettings, and
            // defaults to the bound Binding on DataGridBoundColumn.
            if (column.ClipboardContentBinding is Binding clipboardBinding)
            {
                var clipboardPath = NormalizePath(clipboardBinding.Path?.Path);
                if (!string.IsNullOrEmpty(clipboardPath)) return clipboardPath;
            }

            if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is Binding binding)
            {
                var bindingPath = NormalizePath(binding.Path?.Path);
                if (!string.IsNullOrEmpty(bindingPath)) return bindingPath;
            }

            return column.SortMemberPath;
        }

        /// <summary>
        /// Strips outer indexer brackets from a binding path. WPF auto-generates DataTable column
        /// bindings as <c>[ColumnName]</c>; <see cref="ReflectionHelper.GetPropValue"/> needs the
        /// bare column name to resolve through <see cref="System.ComponentModel.TypeDescriptor"/>.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length >= 2 && path[0] == '[' && path[path.Length - 1] == ']')
                return path.Substring(1, path.Length - 2);
            return path;
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
        GridBody,
    }

    #endregion
}
