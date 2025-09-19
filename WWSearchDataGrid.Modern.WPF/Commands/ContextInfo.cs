using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF.Commands
{
    internal class ContextMenuContext
    {
        public ContextMenuType ContextType { get; set; }
        public SearchDataGrid Grid { get; set; }
        public DataGridColumn Column { get; set; }
        public ColumnSearchBox ColumnSearchBox { get; set; }
        public object RowData { get; set; }
        public object CellValue { get; set; }
        public int RowIndex { get; set; }
    }
}
