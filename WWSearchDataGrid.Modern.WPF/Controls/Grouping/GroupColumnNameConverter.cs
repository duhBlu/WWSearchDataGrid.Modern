using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Produces the column-name prefix for a group header — e.g. <c>"Country: "</c> — so the
    /// default header chrome can render <c>ColumnName: Value</c> instead of the bare group value.
    /// Used by the default group style (<c>DefaultGroupHeaderTemplate</c>) via a
    /// <see cref="MultiBinding"/>.
    /// </summary>
    /// <remarks>
    /// Inputs (in order):
    /// <list type="number">
    ///   <item><c>DataContext</c> — the header's data item (binding source = <c>{Binding}</c>).
    ///         When it is a <see cref="FixedGroupHeaderEntry"/> (the pinned strip, which has no
    ///         <see cref="GroupItem"/> ancestor to walk), the converter reads the caption straight
    ///         off <see cref="FixedGroupHeaderEntry.Column"/> and ignores the remaining inputs.
    ///         For an in-place header (a <see cref="System.Windows.Data.CollectionViewGroup"/>)
    ///         it falls through to the ancestor-walk path below.</item>
    ///   <item><see cref="GroupItem"/> — the originating group container (binding source =
    ///         <c>RelativeSource AncestorType=GroupItem</c>); the converter counts its
    ///         <see cref="GroupItem"/> ancestors (inclusive) to derive the group's nesting level
    ///         (<c>level = count - 1</c>), matching the resolution used by
    ///         <see cref="GroupValueTemplateSelector"/> and
    ///         <see cref="GroupHeaderTemplateSelector"/>.</item>
    ///   <item><see cref="SearchDataGrid"/> — the owning grid (binding source =
    ///         <c>RelativeSource AncestorType=SearchDataGrid</c>); asked for the column whose
    ///         <see cref="GridColumn.GroupLevel"/> matches the derived level.</item>
    /// </list>
    /// Output: the resolved column's <see cref="ColumnLayoutBase.HeaderCaption"/> followed by
    /// <c>": "</c>, or <see cref="string.Empty"/> when the grid or owning column can't be resolved
    /// or the caption is blank. Returning the empty string (rather than a bare <c>": "</c>) keeps
    /// the prefix invisible when there's nothing to show, so the value slot renders alone.
    /// </remarks>
    public class GroupColumnNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1) return string.Empty;

            // Pinned-strip header: the entry carries its owning column directly.
            if (values[0] is FixedGroupHeaderEntry entry)
                return Format(entry.Column?.HeaderCaption);

            if (values.Length < 3) return string.Empty;
            if (values[1] is not GroupItem groupItem) return string.Empty;
            if (values[2] is not SearchDataGrid grid) return string.Empty;

            int level = CountGroupItemAncestors(groupItem) - 1;
            if (level < 0) return string.Empty;

            return Format(grid.GetGroupedColumnAtLevel(level)?.HeaderCaption);
        }

        private static string Format(string caption)
            => string.IsNullOrEmpty(caption) ? string.Empty : caption + ": ";

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => null;

        /// <summary>
        /// Counts the <see cref="GroupItem"/> ancestors of <paramref name="start"/>, inclusive of
        /// itself. Each grouping level nests one more <see cref="GroupItem"/>, so the count maps
        /// directly to the rendered group's depth.
        /// </summary>
        private static int CountGroupItemAncestors(DependencyObject start)
        {
            int count = 0;
            var current = start;
            while (current != null)
            {
                if (current is GroupItem) count++;
                current = VisualTreeHelper.GetParent(current);
            }
            return count;
        }
    }
}
