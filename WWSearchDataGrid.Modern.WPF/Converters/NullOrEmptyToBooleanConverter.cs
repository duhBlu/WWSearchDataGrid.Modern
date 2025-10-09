using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    public class NullOrEmptyToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter is string strParam && strParam.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool isEmpty = value == null || string.IsNullOrWhiteSpace(value.ToString());

            if (invert)
            {
                return isEmpty ? false : true;
            }

            return isEmpty ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
