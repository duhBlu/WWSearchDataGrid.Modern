using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    /// <summary>
    /// Converts a DataGridColumn to its display name by checking ColumnDisplayName first,
    /// then falling back to extracting text from the Header (handling both string and templated headers).
    /// </summary>
    public class ColumnDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DataGridColumn column)
                return string.Empty;

            // Priority 1: Check for GridColumn.ColumnDisplayName attached property
            string displayName = GridColumn.GetColumnDisplayName(column);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            // Priority 2: Extract from Header using SearchDataGrid helper
            displayName = SearchDataGrid.ExtractColumnHeaderText(column);

            return displayName ?? $"Column {column.DisplayIndex + 1}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
