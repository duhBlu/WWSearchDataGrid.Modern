using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWControls.Wpf
{
    public partial class SearchDataGrid
    {
        #region Grouping State

        /// <summary>Re-entry guard for <see cref="RebuildGroupDescriptions"/>; set while normalizing.</summary>
        private bool _rebuildingGroups;

        /// <summary>
        /// Suppresses the per-change rebuild triggered by <see cref="OnColumnGroupIndexChanged"/> so
        /// a batch mutation (e.g. <see cref="ClearGrouping"/>) can set every <c>GroupIndex</c> first
        /// and rebuild once.
        /// </summary>
        private bool _suppressGroupRebuild;

        /// <summary>
        /// Grid-level gate for grouping. <c>true</c> (default) lets columns be grouped subject to
        /// their own <see cref="GridColumn.AllowGrouping"/>. <c>false</c> turns the feature off:
        /// the current grouping is cleared (every <see cref="GridColumn.GroupIndex"/> resets, so
        /// re-enabling later starts ungrouped), the group panel collapses, and the column-header
        /// menu's grouping items are removed. Each column resolves
        /// <see cref="GridColumn.ActualAllowGrouping"/> from this AND its own value.
        /// </summary>
        public static readonly DependencyProperty AllowGroupingProperty =
            DependencyProperty.Register(
                nameof(AllowGrouping),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true, OnAllowGroupingChanged));

        /// <inheritdoc cref="AllowGroupingProperty"/>
        public bool AllowGrouping
        {
            get => (bool)GetValue(AllowGroupingProperty);
            set => SetValue(AllowGroupingProperty, value);
        }

        /// <summary>
        /// Grid-level default for whether grouped columns stay visible. <c>false</c> (default) hides
        /// a column while it is grouped; <c>true</c> keeps it in place. Each column resolves
        /// <see cref="GridColumn.ActualShowGroupedColumn"/> from its own
        /// <see cref="GridColumn.ShowGroupedColumn"/> override falling back to this value. Not
        /// DevExpress's View-only <c>ShowGroupedColumns</c> — the per-column override layers on top (D3).
        /// </summary>
        public static readonly DependencyProperty ShowGroupedColumnsProperty =
            DependencyProperty.Register(
                nameof(ShowGroupedColumns),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false, OnShowGroupedColumnsChanged));

        /// <inheritdoc cref="ShowGroupedColumnsProperty"/>
        public bool ShowGroupedColumns
        {
            get => (bool)GetValue(ShowGroupedColumnsProperty);
            set => SetValue(ShowGroupedColumnsProperty, value);
        }

        private static void OnAllowGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            var descriptors = grid.GridColumns;
            if (descriptors != null)
            {
                foreach (var descriptor in descriptors)
                    descriptor?.RefreshActualAllowGrouping();
            }

            if (e.NewValue is false)
            {
                // Gating the feature off is destructive by design: clear the grouping outright —
                // merely filtering grouped columns out of the projection would leave stale
                // GroupIndex / IsGrouped state that re-groups the grid (and keeps grouped-hidden
                // columns collapsed) the moment the gate reopens. ClearGrouping rebuilds when
                // anything was grouped; when nothing was, there is no projection to tear down.
                grid.ClearGrouping();
                // Collapse the panel with the feature. SetCurrentValue so a consumer's binding or
                // local IsGroupPanelVisible value isn't destroyed, only its current value changed.
                grid.SetCurrentValue(IsGroupPanelVisibleProperty, false);
                return;
            }

            grid.RebuildGroupDescriptions();
        }

        private static void OnShowGroupedColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            var descriptors = grid.GridColumns;
            if (descriptors != null)
            {
                foreach (var descriptor in descriptors)
                    descriptor?.RefreshActualShowGroupedColumn();
            }
            grid.ApplyGroupColumnVisibility();
        }

        private static readonly DependencyPropertyKey GroupCountPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(GroupCount),
                typeof(int),
                typeof(SearchDataGrid),
                new PropertyMetadata(0));

        /// <summary>Identifies the read-only <see cref="GroupCount"/> dependency property.</summary>
        public static readonly DependencyProperty GroupCountProperty = GroupCountPropertyKey.DependencyProperty;

        /// <summary>
        /// Number of columns currently participating in the grouping — i.e. the depth of the group
        /// nesting. <c>0</c> when the grid is ungrouped. Read-only; maintained by the grouping engine.
        /// </summary>
        public int GroupCount => (int)GetValue(GroupCountProperty);

        #endregion

        #region Group Expansion

        /// <summary>
        /// Grid-level default for the initial expanded state of group headers. <c>true</c> (default)
        /// makes every newly-materialized group start expanded; <c>false</c> makes them start
        /// collapsed. The default <see cref="GroupStyle"/> binds each group expander's
        /// <see cref="Expander.IsExpanded"/> to this value, so a runtime toggle expands or collapses
        /// every realized group as well (the callback below drives that pass).
        /// </summary>
        public static readonly DependencyProperty AutoExpandAllGroupsProperty =
            DependencyProperty.Register(
                nameof(AutoExpandAllGroups),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true, OnAutoExpandAllGroupsChanged));

        /// <inheritdoc cref="AutoExpandAllGroupsProperty"/>
        public bool AutoExpandAllGroups
        {
            get => (bool)GetValue(AutoExpandAllGroupsProperty);
            set => SetValue(AutoExpandAllGroupsProperty, value);
        }

        /// <summary>
        /// When <c>true</c>, toggling a group expander cascades the same expanded state to every
        /// nested group beneath it: expanding a parent expands its descendants, collapsing a parent
        /// collapses them. Default <c>false</c>, which preserves the natural Expander behavior
        /// (parent toggles only itself). The cascade is driven by class handlers for
        /// <see cref="Expander.ExpandedEvent"/> / <see cref="Expander.CollapsedEvent"/>; a re-entry
        /// guard prevents the descendant set from re-triggering the walk.
        /// </summary>
        public static readonly DependencyProperty ExpandGroupsRecursivelyProperty =
            DependencyProperty.Register(
                nameof(ExpandGroupsRecursively),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false));

        /// <inheritdoc cref="ExpandGroupsRecursivelyProperty"/>
        public bool ExpandGroupsRecursively
        {
            get => (bool)GetValue(ExpandGroupsRecursivelyProperty);
            set => SetValue(ExpandGroupsRecursivelyProperty, value);
        }

        /// <summary>
        /// When <c>true</c>, expanding or collapsing a single group animates: the header's chevron
        /// rotates between right (collapsed) and down (expanded), and the group's rows slide open /
        /// closed. Animated toggles splice the group's row block in and out of the flat projection
        /// in place instead of reflattening, so the header container (and its chevron) survives the
        /// toggle. Default <c>false</c> — toggles snap exactly as before. Bulk operations
        /// (expand/collapse all, level-wide, <see cref="AutoExpandAllGroups"/> flips, filter/sort
        /// rebuilds) never animate, and a toggle whose row block exceeds the splice limit falls back
        /// to the plain reflatten.
        /// </summary>
        public static readonly DependencyProperty AllowGroupExpandAnimationProperty =
            DependencyProperty.Register(
                nameof(AllowGroupExpandAnimation),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false));

        /// <inheritdoc cref="AllowGroupExpandAnimationProperty"/>
        public bool AllowGroupExpandAnimation
        {
            get => (bool)GetValue(AllowGroupExpandAnimationProperty);
            set => SetValue(AllowGroupExpandAnimationProperty, value);
        }

        /// <summary>
        /// Pixels of indent applied to each nested group level. Drives the
        /// <c>GroupHeaderMarginConverter</c> used by the default <see cref="GroupStyle"/>:
        /// outermost groups (level 0) are left-margined by <see cref="DataGrid.RowHeaderActualWidth"/>
        /// so they align with the cell-content edge past the row-header gutter; deeper groups are
        /// indented by this much relative to their parent's content area. Default 16. Set to 0
        /// to disable per-level indent (the row-header gutter alignment still applies).
        /// </summary>
        public static readonly DependencyProperty GroupIndentWidthProperty =
            DependencyProperty.Register(
                nameof(GroupIndentWidth),
                typeof(double),
                typeof(SearchDataGrid),
                new PropertyMetadata(16d));

        /// <inheritdoc cref="GroupIndentWidthProperty"/>
        public double GroupIndentWidth
        {
            get => (double)GetValue(GroupIndentWidthProperty);
            set => SetValue(GroupIndentWidthProperty, value);
        }

        /// <summary>
        /// Toggles the group panel — a strip above the column headers that shows one pill per
        /// grouped column. Default <c>false</c>. Drag a column header onto the panel to group
        /// by it; click a pill to flip its sort direction; right-click a pill or the panel for
        /// the per-column / global menus.
        /// </summary>
        public static readonly DependencyProperty IsGroupPanelVisibleProperty =
            DependencyProperty.Register(
                nameof(IsGroupPanelVisible),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false));

        /// <inheritdoc cref="IsGroupPanelVisibleProperty"/>
        public bool IsGroupPanelVisible
        {
            get => (bool)GetValue(IsGroupPanelVisibleProperty);
            set => SetValue(IsGroupPanelVisibleProperty, value);
        }

        /// <summary>
        /// When <c>true</c>, the group header(s) for the topmost visible row stay pinned to the
        /// top of the data area as the grid scrolls vertically — nested groups stack their headers
        /// in <see cref="GridColumn.GroupLevel"/> order. As the next sibling group's in-place
        /// header rises into the strip, the topmost pinned header slides up to make room (push
        /// transition). Default <c>false</c>. The strip itself is rendered by
        /// <see cref="FixedGroupHeadersPresenter"/> and only materializes when this is true
        /// AND <see cref="GroupCount"/> &gt; 0.
        /// </summary>
        public static readonly DependencyProperty AllowFixedGroupsProperty =
            DependencyProperty.Register(
                nameof(AllowFixedGroups),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true, OnAllowFixedGroupsChanged));

        /// <inheritdoc cref="AllowFixedGroupsProperty"/>
        public bool AllowFixedGroups
        {
            get => (bool)GetValue(AllowFixedGroupsProperty);
            set => SetValue(AllowFixedGroupsProperty, value);
        }

        private static void OnAllowFixedGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            // Run the resolver once now so the strip reflects the new state immediately. Toggling
            // on: it populates without waiting for the next scroll. Toggling off: the resolver's own
            // gate clears the backing collection, so the (then-empty) strip hides via its Visibility
            // binding. Scroll-driven updates thereafter come from OnScrollViewerScrollChanged.
            grid.UpdateFixedGroupHeaders();
        }

        /// <summary>
        /// Backing collection for <see cref="GroupedColumns"/>. Maintained by
        /// <see cref="RebuildGroupDescriptions"/> as the ordered (by <see cref="GridColumn.GroupLevel"/>)
        /// set of currently-grouped descriptors. Exposed as <see cref="ObservableCollection{T}"/>
        /// so the group panel's <c>ItemsControl</c> binding picks up changes without an explicit
        /// notification dance.
        /// </summary>
        private readonly System.Collections.ObjectModel.ObservableCollection<GridColumn> _groupedColumnsBacking
            = new System.Collections.ObjectModel.ObservableCollection<GridColumn>();

        private static readonly DependencyPropertyKey GroupedColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(GroupedColumns),
                typeof(System.Collections.ObjectModel.ObservableCollection<GridColumn>),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="GroupedColumns"/> dependency property.</summary>
        public static readonly DependencyProperty GroupedColumnsProperty = GroupedColumnsPropertyKey.DependencyProperty;

        /// <summary>
        /// Ordered observable collection of the currently-grouped <see cref="GridColumn"/>
        /// descriptors, by <see cref="GridColumn.GroupLevel"/>. Maintained by the grouping engine;
        /// consumed by the group panel.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<GridColumn> GroupedColumns
            => (System.Collections.ObjectModel.ObservableCollection<GridColumn>)GetValue(GroupedColumnsProperty);

        private static void OnAutoExpandAllGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;

            // "Set all groups to this state" is an explicit override of per-group memory: clear the
            // path-keyed expand map so previously-toggled groups don't fight the new default, then
            // reproject so realized headers reflect it immediately.
            grid._groupExpandState.Clear();
            if (grid.IsGroupingActive) grid.RebuildRowProjection();
        }

        #endregion

        #region Group key path

        /// <summary>
        /// Separator joining a group's display-value path segments into a single key. ASCII Unit
        /// Separator (U+001F) — a non-printing control character unlikely to appear in real group
        /// values — so names containing '/' or other punctuation don't collide. Used by the
        /// projection to build each <c>GroupNode.PathKey</c> for expansion-state persistence.
        /// </summary>
        private const string GroupKeySeparator = "";

        #endregion

        #region Grouping API

        /// <summary>
        /// Adds <paramref name="column"/> to the grouping as the innermost (last) group level. No-op
        /// when the column is null, not owned by this grid, or already grouped.
        /// </summary>
        public void GroupBy(GridColumn column)
        {
            if (column == null) return;
            var descriptors = GridColumns;
            if (descriptors == null || !descriptors.Contains(column)) return;
            if (column.GroupIndex >= 0) return;
            if (!column.ActualAllowGrouping) return;

            // Append at the end of the current grouping; RebuildGroupDescriptions normalizes the
            // range so the exact value here only needs to sort after the existing groups.
            column.GroupIndex = GroupCount;
        }

        /// <summary>
        /// Groups by the column whose <see cref="ColumnDataBase.FieldName"/> matches
        /// <paramref name="fieldName"/> (case-insensitive). No-op when no such column exists.
        /// </summary>
        public void GroupBy(string fieldName)
        {
            var column = FindGroupableColumn(fieldName);
            if (column != null) GroupBy(column);
        }

        /// <summary>
        /// Removes <paramref name="column"/> from the grouping. The remaining grouped columns keep
        /// their relative order and are renormalized to a contiguous range. No-op when the column is
        /// null or not grouped.
        /// </summary>
        public void Ungroup(GridColumn column)
        {
            if (column == null || column.GroupIndex < 0) return;
            column.GroupIndex = -1;
        }

        /// <summary>Removes every column from the grouping in one pass.</summary>
        public void ClearGrouping()
        {
            var descriptors = GridColumns;
            if (descriptors == null) return;

            bool any = false;
            _suppressGroupRebuild = true;
            try
            {
                foreach (var descriptor in descriptors)
                {
                    if (descriptor != null && descriptor.GroupIndex >= 0)
                    {
                        descriptor.GroupIndex = -1;
                        any = true;
                    }
                }
            }
            finally
            {
                _suppressGroupRebuild = false;
            }

            // Ungrouping invalidates every previously-captured group key — drop the path-keyed
            // expand map so a later re-grouping starts fresh rather than replaying stale state.
            _groupExpandState.Clear();
            if (any) RebuildGroupDescriptions();
        }

        /// <summary>
        /// Returns the grouped column at the given zero-based <see cref="GridColumn.GroupLevel"/>,
        /// or <c>null</c> when no column occupies that level. Used by
        /// <see cref="GroupValueTemplateSelector"/> to map a group header's nesting depth back to
        /// the column that owns it.
        /// </summary>
        public GridColumn GetGroupedColumnAtLevel(int level)
        {
            if (level < 0) return null;
            var descriptors = GridColumns;
            if (descriptors == null) return null;
            return descriptors.FirstOrDefault(d => d != null && d.IsGrouped && d.GroupLevel == level);
        }

        /// <summary>
        /// Expands every group, including those currently spliced out of the projection under a
        /// collapsed parent. Clears per-group overrides and flips the <see cref="AutoExpandAllGroups"/>
        /// default so newly-projected groups also start expanded.
        /// </summary>
        public void ExpandAllGroups() => SetAllGroupsExpanded(true);

        /// <summary>
        /// Collapses every group, including those currently spliced out of the projection under a
        /// collapsed parent. See <see cref="ExpandAllGroups"/> for the symmetric details.
        /// </summary>
        public void CollapseAllGroups() => SetAllGroupsExpanded(false);

        /// <summary>
        /// Finds a groupable descriptor by <see cref="ColumnDataBase.FieldName"/>. Used by the
        /// string overload of <see cref="GroupBy(string)"/>.
        /// </summary>
        private GridColumn FindGroupableColumn(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return null;
            var descriptors = GridColumns;
            if (descriptors == null) return null;
            return descriptors.FirstOrDefault(d =>
                d != null && string.Equals(d.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Grouping Engine

        /// <summary>
        /// Invoked by a <see cref="GridColumn"/> when its <see cref="GridColumn.GroupIndex"/>
        /// changes. Rebuilds the grouping unless a batch mutation or normalization pass is in
        /// progress.
        /// </summary>
        internal void OnColumnGroupIndexChanged(GridColumn column)
        {
            if (_rebuildingGroups || _suppressGroupRebuild) return;
            RebuildGroupDescriptions();
        }

        /// <summary>
        /// Single source of grouping truth. Normalizes the grouped columns by
        /// <see cref="GridColumn.GroupIndex"/>, projects <see cref="GridColumn.GroupLevel"/> /
        /// <see cref="GridColumn.IsGrouped"/>, rebuilds the flat group projection, and swaps it in
        /// as the grid's effective ItemsSource (restoring the plain source when nothing is grouped).
        /// The grid renders one flattened list of header sentinels + data rows — there is no
        /// <c>GroupItem</c>/<c>Expander</c> hierarchy — so grouped scrolling stays smooth at any
        /// depth and collapse state.
        /// </summary>
        internal void RebuildGroupDescriptions() => RebuildGroupingProjection();

        /// <summary>
        /// Reconciles each generated column's visibility against its grouping state (D3): a grouped
        /// column whose <see cref="GridColumn.ActualShowGroupedColumn"/> resolves <c>false</c> is
        /// collapsed; every other column reflects its own <see cref="ColumnLayoutBase.Visible"/>.
        /// Driven from the grouping rebuild and from the
        /// <see cref="GridColumn.ShowGroupedColumn"/> / <see cref="ShowGroupedColumns"/> change paths.
        /// The descriptor's own <c>Visible</c> DP is never mutated, so ungrouping restores the
        /// user's intended visibility automatically.
        /// </summary>
        internal void ApplyGroupColumnVisibility()
        {
            var descriptors = GridColumns;
            if (descriptors == null) return;

            foreach (var descriptor in descriptors)
            {
                if (descriptor?.InternalColumn == null) continue;

                bool hideForGroup = descriptor.IsGrouped && !descriptor.ActualShowGroupedColumn;
                Visibility target = hideForGroup
                    ? Visibility.Collapsed
                    : (descriptor.Visible ? Visibility.Visible : Visibility.Collapsed);

                if (descriptor.InternalColumn.Visibility != target)
                    descriptor.InternalColumn.Visibility = target;
            }
        }

        /// <summary>
        /// Builds the <see cref="GroupDescription"/> for a grouped column: a plain
        /// <see cref="PropertyGroupDescription"/> for whole-value grouping
        /// (<see cref="ColumnGroupInterval.Default"/> / <see cref="ColumnGroupInterval.Value"/>), or
        /// an <see cref="IntervalGroupDescription"/> for the derived-bucket modes.
        /// </summary>
        private static GroupDescription CreateGroupDescription(GridColumn descriptor, string path)
        {
            var interval = descriptor.GroupInterval;
            if (interval == ColumnGroupInterval.Default || interval == ColumnGroupInterval.Value)
                return new PropertyGroupDescription(path);
            return new IntervalGroupDescription(path, interval);
        }

        /// <summary>
        /// Resets the grouping projections on a descriptor that is leaving the grid, so a later
        /// re-add starts clean. The caller rebuilds the grouping afterward to drop the column from
        /// the projection.
        /// </summary>
        internal void UnhookGroupObservation(GridColumn descriptor)
        {
            if (descriptor == null) return;
            descriptor.SetGroupLevel(-1);
            descriptor.SetIsGrouped(false);
        }

        #endregion
    }
}
