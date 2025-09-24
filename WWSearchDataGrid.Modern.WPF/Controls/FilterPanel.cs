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

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(FilterPanel),
                new PropertyMetadata(false, OnIsExpandedChanged));

        public static readonly DependencyProperty FilterTokensProperty =
            DependencyProperty.Register("FilterTokens", typeof(ObservableCollection<IFilterToken>), typeof(FilterPanel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty HasOverflowProperty =
            DependencyProperty.Register("HasOverflow", typeof(bool), typeof(FilterPanel),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HoveredFilterIdProperty =
            DependencyProperty.Register("HoveredFilterId", typeof(string), typeof(FilterPanel),
                new PropertyMetadata(null));

        #endregion

        #region Properties
        
        public bool FiltersEnabled
        {
            get => (bool)GetValue(FiltersEnabledProperty);
            set => SetValue(FiltersEnabledProperty, value);
        }

        public ObservableCollection<ColumnFilterInfo> ActiveFilters
        {
            get => (ObservableCollection<ColumnFilterInfo>)GetValue(ActiveFiltersProperty);
            set => SetValue(ActiveFiltersProperty, value);
        }

        public bool HasActiveFilters
        {
            get => (bool)GetValue(HasActiveFiltersProperty);
            private set => SetValue(HasActiveFiltersProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of filter tokens for tokenized display
        /// </summary>
        public ObservableCollection<IFilterToken> FilterTokens
        {
            get => (ObservableCollection<IFilterToken>)GetValue(FilterTokensProperty);
            set => SetValue(FilterTokensProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the filter content overflows and needs expansion
        /// </summary>
        public bool HasOverflow
        {
            get => (bool)GetValue(HasOverflowProperty);
            set => SetValue(HasOverflowProperty, value);
        }

        /// <summary>
        /// Gets or sets the FilterId of the currently hovered filter group
        /// </summary>
        public string HoveredFilterId
        {
            get => (string)GetValue(HoveredFilterIdProperty);
            set => SetValue(HoveredFilterIdProperty, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to toggle all filters on/off
        /// </summary>
        public ICommand ToggleFiltersCommand => new RelayCommand(ExecuteToggleFilters);

        /// <summary>
        /// Gets the command to remove a filter by token
        /// </summary>
        public ICommand RemoveTokenFilterCommand => new RelayCommand<IFilterToken>(ExecuteRemoveTokenFilter, CanRemoveTokenFilter);

        /// <summary>
        /// Gets the command to clear all filters
        /// </summary>
        public ICommand ClearAllFiltersCommand => new RelayCommand(_ => ClearAllFiltersRequested?.Invoke(this, EventArgs.Empty), _ => HasActiveFilters);

        /// <summary>
        /// Gets the command to toggle the expand/collapse state
        /// </summary>
        public ICommand ToggleExpandCommand => new RelayCommand(_ => IsExpanded = !IsExpanded);

        /// <summary>
        /// Gets the command to set the hovered filter id
        /// </summary>
        public ICommand SetHoveredFilterCommand => new RelayCommand<string>(filterId => HoveredFilterId = filterId);

        /// <summary>
        /// Gets the command to clear the hovered filter id
        /// </summary>
        public ICommand ClearHoveredFilterCommand => new RelayCommand(_ => HoveredFilterId = null);

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
            FilterTokens = new ObservableCollection<IFilterToken>();
            UpdateHasActiveFilters();
            
            Loaded += OnFilterPanelLoaded;
            SizeChanged += OnFilterPanelSizeChanged;
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
                panel.UpdateFilterTokens();
            }
        }


        /// <summary>
        /// Handles changes to the IsExpanded property
        /// </summary>
        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilterPanel panel)
            {
                panel.Dispatcher.BeginInvoke(() => panel.CheckForOverflow(), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Handles changes to the ActiveFilters collection
        /// </summary>
        private void OnActiveFiltersCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateHasActiveFilters();
            UpdateFilterTokens();
        }

        /// <summary>
        /// Updates the HasActiveFilters property and command states
        /// </summary>
        private void UpdateHasActiveFilters()
        {
            HasActiveFilters = ActiveFilters?.Count > 0;
            
            if (ClearAllFiltersCommand is RelayCommand clearCommand)
                clearCommand.RaiseCanExecuteChanged();
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
        /// Executes the remove token filter command
        /// </summary>
        private void ExecuteRemoveTokenFilter(IFilterToken token)
        {
            if (token?.SourceFilter != null)
            {
                FilterRemoved?.Invoke(this, new RemoveFilterEventArgs(token.SourceFilter));
                CheckForOverflow(true);
            }
        }

        /// <summary>
        /// Determines whether a token filter can be removed
        /// </summary>
        private bool CanRemoveTokenFilter(IFilterToken token)
        {
            return token?.SourceFilter != null && token.SourceFilter.IsActive;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the active filters collection
        /// </summary>
        public void UpdateActiveFilters(IEnumerable<ColumnFilterInfo> filters)
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

        /// <summary>
        /// Updates the filter tokens collection based on current active filters
        /// </summary>
        private void UpdateFilterTokens()
        {
            if (FilterTokens == null)
                FilterTokens = new ObservableCollection<IFilterToken>();

            FilterTokens.Clear();

            if (ActiveFilters?.Count > 0)
            {
                var tokens = FilterTokenConverter.ConvertToTokens(ActiveFilters);
                foreach (var token in tokens)
                {
                    FilterTokens.Add(token);
                }
            }
            
            Dispatcher.BeginInvoke(() => CheckForOverflow(), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Handles the FilterPanel loaded event
        /// </summary>
        private void OnFilterPanelLoaded(object sender, RoutedEventArgs e)
        {
            CheckForOverflow();
        }

        /// <summary>
        /// Handles the FilterPanel size changed event
        /// </summary>
        private void OnFilterPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CheckForOverflow();
        }

        /// <summary>
        /// Checks if the filter content overflows the available space
        /// </summary>
        private void CheckForOverflow(bool isRemovingFilter = false)
        {
            // If expanded (and not shrinking after a remove), we don't show overflow UI.
            if (IsExpanded && !isRemovingFilter)
            {
                HasOverflow = false;
                return;
            }

            // Get live template parts each call (no OnApplyTemplate needed)
            var tokens = GetTemplateChild("PART_TokenizedFiltersControl") as FrameworkElement;
            if (tokens == null)
                return;

            var chk = GetTemplateChild("PART_FiltersEnabledCheckBox") as FrameworkElement;
            var btnEx = GetTemplateChild("PART_ExpandButton") as FrameworkElement;
            var btnCl = GetTemplateChild("PART_ClearAllButton") as FrameworkElement;

            // Make sure everything has a layout pass before we read ActualWidth
            UpdateLayout();

            // Get the token lane width taking into account other controls in the filter panel
            // (This mirrors your template: [FiltersEnabledCheckBox] [Tokens stretch *] [Buttons])
            double leftSide = GetActualWidthWithMargin(chk);
            double rightSide = GetActualWidthWithMargin(btnEx) + GetActualWidthWithMargin(btnCl);

            // Account for our own padding/margins if you add them later; for now we assume none on the root border
            double availableTokenLane = Math.Max(0, ActualWidth - leftSide - rightSide);

            // Measure how wide the tokens WANT to be on a single line
            // Measure with infinite width to get their natural one-line DesiredSize
            double heightHint = (tokens.ActualHeight > 0) ? tokens.ActualHeight : double.PositiveInfinity;
            tokens.Measure(new Size(double.PositiveInfinity, heightHint));
            double tokenNaturalWidth = tokens.DesiredSize.Width;

            
            bool overflow = tokenNaturalWidth > availableTokenLane;

            // If showing the expand button only appears *because* overflow turned true,
            // the rightSide may have grown; do one stabilizing re-check.
            if (overflow && btnEx != null && btnEx.Visibility == Visibility.Visible)
            {
                rightSide = GetActualWidthWithMargin(btnEx) + GetActualWidthWithMargin(btnCl);
                availableTokenLane = Math.Max(0, ActualWidth - leftSide - rightSide);
                overflow = tokenNaturalWidth > availableTokenLane;
            }

            HasOverflow = overflow;

            // If expanded but no longer overflowing (after removing a filter), auto-collapse
            if (!HasOverflow && IsExpanded)
                IsExpanded = false;
        }

        private static double GetActualWidthWithMargin(FrameworkElement fe)
        {
            if (fe == null || fe.Visibility != Visibility.Visible)
                return 0;

            fe.UpdateLayout();
            var m = fe.Margin;
            return fe.ActualWidth + m.Left + m.Right;
        }

        #endregion
    }
}