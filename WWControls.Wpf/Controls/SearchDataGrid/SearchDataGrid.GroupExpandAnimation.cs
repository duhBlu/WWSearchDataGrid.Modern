using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WWControls.Wpf
{
    /// <summary>
    /// Animated expand/collapse for the flat grouping projection, gated by
    /// <see cref="AllowGroupExpandAnimation"/>. A normal toggle reflattens the whole projection
    /// with a Reset — every row container recycles, so nothing survives to animate. The animated
    /// path instead splices the toggled group's visible row block in or out of
    /// <see cref="_groupRows"/> in place (the header sentinel, its container, and its chevron all
    /// survive), then plays a render-only rigid slide over the realized containers.
    /// </summary>
    /// <remarks>
    /// The slide never animates layout. The splice's layout change lands in ONE pass; the slide is
    /// a single shared <see cref="TranslateTransform"/> applied as the RenderTransform of the
    /// block's containers AND every realized container below them, animated between -blockHeight
    /// and 0. Because block and below translate together, they never overlap each other; the block
    /// intrudes into the rows above only while translated, so its containers get Panel.ZIndex -1
    /// and render BEHIND the (opaque) header — visually sliding out from under it on expand and
    /// retreating beneath it on collapse, at full natural size. Per tick only the transform
    /// changes — no measure/arrange — so the motion stays smooth at any block size. Earlier
    /// approaches that animated LayoutTransform scale or Height both deformed the content
    /// ("squeezing") and re-ran grid layout every frame ("laggy"); don't go back there.
    /// </remarks>
    public partial class SearchDataGrid
    {
        #region Tunables

        private static readonly Duration GroupExpandAnimationDuration = new(TimeSpan.FromMilliseconds(200));
        private static readonly Duration GroupCollapseAnimationDuration = new(TimeSpan.FromMilliseconds(160));

        /// <summary>
        /// Largest row block the splice path will move. The flat collection raises one
        /// CollectionChanged per spliced row (WPF's generator rejects range notifications), so a
        /// huge group is cheaper — and visually indistinguishable, since only the viewport's rows
        /// realize — on the single-Reset reflatten path.
        /// </summary>
        private const int AnimatedSpliceRowLimit = 400;

        #endregion

        #region Animation state

        /// <summary>The in-flight slide, if any. At most one exists at a time.</summary>
        private GroupSlideOp _activeGroupSlide;

        /// <summary>
        /// Marks a container whose RenderTransform / ZIndex are owned by the active slide, so the
        /// container-lifecycle hooks can reset a recycled container without ever touching rows the
        /// animation never claimed.
        /// </summary>
        private static readonly DependencyProperty SlideOpProperty =
            DependencyProperty.RegisterAttached(
                "SlideOp",
                typeof(object),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        /// <summary>
        /// One slide: the shared transform, every container it was applied to, and — for a
        /// collapse — the row range whose removal is deferred until the slide finishes. Any other
        /// projection mutation completes the op first so the captured range stays valid.
        /// </summary>
        private sealed class GroupSlideOp
        {
            public TranslateTransform Transform;
            public List<FrameworkElement> Containers;
            public bool Expanding;
            public int StartIndex;
            public int Count;
            public GroupNode Node;
            public bool Completing;
        }

        #endregion

        #region Splice toggle

        /// <summary>
        /// Animated counterpart to the ApplyGroupExpansion-then-reflatten toggle. Returns
        /// <c>true</c> when the toggle was handled here (expansion state applied, rows spliced or
        /// a fallback reflatten already run); <c>false</c> to let the caller take the plain path
        /// untouched.
        /// </summary>
        private bool TryToggleGroupSpliced(GroupNode node, bool expanded, GroupHeaderRow header)
        {
            if (!AllowGroupExpandAnimation || !_groupingActive) return false;

            // Group footers move surface on toggle — pinned beneath the header while collapsed,
            // docked at the bottom of the content while expanded — which the contiguous self-block
            // splice below doesn't model. Fall back to the plain reflatten path (FlattenInto places
            // the footer rows correctly) whenever footers are in play.
            if (_projectGroupFooterSummaries) return false;

            // Finish any in-flight slide so the indices computed below are stable.
            CompleteActiveGroupSlide();

            bool selfChanged = GetGroupExpandState(node.PathKey) != expanded;

            // A recursive cascade that doesn't flip this group's own state changes visibility only
            // INSIDE the block (descendant groups), which is not a contiguous self-block splice.
            if (!selfChanged && ExpandGroupsRecursively) return false;

            // Resolve the header sentinel and its flat index — the pinned strip's commands carry
            // only the node. A header spliced out under a collapsed ancestor can't animate.
            if (header == null)
            {
                foreach (var item in _groupRows)
                {
                    if (item is GroupHeaderRow h && ReferenceEquals(h.Node, node))
                    {
                        header = h;
                        break;
                    }
                }
            }
            if (header == null) return false;
            int headerIndex = _groupRows.IndexOf(header);
            if (headerIndex < 0) return false;

            if (!selfChanged)
            {
                // Re-asserting the current state (e.g. a strip command racing a toggle): no rows move.
                header.SetIsExpanded(expanded);
                return true;
            }

            if (expanded)
            {
                // State first (including any recursive cascade), then flatten — the block must
                // reflect the post-toggle nested states.
                ApplyGroupExpansion(node, true);
                SyncNodeExpansion(node);

                var block = new List<object>(node.Count);
                FlattenNodeContents(node, block);

                if (block.Count > AnimatedSpliceRowLimit)
                {
                    RebuildRowProjection();
                    return true;
                }

                header.SetIsExpanded(true);
                if (block.Count > 0)
                {
                    for (int i = 0; i < block.Count; i++)
                        _groupRows.Insert(headerIndex + 1 + i, block[i]);
                    ScheduleExpandSlide(header, block.Count, node);
                }
                OnSplicedToggleCompleted(node);
                return true;
            }
            else
            {
                // The visible block is determined by the CURRENT nested states — capture it before
                // a recursive cascade rewrites them.
                var block = new List<object>(node.Count);
                FlattenNodeContents(node, block);

                ApplyGroupExpansion(node, false);
                SyncNodeExpansion(node);

                if (block.Count > AnimatedSpliceRowLimit)
                {
                    RebuildRowProjection();
                    return true;
                }

                header.SetIsExpanded(false);
                if (block.Count > 0)
                {
                    BeginGroupSlide(headerIndex, block.Count, node, expanding: false);
                }
                else
                {
                    OnSplicedToggleCompleted(node);
                }
                return true;
            }
        }

        /// <summary>
        /// Mirrors the path-keyed expand map back onto <paramref name="node"/>'s subtree. A
        /// reflatten rebuilds nodes from the map; the splice path keeps the existing tree, so the
        /// retained nodes must be re-synced for the next <see cref="FlattenInto"/> (and for the
        /// live reads off <see cref="FixedGroupHeaderEntry.IsExpanded"/>).
        /// </summary>
        private void SyncNodeExpansion(GroupNode node)
        {
            node.IsExpanded = GetGroupExpandState(node.PathKey);
            foreach (var child in node.Children)
                SyncNodeExpansion(child);
        }

        /// <summary>
        /// Flattens the rows that render beneath <paramref name="node"/>'s header while it is
        /// expanded — nested headers and leaf rows, honoring each descendant's own expansion.
        /// </summary>
        private void FlattenNodeContents(GroupNode node, List<object> sink)
        {
            if (node.Children.Count > 0)
                FlattenInto(node.Children, sink);
            else
                sink.AddRange(node.Items);
        }

        /// <summary>
        /// Post-splice notifications: reused pinned-strip entries read the flipped node live but
        /// must announce the change themselves, and the strip chain re-resolves after layout —
        /// the same deferred pass a reflatten schedules.
        /// </summary>
        private void OnSplicedToggleCompleted(GroupNode node)
        {
            foreach (var entry in _fixedGroupHeadersBacking)
            {
                if (ReferenceEquals(entry.Node, node))
                    entry.NotifyIsExpandedChanged();
            }

            if (_groupingActive && AllowFixedGroups)
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateFixedGroupHeaders));
        }

        #endregion

        #region Slide engine

        /// <summary>
        /// Defers the expand slide to the end of the layout pass that realizes the just-inserted
        /// containers. LayoutUpdated fires after measure/arrange but before the frame is rendered,
        /// so the initial translated positions land in the SAME frame as the insert — no flash of
        /// the fully-expanded state. The header sentinel doubles as a staleness guard: if anything
        /// reflattened in between, it is no longer in the projection and the slide is skipped.
        /// </summary>
        private void ScheduleExpandSlide(GroupHeaderRow header, int count, GroupNode node)
        {
            EventHandler handler = null;
            handler = (_, _) =>
            {
                LayoutUpdated -= handler;
                if (!_groupingActive) return;
                int headerIndex = _groupRows.IndexOf(header);
                if (headerIndex < 0) return;
                BeginGroupSlide(headerIndex, count, node, expanding: true);
            };
            LayoutUpdated += handler;
        }

        /// <summary>
        /// Starts the rigid slide for the block at [headerIndex + 1, headerIndex + 1 + count).
        /// Collects the block's realized containers (their summed height is the travel distance)
        /// plus the contiguous run of realized containers below the block, puts the block behind
        /// the rows above it, and animates the one shared transform. Containers below the
        /// realization window don't exist and aren't visible; when nothing of the block is
        /// realized there is nothing to show, so a collapse just removes immediately.
        /// </summary>
        private void BeginGroupSlide(int headerIndex, int count, GroupNode node, bool expanding)
        {
            int start = headerIndex + 1;

            double shift = 0;
            var containers = new List<FrameworkElement>();
            int blockContainerCount = 0;
            for (int i = start; i < start + count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe && fe.ActualHeight > 0)
                {
                    containers.Add(fe);
                    shift += fe.ActualHeight;
                }
            }
            blockContainerCount = containers.Count;

            if (blockContainerCount == 0 || shift <= 0)
            {
                if (!expanding)
                {
                    RemoveSplicedRows(start, count);
                    OnSplicedToggleCompleted(node);
                }
                return;
            }

            // The realized window is contiguous; stop at the first unrealized index. (If the
            // block's own tail is unrealized, everything past it is below the cache and offscreen.)
            for (int i = start + count; i < _groupRows.Count; i++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe)
                    containers.Add(fe);
                else
                    break;
            }

            var transform = new TranslateTransform(0, expanding ? -shift : 0);
            var op = new GroupSlideOp
            {
                Transform = transform,
                Containers = containers,
                Expanding = expanding,
                StartIndex = start,
                Count = count,
                Node = node,
            };
            _activeGroupSlide = op;

            for (int i = 0; i < containers.Count; i++)
            {
                var fe = containers[i];
                fe.SetValue(SlideOpProperty, op);
                // Only the block intrudes into the rows above while translated; the below-run
                // stays at or below the block's start the whole way.
                if (i < blockContainerCount)
                    Panel.SetZIndex(fe, -1);
                fe.RenderTransform = transform;
            }

            var slide = new DoubleAnimation(expanding ? -shift : 0, expanding ? 0 : -shift,
                expanding ? GroupExpandAnimationDuration : GroupCollapseAnimationDuration)
            {
                EasingFunction = new QuadraticEase
                {
                    EasingMode = expanding ? EasingMode.EaseOut : EasingMode.EaseIn,
                },
            };
            slide.Completed += (_, _) =>
            {
                if (ReferenceEquals(_activeGroupSlide, op))
                    CompleteActiveGroupSlide();
            };
            transform.BeginAnimation(TranslateTransform.YProperty, slide);
        }

        /// <summary>
        /// Finishes the in-flight slide NOW: strips the shared transform and, for a collapse,
        /// removes the deferred block. The transform clear and the removal happen in the same
        /// callback — the rows' new layout positions equal their final translated positions, so
        /// nothing moves on screen. Runs from the animation's own completion and from every
        /// projection mutation path (reflatten, detach, the next toggle), so the captured index
        /// range is always applied before anything else moves rows. No-op when nothing is pending.
        /// </summary>
        private void CompleteActiveGroupSlide()
        {
            var op = _activeGroupSlide;
            if (op == null || op.Completing) return;
            op.Completing = true;
            _activeGroupSlide = null;

            op.Transform.BeginAnimation(TranslateTransform.YProperty, null);
            foreach (var fe in op.Containers)
            {
                if (!ReferenceEquals(fe.GetValue(SlideOpProperty), op)) continue;
                fe.ClearValue(SlideOpProperty);
                fe.ClearValue(RenderTransformProperty);
                fe.ClearValue(Panel.ZIndexProperty);
            }

            if (!op.Expanding)
            {
                RemoveSplicedRows(op.StartIndex, op.Count);
                OnSplicedToggleCompleted(op.Node);
            }
        }

        /// <summary>
        /// Resets a container the active slide owns when the container leaves that role — recycled
        /// onto another item or cleared by virtualization mid-slide. Marker-guarded, so rows the
        /// animation never claimed are untouched. The row pops out of the slide (it's leaving the
        /// viewport anyway); the rest of the block continues on the shared transform.
        /// </summary>
        private static void ClearSlideOnContainer(DependencyObject element)
        {
            if (element is not FrameworkElement fe) return;
            if (fe.GetValue(SlideOpProperty) == null) return;
            fe.ClearValue(SlideOpProperty);
            fe.ClearValue(RenderTransformProperty);
            fe.ClearValue(Panel.ZIndexProperty);
        }

        /// <summary>Removes [start, start + count) from the flat projection, last-first so indices hold.</summary>
        private void RemoveSplicedRows(int startIndex, int count)
        {
            for (int i = startIndex + count - 1; i >= startIndex; i--)
                _groupRows.RemoveAt(i);
        }

        #endregion
    }
}
