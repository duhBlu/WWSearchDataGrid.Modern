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
        /// Recomputes the active group chain — the pinned-header entries for the topmost visible
        /// row's enclosing groups — and applies it to <see cref="_fixedGroupHeadersBacking"/> via
        /// minimal in-place mutation (so unchanged slots keep their per-item containers across no-op
        /// scrolls). Cheaply bails when the feature is off or the grid is ungrouped; safe to call
        /// from any point that could change the active chain (scroll changes, grouping rebuilds,
        /// layout updates). The chain is computed by <see cref="BuildFixedGroupChain"/>.
        /// </summary>
        internal void UpdateFixedGroupHeaders()
        {
            // All cheap gates: AllowFixedGroups is a DP read, GroupCount is the read-only DP the
            // engine maintains, and _groupingActive is true exactly when the projection owns the
            // ItemsSource. Skipping when ungrouped means the resolver never walks the visual tree on
            // a non-grouped grid even if the scroll handler is wired up.
            if (!AllowFixedGroups || GroupCount == 0 || !_groupingActive)
            {
                ClearStrip();
                return;
            }

            BuildFixedGroupChain();
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
        /// Writes <paramref name="outerToInner"/> into <see cref="_fixedGroupHeadersBacking"/> in
        /// place: slots whose entry hasn't moved (<see cref="FixedGroupHeaderEntry.Equals"/> — same
        /// level + same group identity) are reused so the strip's per-item containers stay alive
        /// across no-op scrolls; differing slots are replaced and trailing slots beyond the new
        /// chain length are trimmed.
        /// </summary>
        private void ApplyEntries(List<FixedGroupHeaderEntry> outerToInner)
        {
            int target = outerToInner.Count;
            for (int level = 0; level < target; level++)
            {
                var entry = outerToInner[level];
                if (level < _fixedGroupHeadersBacking.Count)
                {
                    if (_fixedGroupHeadersBacking[level].Equals(entry)) continue;
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
        /// Resolver: computes the pinned chain by index into
        /// <see cref="_groupRows"/> instead of walking the visual tree for <see cref="GroupItem"/>
        /// ancestors. Finds the topmost realized row straddling the viewport top (the "anchor"),
        /// then collects the nearest preceding <see cref="GroupHeaderRow"/> at each level shallower
        /// than the anchor's own. Those headers are, by list order, exactly the anchor's ancestors
        /// scrolled above the top — so no per-ancestor geometry or expand-state probing is needed
        /// (an ancestor of a visible data row is necessarily expanded). When the anchor is a
        /// top-level header (no shallower ancestor), the chain is empty and the strip hides — the
        /// flat equivalent of "scrolling through collapsed top-level headers."
        /// </summary>
        private void BuildFixedGroupChain()
        {
            var host = ResolveScrollContentPresenter();
            if (host == null)
            {
                ClearStrip();
                return;
            }

            // Every flat row materializes as a SearchDataGridRow : DataGridRow, so the existing
            // largest-Y-straddle walk returns the row rendering at the viewport top (no GroupItems
            // exist in flat mode for it to pick up).
            FrameworkElement topmost = FindTopmostVisibleRowOrGroup(host);
            if (topmost == null)
            {
                ClearStrip();
                return;
            }

            object anchorItem = ItemContainerGenerator.ItemFromContainer(topmost);
            int anchorIndex = anchorItem == null ? -1 : _groupRows.IndexOf(anchorItem);
            if (anchorIndex < 0)
            {
                ClearStrip();
                return;
            }

            // The deepest ancestor level we still need: one above the anchor header's own level, or
            // the innermost group level when the anchor is a data row (all enclosing headers pin).
            int needed = _groupRows[anchorIndex] is GroupHeaderRow anchorHeader
                ? anchorHeader.Level - 1
                : GroupCount - 1;
            if (needed < 0)
            {
                ClearStrip();
                return;
            }

            // Walk back collecting the nearest preceding header at each successively-shallower level.
            var chain = new List<GroupHeaderRow>(needed + 1);
            for (int i = anchorIndex - 1; i >= 0 && needed >= 0; i--)
            {
                if (_groupRows[i] is GroupHeaderRow h && h.Level == needed)
                {
                    chain.Add(h);
                    needed--;
                }
            }

            if (chain.Count == 0)
            {
                ClearStrip();
                return;
            }

            chain.Reverse(); // outermost first, matching the strip's top-down stair-step

            var entries = new List<FixedGroupHeaderEntry>(chain.Count);
            foreach (var h in chain)
                entries.Add(new FixedGroupHeaderEntry(h.Level, h.Node, h.OwningColumn, this));

            ApplyEntries(entries);
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

        #endregion
    }
}
