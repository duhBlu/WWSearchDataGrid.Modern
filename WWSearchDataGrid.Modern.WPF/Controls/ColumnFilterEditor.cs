using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        /// <summary>
        /// Whether changes made inside the editor (dropdown selections, value-tab checkbox
        /// toggles, SearchType combo swaps, etc.) apply to the grid immediately or are deferred
        /// until the popup closes. Defaults to <c>true</c>; <see cref="ColumnSearchBox"/>
        /// overwrites this on popup creation with its row-count-driven auto value, and the
        /// "Live filter" checkbox in the editor footer lets the user override mid-session.
        /// <para>
        /// When <c>false</c>, the editor still calls <c>UpdateFilterExpression</c> internally
        /// so the controller state and "active filter" indicator stay in sync — only the
        /// expensive <c>grid.FilterItemsSource()</c> pass is skipped. <c>PruneAndApply</c> on
        /// unload guarantees the final filter is applied once the editor closes.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty IsLiveApplyEnabledProperty =
            DependencyProperty.Register(nameof(IsLiveApplyEnabled), typeof(bool), typeof(ColumnFilterEditor),
                new PropertyMetadata(true, OnIsLiveApplyEnabledChanged));

        public bool IsLiveApplyEnabled
        {
            get => (bool)GetValue(IsLiveApplyEnabledProperty);
            set => SetValue(IsLiveApplyEnabledProperty, value);
        }

        private static void OnIsLiveApplyEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Propagate the user's footer-checkbox toggle back up to the column descriptor.
            // GridColumn.ImmediateUpdateAutoFilter is the persistent home for this preference —
            // setting it here keeps the in-header SearchTextBox's LiveUpdate mode in sync with
            // the user's popup choice and outlives popup open/close cycles within a session.
            if (d is ColumnFilterEditor editor && editor.DataContext is ColumnFilterControl csb)
            {
                bool newValue = (bool)e.NewValue;
                if (csb.GridColumn != null && csb.GridColumn.ImmediateUpdateAutoFilter != newValue)
                    csb.GridColumn.ImmediateUpdateAutoFilter = newValue;
            }
        }

        #endregion

        #region Properties

        public SearchTemplateController SearchTemplateController { get; set; }

        /// <summary>
        /// Manages the Filter Values tab state (checkbox list, sync with rules).
        /// </summary>
        public FilterValueManager FilterValueManager { get; private set; }

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
        private ICommand _selectAllValuesCommand;
        private ICommand _clearAllValuesCommand;

        public ICommand ClearFilterCommand => _clearFilterCommand ??= new RelayCommand(_ => ClearFilter());
        public ICommand AddSearchTemplateCommand => _addSearchTemplateCommand ??= new RelayCommand(_ => AddSearchTemplate());
        public ICommand RemoveSearchTemplateCommand => _removeSearchTemplateCommand ??= new RelayCommand(template => RemoveSearchTemplate(template));
        public ICommand SelectAllValuesCommand => _selectAllValuesCommand ??= new RelayCommand(_ => ToggleSelectAllValues());
        public ICommand ClearAllValuesCommand => _clearAllValuesCommand ??= new RelayCommand(_ => FilterValueManager?.ClearAllCommand?.Execute(null));

        private void ToggleSelectAllValues()
        {
            if (FilterValueManager == null) return;

            // If all checked → clear all. Otherwise (mixed or none) → select all.
            if (FilterValueManager.SelectAllState == true)
                FilterValueManager.ClearAllCommand?.Execute(null);
            else
                FilterValueManager.SelectAllCommand?.Execute(null);
        }

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

            // Unsubscribe old
            if (_tabControl != null)
                _tabControl.SelectionChanged -= OnTabSelectionChanged;
            if (_resizeBottomLeft != null)
                _resizeBottomLeft.DragDelta -= OnResizeBottomLeftDrag;
            if (_resizeBottomRight != null)
                _resizeBottomRight.DragDelta -= OnResizeBottomRightDrag;

            // Subscribe to tab changes to sync Values tab when switching to it
            _tabControl = GetTemplateChild("PART_TabControl") as System.Windows.Controls.TabControl;
            if (_tabControl != null)
            {
                _lastTabIndex = _tabControl.SelectedIndex;
                _tabControl.SelectionChanged += OnTabSelectionChanged;
            }

            _resizeBottomLeft = GetTemplateChild("PART_ResizeBottomLeft") as Thumb;
            _resizeBottomRight = GetTemplateChild("PART_ResizeBottomRight") as Thumb;
            if (_resizeBottomLeft != null)
                _resizeBottomLeft.DragDelta += OnResizeBottomLeftDrag;
            if (_resizeBottomRight != null)
                _resizeBottomRight.DragDelta += OnResizeBottomRightDrag;
        }

        private System.Windows.Controls.TabControl _tabControl;
        private int _lastTabIndex = -1;
        private Thumb _resizeBottomLeft;
        private Thumb _resizeBottomRight;

        private void OnResizeBottomRightDrag(object sender, DragDeltaEventArgs e)
        {
            ApplyResize(e.HorizontalChange, e.VerticalChange, fromLeft: false);
        }

        private void OnResizeBottomLeftDrag(object sender, DragDeltaEventArgs e)
        {
            ApplyResize(-e.HorizontalChange, e.VerticalChange, fromLeft: true);
        }

        private void ApplyResize(double deltaWidth, double deltaHeight, bool fromLeft)
        {
            double currentWidth = double.IsNaN(Width) ? ActualWidth : Width;
            double currentHeight = double.IsNaN(Height) ? ActualHeight : Height;

            double minWidth = MinWidth > 0 ? MinWidth : 0;
            double maxWidth = double.IsInfinity(MaxWidth) ? double.PositiveInfinity : MaxWidth;
            double minHeight = MinHeight > 0 ? MinHeight : 0;
            double maxHeight = double.IsInfinity(MaxHeight) ? double.PositiveInfinity : MaxHeight;

            double newWidth = Math.Max(minWidth, Math.Min(maxWidth, currentWidth + deltaWidth));
            double newHeight = Math.Max(minHeight, Math.Min(maxHeight, currentHeight + deltaHeight));

            double actualDeltaWidth = newWidth - currentWidth;

            Width = newWidth;
            Height = newHeight;

            if (fromLeft && actualDeltaWidth != 0)
            {
                var popup = FindParentPopup();
                if (popup != null)
                    popup.HorizontalOffset -= actualDeltaWidth;
            }
        }

        private Popup FindParentPopup()
        {
            DependencyObject parent = LogicalTreeHelper.GetParent(this);
            while (parent != null && parent is not Popup)
                parent = LogicalTreeHelper.GetParent(parent);
            return parent as Popup;
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tabControl == null) return;

            int currentIndex = _tabControl.SelectedIndex;
            if (currentIndex == _lastTabIndex) return; // No actual tab change
            _lastTabIndex = currentIndex;

            if (currentIndex == 1 && FilterValueManager != null)
            {
                // Switched to Filter Values tab — sync checkbox states from current rules
                FilterValueManager.SyncFromRules();
            }
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

            if (SearchTemplateController != null)
            {
                // Subscribe to auto-apply for immediate filter application on non-typing changes
                // (dropdown selections, SearchType combo changes, etc.)
                SearchTemplateController.AutoApplyFilter -= OnAutoApplyFilter;
                SearchTemplateController.AutoApplyFilter += OnAutoApplyFilter;

                // Initialize the Filter Values tab manager
                int totalItemCount = 0;
                if (DataContext is ColumnFilterControl csb && csb.SourceDataGrid != null)
                    totalItemCount = csb.SourceDataGrid.OriginalItemsCount;

                FilterValueManager = new FilterValueManager();
                FilterValueManager.Initialize(SearchTemplateController, totalItemCount, ApplyFilter);
                FilterValueManager.FilterApplyRequested += OnFilterValueManagerApplyRequested;
                OnPropertyChanged(nameof(FilterValueManager));

                SelectInitialTab();
            }
        }

        /// <summary>
        /// Columns with discrete/bounded value sets (enum, datetime, or few unique values)
        /// default to the Filter Values tab rather than Filter Rules.
        /// </summary>
        private const int FewUniqueValuesThreshold = 20;

        private void SelectInitialTab()
        {
            if (_tabControl == null || SearchTemplateController == null) return;

            var dataType = SearchTemplateController.ColumnDataType;
            bool preferValuesTab =
                dataType == ColumnDataType.Enum ||
                dataType == ColumnDataType.DateTime ||
                (SearchTemplateController.ColumnValueCounts?.Count ?? 0) <= FewUniqueValuesThreshold;

            if (preferValuesTab && _tabControl.Items.Count > 1)
                _tabControl.SelectedIndex = 1;
        }

        private void OnAutoApplyFilter(object sender, EventArgs e)
        {
            // Don't apply during FilterValueManager sync — it adds/removes templates
            // which triggers AutoApplyFilter repeatedly. The checkbox handler applies after sync.
            if (FilterValueManager != null && FilterValueManager.IsSyncing)
                return;

            // Also skip during initial load
            if (!_isInitialized)
                return;

            if (IsLiveApplyEnabled)
            {
                ApplyFilter();
            }
            else
            {
                // Deferred mode — keep the controller's expression current (so HasCustomExpression
                // / "active filter" indicators stay honest) but skip the grid filter pass. The
                // popup-close PruneAndApply path will run ApplyFilter once when the user finishes.
                SearchTemplateController?.UpdateFilterExpression();
            }
        }

        private void OnFilterValueManagerApplyRequested(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
            if (IsLiveApplyEnabled)
                ApplyFilter();
            else
                SearchTemplateController?.UpdateFilterExpression();
        }

        private void TriggerColumnValueLoading()
        {
            try
            {
                if (DataContext is ColumnFilterControl columnSearchBox &&
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

            if (DataContext is ColumnFilterControl currentColumnSearchBox &&
                currentColumnSearchBox.SourceDataGrid != null
                && currentColumnSearchBox.GridColumn != null)
            {
                var dataGrid = currentColumnSearchBox.SourceDataGrid;
                var currentDescriptor = currentColumnSearchBox.GridColumn;
                int currentIndex = currentDescriptor.InternalColumn?.DisplayIndex >= 0
                    ? currentDescriptor.InternalColumn.DisplayIndex
                    : int.MaxValue;

                // Iterate GridColumn descriptors (the persistent state holders) rather than
                // DataColumns. DataColumns contains only the live, on-screen ColumnSearchBox
                // instances — a column with a filter that has been scrolled off the viewport
                // won't appear there, so the previous version mis-decided visibility whenever
                // a preceding column with a filter happened to be virtualized out.
                if (dataGrid.GridColumns != null)
                {
                    foreach (var descriptor in dataGrid.GridColumns)
                    {
                        if (descriptor == currentDescriptor) continue;

                        int otherIndex = descriptor.InternalColumn?.DisplayIndex >= 0
                            ? descriptor.InternalColumn.DisplayIndex
                            : int.MaxValue;

                        if (otherIndex < currentIndex
                            && descriptor.SearchTemplateController?.HasCustomExpression == true)
                        {
                            IsOperatorVisible = true;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies the current filter rules to the grid immediately.
        /// Called by: dropdown selections, checkbox changes, SearchType combo changes, etc.
        /// </summary>
        public void ApplyFilter()
        {
            if (SearchTemplateController == null) return;

            try
            {
                SearchTemplateController.UpdateFilterExpression();

                // Try DataContext first, then walk visual tree to find the grid
                ColumnFilterControl columnSearchBox = DataContext as ColumnFilterControl;
                SearchDataGrid grid = columnSearchBox?.SourceDataGrid;

                if (grid != null)
                {
                    columnSearchBox.HasAdvancedFilter = SearchTemplateController.HasCustomExpression;
                    grid.FilterItemsSource();
                    grid.UpdateFilterPanel();
                }

                FiltersApplied?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyFilter failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Prunes incomplete/invalid templates on popup close, then applies final filter.
        /// The filter was already applied incrementally during editing; this is a final cleanup pass.
        /// </summary>
        private void PruneAndApply()
        {
            if (SearchTemplateController == null) return;

            // Unsubscribe from events
            SearchTemplateController.AutoApplyFilter -= OnAutoApplyFilter;
            if (FilterValueManager != null)
            {
                FilterValueManager.FilterApplyRequested -= OnFilterValueManagerApplyRequested;
                FilterValueManager.UnsubscribeFromControllerChanges();
            }

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

                // Final apply after pruning
                ApplyFilter();
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

                if (DataContext is ColumnFilterControl columnSearchBox && columnSearchBox.SourceDataGrid != null)
                {
                    columnSearchBox.HasAdvancedFilter = false;
                    columnSearchBox.SourceDataGrid.FilterItemsSource();
                    columnSearchBox.SourceDataGrid.UpdateFilterPanel();
                }

                // Reset Filter Values tab to all-checked (no filter)
                FilterValueManager?.SyncFromRules();

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
