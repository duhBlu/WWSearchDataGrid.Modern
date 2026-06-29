using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Modal Filter Editor — authoring surface for multi-rule, cross-column filter expressions.
    /// Builds an editor-time view-model tree on open from each column's
    /// <see cref="SearchTemplateController.SearchGroups"/>, mutates it in-memory, and writes a
    /// per-column slice back to each controller on Apply.
    /// </summary>
    public class FilterEditorDialog : Control
    {
        #region Dependency Properties

        public static readonly DependencyProperty OwnerGridProperty =
            DependencyProperty.Register(nameof(OwnerGrid), typeof(SearchDataGrid), typeof(FilterEditorDialog),
                new PropertyMetadata(null));

        public static readonly DependencyProperty RootGroupProperty =
            DependencyProperty.Register(nameof(RootGroup), typeof(FilterGroupNode), typeof(FilterEditorDialog),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AvailableColumnsProperty =
            DependencyProperty.Register(nameof(AvailableColumns), typeof(ObservableCollection<GridColumn>), typeof(FilterEditorDialog),
                new PropertyMetadata(null));

        #endregion

        #region Properties

        public SearchDataGrid OwnerGrid
        {
            get => (SearchDataGrid)GetValue(OwnerGridProperty);
            set => SetValue(OwnerGridProperty, value);
        }

        /// <summary>
        /// The root group of the editor-time tree. Built on <see cref="ShowDialog"/> from each
        /// column's controller, mutated in-memory, and written back on Apply.
        /// </summary>
        public FilterGroupNode RootGroup
        {
            get => (FilterGroupNode)GetValue(RootGroupProperty);
            set => SetValue(RootGroupProperty, value);
        }

        /// <summary>
        /// Columns available to any condition node in the tree. The root group consumes this
        /// list; descendant condition nodes resolve it via their parent chain.
        /// </summary>
        public ObservableCollection<GridColumn> AvailableColumns
        {
            get => (ObservableCollection<GridColumn>)GetValue(AvailableColumnsProperty);
            set => SetValue(AvailableColumnsProperty, value);
        }

        #endregion

        #region Commands

        private ICommand _okCommand;
        private ICommand _applyCommand;
        private ICommand _cancelCommand;

        public ICommand OkCommand => _okCommand ??= new RelayCommand(_ => { ExecuteApply(); CloseHostWindow(dialogResult: true); });
        public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(_ => ExecuteApply());
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => CloseHostWindow(dialogResult: false));

        #endregion

        #region Constructor

        public FilterEditorDialog()
        {
            DefaultStyleKey = typeof(FilterEditorDialog);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Opens the Filter Editor as a modal dialog over the given grid. The host window picks
        /// up the library's shared chrome via <see cref="ThemeKeys.PrimitivesWindow"/> —
        /// resolved through <see cref="ComponentResourceKey"/> against the assembly's
        /// <c>Themes/Generic.xaml</c>, matching how <see cref="ColumnChooser"/> styles its host.
        /// </summary>
        public static bool? ShowDialog(SearchDataGrid grid)
        {
            if (grid == null) return null;

            var editor = new FilterEditorDialog
            {
                OwnerGrid = grid,
                AvailableColumns = new ObservableCollection<GridColumn>(grid.GridColumns)
            };
            editor.BuildEditorTree();

            var host = new Window
            {
                Title = "Filter Editor",
                Content = editor,
                Owner = Window.GetWindow(grid),
                Width = 720,
                Height = 520,
                MinWidth = 480,
                MinHeight = 320,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
            };

            // Closing via the chrome's X (SystemCommands.CloseWindow) leaves DialogResult
            // false — the same discard-pending-edits outcome as the footer Cancel button.
            WindowHostHelper.ApplyDefaultChrome(host, editor);

            return host.ShowDialog();
        }

        #endregion

        #region Private helpers

        private void BuildEditorTree()
        {
            var root = FilterEditorTreeBuilder.BuildFromGrid(OwnerGrid);
            root.AvailableColumns = AvailableColumns;

            // Empty state: seed with one condition pre-targeting the first column so the user
            // sees a row immediately instead of an empty surface.
            if (root.Children.Count == 0 && AvailableColumns != null && AvailableColumns.Count > 0)
            {
                var condition = new FilterConditionNode();
                root.Children.Add(condition);
                condition.Column = AvailableColumns[0];
            }

            RootGroup = root;
        }

        /// <summary>
        /// Writes the editor tree back to the per-column controllers and refreshes the grid.
        /// Does not close the host window. Uses the editor-specific filter path so the tree
        /// just written stays as the authoritative predicate source (cross-column groups would
        /// otherwise degrade to per-column AND on the next pass).
        /// </summary>
        private void ExecuteApply()
        {
            if (OwnerGrid == null || RootGroup == null) return;

            FilterEditorTreeBuilder.WriteBackToGrid(RootGroup, OwnerGrid);
            OwnerGrid.FilterItemsSourceFromFilterEditor();
        }

        private void CloseHostWindow(bool dialogResult)
        {
            var host = Window.GetWindow(this);
            if (host == null) return;
            try { host.DialogResult = dialogResult; }
            catch (InvalidOperationException) { /* Non-modal host — DialogResult can't be set; just close. */ }
            host.Close();
        }

        #endregion
    }
}
