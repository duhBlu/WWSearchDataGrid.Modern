using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    internal class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string strParam)
            {
                var options = strParam.Split(',');
                if (options.Length == 2)
                {
                    return boolValue ? options[0] : options[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue && parameter is string strParam)
            {
                var options = strParam.Split(',');
                if (options.Length == 2)
                {
                    return strValue == options[0];
                }
            }
            return false;
        }
    }
}
