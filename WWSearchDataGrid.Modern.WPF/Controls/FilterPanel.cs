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
    /// FilterPanel control for displaying and managing active filters
    /// </summary>
    public class FilterPanel : Control
    {
        #region Dependency Properties

        public static readonly DependencyProperty FiltersEnabledProperty =
            DependencyProperty.Register("FiltersEnabled", typeof(bool), typeof(FilterPanel),
                new PropertyMetadata(true, OnFiltersEnabledChanged));

        public static readonly DependencyProperty ActiveFiltersProperty =
            DependencyProperty.Register("ActiveFilters", typeof(ObservableCollection<ColumnFilterInfo>), typeof(FilterPanel),
                new PropertyMetadata(null, OnActiveFiltersChanged));

        public static readonly DependencyProperty HasActiveFiltersProperty =
            DependencyProperty.Register("HasActiveFilters", typeof(bool), typeof(FilterPanel),
                new PropertyMetadata(false));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether filters are enabled
        /// </summary>
        public bool FiltersEnabled
        {
            get => (bool)GetValue(FiltersEnabledProperty);
            set => SetValue(FiltersEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of active filters
        /// </summary>
        public ObservableCollection<ColumnFilterInfo> ActiveFilters
        {
            get => (ObservableCollection<ColumnFilterInfo>)GetValue(ActiveFiltersProperty);
            set => SetValue(ActiveFiltersProperty, value);
        }

        /// <summary>
        /// Gets whether there are any active filters
        /// </summary>
        public bool HasActiveFilters
        {
            get => (bool)GetValue(HasActiveFiltersProperty);
            private set => SetValue(HasActiveFiltersProperty, value);
        }

        /// <summary>
        /// Gets the command to toggle all filters on/off
        /// </summary>
        public ICommand ToggleFiltersCommand { get; private set; }

        /// <summary>
        /// Gets the command to remove a specific filter
        /// </summary>
        public ICommand RemoveFilterCommand { get; private set; }

        /// <summary>
        /// Gets the command to open the edit filters dialog
        /// </summary>
        public ICommand EditFiltersCommand { get; private set; }

        /// <summary>
        /// Gets the command to clear all filters
        /// </summary>
        public ICommand ClearAllFiltersCommand { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when filters enabled state changes
        /// </summary>
        public event EventHandler<FilterEnabledChangedEventArgs> FiltersEnabledChanged;

        /// <summary>
        /// Event raised when a filter should be removed
        /// </summary>
        public event EventHandler<RemoveFilterEventArgs> FilterRemoved;

        /// <summary>
        /// Event raised when edit filters dialog should be opened
        /// </summary>
        public event EventHandler EditFiltersRequested;

        /// <summary>
        /// Event raised when all filters should be cleared
        /// </summary>
        public event EventHandler ClearAllFiltersRequested;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FilterPanel class
        /// </summary>
        public FilterPanel()
        {
            DefaultStyleKey = typeof(FilterPanel);
            ActiveFilters = new ObservableCollection<ColumnFilterInfo>();
            InitializeCommands();
            UpdateHasActiveFilters();
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Handles changes to the FiltersEnabled property
        /// </summary>
        private static void OnFiltersEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilterPanel panel)
            {
                panel.FiltersEnabledChanged?.Invoke(panel, new FilterEnabledChangedEventArgs((bool)e.NewValue));
            }
        }

        /// <summary>
        /// Handles changes to the ActiveFilters property
        /// </summary>
        private static void OnActiveFiltersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilterPanel panel)
            {
                if (e.OldValue is ObservableCollection<ColumnFilterInfo> oldCollection)
                {
                    oldCollection.CollectionChanged -= panel.OnActiveFiltersCollectionChanged;
                }

                if (e.NewValue is ObservableCollection<ColumnFilterInfo> newCollection)
                {
                    newCollection.CollectionChanged += panel.OnActiveFiltersCollectionChanged;
                }

                panel.UpdateHasActiveFilters();
            }
        }

        /// <summary>
        /// Handles changes to the ActiveFilters collection
        /// </summary>
        private void OnActiveFiltersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateHasActiveFilters();
        }

        /// <summary>
        /// Updates the HasActiveFilters property and command states
        /// </summary>
        private void UpdateHasActiveFilters()
        {
            HasActiveFilters = ActiveFilters?.Count > 0;
            
            if (EditFiltersCommand is RelayCommand editCommand)
                editCommand.RaiseCanExecuteChanged();
            if (ClearAllFiltersCommand is RelayCommand clearCommand)
                clearCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command Initialization

        /// <summary>
        /// Initializes the commands
        /// </summary>
        private void InitializeCommands()
        {
            ToggleFiltersCommand = new RelayCommand(ExecuteToggleFilters);
            RemoveFilterCommand = new RelayCommand<ColumnFilterInfo>(ExecuteRemoveFilter, CanRemoveFilter);
            EditFiltersCommand = new RelayCommand(ExecuteEditFilters, CanEditFilters);
            ClearAllFiltersCommand = new RelayCommand(ExecuteClearAllFilters, CanClearAllFilters);
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Executes the toggle filters command
        /// </summary>
        private void ExecuteToggleFilters(object parameter)
        {
            FiltersEnabled = !FiltersEnabled;
        }

        /// <summary>
        /// Executes the remove filter command
        /// </summary>
        private void ExecuteRemoveFilter(ColumnFilterInfo filterInfo)
        {
            if (filterInfo != null)
            {
                FilterRemoved?.Invoke(this, new RemoveFilterEventArgs(filterInfo));
            }
        }

        /// <summary>
        /// Determines whether a filter can be removed
        /// </summary>
        private bool CanRemoveFilter(ColumnFilterInfo filterInfo)
        {
            return filterInfo != null && filterInfo.IsActive;
        }

        /// <summary>
        /// Executes the edit filters command
        /// </summary>
        private void ExecuteEditFilters(object parameter)
        {
            EditFiltersRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Determines whether filters can be edited
        /// </summary>
        private bool CanEditFilters(object parameter)
        {
            return HasActiveFilters;
        }

        /// <summary>
        /// Executes the clear all filters command
        /// </summary>
        private void ExecuteClearAllFilters(object parameter)
        {
            ClearAllFiltersRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Determines whether all filters can be cleared
        /// </summary>
        private bool CanClearAllFilters(object parameter)
        {
            return HasActiveFilters;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the active filters collection
        /// </summary>
        public void UpdateActiveFilters(System.Collections.Generic.IEnumerable<ColumnFilterInfo> filters)
        {
            if (ActiveFilters == null)
                ActiveFilters = new ObservableCollection<ColumnFilterInfo>();

            ActiveFilters.Clear();
            
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    ActiveFilters.Add(filter);
                }
            }
            
            // Explicitly update HasActiveFilters after collection changes
            UpdateHasActiveFilters();
        }

        #endregion
    }
}