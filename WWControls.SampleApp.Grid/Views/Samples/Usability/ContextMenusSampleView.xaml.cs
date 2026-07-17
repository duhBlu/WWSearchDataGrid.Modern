using System;
using System.Windows;
using System.Windows.Controls;
using WWControls.SampleApp.Grid.Models;

namespace WWControls.SampleApp.Grid.Views.Samples.Usability
{
    public partial class ContextMenusSampleView : UserControl
    {
        public ContextMenusSampleView()
        {
            InitializeComponent();

            // Declarative injection (the collections in XAML) covers the static items; this event
            // covers the dynamic case — items built from the clicked row's state, and per-open
            // tweaks to the built-ins.
            Grid.ContextMenuInitializing += OnGridContextMenuInitializing;
        }

        /// <summary>
        /// Per-open, row-aware edits to the cell menu: hide "Filter By This Value" on cancelled
        /// orders, and add an "Expedite" item for Submitted orders. Both are rebuilt away on the next
        /// opening, so there's nothing to undo. (Adding and hiding are per-open and safe; changing a
        /// built-in's text/command would mutate the shared theme instance for every grid, so prefer
        /// hide + a custom item, or Replace mode, for that.)
        /// </summary>
        private void OnGridContextMenuInitializing(object sender, GridContextMenuInitializingEventArgs e)
        {
            if (e.MenuType != ContextMenuType.Cell) return;
            if (e.Context.RowData is not OrderItem order) return;

            if (order.OrderCancelled)
                e.Hide(GridContextMenuItem.FilterByCellValue);

            if (!string.Equals(order.OrderStatusName, "Submitted", StringComparison.OrdinalIgnoreCase)) return;

            var vm = DataContext as ContextMenusSampleViewModel;

            e.Menu.Items.Add(new Separator());
            e.Menu.Items.Add(new MenuItem
            {
                Header = $"Expedite order #{order.OrderNumber}",
                Command = vm?.ExpediteOrderCommand,
                CommandParameter = order
            });
        }

        /// <summary>Adds / removes the checkbox's <see cref="GridContextMenuItem"/> from the grid's hide-list.</summary>
        private void OnHideItemToggled(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: GridContextMenuItem id } cb) return;

            if (cb.IsChecked == true)
            {
                if (!Grid.HiddenContextMenuItems.Contains(id))
                    Grid.HiddenContextMenuItems.Add(id);
            }
            else
            {
                Grid.HiddenContextMenuItems.Remove(id);
            }
        }

        /// <summary>Flips the cell menu between Append and Replace.</summary>
        private void OnReplaceCellMenuToggled(object sender, RoutedEventArgs e)
            => Grid.CellContextMenuMode = ReplaceCellMenu.IsChecked == true
                ? ContextMenuItemsMode.Replace
                : ContextMenuItemsMode.Append;
    }
}
