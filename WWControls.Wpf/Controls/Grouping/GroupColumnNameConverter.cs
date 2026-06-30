using System;
using System.Globalization;
using System.Windows.Data;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Produces the column-name prefix for a group header — e.g. <c>"Country: "</c> — so the
    /// default header chrome can render <c>ColumnName: Value</c> instead of the bare group value.
    /// Used by <c>DefaultGroupHeaderTemplate</c> via a single <see cref="Binding"/> against the
    /// header's data item.
    /// </summary>
    /// <remarks>
    /// The bound item is the group header's data context, which carries its owning column directly:
    /// a <see cref="GroupHeaderRow"/> (an in-body flat-grouping header row) or a
    /// <see cref="FixedGroupHeaderEntry"/> (a pinned-strip header). The converter reads the column's
    /// <c>HeaderCaption</c> and appends <c>": "</c>; any other item type yields
    /// <see cref="string.Empty"/>. Because both inputs come straight off the bound item, the prefix
    /// resolves the instant the row's <c>DataContext</c> is applied — in step with the value and
    /// count slots — with no <c>RelativeSource</c> ancestor walk to lag behind container generation.
    /// Output: the resolved caption followed by <c>": "</c>, or <see cref="string.Empty"/> when the
    /// column can't be resolved or the caption is blank. Returning the empty string (rather than a
    /// bare <c>": "</c>) keeps the prefix invisible when there's nothing to show, so the value slot
    /// renders alone.
    /// </remarks>
    public class GroupColumnNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value switch
            {
                FixedGroupHeaderEntry entry => Format(entry.Column?.HeaderCaption),
                GroupHeaderRow header => Format(header.OwningColumn?.HeaderCaption),
                _ => string.Empty,
            };

        private static string Format(string caption)
            => string.IsNullOrEmpty(caption) ? string.Empty : caption + ": ";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => null;
    }
}
