using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using WWControls.Core;
using WWControls.Core.Display;
using WWControls.Wpf.Display;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Best-fit (auto-width) engine. Computes a column's ideal pixel width without the
    /// Width-flipping / <c>UpdateLayout</c> passes the legacy context-menu implementation used:
    /// realized cells and headers are measured directly with an infinite constraint (then
    /// re-invalidated so the next layout pass restores real constraints), and
    /// <see cref="BestFitMode.AllRows"/> extends past the viewport by formatting every filtered
    /// leaf row's display text through the same pipeline the cells render with
    /// (<see cref="DisplayValueProviderFactory"/>) and measuring it as
    /// <see cref="FormattedText"/>, calibrated against realized cell chrome.
    /// </summary>
    public partial class SearchDataGrid
    {
        #region Best Fit

        /// <summary>
        /// Grid-wide default for whether best-fit UI gestures (context-menu items, gripper
        /// double-click) are available. Columns inherit via
        /// <see cref="ColumnLayoutBase.ActualAllowBestFit"/> and can override with
        /// <see cref="ColumnLayoutBase.AllowBestFit"/>. Does not gate the explicit
        /// <see cref="BestFitColumn(GridColumn)"/> API.
        /// </summary>
        public static readonly DependencyProperty AllowBestFitProperty =
            DependencyProperty.Register(nameof(AllowBestFit), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(true, OnAllowBestFitChanged));

        public bool AllowBestFit
        {
            get => (bool)GetValue(AllowBestFitProperty);
            set => SetValue(AllowBestFitProperty, value);
        }

        private static void OnAllowBestFitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SearchDataGrid grid)
                return;

            // Re-resolve every column's ActualAllowBestFit. Inheriting columns (no local
            // override) pick up the new grid value; explicitly-set columns keep their override.
            foreach (var column in grid.GridColumns)
                column?.RefreshActualAllowBestFit();
        }

        /// <summary>
        /// Grid-wide default for how many rows a best-fit pass measures. Columns inherit when
        /// their <see cref="ColumnLayoutBase.BestFitMode"/> is
        /// <see cref="Wpf.BestFitMode.Default"/>; a grid-level <c>Default</c> resolves to
        /// <see cref="Wpf.BestFitMode.AllRows"/> (accurate-by-default — explicit best-fit
        /// actions should not depend on scroll position).
        /// </summary>
        public static readonly DependencyProperty BestFitModeProperty =
            DependencyProperty.Register(nameof(BestFitMode), typeof(BestFitMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(BestFitMode.AllRows));

        public BestFitMode BestFitMode
        {
            get => (BestFitMode)GetValue(BestFitModeProperty);
            set => SetValue(BestFitModeProperty, value);
        }

        /// <summary>
        /// Auto best-fit on data load: when not <see cref="Wpf.BestFitMode.Default"/> (= off,
        /// the default), every real <c>ItemsSource</c> change schedules a best-fit pass over all
        /// columns at dispatcher idle, measured with this mode (columns with an explicit
        /// <see cref="ColumnLayoutBase.BestFitMode"/> keep their own). Internal source swaps
        /// (the grouping projection) and <c>Items.Refresh()</c> do not re-fire it. Columns
        /// opted out via <see cref="ColumnLayoutBase.AllowBestFit"/> are skipped.
        /// </summary>
        public static readonly DependencyProperty BestFitModeOnSourceChangeProperty =
            DependencyProperty.Register(nameof(BestFitModeOnSourceChange), typeof(BestFitMode), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(BestFitMode.Default));

        public BestFitMode BestFitModeOnSourceChange
        {
            get => (BestFitMode)GetValue(BestFitModeOnSourceChangeProperty);
            set => SetValue(BestFitModeOnSourceChangeProperty, value);
        }

        /// <summary>
        /// When <c>true</c>, a best-fit-<em>all</em> pass — the <see cref="BestFitAllColumns()"/>
        /// API, the "Best Fit All Columns" menu, and an auto best-fit on source change — sizes each
        /// column to its content and then makes the participating columns fill the full horizontal
        /// viewport instead of leaving empty space on the right. The layout is regime-aware:
        /// <list type="bullet">
        /// <item>When the combined content width fits the viewport, the columns are star-sized
        /// weighted by content width so they stretch to fill it; the slack keeps them resizable
        /// (dragging one redistributes space among the rest).</item>
        /// <item>When the combined content width exceeds the viewport, the columns are frozen at
        /// their content pixel width, so the grid overflows to a horizontal scrollbar with every
        /// column showing its full content and resizable as a normal pixel column — rather than
        /// shrinking columns below their content (which star sizing alone would do) or jamming
        /// them rigidly against the viewport edge.</item>
        /// </list>
        /// The regime is re-evaluated live as the viewport width changes (see
        /// <see cref="ApplyFillViewportLayout"/>), so the fill stays correct across grid resizes.
        /// Single-column best-fit (<see cref="BestFitColumn(GridColumn)"/>) is unaffected and always
        /// freezes a pixel width — and pins that column out of fill management. Opted-out / skipped
        /// columns keep their own width and the participating columns fill around them. Default
        /// <c>false</c> (each column freezes its own content-fit pixel width). Changing this
        /// property does not itself resize columns; call <see cref="BestFitAllColumns()"/> to apply.
        /// </summary>
        public static readonly DependencyProperty BestFitFillViewportProperty =
            DependencyProperty.Register(nameof(BestFitFillViewport), typeof(bool), typeof(SearchDataGrid),
                new FrameworkPropertyMetadata(true));

        public bool BestFitFillViewport
        {
            get => (bool)GetValue(BestFitFillViewportProperty);
            set => SetValue(BestFitFillViewportProperty, value);
        }

        /// <summary>
        /// Content-fit width (px) of each column participating in the active fill-viewport layout,
        /// captured by the last fill best-fit pass. <see cref="ApplyFillViewportLayout"/> re-reads
        /// these against the current viewport width to decide star-fill vs. pixel-overflow without
        /// re-measuring. Empty when no fill layout is active.
        /// </summary>
        private readonly Dictionary<GridColumn, double> _fillContentWidths = new();

        /// <summary>Whether a fill-viewport layout is currently in effect.</summary>
        private bool _fillViewportLayoutActive;

        /// <summary>
        /// Current fill regime: <c>true</c> = star (fits), <c>false</c> = pixel (overflows),
        /// <c>null</c> = not yet applied. Gates <see cref="ApplyFillViewportLayout"/> so it only
        /// rewrites widths when the regime actually flips — within a regime WPF handles resizes.
        /// </summary>
        private bool? _fillCurrentlyStar;

        /// <summary>
        /// Latch so multiple source changes inside one dispatcher frame coalesce into a single
        /// best-fit pass. Stays latched while a not-yet-loaded grid waits for <c>Loaded</c>.
        /// </summary>
        private bool _bestFitOnSourceChangePending;

        /// <summary>
        /// Called at the end of <c>OnItemsSourceChanged</c> for real source changes. Defers the
        /// pass to <see cref="DispatcherPriority.ContextIdle"/> so column generation, filter
        /// re-application, the group projection, and row realization all settle first.
        /// </summary>
        private void ScheduleBestFitOnSourceChange()
        {
            if (BestFitModeOnSourceChange == BestFitMode.Default || _bestFitOnSourceChangePending)
                return;

            _bestFitOnSourceChangePending = true;
            Dispatcher.BeginInvoke(new Action(RunPendingBestFitOnSourceChange), DispatcherPriority.ContextIdle);
        }

        private void RunPendingBestFitOnSourceChange()
        {
            // Column generation is gated on IsLoaded (late-binding sources) — wait for Loaded,
            // keeping the latch held so further source changes don't double-schedule.
            if (!IsLoaded)
            {
                Loaded += OnLoadedRunPendingBestFit;
                return;
            }

            _bestFitOnSourceChangePending = false;
            var mode = BestFitModeOnSourceChange;
            if (mode == BestFitMode.Default)
                return;

            bool fill = BeginBestFitAll();
            foreach (var descriptor in GridColumns)
            {
                if (descriptor?.InternalColumn == null || !descriptor.Visible || !descriptor.ActualAllowBestFit)
                    continue;

                // The on-source-change mode acts as the pass's grid default: an explicit
                // column-level BestFitMode still wins.
                var effectiveMode = descriptor.BestFitMode != BestFitMode.Default
                    ? descriptor.BestFitMode
                    : mode;
                BestFitAllColumnEntry(descriptor, descriptor.BestFitArea, effectiveMode,
                    descriptor.BestFitMaxRowCount, fill);
            }
            EndBestFitAll(fill);
        }

        private void OnLoadedRunPendingBestFit(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoadedRunPendingBestFit;
            Dispatcher.BeginInvoke(new Action(RunPendingBestFitOnSourceChange), DispatcherPriority.ContextIdle);
        }

        /// <summary>
        /// Chrome allowance (padding / borders) added to text-measured cell content when no
        /// realized cell is available to calibrate against.
        /// </summary>
        private const double FallbackCellChrome = 12.0;

        /// <summary>
        /// Chrome allowance (padding, sort glyph, filter icon) added to the measured header
        /// caption when the column's header isn't realized.
        /// </summary>
        private const double FallbackHeaderChrome = 28.0;

        /// <summary>
        /// Auto-sizes <paramref name="column"/> to fit its content and freezes the result as a
        /// pixel width, clamped to the column's Min/MaxWidth. Measurement options come from the
        /// column's <see cref="ColumnLayoutBase.BestFitArea"/> /
        /// <see cref="ColumnLayoutBase.BestFitMode"/> /
        /// <see cref="ColumnLayoutBase.BestFitMaxRowCount"/>. Runs regardless of
        /// <see cref="ColumnLayoutBase.ActualAllowBestFit"/> — that flag gates the UI gestures,
        /// not the API.
        /// </summary>
        public void BestFitColumn(GridColumn column)
        {
            if (column?.InternalColumn == null || !column.Visible)
                return;

            // Single-column best-fit always freezes a pixel width — filling the viewport is a
            // grid-wide policy (BestFitFillViewport) that only the best-fit-all paths honor. An
            // explicit single fit also pins the column out of any active fill layout.
            _fillContentWidths.Remove(column);
            BestFitColumnCore(column, column.BestFitArea, ResolveBestFitMode(column, BestFitMode.Default), column.BestFitMaxRowCount);
        }

        /// <summary>
        /// Auto-sizes <paramref name="column"/> with explicit measurement options, overriding
        /// the column's own BestFit* settings.
        /// </summary>
        /// <param name="column">The column descriptor to size.</param>
        /// <param name="area">Which parts of the column participate in the measurement.</param>
        /// <param name="mode">How many rows to measure. <see cref="Wpf.BestFitMode.Default"/>
        /// falls back to the column's <see cref="ColumnLayoutBase.BestFitMode"/>, then the
        /// grid's <see cref="BestFitMode"/>.</param>
        /// <param name="maxRowCount">Cap on rows scanned in
        /// <see cref="Wpf.BestFitMode.AllRows"/>; negative = unlimited.</param>
        public void BestFitColumn(GridColumn column, BestFitArea area, BestFitMode mode, int maxRowCount)
        {
            if (column?.InternalColumn == null || !column.Visible)
                return;

            _fillContentWidths.Remove(column);
            BestFitColumnCore(column, area, ResolveBestFitMode(column, mode), maxRowCount);
        }

        /// <summary>
        /// Auto-sizes every visible column that allows best-fit
        /// (<see cref="ColumnLayoutBase.ActualAllowBestFit"/>) using each column's own BestFit*
        /// settings. This is the bulk entry the "Best Fit (all columns)" menu uses, so opted-out
        /// columns are skipped — call <see cref="BestFitColumn(GridColumn)"/> directly to size
        /// one regardless. When <see cref="BestFitFillViewport"/> is <c>true</c>, the participating
        /// columns are star-sized to fill the viewport (overflowing to a scrollbar past it) rather
        /// than frozen at their content-fit pixel width.
        /// </summary>
        public void BestFitAllColumns()
        {
            bool fill = BeginBestFitAll();
            foreach (var descriptor in GridColumns)
            {
                if (descriptor?.InternalColumn == null || !descriptor.Visible || !descriptor.ActualAllowBestFit)
                    continue;

                BestFitAllColumnEntry(descriptor, descriptor.BestFitArea,
                    ResolveBestFitMode(descriptor, BestFitMode.Default), descriptor.BestFitMaxRowCount, fill);
            }
            EndBestFitAll(fill);
        }

        /// <summary>
        /// Auto-sizes every visible column that allows best-fit, with explicit measurement
        /// options overriding the columns' own BestFit* settings. Still honors the grid-level
        /// <see cref="BestFitFillViewport"/> policy (it's a layout choice, not a measurement one).
        /// </summary>
        public void BestFitAllColumns(BestFitArea area, BestFitMode mode, int maxRowCount)
        {
            bool fill = BeginBestFitAll();
            foreach (var descriptor in GridColumns)
            {
                if (descriptor?.InternalColumn == null || !descriptor.Visible || !descriptor.ActualAllowBestFit)
                    continue;

                BestFitAllColumnEntry(descriptor, area, ResolveBestFitMode(descriptor, mode), maxRowCount, fill);
            }
            EndBestFitAll(fill);
        }

        /// <summary>
        /// Resolves the effective measurement mode: explicit request &gt; column
        /// <see cref="ColumnLayoutBase.BestFitMode"/> &gt; grid <see cref="BestFitMode"/> &gt;
        /// <see cref="Wpf.BestFitMode.AllRows"/>.
        /// </summary>
        private BestFitMode ResolveBestFitMode(GridColumn descriptor, BestFitMode requested)
        {
            if (requested != BestFitMode.Default)
                return requested;

            var columnMode = descriptor.BestFitMode;
            if (columnMode != BestFitMode.Default)
                return columnMode;

            var gridMode = BestFitMode;
            return gridMode == BestFitMode.Default ? BestFitMode.AllRows : gridMode;
        }

        private void BestFitColumnCore(GridColumn descriptor, BestFitArea area, BestFitMode mode, int maxRowCount)
        {
            double? width = MeasureBestFitWidth(descriptor, area, mode, maxRowCount);
            if (width == null)
                return;

            // Pixel width flows through the descriptor (not the internal column) so the descriptor
            // stays the source of truth — OnLayoutPropertyChanged syncs it down.
            descriptor.Width = new DataGridLength(width.Value);
        }

        /// <summary>
        /// Measures the column's content-fit width with the given options: data cells and/or the
        /// header per <paramref name="area"/>, clamped to the column's Min/MaxWidth and rounded up.
        /// Updates <see cref="ColumnLayoutBase.ActualDataWidth"/>. Returns the width, or <c>null</c>
        /// when nothing measurable was found. Does not assign the width — callers decide whether to
        /// freeze a pixel width or feed it into the fill-viewport layout.
        /// </summary>
        private double? MeasureBestFitWidth(GridColumn descriptor, BestFitArea area, BestFitMode mode, int maxRowCount)
        {
            var column = descriptor.InternalColumn;
            double width = 0;

            if (area != BestFitArea.Header)
            {
                double dataWidth = MeasureDataWidth(descriptor, column, mode, maxRowCount);
                if (dataWidth > 0)
                    descriptor.SetActualDataWidth(dataWidth);
                width = Math.Max(width, dataWidth);
            }

            if (area != BestFitArea.Rows)
                width = Math.Max(width, MeasureHeaderWidth(descriptor, column));

            if (width <= 0)
                return null;

            width = Math.Max(width, descriptor.MinWidth);
            double max = descriptor.MaxWidth;
            if (!double.IsNaN(max) && !double.IsPositiveInfinity(max) && max > 0)
                width = Math.Min(width, max);

            return Math.Ceiling(width);
        }

        /// <summary>
        /// Opens a best-fit-all pass. Returns whether the fill-viewport policy
        /// (<see cref="BestFitFillViewport"/>) is in effect; when it is, the participating columns'
        /// content widths are re-collected from scratch, otherwise any active fill layout is torn
        /// down so the pass freezes plain pixel widths.
        /// </summary>
        private bool BeginBestFitAll()
        {
            bool fill = BestFitFillViewport;
            if (fill)
                _fillContentWidths.Clear();
            else
                ClearFillViewportLayout();
            return fill;
        }

        /// <summary>
        /// Contributes one column to a best-fit-all pass: in fill mode it records the measured
        /// content width for <see cref="ApplyFillViewportLayout"/>; otherwise it freezes a pixel
        /// width immediately.
        /// </summary>
        private void BestFitAllColumnEntry(GridColumn descriptor, BestFitArea area, BestFitMode mode, int maxRowCount, bool fill)
        {
            if (fill)
            {
                double? width = MeasureBestFitWidth(descriptor, area, mode, maxRowCount);
                if (width != null)
                    _fillContentWidths[descriptor] = width.Value;
            }
            else
            {
                BestFitColumnCore(descriptor, area, mode, maxRowCount);
            }
        }

        /// <summary>
        /// Closes a best-fit-all pass. For a fill pass, activates the layout and applies the
        /// initial star/pixel regime; a no-op otherwise.
        /// </summary>
        private void EndBestFitAll(bool fill)
        {
            if (!fill)
                return;

            _fillViewportLayoutActive = _fillContentWidths.Count > 0;
            _fillCurrentlyStar = null; // force the next apply to write widths
            ApplyFillViewportLayout();
        }

        /// <summary>
        /// Tears down any active fill-viewport layout. Column widths themselves are rewritten by
        /// the caller's pixel best-fit; this just drops the cached state so later viewport changes
        /// stop re-applying the fill.
        /// </summary>
        private void ClearFillViewportLayout()
        {
            _fillContentWidths.Clear();
            _fillViewportLayoutActive = false;
            _fillCurrentlyStar = null;
        }

        /// <summary>
        /// Applies the current fill-viewport regime to the participating columns. When their
        /// combined content width (plus the fixed / opted-out columns) fits the scroll viewport,
        /// the participating columns are star-sized weighted by content width so they stretch to
        /// fill it; otherwise they're frozen at their content pixel width so the grid overflows to
        /// a horizontal scrollbar with every column at full width. Gated on a regime flip so it
        /// only rewrites widths when crossing the fit/overflow boundary — within a regime WPF
        /// handles resizes (star re-fills; pixel scrolls) and manual column resizes survive.
        /// Re-runs on viewport-width changes via <c>OnScrollViewerScrollChanged</c>. No-op until
        /// the viewport has a width, so a later viewport-width change picks it up.
        /// </summary>
        private void ApplyFillViewportLayout()
        {
            if (!_fillViewportLayoutActive || _fillContentWidths.Count == 0)
                return;

            double available = (_scrollViewer?.ViewportWidth ?? 0) - RowHeaderActualWidth;
            if (available <= 0)
                return;

            double participating = 0;
            foreach (var kv in _fillContentWidths)
            {
                var d = kv.Key;
                if (d?.InternalColumn == null || !d.Visible)
                    continue;
                participating += kv.Value;
            }

            // Visible columns not managed by the fill (opted-out / pinned by a single best-fit)
            // keep their rendered width and the participating columns fill around them.
            double fixedColumns = 0;
            foreach (var d in GridColumns)
            {
                if (d?.InternalColumn == null || !d.Visible || _fillContentWidths.ContainsKey(d))
                    continue;
                fixedColumns += d.InternalColumn.ActualWidth;
            }

            bool star = participating + fixedColumns <= available;
            if (_fillCurrentlyStar == star)
                return;

            _fillCurrentlyStar = star;
            foreach (var kv in _fillContentWidths)
            {
                var d = kv.Key;
                if (d?.InternalColumn == null || !d.Visible)
                    continue;

                d.Width = star
                    ? new DataGridLength(kv.Value, DataGridLengthUnitType.Star)
                    : new DataGridLength(kv.Value);
            }
        }

        /// <summary>
        /// Widest data-cell width for the column: realized cells measured directly; in
        /// <see cref="BestFitMode.AllRows"/> additionally every filtered leaf row's display text,
        /// plus cell chrome calibrated from the realized cells (desired width minus that same
        /// row's text width — adapts to the theme's actual padding/borders).
        /// </summary>
        private double MeasureDataWidth(GridColumn descriptor, DataGridColumn column, BestFitMode mode, int maxRowCount)
        {
            bool textScan = mode == BestFitMode.AllRows && CanTextScan(descriptor, column);
            IDisplayValueProvider provider = textScan ? DisplayValueProviderFactory.Create(descriptor) : null;
            Typeface typeface = null;
            double pixelsPerDip = 1.0;
            if (textScan)
            {
                typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            }

            double realizedMax = 0;
            double chrome = -1;
            foreach (var row in EnumerateRealizedDataRows())
            {
                var cell = GetCellAt(row, column);
                if (cell == null)
                    continue;

                double desired = MeasureElementWidth(cell);
                realizedMax = Math.Max(realizedMax, desired);

                if (textScan)
                {
                    string text = FormatCellText(provider, descriptor, row.Item);
                    double textWidth = string.IsNullOrEmpty(text)
                        ? 0
                        : MeasureTextWidth(text, typeface, pixelsPerDip);
                    chrome = Math.Max(chrome, desired - textWidth);
                }
            }

            if (!textScan)
                return realizedMax;

            double cellChrome = chrome >= 0 ? chrome : FallbackCellChrome;
            double textMax = 0;
            int scanned = 0;
            var measured = new HashSet<string>();
            foreach (var item in EnumerateFilteredLeafRows())
            {
                if (maxRowCount >= 0 && scanned >= maxRowCount)
                    break;
                scanned++;

                string text = FormatCellText(provider, descriptor, item);
                if (string.IsNullOrEmpty(text) || !measured.Add(text))
                    continue;

                textMax = Math.Max(textMax, MeasureTextWidth(text, typeface, pixelsPerDip));
            }

            return Math.Max(realizedMax, textMax > 0 ? textMax + cellChrome : 0);
        }

        /// <summary>
        /// Ideal header width: the realized header measured with an infinite constraint (captures
        /// caption, padding, sort glyph, and filter-icon chrome from the live template); falls
        /// back to measuring <see cref="ColumnLayoutBase.HeaderCaption"/> as text plus
        /// <see cref="FallbackHeaderChrome"/> when the header isn't realized.
        /// </summary>
        private double MeasureHeaderWidth(GridColumn descriptor, DataGridColumn column)
        {
            var header = FindColumnHeader(column);
            if (header != null)
                return MeasureElementWidth(header);

            string caption = descriptor.HeaderCaption;
            if (string.IsNullOrEmpty(caption))
                return 0;

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            return MeasureTextWidth(caption, typeface, pixelsPerDip) + FallbackHeaderChrome;
        }

        /// <summary>
        /// True when the column's cell content renders through the text display pipeline and can
        /// be measured as <see cref="FormattedText"/>. User cell templates and checkbox columns
        /// have no text representation that matches what's rendered, so they stay realized-only.
        /// </summary>
        private static bool CanTextScan(GridColumn descriptor, DataGridColumn column)
            => !string.IsNullOrEmpty(descriptor.FieldName)
               && !descriptor.HasUserCellTemplate
               && column is not DataGridCheckBoxColumn;

        /// <summary>
        /// The display string the cell renders for <paramref name="item"/>: the column's display
        /// provider (mask &gt; converter &gt; edit-settings mask &gt; string format &gt; combo
        /// lookup) over the raw field value, falling back to <c>ToString</c>. Null values render
        /// empty, matching the cell (unlike the copy pipeline's "NULL" literal).
        /// </summary>
        private static string FormatCellText(IDisplayValueProvider provider, GridColumn descriptor, object item)
        {
            object raw = item == null ? null : ReflectionHelper.GetPropValue(item, descriptor.FieldName);
            if (raw == null)
                return null;

            return provider != null
                ? provider.FormatValue(raw) ?? raw.ToString()
                : raw.ToString();
        }

        private double MeasureTextWidth(string text, Typeface typeface, double pixelsPerDip)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentUICulture,
                FlowDirection,
                typeface,
                FontSize,
                Brushes.Black,
                pixelsPerDip);
            return formatted.WidthIncludingTrailingWhitespace;
        }

        /// <summary>
        /// Measures <paramref name="element"/> with an infinite constraint to get its
        /// content-driven desired width, then re-invalidates so the next layout pass restores the
        /// real (column-width) constraint. Replaces the legacy SizeToCells/SizeToHeader +
        /// <c>UpdateLayout</c> round-trips.
        /// </summary>
        private static double MeasureElementWidth(UIElement element)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double width = element.DesiredSize.Width;
            element.InvalidateMeasure();
            return width;
        }

        /// <summary>
        /// Realized data-row containers: visible <see cref="DataGridRow"/>s under the rows
        /// presenter, excluding group-header sentinels and the new-item placeholder.
        /// </summary>
        private IEnumerable<DataGridRow> EnumerateRealizedDataRows()
        {
            var presenter = VisualTreeHelperMethods.FindVisualDescendant<DataGridRowsPresenter>(this);
            if (presenter == null)
                yield break;

            int count = VisualTreeHelper.GetChildrenCount(presenter);
            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(presenter, i) is not DataGridRow row)
                    continue;
                if (!row.IsVisible)
                    continue;
                if (IsSentinelRow(row.Item) || row.Item == CollectionView.NewItemPlaceholder)
                    continue;

                yield return row;
            }
        }

        #endregion
    }
}
