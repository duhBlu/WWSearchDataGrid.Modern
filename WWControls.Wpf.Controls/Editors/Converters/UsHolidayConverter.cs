using System;
using System.Globalization;
using System.Windows.Data;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Maps a calendar day button's date (its <c>DataContext</c>) to US federal
    /// holiday state. With no <c>ConverterParameter</c> it returns a <see cref="bool"/> for a
    /// day-button <c>DataTrigger</c>; with <c>ConverterParameter="Name"</c> it returns the
    /// holiday name (or <c>null</c>) for a tooltip. Backed by <see cref="UsFederalHolidays"/>.
    /// </summary>
    public class UsHolidayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool wantName = parameter is string s && string.Equals(s, "Name", StringComparison.OrdinalIgnoreCase);
            if (value is DateTime dt)
            {
                return wantName ? (object)UsFederalHolidays.GetHolidayName(dt) : UsFederalHolidays.IsHoliday(dt);
            }
            return wantName ? null : (object)false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
