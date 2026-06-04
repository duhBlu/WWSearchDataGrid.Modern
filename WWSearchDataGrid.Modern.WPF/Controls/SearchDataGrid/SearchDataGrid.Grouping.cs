using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Static Construction

        static SearchDataGrid()
        {
            // Class-level handlers for Expander.ExpandedEvent / CollapsedEvent let the recursive
            // expansion cascade (ExpandGroupsRecursively) live entirely in the grid: any Expander
            // toggle inside the grid's group chrome bubbles up here, and the handler optionally
            // walks descendant Expanders.
            EventManager.RegisterClassHandler(
                typeof(SearchDataGrid),
                Expander.ExpandedEvent,
                new RoutedEventHandler(OnGroupExpanderToggled));
            EventManager.RegisterClassHandler(
                typeof(SearchDataGrid),
                Expander.CollapsedEvent,
                new RoutedEventHandler(OnGroupExpanderToggled));
        }

        #endregion

        #region Grouping State

        /// <summary>
        /// Group paths that grouping currently owns at the front of
        /// <see cref="ItemCollection.SortDescriptions"/>. Tracked so the header-click sort path
        /// (<see cref="ApplyColumnSort"/>) can leave them untouched and the rebuild can replace
        /// only its own descriptions — per D2, grouping leads sorting and the two must not clobber
        /// each other. Kept in <see cref="GroupLevel"/> order.
        /// </summary>
        private readonly List<string> _groupSortPaths = new();

        /// <summary>
        /// Descriptor list parallel to <see cref="_groupSortPaths"/>. Lets a rebuild identify
        /// columns that just lost their group sort so the engine can clear their
        /// <see cref="DataGridColumn.SortDirection"/> (otherwise the header arrow lingers after
        /// ungrouping, since the engine had pushed the group direction onto it).
        /// </summary>
        private readonly List<GridColumn> _groupSortDescriptors = new();

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
        /// their own <see cref="GridColumn.AllowGrouping"/>; <c>false</c> blocks grouping for the
        /// whole grid and ungroups any currently-grouped columns. Each column resolves
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
            // A column that just lost permission must be ungrouped — the engine clears it.
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
        /// When <c>true</c>, group expand/collapse uses a custom animated Expander template
        /// (chevron rotation + content scale). When <c>false</c> (default), the default Expander
        /// template is used and toggling is instant. WPF's built-in Expander has no animation; the
        /// animated template is themed as a <see cref="ControlTemplate"/> swap in the
        /// <c>GridSearchDataGridGroupStyle</c> resource.
        /// </summary>
        public static readonly DependencyProperty UseGroupExpansionAnimationProperty =
            DependencyProperty.Register(
                nameof(UseGroupExpansionAnimation),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false));

        /// <inheritdoc cref="UseGroupExpansionAnimationProperty"/>
        public bool UseGroupExpansionAnimation
        {
            get => (bool)GetValue(UseGroupExpansionAnimationProperty);
            set => SetValue(UseGroupExpansionAnimationProperty, value);
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

        /// <summary>
        /// Re-entry guard for the recursive expansion cascade. Set while the cascade is walking
        /// descendant Expanders so each child's own bubbling Expanded/Collapsed event doesn't
        /// re-trigger the walk.
        /// </summary>
        private bool _cascadingExpansion;

        private static void OnAutoExpandAllGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid) return;
            // "Set all groups to this state" is an explicit override of per-group memory: clear
            // the persistence map so previously-toggled (and currently virtualized) groups don't
            // fight the new default when they next realize.
            grid._groupExpandState.Clear();
            // The Style binding only fires when a group is first realized — that gives newly
            // materialized groups the right initial state. Already-realized groups need this active
            // push so a runtime toggle is felt immediately. SetAllGroupsExpanded fires Expanded /
            // Collapsed events which re-populate the map for the realized set.
            grid.SetAllGroupsExpanded((bool)e.NewValue);
        }

        /// <summary>
        /// Class-level handler for <see cref="Expander.ExpandedEvent"/> and
        /// <see cref="Expander.CollapsedEvent"/>. Captures the new state into
        /// <see cref="_groupExpandState"/> so it survives container recycling
        /// (<see cref="System.Windows.Data.ICollectionView.Refresh"/> / virtualization), and — when
        /// <see cref="ExpandGroupsRecursively"/> is on — cascades the same state onto every
        /// descendant Expander of the originating <see cref="GroupItem"/>.
        /// </summary>
        private static void OnGroupExpanderToggled(object sender, RoutedEventArgs e)
        {
            if (sender is not SearchDataGrid grid) return;
            if (e.OriginalSource is not Expander originating) return;

            var groupItem = VisualTreeHelperMethods.FindVisualAncestor<GroupItem>(originating);
            if (groupItem == null) return;

            bool expanded = originating.IsExpanded;

            // Persist before cascading so reentries from descendant events also land in the map.
            string key = ComputeGroupKey(groupItem);
            if (!string.IsNullOrEmpty(key)) grid._groupExpandState[key] = expanded;

            if (!grid.ExpandGroupsRecursively) return;
            if (grid._cascadingExpansion) return;

            grid._cascadingExpansion = true;
            try
            {
                // Scope the cascade to the originating group's subtree — sibling groups stay
                // untouched. Each descendant toggle re-enters this handler, which captures its
                // state into the map (the guard above skips the inner cascade walk).
                foreach (var descendant in EnumerateDescendants<Expander>(groupItem))
                {
                    if (descendant == originating) continue;
                    if (descendant.IsExpanded != expanded)
                        descendant.IsExpanded = expanded;
                }
            }
            finally
            {
                grid._cascadingExpansion = false;
            }
        }

        #endregion

        #region Group Expansion Persistence

        /// <summary>
        /// Captured expand state by group key (joined <see cref="System.Windows.Data.CollectionViewGroup.Name"/>
        /// path of <see cref="GroupItem"/> ancestors). Populated as the user toggles expanders and
        /// replayed when a container realizes again — so a group keeps its state across
        /// <see cref="System.Windows.Data.ICollectionView.Refresh"/>, item recycling, and
        /// scroll-virtualization. Cleared by every explicit "apply to all" operation
        /// (<see cref="ExpandAllGroups"/>, <see cref="CollapseAllGroups"/>,
        /// <see cref="ClearGrouping"/>, <see cref="AutoExpandAllGroups"/> change) so those override
        /// per-group memory the way a user expects.
        /// </summary>
        private readonly Dictionary<string, bool> _groupExpandState = new();

        /// <summary>
        /// Attached property set in the default <see cref="System.Windows.Controls.GroupStyle"/> to
        /// opt each group Expander into expansion-state persistence. When true, the Expander
        /// subscribes to <see cref="FrameworkElement.LoadedEvent"/> so the grid replays the
        /// previously-captured state on each realization. Loaded is a direct-routed event so a
        /// class handler on the grid can't catch it — this attached behavior wires the per-instance
        /// subscription from the theme.
        /// </summary>
        public static readonly DependencyProperty TrackGroupExpansionProperty =
            DependencyProperty.RegisterAttached(
                "TrackGroupExpansion",
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(false, OnTrackGroupExpansionChanged));

        /// <inheritdoc cref="TrackGroupExpansionProperty"/>
        public static void SetTrackGroupExpansion(DependencyObject element, bool value)
            => element.SetValue(TrackGroupExpansionProperty, value);

        /// <inheritdoc cref="TrackGroupExpansionProperty"/>
        public static bool GetTrackGroupExpansion(DependencyObject element)
            => (bool)element.GetValue(TrackGroupExpansionProperty);

        private static void OnTrackGroupExpansionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Expander expander) return;
            if ((bool)e.NewValue)
                expander.Loaded += OnTrackedExpanderLoaded;
            else
                expander.Loaded -= OnTrackedExpanderLoaded;
        }

        /// <summary>
        /// Per-instance <see cref="FrameworkElement.Loaded"/> handler for tracked group expanders.
        /// On every realization (initial materialize, Items.Refresh, virtualization recycle) looks
        /// up the captured state for this group's key and pushes it via
        /// <see cref="DependencyObject.SetCurrentValue"/> — which overrides the
        /// <see cref="AutoExpandAllGroups"/> Style binding's initial value without breaking the
        /// binding (a later user toggle still wins, and an
        /// <see cref="AutoExpandAllGroups"/> change still clears the map and re-applies).
        /// </summary>
        private static void OnTrackedExpanderLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander expander) return;

            var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(expander);
            if (grid == null) return;

            var groupItem = VisualTreeHelperMethods.FindVisualAncestor<GroupItem>(expander);
            if (groupItem == null) return;

            string key = ComputeGroupKey(groupItem);
            if (string.IsNullOrEmpty(key)) return;

            if (grid._groupExpandState.TryGetValue(key, out bool desired) && expander.IsExpanded != desired)
                expander.SetCurrentValue(Expander.IsExpandedProperty, desired);
        }

        /// <summary>
        /// Separator joining <see cref="GroupItem"/> ancestor names into a single key. ASCII Unit
        /// Separator (U+001F) — a non-printing control character unlikely to appear in real group
        /// values — so names containing '/' or other punctuation don't collide.
        /// </summary>
        private const string GroupKeySeparator = "";

        /// <summary>
        /// Builds a stable string key for a <see cref="GroupItem"/> by walking its
        /// <see cref="GroupItem"/> ancestor chain (outermost first) and joining each ancestor's
        /// <see cref="System.Windows.Data.CollectionViewGroup.Name"/>.
        /// </summary>
        private static string ComputeGroupKey(GroupItem groupItem)
        {
            if (groupItem == null) return null;

            var names = new List<string>();
            DependencyObject current = groupItem;
            while (current != null)
            {
                if (current is GroupItem gi && gi.DataContext is CollectionViewGroup cvg)
                    names.Add(cvg.Name?.ToString() ?? string.Empty);
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            if (names.Count == 0) return null;
            names.Reverse();
            return string.Join(GroupKeySeparator, names);
        }

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

            // Ungrouping invalidates every previously-captured group key — drop the persistence
            // map so a later re-grouping starts fresh rather than replaying stale state.
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
        /// Expands every group's expander, including currently-virtualized groups. Routes through
        /// <see cref="AutoExpandAllGroups"/> so the Style binding's default flips with the call —
        /// when a virtualized group later realizes, its initial <see cref="Expander.IsExpanded"/>
        /// reflects the new default (the persistence map, cleared by the change handler, no
        /// longer holds an override). When the property is already at the target value the
        /// change handler won't fire, so the walk runs directly to catch any groups the user
        /// has toggled since the last call.
        /// </summary>
        public void ExpandAllGroups() => SetAllGroupsExpandedAndDefault(true);

        /// <summary>
        /// Collapses every group's expander, including currently-virtualized groups. Routes
        /// through <see cref="AutoExpandAllGroups"/> so the Style binding's default flips with
        /// the call — when a virtualized group later realizes, its initial
        /// <see cref="Expander.IsExpanded"/> reflects the new default. See
        /// <see cref="ExpandAllGroups"/> for the symmetric details on map clearing and the
        /// already-at-target fall-through.
        /// </summary>
        public void CollapseAllGroups() => SetAllGroupsExpandedAndDefault(false);

        /// <summary>
        /// Shared body for <see cref="ExpandAllGroups"/> / <see cref="CollapseAllGroups"/>.
        /// Flips <see cref="AutoExpandAllGroups"/> when needed (the change handler clears the
        /// persistence map and walks realized expanders); when the property is already at
        /// <paramref name="expanded"/> the handler won't fire, so the work runs here directly.
        /// </summary>
        private void SetAllGroupsExpandedAndDefault(bool expanded)
        {
            if (AutoExpandAllGroups != expanded)
            {
                // OnAutoExpandAllGroupsChanged clears the persistence map and pushes the new
                // state to every realized expander; newly-realized (virtualized) groups pick
                // up the changed default through their Style binding.
                SetCurrentValue(AutoExpandAllGroupsProperty, expanded);
                return;
            }

            // Property unchanged — the change handler won't fire. Clear the map and push the
            // state to realized groups explicitly so a repeat call still collapses groups the
            // user has expanded manually since the last "all" instruction.
            _groupExpandState.Clear();
            SetAllGroupsExpanded(expanded);
        }

        /// <summary>
        /// Walks the realized item containers and sets <see cref="Expander.IsExpanded"/> on every
        /// group expander. Only realized groups are affected — virtualized groups inherit the
        /// template default (<see cref="AutoExpandAllGroups"/>) when they materialize.
        /// </summary>
        private void SetAllGroupsExpanded(bool expanded)
        {
            foreach (var groupItem in EnumerateDescendants<GroupItem>(this))
            {
                var expander = VisualTreeHelperMethods.FindVisualDescendant<Expander>(groupItem);
                if (expander != null) expander.IsExpanded = expanded;
            }
        }

        /// <summary>Depth-first enumeration of visual descendants of type <typeparamref name="T"/>.</summary>
        private static IEnumerable<T> EnumerateDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var descendant in EnumerateDescendants<T>(child))
                    yield return descendant;
            }
        }

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
        /// Single source of grouping truth. Collects the grouped columns (ordered and normalized by
        /// <see cref="GridColumn.GroupIndex"/>), rebuilds <see cref="ItemCollection.GroupDescriptions"/>
        /// inside a <see cref="ItemCollection.DeferRefresh"/>, reconciles the grouping-owned leading
        /// <see cref="SortDescription"/>s (D2 — grouping leads sorting), and pushes
        /// <see cref="GridColumn.GroupLevel"/> / <see cref="GridColumn.IsGrouped"/> back to each
        /// descriptor.
        /// </summary>
        internal void RebuildGroupDescriptions()
        {
            if (_rebuildingGroups) return;

            var descriptors = GridColumns;
            if (descriptors == null) return;

            var items = Items;
            if (items == null) return;

            var candidates = descriptors
                .Where(d => d != null && d.GroupIndex >= 0)
                .OrderBy(d => d.GroupIndex)
                .ToList();

            // Fast path for the common ungrouped grid: nothing to group, nothing previously owned,
            // no stray GroupDescriptions. Skip the DeferRefresh and the SortDescriptions reconcile
            // entirely so an ungrouped grid never pays for grouping it doesn't use.
            if (candidates.Count == 0 && _groupSortPaths.Count == 0 && items.GroupDescriptions.Count == 0)
            {
                if (GroupCount != 0) SetValue(GroupCountPropertyKey, 0);
                return;
            }

            _rebuildingGroups = true;
            try
            {
                // Gate + validate (D5). A column that isn't allowed to group or fails validation has
                // its GroupIndex cleared here (re-entrant writes are swallowed by the guard above)
                // and falls into the ungrouped projection.
                var grouped = new List<GridColumn>();
                foreach (var descriptor in candidates)
                {
                    if (descriptor.ActualAllowGrouping && descriptor.Validate())
                        grouped.Add(descriptor);
                    else
                        descriptor.GroupIndex = -1;
                }

                // Snapshot the previously-grouped descriptor set BEFORE ReconcileGroupSortDescriptions
                // clears _groupSortDescriptors — ApplyGroupSortDirectionsToColumns uses it to find
                // descriptors that just left the grouping so it can clear their SortDirection.
                var previouslyGrouped = _groupSortDescriptors.ToList();

                using (items.DeferRefresh())
                {
                    // Normalize GroupIndex to a contiguous 0..N-1 range and project GroupLevel /
                    // IsGrouped. Re-entrant GroupIndex writes here are swallowed by the guard above.
                    for (int level = 0; level < grouped.Count; level++)
                    {
                        var descriptor = grouped[level];
                        if (descriptor.GroupIndex != level) descriptor.GroupIndex = level;
                        descriptor.SetGroupLevel(level);
                        descriptor.SetIsGrouped(true);
                    }

                    // Clear projections on every ungrouped descriptor.
                    foreach (var descriptor in descriptors)
                    {
                        if (descriptor == null || descriptor.GroupIndex >= 0) continue;
                        descriptor.SetGroupLevel(-1);
                        descriptor.SetIsGrouped(false);
                    }

                    items.GroupDescriptions.Clear();
                    foreach (var descriptor in grouped)
                    {
                        string path = descriptor.ResolveGroupPath();
                        if (string.IsNullOrEmpty(path)) continue;
                        items.GroupDescriptions.Add(CreateGroupDescription(descriptor, path));
                    }

                    ReconcileGroupSortDescriptions(grouped);
                }

                // After DeferRefresh: push SortDirection onto each grouped column AND clear it on
                // descriptors that just left the grouping. Done outside DeferRefresh because WPF's
                // DataGrid resyncs DataGridColumn.SortDirection from SortDescriptions when the
                // deferred refresh flushes — our values need to land last to stick.
                ApplyGroupSortDirectionsToColumns(previouslyGrouped);

                SetValue(GroupCountPropertyKey, grouped.Count);

                // Mirror the ordered grouped set into the observable collection that the
                // GroupPanel binds to. Cheaper than diff-ing — the GroupPanel rebinds in one
                // shot and the panel itself is tiny.
                _groupedColumnsBacking.Clear();
                foreach (var descriptor in grouped) _groupedColumnsBacking.Add(descriptor);

                // Hide grouped columns (D3) / restore ungrouped ones now that IsGrouped is settled.
                ApplyGroupColumnVisibility();

                // Defer the GroupStyle resource resolution until grouping is actually used so
                // ungrouped grids never pay the lookup.
                if (grouped.Count > 0) EnsureGroupStyle();
            }
            finally
            {
                _rebuildingGroups = false;
            }

            // Grouping just changed — the sticky strip's active chain may now have more, fewer, or
            // wholly-different entries (columns added/removed, GroupLevel renormalized). Run the
            // resolver once; it bails out cheaply when AllowFixedGroups=false. Called outside the
            // _rebuildingGroups try/finally so the resolver's own visual-tree walk doesn't see a
            // half-applied state. Scroll-driven updates thereafter come from OnScrollViewerScrollChanged.
            UpdateFixedGroupHeaders();
        }

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
        /// Rewrites the grouping-owned leading <see cref="SortDescription"/>s so that, per D2, each
        /// grouped column has a group-path sort at the front of <see cref="ItemCollection.SortDescriptions"/>
        /// in <see cref="GridColumn.GroupLevel"/> order, ahead of any user sorts.
        /// <see cref="PropertyGroupDescription"/> only buckets values; WPF orders the buckets by the
        /// matching <see cref="SortDescription"/>, so a group with no sort would render in arbitrary
        /// order. The owned paths are tracked in <see cref="_groupSortPaths"/> so the header-click
        /// path leaves them alone.
        /// </summary>
        private void ReconcileGroupSortDescriptions(List<GridColumn> grouped)
        {
            var sorts = Items?.SortDescriptions;
            if (sorts == null) return;

            // Drop any sort whose path this engine previously owned — we re-add the current set
            // below. A path that is still grouped is re-inserted; one that just ungrouped is gone.
            foreach (var path in _groupSortPaths)
            {
                for (int i = sorts.Count - 1; i >= 0; i--)
                {
                    if (sorts[i].PropertyName == path)
                    {
                        sorts.RemoveAt(i);
                        break;
                    }
                }
            }
            _groupSortPaths.Clear();
            _groupSortDescriptors.Clear();

            int insertAt = 0;
            foreach (var descriptor in grouped)
            {
                string path = descriptor.ResolveGroupPath();
                if (string.IsNullOrEmpty(path)) continue;

                // A user sort on this same member would otherwise compete with the group sort;
                // collapse it into the single grouping-owned entry at the front.
                for (int i = sorts.Count - 1; i >= 0; i--)
                {
                    if (sorts[i].PropertyName == path) sorts.RemoveAt(i);
                }

                // Explicit SortOrder (header click / programmatic) wins; otherwise fall back to
                // the column's DefaultGroupBySortDirection (Ascending unless overridden per-column).
                ColumnSortOrder effective = descriptor.SortOrder != ColumnSortOrder.None
                    ? descriptor.SortOrder
                    : descriptor.DefaultGroupBySortDirection;
                var direction = effective == ColumnSortOrder.Descending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                sorts.Insert(insertAt++, new SortDescription(path, direction));
                _groupSortPaths.Add(path);
                _groupSortDescriptors.Add(descriptor);
            }
        }

        /// <summary>
        /// Pushes the resolved group sort direction onto each currently-grouped
        /// <see cref="DataGridColumn.SortDirection"/>, and clears it on descriptors that just
        /// left the grouping (no lingering header / pill arrow). Called from
        /// <see cref="RebuildGroupDescriptions"/> AFTER the <see cref="ItemCollection.DeferRefresh"/>
        /// block has exited — WPF's DataGrid syncs column SortDirection from SortDescriptions
        /// when the deferred refresh flushes, and our values need to land last to stick.
        /// </summary>
        /// <remarks>
        /// The push fires the <see cref="HookSortObservation"/> value-changed handler, which
        /// mirrors <see cref="DataGridColumn.SortDirection"/> onto
        /// <see cref="ColumnDataBase.SortOrder"/> — the property the group pill's
        /// <c>DataTrigger</c> binds to. Without this method the pill arrow would flash on first
        /// render and then be cleared by WPF's resync.
        /// </remarks>
        private void ApplyGroupSortDirectionsToColumns(List<GridColumn> previouslyGrouped)
        {
            var sorts = Items?.SortDescriptions;
            if (sorts == null) return;

            // Build a path → direction lookup from the just-applied group sorts (always the
            // front of the SortDescriptions collection, in _groupSortPaths order).
            var pathToDirection = new Dictionary<string, ListSortDirection>(StringComparer.Ordinal);
            foreach (var path in _groupSortPaths)
            {
                for (int i = 0; i < sorts.Count; i++)
                {
                    if (sorts[i].PropertyName == path)
                    {
                        pathToDirection[path] = sorts[i].Direction;
                        break;
                    }
                }
            }

            // Push direction onto every currently-grouped descriptor's underlying column.
            foreach (var descriptor in _groupSortDescriptors)
            {
                if (descriptor?.InternalColumn == null) continue;
                string path = descriptor.ResolveGroupPath();
                if (string.IsNullOrEmpty(path)) continue;
                if (!pathToDirection.TryGetValue(path, out var direction)) continue;

                if (descriptor.InternalColumn.SortDirection != direction)
                    descriptor.InternalColumn.SortDirection = direction;
            }

            // Clear SortDirection on descriptors that were grouped before this rebuild but
            // aren't anymore — unless a user sort survives on the same path.
            foreach (var prev in previouslyGrouped)
            {
                if (prev == null || prev.InternalColumn == null) continue;
                if (_groupSortDescriptors.Contains(prev)) continue;

                string path = prev.ResolveGroupPath();
                bool hasUserSort = false;
                if (!string.IsNullOrEmpty(path))
                {
                    for (int i = 0; i < sorts.Count; i++)
                    {
                        if (sorts[i].PropertyName == path) { hasUserSort = true; break; }
                    }
                }
                if (!hasUserSort && prev.InternalColumn.SortDirection != null)
                    prev.InternalColumn.SortDirection = null;
            }
        }

        /// <summary>
        /// True when <paramref name="path"/> is currently a grouping-owned leading sort. Consulted
        /// by <see cref="ApplyColumnSort"/> so header-click sorting neither clears nor duplicates a
        /// group sort.
        /// </summary>
        private bool IsGroupSortPath(string path)
            => !string.IsNullOrEmpty(path) && _groupSortPaths.Contains(path);

        /// <summary>
        /// Resets the grouping projections on a descriptor that is leaving the grid, so a later
        /// re-add starts clean. The caller rebuilds the grouping afterward to drop the column's
        /// <see cref="ItemCollection.GroupDescriptions"/> entry.
        /// </summary>
        internal void UnhookGroupObservation(GridColumn descriptor)
        {
            if (descriptor == null) return;
            descriptor.SetGroupLevel(-1);
            descriptor.SetIsGrouped(false);
        }

        /// <summary>
        /// Lazily attaches the default <see cref="System.Windows.Controls.GroupStyle"/> (an expander
        /// group header with the group value and item count) the first time the grid is grouped.
        /// Pulled from the theme by key so a retheme can override it. No-op when a consumer already
        /// supplied a <see cref="System.Windows.Controls.GroupStyle"/>.
        /// </summary>
        private void EnsureGroupStyle()
        {
            if (GroupStyle.Count > 0) return;
            if (TryFindResource(ThemeKeys.GridSearchDataGridGroupStyle) is System.Windows.Controls.GroupStyle style)
                GroupStyle.Add(style);
        }

        #endregion
    }
}
