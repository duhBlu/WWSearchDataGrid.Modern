using System;
using System.Collections.Generic;
using System.Text;

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
            if (decimal.TryParse(value?.ToString(), out decimal parsed))
                return parsed;
            return null;
        }
    }
}
