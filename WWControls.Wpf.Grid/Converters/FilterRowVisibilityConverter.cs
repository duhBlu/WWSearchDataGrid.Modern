using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Converters
{
    /// <summary>
    /// Multi-converter that returns <see cref="Visibility.Visible"/> when both
    /// <see cref="SearchDataGrid.ShowFilterRow"/> is <c>true</c> <em>and</em>
    /// <see cref="SearchDataGrid.FilterEditorPlacement"/> is
    /// <see cref="Wpf.FilterEditorPlacement.Row"/>; <see cref="Visibility.Collapsed"/>
    /// otherwise. Drives the dedicated pinned filter row in the
    /// <see cref="SearchDataGrid"/> template.
    /// </summary>
    /// <remarks>
    /// In-header placement reuses the column header chrome rather than the pinned row,
    /// so the row collapses entirely whenever the placement is not <c>Row</c> — collapsing
    /// (rather than hiding) saves the measure + arrange pass for every column's filter
    /// editor when the row isn't in use.
    /// </remarks>
    public sealed class FilterRowVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            bool show = values[0] is bool b && b;
            bool isRowPlacement = values[1] is FilterEditorPlacement pos && pos == FilterEditorPlacement.Row;
            bool pinnedRowActive = show && isRowPlacement;

            // ConverterParameter="Invert" flips the result. Used in the DataGridColumnHeader
            // template to hide the embedded ColumnSearchBox when the pinned row is active —
            // same inputs, opposite output, no second converter needed.
            bool invert = parameter is string s && string.Equals(s, "Invert", StringComparison.OrdinalIgnoreCase);
            bool visible = invert ? !pinnedRowActive : pinnedRowActive;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
