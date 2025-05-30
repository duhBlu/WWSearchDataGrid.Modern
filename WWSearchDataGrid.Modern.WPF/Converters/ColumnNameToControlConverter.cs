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
    /// Converter that finds the SearchControl matching a column name
    /// </summary>
    public class ColumnNameToControlConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return null;

            string columnName = values[0] as string;
            string bindingPath = values[1] as string;
            var dataColumns = values[2] as System.Collections.Generic.IEnumerable<SearchControl>;

            if (string.IsNullOrEmpty(columnName) || dataColumns == null)
                return null;

            // Find the matching column control
            return dataColumns.FirstOrDefault(c =>
                c.CurrentColumn?.Header?.ToString() == columnName &&
                c.BindingPath == bindingPath);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is SearchControl searchControl)
            {
                return new object[]
                {
                    searchControl.CurrentColumn?.Header?.ToString(),
                    searchControl.BindingPath,
                    null
                };
            }

            return new object[] { null, null, null };
        }
    }
}
