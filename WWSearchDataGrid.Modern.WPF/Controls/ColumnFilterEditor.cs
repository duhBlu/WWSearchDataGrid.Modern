using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
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

        public static readonly DependencyProperty IsOperatorVisibleProperty =
            DependencyProperty.Register(nameof(IsOperatorVisible), typeof(bool), typeof(ColumnFilterEditor),
                new PropertyMetadata(false));

        /// <summary>
        /// Horizontal offset to be applied to the containing Popup.
        /// Positive values move right; negative values move left.
        /// This property is read by the Popup owner (ColumnSearchBox) when creating/showing the Popup.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(
                nameof(HorizontalOffset),
                typeof(double),
                typeof(ColumnFilterEditor),
                new PropertyMetadata(0.0));

        /// <summary>
        /// Vertical offset to be applied to the containing Popup.
        /// Positive values move down; negative values move up.
        /// This property is read by the Popup owner (ColumnSearchBox) when creating/showing the Popup.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(
                nameof(VerticalOffset),
                typeof(double),
                typeof(ColumnFilterEditor),
                new PropertyMetadata(0.0));

        #endregion

        #region Properties

        public SearchTemplateController SearchTemplateController { get; set; }

        public bool IsOperatorVisible
        {
            get => (bool)GetValue(IsOperatorVisibleProperty);
            set => SetValue(IsOperatorVisibleProperty, value);
        }

        /// <summary>
        /// Horizontal offset (pixels) for the containing Popup.
        /// This value is read by the Popup owner when positioning the Popup.
        /// </summary>
        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>
        /// Vertical offset (pixels) for the containing Popup.
        /// This value is read by the Popup owner when positioning the Popup.
        /// </summary>
        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

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
                Debug.WriteLine($"Error loading ColumnFilterEditor: {ex.Message}");
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
                SearchTemplateController.AutoApplyFilter -= OnAutoApplyFilter;
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

            if(SearchTemplateController != null)
            {
                SearchTemplateController.AutoApplyFilter += OnAutoApplyFilter;
            }
        }

        /// <summary>
        /// Triggers loading of column values when filter editor opens
        /// PERFORMANCE: This is where cache loading happens - deferred until user explicitly opens filter
        /// </summary>
        private void TriggerColumnValueLoading()
        {
            try
            {
                if (DataContext is ColumnSearchBox columnSearchBox &&
                    columnSearchBox.SearchTemplateController != null)
                {
                    // Ensure column values are loaded (this will also determine null status)
                    columnSearchBox.SearchTemplateController.EnsureColumnValuesLoadedForFiltering();

                    // Explicitly ensure null status is determined and templates updated
                    columnSearchBox.SearchTemplateController.EnsureNullStatusDetermined();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering column value loading: {ex.Message}");
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
                Debug.WriteLine($"Clear filter failed: {ex.Message}");
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
                Debug.WriteLine($"Add search template failed: {ex.Message}");
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
                Debug.WriteLine($"Remove search template failed: {ex.Message}");
            }
        }

        #endregion

        #region Auto-Apply Methods

        /// <summary>
        /// Handle the unified filter should apply event from SearchTemplateController
        /// </summary>
        private void OnAutoApplyFilter(object sender, EventArgs e)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ApplyFilterAutomatically();
                });
            }
            catch { }
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

                FiltersApplied?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto filter application failed: {ex.Message}");
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