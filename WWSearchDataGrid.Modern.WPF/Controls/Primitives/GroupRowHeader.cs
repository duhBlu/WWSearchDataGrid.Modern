using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom control that displays a group header as a merged DataGrid row
    /// </summary>
    public class GroupRowHeader : Control
    {
        static GroupRowHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupRowHeader),
                new FrameworkPropertyMetadata(typeof(GroupRowHeader)));
        }

        public GroupRowHeader()
        {
            MouseDoubleClick += OnMouseDoubleClick;
            MouseLeftButtonDown += OnMouseLeftButtonDown;

            // Subscribe to events to auto-populate properties
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
        }

        #region Dependency Properties

        public static readonly DependencyProperty GroupProperty =
            DependencyProperty.Register(nameof(Group), typeof(CollectionViewGroup), typeof(GroupRowHeader),
                new PropertyMetadata(null, OnGroupChanged));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(GroupRowHeader),
                new PropertyMetadata(false));

        public static readonly DependencyProperty GroupLevelProperty =
            DependencyProperty.Register(nameof(GroupLevel), typeof(int), typeof(GroupRowHeader),
                new PropertyMetadata(0));

        public static readonly DependencyProperty ColumnNameProperty =
            DependencyProperty.Register(nameof(ColumnName), typeof(string), typeof(GroupRowHeader),
                new PropertyMetadata(string.Empty));

        public CollectionViewGroup Group
        {
            get => (CollectionViewGroup)GetValue(GroupProperty);
            set => SetValue(GroupProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public int GroupLevel
        {
            get => (int)GetValue(GroupLevelProperty);
            set => SetValue(GroupLevelProperty, value);
        }

        public string ColumnName
        {
            get => (string)GetValue(ColumnNameProperty);
            set => SetValue(ColumnNameProperty, value);
        }

        #endregion

        #region Methods

        private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GroupRowHeader header && e.NewValue is CollectionViewGroup group)
            {
                // Auto-populate Group property when it changes
                header.UpdateFromDataContext();
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // DataContext changed - try to update if we're in the visual tree
            if (IsLoaded)
            {
                UpdateFromDataContext();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Control is now in the visual tree - safe to update
            UpdateFromDataContext();
        }

        /// <summary>
        /// Updates GroupLevel and ColumnName from the DataContext (CollectionViewGroup)
        /// </summary>
        private void UpdateFromDataContext()
        {
            if (DataContext is CollectionViewGroup group)
            {
                Group = group;

                // Find the parent DataGrid to get column information
                var dataGrid = FindVisualParent<SearchDataGrid>(this);
                if (dataGrid != null)
                {
                    // Calculate group level
                    int level = CalculateGroupLevel(group, dataGrid);
                    GroupLevel = level;

                    // Get column name
                    string columnName = GetColumnName(group, dataGrid, level);
                    ColumnName = columnName;

                }
            }
        }

        /// <summary>
        /// Calculates the hierarchical level of this group
        /// </summary>
        private int CalculateGroupLevel(CollectionViewGroup group, SearchDataGrid dataGrid)
        {
            try
            {
                var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if (view?.Groups != null)
                {
                    int level = CalculateGroupLevelRecursive(group, view.Groups, 0);
                    // If not found (-1), default to 0
                    return level >= 0 ? level : 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating group level: {ex.Message}");
            }
            return 0;
        }

        private int CalculateGroupLevelRecursive(CollectionViewGroup targetGroup,
            System.Collections.ObjectModel.ReadOnlyObservableCollection<object> groups,
            int currentLevel)
        {
            // Check if target group is at this level
            if (groups.Contains(targetGroup))
                return currentLevel;

            // Check nested groups
            foreach (var item in groups)
            {
                if (item is CollectionViewGroup parentGroup && parentGroup.Items != null && parentGroup.Items.Count > 0)
                {
                    // Check if first item is a group (indicating nested grouping)
                    if (parentGroup.Items[0] is CollectionViewGroup)
                    {
                        // Collect all subgroups
                        var subGroups = new System.Collections.ObjectModel.ObservableCollection<object>();
                        foreach (var subItem in parentGroup.Items)
                        {
                            if (subItem is CollectionViewGroup subGroup)
                                subGroups.Add(subGroup);
                        }

                        if (subGroups.Count > 0)
                        {
                            var readOnlySubGroups = new System.Collections.ObjectModel.ReadOnlyObservableCollection<object>(subGroups);
                            int level = CalculateGroupLevelRecursive(targetGroup, readOnlySubGroups, currentLevel + 1);
                            // If we found it in subgroups (level != -1), return that level
                            if (level != -1)
                                return level;
                        }
                    }
                }
            }

            // Not found at this level or in subgroups
            return -1;
        }

        /// <summary>
        /// Gets the column name for this group
        /// </summary>
        private string GetColumnName(CollectionViewGroup group, SearchDataGrid dataGrid, int groupLevel)
        {
            try
            {
                var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if (view?.GroupDescriptions != null)
                {
                    if (groupLevel >= 0 && groupLevel < view.GroupDescriptions.Count)
                    {
                        var groupDesc = view.GroupDescriptions[groupLevel] as PropertyGroupDescription;
                        if (groupDesc != null)
                        {
                            var column = dataGrid.Columns.FirstOrDefault(c => GetColumnBindingPath(c) == groupDesc.PropertyName);
                            if (column != null)
                            {
                                return SearchDataGrid.ExtractColumnHeaderText(column) ?? groupDesc.PropertyName;
                            }
                            return groupDesc.PropertyName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting column name: {ex.Message}");
            }
            return string.Empty;
        }

        private string GetColumnBindingPath(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding)
                {
                    return binding.Path.Path;
                }
            }
            return null;
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null)
                return null;

            if (parent is T typedParent)
                return typedParent;

            return FindVisualParent<T>(parent);
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Single click behavior can be added here if needed
        }

        #endregion
    }
}
