using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Display
{
    /// <summary>
    /// Creates the appropriate IDisplayValueProvider for a column based on its GridColumn descriptor.
    /// Priority: DisplayMask > DisplayValueConverter > EditSettings mask (UseMaskAsDisplayFormat=true) >
    /// DisplayStringFormat > ComboBoxEditSettings lookup. Returns null if no display transformation is configured.
    /// </summary>
    internal static class DisplayValueProviderFactory
    {
        /// <summary>
        /// Creates a display value provider from a <see cref="GridColumn"/> descriptor,
        /// or null if no display transformation is configured.
        /// </summary>
        public static IDisplayValueProvider Create(ColumnDataBase descriptor)
        {
            if (descriptor == null)
                return null;

            // Priority 1: Explicit DisplayMask on the column (slot grammar by default).
            if (!string.IsNullOrEmpty(descriptor.DisplayMask))
                return new MaskDisplayProvider(descriptor.DisplayMask);

            // Priority 2: Explicit converter.
            if (descriptor.DisplayValueConverter != null)
                return new ConverterDisplayProvider(descriptor.DisplayValueConverter, descriptor.DisplayConverterParameter);

            // Priority 3: EditSettings opting into UseMaskAsDisplayFormat. The cell already
            // routes through MaskFormatConverter for display in that case
            // (Date/TextEditSettings.CreateDisplayTemplate), so the filter chip / filter row /
            // filter expression need to see the same formatted value to stay consistent.
            // Checked BEFORE DisplayStringFormat because UseMaskAsDisplayFormat=true wins
            // over DisplayStringFormat in the cell template — the filter side must match.
            var (editMask, editMaskType) = ResolveEditSettingsDisplayMask(descriptor);
            if (!string.IsNullOrEmpty(editMask))
                return new MaskDisplayProvider(editMask, editMaskType);

            // Priority 4: String format.
            if (!string.IsNullOrEmpty(descriptor.DisplayStringFormat))
                return new StringFormatDisplayProvider(descriptor.DisplayStringFormat);

            // Priority 5: ComboBoxEditSettings lookup. A column whose editor is a ComboBox with
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

        /// <summary>
        /// Extracts the effective display mask from <see cref="GridColumn.EditSettings"/> when
        /// the editor is configured to use its mask as the display format. Returns
        /// <c>(null, default)</c> when no such mask is configured.
        /// </summary>
        private static (string mask, MaskType maskType) ResolveEditSettingsDisplayMask(ColumnDataBase descriptor)
        {
            switch (descriptor.EditSettings)
            {
                case DateEditSettings dateSettings
                    when dateSettings.UseMaskAsDisplayFormat && !string.IsNullOrEmpty(dateSettings.Mask):
                    return (dateSettings.Mask, dateSettings.MaskType);

                case TextEditSettings textSettings
                    when textSettings.UseMaskAsDisplayFormat && !string.IsNullOrEmpty(textSettings.Mask):
                    return (textSettings.Mask, textSettings.MaskType);

                default:
                    return (null, default);
            }
        }
    }
}
