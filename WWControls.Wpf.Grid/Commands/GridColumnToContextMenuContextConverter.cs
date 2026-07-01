using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WWControls.Wpf.Commands
{
    /// <summary>
    /// Adapts a <see cref="GridColumn"/> descriptor into a <see cref="ContextMenuContext"/>
    /// populated with <see cref="ContextMenuContext.Grid"/>, <see cref="ContextMenuContext.Column"/>,
    /// and (when resolvable) <see cref="ContextMenuContext.ColumnSearchBox"/>. Used by the group
    /// panel's pill context menu so the pill can reuse every column-header command without
    /// duplicating each one against a <see cref="GridColumn"/> parameter type.
    /// </summary>
    /// <remarks>
    /// The column-header menu commands take <see cref="ContextMenuContext"/>; their existing
    /// invocation path (<c>SearchDataGridColumnHeader</c>) populates it from the header surface.
    /// On the pill side the binding source is a <see cref="GridColumn"/> directly, so this
    /// converter is the bridge — without it every column-header command would need a parallel
    /// <see cref="GridColumn"/>-typed twin.
    /// </remarks>
    public class GridColumnToContextMenuContextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not GridColumn column) return null;
            var grid = column.View;
            if (grid == null) return null;

            // ColumnSearchBox is needed by the filter-clear command; null is acceptable for
            // every other command, so the lookup failing just means that one item is disabled.
            var host = grid.DataColumns?.FirstOrDefault(h =>
                h != null && (h.GridColumn == column || h.CurrentColumn == column.InternalColumn));

            return new ContextMenuContext
            {
                ContextType = ContextMenuType.ColumnHeader,
                Grid = grid,
                Column = column.InternalColumn,
                ColumnSearchBox = host,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
