using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Information about a column's active filter state
    /// </summary>
    public class ColumnFilterInfo : ObservableObject
    {
        private string columnName;
        private string bindingPath;
        private FilterType filterType;
        private string displayText;
        private bool isActive;
        private object filterData;

        /// <summary>
        /// Gets or sets the display name of the column
        /// </summary>
        public string ColumnName
        {
            get => columnName;
            set => SetProperty(value, ref columnName);
        }

        /// <summary>
        /// Gets or sets the binding path for the column
        /// </summary>
        public string BindingPath
        {
            get => bindingPath;
            set => SetProperty(value, ref bindingPath);
        }

        /// <summary>
        /// Gets or sets the type of filter applied
        /// </summary>
        public FilterType FilterType
        {
            get => filterType;
            set => SetProperty(value, ref filterType);
        }

        /// <summary>
        /// Gets or sets the display text for the filter (e.g., "Contains 'test'")
        /// </summary>
        public string DisplayText
        {
            get => displayText;
            set => SetProperty(value, ref displayText);
        }

        /// <summary>
        /// Gets or sets whether this filter is currently active
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set => SetProperty(value, ref isActive);
        }

        /// <summary>
        /// Gets or sets the filter data reference (SearchControl or SearchTemplateController)
        /// </summary>
        public object FilterData
        {
            get => filterData;
            set => SetProperty(value, ref filterData);
        }
    }

    /// <summary>
    /// Enumeration of filter types
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// Simple text-based filter from SearchControl
        /// </summary>
        Simple,

        /// <summary>
        /// Advanced multi-criteria filter from AdvancedFilterControl
        /// </summary>
        Advanced
    }

    /// <summary>
    /// ViewModel for the FilterPanel control
    /// </summary>
    public class FilterPanelViewModel : ObservableObject
    {
        private bool filtersEnabled = true;
        private ObservableCollection<ColumnFilterInfo> activeFilters;
        private ICommand toggleFiltersCommand;
        private ICommand removeFilterCommand;
        private ICommand editFiltersCommand;
        private ICommand clearAllFiltersCommand;

        /// <summary>
        /// Initializes a new instance of the FilterPanelViewModel class
        /// </summary>
        public FilterPanelViewModel()
        {
            ActiveFilters = new ObservableCollection<ColumnFilterInfo>();
            InitializeCommands();
        }

        /// <summary>
        /// Gets or sets whether filters are enabled (without clearing filter definitions)
        /// </summary>
        public bool FiltersEnabled
        {
            get => filtersEnabled;
            set
            {
                if (SetProperty(value, ref filtersEnabled))
                {
                    FiltersEnabledChanged?.Invoke(this, new FilterEnabledChangedEventArgs(value));
                }
            }
        }

        /// <summary>
        /// Gets the collection of active filters
        /// </summary>
        public ObservableCollection<ColumnFilterInfo> ActiveFilters
        {
            get => activeFilters;
            private set => SetProperty(value, ref activeFilters);
        }

        /// <summary>
        /// Gets whether there are any active filters
        /// </summary>
        public bool HasActiveFilters => ActiveFilters?.Count > 0;

        /// <summary>
        /// Gets the command to toggle all filters on/off
        /// </summary>
        public ICommand ToggleFiltersCommand
        {
            get => toggleFiltersCommand;
            private set => SetProperty(value, ref toggleFiltersCommand);
        }

        /// <summary>
        /// Gets the command to remove a specific filter
        /// </summary>
        public ICommand RemoveFilterCommand
        {
            get => removeFilterCommand;
            private set => SetProperty(value, ref removeFilterCommand);
        }

        /// <summary>
        /// Gets the command to open the edit filters dialog
        /// </summary>
        public ICommand EditFiltersCommand
        {
            get => editFiltersCommand;
            private set => SetProperty(value, ref editFiltersCommand);
        }

        /// <summary>
        /// Gets the command to clear all filters
        /// </summary>
        public ICommand ClearAllFiltersCommand
        {
            get => clearAllFiltersCommand;
            private set => SetProperty(value, ref clearAllFiltersCommand);
        }

        /// <summary>
        /// Event raised when filters enabled state changes
        /// </summary>
        public event EventHandler<FilterEnabledChangedEventArgs> FiltersEnabledChanged;

        /// <summary>
        /// Event raised when a filter should be removed
        /// </summary>
        public event EventHandler<RemoveFilterEventArgs> RemoveFilterRequested;

        /// <summary>
        /// Event raised when edit filters dialog should be opened
        /// </summary>
        public event EventHandler EditFiltersRequested;

        /// <summary>
        /// Event raised when all filters should be cleared
        /// </summary>
        public event EventHandler ClearAllFiltersRequested;

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
                RemoveFilterRequested?.Invoke(this, new RemoveFilterEventArgs(filterInfo));
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

        /// <summary>
        /// Updates the active filters collection
        /// </summary>
        public void UpdateActiveFilters(System.Collections.Generic.IEnumerable<ColumnFilterInfo> filters)
        {
            ActiveFilters.Clear();
            
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    ActiveFilters.Add(filter);
                }
            }

            // Notify that HasActiveFilters property may have changed
            OnPropertyChanged(nameof(HasActiveFilters));
        }
    }

    /// <summary>
    /// Event arguments for filter enabled changed event
    /// </summary>
    public class FilterEnabledChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the FilterEnabledChangedEventArgs class
        /// </summary>
        public FilterEnabledChangedEventArgs(bool enabled)
        {
            Enabled = enabled;
        }

        /// <summary>
        /// Gets whether filters are enabled
        /// </summary>
        public bool Enabled { get; }
    }

    /// <summary>
    /// Event arguments for remove filter event
    /// </summary>
    public class RemoveFilterEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the RemoveFilterEventArgs class
        /// </summary>
        public RemoveFilterEventArgs(ColumnFilterInfo filterInfo)
        {
            FilterInfo = filterInfo;
        }

        /// <summary>
        /// Gets the filter information to remove
        /// </summary>
        public ColumnFilterInfo FilterInfo { get; }
    }
}