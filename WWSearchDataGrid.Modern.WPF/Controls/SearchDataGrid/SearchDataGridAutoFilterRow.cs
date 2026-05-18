using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Auto-filter row plumbing: a registry mapping <see cref="DataGridColumn"/> to its
    /// <c>ColumnFilterControl</c> host. Keyed by column (not header) so the lookup survives
    /// header virtualization.
    /// </summary>
    public partial class SearchDataGrid
    {
        private readonly Dictionary<DataGridColumn, object> _columnFilterControls
            = new Dictionary<DataGridColumn, object>();

        /// <summary>
        /// Returns the column's filter control host, or <c>null</c> if unregistered.
        /// Callers cast to <c>ColumnFilterControl</c>.
        /// </summary>
        public object FindColumnFilterControl(DataGridColumn column)
        {
            if (column == null) return null;
            return _columnFilterControls.TryGetValue(column, out var ctl) ? ctl : null;
        }

        /// <summary>
        /// Registration hook called by <c>ColumnFilterControl.OnApplyTemplate</c>. Pass
        /// <c>null</c> to unregister.
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
        /// Visible filter cells in <see cref="DataGridColumn.DisplayIndex"/> order. Loaded +
        /// presentation-source gate excludes stale entries lingering between Children.Clear()
        /// and the prior control's Unloaded-driven unregister. Consumed by <c>FilterRowNavigator</c>.
        /// </summary>
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
        /// Focuses the column's filter cell, triggering its display → edit transition.
        /// Returns false if unregistered, not in a live visual tree, or Focus() fails.
        /// </summary>
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
        /// Moves CurrentCell to the column's first visible data row and focuses the cell.
        /// Used by Down-arrow / end-of-row Tab handoff. ScrollIntoView + UpdateLayout drive
        /// virtualization realization; NewItemPlaceholder is skipped.
        /// </summary>
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
        /// Moves CurrentCell to the leftmost visible column of the first visible row. Used
        /// by the end-of-row Tab handoff so the user lands on a clean entry point.
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
