using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Filter editor with deferred-apply: changes are NOT applied to the grid while
    /// the editor is open.  When the popup closes, invalid/incomplete rules are pruned
    /// and the resulting filter is applied once.
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

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(ColumnFilterEditor),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(ColumnFilterEditor),
                new PropertyMetadata(0.0));

        #endregion

        #region Properties

        public SearchTemplateController SearchTemplateController { get; set; }

        public bool IsOperatorVisible
        {
            get => (bool)GetValue(IsOperatorVisibleProperty);
            set => SetValue(IsOperatorVisibleProperty, value);
        }

        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        public double VerticalOffset
        {
            get => (double)GetValue(VerticalOffsetProperty);
            set => SetValue(VerticalOffsetProperty, value);
        }

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
                        OnPropertyChanged(nameof(GroupOperatorName));
                    }
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler FiltersApplied;
        public event EventHandler FiltersCleared;

        #endregion

        #region Commands

        private ICommand _clearFilterCommand;
        private ICommand _addSearchTemplateCommand;
        private ICommand _removeSearchTemplateCommand;

        public ICommand ClearFilterCommand => _clearFilterCommand ??= new RelayCommand(_ => ClearFilter());
        public ICommand AddSearchTemplateCommand => _addSearchTemplateCommand ??= new RelayCommand(_ => AddSearchTemplate());
        public ICommand RemoveSearchTemplateCommand => _removeSearchTemplateCommand ??= new RelayCommand(template => RemoveSearchTemplate(template));

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

        /// <summary>
        /// When the editor closes (popup closes), prune invalid rules and apply the filter once.
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                PruneAndApply();
            }
            _isInitialized = false;
        }

        #endregion

        #region Methods

        private void InitializeRulesInterface()
        {
            TriggerColumnValueLoading();
            UpdateOperatorVisibility();

            // NOTE: We intentionally do NOT subscribe to AutoApplyFilter.
            // The editor uses deferred-apply — the filter is applied once when the popup closes.
        }

        private void TriggerColumnValueLoading()
        {
            try
            {
                if (DataContext is ColumnSearchBox columnSearchBox &&
                    columnSearchBox.SearchTemplateController != null)
                {
                    columnSearchBox.SearchTemplateController.EnsureColumnValuesLoadedForFiltering();
                    columnSearchBox.SearchTemplateController.EnsureNullStatusDetermined();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering column value loading: {ex.Message}");
            }
        }

        private void UpdateOperatorVisibility()
        {
            IsOperatorVisible = false;

            if (DataContext is ColumnSearchBox currentColumnSearchBox &&
                currentColumnSearchBox.SourceDataGrid != null)
            {
                var dataGrid = currentColumnSearchBox.SourceDataGrid;

                foreach (var column in dataGrid.DataColumns)
                {
                    if (column == currentColumnSearchBox) break;
                    if (column.HasActiveFilter)
                    {
                        IsOperatorVisible = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Removes incomplete/invalid search templates, then applies the resulting filter to the grid.
        /// </summary>
        private void PruneAndApply()
        {
            if (SearchTemplateController == null) return;

            try
            {
                // Remove invalid templates from each group
                foreach (var group in SearchTemplateController.SearchGroups.ToList())
                {
                    var invalidTemplates = group.SearchTemplates
                        .Where(t => !t.IsValidFilter)
                        .ToList();

                    foreach (var invalid in invalidTemplates)
                    {
                        SearchTemplateController.RemoveSearchTemplate(invalid);
                    }
                }

                // Build and apply the filter expression
                SearchTemplateController.UpdateFilterExpression();

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
                Debug.WriteLine($"PruneAndApply failed: {ex.Message}");
            }
        }

        private void ClearFilter()
        {
            if (SearchTemplateController == null) return;

            try
            {
                SearchTemplateController.ClearAndReset();

                if (DataContext is ColumnSearchBox columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    columnSearchBox.HasAdvancedFilter = false;
                    columnSearchBox.SourceDataGrid.FilterItemsSource();
                    columnSearchBox.SourceDataGrid.UpdateFilterPanel();
                }

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
                SearchTemplateController.RemoveSearchTemplate(searchTemplate);
                UpdateOperatorVisibility();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Remove search template failed: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
