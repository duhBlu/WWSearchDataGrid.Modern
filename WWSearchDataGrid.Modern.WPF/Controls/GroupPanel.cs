using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom control that displays grouped columns and manages grouping state for SearchDataGrid.
    /// </summary>
    public class GroupPanel : Control
    {
        private ICommand? _removeGroupCommand;

        static GroupPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupPanel), new FrameworkPropertyMetadata(typeof(GroupPanel)));
        }

        /// <summary>
        /// Initializes a new instance of the GroupPanel class.
        /// </summary>
        public GroupPanel()
        {
            GroupedColumns = new ObservableCollection<GroupColumnInfo>();
            GroupedColumns.CollectionChanged += (s, e) => OnGroupingChanged();
            AllowDrop = true;
        }

        #region Dependency Properties

        /// <summary>
        /// Dependency property for GroupedColumns.
        /// </summary>
        public static readonly DependencyProperty GroupedColumnsProperty =
            DependencyProperty.Register(
                nameof(GroupedColumns),
                typeof(ObservableCollection<GroupColumnInfo>),
                typeof(GroupPanel),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the list of currently grouped columns in hierarchical order.
        /// </summary>
        public ObservableCollection<GroupColumnInfo> GroupedColumns
        {
            get => (ObservableCollection<GroupColumnInfo>)GetValue(GroupedColumnsProperty);
            set => SetValue(GroupedColumnsProperty, value);
        }

        /// <summary>
        /// Dependency property for IsPanelVisible.
        /// </summary>
        public static readonly DependencyProperty IsPanelVisibleProperty =
            DependencyProperty.Register(
                nameof(IsPanelVisible),
                typeof(bool),
                typeof(GroupPanel),
                new PropertyMetadata(true, OnIsPanelVisibleChanged));

        /// <summary>
        /// Gets or sets whether the panel is shown.
        /// </summary>
        public bool IsPanelVisible
        {
            get => (bool)GetValue(IsPanelVisibleProperty);
            set => SetValue(IsPanelVisibleProperty, value);
        }

        private static void OnIsPanelVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GroupPanel panel)
            {
                panel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Dependency property for ParentDataGrid.
        /// </summary>
        public static readonly DependencyProperty ParentDataGridProperty =
            DependencyProperty.Register(
                nameof(ParentDataGrid),
                typeof(SearchDataGrid),
                typeof(GroupPanel),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the reference to the SearchDataGrid instance.
        /// </summary>
        public SearchDataGrid? ParentDataGrid
        {
            get => (SearchDataGrid?)GetValue(ParentDataGridProperty);
            set => SetValue(ParentDataGridProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when columns are added, removed, or reordered.
        /// </summary>
        public event EventHandler? GroupingChanged;

        /// <summary>
        /// Fired when expand/collapse all is requested.
        /// </summary>
        public event EventHandler<bool>? ExpandCollapseAllRequested;

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to remove a specific column from grouping.
        /// </summary>
        public ICommand RemoveGroupCommand
        {
            get
            {
                _removeGroupCommand ??= new RelayCommand<GroupColumnInfo>(
                    groupInfo => RemoveGrouping(groupInfo),
                    groupInfo => groupInfo != null);
                return _removeGroupCommand;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles dropping a column header to add grouping.
        /// </summary>
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(typeof(DataGridColumn)))
            {
                var column = e.Data.GetData(typeof(DataGridColumn)) as DataGridColumn;
                if (column != null && !string.IsNullOrEmpty(column.SortMemberPath))
                {
                    // Check if already grouped
                    var existing = GroupedColumns.FirstOrDefault(g => g.Column == column);
                    if (existing == null)
                    {
                        var groupInfo = new GroupColumnInfo
                        {
                            Column = column,
                            BindingPath = column.SortMemberPath,
                            HeaderText = column.Header?.ToString() ?? "Unknown",
                            GroupLevel = GroupedColumns.Count
                        };
                        GroupedColumns.Add(groupInfo);
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles drag over event to show drop is allowed.
        /// </summary>
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            if (e.Data.GetDataPresent(typeof(DataGridColumn)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Removes a specific column from grouping.
        /// </summary>
        /// <param name="column">The column to remove.</param>
        public void RemoveGrouping(GroupColumnInfo? column)
        {
            if (column != null && GroupedColumns.Contains(column))
            {
                GroupedColumns.Remove(column);

                // Update group levels for remaining columns
                for (int i = 0; i < GroupedColumns.Count; i++)
                {
                    GroupedColumns[i].GroupLevel = i;
                }
            }
        }

        /// <summary>
        /// Removes all grouping and resets to ungrouped view.
        /// </summary>
        public void ClearAllGrouping()
        {
            GroupedColumns.Clear();
        }

        /// <summary>
        /// Changes the hierarchical order of grouped columns.
        /// </summary>
        /// <param name="oldIndex">The current index.</param>
        /// <param name="newIndex">The new index.</param>
        public void ReorderGrouping(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= GroupedColumns.Count ||
                newIndex < 0 || newIndex >= GroupedColumns.Count)
            {
                return;
            }

            var item = GroupedColumns[oldIndex];
            GroupedColumns.RemoveAt(oldIndex);
            GroupedColumns.Insert(newIndex, item);

            // Update group levels
            for (int i = 0; i < GroupedColumns.Count; i++)
            {
                GroupedColumns[i].GroupLevel = i;
            }
        }

        /// <summary>
        /// Expands all collapsible group sections.
        /// </summary>
        public void ExpandAllGroups()
        {
            ExpandCollapseAllRequested?.Invoke(this, true);
        }

        /// <summary>
        /// Collapses all group sections.
        /// </summary>
        public void CollapseAllGroups()
        {
            ExpandCollapseAllRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Raises the GroupingChanged event.
        /// </summary>
        private void OnGroupingChanged()
        {
            GroupingChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
