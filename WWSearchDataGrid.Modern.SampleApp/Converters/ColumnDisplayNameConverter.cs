using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    /// <summary>
    /// Converts a DataGridColumn to its display name by extracting text from the Header.
    /// </summary>
    public class ColumnDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DataGridColumn column)
                return string.Empty;

            string displayName = SearchDataGrid.ExtractColumnHeaderText(column);

            return displayName ?? $"Column {column.DisplayIndex + 1}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
