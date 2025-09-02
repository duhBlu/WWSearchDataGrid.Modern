using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Simplified filter control with rules-only interface and auto-apply functionality for popup usage
    /// </summary>
    public class ColumnFilterEditor : Control, INotifyPropertyChanged
    {
        #region Fields

        private bool _isInitialized;
        
        #endregion

        #region Dependency Properties

        /// <summary>
        /// Attached property for specifying the default search type for filter templates
        /// </summary>
        public static readonly DependencyProperty DefaultSearchTypeProperty =
            DependencyProperty.RegisterAttached("DefaultSearchType", typeof(SearchType), typeof(ColumnFilterEditor),
                new FrameworkPropertyMetadata(SearchType.Contains, FrameworkPropertyMetadataOptions.Inherits));

        public static SearchType GetDefaultSearchType(DependencyObject obj)
        {
            return (SearchType)obj.GetValue(DefaultSearchTypeProperty);
        }

        public static void SetDefaultSearchType(DependencyObject obj, SearchType value)
        {
            obj.SetValue(DefaultSearchTypeProperty, value);
        }

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

        #region Events

        /// <summary>
        /// Occurs when filters are applied automatically
        /// </summary>
        public event EventHandler FiltersApplied;

        /// <summary>
        /// Occurs when filters are cleared
        /// </summary>
        public event EventHandler FiltersCleared;

        #endregion

        #region Commands

        /// <summary>
        /// Clear filter command
        /// </summary>
        public ICommand ClearFilterCommand => new RelayCommand(_ => ClearFilter());

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
            DefaultStyleKey = typeof(ColumnFilterEditor);
        }



        #endregion

        

        #region Event Handlers

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;

            try
            {
                InitializeRulesInterface();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ColumnFilterEditor: {ex.Message}");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = false;
            CleanupEventSubscriptions();
        }
        
        /// <summary>
        /// Clean up all event subscriptions
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (SearchTemplateController != null)
            {
                SearchTemplateController.FilterShouldApply -= OnFilterShouldApply;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the rules interface
        /// </summary>
        private void InitializeRulesInterface()
        {
            TriggerColumnValueLoading();
            UpdateOperatorVisibility();
            SetupAutoApplyMonitoring();
        }

        /// <summary>
        /// Triggers loading of column values when filter editor opens
        /// </summary>
        private void TriggerColumnValueLoading()
        {
            try
            {
                if (DataContext is ColumnSearchBox columnSearchBox && 
                    columnSearchBox.SearchTemplateController != null)
                {
                    columnSearchBox.SearchTemplateController.EnsureColumnValuesLoadedForFiltering();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error triggering column value loading: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates operator visibility based on whether there are active filters in preceding columns
        /// </summary>
        private void UpdateOperatorVisibility()
        {
            IsOperatorVisible = false;

            if (DataContext is ColumnSearchBox currentColumnSearchBox && 
                currentColumnSearchBox.SourceDataGrid != null)
            {
                var dataGrid = currentColumnSearchBox.SourceDataGrid;
                var currentColumn = currentColumnSearchBox;
                
                foreach (var column in dataGrid.DataColumns)
                {
                    if (column == currentColumn)
                        break;
                        
                    if (column.HasActiveFilter)
                    {
                        IsOperatorVisible = true;
                        break;
                    }
                }
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
                }

                // Notify that filters were cleared
                FiltersCleared?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear filter failed: {ex.Message}");
            }
        }

        private void AddSearchTemplate()
        {
            if (SearchTemplateController == null) return;

            try
            {
                SearchTemplateController.AddSearchTemplate(markAsChanged: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add search template failed: {ex.Message}");
            }
        }

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

        #endregion

        #region Auto-Apply Methods

        /// <summary>
        /// Set up monitoring for auto-apply triggers
        /// </summary>
        private void SetupAutoApplyMonitoring()
        {
            if (SearchTemplateController != null)
            {
                // Subscribe to the single unified event
                SearchTemplateController.FilterShouldApply += OnFilterShouldApply;
            }
        }

        /// <summary>
        /// Handle the unified filter should apply event from SearchTemplateController
        /// </summary>
        private void OnFilterShouldApply(object sender, EventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ApplyFilterAutomatically();
            });
        }

        /// <summary>
        /// Apply filter automatically (triggered by debounced changes)
        /// </summary>
        private void ApplyFilterAutomatically()
        {
            if (SearchTemplateController == null) return;

            try
            {
                SearchTemplateController.UpdateFilterExpression();

                // Update the UI state after successful filter application
                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    columnSearchBox.HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                    columnSearchBox.SourceDataGrid.FilterItemsSource();
                    columnSearchBox.SourceDataGrid.UpdateFilterPanel();
                }

                // Notify that filters were applied
                FiltersApplied?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto filter application failed: {ex.Message}");
            }
        }


        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}