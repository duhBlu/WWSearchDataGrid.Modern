using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// DataGridFilterEditor provides a tabbed interface for editing column filters
    /// </summary>
    public class DataGridFilterEditor : Control
    {
        #region Dependency Properties

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register("SourceDataGrid", typeof(SearchDataGrid), typeof(DataGridFilterEditor),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty FilteredColumnsProperty =
            DependencyProperty.Register("FilteredColumns", typeof(ObservableCollection<FilteredColumnInfo>), typeof(DataGridFilterEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AllFilterGroupsProperty =
            DependencyProperty.Register("AllFilterGroups", typeof(ObservableCollection<FilterGroupInfo>), typeof(DataGridFilterEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DialogAcceptedProperty =
            DependencyProperty.Register("DialogAccepted", typeof(bool), typeof(DataGridFilterEditor),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(DataGridFilterEditor),
                new PropertyMetadata(false));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the source data grid
        /// </summary>
        public SearchDataGrid SourceDataGrid
        {
            get => (SearchDataGrid)GetValue(SourceDataGridProperty);
            set => SetValue(SourceDataGridProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of filtered columns
        /// </summary>
        public ObservableCollection<FilteredColumnInfo> FilteredColumns
        {
            get => (ObservableCollection<FilteredColumnInfo>)GetValue(FilteredColumnsProperty);
            set => SetValue(FilteredColumnsProperty, value);
        }

        /// <summary>
        /// Gets or sets the unified collection of all filter groups from all columns
        /// </summary>
        public ObservableCollection<FilterGroupInfo> AllFilterGroups
        {
            get => (ObservableCollection<FilterGroupInfo>)GetValue(AllFilterGroupsProperty);
            set => SetValue(AllFilterGroupsProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the dialog was accepted (Apply was clicked)
        /// </summary>
        public bool DialogAccepted
        {
            get => (bool)GetValue(DialogAcceptedProperty);
            set => SetValue(DialogAcceptedProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the dialog is currently loading data
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// Gets the collection of available columns that can be added to the filter
        /// </summary>
        public ObservableCollection<ColumnDisplayInfo> AvailableColumns { get; private set; }

        #endregion

        #region Commands

        /// <summary>
        /// Apply filter command
        /// </summary>
        public ICommand ApplyCommand => new RelayCommand(_ => ExecuteApply());

        /// <summary>
        /// Cancel command
        /// </summary>
        public ICommand CancelCommand => new RelayCommand(_ => ExecuteCancel());

        /// <summary>
        /// Add search template command
        /// </summary>
        public ICommand AddSearchTemplateCommand => new RelayCommand<SearchTemplate>(template => ExecuteAddSearchTemplate(template));

        /// <summary>
        /// Remove search template command
        /// </summary>
        public ICommand RemoveSearchTemplateCommand => new RelayCommand<SearchTemplate>(template => ExecuteRemoveSearchTemplate(template));

        /// <summary>
        /// Add column command
        /// </summary>
        public ICommand AddColumnCommand => new RelayCommand<ColumnDisplayInfo>(
            columnInfo => ExecuteAddColumn(columnInfo?.Column),
            columnInfo => columnInfo?.Column != null && !FilteredColumns.Any(fc => fc.BindingPath == columnInfo.Column.SortMemberPath));

        /// <summary>
        /// Remove column command
        /// </summary>
        public ICommand RemoveColumnCommand => new RelayCommand<FilteredColumnInfo>(
            columnInfo => ExecuteRemoveColumn(columnInfo),
            columnInfo => columnInfo != null && FilteredColumns.Count > 1);

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<DialogClosingEventArgs> DialogClosing;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the DataGridFilterEditor class
        /// </summary>
        public DataGridFilterEditor()
        {
            DefaultStyleKey = typeof(DataGridFilterEditor);
            FilteredColumns = new ObservableCollection<FilteredColumnInfo>();
            AllFilterGroups = new ObservableCollection<FilterGroupInfo>();
            AvailableColumns = new ObservableCollection<ColumnDisplayInfo>();
        }

        #endregion

        #region Control Template Methods

        /// <summary>
        /// When the template is applied
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Initialize the dialog if we have a data grid
            if (SourceDataGrid != null)
            {
                InitializeFilteredColumns();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles changes to the SourceDataGrid property
        /// </summary>
        private static void OnSourceDataGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGridFilterEditor dialog && e.NewValue is SearchDataGrid)
            {
                dialog.InitializeFilteredColumns();
            }
        }

        /// <summary>
        /// Initializes the filtered columns collection
        /// </summary>
        private void InitializeFilteredColumns()
        {
            if (SourceDataGrid == null)
                return;

            try
            {
                IsLoading = true;
                FilteredColumns.Clear();
                AllFilterGroups.Clear();

                // Get all columns that have active filters
                var filteredColumns = SourceDataGrid.DataColumns
                    .Where(c => c.HasActiveFilter)
                    .ToList();

                foreach (var column in filteredColumns)
                {
                    var columnInfo = new FilteredColumnInfo
                    {
                        ColumnName = column.CurrentColumn?.Header?.ToString() ?? "Unknown Column",
                        BindingPath = column.BindingPath ?? "Unknown",
                        OriginalController = column.SearchTemplateController,
                        WorkingController = CloneSearchTemplateController(column.SearchTemplateController),
                        ColumnSearchBox = column
                    };

                    FilteredColumns.Add(columnInfo);

                    // Add all search groups from this column to the unified collection
                    foreach (var group in columnInfo.WorkingController.SearchGroups)
                    {
                        var filterGroupInfo = new FilterGroupInfo
                        {
                            SearchTemplateGroup = group,
                            ColumnInfo = columnInfo,
                            DisplayName = columnInfo.ColumnName
                        };

                        AllFilterGroups.Add(filterGroupInfo);
                    }
                }

                // Update operator visibility based on unified group ordering
                UpdateUnifiedOperatorVisibility();
                
                // Refresh available columns list
                RefreshAvailableColumns();

                IsLoading = false;

                // Load available values asynchronously to prevent UI freezing
                _ = LoadAvailableValuesAsync();
            }
            catch (Exception ex)
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine($"Error initializing filtered columns: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads available values asynchronously to prevent UI blocking
        /// </summary>
        private async System.Threading.Tasks.Task LoadAvailableValuesAsync()
        {
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // Process each column's available values on background thread
                    foreach (var columnInfo in FilteredColumns)
                    {
                        // Ensure templates have their available values loaded
                        foreach (var group in columnInfo.WorkingController.SearchGroups)
                        {
                            foreach (var template in group.SearchTemplates)
                            {
                                if (template.AvailableValues?.Count() == 0)
                                {
                                    // Templates will be updated by the controller's provider
                                    // No need to manually load values
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available values: {ex.Message}");
            }
        }

        /// <summary>
        /// Clones a SearchTemplateController for editing
        /// </summary>
        private static SearchTemplateController CloneSearchTemplateController(SearchTemplateController original)
        {
            try
            {
                var clone = new SearchTemplateController(original.SearchTemplateType)
                {
                    ColumnName = original.ColumnName,
                    ColumnDataType = original.ColumnDataType,
                    HasCustomExpression = original.HasCustomExpression,
                    AllowMultipleGroups = true,  // Enable multiple groups for DataGridFilterEditor
                    ColumnValues = original.ColumnValues,
                    ColumnValuesByPath = original.ColumnValuesByPath,
                    PropertyValues = original.PropertyValues,
                    DefaultSearchType = original.DefaultSearchType,
                };

                // Clone search groups
                foreach (var originalGroup in original.SearchGroups)
                {
                    var clonedGroup = new SearchTemplateGroup
                    {
                        GroupNumber = originalGroup.GroupNumber,
                        OperatorName = originalGroup.OperatorName,
                        IsOperatorVisible = originalGroup.IsOperatorVisible
                    };

                    // Clone search templates within the group
                    foreach (var originalTemplate in originalGroup.SearchTemplates)
                    {
                        var clonedTemplate = CloneSearchTemplate(originalTemplate, clone.ColumnValues, clone.ColumnDataType);
                        clonedGroup.SearchTemplates.Add(clonedTemplate);
                    }

                    clone.SearchGroups.Add(clonedGroup);
                }

                return clone;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cloning SearchTemplateController: {ex.Message}");
                return new SearchTemplateController(original.SearchTemplateType);
            }
        }

        /// <summary>
        /// Clones a search template
        /// </summary>
        private static SearchTemplate CloneSearchTemplate(SearchTemplate original, HashSet<object> columnValues, ColumnDataType dataType)
        {
            var clone = new SearchTemplate(dataType)
            {
                SearchType = original.SearchType,
                SelectedValue = original.SelectedValue,
                SelectedSecondaryValue = original.SelectedSecondaryValue,
                OperatorName = original.OperatorName,
                IsOperatorVisible = original.IsOperatorVisible,
                HasChanges = original.HasChanges
            };

            // Copy selected values if any
            if (original.SelectedValues?.Any() == true)
            {
                clone.SelectedValues.Clear();
                foreach (var value in original.SelectedValues)
                {
                    clone.SelectedValues.Add(value);
                }
            }

            // Copy SelectedDates collection
            if (original.SelectedDates?.Any() == true)
            {
                clone.SelectedDates.Clear();
                foreach (DateTime date in original.SelectedDates)
                {
                    clone.SelectedDates.Add(date);
                }
            }

            // Copy DateIntervals collection with IsSelected states
            if (original.DateIntervals?.Any() == true)
            {
                clone.DateIntervals.Clear();
                foreach (DateIntervalItem originalItem in original.DateIntervals)
                {
                    var clonedItem = new DateIntervalItem
                    {
                        Interval = originalItem.Interval,
                        DisplayName = originalItem.DisplayName,
                        IsSelected = originalItem.IsSelected
                    };
                    clone.DateIntervals.Add(clonedItem);
                }
            }

            // Available values will be loaded by the provider when needed
            // No need to manually load values during cloning

            return clone;
        }

        /// <summary>
        /// Executes the apply command
        /// </summary>
        private void ExecuteApply()
        {
            try
            {
                ApplyChanges();
                DialogAccepted = true;
                DialogClosing?.Invoke(this, new DialogClosingEventArgs(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying filter changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes the cancel command
        /// </summary>
        private void ExecuteCancel()
        {
            DialogAccepted = false;
            DialogClosing?.Invoke(this, new DialogClosingEventArgs(false));
        }

        /// <summary>
        /// Applies the changes from working controllers back to original controllers
        /// </summary>
        private void ApplyChanges()
        {
            try
            {
                foreach (var columnInfo in FilteredColumns)
                {
                    if (columnInfo.OriginalController != null && columnInfo.WorkingController != null)
                    {
                        // Copy changes back to original
                        CopyControllerChanges(columnInfo.WorkingController, columnInfo.OriginalController);
                    }
                }

                // Synchronize ColumnSearchBox states after applying changes
                foreach (var columnInfo in FilteredColumns)
                {
                    SynchronizeColumnSearchBoxState(columnInfo);
                }

                // Apply filters to the data grid
                SourceDataGrid?.FilterItemsSource();
                SourceDataGrid?.UpdateFilterPanel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronizes ColumnSearchBox state with the applied filter changes
        /// </summary>
        private static void SynchronizeColumnSearchBoxState(FilteredColumnInfo columnInfo)
        {
            try
            {
                if (columnInfo?.ColumnSearchBox == null || columnInfo.OriginalController == null)
                    return;

                var columnSearchBox = columnInfo.ColumnSearchBox;
                var controller = columnInfo.OriginalController;

                // Force recalculation of the filter expression to ensure HasCustomExpression is accurate
                controller.UpdateFilterExpression();
                
                // Now check if we have advanced filters (custom expressions) - this will be accurate
                bool hasAdvancedFilter = controller.HasCustomExpression;

                // Set HasAdvancedFilter state but don't clear search text
                // This allows incremental search to work alongside advanced filters
                columnSearchBox.HasAdvancedFilter = hasAdvancedFilter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error synchronizing ColumnSearchBox state: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies changes from working controller to original controller
        /// </summary>
        private static void CopyControllerChanges(SearchTemplateController source, SearchTemplateController target)
        {
            try
            {
                // Clear target groups and copy from source
                target.SearchGroups.Clear();

                foreach (var sourceGroup in source.SearchGroups)
                {
                    var targetGroup = new SearchTemplateGroup
                    {
                        GroupNumber = sourceGroup.GroupNumber,
                        OperatorName = sourceGroup.OperatorName,
                        IsOperatorVisible = sourceGroup.IsOperatorVisible
                    };

                    foreach (var sourceTemplate in sourceGroup.SearchTemplates)
                    {
                        var targetTemplate = CloneSearchTemplate(sourceTemplate, target.ColumnValues, target.ColumnDataType);
                        targetGroup.SearchTemplates.Add(targetTemplate);
                    }

                    target.SearchGroups.Add(targetGroup);
                }

                // Update the filter expression - this will recalculate HasCustomExpression properly
                target.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying controller changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes add search template command
        /// </summary>
        private void ExecuteAddSearchTemplate(SearchTemplate referenceTemplate)
        {
            try
            {
                // Find the column info that contains this template
                var filterGroupInfo = AllFilterGroups.FirstOrDefault(fg => 
                    fg.SearchTemplateGroup.SearchTemplates.Contains(referenceTemplate));
                if (filterGroupInfo?.ColumnInfo?.WorkingController != null)
                {
                    filterGroupInfo.ColumnInfo.WorkingController.AddSearchTemplate(true, referenceTemplate);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding search template: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes remove search template command
        /// </summary>
        private void ExecuteRemoveSearchTemplate(SearchTemplate template)
        {
            try
            {
                // Find the column info that contains this template
                var filterGroupInfo = AllFilterGroups.FirstOrDefault(fg => 
                    fg.SearchTemplateGroup.SearchTemplates.Contains(template));
                if (filterGroupInfo?.ColumnInfo?.WorkingController != null)
                {
                    filterGroupInfo.ColumnInfo.WorkingController.RemoveSearchTemplate(template);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing search template: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates operator visibility based on unified AllFilterGroups ordering
        /// </summary>
        private void UpdateUnifiedOperatorVisibility()
        {
            try
            {
                // Update group-level operator visibility based on unified ordering
                for (int i = 0; i < AllFilterGroups.Count; i++)
                {
                    var group = AllFilterGroups[i].SearchTemplateGroup;
                    
                    // First group in the unified view should have operator hidden, subsequent groups should show it
                    group.IsOperatorVisible = i > 0;
                    
                    // Update template-level operator visibility within each group
                    for (int j = 0; j < group.SearchTemplates.Count; j++)
                    {
                        // First template in each group should have operator hidden, subsequent templates should show it
                        group.SearchTemplates[j].IsOperatorVisible = j > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating unified operator visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the available columns list based on current filtered columns
        /// </summary>
        private void RefreshAvailableColumns()
        {
            try
            {
                if (SourceDataGrid == null)
                {
                    AvailableColumns.Clear();
                    return;
                }

                // Get all columns that are not currently filtered
                var filteredBindingPaths = new HashSet<string>(FilteredColumns.Select(fc => fc.BindingPath));
                var availableColumns = SourceDataGrid.Columns
                    .Where(c => !string.IsNullOrEmpty(c.SortMemberPath) && 
                    !filteredBindingPaths.Contains(c.SortMemberPath) &&
                    ShouldIncludeInAdvancedFilter(c))
                    .ToList();

                AvailableColumns.Clear();
                foreach (var column in availableColumns)
                {
                    var displayInfo = new ColumnDisplayInfo
                    {
                        Column = column,
                        DisplayName = GetColumnDisplayName(column)
                    };
                    AvailableColumns.Add(displayInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing available columns: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines whether a column should be included in the advanced filter
        /// </summary>
        /// <param name="column">The column to check</param>
        /// <returns>True if the column should be included, false otherwise</returns>
        private bool ShouldIncludeInAdvancedFilter(DataGridColumn column)
        {
            try
            {
                // Check if the column has the AllowRuleValueFiltering attached property set to false
                bool showInAdvancedFilter = ColumnSearchBox.GetAllowRuleValueFiltering(column);
                return showInAdvancedFilter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking AllowRuleValueFiltering for column {column?.Header}: {ex.Message}");
                // Default to true if we can't determine the property value
                return true;
            }
        }

        /// <summary>
        /// Gets a display-friendly name for a column, handling complex headers
        /// </summary>
        private static string GetColumnDisplayName(DataGridColumn column)
        {
            try
            {
                if (column?.Header == null)
                    return "Unknown Column";

                // If header is a string, use it directly
                if (column.Header is string headerText)
                    return headerText;

                // If header is a FrameworkElement, try to extract text content
                if (column.Header is FrameworkElement element)
                {
                    // Try common text-containing controls
                    if (element is TextBlock textBlock)
                        return textBlock.Text ?? "Text Block";
                    if (element is Label label)
                        return label.Content?.ToString() ?? "Label";
                    if (element is Button button)
                        return button.Content?.ToString() ?? "Button";
                    
                    // For other controls, return the type name
                    return element.GetType().Name;
                }

                // For any other object, use its string representation
                return column.Header.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown Column";
            }
        }

        /// <summary>
        /// Executes the add column command
        /// </summary>
        private void ExecuteAddColumn(DataGridColumn column)
        {
            try
            {
                if (column == null || string.IsNullOrEmpty(column.SortMemberPath))
                    return;

                // Find the corresponding ColumnSearchBox - first check in DataColumns collection
                var columnSearchBox = SourceDataGrid.DataColumns.FirstOrDefault(dc => dc.BindingPath == column.SortMemberPath);
                
                if (columnSearchBox == null)
                {
                    // Create a new ColumnSearchBox for this column
                    columnSearchBox = CreateColumnSearchBoxForColumn(column);
                    if (columnSearchBox == null)
                        return;
                }

                // Create a new FilteredColumnInfo
                var columnInfo = new FilteredColumnInfo
                {
                    ColumnName = column.Header?.ToString() ?? "Unknown Column",
                    BindingPath = column.SortMemberPath,
                    OriginalController = columnSearchBox.SearchTemplateController,
                    WorkingController = CloneSearchTemplateController(columnSearchBox.SearchTemplateController),
                    ColumnSearchBox = columnSearchBox
                };

                // Ensure the working controller has at least one search group
                if (columnInfo.WorkingController.SearchGroups.Count == 0)
                {
                    columnInfo.WorkingController.AddSearchGroup(true, false);
                }

                // Add to FilteredColumns
                FilteredColumns.Add(columnInfo);

                // Add to AllFilterGroups
                foreach (var group in columnInfo.WorkingController.SearchGroups)
                {
                    var filterGroupInfo = new FilterGroupInfo
                    {
                        SearchTemplateGroup = group,
                        ColumnInfo = columnInfo,
                        DisplayName = columnInfo.ColumnName
                    };
                    AllFilterGroups.Add(filterGroupInfo);
                }

                // Update operator visibility and refresh available columns
                UpdateUnifiedOperatorVisibility();
                RefreshAvailableColumns();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding column: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new ColumnSearchBox for the specified column
        /// </summary>
        private ColumnSearchBox CreateColumnSearchBoxForColumn(DataGridColumn column)
        {
            try
            {
                var columnSearchBox = new ColumnSearchBox
                {
                    CurrentColumn = column,
                    SourceDataGrid = SourceDataGrid
                };

                // The ColumnSearchBox will automatically initialize itself when CurrentColumn and SourceDataGrid are set
                // This includes creating the SearchTemplateController and setting BindingPath

                // Ensure the controller is properly initialized and has default groups
                if (columnSearchBox.SearchTemplateController != null)
                {
                    columnSearchBox.SearchTemplateController.AllowMultipleGroups = true;
                    
                    // Ensure there's at least one search group with a template
                    if (columnSearchBox.SearchTemplateController.SearchGroups.Count == 0)
                    {
                        columnSearchBox.SearchTemplateController.AddSearchGroup(true, false);
                    }
                }

                return columnSearchBox;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating ColumnSearchBox for column: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Executes the remove column command
        /// </summary>
        private void ExecuteRemoveColumn(FilteredColumnInfo columnInfo)
        {
            try
            {
                if (columnInfo == null)
                    return;

                // Remove from FilteredColumns
                FilteredColumns.Remove(columnInfo);

                // Remove associated groups from AllFilterGroups
                var groupsToRemove = AllFilterGroups.Where(fg => fg.ColumnInfo == columnInfo).ToList();
                foreach (var group in groupsToRemove)
                {
                    AllFilterGroups.Remove(group);
                }

                // Update operator visibility and refresh available columns
                UpdateUnifiedOperatorVisibility();
                RefreshAvailableColumns();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing column: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Information about a filtered column for editing
    /// </summary>
    public class FilteredColumnInfo
    {
        /// <summary>
        /// Gets or sets the column name
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the binding path
        /// </summary>
        public string BindingPath { get; set; }

        /// <summary>
        /// Gets or sets the original search template controller
        /// </summary>
        public SearchTemplateController OriginalController { get; set; }

        /// <summary>
        /// Gets or sets the working copy of the search template controller
        /// </summary>
        public SearchTemplateController WorkingController { get; set; }

        /// <summary>
        /// Gets or sets the search control reference
        /// </summary>
        public ColumnSearchBox ColumnSearchBox { get; set; }
    }

    /// <summary>
    /// Information about a filter group for the unified display
    /// </summary>
    public class FilterGroupInfo
    {
        /// <summary>
        /// Gets or sets the search template group
        /// </summary>
        public SearchTemplateGroup SearchTemplateGroup { get; set; }

        /// <summary>
        /// Gets or sets the column info this group belongs to
        /// </summary>
        public FilteredColumnInfo ColumnInfo { get; set; }

        /// <summary>
        /// Gets or sets the display name for the group header
        /// </summary>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Information about a column for display in combo boxes
    /// </summary>
    public class ColumnDisplayInfo
    {
        /// <summary>
        /// Gets or sets the actual DataGrid column
        /// </summary>
        public DataGridColumn Column { get; set; }

        /// <summary>
        /// Gets or sets the display-friendly name for the column
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Returns the display name when the object is converted to string
        /// </summary>
        public override string ToString()
        {
            return DisplayName ?? "Unknown Column";
        }
    }

    /// <summary>
    /// Event arguments for dialog closing event
    /// </summary>
    public class DialogClosingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the DialogClosingEventArgs class
        /// </summary>
        public DialogClosingEventArgs(bool accepted)
        {
            Accepted = accepted;
        }

        /// <summary>
        /// Gets whether the dialog was accepted
        /// </summary>
        public bool Accepted { get; }
    }
}