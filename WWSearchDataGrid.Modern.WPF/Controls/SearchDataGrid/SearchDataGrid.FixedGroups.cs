using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WWSearchDataGrid.Modern.WPF
{
    public partial class SearchDataGrid
    {
        #region Fixed Group Headers State

        /// <summary>
        /// Backing collection for <see cref="FixedGroupHeaders"/>. Maintained by the active-chain
        /// resolver (<see cref="UpdateFixedGroupHeaders"/>) as the ordered set of pinned-header
        /// entries — outermost group first — for the topmost visible row. Exposed as
        /// <see cref="ObservableCollection{T}"/> so <see cref="FixedGroupHeadersPresenter"/>'s
        /// <c>ItemsControl.ItemsSource</c> binding picks up changes without an explicit
        /// notification dance.
        /// </summary>
        private readonly ObservableCollection<FixedGroupHeaderEntry> _fixedGroupHeadersBacking
            = new ObservableCollection<FixedGroupHeaderEntry>();

        private static readonly DependencyPropertyKey FixedGroupHeadersPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(FixedGroupHeaders),
                typeof(ObservableCollection<FixedGroupHeaderEntry>),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="FixedGroupHeaders"/> dependency property.</summary>
        public static readonly DependencyProperty FixedGroupHeadersProperty = FixedGroupHeadersPropertyKey.DependencyProperty;

        /// <summary>
        /// Ordered observable collection of pinned-header view-models — one per nesting level, in
        /// <see cref="GridColumn.GroupLevel"/> order — that the sticky strip mirrors for the
        /// topmost visible row. Maintained by the active-chain resolver
        /// (<see cref="UpdateFixedGroupHeaders"/>); empty when <see cref="AllowFixedGroups"/> is
        /// false or grouping is inactive.
        /// </summary>
        public ObservableCollection<FixedGroupHeaderEntry> FixedGroupHeaders
            => (ObservableCollection<FixedGroupHeaderEntry>)GetValue(FixedGroupHeadersProperty);

        /// <summary>
        /// Cached <see cref="ScrollContentPresenter"/> from the inner scroll-viewer template —
        /// the scroll-clipped host of the rows panel. Lazily resolved by descendant-walking the
        /// outer <see cref="ScrollViewer"/> (named <c>DG_ScrollViewer</c> by the grid template) on
        /// the first resolver pass, since the name lives inside the scroll viewer's own
        /// <see cref="ControlTemplate"/> and is not a template part of the grid itself.
        /// </summary>
        private ScrollContentPresenter _scrollContentPresenter;

        #endregion

        #region Resolver

        /// <summary>
        /// Recomputes the active group chain — the ordered list of <see cref="CollectionViewGroup"/>
        /// ancestors of the topmost visible row — and applies it to
        /// <see cref="_fixedGroupHeadersBacking"/> via minimal in-place mutation (so existing
        /// entries keep their realized <see cref="FixedGroupHeaderEntry.RepresentedGroupItem"/>
        /// references and the strip's per-item containers stay in place across no-op scrolls).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bails out cheaply when the feature is off or the grid is ungrouped. The resolver is
        /// safe to call from any point that could change the active chain — scroll changes,
        /// grouping rebuilds, layout updates.
        /// </para>
        /// <para>
        /// Algorithm: locate the topmost realized <see cref="DataGridRow"/> or
        /// <see cref="GroupItem"/> in the viewport (see <see cref="FindTopmostVisibleRowOrGroup"/>
        /// for the largest-Y-straddle selection), then walk up to collect every
        /// <see cref="GroupItem"/> ancestor that is BOTH expanded AND has its own header scrolled
        /// above the viewport top. A collapsed group is never pinned — it has no content to
        /// scroll through, so once its header passes the top the user is effectively scrolling
        /// the parent's content, not the collapsed group's. When the filtered chain is empty —
        /// e.g., scrolling through collapsed top-level headers with no expanded ancestor — the
        /// strip and its drop shadow stay hidden.
        /// </para>
        /// </remarks>
        internal void UpdateFixedGroupHeaders()
        {
            // Both gates cheap: AllowFixedGroups is a DP read, GroupCount is the read-only DP the
            // engine maintains. Skipping when ungrouped means the resolver never walks the visual
            // tree on a non-grouped grid even if the scroll handler is wired up.
            if (!AllowFixedGroups || GroupCount == 0)
            {
                ClearStrip();
                return;
            }

            var host = ResolveScrollContentPresenter();
            if (host == null)
            {
                ClearStrip();
                return;
            }

            FrameworkElement topmost = FindTopmostVisibleRowOrGroup(host);
            if (topmost == null)
            {
                ClearStrip();
                return;
            }

            // Pin an ancestor only when it is expanded AND its in-place header has scrolled above
            // the viewport top (Y < 0). The Y<0 gate drops headers still visible in-place (pinning
            // those would stack a duplicate over a header the user already sees). The expanded gate
            // drops collapsed groups: a collapsed group has no content beneath its header to scroll,
            // so scrolling past its header means we're inside the PARENT, not the collapsed group —
            // pinning it (and its drop shadow) would be meaningless. When the filtered chain is
            // empty, ClearStrip leaves the strip and its shadow collapsed.
            var ancestors = CollectGroupItemAncestors(topmost, host);
            if (ancestors.Count == 0)
            {
                ClearStrip();
                return;
            }

            // Outermost first matches the GroupLevel projection the resolver produces and the
            // top-down stair-step the strip renders.
            ancestors.Reverse();

            ApplyActiveChain(ancestors);
        }

        /// <summary>
        /// Empties the pinned-header collection. Called from every resolver bail-out path so the
        /// strip (and its drop shadow, gated on the chain count) is left collapsed — important when
        /// <see cref="AllowFixedGroups"/> toggles off mid-scroll or the user scrolls back above
        /// every group.
        /// </summary>
        private void ClearStrip()
        {
            if (_fixedGroupHeadersBacking.Count > 0) _fixedGroupHeadersBacking.Clear();
        }

        /// <summary>
        /// Reduces the realized <see cref="GroupItem"/> chain into the projected
        /// <see cref="FixedGroupHeaderEntry"/> sequence and writes it into the backing collection
        /// in place. Entries that haven't moved (same level, same <see cref="CollectionViewGroup"/>
        /// identity) are reused — only the trailing slots that differ get replaced — so the strip's
        /// per-item containers don't get torn down on every scroll change.
        /// </summary>
        private void ApplyActiveChain(List<GroupItem> outerToInner)
        {
            int target = outerToInner.Count;
            for (int level = 0; level < target; level++)
            {
                var groupItem = outerToInner[level];
                var cvg = groupItem.DataContext as System.Windows.Data.CollectionViewGroup;
                var column = GetGroupedColumnAtLevel(level);
                var entry = new FixedGroupHeaderEntry(level, cvg, column, groupItem);

                if (level < _fixedGroupHeadersBacking.Count)
                {
                    var existing = _fixedGroupHeadersBacking[level];
                    // Reuse the slot when the (Level, Group) pair hasn't moved — keeps the per-item
                    // container in the strip alive across no-op scrolls. Replace when the group has
                    // changed (sibling-group transition) so the new entry's RepresentedGroupItem
                    // reference is fresh.
                    if (existing.Equals(entry)) continue;
                    _fixedGroupHeadersBacking[level] = entry;
                }
                else
                {
                    _fixedGroupHeadersBacking.Add(entry);
                }
            }

            // Trim any trailing entries beyond the new chain length — happens when the user has
            // scrolled out of a nested group into a shallower part of the hierarchy.
            for (int i = _fixedGroupHeadersBacking.Count - 1; i >= target; i--)
                _fixedGroupHeadersBacking.RemoveAt(i);
        }

        /// <summary>
        /// Lazily resolves and caches the <see cref="ScrollContentPresenter"/> named
        /// <c>PART_ScrollContentPresenter</c> inside the inner scroll viewer's template. The cached
        /// reference is sticky once found, which is correct for the single-template lifetime the
        /// grid uses in practice.
        /// </summary>
        private ScrollContentPresenter ResolveScrollContentPresenter()
        {
            if (_scrollContentPresenter != null) return _scrollContentPresenter;
            if (_scrollViewer == null) return null;

            _scrollContentPresenter = VisualTreeHelperMethods.FindVisualDescendant<ScrollContentPresenter>(
                _scrollViewer, "PART_ScrollContentPresenter");
            return _scrollContentPresenter;
        }

        /// <summary>
        /// Depth-first walks <paramref name="host"/>'s visual descendants and returns the
        /// realized <see cref="DataGridRow"/> or <see cref="GroupItem"/> currently rendered at
        /// the top of the viewport — the innermost element whose vertical extent straddles Y=0.
        /// </summary>
        /// <remarks>
        /// Selection rule: among elements whose extent contains Y=0 (top above the viewport top,
        /// bottom below it), pick the one with the LARGEST top Y. A <see cref="GroupItem"/>'s
        /// visual bounding box covers all its descendants, so when its header has scrolled off
        /// above the viewport, the outer GroupItem's Y is far more negative than the inner
        /// GroupItem's or any visible row's. Picking the largest Y selects the most-nested
        /// element actually rendering at the viewport's top — walking up from there yields every
        /// <see cref="GroupItem"/> ancestor, which the resolver filters by expand-state and Y.
        /// </remarks>
        private static FrameworkElement FindTopmostVisibleRowOrGroup(ScrollContentPresenter host)
        {
            FrameworkElement best = null;
            double bestY = double.NegativeInfinity;
            WalkForTopmost(host, host, ref best, ref bestY);
            return best;
        }

        private static void WalkForTopmost(DependencyObject node, ScrollContentPresenter host, ref FrameworkElement best, ref double bestY)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(node, i);

                if (child is DataGridRow || child is GroupItem)
                {
                    var fe = (FrameworkElement)child;
                    if (fe.IsVisible && fe.ActualHeight > 0)
                    {
                        Point topInHost = fe.TranslatePoint(new Point(0, 0), host);
                        double y = topInHost.Y;
                        if (y <= 0 && y + fe.ActualHeight > 0 && y > bestY)
                        {
                            bestY = y;
                            best = fe;
                        }
                    }
                }

                WalkForTopmost(child, host, ref best, ref bestY);
            }
        }

        /// <summary>
        /// Walks the visual tree upward from <paramref name="start"/> (inclusive) and collects
        /// every <see cref="GroupItem"/> ancestor that qualifies for pinning — innermost first.
        /// A group qualifies when BOTH:
        /// <list type="bullet">
        ///   <item>its in-place header has scrolled above the viewport top (top Y in
        ///         <paramref name="host"/> coordinates is strictly less than 0) — headers still
        ///         visible in-place are skipped so the strip doesn't pin a duplicate over a
        ///         header the user already sees; and</item>
        ///   <item>it is expanded — a collapsed group has no content beneath its header to
        ///         scroll, so once its header passes the top the user is scrolling the parent's
        ///         content, not the collapsed group's. Pinning a collapsed group (and its drop
        ///         shadow) would be meaningless.</item>
        /// </list>
        /// Caller reverses for outer-to-inner ordering when building the pinned chain.
        /// </summary>
        private static List<GroupItem> CollectGroupItemAncestors(DependencyObject start, ScrollContentPresenter host)
        {
            var result = new List<GroupItem>();
            var current = start;
            while (current != null)
            {
                if (current is GroupItem gi)
                {
                    double y = gi.TranslatePoint(new Point(0, 0), host).Y;
                    if (y < 0 && IsGroupItemExpanded(gi)) result.Add(gi);
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return result;
        }

        /// <summary>
        /// Reads the expand state of a <see cref="GroupItem"/>'s own <see cref="Expander"/> —
        /// the first <see cref="Expander"/> in its template subtree (the group's chrome), found
        /// before any nested group's expander on a depth-first walk. Defaults to <c>true</c>
        /// when no expander is realized yet, matching the
        /// <see cref="SearchDataGrid.AutoExpandAllGroups"/> default.
        /// </summary>
        private static bool IsGroupItemExpanded(GroupItem groupItem)
        {
            var expander = VisualTreeHelperMethods.FindVisualDescendant<Expander>(groupItem);
            return expander?.IsExpanded ?? true;
        }

        #endregion
    }
}
