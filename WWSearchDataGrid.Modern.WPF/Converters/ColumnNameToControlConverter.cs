using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Converter that finds the ColumnSearchBox matching a column name
    /// </summary>
    public class ColumnNameToControlConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return null;

            string columnName = values[0] as string;
            string bindingPath = values[1] as string;

            if (string.IsNullOrEmpty(columnName) || values[2] is not System.Collections.Generic.IEnumerable<ColumnSearchBox> dataColumns)
                return null;

            // Find the matching column control
            return dataColumns.FirstOrDefault(c =>
                c.CurrentColumn?.Header?.ToString() == columnName &&
                c.BindingPath == bindingPath);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is ColumnSearchBox columnSearchBox)
            {
                return new object[]
                {
                    columnSearchBox.CurrentColumn?.Header?.ToString(),
                    columnSearchBox.BindingPath,
                    null
                };
            }

            return new object[] { null, null, null };
        }
    }
}
