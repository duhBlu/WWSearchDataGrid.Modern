using System;
using System.Globalization;

namespace WWSearchDataGrid.Modern.Core
{
    public static class TypeTranslatorHelper
    {
        public static DateTime? ConvertToDateTime(object value)
        {
            if (value is DateTime dt)
                return dt;
            if (DateTime.TryParse(value?.ToString(), out DateTime parsed))
                return parsed;
            return null;
        }

        public static decimal? ConvertToDecimal(object value)
        {
            if (value is decimal dec)
                return dec;
            if (value is int i)
                return (decimal)i;
            if (value is double d)
                return (decimal)d;
            if (value is float f)
                return (decimal)f;
            if (value is long l)
                return (decimal)l;
            if (value is short s)
                return (decimal)s;
            if (value is byte b)
                return (decimal)b;

            string str = value?.ToString();
            if (str == null) return null;

            // Try standard parse first, then formatted parse (handles "$1,234.56", "15.0%", etc.)
            if (decimal.TryParse(str, out decimal parsed))
                return parsed;
            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal formatted))
                return formatted;

            return null;
        }
    }
}
