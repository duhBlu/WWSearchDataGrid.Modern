using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Behaviors
{
    /// <summary>
    /// Attached behavior that manages column name assignment to GroupRowHeader controls
    /// </summary>
    public static class GroupHeaderBehavior
    {
        /// <summary>
        /// Maps binding paths to column header names for the DataGrid
        /// </summary>
        private static readonly Dictionary<DataGrid, Dictionary<string, string>> ColumnNameCache =
            new Dictionary<DataGrid, Dictionary<string, string>>();

        /// <summary>
        /// Attached property to enable group header column name tracking
        /// </summary>
        public static readonly DependencyProperty TrackColumnNamesProperty =
            DependencyProperty.RegisterAttached(
                "TrackColumnNames",
                typeof(bool),
                typeof(GroupHeaderBehavior),
                new PropertyMetadata(false, OnTrackColumnNamesChanged));

        public static bool GetTrackColumnNames(DependencyObject obj)
        {
            return (bool)obj.GetValue(TrackColumnNamesProperty);
        }

        public static void SetTrackColumnNames(DependencyObject obj, bool value)
        {
            obj.SetValue(TrackColumnNamesProperty, value);
        }

        private static void OnTrackColumnNamesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid)
            {
                if ((bool)e.NewValue)
                {
                    dataGrid.Loaded += OnDataGridLoaded;
                }
                else
                {
                    dataGrid.Loaded -= OnDataGridLoaded;
                    if (ColumnNameCache.ContainsKey(dataGrid))
                    {
                        ColumnNameCache.Remove(dataGrid);
                    }
                }
            }
        }

        private static void OnDataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                BuildColumnNameCache(dataGrid);
                UpdateGroupHeaders(dataGrid);
            }
        }

        /// <summary>
        /// Builds a cache of binding path -> column header name mappings
        /// </summary>
        private static void BuildColumnNameCache(DataGrid dataGrid)
        {
            var cache = new Dictionary<string, string>();

            foreach (var column in dataGrid.Columns)
            {
                string bindingPath = GetColumnBindingPath(column);
                if (!string.IsNullOrEmpty(bindingPath))
                {
                    string headerText = ExtractColumnHeaderText(column);
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        cache[bindingPath] = headerText;
                    }
                }
            }

            ColumnNameCache[dataGrid] = cache;
        }

        /// <summary>
        /// Gets the binding path for a DataGrid column
        /// </summary>
        private static string GetColumnBindingPath(DataGridColumn column)
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

        /// <summary>
        /// Extracts the text content from a column header
        /// </summary>
        private static string ExtractColumnHeaderText(DataGridColumn column)
        {
            if (column?.Header == null)
                return null;

            if (column.Header is string headerString)
                return headerString;

            // For complex headers, try to extract text from visual tree
            if (column.Header is FrameworkElement element)
            {
                return SearchDataGrid.ExtractTextFromVisualTree(element);
            }

            return column.Header.ToString();
        }

        /// <summary>
        /// Updates all GroupRowHeader controls with their corresponding column names
        /// </summary>
        private static void UpdateGroupHeaders(DataGrid dataGrid)
        {
            if (!ColumnNameCache.TryGetValue(dataGrid, out var cache))
                return;

            var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            if (view?.GroupDescriptions == null || view.GroupDescriptions.Count == 0)
                return;

            // Get the column names in order of grouping
            var groupedColumnNames = view.GroupDescriptions
                .OfType<PropertyGroupDescription>()
                .Select((desc, index) => new { Path = desc.PropertyName, Level = index, Name = cache.GetValueOrDefault(desc.PropertyName, desc.PropertyName) })
                .ToList();

            // Store in attached property for access by GroupRowHeader
            dataGrid.SetValue(GroupedColumnNamesProperty, groupedColumnNames);
        }

        /// <summary>
        /// Attached property to store grouped column names for the DataGrid
        /// </summary>
        private static readonly DependencyProperty GroupedColumnNamesProperty =
            DependencyProperty.RegisterAttached(
                "GroupedColumnNames",
                typeof(object),
                typeof(GroupHeaderBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets the column name for a specific property path
        /// </summary>
        public static string GetColumnNameForPath(DataGrid dataGrid, string path)
        {
            if (ColumnNameCache.TryGetValue(dataGrid, out var cache))
            {
                return cache.GetValueOrDefault(path, path);
            }
            return path;
        }
    }
}
