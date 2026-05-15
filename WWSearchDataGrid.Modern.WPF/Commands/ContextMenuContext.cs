using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    /// <summary>
    /// Provides context information for context menu operations.
    /// This class is exposed to XAML bindings and can be used as CommandParameter or DataContext for menu items.
    /// </summary>
    public class ContextMenuContext
    {
        /// <summary>
        /// The type of context menu being displayed
        /// </summary>
        public ContextMenuType ContextType { get; set; }

        /// <summary>
        /// The SearchDataGrid instance
        /// </summary>
        public SearchDataGrid Grid { get; set; }

        /// <summary>
        /// The DataGridColumn associated with the context (for ColumnHeader and Cell contexts)
        /// </summary>
        public DataGridColumn Column { get; set; }

        /// <summary>
        /// The filter host associated with the column (for ColumnHeader and Cell contexts).
        /// Typed as <see cref="IColumnFilterHost"/> so context-menu commands work against any
        /// filter-host implementation. Phase 5 generalized this from the legacy
        /// <c>ColumnSearchBox</c> direct type. The XAML property name remains
        /// <c>ColumnSearchBox</c> for binding compatibility with any consumer-authored menu
        /// templates that named the property explicitly.
        /// </summary>
        public IColumnFilterHost ColumnSearchBox { get; set; }

        /// <summary>
        /// The data item for the row (for Cell and Row contexts)
        /// </summary>
        public object RowData { get; set; }

        /// <summary>
        /// The value of the specific cell (for Cell context)
        /// </summary>
        public object CellValue { get; set; }

        /// <summary>
        /// The row index (for Row context)
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Convenience property: returns true if this context has an active filter
        /// </summary>
        public bool HasActiveFilter => ColumnSearchBox?.HasActiveFilter ?? false;

        /// <summary>
        /// Convenience property: returns true if this context has a column
        /// </summary>
        public bool HasColumn => Column != null;

        /// <summary>
        /// Convenience property: returns true if this context has row data
        /// </summary>
        public bool HasRowData => RowData != null;
    }
}
