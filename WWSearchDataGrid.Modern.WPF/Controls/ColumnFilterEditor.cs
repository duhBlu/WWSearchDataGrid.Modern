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

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(ColumnFilterEditor),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(ColumnFilterEditor),
                new PropertyMetadata(0.0));

        #endregion

        #region Properties

        public SearchTemplateController SearchTemplateController { get; set; }

        /// <summary>
        /// Manages the Filter Values tab state (checkbox list, sync with rules).
        /// </summary>
        public FilterValueManager FilterValueManager { get; private set; }

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

        #endregion

        #region Events

        public event EventHandler FiltersApplied;
        public event EventHandler FiltersCleared;

        #endregion

        #region Commands

        private ICommand _clearFilterCommand;
        private ICommand _selectAllValuesCommand;
        private ICommand _clearAllValuesCommand;

        public ICommand ClearFilterCommand => _clearFilterCommand ??= new RelayCommand(_ => ClearFilter());
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
            EnsureSingleRule(SearchTemplateController);

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

            if (ResolveEnableLiveFiltering())
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
            if (ResolveEnableLiveFiltering())
                ApplyFilter();
            else
                SearchTemplateController?.UpdateFilterExpression();
        }

        /// <summary>
        /// Reads <see cref="SearchDataGrid.EnableLiveFiltering"/> via the hosting
        /// <see cref="ColumnFilterControl"/>. Defaults to <c>true</c> when the grid isn't
        /// reachable yet (matches the grid DP's default).
        /// </summary>
        private bool ResolveEnableLiveFiltering()
            => DataContext is ColumnFilterControl csb
               && csb.SourceDataGrid != null
                ? csb.SourceDataGrid.EnableLiveFiltering
                : true;

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

        /// <summary>
        /// Forces the controller to a single-rule shape on open. The column editor is now
        /// single-rule only; the FilterPanel's Filter Editor window owns multi-rule and
        /// cross-column composition. If the column's controller carries pre-existing
        /// multi-template state (e.g. from a previous build, the auto-filter row, or the
        /// Filter Values panel), try to fold it into one template — falling back to a
        /// default empty template when the merge isn't lossless.
        /// </summary>
        /// <remarks>
        /// Heuristic:
        /// <list type="bullet">
        /// <item>Already single template: no-op.</item>
        /// <item>One live filter spread across groups: keep the live one.</item>
        /// <item>Multiple <see cref="SearchType.Equals"/> templates on a non-DateTime column,
        ///   or any mix of <see cref="SearchType.Equals"/> / <see cref="SearchType.IsAnyOf"/>:
        ///   merge values into one <see cref="SearchType.IsAnyOf"/>.</item>
        /// <item>Multiple <see cref="SearchType.Equals"/> templates on a DateTime column,
        ///   or any mix with <see cref="SearchType.IsOnAnyOfDates"/>: merge dates into one
        ///   <see cref="SearchType.IsOnAnyOfDates"/>.</item>
        /// <item>Anything else (Contains stacks, mixed comparators, Between/Not-Between
        ///   combinations): reset to a default empty template.</item>
        /// </list>
        /// </remarks>
        private static void EnsureSingleRule(SearchTemplateController controller)
        {
            if (controller == null) return;

            if (controller.SearchGroups.Count == 1
                && controller.SearchGroups[0].SearchTemplates.Count == 1)
            {
                return;
            }

            var liveTemplates = controller.SearchGroups
                .SelectMany(g => g.SearchTemplates)
                .Where(t => t.HasCustomFilter)
                .ToList();

            if (liveTemplates.Count == 0)
            {
                controller.ClearAndReset();
                return;
            }

            if (liveTemplates.Count == 1)
            {
                var snapshot = TemplateSnapshot.Capture(liveTemplates[0]);
                controller.ClearAndReset();
                snapshot.RestoreInto(controller.SearchGroups[0].SearchTemplates[0]);
                return;
            }

            var merged = TryMergeIntoSingleTemplate(liveTemplates, controller.ColumnDataType);
            controller.ClearAndReset();
            merged?.RestoreInto(controller.SearchGroups[0].SearchTemplates[0]);
        }

        private static TemplateSnapshot TryMergeIntoSingleTemplate(
            IReadOnlyList<SearchTemplate> templates,
            ColumnDataType columnDataType)
        {
            // Date columns: combine Equals + IsOnAnyOfDates dates into one IsOnAnyOfDates.
            if (columnDataType == ColumnDataType.DateTime
                && templates.All(t => t.SearchType == SearchType.Equals
                                   || t.SearchType == SearchType.IsOnAnyOfDates))
            {
                var dates = new List<DateTime>();
                foreach (var t in templates)
                {
                    if (t.SearchType == SearchType.Equals && t.SelectedValue is DateTime dt)
                        dates.Add(dt);
                    else if (t.SelectedDates != null)
                        foreach (var d in t.SelectedDates) dates.Add(d);
                }
                if (dates.Count == 0) return null;
                return new TemplateSnapshot
                {
                    SearchType = SearchType.IsOnAnyOfDates,
                    Dates = dates.Distinct().ToList()
                };
            }

            // Non-date columns: combine Equals + IsAnyOf into one IsAnyOf.
            if (templates.All(t => t.SearchType == SearchType.Equals
                                || t.SearchType == SearchType.IsAnyOf))
            {
                var values = new List<object>();
                foreach (var t in templates)
                {
                    if (t.SearchType == SearchType.Equals)
                    {
                        if (t.SelectedValue != null) values.Add(t.SelectedValue);
                    }
                    else if (t.SelectedValues != null)
                    {
                        foreach (var v in t.SelectedValues) if (v?.Value != null) values.Add(v.Value);
                    }
                }
                if (values.Count == 0) return null;
                return new TemplateSnapshot
                {
                    SearchType = SearchType.IsAnyOf,
                    Values = values.Distinct().ToList()
                };
            }

            return null;
        }

        private sealed class TemplateSnapshot
        {
            public SearchType SearchType;
            public object PrimaryValue;
            public object SecondaryValue;
            public List<object> Values;
            public List<DateTime> Dates;

            public static TemplateSnapshot Capture(SearchTemplate t) => new TemplateSnapshot
            {
                SearchType = t.SearchType,
                PrimaryValue = t.SelectedValue,
                SecondaryValue = t.SelectedSecondaryValue,
                Values = t.SelectedValues?
                    .Where(v => v?.Value != null)
                    .Select(v => (object)v.Value)
                    .ToList(),
                Dates = t.SelectedDates?.ToList()
            };

            public void RestoreInto(SearchTemplate target)
            {
                if (target == null) return;
                target.SearchType = SearchType;
                if (PrimaryValue != null) target.SelectedValue = PrimaryValue;
                if (SecondaryValue != null) target.SelectedSecondaryValue = SecondaryValue;
                if (Values != null && target.SelectedValues != null)
                {
                    target.SelectedValues.Clear();
                    foreach (var v in Values) target.SelectedValues.Add(new SelectableValueItem(v));
                }
                if (Dates != null && target.SelectedDates != null)
                {
                    target.SelectedDates.Clear();
                    foreach (var d in Dates) target.SelectedDates.Add(d);
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
