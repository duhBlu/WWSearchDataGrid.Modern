using System.Windows.Controls;
using System.Windows.Data;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Display
{
    /// <summary>
    /// Creates the appropriate IDisplayValueProvider for a column based on its GridColumn attached properties.
    /// Priority: DisplayMask > DisplayValueConverter > DisplayStringFormat.
    /// Returns null if no display transformation is configured.
    /// </summary>
    internal static class DisplayValueProviderFactory
    {
        /// <summary>
        /// Creates a display value provider for the given column, or null if none is configured.
        /// </summary>
        public static IDisplayValueProvider Create(DataGridColumn column)
        {
            if (column == null)
                return null;

            // Priority 1: Mask
            string mask = GridColumn.GetDisplayMask(column);
            if (!string.IsNullOrEmpty(mask))
                return new MaskDisplayProvider(mask);

            // Priority 2: Explicit converter
            var converter = GridColumn.GetDisplayValueConverter(column);
            if (converter != null)
            {
                var parameter = GridColumn.GetDisplayConverterParameter(column);
                return new ConverterDisplayProvider(converter, parameter);
            }

            // Priority 3: String format
            string format = GridColumn.GetDisplayStringFormat(column);
            if (!string.IsNullOrEmpty(format))
                return new StringFormatDisplayProvider(format);

            return null;
        }
    }
}
