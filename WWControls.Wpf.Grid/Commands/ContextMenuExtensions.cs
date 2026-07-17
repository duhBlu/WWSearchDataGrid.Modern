using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WWControls.Core;
using WWControls.Wpf.Commands;

namespace WWControls.Wpf
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

        /// <summary>
        /// Stable identifier stamped on each built-in menu item by the theme. Read by the hide-list
        /// (<see cref="SearchDataGrid.HiddenContextMenuItems"/>) and the
        /// <see cref="GridContextMenuInitializingEventArgs"/> lookup/hide helpers. Consumer-authored
        /// items normally leave this unset (<see cref="GridContextMenuItem.None"/>); set it only if
        /// you want your own item to be targetable by ID.
        /// </summary>
        public static readonly DependencyProperty ItemIdProperty =
            DependencyProperty.RegisterAttached(
                "ItemId",
                typeof(GridContextMenuItem),
                typeof(ContextMenuExtensions),
                new PropertyMetadata(GridContextMenuItem.None));

        /// <summary>Gets the <see cref="GridContextMenuItem"/> id stamped on a menu item.</summary>
        public static GridContextMenuItem GetItemId(DependencyObject obj)
            => (GridContextMenuItem)obj.GetValue(ItemIdProperty);

        /// <summary>Sets the <see cref="GridContextMenuItem"/> id on a menu item.</summary>
        public static void SetItemId(DependencyObject obj, GridContextMenuItem value)
            => obj.SetValue(ItemIdProperty, value);

        #endregion

        #region Initialization

        private static bool _openedRequeryRegistered;

        /// <summary>
        /// Initializes context menu functionality for the SearchDataGrid
        /// </summary>
        public static void InitializeContextMenu(this SearchDataGrid grid)
        {
            if (grid == null) return;

            EnsureContextMenuOpenedRequery();
            grid.ContextMenuOpening += OnContextMenuOpening;
        }

        /// <summary>
        /// One-time class handler that pulses <see cref="Core.CommandManager"/> whenever a
        /// ContextMenu finishes opening. Core's <see cref="RelayCommand{T}"/> gets no WPF
        /// auto-requery, so a MenuItem re-evaluates <c>CanExecute</c> only when the command
        /// raises <c>CanExecuteChanged</c> — not when its <c>CommandParameter</c> resolves. The
        /// menus whose parameter binds off <c>PlacementTarget</c> (group header, fixed group
        /// header, group panel, pill, and the summary cells) resolve that parameter only as the
        /// popup opens, which is after the <see cref="OnContextMenuOpening"/> pulse has already
        /// fired; without a second pulse their items evaluate once against a null parameter
        /// (<c>null is T</c> is false) and stay disabled. By <see cref="ContextMenu.OpenedEvent"/>
        /// the <c>PlacementTarget</c> bindings have resolved, so re-querying here enables them.
        /// Registered against the <see cref="ContextMenu"/> class (not per grid), so it is guarded
        /// to run once.
        /// </summary>
        private static void EnsureContextMenuOpenedRequery()
        {
            if (_openedRequeryRegistered) return;
            _openedRequeryRegistered = true;

            EventManager.RegisterClassHandler(
                typeof(ContextMenu),
                ContextMenu.OpenedEvent,
                new RoutedEventHandler((_, _) => Core.CommandManager.InvalidateRequerySuggested()));
        }

        /// <summary>
        /// Handles the context menu opening event - determines context and sets attached property
        /// </summary>
        private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not SearchDataGrid grid) return;

            Core.CommandManager.InvalidateRequerySuggested();

            var sourceElement = e.OriginalSource as FrameworkElement;

            // Group-header and group-footer rows carry their own self-contained ContextMenu — the
            // header on an inner border (bound via PlacementTarget.DataContext), the footer on each
            // cell (bound via PlacementTarget) — so they need no column/cell/row context injected
            // here. Bail without handling so WPF opens that menu natively; otherwise the target
            // lookup below resolves the enclosing DataGridRow (whose own ContextMenu is null) and
            // suppresses it.
            if (IsWithinGroupSentinelRow(sourceElement))
                return;

            // Determine context from the source element
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
                    // Rebuild the built-in set before anything else. The menus are shared resources,
                    // so this clears whatever any grid injected last time, restores the built-ins in
                    // order (minus HiddenContextMenuItems), or — in Replace mode — drops them so the
                    // consumer's own items stand alone.
                    BuildBuiltInItems(contextMenu, grid, context.ContextType);

                    contextMenu.DataContext = context;

                    // Consumer customization: declarative per-surface collection first, then the
                    // imperative event for anything built from the click's state.
                    AppendCustomContextMenuItems(contextMenu, grid, context.ContextType);
                    grid.RaiseContextMenuInitializing(contextMenu, context);
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

        #region Custom Item Injection

        /// <summary>
        /// Snapshot of each shared menu's (and submenu's) authored built-in items, captured the first
        /// time a menu is opened — before any injection or hiding. Keyed weakly (on the
        /// <see cref="ItemsControl"/> — the <see cref="ContextMenu"/> and every submenu
        /// <see cref="MenuItem"/>) so menus that fall out of use don't leak.
        /// </summary>
        private static readonly ConditionalWeakTable<ItemsControl, List<object>> _menuSnapshots
            = new ConditionalWeakTable<ItemsControl, List<object>>();

        /// <summary>
        /// Rebuilds a menu's built-in items from its pristine snapshot each opening. In
        /// <see cref="ContextMenuItemsMode.Append"/> the built-ins are restored in order minus
        /// anything in <see cref="SearchDataGrid.HiddenContextMenuItems"/> (submenus included, with
        /// orphaned separators tidied); in <see cref="ContextMenuItemsMode.Replace"/> they are
        /// dropped entirely. Either way this clears whatever collection / event items a prior opening
        /// (on this or, via the shared instance, another grid) left behind, so customization can
        /// never corrupt the built-in set.
        /// </summary>
        private static void BuildBuiltInItems(ContextMenu menu, SearchDataGrid grid, ContextMenuType contextType)
        {
            EnsureCaptured(menu);

            if (grid.GetContextMenuModeFor(contextType) == ContextMenuItemsMode.Replace)
            {
                menu.Items.Clear();
                return;
            }

            var hidden = grid.HiddenContextMenuItems;
            var hiddenSet = (hidden == null || hidden.Count == 0)
                ? null
                : new HashSet<GridContextMenuItem>(hidden);

            RebuildContainer(menu, hiddenSet);
        }

        /// <summary>Recursively snapshots a menu and its submenus on first sight.</summary>
        private static void EnsureCaptured(ItemsControl container)
        {
            if (_menuSnapshots.TryGetValue(container, out _)) return;
            _menuSnapshots.Add(container, container.Items.Cast<object>().ToList());

            foreach (var obj in container.Items)
                if (obj is MenuItem mi && mi.HasItems)
                    EnsureCaptured(mi);
        }

        /// <summary>
        /// Restores one container (menu or submenu) from its snapshot, skipping hidden ids and
        /// dropping separators that would end up leading, trailing, or doubled once items are gone.
        /// </summary>
        private static void RebuildContainer(ItemsControl container, HashSet<GridContextMenuItem> hiddenSet)
        {
            if (!_menuSnapshots.TryGetValue(container, out var snapshot)) return;

            container.Items.Clear();

            object pendingSeparator = null;
            var anyReal = false;

            foreach (var child in snapshot)
            {
                if (child is Separator)
                {
                    // Hold it; only emit once a real item follows (drops leading / doubled ones).
                    if (anyReal) pendingSeparator = child;
                    continue;
                }

                if (child is MenuItem mi)
                {
                    var id = GetItemId(mi);
                    if (id != GridContextMenuItem.None && hiddenSet != null && hiddenSet.Contains(id))
                        continue;
                }

                if (pendingSeparator != null)
                {
                    container.Items.Add(pendingSeparator);
                    pendingSeparator = null;
                }

                container.Items.Add(child);
                anyReal = true;

                if (child is MenuItem sub && _menuSnapshots.TryGetValue(sub, out _))
                    RebuildContainer(sub, hiddenSet);
            }

            // A trailing pendingSeparator is intentionally dropped.
        }

        /// <summary>
        /// Adds the grid's per-surface custom items (<see cref="SearchDataGrid.CellContextMenuItems"/>
        /// and siblings) to the menu — beneath the built-ins under a separator in
        /// <see cref="ContextMenuItemsMode.Append"/>, or as the whole menu in
        /// <see cref="ContextMenuItemsMode.Replace"/> (where the built-ins were already cleared). The
        /// menu's <c>DataContext</c> (the <see cref="ContextMenuContext"/>) flows to each item, so
        /// their command/parameter bindings resolve against the click context.
        /// </summary>
        private static void AppendCustomContextMenuItems(ContextMenu menu, SearchDataGrid grid, ContextMenuType contextType)
        {
            var items = grid.GetContextMenuItemsFor(contextType);
            if (items == null || items.Count == 0) return;

            if (menu.Items.Count > 0)
                menu.Items.Add(new Separator());

            foreach (var item in items)
            {
                if (item == null) continue;

                // A Control lives in one items collection at a time. If the same instance was placed
                // in more than one surface collection, it may still be parented to another shared
                // menu — detach before re-hosting so WPF doesn't throw on the duplicate parent.
                if (item.Parent is ItemsControl owner && !ReferenceEquals(owner, menu))
                    owner.Items.Remove(item);

                if (!menu.Items.Contains(item))
                    menu.Items.Add(item);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// True when the right-click originated inside a flat group sentinel row — a group header or
        /// group footer. Those rows attach their ContextMenu to their own chrome (the header to an
        /// inner border, the footer to each cell) while the row container's own <c>ContextMenu</c> is
        /// null, so the context-injection path here would otherwise resolve the
        /// <see cref="DataGridRow"/> and suppress the menu. The pinned summary strip already opens
        /// natively (it sits outside any row).
        /// </summary>
        private static bool IsWithinGroupSentinelRow(FrameworkElement source)
            => source != null
               && VisualTreeHelperMethods.FindVisualAncestor<SearchDataGridRow>(source) is { } row
               && (row.IsGroupHeader || row.IsGroupFooter);

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
