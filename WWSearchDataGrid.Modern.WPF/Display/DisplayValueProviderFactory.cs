using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Display
{
    /// <summary>
    /// Creates the appropriate IDisplayValueProvider for a column based on its GridColumn descriptor.
    /// Priority: DisplayMask > DisplayValueConverter > DisplayStringFormat.
    /// Returns null if no display transformation is configured.
    /// </summary>
    internal static class DisplayValueProviderFactory
    {
        /// <summary>
        /// Creates a display value provider from a <see cref="GridColumn"/> descriptor,
        /// or null if no display transformation is configured.
        /// </summary>
        public static IDisplayValueProvider Create(GridColumn descriptor)
        {
            if (descriptor == null)
                return null;

            // Priority 1: Mask
            if (!string.IsNullOrEmpty(descriptor.DisplayMask))
                return new MaskDisplayProvider(descriptor.DisplayMask);

            // Priority 2: Explicit converter
            if (descriptor.DisplayValueConverter != null)
                return new ConverterDisplayProvider(descriptor.DisplayValueConverter, descriptor.DisplayConverterParameter);

            // Priority 3: String format
            if (!string.IsNullOrEmpty(descriptor.DisplayStringFormat))
                return new StringFormatDisplayProvider(descriptor.DisplayStringFormat);

            return null;
        }
    }
}
