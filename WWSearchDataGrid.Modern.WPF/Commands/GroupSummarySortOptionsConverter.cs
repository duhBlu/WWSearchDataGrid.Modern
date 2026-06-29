using System;
using System.Globalization;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    /// <summary>
    /// Builds the "Sort By Summary" menu listing for the group-panel pill context menu. The
    /// first input is the pill's <see cref="GridColumn"/> (the menu's DataContext); the result
    /// comes from the owning grid's <c>BuildGroupSummarySortOptions</c>. The remaining inputs
    /// exist only to re-evaluate the listing when the configuration changes underneath an
    /// already-instantiated menu — the <c>GroupSummaries</c> collection (and its Count, which
    /// changes on membership edits), <c>ShowGroupRowCount</c>, and the grid's
    /// <c>ActiveGroupSummarySort</c> (a fresh instance per sort change, refreshing the
    /// check-marks).
    /// </summary>
    public class GroupSummarySortOptionsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not { Length: > 0 } || values[0] is not GridColumn column) return null;
            return column.View?.BuildGroupSummarySortOptions();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
