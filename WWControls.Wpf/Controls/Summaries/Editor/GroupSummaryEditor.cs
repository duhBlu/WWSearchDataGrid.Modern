using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// The "View Totals" dialog body — the group-summary editor. Items tab: per-column
    /// Max / Min / Average / Sum toggles plus "Show row count". Order and Alignment tab:
    /// the configured entries in their Left / Right side lists with reorder / re-side arrows
    /// and per-entry Prefix / Display format / Suffix. Edits a
    /// <see cref="GroupSummaryEditorViewModel"/> working copy; OK applies, Cancel discards.
    /// Templated in the theme (<see cref="ThemeKeys.GroupSummaryEditor"/>); hosted in a window
    /// styled by the shared <see cref="ThemeKeys.PrimitivesWindow"/> chrome, same as the
    /// Filter Editor and Column Chooser.
    /// </summary>
    public class GroupSummaryEditor : Control
    {
        public GroupSummaryEditor()
        {
            DefaultStyleKey = typeof(GroupSummaryEditor);
        }

        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(nameof(OwnerGrid), typeof(SearchDataGrid), typeof(GroupSummaryEditor),
                new PropertyMetadata(null));

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }

        internal GroupSummaryEditorViewModel ViewModel { get; private set; }

        #region Commands

        private ICommand _okCommand;
        private ICommand _cancelCommand;

        public ICommand OkCommand => _okCommand ??= new RelayCommand(_ =>
        {
            ViewModel?.Apply();
            CloseHostWindow(dialogResult: true);
        });

        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => CloseHostWindow(dialogResult: false));

        #endregion

        /// <summary>
        /// Opens the editor for the grid's group-header summaries — one shared set rendered at
        /// every group level. Returns the dialog result (true = applied).
        /// </summary>
        public static bool? ShowGroupDialog(SearchDataGrid grid)
        {
            if (grid?.GridColumns == null) return null;
            return ShowCore(grid, SummaryEditorMode.GroupHeaders, null, "View Totals");
        }

        /// <summary>
        /// Opens the editor for one column's totals cell ("Totals for 'X'") — that column's
        /// <see cref="GridColumn.TotalSummaries"/>, where entries may target other columns'
        /// fields and all render under this column. Returns the dialog result (true = applied).
        /// </summary>
        public static bool? ShowColumnTotalsDialog(SearchDataGrid grid, GridColumn column)
        {
            if (grid?.GridColumns == null || column == null) return null;

            string name = column.FieldName;
            if (string.IsNullOrEmpty(name)) name = column.HeaderCaption;
            string title = string.IsNullOrEmpty(name) ? "Totals" : $"Totals for '{name}'";
            return ShowCore(grid, SummaryEditorMode.ColumnTotals, column, title);
        }

        /// <summary>
        /// Opens the editor for the fixed total summary panel's own definition set
        /// (<see cref="SearchDataGrid.FixedTotalSummaries"/>). Returns the dialog result
        /// (true = applied).
        /// </summary>
        public static bool? ShowFixedTotalsDialog(SearchDataGrid grid)
        {
            if (grid?.GridColumns == null) return null;
            return ShowCore(grid, SummaryEditorMode.FixedTotals, null, "Customize Fixed Totals");
        }

        /// <summary>
        /// Opens the editor for one column's group-footer summaries ("Footer for 'X'") — that
        /// column's <see cref="GridColumn.GroupFooterSummaries"/>, computed per group and rendered
        /// in that column's cell of every group footer row. Returns the dialog result (true =
        /// applied).
        /// </summary>
        public static bool? ShowGroupFooterDialog(SearchDataGrid grid, GridColumn column)
        {
            if (grid?.GridColumns == null || column == null) return null;

            string name = column.FieldName;
            if (string.IsNullOrEmpty(name)) name = column.HeaderCaption;
            string title = string.IsNullOrEmpty(name) ? "Group Footer" : $"Footer for '{name}'";
            return ShowCore(grid, SummaryEditorMode.GroupFooterTotals, column, title);
        }

        private static bool? ShowCore(SearchDataGrid grid, SummaryEditorMode mode, GridColumn ownerColumn, string title)
        {
            var editor = new GroupSummaryEditor { OwnerGrid = grid };
            editor.ViewModel = new GroupSummaryEditorViewModel(grid, mode, ownerColumn);
            editor.DataContext = editor.ViewModel;

            var host = new Window
            {
                Title = title,
                Content = editor,
                Owner = Window.GetWindow(grid),
                Width = 470,
                Height = 440,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
            };

            // Same chrome as the Filter Editor's host — resolved by ComponentResourceKey
            // through the assembly's Themes/Generic.xaml, no manual merge needed. Closing via
            // the chrome's X leaves DialogResult false, the same outcome as Cancel.
            WindowHostHelper.ApplyDefaultChrome(host, editor);

            return host.ShowDialog();
        }

        private void CloseHostWindow(bool dialogResult)
        {
            var host = Window.GetWindow(this);
            if (host == null) return;
            try { host.DialogResult = dialogResult; }
            catch (System.InvalidOperationException)
            {
                // Host wasn't opened via ShowDialog (non-modal embedding) — just close.
                host.Close();
            }
        }
    }
}
