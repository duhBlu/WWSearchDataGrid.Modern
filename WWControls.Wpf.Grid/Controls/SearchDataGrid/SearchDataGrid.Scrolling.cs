using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WWControls.Wpf.Grids
{
    #region Event Args

    public class RowAnimationBeginEventArgs : EventArgs
    {
        public DataGridRow Row { get; }
        public DataGridCellsPresenter CellsPresenter { get; }

        /// <summary>
        /// Zero-based index of this row within the current cascade burst. Resets when the
        /// queue drains. Useful for staggered BeginTime in Custom animations.
        /// </summary>
        public int CascadeIndex { get; }

        /// <summary>Wall-clock time elapsed since the current burst started.</summary>
        public TimeSpan BurstElapsed { get; }

        /// <summary>
        /// BeginTime the built-in Opacity animation would use — already includes slot queue
        /// position and dynamic stagger compression. Apply to Custom animations to match.
        /// </summary>
        public TimeSpan BeginTime { get; }

        /// <summary>
        /// Effective per-row stagger for this wave. Differs from <c>CascadeStagger</c> DP
        /// when dynamic compression engaged. Use this for Custom row-to-row timing.
        /// </summary>
        public TimeSpan EffectiveStagger { get; }

        /// <summary>
        /// Effective animation duration for this wave. Differs from
        /// <c>RowOpacityAnimationDuration</c> DP when dynamic compression engaged.
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
        // CascadeStagger. Fast input extends the queue into the future; slow input lets the
        // clock catch up so each row starts immediately — no explicit reset timer required.
        private readonly Stopwatch _cascadeClock = Stopwatch.StartNew();
        private double _nextCascadeSlotMs;

        // Monotonic index within the current "burst" (a contiguous run of reveals where the
        // queue hasn't drained). Reset to 0 when the queue drains so Custom animation
        // consumers get a sensible per-burst index. Advances once per revealed row.
        private int _cascadeIndex;
        private double _burstStartMs;

        // Effective stagger/duration snapshotted per wave. The user's DP values are the
        // SLOW-end target; large batches compress toward the fast ratios. Held constant
        // for the wave so timing stays internally consistent (no mid-wave jitter).
        private double _currentWaveStaggerMs;
        private double _currentWaveDurationMs;

        // Containers realized into the virtualization cache but not yet in the viewport.
        // Held at Opacity=0 until a scroll brings them in — otherwise cache rows would
        // burn their fade offscreen and look already-opaque on arrival.
        private readonly HashSet<DataGridRow> _pendingVisibilityRows = new HashSet<DataGridRow>();
        private bool _pendingVisibilityProcessScheduled;

        // Last-known scroll direction (+1 down, -1 up, 0 initial). The cascade sorts in the
        // direction rows actually enter the viewport so the wave flows with scroll motion.
        private int _lastScrollDirection;

        #endregion

        #region Events

        public event EventHandler<RowAnimationBeginEventArgs> RowAnimationBegin;
        public event EventHandler<CustomScrollAnimationEventArgs> CustomScrollAnimation;

        #endregion

        #region Dependency Properties

        // --- Gridline Visibility ---

        /// <summary>
        /// Horizontal gridlines between rows. Rendered outside the CellsPresenter so they
        /// stay opaque during row animations.
        /// </summary>
        public static readonly DependencyProperty ShowHorizontalGridLinesProperty =
            DependencyProperty.Register("ShowHorizontalGridLines", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(true));

        /// <summary>
        /// Vertical gridlines between columns. Part of the cell — animates with row content
        /// when RowAnimationKind is Opacity.
        /// </summary>
        public static readonly DependencyProperty ShowVerticalGridLinesProperty =
            DependencyProperty.Register("ShowVerticalGridLines", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(true));

        // --- Scroll Animation ---

        /// <summary>
        /// Switches scrolling from item-based to pixel-based (sub-row positioning). Required
        /// for <see cref="AllowScrollAnimation"/>.
        /// <para>
        /// <b>Performance caveat — do not enable on large datasets.</b> Pixel-mode forces
        /// <c>VirtualizingStackPanel</c> to invalidate measure on every fractional offset
        /// change; item mode short-circuits until the visible set actually changes. Suitable
        /// for grids up to ~100k rows; item mode scales cleanly to millions.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty AllowPerPixelScrollingProperty =
            DependencyProperty.Register("AllowPerPixelScrolling", typeof(bool), typeof(SearchDataGrid),
                new PropertyMetadata(false, OnAllowPerPixelScrollingChanged));

        /// <summary>
        /// Momentum/inertia scrolling for mouse wheel input. Forces
        /// <see cref="AllowPerPixelScrolling"/> on (and disabling per-pixel disables this).
        /// <para>
        /// <b>Performance caveat — do not enable on large datasets.</b> Inherits the
        /// per-pixel cliff and compounds it with per-frame <c>ScrollToVerticalOffset</c>.
        /// Suitable up to ~100k rows.
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

            // Template (re)applied — cached references into the old scroll-viewer template are stale.
            // Drop them so they re-resolve against the new template on the next resolver pass.
            _scrollContentPresenter = null;
            _fixedGroupHeadersPresenter = null;
            _fixedGroupShadow = null;

            // ScrollChanged drives pending-row reveals AND the sticky-group strip's chain resolve;
            // fires every frame during smooth scroll, idle otherwise.
            _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;

            ApplyScrollMode();
            ApplyScrollAnimation();
            ApplyVirtualizationCacheLength();

            // Populate the sticky strip once against the freshly-applied template (covers a
            // template reapply mid-scroll); bails cheaply when the feature is off or ungrouped.
            UpdateFixedGroupHeaders();

            // Drain any rows realized before the scroll infrastructure was wired up.
            if (_pendingVisibilityRows.Count > 0)
                ScheduleProcessPendingVisibility();
        }

        #endregion

        #region Row Animation

        /// <summary>
        /// OnLoadingRow hook. Hides the CellsPresenter immediately and queues the row for a
        /// viewport-entry check — the reveal is played by <see cref="ProcessPendingVisibleRows"/>
        /// once the transform confirms visibility. Animating on OnLoadingRow would burn the
        /// fade-in offscreen for virtualization-cache rows. Gridlines render outside the
        /// CellsPresenter so they stay visible during the hide.
        /// </summary>
        internal void HandleRowAnimationOnLoadingRow(DataGridRow row)
        {
            if (!AllowCascadeUpdate)
                return;

            if (RowAnimationKind == RowAnimationKind.None)
                return;

            var cellsPresenter = VisualTreeHelperMethods.FindVisualDescendant<DataGridCellsPresenter>(row);
            if (cellsPresenter == null)
                return;

            // Opacity=0, not Visibility.Collapsed — preserves row height (no layout shift)
            // and WPF skips render for fully transparent elements.
            cellsPresenter.BeginAnimation(OpacityProperty, null);
            cellsPresenter.Opacity = 0;

            // ScrollChanged triggers processing; the deferred pass covers initial realization
            // before any scroll event has fired.
            _pendingVisibilityRows.Add(row);
            ScheduleProcessPendingVisibility();
        }

        internal void HandleRowAnimationOnUnloadingRow(DataGridRow row)
        {
            if (!AllowCascadeUpdate || RowAnimationKind == RowAnimationKind.None)
                return;

            // Drop from pending — the row may have been recycled before ever becoming visible.
            _pendingVisibilityRows.Remove(row);

            // Reset opacity so the container is clean for the next OnLoadingRow.
            var cellsPresenter = VisualTreeHelperMethods.FindVisualDescendant<DataGridCellsPresenter>(row);
            if (cellsPresenter != null)
            {
                cellsPresenter.BeginAnimation(OpacityProperty, null);
                cellsPresenter.Opacity = 1;
            }
        }

        /// <summary>
        /// Schedules a single deferred pass at <see cref="DispatcherPriority.Loaded"/> so
        /// row transforms are valid. The flag coalesces N same-tick OnLoadingRow calls.
        /// </summary>
        private void ScheduleProcessPendingVisibility()
        {
            if (_pendingVisibilityProcessScheduled)
                return;

            _pendingVisibilityProcessScheduled = true;
            Dispatcher.BeginInvoke(new Action(ProcessPendingVisibleRows), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Reveals pending rows that have entered the viewport. Called from the scheduled
        /// callback (initial realization) and from ScrollChanged (mid-scroll). Rows still
        /// outside stay pending until the next ScrollChanged.
        /// </summary>
        private void ProcessPendingVisibleRows()
        {
            _pendingVisibilityProcessScheduled = false;

            if (_pendingVisibilityRows.Count == 0 || _scrollViewer == null)
                return;

            // Sort by item index — HashSet iteration order would otherwise produce a
            // disordered wave against the cascade's top-to-bottom stagger design.
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

            // Sort matches how rows enter the viewport: down-scroll → ascending index,
            // up-scroll → descending. Cascade then flows in the direction of motion.
            bool descending = _lastScrollDirection < 0;
            toAnimate.Sort((a, b) =>
            {
                int ai = ItemContainerGenerator.IndexFromContainer(a);
                int bi = ItemContainerGenerator.IndexFromContainer(b);
                return descending ? bi.CompareTo(ai) : ai.CompareTo(bi);
            });

            // Duration is snapshotted at wave start (mid-wave changes would let later rows
            // overtake earlier ones in opacity). Stagger updates per batch using max of batch
            // size or queue lookahead — safe mid-wave because the slot queue handles ordering.
            double nowMs = _cascadeClock.Elapsed.TotalMilliseconds;
            bool isNewWave = nowMs >= _nextCascadeSlotMs;
            UpdateEffectiveCascadeSettings(toAnimate.Count, isNewWave, nowMs);

            foreach (var row in toAnimate)
            {
                _pendingVisibilityRows.Remove(row);

                var cellsPresenter = VisualTreeHelperMethods.FindVisualDescendant<DataGridCellsPresenter>(row);
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

        // Queue lookahead threshold for full compression. When (_nextCascadeSlotMs - now)
        // reaches this, the user is outpacing the cascade and stagger needs to compress
        // before the blank queue grows further.
        private const double MaxPreferredQueueLookaheadMs = 400.0;

        /// <summary>
        /// Computes effective stagger/duration for the batch. Duration snapshots at wave
        /// start; stagger uses max(batchSize, queueLookahead) so either signal can trigger
        /// compression, and a backed-up queue stays compressed even after trickle reveals.
        /// </summary>
        private void UpdateEffectiveCascadeSettings(int batchSize, bool isNewWave, double nowMs)
        {
            double slowStaggerMs = Math.Max(0, CascadeStagger);
            double slowDurationMs = RowOpacityAnimationDuration > 0 ? RowOpacityAnimationDuration : 200;

            double denom = FastCascadeBatchThreshold - 1;
            double tBatch = denom > 0
                ? Math.Clamp((batchSize - 1.0) / denom, 0.0, 1.0)
                : 1.0;

            // Lookahead = time until the queue head drains; 0 when caught up to wall clock.
            double lookaheadMs = Math.Max(0, _nextCascadeSlotMs - nowMs);
            double tQueue = Math.Clamp(lookaheadMs / MaxPreferredQueueLookaheadMs, 0.0, 1.0);

            double t = Math.Max(tBatch, tQueue);

            double fastStaggerMs = slowStaggerMs * FastCascadeStaggerRatio;
            _currentWaveStaggerMs = slowStaggerMs + t * (fastStaggerMs - slowStaggerMs);

            if (isNewWave)
            {
                // Duration uses batchSize only — at wave start the queue is empty, so
                // lookahead would always be 0.
                double fastDurationMs = slowDurationMs * FastCascadeDurationRatio;
                _currentWaveDurationMs = slowDurationMs + tBatch * (fastDurationMs - slowDurationMs);
            }
        }

        /// <summary>
        /// Starts the row's reveal at the next cascade slot. Slot ordering guarantees new
        /// reveals never start before previously-queued ones.
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
        /// Claims the next cascade slot using the wave-snapshot stagger. Slot math:
        ///   slot = max(now, _nextCascadeSlotMs); BeginTime = slot - now; _nextCascadeSlotMs += stagger.
        /// BeginTime is capped (scaled with stagger) so deep queues don't leave rows blank
        /// for seconds. Burst counters reset when the queue drains.
        /// </summary>
        private (TimeSpan beginTime, int cascadeIndex, TimeSpan burstElapsed) ReserveNextCascadeSlot()
        {
            double nowMs = _cascadeClock.Elapsed.TotalMilliseconds;
            double staggerMs = _currentWaveStaggerMs;

            // No stagger → every row is its own trivial burst; keeps Custom consumers sane.
            if (staggerMs <= 0 || RowAnimationEasing == RowAnimationEasing.None)
            {
                _cascadeIndex = 0;
                _burstStartMs = nowMs;
                _nextCascadeSlotMs = nowMs;
                return (TimeSpan.Zero, 0, TimeSpan.Zero);
            }

            // Queue drained — reset burst counters so Custom handlers see a 0-based index per wave.
            if (nowMs >= _nextCascadeSlotMs)
            {
                _cascadeIndex = 0;
                _burstStartMs = nowMs;
            }

            double slotMs = Math.Max(nowMs, _nextCascadeSlotMs);
            double beginMs = slotMs - nowMs;

            // Cap scales with effective stagger so slow waves still cover a viewport while
            // fast waves don't waste budget on lookahead they won't use.
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

        // Cached row height for pixel→item-index translation. DataGrid rows are uniform
        // height, so a single sample is authoritative.
        private double _cachedRowHeight;

        /// <summary>
        /// True if the row is in the visible viewport. Uses item-index arithmetic in both
        /// scroll modes to avoid TransformToAncestor — prohibitively expensive when called
        /// per-row per-frame during smooth scroll. Pixel-mode offsets are translated to
        /// item-index space using the uniform row height so both paths share one comparison.
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
            // Update direction only on actual vertical movement — horizontal/resize events
            // shouldn't rewrite cascade direction.
            if (e.VerticalChange > 0) _lastScrollDirection = 1;
            else if (e.VerticalChange < 0) _lastScrollDirection = -1;

            // A viewport-width change (splitter/window resize, vertical scrollbar appearing) may
            // flip the fill-viewport layout between star-fill and pixel-overflow. Re-evaluating
            // changes column widths → extent change only, not viewport width, so this can't loop.
            if (e.ViewportWidthChange != 0 && _fillViewportLayoutActive)
                ApplyFillViewportLayout();

            bool verticalOrExtentChanged = e.VerticalChange != 0
                || e.ViewportHeightChange != 0
                || e.ExtentHeightChange != 0;

            // Re-resolve the sticky-group strip's active chain — and, in pixel-scroll mode, its push
            // transform — on vertical/extent/viewport change. ScrollChanged fires per frame during
            // smooth (pixel) scrolling, so the push reads as continuous; while idle there's no
            // always-on render loop. Cheap gates first so a non-grouped grid never walks the visual
            // tree here.
            if (verticalOrExtentChanged && AllowFixedGroups && GroupCount > 0)
                UpdateFixedGroupHeaders();

            if (_pendingVisibilityRows.Count == 0)
                return;

            if (!verticalOrExtentChanged)
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
        /// Builds the per-row fade-in. Duration comes from the wave snapshot, not the DP,
        /// so all rows in one wave share a single duration even if the slider moves mid-scroll.
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
