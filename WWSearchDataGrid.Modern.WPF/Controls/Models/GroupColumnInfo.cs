using System.Windows.Controls;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Represents a column that is being used for grouping in the SearchDataGrid.
    /// </summary>
    public class GroupColumnInfo : ObservableObject
    {
        private DataGridColumn? _column;
        private string _bindingPath = string.Empty;
        private string _headerText = string.Empty;
        private int _groupLevel;

        /// <summary>
        /// Gets or sets the DataGrid column being grouped.
        /// </summary>
        public DataGridColumn? Column
        {
            get => _column;
            set => SetProperty(value, ref _column);
        }

        /// <summary>
        /// Gets or sets the property path used for grouping (from SortMemberPath).
        /// </summary>
        public string BindingPath
        {
            get => _bindingPath;
            set => SetProperty(value, ref _bindingPath);
        }

        /// <summary>
        /// Gets or sets the display text for the group chip.
        /// </summary>
        public string HeaderText
        {
            get => _headerText;
            set => SetProperty(value, ref _headerText);
        }

        /// <summary>
        /// Gets or sets the hierarchical level (0-based, 0 is outermost group).
        /// </summary>
        public int GroupLevel
        {
            get => _groupLevel;
            set => SetProperty(value, ref _groupLevel);
        }
    }
}
