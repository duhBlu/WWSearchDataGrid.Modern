using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Display
{
    /// <summary>
    /// Creates the appropriate IDisplayValueProvider for a column based on its GridColumn descriptor.
    /// Priority: DisplayMask > DisplayValueConverter > DisplayStringFormat > ComboBoxEditSettings lookup.
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

            // Priority 4: ComboBoxEditSettings lookup. A column whose editor is a ComboBox with
            // a DisplayMemberPath wants its filter popup / chips / copy commands to show the
            // display name rather than the raw id (or raw item ToString). Only kicks in when
            // there's actually a translation to do — a string-list ComboBox (no DisplayMember /
            // SelectedValuePath) leaves the value unchanged and gets no provider.
            if (descriptor.EditSettings is ComboBoxEditSettings comboSettings
                && !string.IsNullOrEmpty(comboSettings.DisplayMemberPath)
                && comboSettings.ItemsSource != null)
            {
                return new ComboBoxLookupDisplayProvider(
                    comboSettings.ItemsSource,
                    comboSettings.DisplayMemberPath,
                    comboSettings.SelectedValuePath);
            }

            return null;
        }
    }
}
