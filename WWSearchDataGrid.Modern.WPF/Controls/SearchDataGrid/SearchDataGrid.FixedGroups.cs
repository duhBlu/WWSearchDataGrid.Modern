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

        /// <summary>
        /// The sticky strip's entry presenter (<c>PART_FixedGroupHeaders</c>) and the drop-shadow
        /// panel (<c>PART_FixedGroupShadow</c>) that rides the strip's visible bottom edge. Both
        /// live inside the inner scroll-viewer's template, so they're descendant-resolved (like
        /// <see cref="_scrollContentPresenter"/>) and cached for the template's lifetime.
        /// </summary>
        private FixedGroupHeadersPresenter _fixedGroupHeadersPresenter;
        private FrameworkElement _fixedGroupShadow;

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
            ResetStripPush();
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
        /// scrolled above the top — so no per-ancestor geometry is needed (an ancestor of a visible
        /// data row is necessarily expanded).
        /// </summary>
        /// <remarks>
        /// When the anchor is itself a header that has begun scrolling off the top (its top is above
        /// the viewport top) and is expanded — so its own content lies below it — the header is
        /// pinned too, holding it at the line instead of letting it scroll past for a header-height
        /// before the next chain snaps in. A header still resting exactly at the top, or a collapsed
        /// header, pins only its ancestors; so a flat run of collapsed top-level headers scrolls past
        /// freely (chain empty, strip hidden), unchanged from before. Headers still below the
        /// viewport top but inside the strip's stair-step are docked by the forward pass
        /// (<see cref="ResolveUpcomingHeaders"/>), which also yields the push translate.
        /// </remarks>
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

            // Seed the chain and decide the deepest ancestor level still needed. For a data-row
            // anchor, every enclosing header pins (needed = innermost level). For a header anchor,
            // pin its ancestors (needed = its level − 1); additionally pin the header itself once it
            // has scrolled above the top with its own content below it (expanded) — that's what
            // holds the incoming group at the line instead of letting it scroll past before the swap.
            // The < 0 test treats a header resting exactly at the top as not-yet-scrolled-off.
            var chain = new List<GroupHeaderRow>(GroupCount);
            int needed;
            if (_groupRows[anchorIndex] is GroupHeaderRow anchorHeader)
            {
                double anchorTop = topmost.TranslatePoint(new Point(0, 0), host).Y;
                if (anchorHeader.IsExpanded && anchorTop < -0.5)
                    chain.Add(anchorHeader);
                needed = anchorHeader.Level - 1;
            }
            else
            {
                needed = GroupCount - 1;
            }

            if (needed < 0 && chain.Count == 0)
            {
                ClearStrip();
                return;
            }

            // Walk back collecting the nearest preceding header at each successively-shallower level.
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

            double pushY = ResolveUpcomingHeaders(host, anchorIndex, chain, out int pushLevel);

            var entries = new List<FixedGroupHeaderEntry>(chain.Count);
            foreach (var h in chain)
                entries.Add(new FixedGroupHeaderEntry(h.Level, h.Node, h.OwningColumn, this));

            ApplyEntries(entries);
            ApplyStripPush(pushLevel, pushY);
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
        /// Forward pass over the headers below the anchor: docks every header that has crossed its
        /// per-level pin line into <paramref name="chain"/> (mutated in place), and returns the
        /// push translate for the first header still short of its line — the boundary. The pin
        /// line for a level-N header is the bottom edge of the pinned entries above it (cumulative
        /// height of levels 0..N−1), so an incoming header docks the instant its real row touches
        /// the chain's stair-step — its pinned copy renders exactly where the real row sits at that
        /// moment — instead of sliding on under the strip to the viewport top and snapping in late.
        /// </summary>
        /// <remarks>
        /// A header that crossed its line owns its level: any deeper entries belong to the group it
        /// just ended and are dropped, and the header itself is appended only when expanded (a
        /// collapsed header has no content below it, so it scrolls on under the strip like a data
        /// row). The boundary's push slides the chain suffix at indices ≥ its level so the suffix's
        /// bottom edge rides the incoming header's top: 0 when the header's top is at the strip's
        /// bottom, −(suffix height) at its pin line — by which point the dock swap has replaced the
        /// suffix and the transform rests. A level-0 boundary pushes the whole chain, which is the
        /// old whole-strip behavior. Symmetric on the way back up: the outgoing header descending
        /// through the band slides the freshly-resolved suffix back down from under the entries
        /// above it. Pixel-scroll only — item-mode offsets jump a whole row at a time, so there the
        /// suffix snaps (translate stays 0); docking applies in both modes. Driven from
        /// <c>ScrollChanged</c>, which fires per frame during smooth scrolling, so the transform
        /// and the chain resolve stay on the same tick — no separate render loop, no
        /// content/transform desync.
        /// </remarks>
        private double ResolveUpcomingHeaders(ScrollContentPresenter host, int anchorIndex, List<GroupHeaderRow> chain, out int pushLevel)
        {
            pushLevel = -1;

            // A header interacting with the strip sits within a few rows below the viewport top
            // (the anchor), so cap the forward scan — without it, scrolling deep inside one huge
            // group (no following header for thousands of rows) would walk to the group's end
            // every frame. Anything past the cap is far below the strip and never docks or pushes.
            const int maxLookahead = 128;
            int scanEnd = Math.Min(_groupRows.Count, anchorIndex + 1 + maxLookahead);

            for (int i = anchorIndex + 1; i < scanEnd; i++)
            {
                if (!(_groupRows[i] is GroupHeaderRow header)) continue;

                // An unrealized container means the row is below the realization window, far
                // outside the strip's reach — and so is every row after it.
                if (!(ItemContainerGenerator.ContainerFromItem(header) is FrameworkElement container) || !container.IsVisible)
                    break;

                // Document order guarantees a header's parent precedes it, so a level deeper than
                // the chain can only follow a collapsed (undocked) parent — out of the strip's reach.
                if (header.Level > chain.Count) break;

                double top = container.TranslatePoint(new Point(0, 0), host).Y;
                double pinLine = PinnedEntryHeightAbove(header.Level);

                if (top < pinLine - 0.5)
                {
                    if (chain.Count > header.Level)
                        chain.RemoveRange(header.Level, chain.Count - header.Level);
                    if (header.IsExpanded)
                        chain.Add(header);
                    continue;
                }

                // First header short of its pin line is the push boundary; nothing below it can
                // touch the strip this frame.
                if (AllowPerPixelScrolling && header.Level < chain.Count)
                {
                    double stripHeight = PinnedEntryHeightAbove(chain.Count);
                    if (stripHeight > 0 && top < stripHeight)
                    {
                        pushLevel = header.Level;
                        return top - stripHeight;
                    }
                }
                break;
            }

            return 0;
        }

        /// <summary>
        /// Cumulative rendered height of the pinned entries at indices below
        /// <paramref name="level"/> — the stair-step pin line a level-N header docks against, and,
        /// passed the full chain length, the strip's content height (excluding the drop shadow).
        /// An entry whose container hasn't rendered yet (docked this pass, generated on the next
        /// layout) borrows the last rendered height seen, exact in practice since every entry
        /// shares the strip's item template.
        /// </summary>
        private double PinnedEntryHeightAbove(int level)
        {
            var presenter = ResolveFixedGroupHeadersPresenter();
            if (presenter == null) return 0;

            var generator = presenter.ItemContainerGenerator;
            double sum = 0, lastRendered = 0;
            for (int i = 0; i < level; i++)
            {
                double height = (generator.ContainerFromIndex(i) as FrameworkElement)?.ActualHeight ?? 0;
                if (height > 0) lastRendered = height; else height = lastRendered;
                sum += height;
            }
            return sum;
        }

        /// <summary>
        /// Lazily resolves and caches the strip's entry presenter (<c>PART_FixedGroupHeaders</c>)
        /// inside the inner scroll viewer's template — the source of the pinned-entry containers
        /// the pin-line heights and push transforms are read from and written to.
        /// </summary>
        private FixedGroupHeadersPresenter ResolveFixedGroupHeadersPresenter()
        {
            if (_fixedGroupHeadersPresenter != null) return _fixedGroupHeadersPresenter;
            if (_scrollViewer == null) return null;

            _fixedGroupHeadersPresenter = VisualTreeHelperMethods.FindVisualDescendant<FixedGroupHeadersPresenter>(
                _scrollViewer, "PART_FixedGroupHeaders");
            return _fixedGroupHeadersPresenter;
        }

        /// <summary>
        /// Lazily resolves and caches the drop-shadow panel (<c>PART_FixedGroupShadow</c>) below the
        /// pinned entries — translated with the pushed suffix so the shadow stays on the strip's
        /// visible bottom edge instead of floating where the resting bottom was.
        /// </summary>
        private FrameworkElement ResolveFixedGroupShadow()
        {
            if (_fixedGroupShadow != null) return _fixedGroupShadow;
            if (_scrollViewer == null) return null;

            _fixedGroupShadow = VisualTreeHelperMethods.FindVisualDescendant<DockPanel>(
                _scrollViewer, "PART_FixedGroupShadow");
            return _fixedGroupShadow;
        }

        /// <summary>
        /// Applies the push to the chain suffix: every pinned entry at index ≥
        /// <paramref name="pushLevel"/>, plus the drop shadow, is translated by
        /// <paramref name="translateY"/>; entries above the boundary's level hold still. Z-order is
        /// stamped outermost-on-top so a pushed suffix tucks under the entry above it instead of
        /// drawing over it (a StackPanel renders later children on top). Transforms are written
        /// only on change so an unchanged scroll frame triggers no re-render; a
        /// <paramref name="pushLevel"/> of −1 rests the whole strip.
        /// </summary>
        private void ApplyStripPush(int pushLevel, double translateY)
        {
            var presenter = ResolveFixedGroupHeadersPresenter();
            if (presenter == null) return;

            var generator = presenter.ItemContainerGenerator;
            int count = _fixedGroupHeadersBacking.Count;
            for (int i = 0; i < count; i++)
            {
                if (!(generator.ContainerFromIndex(i) is FrameworkElement entry)) continue;
                Panel.SetZIndex(entry, count - i);
                SetTranslateY(entry, pushLevel >= 0 && i >= pushLevel ? translateY : 0);
            }

            var shadow = ResolveFixedGroupShadow();
            if (shadow != null)
                SetTranslateY(shadow, pushLevel >= 0 ? translateY : 0);
        }

        /// <summary>
        /// Writes a vertical render translate, materializing the element's
        /// <see cref="TranslateTransform"/> on first non-zero use and skipping the write when the
        /// value is already current.
        /// </summary>
        private static void SetTranslateY(FrameworkElement element, double y)
        {
            if (!(element.RenderTransform is TranslateTransform transform))
            {
                if (y == 0) return;
                transform = new TranslateTransform();
                element.RenderTransform = transform;
            }
            if (transform.Y != y) transform.Y = y;
        }

        /// <summary>Returns every pinned entry and the shadow to rest.</summary>
        private void ResetStripPush() => ApplyStripPush(-1, 0);

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
