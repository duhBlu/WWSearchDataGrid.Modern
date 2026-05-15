using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Auto-filter row plumbing on <see cref="SearchDataGrid"/>: the registry mapping a
    /// <see cref="DataGridColumn"/> to its <c>ColumnFilterControl</c> host, used by
    /// <see cref="AutoFilterRowPresenter"/> to resolve a stable per-column child for the
    /// pinned filter row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Phase 2 wires the surface point only — the registry is always empty and
    /// <see cref="FindColumnFilterControl"/> returns <c>null</c>, which the presenter handles
    /// by parenting a placeholder Border per column. Phase 3 introduces
    /// <c>ColumnFilterControl</c> and registers each instance in its <c>OnApplyTemplate</c>.
    /// </para>
    /// <para>
    /// The registry is keyed off <see cref="DataGridColumn"/> rather than the recycled
    /// <see cref="System.Windows.Controls.Primitives.DataGridColumnHeader"/> so the lookup
    /// survives header virtualization: when WPF recycles a header to a different column the
    /// header object identity changes but the column does not.
    /// </para>
    /// </remarks>
    public partial class SearchDataGrid
    {
        private readonly Dictionary<DataGridColumn, object> _columnFilterControls
            = new Dictionary<DataGridColumn, object>();

        /// <summary>
        /// Resolves the column's filter control host, or <c>null</c> if no host is registered.
        /// In Phase 2 always returns <c>null</c> — the registry will be populated by
        /// <c>ColumnFilterControl</c> in Phase 3.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="object"/> rather than a typed control so this method's signature
        /// stays stable across phases without referencing a type that doesn't yet exist.
        /// Callers cast to <c>ColumnFilterControl</c> once Phase 3 lands.
        /// </remarks>
        public object FindColumnFilterControl(DataGridColumn column)
        {
            if (column == null) return null;
            return _columnFilterControls.TryGetValue(column, out var ctl) ? ctl : null;
        }

        /// <summary>
        /// Internal registration hook used by <c>ColumnFilterControl</c> in Phase 3.
        /// Pass <c>null</c> to unregister.
        /// </summary>
        internal void RegisterColumnFilterControl(DataGridColumn column, object control)
        {
            if (column == null) return;
            if (control == null)
                _columnFilterControls.Remove(column);
            else
                _columnFilterControls[column] = control;
        }

        /// <summary>
        /// Returns the filter cells in <see cref="DataGridColumn.DisplayIndex"/> order,
        /// restricted to entries whose column is currently visible and whose control is
        /// loaded into a live presentation source. The presence + loaded gate excludes the
        /// stale entries that linger in the registry during the brief window between
        /// <see cref="AutoFilterRowPresenter"/>'s synchronous <c>Children.Clear()</c> and
        /// the previous control's <c>Unloaded</c>-driven unregister — focusing one of
        /// those would be a no-op (the control has no live visual parent).
        /// </summary>
        /// <remarks>
        /// Consumed by <c>FilterRowNavigator</c> to compute Tab/arrow targets in the order
        /// the user sees on screen, which is independent from the registry's column-insertion
        /// order. The list is freshly allocated per call — the registry is small (one entry
        /// per column) and the call rate is keystroke-frequency, so amortizing via a cached
        /// view isn't worth the invalidation bookkeeping under column add/remove/reorder.
        /// </remarks>
        public IReadOnlyList<ColumnFilterControl> GetFilterControlsInDisplayOrder()
        {
            if (_columnFilterControls.Count == 0)
                return Array.Empty<ColumnFilterControl>();

            var live = new List<(int displayIndex, ColumnFilterControl ctl)>(_columnFilterControls.Count);
            foreach (var pair in _columnFilterControls)
            {
                var column = pair.Key;
                if (column == null) continue;
                if (column.Visibility != Visibility.Visible) continue;
                if (pair.Value is not ColumnFilterControl ctl) continue;
                if (!ctl.IsLoaded) continue;
                if (PresentationSource.FromVisual(ctl) == null) continue;
                live.Add((column.DisplayIndex, ctl));
            }
            live.Sort(static (a, b) => a.displayIndex.CompareTo(b.displayIndex));

            var result = new ColumnFilterControl[live.Count];
            for (int i = 0; i < live.Count; i++)
                result[i] = live[i].ctl;
            return result;
        }

        /// <summary>
        /// Focuses the filter cell for <paramref name="column"/>. Returns <c>false</c> if
        /// no cell is registered for the column, the cell isn't currently in a live visual
        /// tree, or the focus call itself reports failure (Focus() returns false when the
        /// element is disabled, collapsed, or already keyboard-focused inside).
        /// </summary>
        /// <remarks>
        /// Focusing the host triggers the existing
        /// <see cref="System.Windows.UIElement.IsKeyboardFocusWithin"/> pipeline on
        /// <c>ColumnFilterControl</c>: the display → edit transition runs and the
        /// materialized editor receives keyboard focus on the dispatcher's next Input
        /// pump. For NoInput operators where the editor is disabled, focus stays on the
        /// host so Enter has a recipient to commit against.
        /// </remarks>
        public bool TryFocusFilterCellForColumn(DataGridColumn column)
        {
            if (column == null) return false;
            if (!_columnFilterControls.TryGetValue(column, out var entry)) return false;
            if (entry is not ColumnFilterControl ctl) return false;
            if (!ctl.IsLoaded) return false;
            if (PresentationSource.FromVisual(ctl) == null) return false;
            return ctl.Focus();
        }

        /// <summary>
        /// Focuses the first visible filter cell in <see cref="DataGridColumn.DisplayIndex"/>
        /// order. Returns <c>false</c> if no visible, loaded filter cells exist.
        /// </summary>
        public bool TryFocusFirstFilterCell()
        {
            var ordered = GetFilterControlsInDisplayOrder();
            return ordered.Count > 0 && ordered[0].Focus();
        }

        /// <summary>
        /// Moves <see cref="DataGrid.CurrentCell"/> to the first visible data row at
        /// <paramref name="column"/> and focuses its <see cref="DataGridCell"/> container.
        /// Used by the filter-row Down arrow / end-of-row Tab handoff. Returns <c>false</c>
        /// when there are no data rows, the target row hasn't realized, or the cell's
        /// <c>Focus()</c> call reports failure.
        /// </summary>
        /// <remarks>
        /// Mirrors the realization dance in <c>GetWrapTargetAtRowEdge</c>: under
        /// virtualization the target row's container may not exist until
        /// <see cref="DataGrid.ScrollIntoView(object, DataGridColumn)"/> +
        /// <see cref="UIElement.UpdateLayout"/> drives a measure pass. The cell lookup
        /// is then via <c>GetCellAt</c> on the realized row container.
        /// <see cref="System.Windows.Data.CollectionView.NewItemPlaceholder"/> is skipped
        /// — it isn't a real data row and focusing it would attempt to begin add-new on
        /// a column the filter row is trying to leave.
        /// </remarks>
        public bool MoveCurrentCellToFirstDataRow(DataGridColumn column)
        {
            if (column == null) return false;
            if (Items == null || Items.Count == 0) return false;

            object firstItem = null;
            foreach (var item in Items)
            {
                if (item == null) continue;
                if (item == CollectionView.NewItemPlaceholder) continue;
                firstItem = item;
                break;
            }
            if (firstItem == null) return false;

            CurrentCell = new DataGridCellInfo(firstItem, column);
            ScrollIntoView(firstItem, column);
            UpdateLayout();

            if (ItemContainerGenerator.ContainerFromItem(firstItem) is not DataGridRow row)
                return false;
            var cell = GetCellAt(row, column);
            return cell != null && cell.Focus();
        }

        /// <summary>
        /// Moves <see cref="DataGrid.CurrentCell"/> to the first visible cell of the first
        /// visible data row (leftmost <see cref="DataGridColumn.DisplayIndex"/> among
        /// columns with <see cref="UIElement.Visibility"/> <c>Visible</c>) and focuses it.
        /// Used by the end-of-row Tab handoff to land the user on a clean entry point
        /// into the data area rather than the column they happened to be in.
        /// </summary>
        public bool MoveCurrentCellToFirstDataCell()
        {
            DataGridColumn firstVisible = null;
            int bestDisplayIndex = int.MaxValue;
            foreach (var col in Columns)
            {
                if (col == null) continue;
                if (col.Visibility != Visibility.Visible) continue;
                if (col.DisplayIndex < bestDisplayIndex)
                {
                    firstVisible = col;
                    bestDisplayIndex = col.DisplayIndex;
                }
            }
            return firstVisible != null && MoveCurrentCellToFirstDataRow(firstVisible);
        }
    }
}
