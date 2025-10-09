using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF.Behaviors
{
    /// <summary>
    /// Attached behaviors for GroupPanel functionality
    /// </summary>
    public static class GroupPanelBehaviors
    {
        /// <summary>
        /// Attached property to enable click-to-sort on grouped column items
        /// </summary>
        public static readonly DependencyProperty EnableClickToSortProperty =
            DependencyProperty.RegisterAttached(
                "EnableClickToSort",
                typeof(bool),
                typeof(GroupPanelBehaviors),
                new PropertyMetadata(false, OnEnableClickToSortChanged));

        public static bool GetEnableClickToSort(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableClickToSortProperty);
        }

        public static void SetEnableClickToSort(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableClickToSortProperty, value);
        }

        private static void OnEnableClickToSortChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseLeftButtonDown += OnGroupedColumnItemClick;
                }
                else
                {
                    element.MouseLeftButtonDown -= OnGroupedColumnItemClick;
                }
            }
        }

        /// <summary>
        /// Handles click on grouped column items to cycle through sort states.
        /// </summary>
        private static void OnGroupedColumnItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not GroupColumnInfo groupInfo)
                return;

            var column = groupInfo.Column;
            if (column == null || !column.CanUserSort || string.IsNullOrEmpty(column.SortMemberPath))
                return;

            // Don't handle if we're not left clicking
            if (e.ChangedButton != MouseButton.Left)
                return;

            // Find the parent GroupPanel
            var groupPanel = FindAncestor<GroupPanel>(element);
            if (groupPanel?.ParentDataGrid == null)
                return;

            var grid = groupPanel.ParentDataGrid;
            if (grid?.Items == null)
                return;

            try
            {
                // Get the collection view
                var collectionView = CollectionViewSource.GetDefaultView(grid.Items);
                if (collectionView == null)
                    return;

                // Determine next sort direction (cycle: None -> Ascending -> Descending -> None)
                ListSortDirection? nextDirection;
                if (column.SortDirection == null)
                {
                    nextDirection = ListSortDirection.Ascending;
                }
                else if (column.SortDirection == ListSortDirection.Ascending)
                {
                    nextDirection = ListSortDirection.Descending;
                }
                else
                {
                    nextDirection = null;
                }

                // Clear existing sort descriptions
                collectionView.SortDescriptions.Clear();

                // Clear all column sort directions
                foreach (var col in grid.Columns)
                {
                    col.SortDirection = null;
                }

                // Apply new sort if not null
                if (nextDirection.HasValue)
                {
                    collectionView.SortDescriptions.Add(new SortDescription(column.SortMemberPath, nextDirection.Value));
                    column.SortDirection = nextDirection.Value;
                }

                // Refresh the view
                collectionView.Refresh();

                e.Handled = true;

                Debug.WriteLine(
                    $"Group panel sort: Column '{groupInfo.HeaderText}' -> {(nextDirection?.ToString() ?? "None")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sorting from group panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds an ancestor of a specific type in the visual tree
        /// </summary>
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T ancestor)
                    return ancestor;

                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
