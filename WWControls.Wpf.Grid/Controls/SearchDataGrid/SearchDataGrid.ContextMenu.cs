using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Consumer-facing extension points for the four primary right-click menus — cell, column
    /// header, row, and grid body. Two complementary, strictly-additive APIs:
    /// <list type="bullet">
    ///   <item>
    ///     <description>The declarative per-surface collections (<see cref="CellContextMenuItems"/>
    ///     and siblings) — populate them in XAML for static items.</description>
    ///   </item>
    ///   <item>
    ///     <description>The <see cref="ContextMenuInitializing"/> event — handle it to build items
    ///     imperatively from the clicked row's state.</description>
    ///   </item>
    /// </list>
    /// Both run each time a menu opens: the menu is first rebuilt from its pristine built-in set,
    /// then the collection items are appended (under a separator), then the event fires. Because the
    /// built-ins are restored on every opening, consumer items can never reorder or drop them, and
    /// nothing bleeds between grids through the shared menu instance.
    /// <para>
    /// Injected items inherit <c>DataContext</c> = the <see cref="Commands.ContextMenuContext"/> for
    /// the click. Since <c>Grid.DataContext</c> is the host view-model (the grid lives in the visual
    /// tree), the clean binding is
    /// <c>Command="{Binding Grid.DataContext.MyCommand}"</c> with
    /// <c>CommandParameter="{Binding RowData}"</c> / <c>{Binding CellValue}</c> / <c>{Binding Column}</c>.
    /// </para>
    /// </summary>
    public partial class SearchDataGrid
    {
        #region Cell

        /// <summary>Read-only key for <see cref="CellContextMenuItems"/>.</summary>
        private static readonly DependencyPropertyKey CellContextMenuItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(CellContextMenuItems),
                typeof(ObservableCollection<Control>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="CellContextMenuItems"/> dependency property.</summary>
        public static readonly DependencyProperty CellContextMenuItemsProperty =
            CellContextMenuItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Custom items (<see cref="MenuItem"/> / <see cref="Separator"/>) appended to the cell
        /// right-click menu beneath the built-in Copy / Filter actions. See the type remarks for the
        /// binding pattern.
        /// </summary>
        public ObservableCollection<Control> CellContextMenuItems =>
            (ObservableCollection<Control>)GetValue(CellContextMenuItemsProperty);

        #endregion

        #region Column Header

        /// <summary>Read-only key for <see cref="ColumnHeaderContextMenuItems"/>.</summary>
        private static readonly DependencyPropertyKey ColumnHeaderContextMenuItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnHeaderContextMenuItems),
                typeof(ObservableCollection<Control>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="ColumnHeaderContextMenuItems"/> dependency property.</summary>
        public static readonly DependencyProperty ColumnHeaderContextMenuItemsProperty =
            ColumnHeaderContextMenuItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Custom items appended to the column-header right-click menu beneath the built-in Sort /
        /// Group / Best-Fit actions. The clicked column is available as <c>{Binding Column}</c>.
        /// </summary>
        public ObservableCollection<Control> ColumnHeaderContextMenuItems =>
            (ObservableCollection<Control>)GetValue(ColumnHeaderContextMenuItemsProperty);

        #endregion

        #region Row

        /// <summary>Read-only key for <see cref="RowContextMenuItems"/>.</summary>
        private static readonly DependencyPropertyKey RowContextMenuItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(RowContextMenuItems),
                typeof(ObservableCollection<Control>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="RowContextMenuItems"/> dependency property.</summary>
        public static readonly DependencyProperty RowContextMenuItemsProperty =
            RowContextMenuItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Custom items appended to the row-header right-click menu. The clicked row's data item is
        /// available as <c>{Binding RowData}</c>.
        /// </summary>
        public ObservableCollection<Control> RowContextMenuItems =>
            (ObservableCollection<Control>)GetValue(RowContextMenuItemsProperty);

        #endregion

        #region Grid Body

        /// <summary>Read-only key for <see cref="BodyContextMenuItems"/>.</summary>
        private static readonly DependencyPropertyKey BodyContextMenuItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(BodyContextMenuItems),
                typeof(ObservableCollection<Control>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="BodyContextMenuItems"/> dependency property.</summary>
        public static readonly DependencyProperty BodyContextMenuItemsProperty =
            BodyContextMenuItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Custom items appended to the grid-body right-click menu (the menu shown when the click
        /// lands outside any cell) beneath the built-in filter / profile / layout actions.
        /// </summary>
        public ObservableCollection<Control> BodyContextMenuItems =>
            (ObservableCollection<Control>)GetValue(BodyContextMenuItemsProperty);

        #endregion

        #region Hidden items

        /// <summary>Read-only key for <see cref="HiddenContextMenuItems"/>.</summary>
        private static readonly DependencyPropertyKey HiddenContextMenuItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HiddenContextMenuItems),
                typeof(ObservableCollection<GridContextMenuItem>),
                typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="HiddenContextMenuItems"/> dependency property.</summary>
        public static readonly DependencyProperty HiddenContextMenuItemsProperty =
            HiddenContextMenuItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// Built-in items to drop from every menu, by <see cref="GridContextMenuItem"/> id. IDs are
        /// semantic, so e.g. adding <see cref="GridContextMenuItem.Copy"/> removes Copy from the
        /// cell, row-header, and column-header menus. Re-evaluated on each opening. This is the
        /// fine-grained complement to the coarse feature gates (<see cref="AllowGrouping"/>,
        /// <see cref="AllowFixedColumnMenu"/>, <see cref="IsColumnChooserEnabled"/>, …). For
        /// per-surface or conditional hiding, handle <see cref="ContextMenuInitializing"/> instead.
        /// </summary>
        public ObservableCollection<GridContextMenuItem> HiddenContextMenuItems =>
            (ObservableCollection<GridContextMenuItem>)GetValue(HiddenContextMenuItemsProperty);

        #endregion

        #region Per-surface mode

        /// <summary>Identifies the <see cref="CellContextMenuMode"/> dependency property.</summary>
        public static readonly DependencyProperty CellContextMenuModeProperty =
            DependencyProperty.Register(nameof(CellContextMenuMode), typeof(ContextMenuItemsMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(ContextMenuItemsMode.Append));

        /// <summary>Whether <see cref="CellContextMenuItems"/> appends to or replaces the built-in cell menu.</summary>
        public ContextMenuItemsMode CellContextMenuMode
        {
            get => (ContextMenuItemsMode)GetValue(CellContextMenuModeProperty);
            set => SetValue(CellContextMenuModeProperty, value);
        }

        /// <summary>Identifies the <see cref="ColumnHeaderContextMenuMode"/> dependency property.</summary>
        public static readonly DependencyProperty ColumnHeaderContextMenuModeProperty =
            DependencyProperty.Register(nameof(ColumnHeaderContextMenuMode), typeof(ContextMenuItemsMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(ContextMenuItemsMode.Append));

        /// <summary>Whether <see cref="ColumnHeaderContextMenuItems"/> appends to or replaces the built-in column-header menu.</summary>
        public ContextMenuItemsMode ColumnHeaderContextMenuMode
        {
            get => (ContextMenuItemsMode)GetValue(ColumnHeaderContextMenuModeProperty);
            set => SetValue(ColumnHeaderContextMenuModeProperty, value);
        }

        /// <summary>Identifies the <see cref="RowContextMenuMode"/> dependency property.</summary>
        public static readonly DependencyProperty RowContextMenuModeProperty =
            DependencyProperty.Register(nameof(RowContextMenuMode), typeof(ContextMenuItemsMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(ContextMenuItemsMode.Append));

        /// <summary>Whether <see cref="RowContextMenuItems"/> appends to or replaces the built-in row-header menu.</summary>
        public ContextMenuItemsMode RowContextMenuMode
        {
            get => (ContextMenuItemsMode)GetValue(RowContextMenuModeProperty);
            set => SetValue(RowContextMenuModeProperty, value);
        }

        /// <summary>Identifies the <see cref="BodyContextMenuMode"/> dependency property.</summary>
        public static readonly DependencyProperty BodyContextMenuModeProperty =
            DependencyProperty.Register(nameof(BodyContextMenuMode), typeof(ContextMenuItemsMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(ContextMenuItemsMode.Append));

        /// <summary>Whether <see cref="BodyContextMenuItems"/> appends to or replaces the built-in grid-body menu.</summary>
        public ContextMenuItemsMode BodyContextMenuMode
        {
            get => (ContextMenuItemsMode)GetValue(BodyContextMenuModeProperty);
            set => SetValue(BodyContextMenuModeProperty, value);
        }

        /// <summary>The append/replace mode for a given right-click surface.</summary>
        internal ContextMenuItemsMode GetContextMenuModeFor(ContextMenuType contextType) =>
            contextType switch
            {
                ContextMenuType.Cell => CellContextMenuMode,
                ContextMenuType.ColumnHeader => ColumnHeaderContextMenuMode,
                ContextMenuType.Row => RowContextMenuMode,
                ContextMenuType.GridBody => BodyContextMenuMode,
                _ => ContextMenuItemsMode.Append
            };

        #endregion

        #region Wiring

        /// <summary>
        /// Seeds the custom-item and hidden-item collections with empty instances so XAML
        /// property-element syntax can populate them immediately. Called once from the constructor —
        /// mirrors how the <see cref="GridColumns"/> / summary collections are seeded.
        /// </summary>
        private void InitializeCustomContextMenuItems()
        {
            SetValue(CellContextMenuItemsPropertyKey, new ObservableCollection<Control>());
            SetValue(ColumnHeaderContextMenuItemsPropertyKey, new ObservableCollection<Control>());
            SetValue(RowContextMenuItemsPropertyKey, new ObservableCollection<Control>());
            SetValue(BodyContextMenuItemsPropertyKey, new ObservableCollection<Control>());
            SetValue(HiddenContextMenuItemsPropertyKey, new ObservableCollection<GridContextMenuItem>());
        }

        /// <summary>
        /// The custom-item collection for a given right-click surface, or <c>null</c> for surfaces
        /// that don't take custom items. Consumed by the menu-splice logic in
        /// <see cref="ContextMenuExtensions"/>.
        /// </summary>
        internal ObservableCollection<Control> GetContextMenuItemsFor(ContextMenuType contextType) =>
            contextType switch
            {
                ContextMenuType.Cell => CellContextMenuItems,
                ContextMenuType.ColumnHeader => ColumnHeaderContextMenuItems,
                ContextMenuType.Row => RowContextMenuItems,
                ContextMenuType.GridBody => BodyContextMenuItems,
                _ => null
            };

        /// <summary>
        /// Raised as one of the four primary right-click menus is about to open, after the built-in
        /// items have been restored and any per-surface collection items appended. Handlers get the
        /// live menu and the resolved context and may add further items — additions are transient,
        /// since the next opening rebuilds the menu from its pristine built-in set.
        /// </summary>
        public event EventHandler<GridContextMenuInitializingEventArgs> ContextMenuInitializing;

        /// <summary>Fires <see cref="ContextMenuInitializing"/>. Called from the menu-splice logic.</summary>
        internal void RaiseContextMenuInitializing(ContextMenu menu, Commands.ContextMenuContext context)
            => ContextMenuInitializing?.Invoke(this, new GridContextMenuInitializingEventArgs(this, menu, context));

        #endregion
    }

    /// <summary>
    /// Data for <see cref="SearchDataGrid.ContextMenuInitializing"/> — the grid, the live
    /// <see cref="ContextMenu"/> being opened, and the resolved click context.
    /// </summary>
    public sealed class GridContextMenuInitializingEventArgs : EventArgs
    {
        /// <summary>Creates the event data.</summary>
        public GridContextMenuInitializingEventArgs(SearchDataGrid grid, ContextMenu menu, Commands.ContextMenuContext context)
        {
            Grid = grid;
            Menu = menu;
            Context = context;
        }

        /// <summary>The grid whose menu is opening.</summary>
        public SearchDataGrid Grid { get; }

        /// <summary>
        /// The live menu being opened. Add items to <see cref="ItemsControl.Items"/>; they render
        /// when the popup shows and are discarded on the next opening.
        /// </summary>
        public ContextMenu Menu { get; }

        /// <summary>
        /// Resolved click context — surface type, column, row data, cell value, and filter host.
        /// </summary>
        public Commands.ContextMenuContext Context { get; }

        /// <summary>Convenience alias for <c>Context.ContextType</c>.</summary>
        public ContextMenuType MenuType => Context.ContextType;

        /// <summary>
        /// The first built-in item in the menu carrying the given id (searching submenus too), or
        /// <c>null</c>. Use it to relabel, re-icon, disable, or swap the command of a built-in —
        /// e.g. <c>e.FindItem(GridContextMenuItem.Copy).Header = "Copy cells"</c>.
        /// </summary>
        public MenuItem FindItem(GridContextMenuItem id)
        {
            foreach (var match in Enumerate(Menu, id))
                return match;
            return null;
        }

        /// <summary>All built-in items in the menu carrying the given id (searching submenus too).</summary>
        public IReadOnlyList<MenuItem> FindItems(GridContextMenuItem id)
        {
            var list = new List<MenuItem>();
            list.AddRange(Enumerate(Menu, id));
            return list;
        }

        /// <summary>
        /// Removes every built-in item carrying any of the given ids from the menu (submenus
        /// included). A per-open, conditional alternative to
        /// <see cref="SearchDataGrid.HiddenContextMenuItems"/>.
        /// </summary>
        public void Hide(params GridContextMenuItem[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            var set = new HashSet<GridContextMenuItem>();
            foreach (var id in ids)
                if (id != GridContextMenuItem.None) set.Add(id);
            if (set.Count == 0) return;
            RemoveMatching(Menu, set);
        }

        private static void RemoveMatching(ItemsControl container, HashSet<GridContextMenuItem> ids)
        {
            var toRemove = new List<MenuItem>();
            foreach (var obj in container.Items)
            {
                if (obj is not MenuItem mi) continue;
                RemoveMatching(mi, ids); // recurse into submenus first
                if (ids.Contains(ContextMenuExtensions.GetItemId(mi))) toRemove.Add(mi);
            }
            foreach (var mi in toRemove)
                container.Items.Remove(mi);
        }

        private static IEnumerable<MenuItem> Enumerate(ItemsControl root, GridContextMenuItem id)
        {
            if (root == null) yield break;
            foreach (var obj in root.Items)
            {
                if (obj is not MenuItem mi) continue;
                if (ContextMenuExtensions.GetItemId(mi) == id) yield return mi;
                foreach (var nested in Enumerate(mi, id)) yield return nested;
            }
        }
    }
}
