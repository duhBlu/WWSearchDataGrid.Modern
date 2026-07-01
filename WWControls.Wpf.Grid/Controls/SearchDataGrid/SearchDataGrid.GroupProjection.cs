using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// The grouping engine. When the grid is grouped, the group tree is projected into a single
    /// list of header sentinels + raw data rows and set as the DataGrid's effective ItemsSource, so
    /// the rows panel virtualizes it as one uniform list — no per-<c>GroupItem</c> realize cost, so
    /// grouped scrolling stays smooth at any depth and collapse state. When nothing is grouped the
    /// grid binds straight to the user's source.
    /// </summary>
    public partial class SearchDataGrid
    {
        #region Grouping projection state

        /// <summary>The row projection bound as the DataGrid's ItemsSource while the grid is grouped.</summary>
        private readonly GroupRowCollection _groupRows = new();

        /// <summary>Per-group expansion state, keyed by <see cref="GroupNode.PathKey"/>; survives reflatten.</summary>
        private readonly Dictionary<string, bool> _groupExpandState = new();

        /// <summary>
        /// Root group nodes from the most recent projection. Retained (the flattened
        /// <see cref="_groupRows"/> only holds the <em>visible</em> headers) so level-wide expand /
        /// collapse can reach groups whose headers are currently spliced out under a collapsed parent.
        /// </summary>
        private List<GroupNode> _groupRoots = new();

        /// <summary>Grouped descriptors (outer→inner) driving the current projection.</summary>
        private List<GridColumn> _groupColumns = new();

        /// <summary>Row predicate captured from the filter pipeline; applied upstream of the projection.</summary>
        private Predicate<object> _groupFilterPredicate;

        /// <summary>
        /// User (non-group) sort keys, in priority order, applied to leaf rows <em>within</em> their
        /// innermost group (the group paths always lead, per D2). This is flat mode's stand-in for
        /// the user's entries in <see cref="ItemCollection.SortDescriptions"/> — the displayed flat
        /// view carries no sort of its own (see <see cref="AttachProjection"/>), so within-group order
        /// is shaped here instead. Survives reflatten; bridged back onto the ungrouped view in
        /// <see cref="DetachProjection"/>.
        /// </summary>
        private readonly List<SortDescription> _withinGroupSorts = new();

        private bool _groupingActive;

        /// <summary>Re-entry guard: set while we swap the DataGrid's ItemsSource to/from the flat list.</summary>
        private bool _applyingProjectionSource;

        /// <summary>True while the flat projection owns the DataGrid's ItemsSource.</summary>
        internal bool IsGroupingActive => _groupingActive;

        #endregion

        #region Rebuild (projection)

        /// <summary>
        /// Engine behind <see cref="RebuildGroupDescriptions"/>. Computes the grouped
        /// descriptor set and pushes the same <see cref="GridColumn.GroupLevel"/>/
        /// <see cref="GridColumn.IsGrouped"/>/<see cref="GroupCount"/>/<see cref="GroupedColumns"/>
        /// projections the legacy path does (so the group panel and column hiding still work), then
        /// builds the flat projection and swaps it in. When nothing is grouped, restores the plain
        /// user source.
        /// </summary>
        private void RebuildGroupingProjection()
        {
            if (_rebuildingGroups) return;

            var descriptors = GridColumns;
            if (descriptors == null) return;

            _rebuildingGroups = true;
            try
            {
                var grouped = descriptors
                    .Where(d => d != null && d.GroupIndex >= 0)
                    .OrderBy(d => d.GroupIndex)
                    .Where(d => d.ActualAllowGrouping && d.Validate())
                    .ToList();

                // Normalize GroupIndex → contiguous 0..N-1 and project GroupLevel / IsGrouped.
                for (int level = 0; level < grouped.Count; level++)
                {
                    var descriptor = grouped[level];
                    if (descriptor.GroupIndex != level) descriptor.GroupIndex = level;
                    descriptor.SetGroupLevel(level);
                    descriptor.SetIsGrouped(true);
                }
                foreach (var descriptor in descriptors)
                {
                    if (descriptor == null || descriptor.GroupIndex >= 0) continue;
                    descriptor.SetGroupLevel(-1);
                    descriptor.SetIsGrouped(false);
                }

                SetValue(GroupCountPropertyKey, grouped.Count);

                _groupedColumnsBacking.Clear();
                foreach (var descriptor in grouped) _groupedColumnsBacking.Add(descriptor);

                ApplyGroupColumnVisibility();

                // Snapshot the prior grouped set BEFORE reassigning so the sort-direction sync can
                // clear the header arrow on columns that just left the grouping.
                var previouslyGrouped = _groupColumns;
                _groupColumns = grouped;

                // Push the resolved group sort direction onto each grouped column's header (and
                // clear it on departed ones) so the pill / column-header arrow tracks the grouping —
                // the flat counterpart to ApplyGroupSortDirectionsToColumns on the legacy path.
                ApplyGroupSortDirections(grouped, previouslyGrouped);

                if (grouped.Count == 0)
                {
                    DetachProjection();
                }
                else
                {
                    _groupFilterPredicate = SearchFilter;
                    RebuildRowProjection();
                    AttachProjection();
                }
            }
            finally
            {
                _rebuildingGroups = false;
            }

            // Re-run the resolver on every grouping change, grouped or not. On the ungroup-to-zero
            // path DetachProjection has cleared _groupingActive / GroupCount, so the resolver's gate
            // empties the strip; otherwise it recomputes the pinned chain for the new grouping.
            UpdateFixedGroupHeaders();
        }

        #endregion

        #region Projection

        /// <summary>
        /// Rebuilds <see cref="_groupRows"/> from the current source, filter, group columns, and
        /// expansion state. Cheap enough to run on every toggle/filter/source change.
        /// </summary>
        private void RebuildRowProjection()
        {
            // A reflatten replaces the whole projection: finish any in-flight slide first — a
            // collapse's deferred removal is captured against the current list.
            CompleteActiveGroupSlide();

            var grouped = _groupColumns;
            if (grouped == null || grouped.Count == 0)
            {
                _groupRows.ResetWith(Array.Empty<object>());
                return;
            }

            var culture = CultureInfo.CurrentCulture;
            var paths = grouped.Select(g => g.ResolveGroupPath()).ToList();
            var descs = new List<GroupDescription>(grouped.Count);
            for (int i = 0; i < grouped.Count; i++)
                descs.Add(CreateGroupDescription(grouped[i], paths[i]));

            // Filter (reuse the captured row predicate) then order by each group path (raw value)
            // honoring the group sort direction — interval buckets surface in natural order because
            // the underlying rows are sorted by raw value (see IntervalGroupDescription remarks).
            IEnumerable<object> src = originalItemsSource?.Cast<object>() ?? Enumerable.Empty<object>();
            var pred = _groupFilterPredicate;
            if (pred != null) src = src.Where(o => SafeFilter(pred, o));

            IOrderedEnumerable<object> ordered = null;
            for (int level = 0; level < grouped.Count; level++)
            {
                string path = paths[level];
                Func<object, object> key = o => ReflectionHelper.GetPropValue(o, path);
                bool descending = ResolveGroupDescending(grouped[level]);

                if (ordered == null)
                    ordered = descending ? src.OrderByDescending(key, GroupKeyComparer.Instance)
                                         : src.OrderBy(key, GroupKeyComparer.Instance);
                else
                    ordered = descending ? ordered.ThenByDescending(key, GroupKeyComparer.Instance)
                                         : ordered.ThenBy(key, GroupKeyComparer.Instance);
            }

            // Apply user (non-group) sorts as additional tie-breakers. Because the group paths above
            // already lead the ordering, these only reorder leaves within their innermost group —
            // groups themselves stay keyed by their own direction (D2: grouping leads sorting).
            foreach (var sort in _withinGroupSorts)
            {
                string sortPath = sort.PropertyName;
                if (string.IsNullOrEmpty(sortPath)) continue;
                Func<object, object> userKey = o => ReflectionHelper.GetPropValue(o, sortPath);
                bool descending = sort.Direction == ListSortDirection.Descending;

                if (ordered == null)
                    ordered = descending ? src.OrderByDescending(userKey, GroupKeyComparer.Instance)
                                         : src.OrderBy(userKey, GroupKeyComparer.Instance);
                else
                    ordered = descending ? ordered.ThenByDescending(userKey, GroupKeyComparer.Instance)
                                         : ordered.ThenBy(userKey, GroupKeyComparer.Instance);
            }

            var orderedList = (ordered ?? src.OrderBy(o => 0)).ToList();

            // Resolved once per projection: BuildNodes only pays the per-node leaf walk +
            // aggregate pass when the row count is on or the grid defines group / footer summaries.
            _projectGroupSummaries = HasAnyGroupSummaryContent();
            _alignGroupSummariesByColumns = ResolveAlignGroupSummaries();
            _projectGroupFooterSummaries = HasAnyGroupFooterContent();

            var roots = BuildNodes(orderedList, descs, grouped, culture, level: 0, parentPath: null);
            ApplyGroupSummarySort(roots);
            _groupRoots = roots;

            var flat = new List<object>(orderedList.Count + roots.Count);
            FlattenInto(roots, flat);
            _groupRows.ResetWith(flat);

            // Re-resolve the sticky strip after a reflatten driven by a user action (toggle, filter,
            // sort). Deferred to post-layout so the row containers have re-realized before the
            // resolver walks them. Skipped during the initial RebuildGroupingProjection pass
            // (which runs this BEFORE AttachProjection, then calls UpdateFixedGroupHeaders itself);
            // scroll-driven refreshes come from OnScrollViewerScrollChanged.
            if (_groupingActive && AllowFixedGroups)
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                    new Action(UpdateFixedGroupHeaders));
        }

        /// <summary>Recursively buckets <paramref name="items"/> into the group tree for one level.</summary>
        private List<GroupNode> BuildNodes(
            IEnumerable<object> items,
            List<GroupDescription> descs,
            List<GridColumn> grouped,
            CultureInfo culture,
            int level,
            string parentPath)
        {
            var nodes = new List<GroupNode>();

            // Items are pre-sorted by all group keys, so same-key rows are contiguous; GroupBy
            // preserves first-appearance order, yielding groups in sorted order.
            foreach (var bucket in items.GroupBy(o => descs[level].GroupNameFromItem(o, level, culture) ?? NullKey))
            {
                object displayValue = ReferenceEquals(bucket.Key, NullKey) ? null : bucket.Key;
                string keyText = displayValue?.ToString() ?? string.Empty;
                string pathKey = parentPath == null ? keyText : parentPath + GroupKeySeparator + keyText;

                var node = new GroupNode
                {
                    Level = level,
                    PathKey = pathKey,
                    DisplayValue = displayValue,
                    OwningColumn = grouped[level],
                    IsExpanded = GetGroupExpandState(pathKey),
                };

                int count;
                if (level + 1 < grouped.Count)
                {
                    var children = BuildNodes(bucket, descs, grouped, culture, level + 1, pathKey);
                    node.Children.AddRange(children);
                    count = children.Sum(c => c.Count);
                }
                else
                {
                    node.Items.AddRange(bucket);
                    count = node.Items.Count;
                }
                node.Count = count;

                if (_projectGroupSummaries || _projectGroupFooterSummaries)
                {
                    // Parent levels re-walk their descendant leaves; the innermost level reads
                    // its own Items directly. Every level renders the same grid-level set, and
                    // each level's footer aggregates that level's own leaves.
                    var leaves = node.Children.Count > 0
                        ? EnumerateLeafRows(node).ToList()
                        : node.Items;
                    ComputeNodeSummaries(node, leaves);
                }

                nodes.Add(node);
            }

            return nodes;
        }

        private void FlattenInto(List<GroupNode> nodes, List<object> sink)
        {
            bool footers = _projectGroupFooterSummaries;
            foreach (var node in nodes)
            {
                sink.Add(new GroupHeaderRow(node));
                if (!node.IsExpanded)
                {
                    // Collapsed: the footer pins directly beneath the header so the group's
                    // totals stay visible without expanding it.
                    if (footers) sink.Add(new GroupFooterRow(node));
                    continue;
                }

                if (node.Children.Count > 0)
                    FlattenInto(node.Children, sink);
                else
                    sink.AddRange(node.Items);

                // Expanded: the footer docks at the bottom of the group's content.
                if (footers) sink.Add(new GroupFooterRow(node));
            }
        }

        private bool GetGroupExpandState(string pathKey)
            => _groupExpandState.TryGetValue(pathKey, out bool v) ? v : AutoExpandAllGroups;

        private static bool ResolveGroupDescending(GridColumn descriptor)
        {
            var effective = descriptor.SortOrder != ColumnSortOrder.None
                ? descriptor.SortOrder
                : descriptor.DefaultGroupBySortDirection;
            return effective == ColumnSortOrder.Descending;
        }

        private static bool SafeFilter(Predicate<object> predicate, object item)
        {
            try { return predicate(item); }
            catch { return true; }
        }

        /// <summary>Sentinel key for null group values so they bucket together rather than throwing.</summary>
        private static readonly object NullKey = new();

        /// <summary>Null-tolerant comparer for group-key ordering (nulls sort first).</summary>
        private sealed class GroupKeyComparer : IComparer<object>
        {
            public static readonly GroupKeyComparer Instance = new();

            public int Compare(object x, object y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                if (x is IComparable cx && x.GetType() == y.GetType()) return cx.CompareTo(y);
                return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
            }
        }

        #endregion

        #region Sorting (flat path)

        /// <summary>
        /// Engine behind <see cref="ApplyColumnSort"/>. A header click on a <em>grouped</em>
        /// column re-orders that group level (grouping always carries a direction, so a "clear"
        /// falls back to ascending); a click on any other column maintains <see cref="_withinGroupSorts"/>
        /// — the within-group leaf sort — and reflattens. Nothing is written to the displayed flat
        /// view's <see cref="ItemCollection.SortDescriptions"/> (that would re-sort the header
        /// sentinels); ordering is shaped entirely in <see cref="RebuildRowProjection"/>.
        /// </summary>
        private void ApplyGroupedColumnSort(DataGridColumn col, ListSortDirection? direction, bool multiColumn)
        {
            string path = col?.SortMemberPath;
            if (string.IsNullOrEmpty(path)) return;

            var descriptor = FindGridColumnDescriptor(col);
            if (descriptor != null && descriptor.IsGrouped)
            {
                var groupDir = direction ?? ListSortDirection.Ascending;
                // Mirrors onto the descriptor's SortOrder via HookSortObservation; the projection's
                // ResolveGroupDescending then reads it back on reflatten.
                if (col.SortDirection != groupDir) col.SortDirection = groupDir;
                // A direct sort on the grouped column is an explicit ordering choice — drop any
                // active sort-by-summary, which would otherwise keep overriding the key order.
                ClearGroupSummarySortCore();
                RebuildRowProjection();
                return;
            }

            // Non-grouped column → user sort within groups. Drop any prior entry for this path first.
            for (int i = _withinGroupSorts.Count - 1; i >= 0; i--)
            {
                if (_withinGroupSorts[i].PropertyName == path) _withinGroupSorts.RemoveAt(i);
            }

            if (!multiColumn)
            {
                _withinGroupSorts.Clear();
                foreach (var other in Columns)
                {
                    if (other == col) continue;
                    // Leave grouped columns' arrows alone — their group sort persists.
                    var otherDescriptor = FindGridColumnDescriptor(other);
                    if (otherDescriptor != null && otherDescriptor.IsGrouped) continue;
                    if (other.SortDirection != null) other.SortDirection = null;
                }
            }

            if (direction == null)
            {
                col.SortDirection = null;
            }
            else
            {
                _withinGroupSorts.Add(new SortDescription(path, direction.Value));
                col.SortDirection = direction.Value;
            }

            RebuildRowProjection();
            RefreshAllSortIndices();
        }

        /// <summary>
        /// Context-menu entry point (Sort Ascending / Descending) routed here while flat mode owns
        /// the grid. Single-column semantics, matching the legacy menu behavior.
        /// </summary>
        internal void SortColumnFromMenu(DataGridColumn col, ListSortDirection direction)
            => ApplyGroupedColumnSort(col, direction, multiColumn: false);

        /// <summary>
        /// "Clear Sorting": drops every user (non-group) sort — including an active
        /// sort-by-summary — and clears the header arrow on non-grouped columns, then
        /// reflattens. Grouped columns keep their group sort (D2).
        /// </summary>
        internal void ClearColumnSortsFromMenu()
        {
            _withinGroupSorts.Clear();
            ClearGroupSummarySortCore();
            foreach (var col in Columns)
            {
                var descriptor = FindGridColumnDescriptor(col);
                if (descriptor != null && descriptor.IsGrouped) continue;
                if (col.SortDirection != null) col.SortDirection = null;
            }
            RebuildRowProjection();
            RefreshAllSortIndices();
        }

        /// <summary>
        /// Flips a grouped column's sort direction from the group-panel pill while flat mode is
        /// active. Grouping leads sorting (D2), so there is no unsorted state — Ascending ↔
        /// Descending only. Supersedes any active sort-by-summary, same as a header-click sort
        /// on the grouped column.
        /// </summary>
        internal void ToggleGroupSort(GridColumn column)
        {
            if (column?.InternalColumn == null || !column.IsGrouped) return;
            var next = ResolveGroupDescending(column)
                ? ListSortDirection.Ascending
                : ListSortDirection.Descending;
            if (column.InternalColumn.SortDirection != next)
                column.InternalColumn.SortDirection = next; // mirrors to SortOrder
            ClearGroupSummarySortCore();
            RebuildRowProjection();
        }

        /// <summary>
        /// Pushes the resolved group sort direction onto each currently-grouped column's generated
        /// <see cref="DataGridColumn.SortDirection"/>, and clears it on columns that just left the
        /// grouping (unless a <see cref="_withinGroupSorts"/> entry survives on that path). The
        /// SortDirection set here mirrors onto <see cref="ColumnDataBase.SortOrder"/> via
        /// <see cref="HookSortObservation"/>, which <see cref="ResolveGroupDescending"/> reads back.
        /// </summary>
        private void ApplyGroupSortDirections(List<GridColumn> grouped, List<GridColumn> previouslyGrouped)
        {
            foreach (var descriptor in grouped)
            {
                if (descriptor?.InternalColumn == null) continue;
                var dir = ResolveGroupDescending(descriptor)
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                if (descriptor.InternalColumn.SortDirection != dir)
                    descriptor.InternalColumn.SortDirection = dir;
            }

            foreach (var prev in previouslyGrouped)
            {
                if (prev?.InternalColumn == null) continue;
                if (grouped.Contains(prev)) continue;

                string path = prev.ResolveGroupPath();
                bool hasUserSort = !string.IsNullOrEmpty(path)
                    && _withinGroupSorts.Exists(s => s.PropertyName == path);
                if (!hasUserSort && prev.InternalColumn.SortDirection != null)
                    prev.InternalColumn.SortDirection = null;
            }
        }

        #endregion

        #region ItemsSource swap

        private void AttachProjection()
        {
            // WPF's ItemCollection persists GroupDescriptions / SortDescriptions / Filter ACROSS an
            // ItemsSource swap. If the legacy path (or a prior filter) left any set, they'd re-shape
            // the flat list — re-grouping the header sentinels (which have no data fields) into a
            // null bucket and even resurrecting GroupItem chrome. Grouped mode shapes upstream, so the
            // displayed view must carry none of these.
            if (Items.GroupDescriptions.Count > 0) Items.GroupDescriptions.Clear();
            if (Items.SortDescriptions.Count > 0) Items.SortDescriptions.Clear();
            if (Items.Filter != null) Items.Filter = null;

            _groupingActive = true;
            if (!ReferenceEquals(ItemsSource, _groupRows))
            {
                _applyingProjectionSource = true;
                try { SetCurrentValue(ItemsSourceProperty, _groupRows); }
                finally { _applyingProjectionSource = false; }
            }
        }

        private void DetachProjection()
        {
            if (!_groupingActive) return;

            CompleteActiveGroupSlide();

            _groupingActive = false;

            var restore = originalItemsSource;
            _applyingProjectionSource = true;
            try { SetCurrentValue(ItemsSourceProperty, restore); }
            finally { _applyingProjectionSource = false; }

            _groupRows.ResetWith(Array.Empty<object>());

            // Carry any within-group user sorts onto the now-ungrouped view so its row order — and
            // the header arrows we set while grouped — agree with the legacy SortDescriptions path
            // that owns sorting from here on.
            var sorts = Items?.SortDescriptions;
            if (sorts != null)
            {
                sorts.Clear();
                foreach (var s in _withinGroupSorts) sorts.Add(s);
            }

            // Re-apply any active filter to the restored (ungrouped) view — flat mode had it
            // captured upstream, so the view itself currently has no predicate.
            if (SearchFilter != null || HasActiveColumnFilters())
                FilterItemsSource();
        }

        #endregion

        #region Expansion / filter hooks

        /// <summary>Toggles one group's expansion. Used by the header row's toggle.</summary>
        public void ToggleGroup(GroupHeaderRow header)
        {
            if (header?.Node == null || !_groupingActive) return;
            ToggleGroupCore(header.Node, !header.Node.IsExpanded, header);
        }

        /// <summary>
        /// Sets one group's expansion. The strip-routing counterpart to <see cref="ToggleGroup"/>,
        /// used by the pinned fixed-group chrome whose commands carry a <see cref="GroupNode"/>
        /// rather than a header row.
        /// </summary>
        internal void SetGroupExpanded(GroupNode node, bool expanded)
        {
            if (node == null || !_groupingActive) return;
            ToggleGroupCore(node, expanded, null);
        }

        /// <summary>
        /// Single-group toggle chokepoint. The animated splice path
        /// (<see cref="TryToggleGroupSpliced"/>) handles it in place when
        /// <see cref="AllowGroupExpandAnimation"/> is on and the toggle qualifies; otherwise the
        /// classic apply-state-then-reflatten runs unchanged.
        /// </summary>
        private void ToggleGroupCore(GroupNode node, bool expanded, GroupHeaderRow header)
        {
            if (TryToggleGroupSpliced(node, expanded, header)) return;
            if (ApplyGroupExpansion(node, expanded))
                RebuildRowProjection();
        }

        /// <summary>Current expansion of a group by <see cref="GroupNode.PathKey"/> (for the strip's chevron / command guards).</summary>
        internal bool GetGroupExpandedByPath(string pathKey) => GetGroupExpandState(pathKey);

        /// <summary>
        /// Sets expansion for every group at <paramref name="level"/> (the strip's "expand/collapse
        /// all at this level"). Walks the retained <see cref="_groupRoots"/> tree so groups hidden
        /// under a collapsed parent are reached too, then reflattens once.
        /// </summary>
        internal void SetLevelExpanded(int level, bool expanded)
        {
            if (level < 0 || !_groupingActive) return;
            bool changed = false;
            ForEachNodeAtLevel(_groupRoots, level, node =>
            {
                if (ApplyGroupExpansion(node, expanded)) changed = true;
            });
            if (changed) RebuildRowProjection();
        }

        /// <summary>
        /// Writes <paramref name="node"/>'s expansion into the path-keyed state map and, when
        /// <see cref="ExpandGroupsRecursively"/> is on, cascades the same state onto every descendant
        /// node — the flat-mode equivalent of the legacy descendant-Expander cascade in
        /// <c>OnGroupExpanderToggled</c>. The tree is fully built every projection regardless of
        /// expand state (collapsed subtrees are only omitted from the flattened output, not from
        /// <see cref="GroupNode.Children"/>), so <paramref name="node"/>'s subtree is always
        /// reachable here. Returns <c>true</c> when any group's state actually changed, so the caller
        /// can skip a no-op reflatten.
        /// </summary>
        private bool ApplyGroupExpansion(GroupNode node, bool expanded)
        {
            bool changed = GetGroupExpandState(node.PathKey) != expanded;
            _groupExpandState[node.PathKey] = expanded;

            if (ExpandGroupsRecursively)
            {
                foreach (var child in node.Children)
                    changed |= ApplyGroupExpansion(child, expanded);
            }

            return changed;
        }

        private static void ForEachNodeAtLevel(List<GroupNode> nodes, int level, Action<GroupNode> action)
        {
            foreach (var node in nodes)
            {
                if (node.Level == level) action(node);
                else if (node.Level < level && node.Children.Count > 0)
                    ForEachNodeAtLevel(node.Children, level, action);
            }
        }

        /// <summary>implementation of expand/collapse-all; clears per-group overrides.</summary>
        private void SetAllGroupsExpanded(bool expanded)
        {
            _groupExpandState.Clear();
            // With no per-group overrides, GetGroupExpandState falls back to AutoExpandAllGroups;
            // force that to the requested state so the reflatten reflects "all".
            if (AutoExpandAllGroups != expanded)
                SetCurrentValue(AutoExpandAllGroupsProperty, expanded); // triggers rebuild via change handler
            else
                RebuildRowProjection();
        }

        /// <summary>
        /// Filter chokepoint. In flat mode the predicate is captured and applied upstream of the
        /// projection (the displayed flat view must stay unfiltered — it holds header sentinels);
        /// otherwise this is exactly the legacy <c>Items.Filter = p; SearchFilter = p;</c>.
        /// </summary>
        private void ApplyDisplayFilter(Predicate<object> predicate)
        {
            SearchFilter = predicate;
            if (_groupingActive)
            {
                _groupFilterPredicate = predicate;
                RebuildRowProjection();
            }
            else
            {
                Items.Filter = predicate;
            }

            // Single chokepoint for every filter path (sync, async, editor-tree, clear), so the
            // filtered count stays in step with whatever was just applied.
            UpdateFilteredItemCount();
        }

        #endregion

        #region Selection / editing guards (header sentinels)

        /// <summary>Re-entry guard: set while a scrub pass mutates the selection collections.</summary>
        private bool _scrubbingHeaderSelection;

        /// <summary>True when <paramref name="item"/> is a flat-grouping group-header sentinel.</summary>
        internal static bool IsHeaderItem(object item) => item is GroupHeaderRow;

        /// <summary>
        /// True when <paramref name="item"/> is any full-width grouping sentinel (group header or
        /// group footer) rather than a real user row — neither carries data cells, so both are
        /// excluded from selection, select-all, and best-fit measurement.
        /// </summary>
        internal static bool IsSentinelRow(object item) => item is GroupHeaderRow or GroupFooterRow;

        /// <summary>
        /// Removes any group-header sentinel from the live selection so <see cref="Selector.SelectedItem"/>,
        /// <see cref="MultiSelector.SelectedItems"/>, and <see cref="DataGrid.SelectedCells"/> only ever
        /// surface real user rows. Runs after every selection change (row range, Shift-extend,
        /// Ctrl+A, click, programmatic). Handles both row-selection modes (scrub
        /// <see cref="MultiSelector.SelectedItems"/>; cells follow in FullRow) and cell-selection
        /// modes (scrub <see cref="DataGrid.SelectedCells"/>). Re-entrancy-guarded; a no-op outside
        /// flat mode or when nothing header-ish is selected.
        /// </summary>
        private void ScrubHeaderSelection()
        {
            if (!_groupingActive || _scrubbingHeaderSelection) return;

            bool hasHeaderItem = false;
            for (int i = 0; i < SelectedItems.Count; i++)
            {
                if (IsSentinelRow(SelectedItems[i])) { hasHeaderItem = true; break; }
            }

            List<DataGridCellInfo> headerCells = null;
            foreach (var cell in SelectedCells)
            {
                if (IsSentinelRow(cell.Item))
                    (headerCells ??= new List<DataGridCellInfo>()).Add(cell);
            }

            if (!hasHeaderItem && headerCells == null) return;

            _scrubbingHeaderSelection = true;
            try
            {
                if (hasHeaderItem)
                {
                    for (int i = SelectedItems.Count - 1; i >= 0; i--)
                    {
                        if (IsSentinelRow(SelectedItems[i])) SelectedItems.RemoveAt(i);
                    }
                }

                if (headerCells != null)
                {
                    // Writable only in cell-selection modes; in FullRow the SelectedItems scrub
                    // above already dropped the row's cells, so a throw here is expected and benign.
                    foreach (var cell in headerCells)
                    {
                        try { SelectedCells.Remove(cell); }
                        catch (NotSupportedException) { }
                    }
                }
            }
            finally
            {
                _scrubbingHeaderSelection = false;
            }
        }

        #endregion

        #region Row containers (header rendering)

        /// <summary>
        /// Generates a <see cref="SearchDataGridRow"/> for every row so flat-mode group-header
        /// sentinels can swap to the full-width header template. For data rows (and in the legacy
        /// path) the subclass behaves exactly like the stock <see cref="DataGridRow"/>.
        /// </summary>
        protected override DependencyObject GetContainerForItemOverride() => new SearchDataGridRow();

        /// <summary>
        /// Pushes the per-item group-header flag onto the container before the base prepares it, so
        /// the row style's <c>IsGroupHeader</c> trigger resolves the right template up front. The
        /// item is a <see cref="GroupHeaderRow"/> iff this is a header sentinel.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is SearchDataGridRow row)
            {
                row.SetGroupHeader(item as GroupHeaderRow);
                row.SetGroupFooter(item as GroupFooterRow);
            }
            // A recycled container may still carry the active slide's transform; reset it before
            // the container takes on its new item.
            ClearSlideOnContainer(element);
            base.PrepareContainerForItemOverride(element, item);

            // Re-apply edit-form open state: a recycled row scrolling back onto the editing item must
            // re-show its form (and re-hide its cells for InlineHideRow); every other row is forced
            // closed so a recycled container never inherits the prior item's open form. Only runs
            // once the edit-form host has been wired (EditFormShowMode != None), so the stock path is
            // untouched.
            if (_editFormHostingApplied && element is SearchDataGridRow editFormRow)
                ApplyEditFormRowState(editFormRow, IsEditFormItem(item));
        }

        /// <summary>
        /// Clears the group-header flag when a recycled container is released, so it never renders
        /// the previous item's header chrome before <see cref="PrepareContainerForItemOverride"/>
        /// re-stamps it on reuse.
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is SearchDataGridRow row)
            {
                row.SetGroupHeader(null);
                row.SetGroupFooter(null);
                // Drop any InlineHideRow cells-hidden flag so a recycled container never reuses it;
                // PrepareContainerForItemOverride re-stamps the right state for the next item.
                if (_editFormHostingApplied)
                    row.SetCellsHidden(false);
            }
            ClearSlideOnContainer(element);
            base.ClearContainerForItemOverride(element, item);
        }

        #endregion
    }
}
