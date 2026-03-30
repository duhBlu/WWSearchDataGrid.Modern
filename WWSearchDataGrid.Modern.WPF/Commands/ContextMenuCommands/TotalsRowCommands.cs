using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    public partial class ContextMenuCommands
    {
        /// <summary>
        /// Shows count summary for the column
        /// </summary>
        private static ICommand _showCountSummaryCommand;
        public static ICommand ShowCountSummaryCommand => _showCountSummaryCommand ??= new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Count Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show count summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows sum summary for the column
        /// </summary>
        private static ICommand _showSumSummaryCommand;
        public static ICommand ShowSumSummaryCommand => _showSumSummaryCommand ??= new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Sum Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show sum summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows average summary for the column
        /// </summary>
        private static ICommand _showAverageSummaryCommand;
        public static ICommand ShowAverageSummaryCommand => _showAverageSummaryCommand ??= new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Average Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show average summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows minimum value summary for the column
        /// </summary>
        private static ICommand _showMinSummaryCommand;
        public static ICommand ShowMinSummaryCommand => _showMinSummaryCommand ??= new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Min Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show minimum summary in footer
        }, column => column != null);

        /// <summary>
        /// Shows maximum value summary for the column
        /// </summary>
        private static ICommand _showMaxSummaryCommand;
        public static ICommand ShowMaxSummaryCommand => _showMaxSummaryCommand ??= new RelayCommand<DataGridColumn>(column =>
        {
            Debug.WriteLine($"[PLACEHOLDER] Show Max Summary: Column '{column?.Header}' - Not implemented");
            // TODO: Show maximum summary in footer
        }, column => column != null);
    }
}
