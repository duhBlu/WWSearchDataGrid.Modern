using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// FilterEditDialog provides a tabbed interface for editing column filters
    /// </summary>
    public class FilterEditDialog : Control
    {
        #region Fields

        private TabControl tabControl; // Legacy - kept for compatibility
        private ListBox filterGroupsListBox;
        private Button applyButton;
        private Button cancelButton;
        private Button closeButton;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SourceDataGridProperty =
            DependencyProperty.Register("SourceDataGrid", typeof(SearchDataGrid), typeof(FilterEditDialog),
                new PropertyMetadata(null, OnSourceDataGridChanged));

        public static readonly DependencyProperty FilteredColumnsProperty =
            DependencyProperty.Register("FilteredColumns", typeof(ObservableCollection<FilteredColumnInfo>), typeof(FilterEditDialog),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AllFilterGroupsProperty =
            DependencyProperty.Register("AllFilterGroups", typeof(ObservableCollection<FilterGroupInfo>), typeof(FilterEditDialog),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DialogAcceptedProperty =
            DependencyProperty.Register("DialogAccepted", typeof(bool), typeof(FilterEditDialog),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(FilterEditDialog),
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
        /// Close command
        /// </summary>
        public ICommand CloseCommand => new RelayCommand(_ => ExecuteClose());

        /// <summary>
        /// Add search group command
        /// </summary>
        public ICommand AddSearchGroupCommand => new RelayCommand<SearchTemplateGroup>(group => ExecuteAddSearchGroup(group));

        /// <summary>
        /// Remove search group command
        /// </summary>
        public ICommand RemoveSearchGroupCommand => new RelayCommand<SearchTemplateGroup>(group => ExecuteRemoveSearchGroup(group));

        /// <summary>
        /// Add search template command
        /// </summary>
        public ICommand AddSearchTemplateCommand => new RelayCommand<SearchTemplate>(template => ExecuteAddSearchTemplate(template));

        /// <summary>
        /// Remove search template command
        /// </summary>
        public ICommand RemoveSearchTemplateCommand => new RelayCommand<SearchTemplate>(template => ExecuteRemoveSearchTemplate(template));

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the dialog should be closed
        /// </summary>
        public event EventHandler<DialogClosingEventArgs> DialogClosing;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FilterEditDialog class
        /// </summary>
        public FilterEditDialog()
        {
            DefaultStyleKey = typeof(FilterEditDialog);
            FilteredColumns = new ObservableCollection<FilteredColumnInfo>();
            AllFilterGroups = new ObservableCollection<FilterGroupInfo>();
        }

        #endregion

        #region Control Template Methods

        /// <summary>
        /// When the template is applied
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Get template parts
            tabControl = GetTemplateChild("PART_TabControl") as TabControl; // Legacy
            filterGroupsListBox = GetTemplateChild("PART_FilterGroupsListBox") as ListBox;
            applyButton = GetTemplateChild("PART_ApplyButton") as Button;
            cancelButton = GetTemplateChild("PART_CancelButton") as Button;
            closeButton = GetTemplateChild("PART_CloseButton") as Button;

            // Wire up button events
            if (applyButton != null)
                applyButton.Click += (s, e) => ExecuteApply();

            if (cancelButton != null)
                cancelButton.Click += (s, e) => ExecuteCancel();

            if (closeButton != null)
                closeButton.Click += (s, e) => ExecuteClose();

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
            if (d is FilterEditDialog dialog && e.NewValue is SearchDataGrid dataGrid)
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
                        SearchControl = column
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
                                if (template.AvailableValues?.Count == 0)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        template.LoadAvailableValues(columnInfo.WorkingController.ColumnValues);
                                    });
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
        private SearchTemplateController CloneSearchTemplateController(SearchTemplateController original)
        {
            try
            {
                var clone = new SearchTemplateController(original.SearchTemplateType)
                {
                    ColumnName = original.ColumnName,
                    ColumnDataType = original.ColumnDataType,
                    HasCustomExpression = original.HasCustomExpression,
                    AllowMultipleGroups = true  // Enable multiple groups for FilterEditDialog
                };

                // Share reference to column values instead of deep copying (performance optimization)
                clone.ColumnValues = original.ColumnValues;
                clone.ColumnValuesByPath = original.ColumnValuesByPath;
                clone.PropertyValues = original.PropertyValues;

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
        private SearchTemplate CloneSearchTemplate(SearchTemplate original, HashSet<object> columnValues, ColumnDataType dataType)
        {
            var clone = new SearchTemplate(columnValues, dataType)
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
                foreach (FilterListValue value in original.SelectedValues)
                {
                    clone.SelectedValues.Add(new FilterListValue
                    {
                        Value = value.Value
                    });
                }
            }

            // Copy available values
            clone.LoadAvailableValues(columnValues);

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
        /// Executes the close command
        /// </summary>
        private void ExecuteClose()
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
        /// Copies changes from working controller to original controller
        /// </summary>
        private void CopyControllerChanges(SearchTemplateController source, SearchTemplateController target)
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

                // Copy other properties
                target.HasCustomExpression = source.HasCustomExpression;

                // Update the filter expression
                target.UpdateFilterExpression();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying controller changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes add search group command
        /// </summary>
        private void ExecuteAddSearchGroup(SearchTemplateGroup referenceGroup)
        {
            try
            {
                // Find the column info that contains this group
                var filterGroupInfo = AllFilterGroups.FirstOrDefault(fg => fg.SearchTemplateGroup == referenceGroup);
                if (filterGroupInfo?.ColumnInfo?.WorkingController != null)
                {
                    var newGroup = new SearchTemplateGroup();
                    filterGroupInfo.ColumnInfo.WorkingController.AddSearchGroup(true, true, referenceGroup);
                    
                    // Add the new group to our unified collection
                    RefreshAllFilterGroups();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding search group: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes remove search group command
        /// </summary>
        private void ExecuteRemoveSearchGroup(SearchTemplateGroup group)
        {
            try
            {
                // Find the column info that contains this group
                var filterGroupInfo = AllFilterGroups.FirstOrDefault(fg => fg.SearchTemplateGroup == group);
                if (filterGroupInfo?.ColumnInfo?.WorkingController != null)
                {
                    filterGroupInfo.ColumnInfo.WorkingController.RemoveSearchGroup(group);
                    RefreshAllFilterGroups();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing search group: {ex.Message}");
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
                    filterGroupInfo.ColumnInfo.WorkingController.AddSearchTemplate(true, true, referenceTemplate);
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
        /// Gets the currently selected column info from the tab control (legacy method, kept for compatibility)
        /// </summary>
        private FilteredColumnInfo GetCurrentColumnInfo()
        {
            if (tabControl?.SelectedItem is FilteredColumnInfo columnInfo)
            {
                return columnInfo;
            }
            return FilteredColumns?.FirstOrDefault();
        }

        /// <summary>
        /// Refreshes the AllFilterGroups collection after changes
        /// </summary>
        private void RefreshAllFilterGroups()
        {
            try
            {
                AllFilterGroups.Clear();

                foreach (var columnInfo in FilteredColumns)
                {
                    foreach (var group in columnInfo.WorkingController.SearchGroups)
                    {
                        var filterGroupInfo = new FilterGroupInfo
                        {
                            SearchTemplateGroup = group,
                            ColumnInfo = columnInfo,
                            DisplayName = $"Advanced Filter: {columnInfo.ColumnName}"
                        };

                        AllFilterGroups.Add(filterGroupInfo);
                    }
                }

                // Update operator visibility based on unified group ordering
                UpdateUnifiedOperatorVisibility();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing filter groups: {ex.Message}");
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
        public SearchControl SearchControl { get; set; }
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