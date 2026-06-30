using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// Converter that finds the <see cref="ColumnFilterControl"/> matching a column name +
    /// binding path tuple. Operates against any <see cref="IColumnFilterHost"/> collection;
    /// only <see cref="ColumnFilterControl"/> exposes a public <c>BindingPath</c>, so
    /// non-matching hosts are skipped.
    /// </summary>
    public class ColumnNameToControlConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return null;

            string columnName = values[0] as string;
            string bindingPath = values[1] as string;

            if (string.IsNullOrEmpty(columnName) || values[2] is not IEnumerable<IColumnFilterHost> dataColumns)
                return null;

            return dataColumns
                .OfType<ColumnFilterControl>()
                .FirstOrDefault(c =>
                    c.CurrentColumn?.Header?.ToString() == columnName &&
                    c.BindingPath == bindingPath);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is ColumnFilterControl host)
            {
                return new object[]
                {
                    host.CurrentColumn?.Header?.ToString(),
                    host.BindingPath,
                    null
                };
            }

            return new object[] { null, null, null };
        }
    }
}
