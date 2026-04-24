using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.WPF
{
    #region Event Args

    public class RowAnimationBeginEventArgs : EventArgs
    {
        public DataGridRow Row { get; }
        public DataGridCellsPresenter CellsPresenter { get; }

        /// <summary>
        /// Zero-based index of this row within the current cascade burst. Resets
        /// when the cascade queue drains between reveals. Useful for applying
        /// your own staggered BeginTime in Custom row animations.
        /// </summary>
        public int CascadeIndex { get; }

        /// <summary>
        /// Wall-clock time elapsed since the current burst started.
        /// </summary>
        public TimeSpan BurstElapsed { get; }

        /// <summary>
        /// The BeginTime the built-in Opacity animation would use for this row — already
        /// accounts for slot queue position and any dynamic stagger compression. Apply
        /// this to your Custom animation's BeginTime to match the system cascade timing
        /// exactly.
        /// </summary>
        public TimeSpan BeginTime { get; }

        /// <summary>
        /// Effective per-row stagger in use for the current wave. Differs from the
        /// user's <c>CascadeStagger</c> when the dynamic compression engaged (large
        /// batch of rows revealed together triggers shorter stagger). Use this — not
        /// the DP — if your Custom handler is computing its own row-to-row timing.
        /// </summary>
        public TimeSpan EffectiveStagger { get; }

        /// <summary>
        /// Effective animation duration in use for the current wave. Differs from the
        /// user's <c>RowOpacityAnimationDuration</c> when the dynamic compression engaged.
        /// </summary>
        public TimeSpan EffectiveDuration { get; }

        public RowAnimationBeginEventArgs(DataGridRow row, DataGridCellsPresenter cellsPresenter)
            : this(row, cellsPresenter, 0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero)
        {
        }

        public RowAnimationBeginEventArgs(DataGridRow row, DataGridCellsPresenter cellsPresenter, int cascadeIndex, TimeSpan burstElapsed)
            : this(row, cellsPresenter, cascadeIndex, burstElapsed, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero)
        {
        }

        public RowAnimationBeginEventArgs(
            DataGridRow row,
            DataGridCellsPresenter cellsPresenter,
            int cascadeIndex,
            TimeSpan burstElapsed,
            TimeSpan beginTime,
            TimeSpan effectiveStagger,
            TimeSpan effectiveDuration)
        {
            Row = row;
            CellsPresenter = cellsPresenter;
            CascadeIndex = cascadeIndex;
            BurstElapsed = burstElapsed;
            BeginTime = beginTime;
            EffectiveStagger = effectiveStagger;
            EffectiveDuration = effectiveDuration;
        }
    }

    public class CustomScrollAnimationEventArgs : EventArgs
    {
        public double OldOffset { get; }
        public double NewOffset { get; }
        public System.Windows.Media.Animation.Storyboard Storyboard { get; set; }

        public CustomScrollAnimationEventArgs(double oldOffset, double newOffset)
        {
            OldOffset = oldOffset;
            NewOffset = newOffset;
        }
    }

    #endregion

    public partial class SearchDataGrid
    {
        #region Scroll Infrastructure Fields

        private ScrollViewer _scrollViewer;
        private bool _scrollInfrastructureReady;

        // Cascade timing uses a continuous wall-clock slot queue. Each reveal claims the
        // next available slot at max(now, _nextCascadeSlotMs), then advances the slot by
        // CascadeStagger. When rows arrive faster than stagger the queue extends into the
        // future and subsequent rows wait their turn. When rows trickle in slowly the wall
        // clock catches up (now ≥ _nextCascadeSlotMs) and each row starts immediately — no
        // explicit reset timer required.
        //
        // This replaces an older burst+timer model whose reset on scroll-stop-scroll could
        // start a fresh burst while the previous one still had rows waiting at the BeginTime
        // cap, producing a visible opacity inversion where new rows overtook stalled old ones.
        private readonly Stopwatch _cascadeClock = Stopwatch.StartNew();
        private double _nextCascadeSlotMs;

        // Monotonic index within the current "burst" (a contiguous run of reveals where the
        // queue hasn't drained). Reset to 0 when the queue drains so Custom animation
        // consumers get a sensible per-burst index. Advances once per revealed row.
        private int _cascadeIndex;
        private double _burstStartMs;

        // Effective stagger/duration snapshotted at the start of each wave (see
        // SnapshotWaveSettings). The user's CascadeStagger / RowOpacityAnimationDuration
        // are treated as the SLOW-end target; the actual values used for a wave scale
        // toward faster settings based on how many rows entered the viewport together.
        //   • Small batches (slow scrolling, few reveals)   → use the user's values verbatim.
        //   • Large batches (fast scrolling, many reveals)  → compress toward the fast ratios.
        // Once set at wave start, these stay constant for every row in the wave so the
        // cascade has internally consistent timing and no mid-wave jitter.
        private double _currentWaveStaggerMs;
        private double _currentWaveDurationMs;

        // Rows whose containers have been realized (often into the virtualization cache)
        // but have not yet entered the viewport. We hold them at Opacity=0 until a scroll
        // brings them into view, then animate the reveal. Without this, cache-prerealized
        // rows would burn their fade-in animation offscreen and appear already-opaque when
        // the user scrolls to them.
        private readonly HashSet<DataGridRow> _pendingVisibilityRows = new HashSet<DataGridRow>();
        private bool _pendingVisibilityProcessScheduled;

        // Last-known scroll direction, used to order the cascade so it flows with the user's
        // scroll motion: +1 = scrolling down (cascade ascends by index), -1 = scrolling up
        // (cascade descends by index), 0 = no scroll yet (default to ascending, which reads
        // top-to-bottom for initial load). Updated on every ScrollChanged with non-zero
        // VerticalChange. When scrolling up, rows enter the viewport in descending-index
        // order (row 99 first when moving from [100..124] to [97..121], then 98, then 97)
        // so cascading in descending order matches the order they appeared.
        private int _lastScrollDirection;

        #endregion

        #region Events

        public event EventHandler<RowAnimationBeginEventArgs> RowAnimationBegin;
        public event EventHandler<CustomScrollAnimationEventArgs> CustomScrollAnimation;

        #endregion

        #region Dependency Properties

        // --- Gridline Visibility ---

        /// <summary>
        /// Gets or sets whether horizontal gridlines are shown between rows.
        /// Horizontal gridlines are rendered outside the CellsPresenter so they
        /// stay fully opaque during row animations.
        /// </summary>
        public static readonly DependencyProperty ShowHorizontalGridLinesProperty =
            DependencyProperty.Register("ShowHorizontalGridLines", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether vertical gridlines are shown between columns.
        /// Vertical gridlines are part of the cell and animate with the row content
        /// when RowAnimationKind is Opacity — this matches commercial DataGrid behavior.
        /// </summary>
        public static readonly DependencyProperty ShowVerticalGridLinesProperty =
            DependencyProperty.Register("ShowVerticalGridLines", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(true));

        // --- Scroll Animation ---

        /// <summary>
        /// Switches scrolling from item-based (the default, one row per scroll step) to
        /// pixel-based (smooth sub-row positioning). Required for <see cref="AllowScrollAnimation"/>.
        /// <para>
        /// <b>Performance caveat — do not enable on large datasets.</b> Pixel-mode scrolling
        /// forces WPF's <c>VirtualizingStackPanel</c> to invalidate measure on every fractional
        /// pixel change in scroll offset, versus item mode where the panel short-circuits until
        /// the visible row set actually changes. Combined with per-frame <c>ScrollToVerticalOffset</c>
        /// calls from the smooth-scroll animation, this produces noticeably choppy scrolling
        /// starting around a few hundred thousand rows and gets worse from there. Item mode
        /// (the default) scales cleanly to millions of rows.
        /// </para>
        /// <para>
        /// Use this for grids up to ~100k rows where UX polish matters. For larger datasets,
        /// leave this off.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty AllowPerPixelScrollingProperty =
            DependencyProperty.Register("AllowPerPixelScrolling", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnAllowPerPixelScrollingChanged));

        /// <summary>
        /// Enables animated momentum/inertia scrolling for mouse wheel input. Each wheel tick
        /// contributes to a velocity that decays over time, producing smooth coasting.
        /// Automatically enables <see cref="AllowPerPixelScrolling"/> because momentum requires
        /// sub-row positioning; disabling per-pixel scrolling also disables this.
        /// <para>
        /// <b>Performance caveat — do not enable on large datasets.</b> This inherits the
        /// per-pixel-scrolling performance cliff (see <see cref="AllowPerPixelScrolling"/>) and
        /// compounds it by calling <c>ScrollToVerticalOffset</c> on every animation frame.
        /// Suitable for grids up to ~100k rows; leave off for larger datasets.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty AllowScrollAnimationProperty =
            DependencyProperty.Register("AllowScrollAnimation", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnAllowScrollAnimationChanged));

        public static readonly DependencyProperty ScrollAnimationModeProperty =
            DependencyProperty.Register("ScrollAnimationMode", typeof(ScrollAnimationMode), typeof(SearchDataGrid),
                new PropertyMetadata(ScrollAnimationMode.EaseOut, OnScrollAnimationSettingChanged));

        public static readonly DependencyProperty ScrollAnimationDurationProperty =
            DependencyProperty.Register("ScrollAnimationDuration", typeof(double), typeof(SearchDataGrid),
                new PropertyMetadata(800.0, OnScrollAnimationSettingChanged));

        // --- Row Animation ---

        /// <summary>
        /// Master switch for cascading data updates. When false, rows load synchronously
        /// regardless of <see cref="RowAnimationKind"/>.
        /// </summary>
        public static readonly DependencyProperty AllowCascadeUpdateProperty =
            DependencyProperty.Register("AllowCascadeUpdate", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false));

        public static readonly DependencyProperty RowAnimationKindProperty =
            DependencyProperty.Register("RowAnimationKind", typeof(RowAnimationKind), typeof(SearchDataGrid),
                new PropertyMetadata(RowAnimationKind.None));

        /// <summary>
        /// Duration in milliseconds of the row opacity animation.
        /// </summary>
        public static readonly DependencyProperty RowOpacityAnimationDurationProperty =
            DependencyProperty.Register("RowOpacityAnimationDuration", typeof(double), typeof(SearchDataGrid),
                new PropertyMetadata(200.0, OnRowAnimationSettingChanged));

        /// <summary>
        /// Easing curve for the row opacity animation. Controls how the fade-in accelerates.
        /// </summary>
        public static readonly DependencyProperty RowAnimationEasingProperty =
            DependencyProperty.Register("RowAnimationEasing", typeof(RowAnimationEasing), typeof(SearchDataGrid),
                new PropertyMetadata(RowAnimationEasing.EaseOut, OnRowAnimationSettingChanged));

        /// <summary>
        /// Delay in milliseconds between the start of each row's animation in a burst,
        /// creating a cascading wave effect. Set to 0 to disable stagger (all rows animate simultaneously).
        /// </summary>
        public static readonly DependencyProperty CascadeStaggerProperty =
            DependencyProperty.Register("CascadeStagger", typeof(double), typeof(SearchDataGrid),
                new PropertyMetadata(20.0));

        // --- Virtualization ---

        public static readonly DependencyProperty VirtualizationCacheLengthProperty =
            DependencyProperty.Register("VirtualizationCacheLength", typeof(double), typeof(SearchDataGrid),
                new PropertyMetadata(2.0, OnVirtualizationCacheLengthChanged));

        #endregion

        #region CLR Property Wrappers

        public bool ShowHorizontalGridLines
        {
            get => (bool)GetValue(ShowHorizontalGridLinesProperty);
            set => SetValue(ShowHorizontalGridLinesProperty, value);
        }

        public bool ShowVerticalGridLines
        {
            get => (bool)GetValue(ShowVerticalGridLinesProperty);
            set => SetValue(ShowVerticalGridLinesProperty, value);
        }

        /// <inheritdoc cref="AllowPerPixelScrollingProperty"/>
        public bool AllowPerPixelScrolling
        {
            get => (bool)GetValue(AllowPerPixelScrollingProperty);
            set => SetValue(AllowPerPixelScrollingProperty, value);
        }

        /// <inheritdoc cref="AllowScrollAnimationProperty"/>
        public bool AllowScrollAnimation
        {
            get => (bool)GetValue(AllowScrollAnimationProperty);
            set => SetValue(AllowScrollAnimationProperty, value);
        }

        public ScrollAnimationMode ScrollAnimationMode
        {
            get => (ScrollAnimationMode)GetValue(ScrollAnimationModeProperty);
            set => SetValue(ScrollAnimationModeProperty, value);
        }

        public double ScrollAnimationDuration
        {
            get => (double)GetValue(ScrollAnimationDurationProperty);
            set => SetValue(ScrollAnimationDurationProperty, value);
        }

        public bool AllowCascadeUpdate
        {
            get => (bool)GetValue(AllowCascadeUpdateProperty);
            set => SetValue(AllowCascadeUpdateProperty, value);
        }

        public RowAnimationKind RowAnimationKind
        {
            get => (RowAnimationKind)GetValue(RowAnimationKindProperty);
            set => SetValue(RowAnimationKindProperty, value);
        }

        public double RowOpacityAnimationDuration
        {
            get => (double)GetValue(RowOpacityAnimationDurationProperty);
            set => SetValue(RowOpacityAnimationDurationProperty, value);
        }

        public RowAnimationEasing RowAnimationEasing
        {
            get => (RowAnimationEasing)GetValue(RowAnimationEasingProperty);
            set => SetValue(RowAnimationEasingProperty, value);
        }

        public double CascadeStagger
        {
            get => (double)GetValue(CascadeStaggerProperty);
            set => SetValue(CascadeStaggerProperty, value);
        }

        public double VirtualizationCacheLength
        {
            get => (double)GetValue(VirtualizationCacheLengthProperty);
            set => SetValue(VirtualizationCacheLengthProperty, value);
        }

        #endregion

        #region Initialization

        private void InitializeScrollInfrastructure()
        {
            _scrollViewer = GetTemplateChild("DG_ScrollViewer") as ScrollViewer;
            if (_scrollViewer == null)
                return;

            _scrollInfrastructureReady = true;

            // ScrollChanged is how we discover pre-cached rows entering the viewport.
            // During smooth scrolling this fires every frame (ScrollToVerticalOffset
            // is called in the render loop), so pending rows get revealed promptly.
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;

            ApplyScrollMode();
            ApplyScrollAnimation();
            ApplyVirtualizationCacheLength();

            // If rows were realized before the scroll infrastructure was ready, drain
            // any that are now visible in the freshly wired-up viewport.
            if (_pendingVisibilityRows.Count > 0)
                ScheduleProcessPendingVisibility();
        }

        #endregion

        #region Row Animation

        /// <summary>
        /// Called from OnLoadingRow. Hides the CellsPresenter (Opacity=0) immediately so the
        /// row doesn't flash its contents, then queues the row for a viewport-entry check.
        /// The actual reveal animation is played by <see cref="ProcessPendingVisibleRows"/>
        /// once the row's transform confirms it intersects the viewport.
        ///
        /// Why the split: the virtualization cache pre-realizes rows that are near the viewport
        /// but not in it. If we animated on OnLoadingRow, cache rows would burn their fade-in
        /// offscreen and appear already-opaque when the user scrolls to them — producing the
        /// "first N rows don't animate" bug. Deferring the reveal to confirmed visibility ties
        /// the cascade to what the user actually sees.
        ///
        /// Gridlines (horizontal + vertical) are rendered outside the CellsPresenter so they stay visible.
        /// </summary>
        internal void HandleRowAnimationOnLoadingRow(DataGridRow row)
        {
            if (!AllowCascadeUpdate)
                return;

            if (RowAnimationKind == RowAnimationKind.None)
                return;

            var cellsPresenter = VisualTreeHelperMethods.FindVisualChild<DataGridCellsPresenter>(row);
            if (cellsPresenter == null)
                return;

            // Hide cell content with Opacity = 0 rather than Visibility.Collapsed.
            // Opacity preserves the row's natural height (no layout shift / jitter) and
            // WPF skips expensive render operations for fully transparent elements.
            cellsPresenter.BeginAnimation(OpacityProperty, null);
            cellsPresenter.Opacity = 0;

            // Queue the row for the visibility check. ScrollChanged also triggers processing,
            // but scheduling a deferred pass covers the initial-realization case where no
            // scroll event has fired yet.
            _pendingVisibilityRows.Add(row);
            ScheduleProcessPendingVisibility();
        }

        internal void HandleRowAnimationOnUnloadingRow(DataGridRow row)
        {
            if (!AllowCascadeUpdate || RowAnimationKind == RowAnimationKind.None)
                return;

            // A row may be unloading before it ever became visible (scrolled past in the cache).
            // Either way, drop it from the pending set so we don't try to animate a recycled container.
            _pendingVisibilityRows.Remove(row);

            // Reset for clean recycling — stop any running opacity animation and fully reveal
            // the CellsPresenter. The container will be reused for new data on the next OnLoadingRow.
            var cellsPresenter = VisualTreeHelperMethods.FindVisualChild<DataGridCellsPresenter>(row);
            if (cellsPresenter != null)
            {
                cellsPresenter.BeginAnimation(OpacityProperty, null);
                cellsPresenter.Opacity = 1;
            }
        }

        /// <summary>
        /// Schedules a single deferred pass over <see cref="_pendingVisibilityRows"/> at
        /// <see cref="DispatcherPriority.Loaded"/> — i.e. after the current layout pass
        /// completes, so row transforms are valid. The flag prevents multiple OnLoadingRow
        /// calls in the same tick from queuing N redundant dispatch operations.
        /// </summary>
        private void ScheduleProcessPendingVisibility()
        {
            if (_pendingVisibilityProcessScheduled)
                return;

            _pendingVisibilityProcessScheduled = true;
            Dispatcher.BeginInvoke(new Action(ProcessPendingVisibleRows), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Drains any pending rows whose transforms place them inside the ScrollViewer's
        /// viewport. Called from both the scheduled dispatcher callback (covers initial
        /// realization) and ScrollChanged (covers rows entering the viewport mid-scroll).
        /// Rows that remain outside the viewport stay pending — their reveal is driven by
        /// the next ScrollChanged that brings them in.
        /// </summary>
        private void ProcessPendingVisibleRows()
        {
            _pendingVisibilityProcessScheduled = false;

            if (_pendingVisibilityRows.Count == 0 || _scrollViewer == null)
                return;

            // Gather rows that the transform check says are visible right now. Sorting by
            // item index gives a visual top-to-bottom order, which is what the cascade
            // BeginTime stagger is designed around — without the sort, HashSet iteration
            // order could produce a disordered wave.
            List<DataGridRow> toAnimate = null;
            foreach (var row in _pendingVisibilityRows)
            {
                if (IsRowInViewport(row))
                {
                    (toAnimate ??= new List<DataGridRow>()).Add(row);
                }
            }

            if (toAnimate == null)
                return;

            // Sort in the direction that matches how rows entered the viewport. For a
            // downward scroll, rows enter from the bottom in index-ascending order — row 125
            // first, then 126, 127 — so ascending sort makes the cascade flow with the
            // scroll. For an upward scroll, rows enter at the top in index-descending order
            // (99 first as the viewport moves up by one, then 98, 97), so descending sort
            // keeps the cascade in entry order and the wave flows with the scroll.
            bool descending = _lastScrollDirection < 0;
            toAnimate.Sort((a, b) =>
            {
                int ai = ItemContainerGenerator.IndexFromContainer(a);
                int bi = ItemContainerGenerator.IndexFromContainer(b);
                return descending ? bi.CompareTo(ai) : ai.CompareTo(bi);
            });

            // Recompute effective cascade settings for this batch. Two decisions:
            //   • Duration is snapshotted only at wave start. Changing duration mid-wave would
            //     let later (shorter-duration) rows overtake earlier ones in opacity — a
            //     visible inversion. So a wave picks one duration and keeps it.
            //   • Stagger is updated every batch using the stronger of two signals: batch
            //     size (this reveal is big) or queue lookahead (the queue is already backed
            //     up, so the user is scrolling faster than the cascade can emit). Either
            //     signal crossing its threshold compresses stagger toward the fast end.
            //     Stagger can change mid-wave safely because the slot queue already handles
            //     row-to-row ordering regardless of gap size.
            double nowMs = _cascadeClock.Elapsed.TotalMilliseconds;
            bool isNewWave = nowMs >= _nextCascadeSlotMs;
            UpdateEffectiveCascadeSettings(toAnimate.Count, isNewWave, nowMs);

            foreach (var row in toAnimate)
            {
                _pendingVisibilityRows.Remove(row);

                var cellsPresenter = VisualTreeHelperMethods.FindVisualChild<DataGridCellsPresenter>(row);
                if (cellsPresenter == null)
                    continue;

                StartRowRevealAnimation(row, cellsPresenter);
            }
        }

        // Batch size at which cascade timing reaches full compression based on batch size
        // alone. Between 1 and this threshold, interpolation is linear; at or above, the
        // batch-size signal is at full compression.
        private const int FastCascadeBatchThreshold = 15;

        // Stagger compression at the fast end. 0.2 means a user's 100ms stagger becomes
        // 20ms during fast scrolls — cascade is still perceptible but doesn't bottleneck
        // a fast user.
        private const double FastCascadeStaggerRatio = 0.2;

        // Duration compression at the fast end. Less aggressive than stagger because each
        // row's fade still needs enough time to read as a deliberate reveal rather than a
        // flicker — 0.5 keeps the individual animation legible while halving the wait.
        private const double FastCascadeDurationRatio = 0.5;

        // Queue lookahead at which the queue-backup signal reaches full compression.
        // Lookahead = (_nextCascadeSlotMs - now) — the wall-clock time the current queue
        // will take to drain. When this reaches the threshold, the user is scrolling faster
        // than the cascade can emit, and continuing with slow stagger would stack rows in
        // a growing blank queue. 400ms was chosen so users with default settings (20ms
        // stagger, 200ms duration) see compression kick in at ~20 rows deep, while users
        // with slow settings (100ms stagger, 1000ms duration) see it trigger at ~4 rows
        // deep — appropriate since their cascade is inherently slower per-row.
        private const double MaxPreferredQueueLookaheadMs = 400.0;

        /// <summary>
        /// Computes the effective cascade settings for the incoming batch. Duration is
        /// snapshotted only at wave start; stagger is re-evaluated every batch using the
        /// stronger of two signals:
        ///   • <b>Batch size</b> — large single reveals compress (fast scroll jumps, cache
        ///     flushes, initial load).
        ///   • <b>Queue lookahead</b> — when the slot queue has already backed up, further
        ///     additions compress even if they arrive in small chunks, because the user
        ///     is clearly outpacing the current wave's stagger.
        /// Using <c>max</c> of the two t-values means either condition can trigger
        /// compression independently, and small trickle scrolls after a queue-backup keep
        /// the fast settings instead of relaxing back to slow mid-wave.
        /// </summary>
        private void UpdateEffectiveCascadeSettings(int batchSize, bool isNewWave, double nowMs)
        {
            double slowStaggerMs = Math.Max(0, CascadeStagger);
            double slowDurationMs = RowOpacityAnimationDuration > 0 ? RowOpacityAnimationDuration : 200;

            double denom = FastCascadeBatchThreshold - 1;
            double tBatch = denom > 0
                ? Math.Clamp((batchSize - 1.0) / denom, 0.0, 1.0)
                : 1.0;

            // Queue lookahead = time until the current queue head drains. 0 when the queue
            // has caught up to wall clock (isNewWave case).
            double lookaheadMs = Math.Max(0, _nextCascadeSlotMs - nowMs);
            double tQueue = Math.Clamp(lookaheadMs / MaxPreferredQueueLookaheadMs, 0.0, 1.0);

            double t = Math.Max(tBatch, tQueue);

            double fastStaggerMs = slowStaggerMs * FastCascadeStaggerRatio;
            _currentWaveStaggerMs = slowStaggerMs + t * (fastStaggerMs - slowStaggerMs);

            if (isNewWave)
            {
                // Duration uses the batch signal only (not queue lookahead — the queue is
                // by definition empty at wave start, so lookahead would always be 0).
                double fastDurationMs = slowDurationMs * FastCascadeDurationRatio;
                _currentWaveDurationMs = slowDurationMs + tBatch * (fastDurationMs - slowDurationMs);
            }
        }

        /// <summary>
        /// Kicks off the row's reveal animation, placing it at the next slot in the
        /// continuous cascade queue. The slot logic in <see cref="ReserveNextCascadeSlot"/>
        /// guarantees that new reveals never start before previously-queued reveals — no
        /// matter how long the previous burst's stagger, rows that were already queued
        /// keep their scheduled start times.
        /// </summary>
        private void StartRowRevealAnimation(DataGridRow row, DataGridCellsPresenter cellsPresenter)
        {
            var kind = RowAnimationKind;
            if (kind == RowAnimationKind.None)
                return;

            var (beginTime, cascadeIndexForThisRow, burstElapsed) = ReserveNextCascadeSlot();

            switch (kind)
            {
                case RowAnimationKind.Opacity:
                    cellsPresenter.BeginAnimation(OpacityProperty, BuildOpacityAnimation(beginTime));
                    break;

                case RowAnimationKind.Custom:
                    RowAnimationBegin?.Invoke(this,
                        new RowAnimationBeginEventArgs(
                            row,
                            cellsPresenter,
                            cascadeIndexForThisRow,
                            burstElapsed,
                            beginTime,
                            TimeSpan.FromMilliseconds(_currentWaveStaggerMs),
                            TimeSpan.FromMilliseconds(_currentWaveDurationMs)));
                    break;
            }
        }

        /// <summary>
        /// Claims the next cascade slot and returns the computed BeginTime + burst context.
        /// Uses the wave-snapshot stagger (<see cref="_currentWaveStaggerMs"/>), not the
        /// user's raw CascadeStagger, so a wave whose batch size triggered compression
        /// stays compressed for every row. Slot math:
        ///   slot = max(now, _nextCascadeSlotMs)
        ///   BeginTime = slot - now
        ///   _nextCascadeSlotMs += effectiveStagger
        /// A cap on BeginTime (scaled with the effective stagger) prevents pathologically
        /// long queues from leaving rows blank for many seconds. Burst index/elapsed are
        /// refreshed when the queue fully drains so Custom consumers see a 0 index per wave.
        /// </summary>
        private (TimeSpan beginTime, int cascadeIndex, TimeSpan burstElapsed) ReserveNextCascadeSlot()
        {
            double nowMs = _cascadeClock.Elapsed.TotalMilliseconds;
            double staggerMs = _currentWaveStaggerMs;

            // When there is no stagger, every row starts immediately and we treat each
            // row as its own trivial "burst" — keeps Custom consumers sane.
            if (staggerMs <= 0 || RowAnimationEasing == RowAnimationEasing.None)
            {
                _cascadeIndex = 0;
                _burstStartMs = nowMs;
                _nextCascadeSlotMs = nowMs;
                return (TimeSpan.Zero, 0, TimeSpan.Zero);
            }

            // Queue has drained — wall clock caught up to the head of the queue, so this
            // is the start of a fresh burst. Reset per-burst counters so Custom handlers
            // see a coherent 0-based index for each wave.
            if (nowMs >= _nextCascadeSlotMs)
            {
                _cascadeIndex = 0;
                _burstStartMs = nowMs;
            }

            double slotMs = Math.Max(nowMs, _nextCascadeSlotMs);
            double beginMs = slotMs - nowMs;

            // Dynamic cap: scale with the wave's effective stagger so a long slow-scroll
            // wave still covers a full viewport before batching kicks in, while a fast
            // compressed wave doesn't waste its budget on queue lookahead it won't use.
            double maxBeginMs = Math.Max(MinCascadeCapMs, staggerMs * MaxCascadeCapSlots);
            if (beginMs > maxBeginMs)
                beginMs = maxBeginMs;

            _nextCascadeSlotMs = slotMs + staggerMs;

            int indexForThisRow = _cascadeIndex;
            _cascadeIndex++;

            return (
                beginTime: TimeSpan.FromMilliseconds(beginMs),
                cascadeIndex: indexForThisRow,
                burstElapsed: TimeSpan.FromMilliseconds(nowMs - _burstStartMs));
        }

        // Last-known row height, used to map pixel offsets back to item indices in
        // AllowPerPixelScrolling mode. Updated opportunistically whenever IsRowInViewport
        // sees an arranged row with a real ActualHeight. DataGrid rows are uniform height
        // by default, so a single sample is authoritative.
        private double _cachedRowHeight;

        /// <summary>
        /// Returns true if the row is currently in the visible viewport. Uses pure item-index
        /// arithmetic in both scroll modes to avoid TransformToAncestor, which walks the visual
        /// tree and invalidates transform caches — prohibitively expensive when called once per
        /// pending row per frame of a smooth-scroll animation.
        ///
        /// ScrollViewer reports VerticalOffset/ViewportHeight in different units depending on
        /// mode: item indices in item mode, pixels in pixel mode. We translate the pixel-mode
        /// values into item-index space using the known (uniform) row height, so the final
        /// comparison is identical in both paths.
        /// </summary>
        private bool IsRowInViewport(DataGridRow row)
        {
            if (row == null || _scrollViewer == null)
                return false;

            int rowIndex = ItemContainerGenerator.IndexFromContainer(row);
            if (rowIndex < 0)
                return false;

            double firstVisibleIndex;
            double lastVisibleIndexExclusive;

            if (AllowPerPixelScrolling)
            {
                if (row.ActualHeight > 0)
                    _cachedRowHeight = row.ActualHeight;

                double rowHeight = _cachedRowHeight;
                if (rowHeight <= 0)
                    return false;

                firstVisibleIndex = _scrollViewer.VerticalOffset / rowHeight;
                lastVisibleIndexExclusive = (_scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight) / rowHeight;
            }
            else
            {
                // Item mode: offsets are already item indices.
                firstVisibleIndex = _scrollViewer.VerticalOffset;
                lastVisibleIndexExclusive = _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight;
            }

            int firstIdx = (int)Math.Floor(firstVisibleIndex);
            int lastIdxExc = (int)Math.Ceiling(lastVisibleIndexExclusive);
            return rowIndex >= firstIdx && rowIndex < lastIdxExc;
        }

        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Track vertical scroll direction so the cascade flows with the user's motion.
            // We only update on non-zero vertical movement — a horizontal-only or viewport-
            // resize event shouldn't rewrite the direction we want to use for the cascade.
            if (e.VerticalChange > 0) _lastScrollDirection = 1;
            else if (e.VerticalChange < 0) _lastScrollDirection = -1;

            // Fast early-out: scroll events also fire for horizontal-only changes, which
            // can't reveal new vertical rows. Skip those.
            if (_pendingVisibilityRows.Count == 0)
                return;

            if (e.VerticalChange == 0 && e.ViewportHeightChange == 0 && e.ExtentHeightChange == 0)
                return;

            ProcessPendingVisibleRows();
        }

        // Floor on the dynamic BeginTime cap. Even with very small staggers, we never cap
        // below this — protects against aggressive user-settings-driven batching.
        private const double MinCascadeCapMs = 1000.0;

        // How many "slots" of cascade we're willing to have queued ahead. The dynamic cap
        // is staggerMs * this constant, so a user-chosen stagger preserves a ~30-row
        // cascade before batching. Typical viewports are smaller than this.
        private const int MaxCascadeCapSlots = 30;

        /// <summary>
        /// Builds the per-row fade-in animation with the supplied BeginTime (already
        /// computed by <see cref="ReserveNextCascadeSlot"/>). Reads the effective
        /// duration from the current wave snapshot rather than the DP directly, so
        /// all rows in one wave share a single consistent duration even if the user
        /// moves the slider mid-scroll.
        /// </summary>
        private DoubleAnimation BuildOpacityAnimation(TimeSpan beginTime)
        {
            var easing = RowAnimationEasing;

            // None = instant (ReserveNextCascadeSlot already short-circuited the stagger)
            if (easing == RowAnimationEasing.None)
            {
                var instant = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(1));
                instant.Freeze();
                return instant;
            }

            double durationMs = _currentWaveDurationMs > 0 ? _currentWaveDurationMs : 200;
            var duration = TimeSpan.FromMilliseconds(durationMs);

            var animation = new DoubleAnimation(0, 1, new Duration(duration));

            if (beginTime > TimeSpan.Zero)
                animation.BeginTime = beginTime;

            // Linear = no easing function (constant rate)
            if (easing != RowAnimationEasing.Linear)
            {
                var easingMode = easing switch
                {
                    RowAnimationEasing.EaseOut => EasingMode.EaseOut,
                    RowAnimationEasing.EaseIn => EasingMode.EaseIn,
                    RowAnimationEasing.EaseInOut => EasingMode.EaseInOut,
                    _ => EasingMode.EaseOut
                };
                animation.EasingFunction = new QuadraticEase { EasingMode = easingMode };
            }

            animation.Freeze();
            return animation;
        }

        #endregion

        #region Custom Scroll Animation Support

        internal Storyboard RaiseCustomScrollAnimation(double oldOffset, double newOffset)
        {
            var args = new CustomScrollAnimationEventArgs(oldOffset, newOffset);
            CustomScrollAnimation?.Invoke(this, args);
            return args.Storyboard;
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnAllowPerPixelScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                if (!(bool)e.NewValue && grid.AllowScrollAnimation)
                    grid.AllowScrollAnimation = false;

                if (grid._scrollInfrastructureReady)
                {
                    grid.ApplyScrollMode();
                    grid.ApplyScrollAnimation();
                }
            }
        }

        private static void OnAllowScrollAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid)
            {
                if ((bool)e.NewValue && !grid.AllowPerPixelScrolling)
                    grid.AllowPerPixelScrolling = true;

                if (grid._scrollInfrastructureReady)
                    grid.ApplyScrollAnimation();
            }
        }

        private static void OnScrollAnimationSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid && grid._scrollInfrastructureReady)
                grid.ApplyScrollAnimation();
        }

        private static void OnRowAnimationSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Animations are created per-row with staggered BeginTime — no cache to invalidate
        }

        private static void OnVirtualizationCacheLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchDataGrid grid && grid._scrollInfrastructureReady)
                grid.ApplyVirtualizationCacheLength();
        }

        #endregion

        #region Apply Methods

        private void ApplyScrollMode()
        {
            var unit = AllowPerPixelScrolling
                ? System.Windows.Controls.ScrollUnit.Pixel
                : System.Windows.Controls.ScrollUnit.Item;
            SetValue(VirtualizingPanel.ScrollUnitProperty, unit);
        }

        private void ApplyScrollAnimation()
        {
            if (_scrollViewer == null) return;

            bool shouldEnable = AllowPerPixelScrolling && AllowScrollAnimation;
            SmoothScrollBehavior.SetEnableSmoothScroll(_scrollViewer, shouldEnable);

            if (shouldEnable)
            {
                double durationSec = ScrollAnimationDuration / 1000.0;
                durationSec = Math.Clamp(durationSec, 0.1, 5.0);
                double friction = Math.Pow(0.01, 1.0 / (durationSec * 60.0));

                SmoothScrollBehavior.SetFriction(_scrollViewer, friction);
                SmoothScrollBehavior.SetAnimationMode(_scrollViewer, ScrollAnimationMode);
                SmoothScrollBehavior.SetOwnerGrid(_scrollViewer, this);
            }
        }

        private void ApplyVirtualizationCacheLength()
        {
            var length = VirtualizationCacheLength;
            SetValue(VirtualizingPanel.CacheLengthProperty,
                new VirtualizationCacheLength(length, length));
        }

        #endregion
    }
}
