using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    internal partial class ContextMenuCommands
    {
        /// <summary>
        /// Shows count summary for the column
        /// </summary>
        public static ICommand ShowCountSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Count Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show count summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows sum summary for the column
        /// </summary>
        public static ICommand ShowSumSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Sum Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show sum summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows average summary for the column
        /// </summary>
        public static ICommand ShowAverageSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Average Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show average summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows minimum value summary for the column
        /// </summary>
        public static ICommand ShowMinSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Min Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show minimum summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows maximum value summary for the column
        /// </summary>
        public static ICommand ShowMaxSummaryCommand => new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Max Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show maximum summary in footer
        }, column => column != null);
    }
}
