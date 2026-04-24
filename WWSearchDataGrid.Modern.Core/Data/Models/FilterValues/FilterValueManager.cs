using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Manages the Filter Values tab state and bidirectional sync with filter rules.
    /// Translates checkbox selections into IsAnyOf/IsNoneOf/Equals search templates
    /// and vice versa.
    /// </summary>
    public class FilterValueManager : ObservableObject
    {
        #region Events

        /// <summary>
        /// Raised when the filter should be applied to the grid (after checkbox changes, Select All, etc.).
        /// The ColumnFilterEditor subscribes to this to call ApplyFilter().
        /// </summary>
        public event EventHandler FilterApplyRequested;

        #endregion

        #region Fields

        private SearchTemplateController _controller;
        private bool _isSyncing;
        private Action _applyFilterAction;

        /// <summary>
        /// Whether the manager is currently syncing (adding/removing templates).
        /// Used by the editor to suppress AutoApplyFilter during batch template operations.
        /// </summary>
        public bool IsSyncing => _isSyncing;
        private string _searchText = string.Empty;
        private bool? _selectAllState = true;
        private ICommand _selectAllCommand;
        private ICommand _clearAllCommand;

        #endregion

        #region Properties

        /// <summary>
        /// Flat list of all checkable column values.
        /// </summary>
        public ObservableCollection<CheckableValueItem> ValueItems { get; } = new ObservableCollection<CheckableValueItem>();

        /// <summary>
        /// Year-level root nodes for DateTime columns. Empty for non-DateTime.
        /// </summary>
        public ObservableCollection<DateTreeGroupItem> DateTreeRoots { get; } = new ObservableCollection<DateTreeGroupItem>();

        /// <summary>
        /// Whether this column contains DateTime values (drives tree vs flat list UI).
        /// </summary>
        public bool IsDateTimeColumn { get; private set; }

        /// <summary>
        /// Tri-state for the Select All checkbox. True=all, False=none, null=mixed.
        /// Setting to true calls SelectAll, setting to false calls ClearAll.
        /// </summary>
        public bool? SelectAllState
        {
            get => _selectAllState;
            private set => SetProperty(value, ref _selectAllState);
        }

        /// <summary>
        /// Search text for filtering the value list.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(value, ref _searchText))
                    ApplySearchFilter();
            }
        }

        /// <summary>
        /// Number of currently checked items.
        /// </summary>
        public int CheckedCount => ValueItems.Count(v => v.IsChecked);

        /// <summary>
        /// Total number of items.
        /// </summary>
        public int TotalCount => ValueItems.Count;

        public ICommand SelectAllCommand => _selectAllCommand ?? (_selectAllCommand = new RelayCommand(_ => SelectAll()));
        public ICommand ClearAllCommand => _clearAllCommand ?? (_clearAllCommand = new RelayCommand(_ => ClearAll()));

        #endregion

        #region Initialization

        /// <summary>
        /// Builds the value items from the controller's column data and syncs initial state from rules.
        /// </summary>
        /// <summary>
        /// Builds the value items from the controller's column data and syncs initial state from rules.
        /// </summary>
        /// <param name="controller">The search template controller for this column</param>
        /// <param name="totalItemCount">Total number of rows in the data source (for calculating null count)</param>
        /// <param name="applyFilterAction">Optional callback invoked after checkbox changes to apply the filter immediately</param>
        public void Initialize(SearchTemplateController controller, int totalItemCount = 0, Action applyFilterAction = null)
        {
            // Unsubscribe from previous controller's collection changes
            UnsubscribeFromControllerChanges();

            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _applyFilterAction = applyFilterAction;

            // Unsubscribe from old items
            foreach (var item in ValueItems)
                item.PropertyChanged -= OnValueItemCheckedChanged;

            ValueItems.Clear();
            DateTreeRoots.Clear();

            // Ensure values are loaded
            controller.EnsureColumnValuesLoadedForFiltering();

            IsDateTimeColumn = controller.ColumnDataType == ColumnDataType.DateTime;

            // Build flat checkable items from column values
            var rawValues = controller.ColumnValues;
            var counts = controller.ColumnValueCounts;

            for (int i = 0; i < rawValues.Count; i++)
            {
                var raw = rawValues[i];
                string display = controller.GetDisplayValue(raw);
                int count = 0;
                if (counts != null && counts.ContainsKey(raw))
                    count = counts[raw];

                var item = new CheckableValueItem
                {
                    RawValue = raw,
                    DisplayValue = display,
                    Count = count,
                    IsChecked = true, // default: all checked = no filter
                    IsBlank = false
                };

                item.PropertyChanged += OnValueItemCheckedChanged;
                ValueItems.Add(item);
            }

            // Add blank item at the TOP if column has nulls
            if (controller.ContainsNullValues)
            {
                // Null count = total rows - sum of all non-null value counts
                int nonNullCount = counts != null ? counts.Values.Sum() : 0;
                int nullCount = Math.Max(0, totalItemCount - nonNullCount);

                var blankItem = new CheckableValueItem
                {
                    RawValue = null,
                    DisplayValue = "(Blank)",
                    Count = nullCount,
                    IsChecked = true,
                    IsBlank = true
                };
                blankItem.PropertyChanged += OnValueItemCheckedChanged;
                ValueItems.Insert(0, blankItem);
            }

            // Build DateTime tree if applicable
            if (IsDateTimeColumn)
                BuildDateTree();

            // Sync initial state from any existing rules
            SyncFromRules();

            // Watch for external template removals (filter panel chip X, context menu, etc.)
            SubscribeToControllerChanges();

            UpdateSelectAllState();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(CheckedCount));
            OnPropertyChanged(nameof(IsDateTimeColumn));
        }

        /// <summary>
        /// Unsubscribes from all controller collection change events.
        /// Call before re-initializing or when the manager is being disposed.
        /// </summary>
        public void UnsubscribeFromControllerChanges()
        {
            if (_controller?.SearchGroups == null) return;

            _controller.SearchGroups.CollectionChanged -= OnSearchGroupsCollectionChanged;
            foreach (var group in _controller.SearchGroups)
                group.SearchTemplates.CollectionChanged -= OnTemplatesCollectionChanged;
        }

        private void SubscribeToControllerChanges()
        {
            if (_controller?.SearchGroups == null) return;

            _controller.SearchGroups.CollectionChanged += OnSearchGroupsCollectionChanged;
            foreach (var group in _controller.SearchGroups)
                group.SearchTemplates.CollectionChanged += OnTemplatesCollectionChanged;
        }

        private void OnSearchGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Track new/removed groups for template-level subscriptions
            if (e.OldItems != null)
                foreach (SearchTemplateGroup g in e.OldItems)
                    g.SearchTemplates.CollectionChanged -= OnTemplatesCollectionChanged;
            if (e.NewItems != null)
                foreach (SearchTemplateGroup g in e.NewItems)
                    g.SearchTemplates.CollectionChanged += OnTemplatesCollectionChanged;

            // If groups were removed externally (not by us), sync checkboxes from the new rule state
            if (!_isSyncing && (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Reset))
            {
                SyncFromRules();
            }
        }

        private void OnTemplatesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // If templates were removed externally (not by SyncToRules), sync checkboxes
            if (!_isSyncing && (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Reset))
            {
                SyncFromRules();
            }
        }

        #endregion

        #region Values → Rules Sync

        /// <summary>
        /// Reads checkbox states and creates the optimal set of value-filter templates.
        /// Uses multiple templates joined with OR when that produces cleaner rules
        /// (e.g., "IsNull OR = 'A'" instead of a long IsNoneOf list).
        ///
        /// Strategy:
        ///   - Always clear all value-filter templates first, then build fresh
        ///   - Blank (null) handling is always a separate IsNull/IsNotNull template
        ///   - Non-blank values use Equals/IsAnyOf/IsNoneOf (whichever is shortest)
        ///   - Multiple templates are joined with OR within the same group
        /// </summary>
        public void SyncToRules()
        {
            if (_isSyncing || _controller == null) return;

            _isSyncing = true;
            try
            {
                // Remove all value-related templates, empty defaults, and clean up groups.
                // Uses direct collection removal to avoid controller's auto-repopulate behavior.
                ClearTemplatesForValueSync();

                var checkedNonBlank = ValueItems.Where(v => v.IsChecked && !v.IsBlank).ToList();
                var uncheckedNonBlank = ValueItems.Where(v => !v.IsChecked && !v.IsBlank).ToList();
                int totalNonBlank = ValueItems.Count(v => !v.IsBlank);

                bool blankExists = ValueItems.Any(v => v.IsBlank);
                bool blankChecked = blankExists && ValueItems.First(v => v.IsBlank).IsChecked;

                // ── ALL CHECKED or ALL UNCHECKED → no filter needed ──
                // Both extremes clear the filter. All-checked is the natural "no filter" state.
                // All-unchecked is treated as a reset (not "show nothing") so toggling
                // Select All is instantaneous regardless of dataset size.
                if (checkedNonBlank.Count == totalNonBlank && (!blankExists || blankChecked))
                    return;
                if (checkedNonBlank.Count == 0 && (!blankExists || !blankChecked))
                    return;

                // ── Handle blank (null) ──
                if (blankChecked && checkedNonBlank.Count < totalNonBlank)
                    AddValueFilterTemplate(SearchType.IsNull, null, null);

                // ── Handle non-blank values ──
                if (checkedNonBlank.Count == 0)
                    return;

                if (checkedNonBlank.Count == totalNonBlank)
                {
                    if (blankExists && !blankChecked)
                        AddValueFilterTemplate(SearchType.IsNotNull, null, null);
                    return;
                }

                // Some non-blank values checked: pick shortest representation
                bool isFirst = !blankChecked;
                string operatorName = isFirst ? null : "Or";

                if (IsDateTimeColumn)
                {
                    // DateTime: use Between for contiguous value ranges, IsAnyOf/IsNoneOf for scattered values
                    BuildDateTimeTemplates(checkedNonBlank, uncheckedNonBlank, blankExists, blankChecked, operatorName);
                }
                else if (checkedNonBlank.Count == 1)
                {
                    AddValueFilterTemplate(SearchType.Equals, checkedNonBlank[0].RawValue, null, operatorName);
                }
                else if (uncheckedNonBlank.Count == 1 && (!blankExists || blankChecked))
                {
                    AddValueFilterTemplate(SearchType.NotEquals, uncheckedNonBlank[0].RawValue, null, operatorName);
                }
                else if (uncheckedNonBlank.Count == 1 && blankExists && !blankChecked)
                {
                    var values = checkedNonBlank.Select(v => new SelectableValueItem(v.RawValue)).ToList();
                    AddValueFilterTemplate(SearchType.IsAnyOf, null, values, operatorName);
                }
                else if (checkedNonBlank.Count <= uncheckedNonBlank.Count)
                {
                    var values = checkedNonBlank.Select(v => new SelectableValueItem(v.RawValue)).ToList();
                    AddValueFilterTemplate(SearchType.IsAnyOf, null, values, operatorName);
                }
                else
                {
                    if (blankExists && !blankChecked)
                    {
                        var anyOfValues = checkedNonBlank.Select(v => new SelectableValueItem(v.RawValue)).ToList();
                        AddValueFilterTemplate(SearchType.IsAnyOf, null, anyOfValues, operatorName);
                    }
                    else
                    {
                        var values = uncheckedNonBlank.Select(v => new SelectableValueItem(v.RawValue)).ToList();
                        AddValueFilterTemplate(SearchType.IsNoneOf, null, values, operatorName);
                    }
                }
            }
            finally
            {
                _isSyncing = false;
                UpdateSelectAllState();
                OnPropertyChanged(nameof(CheckedCount));
            }
        }

        /// <summary>
        /// Syncs checkbox states to rule templates and applies the filter to the grid.
        /// This is the method all user-initiated actions should call.
        /// The apply is separate from SyncToRules to avoid re-entrancy with AutoApplyFilter.
        /// </summary>
        private void SyncToRulesAndApply()
        {
            SyncToRules();
            _applyFilterAction?.Invoke();
            FilterApplyRequested?.Invoke(this, EventArgs.Empty);
        }

        #region Template Management

        private SearchTemplate AddValueFilterTemplate(SearchType searchType, object singleValue,
            List<SelectableValueItem> multiValues, string operatorName = null, object secondaryValue = null)
        {
            if (_controller.SearchGroups.Count == 0)
            {
                _controller.SearchGroups.Add(new SearchTemplateGroup());
            }

            var group = _controller.SearchGroups[0];
            var template = new SearchTemplate(_controller.ColumnDataType)
            {
                IsValueFilterTemplate = true,
                SearchTemplateController = _controller,
                SearchType = searchType,
                HasChanges = true
            };

            if (operatorName != null)
                template.OperatorName = operatorName;

            if (singleValue != null)
                template.SelectedValue = singleValue;

            if (secondaryValue != null)
                template.SelectedSecondaryValue = secondaryValue;

            if (multiValues != null)
            {
                if (template.SelectedValues == null)
                    template.SelectedValues = new System.Collections.ObjectModel.ObservableCollection<SelectableValueItem>();
                foreach (var v in multiValues)
                {
                    if (v != null)
                        template.SelectedValues.Add(v);
                }
            }

            _controller.SubscribeToTemplateChanges(template);
            group.SearchTemplates.Add(template);
            return template;
        }

        private void RemoveAllValueFilterTemplates()
        {
            if (_controller?.SearchGroups == null) return;

            foreach (var group in _controller.SearchGroups.ToList())
            {
                var toRemove = group.SearchTemplates.Where(t => t.IsValueFilterTemplate).ToList();
                foreach (var template in toRemove)
                {
                    _controller.RemoveSearchTemplate(template);
                }
            }
        }

        /// <summary>
        /// Removes all value-mappable and empty/default templates directly from groups
        /// WITHOUT using controller.RemoveSearchTemplate (which auto-creates new groups/templates
        /// when a group becomes empty). Cleans up empty groups but does NOT auto-repopulate.
        /// </summary>
        private void ClearTemplatesForValueSync()
        {
            if (_controller?.SearchGroups == null) return;

            var valueMappableTypes = new HashSet<SearchType>
            {
                SearchType.Equals, SearchType.NotEquals,
                SearchType.IsAnyOf, SearchType.IsNoneOf,
                SearchType.IsNull, SearchType.IsNotNull,
                SearchType.Between, SearchType.NotBetween,
                SearchType.BetweenDates, SearchType.NotBetweenDates
            };

            foreach (var group in _controller.SearchGroups.ToList())
            {
                var toRemove = group.SearchTemplates
                    .Where(t => t.IsValueFilterTemplate
                        || valueMappableTypes.Contains(t.SearchType)
                        || !t.IsValidFilter)
                    .ToList();

                foreach (var template in toRemove)
                {
                    // Remove directly from the collection — bypasses auto-repopulate logic
                    group.SearchTemplates.Remove(template);
                }

                // Remove empty groups but do NOT auto-create replacements
                if (group.SearchTemplates.Count == 0)
                {
                    _controller.SearchGroups.Remove(group);
                }
            }
        }

        private List<SearchTemplate> FindAllValueFilterTemplates()
        {
            var result = new List<SearchTemplate>();
            if (_controller?.SearchGroups == null) return result;

            foreach (var group in _controller.SearchGroups)
            {
                foreach (var template in group.SearchTemplates)
                {
                    if (template.IsValueFilterTemplate)
                        result.Add(template);
                }
            }
            return result;
        }

        #endregion

        #endregion

        #region Rules → Values Sync

        /// <summary>
        /// Reads ALL value-mappable templates (not just IsValueFilterTemplate ones) and sets
        /// checkbox states accordingly. This means manually created Equals/NotEquals/IsAnyOf/IsNoneOf
        /// rules will properly reflect in the Values tab.
        /// Non-value rules (Contains, GreaterThan, etc.) are ignored — they can't be represented as checkboxes.
        /// </summary>
        public void SyncFromRules()
        {
            if (_isSyncing || _controller == null) return;

            _isSyncing = true;
            try
            {
                // Check if there are any valid filter templates at all
                bool hasAnyFilter = false;
                if (_controller.SearchGroups != null)
                {
                    foreach (var group in _controller.SearchGroups)
                    {
                        foreach (var template in group.SearchTemplates)
                        {
                            if (template.IsValidFilter)
                            {
                                hasAnyFilter = true;
                                break;
                            }
                        }
                        if (hasAnyFilter) break;
                    }
                }

                if (!hasAnyFilter)
                {
                    // No active filters = all checked
                    foreach (var item in ValueItems)
                        item.IsChecked = true;
                }
                else
                {
                    // Evaluate each value against the compiled filter expression.
                    // This handles ALL search types (Contains, Between, GreaterThan, Equals, etc.)
                    // by actually running the filter logic against each value.
                    _controller.UpdateFilterExpression();
                    var filterExpression = _controller.FilterExpression;

                    // Collection-context filters (AboveAverage, Unique, etc.) produce a null
                    // FilterExpression because they can't be compiled without dataset context.
                    // Build a temporary CollectionContext from the value items so we can
                    // evaluate each checkbox against the real filter logic.
                    bool hasCollectionContextFilters = HasCollectionContextTemplates();
                    CollectionContext tempContext = null;
                    if (filterExpression == null && hasCollectionContextFilters)
                    {
                        tempContext = BuildCollectionContextFromValueItems();
                    }

                    foreach (var item in ValueItems)
                    {
                        var value = item.IsBlank ? null : item.RawValue;

                        if (filterExpression != null)
                        {
                            try { item.IsChecked = filterExpression(value); }
                            catch { item.IsChecked = item.IsBlank ? false : true; }
                        }
                        else if (tempContext != null)
                        {
                            try { item.IsChecked = _controller.EvaluateWithCollectionContext(value, tempContext); }
                            catch { item.IsChecked = true; }
                        }
                        else
                        {
                            item.IsChecked = true;
                        }
                    }
                }

                // Update DateTime tree states from leaf values (depth-first)
                if (IsDateTimeColumn)
                {
                    foreach (var root in DateTreeRoots)
                        root.RefreshCheckStateFromLeaves();
                }
            }
            finally
            {
                _isSyncing = false;
                UpdateSelectAllState();
                OnPropertyChanged(nameof(CheckedCount));
            }
        }

        private void ApplyInclusionTemplate(SearchTemplate template)
        {
            switch (template.SearchType)
            {
                case SearchType.Equals:
                    string eqValue = template.SelectedValue?.ToString() ?? "";
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank && string.Equals(item.RawValue?.ToString(), eqValue, StringComparison.OrdinalIgnoreCase))
                            item.IsChecked = true;
                    }
                    break;

                case SearchType.IsAnyOf:
                    var includeSet = new HashSet<string>(
                        (template.SelectedValues ?? Enumerable.Empty<SelectableValueItem>()).Select(v => v?.Value ?? ""),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank && includeSet.Contains(item.RawValue?.ToString() ?? ""))
                            item.IsChecked = true;
                    }
                    break;

                case SearchType.IsNull:
                    foreach (var item in ValueItems)
                    {
                        if (item.IsBlank)
                            item.IsChecked = true;
                    }
                    break;

                // Exclusion types in inclusion mode: treat NotEquals as "include everything except"
                case SearchType.NotEquals:
                    string neValue = template.SelectedValue?.ToString() ?? "";
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank && !string.Equals(item.RawValue?.ToString(), neValue, StringComparison.OrdinalIgnoreCase))
                            item.IsChecked = true;
                    }
                    break;

                case SearchType.IsNoneOf:
                    var excludeSet = new HashSet<string>(
                        (template.SelectedValues ?? Enumerable.Empty<SelectableValueItem>()).Select(v => v?.Value ?? ""),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank && !excludeSet.Contains(item.RawValue?.ToString() ?? ""))
                            item.IsChecked = true;
                    }
                    break;

                case SearchType.IsNotNull:
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank)
                            item.IsChecked = true;
                    }
                    break;
            }
        }

        private void ApplyExclusionTemplate(SearchTemplate template)
        {
            switch (template.SearchType)
            {
                case SearchType.NotEquals:
                    string neValue = template.SelectedValue?.ToString() ?? "";
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank && string.Equals(item.RawValue?.ToString(), neValue, StringComparison.OrdinalIgnoreCase))
                            item.IsChecked = false;
                    }
                    break;

                case SearchType.IsNoneOf:
                    var excludeSet = new HashSet<string>(
                        (template.SelectedValues ?? Enumerable.Empty<SelectableValueItem>()).Select(v => v?.Value ?? ""),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (var item in ValueItems)
                    {
                        if (!item.IsBlank && excludeSet.Contains(item.RawValue?.ToString() ?? ""))
                            item.IsChecked = false;
                    }
                    break;

                case SearchType.IsNotNull:
                    foreach (var item in ValueItems)
                    {
                        if (item.IsBlank)
                            item.IsChecked = false;
                    }
                    break;
            }
        }

        #endregion

        #region DateTime Tree

        /// <summary>
        /// For DateTime columns, generates real Between/NotBetween templates for contiguous
        /// value ranges and Equals/NotEquals/IsAnyOf/IsNoneOf for isolated values.
        /// All templates use actual DateTime values — no display overrides.
        /// Picks the smaller side (checked vs unchecked) for fewer templates.
        /// </summary>
        private void BuildDateTimeTemplates(
            List<CheckableValueItem> checkedItems, List<CheckableValueItem> uncheckedItems,
            bool blankExists, bool blankChecked, string operatorName)
        {
            // Simple single-value cases
            if (checkedItems.Count == 1)
            {
                AddValueFilterTemplate(SearchType.Equals, checkedItems[0].RawValue, null, operatorName);
                return;
            }
            if (uncheckedItems.Count == 1)
            {
                AddValueFilterTemplate(SearchType.NotEquals, uncheckedItems[0].RawValue, null, operatorName);
                return;
            }

            // Pick the smaller side for fewer templates
            bool useExclusion = uncheckedItems.Count < checkedItems.Count && (!blankExists || blankChecked);
            var items = useExclusion ? uncheckedItems : checkedItems;

            // Sort the DateTime values and find contiguous runs
            // "Contiguous" = adjacent in the sorted column value list (no gaps in actual data)
            var allSorted = ValueItems.Where(v => !v.IsBlank && v.RawValue is DateTime)
                .OrderBy(v => (DateTime)v.RawValue).ToList();
            var itemSet = new HashSet<CheckableValueItem>(items);

            var ranges = FindContiguousValueRanges(allSorted, itemSet);

            // Separate multi-value ranges (Between) from isolated single values (group into IsAnyOf/IsNoneOf)
            var singleValues = new List<CheckableValueItem>();
            var multiRanges = new List<List<CheckableValueItem>>();

            foreach (var range in ranges)
            {
                if (range.Count <= 2)
                    singleValues.AddRange(range);
                else
                    multiRanges.Add(range);
            }

            // Emit Between templates for contiguous ranges
            bool isFirstTemplate = true;
            foreach (var range in multiRanges)
            {
                string op = isFirstTemplate ? operatorName : (useExclusion ? "And" : "Or");
                var first = (DateTime)range[0].RawValue;
                var last = (DateTime)range[range.Count - 1].RawValue;
                var type = useExclusion ? SearchType.NotBetweenDates : SearchType.BetweenDates;
                AddValueFilterTemplate(type, first, null, op, last);
                isFirstTemplate = false;
            }

            // Group isolated single values into one IsAnyOf/IsNoneOf (or Equals/NotEquals if just one)
            if (singleValues.Count == 1)
            {
                string op = isFirstTemplate ? operatorName : (useExclusion ? "And" : "Or");
                var type = useExclusion ? SearchType.NotEquals : SearchType.Equals;
                AddValueFilterTemplate(type, singleValues[0].RawValue, null, op);
            }
            else if (singleValues.Count > 1)
            {
                string op = isFirstTemplate ? operatorName : (useExclusion ? "And" : "Or");
                var type = useExclusion ? SearchType.IsNoneOf : SearchType.IsAnyOf;
                var values = singleValues.Select(v => new SelectableValueItem(v.RawValue)).ToList();
                AddValueFilterTemplate(type, null, values, op);
            }
        }

        /// <summary>
        /// Finds contiguous runs of selected items within the sorted value list.
        /// Two values are "contiguous" if they are adjacent in the sorted data — no unselected
        /// values exist between them. This produces Between ranges that exactly match the data.
        /// </summary>
        private static List<List<CheckableValueItem>> FindContiguousValueRanges(
            List<CheckableValueItem> allSorted, HashSet<CheckableValueItem> selectedSet)
        {
            var ranges = new List<List<CheckableValueItem>>();
            List<CheckableValueItem> currentRun = null;

            foreach (var item in allSorted)
            {
                if (selectedSet.Contains(item))
                {
                    if (currentRun == null)
                        currentRun = new List<CheckableValueItem>();
                    currentRun.Add(item);
                }
                else
                {
                    if (currentRun != null)
                    {
                        ranges.Add(currentRun);
                        currentRun = null;
                    }
                }
            }

            if (currentRun != null)
                ranges.Add(currentRun);

            return ranges;
        }

        private void BuildDateTree()
        {
            DateTreeRoots.Clear();

            var dateItems = ValueItems.Where(v => !v.IsBlank && v.RawValue is DateTime).ToList();
            if (dateItems.Count == 0) return;

            // Determine whether the column's display format shows time information.
            // Compare how the display provider formats the same date at 2:30 PM vs midnight —
            // if the results differ, the format includes time and we add a time level to the tree.
            bool displayShowsTime = false;
            try
            {
                var sampleWithTime = new DateTime(2000, 6, 15, 14, 30, 0);
                var sampleDateOnly = sampleWithTime.Date;
                displayShowsTime = !string.Equals(
                    _controller.GetDisplayValue(sampleWithTime),
                    _controller.GetDisplayValue(sampleDateOnly));
            }
            catch
            {
                // Fallback: check if any raw values have non-midnight times
                displayShowsTime = dateItems.Any(v => ((DateTime)v.RawValue).TimeOfDay != System.TimeSpan.Zero);
            }

            var yearGroups = dateItems
                .GroupBy(v => ((DateTime)v.RawValue).Year)
                .OrderBy(g => g.Key);

            foreach (var yearGroup in yearGroups)
            {
                var yearNode = new DateTreeGroupItem
                {
                    DisplayName = yearGroup.Key.ToString(),
                    IsExpanded = true,
                    Manager = this
                };

                var monthGroups = yearGroup
                    .GroupBy(v => ((DateTime)v.RawValue).Month)
                    .OrderBy(g => g.Key);

                foreach (var monthGroup in monthGroups)
                {
                    var monthNode = new DateTreeGroupItem
                    {
                        DisplayName = new DateTime(yearGroup.Key, monthGroup.Key, 1).ToString("MMMM"),
                        Parent = yearNode,
                        Manager = this
                    };

                    // Group by calendar date (ignoring time)
                    var dayGroups = monthGroup
                        .GroupBy(v => ((DateTime)v.RawValue).Date)
                        .OrderBy(g => g.Key.Day);

                    foreach (var dayGroup in dayGroups)
                    {
                        var dt = dayGroup.Key;
                        var dayLeaves = dayGroup.ToList();
                        var dayNode = new DateTreeGroupItem
                        {
                            DisplayName = dt.ToString("dd") + " - " + dt.ToString("ddd"),
                            Parent = monthNode,
                            TotalCount = dayLeaves.Sum(d => d.Count),
                            Manager = this
                        };

                        if (displayShowsTime)
                        {
                            // Display format shows time — create time-level children under each day
                            foreach (var timeItem in dayLeaves.OrderBy(v => (DateTime)v.RawValue))
                            {
                                var timeDt = (DateTime)timeItem.RawValue;
                                var timeNode = new DateTreeGroupItem
                                {
                                    DisplayName = timeDt.ToString("h:mm:ss tt"),
                                    Parent = dayNode,
                                    TotalCount = timeItem.Count,
                                    LeafValues = new System.Collections.Generic.List<CheckableValueItem> { timeItem },
                                    Manager = this
                                };
                                dayNode.Children.Add(timeNode);
                            }
                        }
                        else
                        {
                            // Display format is date-only — day is the leaf level
                            dayNode.LeafValues = dayLeaves;
                        }

                        monthNode.Children.Add(dayNode);
                    }

                    monthNode.TotalCount = monthGroup.Sum(d => d.Count);
                    yearNode.Children.Add(monthNode);
                }

                yearNode.TotalCount = yearNode.Children.Sum(c => c.TotalCount);
                DateTreeRoots.Add(yearNode);
            }
        }

        #endregion

        #region Search Filtering

        private void ApplySearchFilter()
        {
            string filter = _searchText?.Trim() ?? "";
            bool hasFilter = !string.IsNullOrEmpty(filter);

            foreach (var item in ValueItems)
            {
                item.IsVisible = !hasFilter ||
                    (item.DisplayValue != null && item.DisplayValue.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        #endregion

        #region Select All / Clear All

        private void SelectAll()
        {
            _isSyncing = true;
            try
            {
                bool hasSearch = !string.IsNullOrEmpty(_searchText?.Trim());
                foreach (var item in ValueItems)
                {
                    if (!hasSearch || item.IsVisible)
                        item.IsChecked = true;
                }

                if (IsDateTimeColumn)
                    foreach (var root in DateTreeRoots)
                        root.RefreshCheckStateFromLeaves();
            }
            finally
            {
                _isSyncing = false;
            }
            SyncToRulesAndApply();
        }

        private void ClearAll()
        {
            _isSyncing = true;
            try
            {
                bool hasSearch = !string.IsNullOrEmpty(_searchText?.Trim());
                foreach (var item in ValueItems)
                {
                    if (!hasSearch || item.IsVisible)
                        item.IsChecked = false;
                }

                if (IsDateTimeColumn)
                    foreach (var root in DateTreeRoots)
                        root.RefreshCheckStateFromLeaves();
            }
            finally
            {
                _isSyncing = false;
            }
            SyncToRulesAndApply();
        }

        private void UpdateSelectAllState()
        {
            if (ValueItems.Count == 0)
            {
                SelectAllState = false;
                return;
            }

            bool allChecked = ValueItems.All(v => v.IsChecked);
            bool noneChecked = ValueItems.All(v => !v.IsChecked);
            SelectAllState = allChecked ? true : noneChecked ? false : (bool?)null;
        }

        #endregion

        #region Event Handling

        private bool _batchingTreeUpdate;

        /// <summary>
        /// Called by DateTreeGroupItem when a tree node checkbox is toggled by the user.
        /// Batches all leaf value changes and syncs once at the end.
        /// </summary>
        public void OnTreeNodeCheckedByUser(DateTreeGroupItem node, bool isChecked)
        {
            if (_isSyncing) return;

            _batchingTreeUpdate = true;
            try
            {
                node.SetCheckStateRecursive(isChecked);
                // Update parent nodes upward
                node.Parent?.UpdateCheckStateFromChildren();
            }
            finally
            {
                _batchingTreeUpdate = false;
            }
            SyncToRulesAndApply();
        }

        private void OnValueItemCheckedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CheckableValueItem.IsChecked) && !_isSyncing && !_batchingTreeUpdate)
            {
                // Update DateTime tree parent nodes if a leaf checkbox changed directly
                if (IsDateTimeColumn && sender is CheckableValueItem changedItem && changedItem.RawValue is DateTime)
                {
                    // Find the leaf node (day or time) containing this item and propagate upward
                    FindAndUpdateLeafNode(DateTreeRoots, changedItem);
                }

                SyncToRulesAndApply();
            }
        }

        /// <summary>
        /// Recursively searches the tree for the leaf node containing the given item
        /// and calls UpdateCheckStateFromChildren on it.
        /// </summary>
        private static bool FindAndUpdateLeafNode(
            System.Collections.Generic.IEnumerable<DateTreeGroupItem> nodes, CheckableValueItem item)
        {
            foreach (var node in nodes)
            {
                // Check if this node directly owns the leaf
                if (node.LeafValues != null && node.LeafValues.Contains(item))
                {
                    node.UpdateCheckStateFromChildren();
                    return true;
                }

                // Recurse into children
                if (node.Children.Count > 0 && FindAndUpdateLeafNode(node.Children, item))
                    return true;
            }
            return false;
        }

        #endregion

        #region Collection-Context Helpers

        /// <summary>
        /// Checks whether any active search template requires collection context
        /// </summary>
        private bool HasCollectionContextTemplates()
        {
            if (_controller?.SearchGroups == null) return false;
            return _controller.SearchGroups
                .SelectMany(g => g.SearchTemplates)
                .Any(t => t.IsValidFilter && SearchEngine.RequiresCollectionContext(t.SearchType));
        }

        /// <summary>
        /// Builds a temporary CollectionContext from the value items so that
        /// collection-context evaluators (AboveAverage, Unique, etc.) can
        /// determine check states. Each value is expanded by its Count so the
        /// average and frequency calculations match the real dataset.
        /// </summary>
        private CollectionContext BuildCollectionContextFromValueItems()
        {
            try
            {
                // Expand values by count to reconstruct the dataset frequencies
                var expanded = new List<ValueWrapper>();
                foreach (var item in ValueItems)
                {
                    if (item.IsBlank) continue;
                    int count = Math.Max(1, item.Count);
                    for (int i = 0; i < count; i++)
                        expanded.Add(new ValueWrapper(item.RawValue));
                }

                if (expanded.Count == 0)
                    return null;

                return new CollectionContext(
                    expanded.Cast<object>().ToList(),
                    nameof(ValueWrapper.Value));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Simple wrapper so the CollectionContext can extract values via reflection
        /// using a known property path.
        /// </summary>
        private class ValueWrapper
        {
            public object Value { get; }
            public ValueWrapper(object value) { Value = value; }
        }

        #endregion
    }
}
