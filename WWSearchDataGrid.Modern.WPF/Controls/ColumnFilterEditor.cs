using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Simplified filter control with rules-only interface (no value selection tab)
    /// </summary>
    public class ColumnFilterEditor : Control, INotifyPropertyChanged
    {
        #region Fields

        private ColumnDataType columnDataType;
        private bool _isInitialized;
        
        #endregion

        #region Dependency Properties

        /// <summary>
        /// Attached property for specifying the default search type for filter templates
        /// </summary>
        public static readonly DependencyProperty DefaultSearchTypeProperty =
            DependencyProperty.RegisterAttached("DefaultSearchType", typeof(SearchType), typeof(ColumnFilterEditor),
                new FrameworkPropertyMetadata(SearchType.Contains, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Dependency property for controlling operator ComboBox visibility
        /// </summary>
        public static readonly DependencyProperty IsOperatorVisibleProperty =
            DependencyProperty.Register(nameof(IsOperatorVisible), typeof(bool), typeof(ColumnFilterEditor),
                new PropertyMetadata(false));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the search controller for this control
        /// </summary>
        public SearchTemplateController SearchTemplateController { get; set; }

        /// <summary>
        /// Gets or sets the column data type
        /// </summary>
        public ColumnDataType ColumnDataType
        {
            get => columnDataType;
            set => SetProperty(value, ref columnDataType);
        }

        /// <summary>
        /// Gets or sets whether the operator ComboBox is visible
        /// </summary>
        public bool IsOperatorVisible
        {
            get => (bool)GetValue(IsOperatorVisibleProperty);
            set => SetValue(IsOperatorVisibleProperty, value);
        }

        /// <summary>
        /// Gets the group operator name from the first search group
        /// </summary>
        public string GroupOperatorName
        {
            get => SearchTemplateController?.SearchGroups?.Count > 0 ? SearchTemplateController.SearchGroups[0].OperatorName : "Or";
            set
            {
                if (SearchTemplateController?.SearchGroups?.Count > 0)
                {
                    var currentValue = SearchTemplateController.SearchGroups[0].OperatorName;
                    if (currentValue != value)
                    {
                        SearchTemplateController.SearchGroups[0].OperatorName = value;
                        SearchTemplateController.UpdateFilterExpression();
                        OnPropertyChanged(nameof(GroupOperatorName));
                    }
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Apply filter command
        /// </summary>
        public ICommand ApplyFilterCommand => new RelayCommand(_ => ApplyFilter());

        /// <summary>
        /// Clear filter command
        /// </summary>
        public ICommand ClearFilterCommand => new RelayCommand(_ => ClearFilter());

        /// <summary>
        /// Close window command
        /// </summary>
        public ICommand CloseWindowCommand => new RelayCommand(_ => CloseWindow());

        /// <summary>
        /// Add search template command
        /// </summary>
        public ICommand AddSearchTemplateCommand => new RelayCommand(_ => AddSearchTemplate());

        /// <summary>
        /// Remove search template command
        /// </summary>
        public ICommand RemoveSearchTemplateCommand => new RelayCommand(template => RemoveSearchTemplate(template));

        #endregion

        #region Constructors

        public ColumnFilterEditor()
        {
            Loaded += OnControlLoaded;
            Unloaded += OnUnloaded;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle control loaded event
        /// </summary>
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;

            try
            {
                // Initialize the rules interface
                InitializeRulesInterface();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ColumnFilterEditor: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle control unloaded event
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the rules interface
        /// </summary>
        private void InitializeRulesInterface()
        {
            // Set up operator visibility based on search groups
            UpdateOperatorVisibility();
        }

        /// <summary>
        /// Updates operator visibility based on whether there are active filters in preceding columns
        /// </summary>
        private void UpdateOperatorVisibility()
        {
            IsOperatorVisible = false;

            // Check if we have access to the data grid through the column search box
            if (DataContext is ColumnSearchBox currentColumnSearchBox && 
                currentColumnSearchBox.SourceDataGrid != null)
            {
                var dataGrid = currentColumnSearchBox.SourceDataGrid;
                var currentColumn = currentColumnSearchBox;
                
                // Check if any preceding columns have active filters
                foreach (var column in dataGrid.DataColumns)
                {
                    // Stop when we reach the current column
                    if (column == currentColumn)
                        break;
                        
                    // If this preceding column has an active filter, show the operator
                    if (column.HasActiveFilter)
                    {
                        IsOperatorVisible = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Apply filter directly and close the window
        /// </summary>
        private void ApplyFilter()
        {
            if (SearchTemplateController == null) return;
            
            try
            {
                // Apply filter directly through SearchTemplateController - no service layer needed
                SearchTemplateController.UpdateFilterExpression();
                
                // Update the UI state after successful filter application
                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    columnSearchBox.HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                    columnSearchBox.SourceDataGrid.FilterItemsSource();
                    columnSearchBox.SourceDataGrid.UpdateFilterPanel();
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filter application failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all filters
        /// </summary>
        private void ClearFilter()
        {
            if (SearchTemplateController == null) return;

            try
            {
                // Clear filters directly through SearchTemplateController
                SearchTemplateController.ClearAndReset();
                
                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    columnSearchBox.HasAdvancedFilter = false;
                    columnSearchBox.SourceDataGrid.FilterItemsSource();
                    columnSearchBox.SourceDataGrid.UpdateFilterPanel();
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear filter failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the window
        /// </summary>
        private void CloseWindow()
        {
            Window.GetWindow(this)?.Close();
        }

        /// <summary>
        /// Add a new search template rule
        /// </summary>
        private void AddSearchTemplate()
        {
            if (SearchTemplateController == null) return;

            try
            {
                // Delegate to SearchTemplateController - it handles all the logic
                SearchTemplateController.AddSearchTemplate(markAsChanged: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add search template failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a search template rule
        /// </summary>
        private void RemoveSearchTemplate(object template)
        {
            if (SearchTemplateController == null || template is not SearchTemplate searchTemplate) return;

            try
            {
                // Delegate to SearchTemplateController - it handles all the logic
                SearchTemplateController.RemoveSearchTemplate(searchTemplate);
                
                // Update UI state
                UpdateOperatorVisibility();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remove search template failed: {ex.Message}");
            }
        }


        /// <summary>
        /// Get attached DefaultSearchType property
        /// </summary>
        public static SearchType GetDefaultSearchType(DependencyObject obj)
        {
            return (SearchType)obj.GetValue(DefaultSearchTypeProperty);
        }

        /// <summary>
        /// Set attached DefaultSearchType property
        /// </summary>
        public static void SetDefaultSearchType(DependencyObject obj, SearchType value)
        {
            obj.SetValue(DefaultSearchTypeProperty, value);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises property changed event
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets property value and raises property changed event if changed
        /// </summary>
        protected bool SetProperty<T>(T value, ref T field, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}