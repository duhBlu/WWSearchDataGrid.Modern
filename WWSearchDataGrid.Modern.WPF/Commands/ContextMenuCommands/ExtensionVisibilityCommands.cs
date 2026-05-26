using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        private static ICommand _showColumnChooserCommand;
        /// <summary>
        /// Opens the column management editor dialog
        /// </summary>
        public static ICommand ShowColumnChooserCommand => _showColumnChooserCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            try
            {
                var ColumnChooser = new ColumnChooser
                {
                    SourceDataGrid = grid
                };

                // Show the non-modal window
                ColumnChooser.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowColumnChooserCommand: {ex.Message}");
            }
        }, grid => grid != null);

        /// <summary>
        /// Opens the modal multi-column Filter Editor. The command accepts either a
        /// <see cref="SearchDataGrid"/> directly (button binding) or a
        /// <see cref="DataGridColumnHeader"/> (context menu binding), in which case the grid
        /// is resolved by walking the visual tree.
        /// </summary>
        private static ICommand _openFilterEditorCommand;
        public static ICommand OpenFilterEditorCommand => _openFilterEditorCommand ??= new RelayCommand<object>(parameter =>
        {
            try
            {
                var grid = ResolveSearchDataGrid(parameter);
                if (grid == null) return;
                FilterEditor.ShowDialog(grid);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OpenFilterEditorCommand: {ex.Message}");
            }
        }, parameter => ResolveSearchDataGrid(parameter) != null);

        private static SearchDataGrid ResolveSearchDataGrid(object parameter)
        {
            switch (parameter)
            {
                case SearchDataGrid grid: return grid;
                case DataGridColumnHeader header: return FindAncestor<SearchDataGrid>(header);
                case DependencyObject dep: return FindAncestor<SearchDataGrid>(dep);
            }
            return null;
        }

        /// <summary>
        /// Toggles the visibility of the totals/summary row
        /// </summary>
        private static ICommand _toggleTotalsRowCommand;
        public static ICommand ToggleTotalsRowCommand => _toggleTotalsRowCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Totals Row - Not implemented");
            // TODO: Toggle summary row visibility
        }, grid => grid != null);

        /// <summary>
        /// Toggles the visibility of the filter panel
        /// </summary>
        private static ICommand _toggleFilterPanelCommand;
        public static ICommand ToggleFilterPanelCommand => _toggleFilterPanelCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Filter Panel - Not implemented");
            // TODO: Toggle FilterPanel visibility
        }, grid => grid?.FilterPanel != null);

        /// <summary>
        /// Toggles the visibility of search controls
        /// </summary>
        private static ICommand _toggleSearchControlCommand;
        public static ICommand ToggleSearchControlCommand => _toggleSearchControlCommand ??= new RelayCommand<SearchDataGrid>(grid =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Toggle Search Controls - Not implemented");
            // TODO: Toggle search box visibility in column headers
        }, grid => grid != null);

        /// <summary>
        /// Opens the rule-filter editor popup for a column. Invoked by the per-column
        /// filter button hosted in the <see cref="DataGridColumnHeader"/> chrome — the button's
        /// <c>CommandParameter</c> binds to its templated header, the command walks up to the
        /// owning <see cref="SearchDataGrid"/>, looks up the column's filter host through
        /// <see cref="SearchDataGrid.FindColumnFilterControl"/>, and asks it to show its
        /// popup via <see cref="IColumnFilterHost.ShowFilterEditor"/>. Falls back gracefully
        /// when the parameter is the host directly (consumer-driven invocation paths).
        /// </summary>
        /// <remarks>
        /// CanExecute checks parameter shape only — not registry state. The pinned
        /// AutoFilterRowPresenter creates and registers a ColumnFilterControl per column
        /// asynchronously (one dispatcher tick after the header template applies), so the
        /// registry is briefly empty when the header button is first templated. Gating
        /// CanExecute on registry presence would leave the button IsEnabled=false until
        /// CommandManager.RequerySuggested fires, and RequerySuggested only fires on
        /// key/mouse-button/focus events — not on mouse moves — so the button would appear
        /// dead to hover/click until the user interacted with the filter row first.
        /// </remarks>
        private static ICommand _showFilterEditorCommand;
        public static ICommand ShowFilterEditorCommand => _showFilterEditorCommand ??= new RelayCommand<object>(
            parameter => ResolveColumnFilterHost(parameter)?.ShowFilterEditor(),
            parameter => CanResolveColumnFilterHost(parameter));

        private static bool CanResolveColumnFilterHost(object parameter)
        {
            return parameter switch
            {
                IColumnFilterHost => true,
                DataGridColumnHeader header => header.Column != null,
                _ => false,
            };
        }

        private static IColumnFilterHost ResolveColumnFilterHost(object parameter)
        {
            switch (parameter)
            {
                case IColumnFilterHost host:
                    return host;

                case DataGridColumnHeader header:
                    if (header.Column == null) return null;
                    var grid = FindAncestor<SearchDataGrid>(header);
                    return grid?.FindColumnFilterControl(header.Column) as IColumnFilterHost;
            }
            return null;
        }

        private static T FindAncestor<T>(DependencyObject from) where T : DependencyObject
        {
            var current = from;
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
